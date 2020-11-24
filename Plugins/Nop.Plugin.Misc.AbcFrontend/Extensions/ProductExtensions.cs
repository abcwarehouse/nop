using Nop.Core.Domain.Catalog;
using Nop.Core.Infrastructure;
using Nop.Services.Common;
using System;

namespace Nop.Plugin.Misc.AbcFrontend.Extensions
{
    public static class ProductExtensions
    {
        public static bool IsAbcGiftCard(this Product product)
        {
            return product.Gtin == "077777965061";
        }

        public static decimal GetSpecialPrice(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            return genericAttributeService.GetAttribute<decimal>(product, "SpecialPrice");
        }
    }
}
