using System.Collections.Generic;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.AbcExportOrder.Models;

namespace Nop.Plugin.Misc.AbcExportOrder.Services
{
    public interface IYahooService
    {
        IList<YahooShipToRow> GetYahooShipToRows(Order order);
        IList<YahooHeaderRow> GetYahooHeaderRows(Order order);
        IList<YahooDetailRow> GetYahooDetailRows(Order order);
    }
}