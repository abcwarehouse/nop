using System;
using System.Linq;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Data;
using Nop.Plugin.Tax.AbcCountryStateZip.Domain;
using Nop.Plugin.Tax.AbcCountryStateZip.Infrastructure.Cache;
using Nop.Services.Caching;
using Nop.Services.Events;
using System.Threading.Tasks;

namespace Nop.Plugin.Tax.AbcCountryStateZip.Services
{
    /// <summary>
    /// Tax rate service
    /// </summary>
    public partial class TaxRateService : ITaxRateService
    {
        #region Constants
        private const string TAXRATE_ALL_KEY = "Nop.taxrate.all-{0}-{1}";
        private const string TAXRATE_PATTERN_KEY = "Nop.taxrate.";
        #endregion

        #region Fields

        private readonly IRepository<TaxRate> _taxRateRepository;
        private readonly IStaticCacheManager _staticCacheManager;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="eventPublisher">Event publisher</param>
        /// <param name="staticCacheManager">Cache manager</param>
        /// <param name="taxRateRepository">Tax rate repository</param>
        public TaxRateService(
            IStaticCacheManager staticCacheManager,
            IRepository<TaxRate> taxRateRepository
        )
        {
            _eventPublisher = eventPublisher;
            _staticCacheManager = staticCacheManager;
            _taxRateRepository = taxRateRepository;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Deletes a tax rate
        /// </summary>
        /// <param name="taxRate">Tax rate</param>
        public virtual async Task DeleteTaxRateAsync(TaxRate taxRate)
        {
            if (taxRate == null)
                throw new ArgumentNullException("taxRate");

            _taxRateRepository.Delete(taxRate);

            _staticCacheManager.RemoveByPrefix(TAXRATE_PATTERN_KEY);

            //event notification
            _eventPublisher.EntityDeleted(taxRate);
        }

        /// <summary>
        /// Gets all tax rates
        /// </summary>
        /// <returns>Tax rates</returns>
        public virtual async Task<IPagedList<TaxRate>> GetAllTaxRatesAsync(int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var key = _cacheKeyService.PrepareKeyForShortTermCache(ModelCacheEventConsumer.TAXRATE_ALL_KEY);
            var rez = _staticCacheManager.Get(key, () =>
            {
                var query = from tr in _taxRateRepository.Table
                            orderby tr.StoreId, tr.CountryId, tr.StateProvinceId, tr.Zip, tr.TaxCategoryId
                            select tr;

                return query.ToList();
            });

            var records = new PagedList<TaxRate>(rez, pageIndex, pageSize);

            return records;
        }

        /// <summary>
        /// Gets a tax rate
        /// </summary>
        /// <param name="taxRateId">Tax rate identifier</param>
        /// <returns>Tax rate</returns>
        public virtual async Task<TaxRate> GetTaxRateByIdAsync(int taxRateId)
        {
            return await _taxRateRepository.GetByIdAsync(taxRateId);
        }

        /// <summary>
        /// Inserts a tax rate
        /// </summary>
        /// <param name="taxRate">Tax rate</param>
        public virtual async Task InsertTaxRateAsync(TaxRate taxRate)
        {
            await _taxRateRepository.InsertAsync(taxRate);
        }

        /// <summary>
        /// Updates the tax rate
        /// </summary>
        /// <param name="taxRate">Tax rate</param>
        public virtual async Task UpdateTaxRateAsync(TaxRate taxRate)
        {
            await _taxRateRepository.UpdateAsync(taxRate);
        }
        #endregion
    }
}
