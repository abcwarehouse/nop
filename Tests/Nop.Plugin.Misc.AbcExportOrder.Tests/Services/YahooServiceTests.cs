using NUnit.Framework;
using Nop.Plugin.Misc.AbcExportOrder.Services;
using Nop.Core.Domain.Orders;
using FluentAssertions;
using Nop.Services.Common;
using Moq;
using System.Linq;
using Nop.Services.Orders;
using System.Collections.Generic;
using Nop.Services.Directory;
using Nop.Plugin.Misc.AbcExportOrder.Models;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Security;
using Nop.Services.Security;
using Nop.Services.Catalog;
using System;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcFrontend.Services;
using Nop.Plugin.Misc.AbcMattresses.Services;

namespace Nop.Plugin.Misc.AbcExportOrder.Tests
{
    public class YahooShipToTests
    {
        private IYahooService _yahooService;

        private Order _orderShippingOnly = new Order()
        {
            Id = 1000,
            ShippingAddressId = 1
        };
        private Order _orderPickupOnly = new Order()
        {
            Id = 1001,
            ShippingAddressId = 1
        };
        private Order _orderBothShippingPickup = new Order()
        {
            Id = 1002,
            ShippingAddressId = 1
        };

        private OrderItem _orderItemShipping = new OrderItem()
        {
            OrderId = 1000
        };
        private OrderItem _orderItemPickup = new OrderItem()
        {
            OrderId = 1001,
            AttributeDescription = "Pickup: "
        };

        [SetUp]
        public void Setup()
        {
            _yahooService = new YahooService(
                new Mock<IAbcMattressBaseService>().Object,
                new Mock<IAbcMattressEntryService>().Object,
                new Mock<IAbcMattressGiftService>().Object,
                new Mock<IAbcMattressModelService>().Object,
                new Mock<IAbcMattressPackageService>().Object,
                MockAddressService().Object,
                new Mock<IAttributeUtilities>().Object,
                MockCountryService().Object,
                MockCustomOrderService().Object,
                new Mock<ICustomShopService>().Object,
                new Mock<IEncryptionService>().Object,
                new Mock<IGenericAttributeService>().Object,
                MockGiftCardService().Object,
                MockPriceCalculationService().Object,
                MockProductService().Object,
                new Mock<IProductAbcDescriptionService>().Object,
                MockStateProvinceService().Object,
                new Mock<IStoreService>().Object,
                MockUrlRecordService().Object,
                new Mock<IWarrantyService>().Object,
                new Mock<ExportOrderSettings>().Object,
                new Mock<SecuritySettings>().Object
            );
        }

        [Test]
        public void Returns_Empty_YahooDetailRows_If_No_OrderItems()
        {
            var detailRows = _yahooService.GetYahooDetailRows(new Order());

            detailRows.Should().HaveCount(0);
        }

        [Test]
        public void Creates_YahooDetailRows_StandardItem()
        {
            var detailRows = _yahooService.GetYahooDetailRows(_orderShippingOnly);

            detailRows.Should().HaveCount(1);
        }

        [Test]
        public void Returns_Empty_YahooShipToRows_If_No_OrderItems()
        {
            var yahooShipToRows = _yahooService.GetYahooShipToRows(new Order());

            yahooShipToRows.Should().HaveCount(0);
            
        }

        [Test]
        public void Creates_YahooShipToRows_Shipping_Only()
        {
            var yahooShipToRows = _yahooService.GetYahooShipToRows(_orderShippingOnly);

            yahooShipToRows.Should().HaveCount(1);
            
            var row = yahooShipToRows.FirstOrDefault();
            row.Should().BeOfType<YahooShipToRowShipping>();
        }

        [Test]
        public void Creates_YahooShipToRows_Pickup_Only()
        {
            var yahooShipToRows = _yahooService.GetYahooShipToRows(_orderPickupOnly);

            yahooShipToRows.Should().HaveCount(1);
            
            var row = yahooShipToRows.FirstOrDefault();
            row.Should().BeOfType<YahooShipToRow>();
        }

        [Test]
        public void Creates_YahooShipToRows_Both()
        {
            var yahooShipToRows = _yahooService.GetYahooShipToRows(_orderBothShippingPickup);

            yahooShipToRows.Should().HaveCount(2);
        }

        [Test]
        public void Creates_YahooHeaderRows_Pickup()
        {
            var yahooHeaderRows = _yahooService.GetYahooHeaderRows(_orderPickupOnly);

            yahooHeaderRows.Should().HaveCount(1);
            var row = yahooHeaderRows.FirstOrDefault();
            row.Should().BeOfType<YahooHeaderRow>();
        }

        [Test]
        public void Creates_YahooHeaderRows_Shipping()
        {
            var yahooHeaderRows = _yahooService.GetYahooHeaderRows(_orderShippingOnly);

            yahooHeaderRows.Should().HaveCount(1);
            var row = yahooHeaderRows.FirstOrDefault();
            row.Should().BeOfType<YahooHeaderRowShipping>();
        }

        [Test]
        public void Creates_YahooHeaderRows_Both()
        {
            var yahooHeaderRows = _yahooService.GetYahooHeaderRows(_orderBothShippingPickup);

            yahooHeaderRows.Should().HaveCount(2);
        }

        private Mock<IProductService> MockProductService()
        {
            var mockService = new Mock<IProductService>();
            mockService.Setup(s => s.GetProductById(It.IsAny<int>()))
                         .Returns(new Product()
                         {
                             Name = "Test Product"
                         });
            return mockService;
        }

        private Mock<IPriceCalculationService> MockPriceCalculationService()
        {
            var mockPriceCalculationService = new Mock<IPriceCalculationService>();
            mockPriceCalculationService.Setup(s => s.RoundPrice(It.IsAny<decimal>(), null))
                         .Returns(0.0M);
            return mockPriceCalculationService;
        }

        private Mock<IGiftCardService> MockGiftCardService()
        {
            var mockGiftCardService = new Mock<IGiftCardService>();
            mockGiftCardService.Setup(s => s.GetGiftCardUsageHistory(It.IsAny<Order>()))
                         .Returns(new List<GiftCardUsageHistory>());
            return mockGiftCardService;
        }

        private Mock<ICustomOrderService> MockCustomOrderService()
        {
            var mockOrderService = new Mock<ICustomOrderService>();
            mockOrderService.Setup(s => s.GetOrderItems(0, null, null, 0))
                         .Returns(new List<OrderItem>());
            mockOrderService.Setup(s => s.GetOrderItems(_orderShippingOnly.Id, null, null, 0))
                         .Returns(new List<OrderItem>()
                         {
                             _orderItemShipping
                         });
            mockOrderService.Setup(s => s.GetOrderItems(_orderPickupOnly.Id, null, null, 0))
                         .Returns(new List<OrderItem>()
                         {
                             _orderItemPickup
                         });
            mockOrderService.Setup(s => s.GetOrderItems(_orderBothShippingPickup.Id, null, null, 0))
                         .Returns(new List<OrderItem>()
                         {
                             _orderItemShipping,
                             _orderItemPickup
                         });
            return mockOrderService;
        }

        private Mock<IAddressService> MockAddressService()
        {
            var mockAddressService = new Mock<IAddressService>();
            mockAddressService.Setup(s => s.GetAddressById(It.IsAny<int>()))
                         .Returns(new Address());
            return mockAddressService;
        }

        private Mock<ICountryService> MockCountryService()
        {
            var mockService = new Mock<ICountryService>();
            mockService.Setup(s => s.GetCountryByAddress(It.IsAny<Address>()))
                         .Returns(new Country());
            return mockService;
        }

        private Mock<IStateProvinceService> MockStateProvinceService()
        {
            var mockService = new Mock<IStateProvinceService>();
            mockService.Setup(s => s.GetStateProvinceByAddress(It.IsAny<Address>()))
                         .Returns(new StateProvince());
            return mockService;
        }

        private Mock<IUrlRecordService> MockUrlRecordService()
        {
            var mockService = new Mock<IUrlRecordService>();
            mockService.Setup(s => s.GetSeName(It.IsAny<Product>(), null, true, true))
                         .Returns("test-se-name");
            return mockService;
        }
    }
}