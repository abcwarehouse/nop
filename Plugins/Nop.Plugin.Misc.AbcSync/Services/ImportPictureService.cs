using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Logging;
using Nop.Core.Domain.Media;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcSync.Services.Staging;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace Nop.Plugin.Misc.AbcSync.Services
{
    public class ImportPictureService : BaseAbcWarehouseService, IImportPictureService
    {
        private readonly string ImportFirstPictureOnlySettingKey = "abcwarehouse.importfirstpictureonly";
        private readonly bool ImportFirstPictureOnly;

        private readonly Dictionary<string, int> ExistingImageToId;
        private readonly HashSet<string> ExistingImagesWithMappings;

        private readonly IRepository<Picture> _pictureRepository;
        private readonly IRepository<ProductPicture> _productPictureRepository;
        private readonly INopDataProvider _nopDbContext;
        private readonly IImportUtilities _importUtilities;
        private readonly ILogger _logger;
        private readonly ImportSettings _importSettings;
        private readonly ISettingService _settingService;
        private readonly IPictureService _pictureService;
        private readonly IProductDataProductService _productDataProductService;
        private readonly IProductDataProductImageService _productDataProductImageService;

        private static readonly string shortLogWarning = "Picture Import: Failed to download picture";
        private static readonly string fullLogWarning = "Picture Import, failed to download picture from URL: {0} for product: {1} with id: {2}. Exception thrown: {3}";

        public ImportPictureService(
            IRepository<Picture> pictureRepository,
            IRepository<ProductPicture> productPictureRepository,
            INopDataProvider nopDbContext,
            IImportUtilities importUtilities,
            ILogger logger,
            ImportSettings importSettings,
            ISettingService settingService,
            IPictureService pictureService,
            IProductDataProductService productDataProductService,
            IProductDataProductImageService productDataProductImageService
        )
        {
            _pictureRepository = pictureRepository;
            _productPictureRepository = productPictureRepository;

            _nopDbContext = nopDbContext;
            _importUtilities = importUtilities;
            _logger = logger;

            _importSettings = importSettings;
            _settingService = settingService;
            _pictureService = pictureService;

            _productDataProductService = productDataProductService;
            _productDataProductImageService = productDataProductImageService;

            ImportFirstPictureOnly = Convert.ToBoolean(_settingService.GetSetting(ImportFirstPictureOnlySettingKey)?.Value);

            ExistingImageToId = _pictureRepository.Table
                                                  .Where(p => p.SeoFilename != null).ToList()
                                                  .GroupBy(p => p.SeoFilename)
                                                  .Select(pg => pg.FirstOrDefault())
                                                  .Select(p => new { p.SeoFilename, ID = p.Id })
                                                  .ToDictionary(o => o.SeoFilename, o => o.ID);
            var existingImageIds = new HashSet<int>(
                _productPictureRepository.Table.Select(
                    ppm => ppm.PictureId).Distinct());
            ExistingImagesWithMappings = new HashSet<string>(
                _pictureRepository.Table.Where(p => existingImageIds.Contains(p.Id)).Select(p => p.SeoFilename).Distinct()
            );
        }

        public void ImportSiteOnTimePictures()
        {
            if (!_pictureService.StoreInDb)
            {
                _logger.Warning("Images not stored in DB, cannot run Import Site On Time Pictures task.");
                return;
            }

            // get an enumerable of all ProductDataProducts
            var sotProducts = _productDataProductService.GetProductDataProducts()
                                                       .Select(sp => new { sp.id, sp.SKU, sp.large }).ToList();

            List<ProductPicture> productPictures = new List<ProductPicture>();

            PictureInsertManager pictureManager = new PictureInsertManager(_nopDbContext);
            EntityManager<ProductPicture> productPictureManager
                = new EntityManager<ProductPicture>(_productPictureRepository);

            // find sot products with matching nop sku
            foreach (var sotProduct in sotProducts)
            {
                // got a product with the corresponding sotproduct sku
                Product product = _importUtilities.GetExistingProductBySku(sotProduct.SKU);
                if (product == null || product.Deleted || !product.Published)
                {
                    continue;
                }

                // grab all picture's urls
                List<string> pictureUrls = new List<string>
                {
                    sotProduct.large
                };

                if (!ImportFirstPictureOnly)
                {
                    // additional pictures in sot database
                    List<string> additionalImageUrls =
                        _productDataProductImageService.GetProductDataProductImages()
                        .Where(pp => pp.ProductDataProduct_id == sotProduct.id)
                        .Select(pp => pp.Large).ToList();
                    pictureUrls.AddRange(additionalImageUrls);
                }

                // download all images related to that product
                foreach (string url in pictureUrls)
                {
                    if (url == null)
                    {
                        _logger.Warning($"SOT Image URL was null for SKU: {sotProduct.SKU}, skipping processing");
                        continue;
                    }

                    if (!url.Contains("http"))
                    {
                        continue;
                    }

                    var unsecureUrl = url.Replace("https:", "http:");

                    // download those images
                    byte[] pictureBytes = null;
                    //currently skipping images that have already been downloaded
                    string seoName = Path.GetFileNameWithoutExtension(unsecureUrl);

                    var isExistingImage = ExistingImageToId.ContainsKey(seoName);
                    //if this product image is orphaned, add a new mapping for it
                    if (isExistingImage && !ExistingImagesWithMappings.Contains(seoName))
                    {
                        _productPictureRepository.Insert(new ProductPicture { PictureId = ExistingImageToId[seoName], ProductId = product.Id });
                        ExistingImagesWithMappings.Add(seoName);
                    }

                    try
                    {
                        // make a request to determine if the resource has been modified
                        HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(unsecureUrl);
                        webRequest.Method = "HEAD";
                        HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
                        //no good, skip this picture
                        if (webResponse.StatusCode != HttpStatusCode.OK)
                        {
                            _logger.InsertLog(LogLevel.Warning,
                            shortLogWarning + $". {unsecureUrl} returned status code {webResponse.StatusCode}", $"There was a problem while fetching the picture at {unsecureUrl} for product {product.Name} with id {product.Id}. Response: {webResponse}");
                            webResponse.Close();
                            continue;
                        }

                        // if the picture has not been modified since the last picture update, we skip downloading and updating
                        if (isExistingImage && DateTime.Compare(webResponse.LastModified, _importSettings.LastPictureUpdate) < 0)
                        {
                            webResponse.Close();
                            continue;
                        }
                        webResponse.Close();

                        using (var webClient = new WebClient())
                        {
                            // Could I add TLS 1.2 reference here?
                            pictureBytes = webClient.DownloadData(unsecureUrl);
                        }
                    }
                    catch (WebException ex)
                    {
                        // log/show error 
                        _logger.InsertLog(LogLevel.Warning,
                            shortLogWarning, string.Format(fullLogWarning, unsecureUrl, product.Name, product.Id, ex));
                        continue;
                    }
                    catch (UriFormatException ex)
                    {
                        _logger.InsertLog(LogLevel.Warning,
                            shortLogWarning, string.Format(fullLogWarning, unsecureUrl, product.Name, product.Id, ex));
                        continue;
                    }
                    catch (ArgumentNullException ex)
                    {
                        _logger.InsertLog(LogLevel.Warning,
                            shortLogWarning, string.Format(fullLogWarning, unsecureUrl, product.Name, product.Id, ex));
                        continue;
                    }

                    //insert a new image
                    if (!isExistingImage)
                    {
                        pictureManager.Insert(pictureBytes, seoName, unsecureUrl, product);
                    }
                    //else this image already exists and needs an update
                    else
                    {
                        pictureManager.Update(pictureBytes, seoName, unsecureUrl);
                    }
                }
            }

            pictureManager.Flush();
            pictureManager.FlushProductPictures(_productPictureRepository);
        }

        public void DeleteAllProductPictures()
        {
            // clear product pictures
            string deleteQuery = $"DELETE FROM {_nopDbContext.GetTable<Picture>()} WHERE Id in"
                                    + $" (SELECT PictureId FROM {_nopDbContext.GetTable<ProductPicture>()})";

            string deleteProductPictureMappingQuery = $"DELETE FROM {_nopDbContext.GetTable<ProductPicture>()}";

            _nopDbContext.ExecuteNonQuery(deleteQuery);
            _nopDbContext.ExecuteNonQuery(deleteProductPictureMappingQuery);
        }
    }
}