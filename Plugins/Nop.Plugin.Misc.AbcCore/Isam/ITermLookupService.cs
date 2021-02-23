using System.Collections.Generic;
using Nop.Core.Domain.Orders;

namespace Nop.Plugin.Misc.AbcCore.Services
{
    public interface ITermLookupService
    {
        (string termNo, string description, string link) GetTerm(
            IList<ShoppingCartItem> cart
        );
    }
}