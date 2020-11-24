using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcFrontend.Services
{
    public interface IWarrantyService
    {
        void CalculateWarrantyTax(ShoppingCartItem sci, Customer customer, decimal sciSubTotalExclTax, out decimal taxRate, out decimal sciSubTotalInclTax);
        void CalculateWarrantyTax(ShoppingCartItem sci, Customer customer, decimal sciSubTotalExclTax, decimal sciUnitPriceExclTax, out decimal taxRate, out decimal sciSubTotalInclTax, out decimal sciUnitPriceInclTax, out decimal warrantyUnitPriceExclTax, out decimal warrantyUnitPriceInclTax);
        bool CartContainsWarranties(IList<ShoppingCartItem> cart);
    }
}