using Nop.Core.Caching;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore.Domain;
using System.Threading.Tasks;
using System.Linq;

namespace Nop.Plugin.Misc.AbcCore.Services
{
    public class ProductAbcFinanceService : IProductAbcFinanceService
    {
        private readonly IRepository<ProductAbcFinance> _productAbcFinanceRepository;

        public ProductAbcFinanceService(
            IRepository<ProductAbcFinance> productAbcFinanceRepository
        )
        {
            _productAbcFinanceRepository = productAbcFinanceRepository;
        }

        public async Task<ProductAbcFinance> GetProductAbcFinanceByAbcItemNumberAsync(string abcItemNumber)
        {
            return await _productAbcFinanceRepository.Table.Where(paf => paf.AbcItemNumber == abcItemNumber)
                                                     .FirstOrDefaultAsync();
        }
    }
}
