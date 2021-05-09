using Nop.Core.Caching;
using Nop.Data;
using Nop.Plugin.Widgets.AbcSynchronyPayments.Domain;

namespace Nop.Plugin.Widgets.AbcSynchronyPayments.Services
{
    public class ProductAbcFinanceService : IProductAbcFinanceService
    {
        private const string PRODUCT_ABC_FINANCE = "AbcWarehouse.productabcfinance.{0}";

        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IRepository<ProductAbcFinance> _productAbcFinanceRepository;

        public ProductAbcFinanceService(
            IStaticCacheManager staticCacheManager,
            IRepository<ProductAbcFinance> productAbcFinanceRepository
        )
        {
            _staticCacheManager = staticCacheManager;
            _productAbcFinanceRepository = productAbcFinanceRepository;
        }

        public ProductAbcFinance GetProductAbcFinanceByAbcItemNumber(string abcItemNumber)
        {
            if (string.IsNullOrWhiteSpace(abcItemNumber)) { return null; }
            return _staticCacheManager.Get(
                new CacheKey(string.Format(PRODUCT_ABC_FINANCE, abcItemNumber), "Abc."),
                ProductAbcFinance.GetByAbcItemNumberFunc(_productAbcFinanceRepository, abcItemNumber));
        }
    }
}
