using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Localization;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Stores;
using Nop.Data;
using Nop.Services.Caching;
using Nop.Services.Catalog;
using Nop.Services.Customers;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Media;
using Nop.Services.Security;
using Nop.Services.Shipping.Date;
using Nop.Services.Stores;

namespace Nop.Plugin.Misc.AbcCore.Services.Custom
{
    public class CustomProductService : ProductService, ICustomProductService
    {
        private readonly IPictureService _pictureService;

        public CustomProductService(
            CatalogSettings catalogSettings,
            CommonSettings commonSettings,
            IAclService aclService,
            ICacheKeyService cacheKeyService,
            ICustomerService customerService,
            INopDataProvider dataProvider,
            IDateRangeService dateRangeService,
            IEventPublisher eventPublisher,
            ILanguageService languageService,
            ILocalizationService localizationService,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IRepository<AclRecord> aclRepository,
            IRepository<CrossSellProduct> crossSellProductRepository,
            IRepository<DiscountProductMapping> discountProductMappingRepository,
            IRepository<Product> productRepository,
            IRepository<ProductAttributeCombination> productAttributeCombinationRepository,
            IRepository<ProductAttributeMapping> productAttributeMappingRepository,
            IRepository<ProductCategory> productCategoryRepository,
            IRepository<ProductPicture> productPictureRepository,
            IRepository<ProductReview> productReviewRepository,
            IRepository<ProductReviewHelpfulness> productReviewHelpfulnessRepository,
            IRepository<ProductWarehouseInventory> productWarehouseInventoryRepository,
            IRepository<RelatedProduct> relatedProductRepository,
            IRepository<Shipment> shipmentRepository,
            IRepository<StockQuantityHistory> stockQuantityHistoryRepository,
            IRepository<StoreMapping> storeMappingRepository,
            IRepository<TierPrice> tierPriceRepository,
            IRepository<Warehouse> warehouseRepositor,
            IStaticCacheManager staticCacheManager,
            IStoreService storeService,
            IStoreMappingService storeMappingService,
            IWorkContext workContext,
            LocalizationSettings localizationSettings,
            IPictureService pictureService
        ) : base(catalogSettings, commonSettings, aclService, cacheKeyService,
                customerService, dataProvider, dateRangeService, eventPublisher,
                languageService, localizationService, productAttributeParser,
                productAttributeService, aclRepository, crossSellProductRepository,
                discountProductMappingRepository, productRepository, productAttributeCombinationRepository,
                productAttributeMappingRepository, productCategoryRepository,
                productPictureRepository, productReviewRepository, productReviewHelpfulnessRepository,
                productWarehouseInventoryRepository, relatedProductRepository,
                shipmentRepository, stockQuantityHistoryRepository, storeMappingRepository,
                tierPriceRepository, warehouseRepositor, staticCacheManager,
                storeService, storeMappingService, workContext, localizationSettings)
        {
            _pictureService = pictureService;
        }

        public IList<Product> GetProductsWithoutImages()
        {
            var publishedProducts =
                _productRepository.Table.Where(
                    p => !p.Deleted &&
                          p.Published).ToList();
            var publishedProductsWithNoPictures = new List<Product>();

            foreach (var product in publishedProducts)
            {
                if (!_pictureService.GetPicturesByProductId(product.Id, 1).Any())
                {
                    publishedProductsWithNoPictures.Add(product);
                }
            }

            return publishedProductsWithNoPictures;
        }
    }
}