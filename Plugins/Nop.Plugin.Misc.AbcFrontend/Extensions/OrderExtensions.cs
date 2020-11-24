using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Core.Infrastructure;

namespace Nop.Plugin.Misc.AbcFrontend.Extensions
{
    public static class OrderExtensions
    {
        public static readonly string CardRefNoKey = "CardRefNo";

        public static string GetCardRefNo(this Order order)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            return genericAttributeService.GetAttribute<string>(order, "CardRefNoKey");
        }

        public static void SetCardRefNo(this Order order, string value)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            genericAttributeService.SaveAttribute(order, CardRefNoKey, value);
        }
    }
}
