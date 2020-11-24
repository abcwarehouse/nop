using System.Collections.Generic;
using Nop.Plugin.Misc.AbcCore.Models;

namespace Nop.Plugin.Misc.AbcCore.Services
{
    public interface IBackendStockService
    {
        Dictionary<int, int> GetStock(int productId);
        StockResponse GetApiStock(int productId);
        bool AvailableAtStore(int shopId, int productId);
    }
}