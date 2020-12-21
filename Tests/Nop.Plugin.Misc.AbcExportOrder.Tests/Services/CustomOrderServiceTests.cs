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
                new Mock<IProductAttributeService>().Object
            );
        }

        [Test]
        public void Returns_Null_For_OrderItem_No_Warranty()
        {
            var warranty = _customOrderService.GetOrderItemWarranty(new OrderItem());

            warranty.Should().BeNull();
        }
    }
}