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

        private OrderItem _orderItemShipping = new OrderItem();
        private OrderItem _orderItemPickup = new OrderItem()
        {
            AttributeDescription = "Pickup: "
        };

        [SetUp]
        public void Setup()
        {
            _yahooService = new YahooService(
                MockAddressService().Object,
                MockCountryService().Object,
                MockOrderService().Object,
                MockStateProvinceService().Object,
                new Mock<ExportOrderSettings>().Object
            );
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
            row.Should().BeOfType<YahooShipToRowPickup>();
        }

        [Test]
        public void Creates_YahooShipToRows_Both()
        {
            var yahooShipToRows = _yahooService.GetYahooShipToRows(_orderBothShippingPickup);

            yahooShipToRows.Should().HaveCount(2);
        }

        private Mock<IOrderService> MockOrderService()
        {
            var mockOrderService = new Mock<IOrderService>();
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
    }
}