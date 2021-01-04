using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Net;
using System.Diagnostics;
//using System.Web.Script.Serialization;
using Nop.Services.Logging;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcSync;
using Nop.Plugin.Misc.AbcSync.Data;
using Nop.Core;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync
{
    public class SyncSiteOnTimeTask : IScheduleTask
    {
        private readonly ILogger _logger;
        private readonly AbcSiteOnTimeSyncSettings _settings;
        private readonly StagingDb _stagingDb;
        private readonly IProductDataProductService _productDataProductService;
        private readonly IProductDataProductDimensionService _productDataProductDimensionService;
        private readonly IProductDataProductDownloadService _productDataProductDownloadService;
        private readonly IProductDataProductFeatureService _productDataProductFeatureService;
        private readonly IProductDataProductFilterService _productDataProductFilterService;
        private readonly IProductDataProductImageService _productDataProductImageService;
        private readonly IProductDataProductpmapService _productDataProductpmapService;
        private readonly IProductDataProductRelatedItemService _productDataProductRelatedItemService;
        private readonly ISiteOnTimeBrandService _siteOnTimeBrandService;
        Object lockMe = new Object();

        public SyncSiteOnTimeTask(
            ILogger logger,
            AbcSiteOnTimeSyncSettings settings,
            StagingDb stagingDb,
            IProductDataProductService productDataProductService,
            IProductDataProductDimensionService productDataProductDimensionService,
            IProductDataProductDownloadService productDataProductDownloadService,
            IProductDataProductFeatureService productDataProductFeatureService,
            IProductDataProductFilterService productDataProductFilterService,
            IProductDataProductImageService productDataProductImageService,
            IProductDataProductpmapService productDataProductpmapService,
            IProductDataProductRelatedItemService productDataProductRelatedItemService,
            ISiteOnTimeBrandService siteOnTimeBrandService
        )
        {
            _logger = logger;
            _settings = settings;
            _stagingDb = stagingDb;
            _productDataProductService = productDataProductService;
            _productDataProductDimensionService = productDataProductDimensionService;
            _productDataProductDownloadService = productDataProductDownloadService;
            _productDataProductFeatureService = productDataProductFeatureService;
            _productDataProductFilterService = productDataProductFilterService;
            _productDataProductImageService = productDataProductImageService;
            _productDataProductpmapService = productDataProductpmapService;
            _productDataProductRelatedItemService = productDataProductRelatedItemService;
            _siteOnTimeBrandService = siteOnTimeBrandService;
        }

        public void Execute()
        {
            if (!_settings.AreValid)
            {
                throw new NopException("AbcSiteOnTimeSync Settings are not set up correctly, please set them in ABC Site On Time Sync Configuration.");
            }

            _logger.Information("Site on Time sync started");

            //Start measurement of task running
            var watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            Debug.WriteLine("+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Debug.WriteLine("++++++++++++++++++++++++++++++++++++++++ Start FillStagingSiteOnTime Task +++++++++++++++++++++++++++++++++++++++++");
            Debug.WriteLine("+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");

            main();

            watch.Stop();
            Debug.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Debug.WriteLine("**  End+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Debug.WriteLine($"** Execution Time: {watch.ElapsedMilliseconds} ms, {watch.Elapsed}");
            Debug.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            Debug.WriteLine("++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++");
            _logger.Information("Site on Time sync completed");
        }

        private ProductDataProduct getProductDetail(CJsonItem sku, bool? hawOnly)
        {
            try
            {
                CJsonRoot product = callCMICApi(sku.href);
                if (product == null || product.collection.items == null)
                {
                    return null;
                }

                ProductDataProduct result = new ProductDataProduct();

                CJsonData categoryDataForProduct = product.collection.items[0].data.Find(d => d.name == "category");

                //var categoryMap = cateogryMapListFromDb.Find(c => c.SoT_Id == categoryDataForProduct.oldDbId);
                //if (categoryMap != null && categoryMap.Nop_Id != 2280)                
                //Debug.WriteLine("SKU: {0}", sku.data[0].value);

                var skuDataForProduct = product.collection.items[0].data.Find(d => d.name == "sku");
                result.SKU = skuDataForProduct.value;


                result.pkID = skuDataForProduct.oldDbProductId;


                var brandDataForProduct = product.collection.items[0].data.Find(d => d.name == "brand");
                result.Brand = brandDataForProduct.value;
                result.pkBrand = brandDataForProduct.oldDbId;


                var seriesDataForProduct = product.collection.items[0].data.Find(d => d.name == "series");
                result.SeriesName = (seriesDataForProduct != null) ? seriesDataForProduct.value : "";


                var classNameDataForProduct = product.collection.items[0].data.Find(d => d.name == "class_name");
                result.cgDescription = (classNameDataForProduct != null) ? classNameDataForProduct.value : seriesDataForProduct.value;


                result.CatDescription = categoryDataForProduct.value;


                result.pkCategory = categoryDataForProduct.oldDbId;


                result.ModelDescription = "";
                var descriptionsLinkData = product.collection.items[0].links.Find(link => link.rel == "Descriptions");
                CJsonRoot descriptionListFromApi = callCMICApi(descriptionsLinkData.href);
                if (descriptionListFromApi != null && descriptionListFromApi.collection.items != null)
                {
                    var longer_basic_description_obj = descriptionListFromApi.collection.items.Find(item => item.data[0].value == "Longer Basic Description");
                    if (longer_basic_description_obj != null)
                    {
                        result.ModelDescription = longer_basic_description_obj.data[1].value;
                    }
                    else
                    {
                        var basic_description_obj = descriptionListFromApi.collection.items.Find(item => item.data[0].value == "Basic Description");
                        if (basic_description_obj != null)
                        {
                            result.ModelDescription = basic_description_obj.data[1].value;
                        }
                    }

                    var master_description = descriptionListFromApi.collection.items.Find(item => item.data[0].value == "Master Description");
                    result.ExpandedDescription = (master_description != null) ? master_description.data[1].value : "";
                }


                result.CustomDescription = "";


                var standard_colorDataForProduct = product.collection.items[0].data.Find(d => d.name == "standard_color");
                result.StandardColor = (standard_colorDataForProduct != null) ? standard_colorDataForProduct.value : "";


                var color_descriptionDataForProduct = product.collection.items[0].data.Find(d => d.name == "color_description");
                result.ColorDescription = (color_descriptionDataForProduct != null) ? color_descriptionDataForProduct.value : "";


                var keyFeaturesLinkData = product.collection.items[0].links.Find(link => link.rel == "Key Features");
                CJsonRoot keyFeatureListFromApi = callCMICApi(keyFeaturesLinkData.href);
                if (keyFeatureListFromApi != null && keyFeatureListFromApi.collection.items != null)
                {
                    var keyFeature1 = keyFeatureListFromApi.collection.items.Find(item => item.data[0].name == "feature1");
                    result.KeyFeature1 = (keyFeature1 != null) ? keyFeature1.data[0].value : "";

                    var keyFeature2 = keyFeatureListFromApi.collection.items.Find(item => item.data[0].name == "feature2");
                    result.KeyFeature2 = (keyFeature2 != null) ? keyFeature2.data[0].value : "";

                    var keyFeature3 = keyFeatureListFromApi.collection.items.Find(item => item.data[0].name == "feature3");
                    result.KeyFeature3 = (keyFeature3 != null) ? keyFeature3.data[0].value : "";

                    var keyFeature4 = keyFeatureListFromApi.collection.items.Find(item => item.data[0].name == "feature4");
                    result.KeyFeature4 = (keyFeature4 != null) ? keyFeature4.data[0].value : "";

                    var keyFeature5 = keyFeatureListFromApi.collection.items.Find(item => item.data[0].name == "feature5");
                    result.KeyFeature5 = (keyFeature5 != null) ? keyFeature5.data[0].value : "";
                }


                var root_modelDataForProduct = product.collection.items[0].data.Find(d => d.name == "root_model");
                result.RootModelNumber = (root_modelDataForProduct != null) ? root_modelDataForProduct.value : "";


                result.ModelStatus = "";
                /*
                var model_statusDataForProduct = product.collection.items[0].data.Find(d => d.name == "is_inactive");
                if (model_statusDataForProduct != null)
                {
                    if (model_statusDataForProduct.value == "false")
                    {
                        p.ModelStatus = "A";
                    }
                    if (model_statusDataForProduct.value == "true")
                    {
                        p.ModelStatus = "D";
                    }
                }
                */
                var model_statusDataForProduct = product.collection.items[0].data.Find(d => d.name == "status_codes");
                if (model_statusDataForProduct != null)
                {
                    result.ModelStatus = (model_statusDataForProduct.value == "") ? "A" : model_statusDataForProduct.value;
                }


                result.UPC = "";
                var upcDataForProduct = product.collection.items[0].data.Find(d => d.name == "upc");
                if (upcDataForProduct != null)
                {
                    result.UPC = upcDataForProduct.value;
                }


                result.Haw_Only = "No";
                if (hawOnly == true)
                {
                    result.Haw_Only = "Yes";
                }


                result.Dimensions = new List<ProductDataProductDimension>();
                var measurementsLinkData = product.collection.items[0].links.Find(link => link.rel == "Measurements");
                CJsonRoot measurementListFromApi = callCMICApi(measurementsLinkData.href);
                if (measurementListFromApi != null && measurementListFromApi.collection.items != null)
                {
                    foreach (var measurement in measurementListFromApi.collection.items ?? Enumerable.Empty<CJsonItem>())
                    {
                        ProductDataProductDimension dimension = new ProductDataProductDimension();
                        dimension.MeasurementValue = float.Parse(measurement.data.Find(d => d.name == "measurement_value").value);
                        dimension.FractionalDimensionValue = measurement.data.Find(d => d.name == "measurement_fractional_value").value;
                        dimension.MeasurementName = measurement.data.Find(d => d.name == "measurement_name").value;
                        dimension.UnitsOfMeasuremen = measurement.data.Find(d => d.name == "measurement_units").value;
                        result.Dimensions.Add(dimension);
                    }
                }


                result.Features = new List<ProductDataProductFeature>();
                var featuresLinkData = product.collection.items[0].links.Find(link => link.rel == "Enhanced Features");
                CJsonRoot featureListFromApi = callCMICApi(featuresLinkData.href);
                if (featureListFromApi != null && featureListFromApi.collection.items != null)
                {
                    int seq = 0;
                    foreach (var featureApi in featureListFromApi.collection.items ?? Enumerable.Empty<CJsonItem>())
                    {
                        ProductDataProductFeature featureData = new ProductDataProductFeature();
                        featureData.FeatureCategory = featureApi.data.Find(d => d.name == "feature_name").value;
                        featureData.Featurevalue = featureApi.data.Find(d => d.name == "feature").value;
                        featureData.FeatureSeq = seq++.ToString();

                        result.Features.Add(featureData);
                    }
                }


                result.thumb = "";
                result.large = "";
                result.Images = new List<ProductDataProductImage>();
                result.Downloads = new List<ProductDataProductDownload>();
                var assetsLinkData = product.collection.items[0].links.Find(link => link.rel == "Assets: Images and PDFs");
                CJsonRoot assetListFromApi = callCMICApi(assetsLinkData.href);
                if (assetListFromApi != null && assetListFromApi.collection.items != null)
                {
                    foreach (var assetApi in assetListFromApi.collection.items ?? Enumerable.Empty<CJsonItem>())
                    {
                        if (assetApi.links == null)
                        {
                            break;
                        }

                        if (assetApi.characterization == "Images")
                        {
                            var tmpImageList = new Dictionary<string, ProductDataProductImage>();
                            foreach (var imageApi in assetApi.links.ToList())
                            {
                                if (imageApi.rel == "500X500Gallery")
                                {
                                    Uri uri = new Uri(imageApi.href);
                                    string filename = System.IO.Path.GetFileName(uri.LocalPath);
                                    if (tmpImageList.ContainsKey(filename))
                                    {
                                        ProductDataProductImage image = tmpImageList[filename];
                                        image.Thumb = imageApi.href;
                                        tmpImageList[filename] = image;
                                    }
                                    else
                                    {
                                        ProductDataProductImage image = new ProductDataProductImage();
                                        image.Thumb = imageApi.href;
                                        tmpImageList.Add(filename, image);
                                    }
                                }
                                if (imageApi.rel == "LargeGallery")
                                {
                                    Uri uri = new Uri(imageApi.href);
                                    string filename = System.IO.Path.GetFileName(uri.LocalPath);
                                    if (tmpImageList.ContainsKey(filename))
                                    {
                                        ProductDataProductImage image = tmpImageList[filename];
                                        image.Large = imageApi.href;
                                        tmpImageList[filename] = image;
                                    }
                                    else
                                    {
                                        ProductDataProductImage image = new ProductDataProductImage();
                                        image.Large = imageApi.href;
                                        tmpImageList.Add(filename, image);
                                    }
                                }
                                if (imageApi.rel == "LargePrimary")
                                {
                                    result.large = imageApi.href;
                                }
                                if (imageApi.rel == "500X500Primary")
                                {
                                    result.thumb = imageApi.href;
                                }
                            }
                            foreach (var item in tmpImageList)
                            {
                                ProductDataProductImage image = item.Value;
                                if (image.Large == "" || image.Large == null)
                                {
                                    image.Large = image.Thumb;
                                }
                                if (image.Thumb == "" || image.Thumb == null)
                                {
                                    image.Thumb = image.Large;
                                }
                                result.Images.Add(image);
                            }
                        }
                        if (assetApi.characterization == "Assets")
                        {
                            foreach (var downloadApi in assetApi.links.ToList())
                            {
                                ProductDataProductDownload download = new ProductDataProductDownload();
                                download.AST_Role_Txt = downloadApi.rel;
                                download.AST_URL_Txt = downloadApi.href;
                                result.Downloads.Add(download);
                            }
                        }
                    }
                }


                var classificationSortListFromApi = new CJsonRoot();
                if (classNameDataForProduct != null && classNameDataForProduct.value != "")
                {
                    string classificationsURL = "https://www.cmicdataservices.com/datacenterrj/api/classificationsclasses/" + classNameDataForProduct.id + "?includeolddbid=yes";
                    CJsonRoot classificationListFromApi = callCMICApi(classificationsURL);
                    if (classificationListFromApi != null && classificationListFromApi.collection != null && classificationListFromApi.collection.links != null)
                    {
                        CJsonLink classificationSortFromApi = classificationListFromApi.collection.links.Find(Link => Link.rel == "Sort Fields (filterable attributes) for this Class");
                        if (classificationSortFromApi != null && classificationSortFromApi.href != "")
                        {
                            classificationSortListFromApi = callCMICApi(classificationSortFromApi.href);
                        }
                    }
                }

                result.Filters = new List<ProductDataProductFilter>();
                var filtersLinkData = product.collection.items[0].links.Find(link => link.rel == "Sort Fields");
                CJsonRoot filterListFromApi = callCMICApi(filtersLinkData.href);
                if (filterListFromApi != null && filterListFromApi.collection.items != null)
                {
                    foreach (var filterApi in filterListFromApi.collection.items ?? Enumerable.Empty<CJsonItem>())
                    {
                        ProductDataProductFilter filter = new ProductDataProductFilter();

                        filter.SortField = filterApi.data.Find(d => d.name == "sort_field_name").value;
                        filter.SortValue = filterApi.data.Find(d => d.name == "sort_field_value").value;

                        if (classificationSortListFromApi != null && classificationSortListFromApi.collection != null)
                        {
                            var sortItem = classificationSortListFromApi.collection.items.Find(item => item.data.Find(d => d.value == filter.SortField) != null);
                            filter.Units = sortItem.data.Find(d => d.name == "sort_field_units").value;
                            var sortLink = sortItem.links.Find(link => link.rel == "Sort Field Values for this Sort Field");
                            if (sortLink != null)
                            {
                                CJsonRoot sortFieldValues = callCMICApi(sortLink.href);
                                if (sortFieldValues != null && sortFieldValues.collection != null && sortFieldValues.collection.items != null)
                                {
                                    var sortFieldValueItem = sortFieldValues.collection.items.Find(item => item.data.Find(data => data.name == "sort_field_value").value == filter.SortValue);
                                    if (sortFieldValueItem != null)
                                    {
                                        filter.ValueDisplayOrder = Int32.Parse(sortFieldValueItem.data.Find(data => data.name == "sort_field_value_display_order").value);
                                    }
                                }
                            }
                        }

                        result.Filters.Add(filter);
                    }
                }


                result.RelatedItems = new List<ProductDataProductRelatedItem>();
                var relatedItemsLinkData = product.collection.items[0].links.Find(link => link.rel == "Related SKUs");
                CJsonRoot relatedItemListFromApi = callCMICApi(relatedItemsLinkData.href);
                if (relatedItemListFromApi != null && relatedItemListFromApi.collection.items != null)
                {
                    foreach (var relatedItemApi in relatedItemListFromApi.collection.items ?? Enumerable.Empty<CJsonItem>())
                    {
                        ProductDataProductRelatedItem relatedItem = new ProductDataProductRelatedItem();
                        relatedItem.Related = relatedItemApi.data.Find(d => d.name == "linked_to_sku").value;
                        result.RelatedItems.Add(relatedItem);
                    }
                }


                result.MSRP = 0;
                result.Sale = 0;
                //p.pmaps = new List<ProductDataProductpmap>();
                var pmapsLinkData = product.collection.items[0].links.Find(link => link.rel == "MAPs and Target Prices");
                CJsonRoot pmapListFromApi = callCMICApi(pmapsLinkData.href);
                if (pmapListFromApi != null && pmapListFromApi.collection.items != null)
                {
                    foreach (var pmapApi in pmapListFromApi.collection.items ?? Enumerable.Empty<CJsonItem>())
                    {
                        /* pmaps comment
                        var currentMapApi = pmapApi.data.Find(d => d.value == "Current MAP");
                        if (currentMapApi != null)
                        {
                            ProductDataProductpmap pmap = new ProductDataProductpmap();
                            string price = pmapApi.data.Find(d => d.name == "value").value;
                            pmap.Price = Decimal.Parse(Regex.Replace(price, @"[^\d.]", ""));
                            //missed column
                            pmap.Discount = 0;
                            string start_date = pmapApi.data.Find(d => d.name == "start_date").value;
                            if (!String.IsNullOrEmpty(start_date))
                                pmap.Startdate = DateTime.Parse(start_date);
                            string end_date = pmapApi.data.Find(d => d.name == "end_date").value;
                            if (!String.IsNullOrEmpty(end_date))
                                pmap.Enddate = DateTime.Parse(end_date);
                            p.pmaps.Add(pmap);
                        }
                        */

                        var maximumTargetApi = pmapApi.data.Find(d => d.value == "Maximum Target Price");
                        if (maximumTargetApi != null)
                        {
                            string price = pmapApi.data.Find(d => d.name == "value").value;
                            result.MSRP = float.Parse(Regex.Replace(price, @"[^\d.]", ""));
                        }

                        var minimumTargetApi = pmapApi.data.Find(d => d.value == "Minimum Target Price");
                        if (minimumTargetApi != null)
                        {
                            string price = pmapApi.data.Find(d => d.name == "value").value;
                            result.Sale = float.Parse(Regex.Replace(price, @"[^\d.]", ""));
                        }
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                Debug.WriteLine("EEEEEEE GetProductDetail Error: " + e.Message);
                return null;
            }
        }

        private void main()
        {
            Debug.WriteLine("*** Step2: Read Brand and Category mapping data from SiteOnTimeBrand and SoTCategory_NopCategory tables ************");
            var brandListFromDb = _siteOnTimeBrandService.GetSiteOnTimeBrands().ToList();

            Debug.WriteLine("*** Step3: Call a top level CMIC brand API to get all brand list provided by CMIC **********************************");
            //string brandListApi = "https://www.cmicdataservices.com/datacenterrj/api/brands?includeolddbid=yes";
            string brandListApi = _settings.CmicApiBrandUrl;
            CJsonRoot brandListFromApi = callCMICApi(brandListApi);
            if (brandListFromApi == null || brandListFromApi.collection == null || brandListFromApi.collection.items == null)
            {
                throw new NopException("No brands returned from Site on Time API.");
            }
            int countBrands = brandListFromApi.collection.items.Count;


            Debug.WriteLine("*** Step4: Start Main Looping _ Count of Brands - {0} ****************************", countBrands);
            int MaxTaskNumbers = 20;
            int countNullPassedProducts = 0;

            /*Parallel.ForEach(
                brandListFromApi.collection.items,
                new ParallelOptions { MaxDegreeOfParallelism = MaxTaskNumbers },
                brandApi =>*/

            foreach (var brandApi in brandListFromApi.collection.items)
            {
                try
                {
                    var brandDb = brandListFromDb.Find(brand => brand.CommonBrandName == brandApi.data[0].value && brand.Download == true);
                    Debug.WriteLine("*****************************************************************************************");
                    Debug.WriteLine("***** Step 4.1 BThread {0} Compare BrandDb :_____ {1}", Thread.CurrentThread.ManagedThreadId, brandApi.data[0].value);

                    if (brandDb != null)
                    {
                        CJsonRoot skuListFromApi = callCMICApi(brandApi.href);
                        if (skuListFromApi != null && skuListFromApi.collection.items != null)
                        {
                            Debug.WriteLine("***** Step 4.2 BThread {0} Looping Brand :_____ {1}     SkuList :____ {2}", Thread.CurrentThread.ManagedThreadId, brandDb.CommonBrandName, skuListFromApi.collection.items.Count);
                            List<ProductDataProduct> productList = new List<ProductDataProduct>();
                            Parallel.ForEach(
                                skuListFromApi.collection.items,
                                new ParallelOptions { MaxDegreeOfParallelism = MaxTaskNumbers },
                                sku =>
                                {
                                    try
                                    {
                                        Debug.WriteLine("<<<<<<<<<< PThread {0} :____   {1}  ->  {2}  Start", Thread.CurrentThread.ManagedThreadId, brandDb.CommonBrandName, sku.data[0].value);

                                        ProductDataProduct product = getProductDetail(sku, brandDb.Haw_Only);

                                        if (product != null)
                                        {
                                            Debug.WriteLine("AAAAAAAAA PThread {0} :____   {1} ->  {2}  ==  {3}  <->  {4}", Thread.CurrentThread.ManagedThreadId, brandDb.CommonBrandName, sku.data[0].value, product.SKU, product.pkID);
                                            productList.Add(product);
                                        }
                                        else
                                        {
                                            countNullPassedProducts++;
                                            Debug.WriteLine("NNNNNNNNNN PThread {0} __  {1} -> {2}   ===   Product is Null.", Thread.CurrentThread.ManagedThreadId, brandDb.CommonBrandName, sku.data[0].value);
                                        }

                                        Debug.WriteLine(">>>>>>>>>> PThread: {0} __ {1} -> {2} End", Thread.CurrentThread.ManagedThreadId, brandDb.CommonBrandName, sku.data[0].value);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine("EEEEEEEEEE PThread {0} ___ Error in {1}  ___  ", Thread.CurrentThread.ManagedThreadId, sku.data[0].value + e.Message);
                                    }
                                }
                            );

                            //lock (lockMe)
                            //{
                            Debug.WriteLine("***** Step 4.2 BThread {0} Save Brand :_____ {1}    Total  :____ {2}  _NULL__  {3}", Thread.CurrentThread.ManagedThreadId, brandDb.CommonBrandName, productList.Count, countNullPassedProducts);
                            //Save on db per brand
                            if (productList != null)
                            {
                                //siteOnDelete from [StagingDb_mz].[dbo].ProductDataProducts] where id = 16727245TimeContext.Products.AddRange(pList.OrderBy(p => p.pkID));
                                foreach (var product in productList.OrderBy(a => a.SKU))
                                {
                                    try
                                    {
                                        var existInDb = _productDataProductService.FindProductDataProduct(product.SKU, product.Brand);

                                        if (product.ModelStatus != "D")
                                        {
                                            if (product.Sale <= 0)
                                                product.Sale = product.MSRP;

                                            int pdpId = _productDataProductService.InsertProductDataProduct(product);
                                            if (product.Dimensions != null)
                                                _productDataProductDimensionService.InsertProductDataProductDimensions(product.Dimensions, pdpId);
                                            if (product.Downloads != null)
                                                _productDataProductDownloadService.InsertProductDataProductDownloads(product.Downloads, pdpId);
                                            if (product.Features != null)
                                                _productDataProductFeatureService.InsertProductDataProductFeatures(product.Features, pdpId);
                                            if (product.Filters != null)
                                                _productDataProductFilterService.InsertProductDataProductFilters(product.Filters, pdpId);
                                            if (product.Images != null)
                                                _productDataProductImageService.InsertProductDataProductImages(product.Images, pdpId);
                                            if (product.pmaps != null)
                                                _productDataProductpmapService.InsertProductDataProductpmaps(product.pmaps, pdpId);
                                            if (product.RelatedItems != null)
                                                _productDataProductRelatedItemService.InsertProductDataProductRelatedItems(product.RelatedItems, pdpId);
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine("EEEEEEEEEE " + e.Message + "  " + product.SKU);
                                    }
                                }
                            }
                            //}
                        }
                    }
                    Debug.WriteLine("***** Step 4.4 BThread {0} End Brand :______ {1}", Thread.CurrentThread.ManagedThreadId, brandApi.data[0].value);
                    Debug.WriteLine("*****************************************************************************************");
                }
                catch (Exception e)
                {
                    Debug.WriteLine("EEEEEEEEEE BThread {0} ___ Error ___ {1}", Thread.CurrentThread.ManagedThreadId, brandApi.data[0].value + e.Message);
                }
            }


            Debug.WriteLine("*** Step5.1: convert Proper Case *********************************************************");
            var convertProperCase = $@"
                    UPDATE ProductDataProductFilters
                    SET SortField = 'Depth'
                    Where SortField COLLATE Latin1_General_CS_AS = 'depth'
  
                    UPDATE ProductDataProductFilters
                    SET SortField = 'Capacity'
                    Where SortField COLLATE Latin1_General_CS_AS = 'CAPACITY'
                ";
            _stagingDb.ExecuteNonQuery(convertProperCase);

            Debug.WriteLine("*** Step5.2: migrate ProductDataProducts to SiteOnTimeProduct table *********************************************************");
            var deleteCommand = "TRUNCATE TABLE SiteOnTimeProduct";
            _stagingDb.ExecuteNonQuery(deleteCommand);
            var command = $@"
                    DECLARE @BRANDS varchar(100)
                    DECLARE @BRDFRI varchar(100)

                    SET @BRANDS = '9   ,25  ,56  ,70  ,85  ,95  ,110 ,123 ,135 ,191 ,245 ,267 ,651 ,2452'
                    SET @BRDFRI = '9   ,25  ,56  ,85  ,95  ,110 ,123 ,135 ,191 ,245 ,267 ,651 ,2452'   /***   No Fridigdaire****/
	                    -- SET NOCOUNT ON added to prevent extra result sets from
	                    -- interfering with SELECT statements.
	                    SET NOCOUNT ON;

                    WITH pmap AS(
                        SELECT pdp.* From ProductDataProductpmaps pdp WHERE pdp.Startdate <= getdate() and pdp.Enddate > getdate() - 1
                    )
                    INSERT INTO [dbo].[SiteOnTimeProduct]
		                       ([Name]
		                       ,[ShortDescription]
		                       ,[OnAbcSite]
		                       ,[OnHawthorneSite]
		                       ,[OnAbcClearanceSite]
		                       ,[OnHawthorneClearanceSite]
		                       ,[Sku]
		                       ,[ManufacturerNumber]
		                       ,[Manufacturer]
		                       ,[DisableBuying]
		                       ,[Weight]
		                       ,[Length]
		                       ,[Width]
		                       ,[Height]
		                       ,[AllowInStorePickup]
		                       ,[IsNew]
		                       ,[NewExpectedDate]
		                       ,[LimitedStockDate]
		                       ,[BasePrice]
		                       ,[DisplayPrice]
		                       ,[CartPrice]
		                       ,[UsePairPricing]
		                       ,[PriceBucketCode]
		                       ,Upc)
	                     SELECT
			                    p.Brand + ' ' + p.SKU,
			                    p.ModelDescription,
			                    CASE
				                    WHEN p.Haw_Only = 'Yes' THEN 0
				                    ELSE 1 END,
			                    1,
			                    0,
			                    0,
			                    p.SKU,
			                    p.RootModelNumber,
			                    CASE WHEN sm_nm.NopManufacturer is null THEN p.Brand ELSE sm_nm.NopManufacturer END,
			                    0,
			                    ISNULL((SELECT TOP 1 MeasurementValue FROM fDimensions(p.id) WHERE MeasurementName = 'Weight'), 0),
			                    ISNULL((SELECT TOP 1 MeasurementValue FROM fDimensions(p.id) WHERE MeasurementName = 'Depth'), 0),
			                    ISNULL((SELECT TOP 1 MeasurementValue FROM fDimensions(p.id) WHERE MeasurementName = 'Width'), 0),
			                    ISNULL((SELECT TOP 1 MeasurementValue FROM fDimensions(p.id) WHERE MeasurementName = 'Height'), 0),
			                    0,
			                    0,
			                    NULL,
			                    NULL,
			                    CASE 
			                      WHEN p.Sale = 0 THEN p.MSRP
			                      ELSE p.Sale END,
			                    CASE
			                      WHEN pmap.ProductDataProduct_id IS NOT NULL THEN FLOOR( pmap.Price * pmap.Discount)
			                      WHEN p.Sale = 0 THEN IIF( CHARINDEX( Convert(char (4) ,p.pkBrand),  @BRANDS ) > 0, FLOOR( p.MSRP*.90 ), p.MSRP)
			                      ELSE IIF( CHARINDEX( Convert(char (4) ,p.pkBrand),  @BRDFRI ) > 0, FLOOR(p.Sale * .90) - 2 , p.Sale) END,
			                    CASE
			                      WHEN p.Sale = 0 THEN p.MSRP
			                      ELSE p.Sale END,
			                    0,
			                    0,
			                    UPC
	                     FROM 
			                    ProductDataProducts as p LEFT JOIN SoTManufacturer_NopManufacturer sm_nm on p.pkBrand = sm_nm.SiteOnTimeManufacturer_id  
				                    LEFT JOIN pmap ON p.id = pmap.ProductDataProduct_id
	                     WHERE p.SKU <> ''
                ";

            _stagingDb.ExecuteNonQuery(command);
        }

        private CJsonRoot callCMICApi(string url)
        {
            CJsonRoot result = null;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.ContentType = "application/json";
                String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(_settings.CmicApiUsername + ":" + _settings.CmicApiPassword));
                request.Headers.Add("Authorization", "Basic " + encoded);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                StreamReader readerHttp = new StreamReader(response.GetResponseStream());
                string responseJsonTxt = readerHttp.ReadToEnd();
                //JavaScriptSerializer jsSerializer = new JavaScriptSerializer();
                //result = (CJsonRoot)jsSerializer.Deserialize(responseJsonTxt, typeof(CJsonRoot));
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                options.Converters.Add(new Int32Converter());
                result = JsonSerializer.Deserialize<CJsonRoot>(responseJsonTxt, options);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(url);
            }

            return result;
        }
    }
}
