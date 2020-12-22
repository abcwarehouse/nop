using NUnit.Framework;
using Nop.Plugin.Misc.AbcExportOrder.Services;
using Nop.Core.Domain.Orders;
using FluentAssertions;
using Moq;
using Nop.Core.Domain.Common;
using Nop.Services.Catalog;
using Nop.Core.Domain.Catalog;
using Nop.Core.Caching;
using Nop.Services.Events;
using Nop.Data;
using Nop.Services.Shipping;
using Nop.Core.Domain.Customers;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcExportOrder.Tests
{
    public class CustomOrderServiceTests
    {
        private ICustomOrderService _customOrderService;

        [SetUp]
        public void Setup()
        {
            _customOrderService = new CustomOrderService(
                new Mock<CachingSettings>().Object,
                new Mock<IEventPublisher>().Object,
                new Mock<IProductService>().Object,
                new Mock<IRepository<Address>>().Object,
                new Mock<IRepository<Customer>>().Object,
                new Mock<IRepository<Order>>().Object,
                new Mock<IRepository<OrderItem>>().Object,
                new Mock<IRepository<OrderNote>>().Object,
                new Mock<IRepository<Product>>().Object,
                new Mock<IRepository<ProductWarehouseInventory>>().Object,
                new Mock<IRepository<RecurringPayment>>().Object,
                new Mock<IRepository<RecurringPaymentHistory>>().Object,
                new Mock<IShipmentService>().Object,
                MockProductAttributeParser().Object,
                MockProductAttributeService().Object
            );
        }

        private Mock<IProductAttributeParser> MockProductAttributeParser()
        {
            var mock = new Mock<IProductAttributeParser>();
            mock.Setup(p => p.ParseProductAttributeValues(It.IsAny<string>(), 0))
                .Returns(new List<ProductAttributeValue>()
                {
                    new ProductAttributeValue() { Name = "Warranty" }
                }
            );

            return mock;
        }

        private Mock<IProductAttributeService> MockProductAttributeService()
        {
            var mock = new Mock<IProductAttributeService>();
            mock.Setup(s => s.GetProductAttributeById(It.IsAny<int>()))
                .Returns(new ProductAttribute() { Name = "Warranty" });
            mock.Setup(s => s.GetProductAttributeMappingById(It.IsAny<int>()))
                .Returns(new ProductAttributeMapping());

            return mock;
        }

        [Test]
        public void Returns_Null_For_OrderItem_No_Warranty()
        {
            var warranty = _customOrderService.GetOrderItemWarranty(new OrderItem());

            warranty.Should().BeNull();
        }

        [Test]
        public void Returns_OrderItem_Warranty()
        {
            var warranty = _customOrderService.GetOrderItemWarranty(new OrderItem()
            {
                AttributeDescription = "Warranty: "
            });

            warranty.Should().NotBeNull();
        }
    }
}