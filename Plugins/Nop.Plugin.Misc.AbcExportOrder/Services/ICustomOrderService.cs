using Nop.Core.Domain.Orders;
using Nop.Services.Orders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcExportOrder.Services
{
    public interface ICustomOrderService : IOrderService
    {
        IList<Order> GetUnsubmittedOrders();
    }
}
