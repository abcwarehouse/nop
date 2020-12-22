using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Data;
using Nop.Plugin.Misc.AbcExportOrder.Extensions;
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

        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IProductAttributeService _productAttributeService;

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
            IShipmentService shipmentService,
            IProductAttributeParser productAttributeParser,
            IProductAttributeService productAttributeService
        ) : base(cachingSettings, eventPublisher, productService, addressRepository,
                 customerRepository, orderRepository, orderItemRepository, orderNoteRepository,
                 productRepository, productWarehouseInventoryRepository, recurringPaymentRepository,
                 recurringPaymentHistoryRepository, shipmentService)
        {
            _orderRepository = orderRepository;
            _productAttributeParser = productAttributeParser;
            _productAttributeService = productAttributeService;
        }

        public ProductAttributeValue GetOrderItemWarranty(OrderItem orderItem)
        {
            if (!orderItem.HasWarranty()) { return null; }

            return _productAttributeParser.ParseProductAttributeValues(
                       orderItem.AttributesXml
                   )
                   .Where(val => IsWarranty(
                       _productAttributeService.GetProductAttributeById(
                           _productAttributeService.GetProductAttributeMappingById(
                               val.ProductAttributeMappingId
                           ).ProductAttributeId
                       )))
                   .FirstOrDefault();
        }

        public IList<Order> GetUnsubmittedOrders()
        {
            var lastMonth = DateTime.Today.AddMonths(-1);
            return _orderRepository.Table.Where(o => o.CreatedOnUtc > lastMonth && o.CardNumber != null).ToList();
        }

        private static bool IsWarranty(ProductAttribute productAttribute)
        {
            return productAttribute.Name == "Warranty";
        }
    }
}
