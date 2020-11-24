using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Services.Catalog;
using Nop.Services.Events;
using Nop.Services.Orders;
using Nop.Services.Shipping;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcExportOrder.Services
{
    public class CustomOrderService : OrderService, ICustomOrderService
    {
        private readonly IRepository<Order> _orderRepository;

        public CustomOrderService(
            CachingSettings cachingSettings,
            IEventPublisher eventPublisher,
            IProductService productService,
            IRepository<Address> addressRepository,
            IRepository<Customer> customerRepository,
            IRepository<Order> orderRepository,
            IRepository<OrderItem> orderItemRepository,
            IRepository<OrderNote> orderNoteRepository,
            IRepository<Product> productRepository,
            IRepository<ProductWarehouseInventory> productWarehouseInventoryRepository,
            IRepository<RecurringPayment> recurringPaymentRepository,
            IRepository<RecurringPaymentHistory> recurringPaymentHistoryRepository,
            IShipmentService shipmentService
        ) : base(cachingSettings, eventPublisher, productService, addressRepository,
                 customerRepository, orderRepository, orderItemRepository, orderNoteRepository,
                 productRepository, productWarehouseInventoryRepository, recurringPaymentRepository,
                 recurringPaymentHistoryRepository, shipmentService)
        {
            _orderRepository = orderRepository;
        }

        public IList<Order> GetUnsubmittedOrders()
        {
            var lastMonth = DateTime.Today.AddMonths(-1);
            return _orderRepository.Table.Where(o => o.CreatedOnUtc > lastMonth && o.CardNumber != null).ToList();
        }
    }
}
