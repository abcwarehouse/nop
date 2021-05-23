using LinqToDB;
using LinqToDB.Data;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Media;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
    public class ImportLocalPicturesTask : IScheduleTask
    {
        private readonly IPictureService _pictureService;
        private readonly ILogger _logger;
        private readonly ImportSettings _importSettings;
        private readonly IRepository<Picture> _pictureRepository;
        private readonly IRepository<ProductPicture> _productPictureRepository;
        private readonly INopDataProvider _nopDbContext;
        private readonly ISettingService _settingService;

        private readonly Dictionary<string, int> ExistingImageToId;
        private readonly HashSet<string> ExistingImagesWithMappings;

        public ImportLocalPicturesTask(
            IPictureService pictureService,
            ILogger logger,
            ImportSettings importSettings,
            IRepository<Picture> pictureRepository,
            IRepository<ProductPicture> productPictureRepository,
            INopDataProvider nopDbContext,
            ISettingService settingService)
        {
            _pictureService = pictureService;
            _logger = logger;
            _importSettings = importSettings;
            _pictureRepository = pictureRepository;
            _productPictureRepository = productPictureRepository;
            _nopDbContext = nopDbContext;
            _settingService = settingService;

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

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipImportLocalPicturesTask)
            {
                this.Skipped();
                return;
            }

            
            if (!(await _pictureService.IsStoreInDbAsync()))
            {
                await _logger.WarningAsync("Images not stored in DB, cannot run Import Local Pictures task.");
                return;
            }

            List<string> pictureFileUrls = new List<string>();

            //only taking the latest file if two exist with different extensions
            var directoryInfo = _importSettings.GetLocalPicturesDirectory();
            pictureFileUrls = directoryInfo.GetFiles()
                .GroupBy(fi => fi.Name.Split('.')[0])
                .Select(g => g.OrderByDescending(fi => fi.LastWriteTime).First())
                .Select(fi => fi.FullName)
                .ToList();

            PictureInsertManager pictureManager = new PictureInsertManager(_nopDbContext);

            // only grab 'large' images
            foreach (string pictureFileUrl in pictureFileUrls)
            {
                await ProcessLocalPictureAsync(pictureFileUrl, pictureManager);
            }
            pictureManager.Flush();
            await pictureManager.FlushProductPicturesAsync(_productPictureRepository);

            var setDisplayOrderCommand = @"
               UPDATE Product_Picture_Mapping
                SET DisplayOrder = -3
                FROM Product_Picture_Mapping 
                JOIN Picture p on PictureId = p.Id
                WHERE p.SeoFilename like '%[_]large[_]0'
            ";

            await _nopDbContext.ExecuteNonQueryAsync(setDisplayOrderCommand);
            _importSettings.LastPictureUpdate = DateTime.Now;
            await _settingService.SaveSettingAsync(_importSettings);
            
        }

        private async System.Threading.Tasks.Task ProcessLocalPictureAsync(string pictureFileUrl, PictureInsertManager pictureInsertManager)
        {
            // parse filename
            // assume file name is in format: <item number>_<imgSize - thumb/large/etc>.<ext>
            string fullFilename = pictureFileUrl.Substring(pictureFileUrl.LastIndexOf('\\') + 1);

            if (fullFilename == "Thumbs.db")
            {
                return;
            }

            string[] parts = fullFilename.Split('.');
            if (parts.Length < 2)
            {
                await _logger.WarningAsync($"Local picture import: encountered file {fullFilename} with missing extension.");
                return;
            }
            string filename = parts[0];

            parts = filename.Split('_');
            if (parts.Length < 2)
            {
                await _logger.WarningAsync($"Local picture import: encountered file {fullFilename} formatted incorrectly (missing '_').");
                return;
            }
            string itemNum = parts[0];
            string imgSize = parts[1];

            // do string parsing to choose which images we want to use
            if (!imgSize.Equals("large"))
            {
                await _logger.WarningAsync($"Local picture import: encountered file {fullFilename} formatted incorrectly (missing 'large').");
                return;
            }

            var nopProducts = await _nopDbContext.QueryProcAsync<Product>(
                "GetProductByItemNo",
                new DataParameter { Name = "ItemNo", Value = itemNum, DataType = DataType.NVarChar }
            );

            if (nopProducts == null || !nopProducts.Any()) { return ;}

            var nopProduct = nopProducts.First();
            if (nopProduct == null || nopProduct.Deleted)
            {
                return;
            }

            try
            {
                var seoName = itemNum + "_large_0";
                var isExistingImage = ExistingImageToId.ContainsKey(seoName);

                // insert the picture, map it to the product we found
                byte[] pictureBytes;
                //if this product image is orphaned, add a new mapping for it
                if (isExistingImage && !ExistingImagesWithMappings.Contains(seoName))
                {
                    await _productPictureRepository.InsertAsync(new ProductPicture { PictureId = ExistingImageToId[seoName], ProductId = nopProduct.Id });
                    ExistingImagesWithMappings.Add(seoName);
                }
                else if (!isExistingImage)
                {
                    pictureBytes = File.ReadAllBytes(pictureFileUrl);
                    // insert the picture if no file errors
                    pictureInsertManager.Insert(pictureBytes, seoName, pictureFileUrl, nopProduct);
                    await _logger.InformationAsync($"Product #{itemNum} local image added.");
                }

                if (isExistingImage)
                {
                    pictureBytes = File.ReadAllBytes(pictureFileUrl);

                    var matchingPicture = _pictureRepository.Table
                                                            .Where(p => p.SeoFilename == seoName)
                                                            .FirstOrDefault();

                    var existingPictureBytes = await _pictureService.LoadPictureBinaryAsync(matchingPicture);

                    // If we didn't find an exact matching picture, update the one in place
                    if (matchingPicture == null || !existingPictureBytes.SequenceEqual(pictureBytes))
                    {
                        pictureInsertManager.Update(pictureBytes, seoName, pictureFileUrl);
                        await _logger.InformationAsync($"Product #{itemNum} local image updated.");
                    }
                }
            }
            catch (IOException ioException)
            {
                throw new NopException("Task Failed: Could not open the file, Exception Details: " + ioException.Message);
            }
            catch (UnauthorizedAccessException unauthorizedAccessException)
            {
                throw new NopException("Task failed: Could not open the file because the current user does not have permissions for " + pictureFileUrl + ",  "
                    + "Exception Details: " + unauthorizedAccessException.Message);
            }
        }
    }
}