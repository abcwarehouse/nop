using Nop.Core.Infrastructure;
using Nop.Plugin.Tax.AbcCountryStateZip.Domain;
using Nop.Services.Events;
using Nop.Core.Events;
using Nop.Core.Caching;

namespace Nop.Plugin.Tax.AbcCountryStateZip.Infrastructure.Cache
{
    /// <summary>
    /// Model cache event consumer (used for caching of presentation layer models)
    /// </summary>
    public partial class ModelCacheEventConsumer : 
        //tax rates
        IConsumer<EntityInsertedEvent<TaxRate>>,
        IConsumer<EntityUpdatedEvent<TaxRate>>,
        IConsumer<EntityDeletedEvent<TaxRate>>
    {
        /// <summary>
        /// Key for caching
        /// </summary>
        public const string ALL_TAX_RATES_PATTERN_KEY = "Nop.plugins.Tax.AbcCountryStateZip.";

        public static CacheKey ALL_TAX_RATES_MODEL_KEY = new CacheKey("Nop.plugins.tax.AbcCountryStateZip.all", ALL_TAX_RATES_PATTERN_KEY);
        public static CacheKey TAXRATE_ALL_KEY = new CacheKey("Nop.plugins.tax.AbcCountryStateZip.taxrate.all", ALL_TAX_RATES_PATTERN_KEY);

        private readonly IStaticCacheManager _staticCacheManager;
        
        public ModelCacheEventConsumer(
            IStaticCacheManager staticCacheManager
        )
        {
            _staticCacheManager = staticCacheManager;
        }

        //tax rates
        public void HandleEvent(EntityInsertedEvent<TaxRate> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(ALL_TAX_RATES_PATTERN_KEY);
        }
        public void HandleEvent(EntityUpdatedEvent<TaxRate> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(ALL_TAX_RATES_PATTERN_KEY);
        }
        public void HandleEvent(EntityDeletedEvent<TaxRate> eventMessage)
        {
            _staticCacheManager.RemoveByPrefix(ALL_TAX_RATES_PATTERN_KEY);
        }
    }
}
