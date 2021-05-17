using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcSync.Services.Staging;
using Nop.Plugin.Misc.AbcSync.Staging;
using Nop.Services.Stores;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcSync
{
    public class ImportProductFlags : BaseAbcWarehouseService, IImportProductFlags
    {
        private readonly string PRICE_BUCKET_CODE_FOLDER_PATH = "/Plugins/Misc.AbcFrontend/Images/";

        private readonly IImportUtilities _importUtilities;
        private readonly IRepository<ProductFlag> _productFlagRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IStoreService _storeService;
        private readonly INopDataProvider _nopDbContext;
        private readonly ImportSettings _settings;
        private readonly IIsamProductService _isamProductService;

        public ImportProductFlags(
            IImportUtilities importUtilities,
            IRepository<ProductFlag> productFlagRepository,
            IRepository<Product> productRepository,
            INopDataProvider nopDbContext,
            IStoreService storeService,
            ImportSettings importSettings,
            IIsamProductService isamProductService
        )
        {
            _importUtilities = importUtilities;
            _productFlagRepository = productFlagRepository;
            _productRepository = productRepository;
            _nopDbContext = nopDbContext;
            _settings = importSettings;
            _storeService = storeService;
            _isamProductService = isamProductService;

            return;
        }

        /// <summary>
        ///		Begin the import process for the product flags.
        /// </summary>
        public void Import()
        {
            await _nopDbContext.ExecuteNonQueryAsync($"DELETE FROM ProductFlag;");

            var productFlagManager = new EntityManager<ProductFlag>(_productFlagRepository);

            // insert all price bucket pictures & get their id's
            // produce a dictionary of pricebucketcode -> picture
            Dictionary<PriceBucketCode, string> priceBucketToImageUrl = InitializePriceBucketToImageUrlDictionary();
            Dictionary<InstockFlag, string> stockFlagMessage = InitializeStockFlagMessageDictionary();

            // produce a dictionary of stockflag -> text

            var productList = _isamProductService.GetIsamProducts()
                .Where(sp => sp.InstockFlag > 0 || sp.PriceBucketCode > 0 || sp.IsNew).Select(sp => sp);
            foreach (var product in productList)
            {
                var nopProduct = _importUtilities.GetExistingProductBySku(product.Sku);
                if (nopProduct == null)
                {
                    continue;
                }

                PriceBucketCode priceCode = (PriceBucketCode)product.PriceBucketCode;
                InstockFlag stockFlag = (InstockFlag)product.InstockFlag;
                string stockMessage = stockFlagMessage[stockFlag];
                if (stockFlag == InstockFlag.LowQuantity)
                {
                    if (product.LimitedStockDate.HasValue)
                    {
                        stockMessage += "More coming " + product.LimitedStockDate.Value.ToString("MMMM yyyy");
                    }
                    else
                    {
                        stockMessage += "More coming soon!";
                    }
                }

                string newModelMessage = "";
                if (product.IsNew)
                {
                    if (product.NewExpectedDate.HasValue)
                    {
                        newModelMessage = "This new model is coming " + product.NewExpectedDate.Value.ToString("MMMM yyyy");
                    }
                    else
                    {
                        newModelMessage = "This new model is coming soon!";
                    }
                }

                var newProductFlag = new ProductFlag
                {
                    ProductId = nopProduct.Id,
                    PriceBucketImageUrl = priceBucketToImageUrl[priceCode],
                    StockMessage = stockMessage,
                    NewModelMessage = newModelMessage
                };

                // make sure to include gift card
                nopProduct.IsFreeShipping = priceCode == PriceBucketCode.OnlineOnlyFreeShipping;
                string giftCardGtin = "077777965061";
                if (nopProduct.Gtin != giftCardGtin && nopProduct.IsFreeShipping)
                {
                    _productRepository.Update(nopProduct);
                }
                productFlagManager.Insert(newProductFlag);
            }
            productFlagManager.Flush();
        }

        private Dictionary<PriceBucketCode, string> InitializePriceBucketToImageUrlDictionary()
        {
            // Check if there's a Hawthorne store in the Store list - reference Hawthorne images if so.
            bool isHawthorne = await _storeService.GetAllStoresAsync().Where(s => s.Name.Contains("Hawthorne")).FirstOrDefault() != null;
            string StoreDirectory = isHawthorne ? "haw/" : "abc/";

            Dictionary<PriceBucketCode, string> priceCodeToPictureId = new Dictionary<PriceBucketCode, string>
            {
                { PriceBucketCode.NoValue, String.Empty },
                { PriceBucketCode.OrLess, PRICE_BUCKET_CODE_FOLDER_PATH + StoreDirectory + "OrLess.jpg" },
                { PriceBucketCode.BestDeal, PRICE_BUCKET_CODE_FOLDER_PATH + StoreDirectory +  "BestDeal.jpg" },
                { PriceBucketCode.CallUs, PRICE_BUCKET_CODE_FOLDER_PATH + StoreDirectory +  "CallUs.jpg" },
                { PriceBucketCode.InStoreOnly, PRICE_BUCKET_CODE_FOLDER_PATH + StoreDirectory +  "InStore.jpg" },
                { PriceBucketCode.OnlineOnly, PRICE_BUCKET_CODE_FOLDER_PATH + StoreDirectory +  "Online.jpg" },
                { PriceBucketCode.OnlineOnlyFreeShipping, PRICE_BUCKET_CODE_FOLDER_PATH + StoreDirectory +  "OnlineOnlyFreeShipping.jpg" },
                { PriceBucketCode.AddToCartForCurrentPricing, PRICE_BUCKET_CODE_FOLDER_PATH + StoreDirectory +  "MapDiscount.jpg" },
                { PriceBucketCode.AddToCartForCurrentPricingRequireLogin, PRICE_BUCKET_CODE_FOLDER_PATH + StoreDirectory +  "MapDiscount.jpg" },
                { PriceBucketCode.OpenBox10Percent, "OpenBox10Percent" },
                { PriceBucketCode.OpenBox15Percent, "OpenBox15Percent" },
                { PriceBucketCode.OpenBox20Percent, "OpenBox20Percent" }
            };

            return priceCodeToPictureId;
        }

        private Dictionary<InstockFlag, string> InitializeStockFlagMessageDictionary()
        {
            Dictionary<InstockFlag, string> stockFlagMessages = new Dictionary<InstockFlag, string>();
            stockFlagMessages.Add(InstockFlag.NoValue, String.Empty);
            stockFlagMessages.Add(InstockFlag.ShipsIn2To3Weeks, "Normally ships in 2-3 weeks.");
            stockFlagMessages.Add(InstockFlag.LowQuantity, "Low Quantity, Check your local store. ");
            stockFlagMessages.Add(InstockFlag.ItemDiscontinued, "Item Discontinued");
            stockFlagMessages.Add(InstockFlag.SeeStoreForDetails, "See Store for Details");
            return stockFlagMessages;
        }
    }
}