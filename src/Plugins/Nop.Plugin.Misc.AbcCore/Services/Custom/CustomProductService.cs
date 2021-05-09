using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            ICustomerService customerService,
            IDateRangeService dateRangeService,
            ILanguageService languageService,
            ILocalizationService localizationService,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService,
            IRepository<CrossSellProduct> crossSellProductRepository,
            IRepository<DiscountProductMapping> discountProductMappingRepository,
            IRepository<LocalizedProperty> localizedPropertyRepository,
            IRepository<Product> productRepository,
            IRepository<ProductAttributeCombination> productAttributeCombinationRepository,
            IRepository<ProductAttributeMapping> productAttributeMappingRepository,
            IRepository<ProductCategory> productCategoryRepository,
            IRepository<ProductManufacturer> productManufacturerRepository,
            IRepository<ProductPicture> productPictureRepository,
            IRepository<ProductProductTagMapping> productTagMappingRepository,
            IRepository<ProductReview> productReviewRepository,
            IRepository<ProductReviewHelpfulness> productReviewHelpfulnessRepository,
            IRepository<ProductSpecificationAttribute> productSpecificationAttributeRepository,
            IRepository<ProductTag> productTagRepository,
            IRepository<ProductWarehouseInventory> productWarehouseInventoryRepository,
            IRepository<RelatedProduct> relatedProductRepository,
            IRepository<Shipment> shipmentRepository,
            IRepository<StockQuantityHistory> stockQuantityHistoryRepository,
            IRepository<TierPrice> tierPriceRepository,
            IRepository<Warehouse> warehouseRepository,
            IStaticCacheManager staticCacheManager,
            IStoreService storeService,
            IStoreMappingService storeMappingService,
            IWorkContext workContext,
            LocalizationSettings localizationSettings,
            IPictureService pictureService
        ) : base(catalogSettings, commonSettings, aclService,
                customerService, dateRangeService,
                languageService, localizationService, productAttributeParser,
                productAttributeService, crossSellProductRepository,
                discountProductMappingRepository, localizedPropertyRepository,
                productRepository, productAttributeCombinationRepository,
                productAttributeMappingRepository, productCategoryRepository,
                productManufacturerRepository, productPictureRepository,
                productTagMappingRepository, productReviewRepository,
                productReviewHelpfulnessRepository,
                productSpecificationAttributeRepository, productTagRepository,
                productWarehouseInventoryRepository, relatedProductRepository,
                shipmentRepository, stockQuantityHistoryRepository,
                tierPriceRepository, warehouseRepository, staticCacheManager,
                storeService, storeMappingService, workContext, localizationSettings)
        {
            _pictureService = pictureService;
        }

        public async Task<IList<Product>> GetProductsWithoutImages()
        {
            var publishedProducts =
                _productRepository.Table.Where(
                    p => !p.Deleted &&
                          p.Published).ToList();
            var publishedProductsWithNoPictures = new List<Product>();

            foreach (var product in publishedProducts)
            {
                var productPicture = await _pictureService.GetPicturesByProductIdAsync(product.Id, 1);
                if (productPicture == null)
                {
                    publishedProductsWithNoPictures.Add(product);
                }
            }

            return publishedProductsWithNoPictures;
        }
    }
}