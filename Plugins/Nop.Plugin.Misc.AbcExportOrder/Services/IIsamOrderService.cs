using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Misc.AbcExportOrder.Services
{
    public interface IIsamOrderService
    {
        void InsertOrder(Order order);
    }
}