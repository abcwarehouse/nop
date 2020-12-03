using NUnit.Framework;
using FluentAssertions;
using Nop.Plugin.Misc.AbcMattresses.Services;
using Moq;
using Nop.Services.Catalog;
using Nop.Core.Domain.Catalog;
using System.Collections.Generic;
using Nop.Web.Factories;
using Nop.Plugin.Misc.AbcMattresses.Factories;
using Nop.Core.Domain.Security;
using Nop.Core.Domain.Customers;
using Nop.Services.Caching;
using Nop.Services.Directory;
using Nop.Services.Customers;
using Nop.Services.Shipping.Date;
using Nop.Services.Helpers;
using Nop.Services.Media;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Core.Caching;
using Nop.Core;
using Nop.Services.Tax;
using Nop.Services.Seo;
using Nop.Services.Vendors;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Seo;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Vendors;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses.Tests
{
    public class CustomProductModelFactoryTests
    {
        private IProductModelFactory _customProductModelFactory;

        private Product NonMattressProduct = new Product()
        {
            Price = 20.00M,
            ProductType = ProductType.SimpleProduct
        };

        private Product MattressProduct = new Product()
        {
            Id = 5,
            Price = 20.00M,
            ProductType = ProductType.SimpleProduct
        };

        [SetUp]
        public void Setup()
        {
            var mockPriceFormatter = new Mock<IPriceFormatter>();
            mockPriceFormatter.Setup(x => x.FormatPrice(It.IsAny<decimal>()))
                .Returns("$20.00");

            var mockPermissionService = new Mock<IPermissionService>();
            mockPermissionService.Setup(x => x.Authorize(It.IsAny<PermissionRecord>()))
                .Returns(true);

            var mockPriceCalculationService = new Mock<IPriceCalculationService>();
            mockPriceCalculationService.Setup(x => x.GetFinalPrice(
                It.IsAny<Product>(),
                It.IsAny<Customer>(),
                It.IsAny<decimal>(),
                It.IsAny<bool>(),
                It.IsAny<int>()
            ))
            .Returns(20.00M);

            var mockAbcMattressListingPriceService = new Mock<IAbcMattressListingPriceService>();
            mockAbcMattressListingPriceService
                .Setup(x => x.GetListingPriceForMattressProduct(MattressProduct.Id, null))
                .Returns(40.00M);

            _customProductModelFactory = new CustomProductModelFactory(
                new Mock<CaptchaSettings>().Object,
                new Mock<CatalogSettings>().Object,
                new Mock<CustomerSettings>().Object,
                new Mock<ICacheKeyService>().Object,
                new Mock<ICategoryService>().Object,
                new Mock<ICurrencyService>().Object,
                new Mock<ICustomerService>().Object,
                new Mock<IDateRangeService>().Object,
                new Mock<IDateTimeHelper>().Object,
                new Mock<IDownloadService>().Object,
                new Mock<IGenericAttributeService>().Object,
                new Mock<ILocalizationService>().Object,
                new Mock<IManufacturerService>().Object,
                mockPermissionService.Object,
                new Mock<IPictureService>().Object,
                mockPriceCalculationService.Object,
                mockPriceFormatter.Object,
                new Mock<IProductAttributeParser>().Object,
                new Mock<IProductAttributeService>().Object,
                new Mock<IProductService>().Object,
                new Mock<IProductTagService>().Object,
                new Mock<IProductTemplateService>().Object,
                new Mock<IReviewTypeService>().Object,
                new Mock<ISpecificationAttributeService>().Object,
                new Mock<IStaticCacheManager>().Object,
                new Mock<IStoreContext>().Object,
                new Mock<IShoppingCartModelFactory>().Object,
                new Mock<ITaxService>().Object,
                new Mock<IUrlRecordService>().Object,
                new Mock<IVendorService>().Object,
                new Mock<IWebHelper>().Object,
                new Mock<IWorkContext>().Object,
                new Mock<MediaSettings>().Object,
                new Mock<OrderSettings>().Object,
                new Mock<SeoSettings>().Object,
                new Mock<ShippingSettings>().Object,
                new Mock<VendorSettings>().Object,
                mockAbcMattressListingPriceService.Object
            );
        }

        [Test]
        public void Returns_Normal_Price_For_NonMattressProduct()
        {
            var productModel = _customProductModelFactory.PrepareProductOverviewModels
            (
                new List<Product>()
                {
                    NonMattressProduct
                }
            ).FirstOrDefault();

            productModel.ProductPrice.Price.Should().Be("$20.00");
        }

        [Test]
        public void Returns_Modified_Price_For_MattressProduct()
        {
            var productModel = _customProductModelFactory.PrepareProductOverviewModels
            (
                new List<Product>()
                {
                    MattressProduct
                }
            ).FirstOrDefault();

            // should be changed to reflect abcMattressListingPriceService results
            productModel.ProductPrice.Price.Should().Be("$40.00");
        }
    }
}