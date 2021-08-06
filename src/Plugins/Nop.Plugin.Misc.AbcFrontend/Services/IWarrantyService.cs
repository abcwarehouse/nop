using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.AbcCore.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcFrontend.Services
{
    public interface IWarrantyService
    {
        Task<(decimal taxRate, decimal sciSubTotalInclTax)> CalculateWarrantyTaxAsync(ShoppingCartItem sci, Customer customer, decimal sciSubTotalExclTax);
        Task<(decimal taxRate, decimal sciSubTotalInclTax, decimal sciUnitPriceInclTax, decimal warrantyUnitPriceExclTax, decimal warrantyUnitPriceInclTax)> CalculateWarrantyTaxAsync(ShoppingCartItem sci, Customer customer, decimal sciSubTotalExclTax, decimal sciUnitPriceExclTax);
        Task<bool> CartContainsWarrantiesAsync(IList<ShoppingCartItem> cart);
        string GetWarrantySkuByName(string name);
    }
}