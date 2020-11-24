using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Services.Catalog;

namespace Nop.Plugin.Misc.AbcCore.Services
{
    public class AbcPromoService : IAbcPromoService
    {
        private readonly IRepository<AbcPromoProductMapping> _abcPromoProductMappingRepository;
        private readonly IRepository<AbcPromo> _abcPromoRepository;

        private readonly IProductService _productService;

        public AbcPromoService(
            IRepository<AbcPromoProductMapping> abcPromoProductMappingRepository,
            IRepository<AbcPromo> abcPromoRepository,
            IProductService productService
        )
        {
            _abcPromoProductMappingRepository = abcPromoProductMappingRepository;
            _abcPromoRepository = abcPromoRepository;
            _productService = productService;
        }

        public IList<AbcPromo> GetAllPromos()
        {
            return _abcPromoRepository.Table.ToList();
        }

        public IList<AbcPromo> GetActivePromos()
        {
            return GetAllPromos().Where(p => p.IsActive()).ToList();
        }

        public IList<AbcPromo> GetExpiredPromos()
        {
            return GetAllPromos().Where(p => p.IsExpired()).ToList();
        }

        public AbcPromo GetPromoById(int promoId)
        {
            return GetAllPromos().Where(p => p.Id == promoId).FirstOrDefault();
        }

        // Will include promos expired by one month.
        public IList<AbcPromo> GetAllPromosByProductId(int productId)
        {
            var productPromoIds = _abcPromoProductMappingRepository.Table
                                                            .Where(appm => appm.ProductId == productId)
                                                            .Select(appm => appm.AbcPromoId)
                                                            .ToList();
            return GetAllPromos().Where(p => productPromoIds.Contains(p.Id))
                                 .ToList();
        }

        public IList<AbcPromo> GetActivePromosByProductId(int productId)
        {
            return GetAllPromosByProductId(productId).Where(p => p.IsActive()).ToList();
        }

        public IList<Product> GetProductsByPromoId(int promoId)
        {
            var productIds = _abcPromoProductMappingRepository.Table
                                        .Where(appm => appm.AbcPromoId == promoId)
                                        .Select(appm => appm.ProductId)
                                        .ToArray();

            return _productService.GetProductsByIds(productIds);
        }

        public IList<Product> GetPublishedProductsByPromoId(int promoId)
        {
            return GetProductsByPromoId(promoId).Where(p => p.Published).ToList();
        }

        public void UpdatePromo(AbcPromo promo)
        {
            if (promo == null)
                throw new ArgumentNullException(nameof(promo));

            _abcPromoRepository.Update(promo);
        }
    }
}
