using Nop.Plugin.Misc.AbcSync.Domain.Staging;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSync.Services.Staging
{
    public interface IPrFileDiscountService
    {
        IEnumerable<PrFileDiscount> GetPrFileDiscounts();
    }
}