using System.Collections.Generic;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcCore.Domain;

namespace Nop.Plugin.Misc.AbcCore.Services
{
    public interface IAbcPromoService
    {
        IList<AbcPromo> GetAllPromos();
        IList<AbcPromo> GetActivePromos();
        IList<AbcPromo> GetExpiredPromos();
        AbcPromo GetPromoById(int promoId);
        IList<AbcPromo> GetActivePromosByProductId(int productId);
        IList<AbcPromo> GetAllPromosByProductId(int productId);
        IList<Product> GetProductsByPromoId(int abcPromoId);
        IList<Product> GetPublishedProductsByPromoId(int abcPromoId);
        void UpdatePromo(AbcPromo promo);
    }
}