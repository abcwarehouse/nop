using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
using Nop.Data;
using Nop.Plugin.Tax.AbcTax.Domain;
using Nop.Plugin.Tax.AbcTax.Infrastructure.Cache;

namespace Nop.Plugin.Tax.AbcTax.Services
{
    public partial class AbcTaxService : IAbcTaxService
    {
        private readonly IRepository<AbcTaxRate> _abcTaxRateRepository;

        public AbcTaxService(IRepository<AbcTaxRate> abcTaxRateRepository)
        {
            _abcTaxRateRepository = abcTaxRateRepository;
        }

        public virtual async Task DeleteTaxRateAsync(AbcTaxRate taxRate)
        {
            await _abcTaxRateRepository.DeleteAsync(taxRate);
        }

        public virtual async Task<IPagedList<AbcTaxRate>> GetAllTaxRatesAsync(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var rez = await _abcTaxRateRepository.GetAllAsync(query =>
            {
                return from tr in query
                    orderby tr.StoreId, tr.CountryId, tr.StateProvinceId, tr.Zip, tr.TaxCategoryId
                    select tr;
            }, cache => cache.PrepareKeyForShortTermCache(ModelCacheEventConsumer.TAXRATE_ALL_KEY));

            var records = new PagedList<AbcTaxRate>(rez, pageIndex, pageSize);

            return records;
        }

        public virtual async Task<AbcTaxRate> GetTaxRateByIdAsync(int taxRateId)
        {
            return await _abcTaxRateRepository.GetByIdAsync(taxRateId);
        }
        public virtual async Task InsertTaxRateAsync(AbcTaxRate taxRate)
        {
            await _abcTaxRateRepository.InsertAsync(taxRate);
        }
        public virtual async Task UpdateTaxRateAsync(AbcTaxRate taxRate)
        {
            await _abcTaxRateRepository.UpdateAsync(taxRate);
        }
    }
}