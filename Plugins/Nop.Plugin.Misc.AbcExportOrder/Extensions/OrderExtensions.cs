using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.AbcExportOrder.Services;
using Nop.Services.Common;
using Nop.Services.Orders;
using System;

namespace Nop.Plugin.Misc.AbcExportOrder.Extensions
{
    public static class OrderExtensions
    {
        public static readonly string CardRefNoKey = "CardRefNo";

        public static bool HasCCInfo(this Order order)
        {
            return order.CardNumber == null &&
                   order.CardCvv2 == null &&
                   order.CardName == null;
        }

        public static void SubmitToISAM(this Order order)
        {
            var isamOrderService = EngineContext.Current.Resolve<IIsamOrderService>();
            isamOrderService.InsertOrder(order);

            order.CardNumber = null;
            order.CardCvv2 = null;
            order.CardName = null;

            var orderService = EngineContext.Current.Resolve<IOrderService>();
            var orderNote = new OrderNote
            {
                OrderId = order.Id,
                DisplayToCustomer = false,
                Note = "Submitted to ISAM",
                CreatedOnUtc = DateTime.UtcNow,
            };
            orderService.InsertOrderNote(orderNote);
            orderService.UpdateOrder(order);
        }
    }
}
