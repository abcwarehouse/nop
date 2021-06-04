using Nop.Core.Infrastructure;
using Nop.Plugin.Tax.AbcCountryStateZip.Domain;
using Nop.Services.Events;
using Nop.Core.Events;
using Nop.Core.Caching;
using System.Threading.Tasks;
using Nop.Core.Domain.Tax;
using Nop.Plugin.Tax.AbcCountryStateZip.Services;
using Nop.Services.Configuration;
using System.Linq;

namespace Nop.Plugin.Tax.AbcCountryStateZip.Infrastructure.Cache
{
    /// <summary>
    /// Model cache event consumer (used for caching of presentation layer models)
    /// </summary>
    public partial class ModelCacheEventConsumer :
        //tax rates
        IConsumer<EntityInsertedEvent<TaxRate>>,
        IConsumer<EntityUpdatedEvent<TaxRate>>,
        IConsumer<EntityDeletedEvent<TaxRate>>,
        IConsumer<EntityDeletedEvent<TaxCategory>>
    {
        /// <summary>
        /// Key for caching
        /// </summary>
        public const string ALL_TAX_RATES_PATTERN_KEY = "Nop.plugins.Tax.AbcCountryStateZip.";

        public static CacheKey ALL_TAX_RATES_MODEL_KEY = new CacheKey("Nop.plugins.tax.AbcCountryStateZip.all", ALL_TAX_RATES_PATTERN_KEY);
        public static CacheKey TAXRATE_ALL_KEY = new CacheKey("Nop.plugins.tax.AbcCountryStateZip.taxrate.all", ALL_TAX_RATES_PATTERN_KEY);

        private readonly ITaxRateService _taxRateService;
        private readonly ISettingService _settingService;
        private readonly IStaticCacheManager _staticCacheManager;

        public ModelCacheEventConsumer(
            ITaxRateService taxRateService,
            ISettingService settingService,
            IStaticCacheManager staticCacheManager
        )
        {
            _taxRateService = taxRateService;
            _settingService = settingService;
            _staticCacheManager = staticCacheManager;
        }

        //tax rates
        public async Task HandleEventAsync(EntityInsertedEvent<TaxRate> eventMessage)
        {
            await _staticCacheManager.RemoveByPrefixAsync(ALL_TAX_RATES_PATTERN_KEY);
        }
        public async Task HandleEventAsync(EntityUpdatedEvent<TaxRate> eventMessage)
        {
            await _staticCacheManager.RemoveByPrefixAsync(ALL_TAX_RATES_PATTERN_KEY);
        }
        public async Task HandleEventAsync(EntityDeletedEvent<TaxRate> eventMessage)
        {
            await _staticCacheManager.RemoveByPrefixAsync(ALL_TAX_RATES_PATTERN_KEY);
        }

        public async Task HandleEventAsync(EntityDeletedEvent<TaxCategory> eventMessage)
        {
            var taxCategory = eventMessage?.Entity;
            if (taxCategory == null)
                return;

            //delete an appropriate record when tax category is deleted
            var recordsToDelete = (await _taxRateService.GetAllTaxRatesAsync())
                                    .Where(taxRate => taxRate.TaxCategoryId == taxCategory.Id).ToList();
            foreach (var taxRate in recordsToDelete)
            {
                await _taxRateService.DeleteTaxRateAsync(taxRate);
            }
        }
    }
}
