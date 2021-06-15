using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Shipping;
using Nop.Core.Domain.Tax;
using Nop.Plugin.Tax.AbcCountryStateZip;
using Nop.Plugin.Tax.AbcCountryStateZip.Services;
using Nop.Services.Caching;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tax;
using Nop.Core.Events;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcFrontend.Services
{
    public class CustomTaxService : TaxService, ICustomTaxService
    {
        private readonly ITaxPluginManager _taxPluginManager;
        private readonly IAddressService _addressService;

        // Used for janky solution below
        private readonly ITaxRateService _taxRateService;
        private readonly IStoreContext _storeContext;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly AbcCountyStateZipSettings _settings;
        private readonly ILogger _logger;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        private readonly ICountryService _countryService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly TaxSettings _taxSettings;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IPaymentService _paymentService;
        private readonly ITaxService _taxService;

        public CustomTaxService(
            // base dependencies
            AddressSettings addressSettings,
            CustomerSettings customerSettings,
            IAddressService addressService,
            ICountryService countryService,
            ICustomerService customerService,
            IEventPublisher eventPublisher,
            IGenericAttributeService genericAttributeService,
            IGeoLookupService geoLookupService,
            ILogger logger,
            IStateProvinceService stateProvinceService,
            IStoreContext storeContext,
            ITaxPluginManager taxPluginManager,
            IWebHelper webHelper,
            IWorkContext workContext,
            ShippingSettings shippingSettings,
            TaxSettings taxSettings,
            // start including for janky
            ITaxRateService taxRateService,
            IStaticCacheManager staticCacheManager,
            AbcCountyStateZipSettings settings,
            ILocalizationService localizationService,
            IHttpContextAccessor httpContextAccessor,
            IOrderTotalCalculationService orderTotalCalculationService,
            IPaymentService paymentService,
            ITaxService taxService
        ) : base(addressSettings, customerSettings, addressService, countryService, customerService,
            eventPublisher, genericAttributeService, geoLookupService, logger, stateProvinceService,
            storeContext, taxPluginManager, webHelper, workContext, shippingSettings, taxSettings)
        {
            _taxPluginManager = taxPluginManager;
            _addressService = addressService;

            // for janky
            _taxRateService = taxRateService;
            _storeContext = storeContext;
            _staticCacheManager = staticCacheManager;
            _settings = settings;
            _logger = logger;
            _localizationService = localizationService;
            _webHelper = webHelper;
            _countryService = countryService;
            _httpContextAccessor = httpContextAccessor;
            _orderTotalCalculationService = orderTotalCalculationService;
            _taxSettings = taxSettings;
            _genericAttributeService = genericAttributeService;
            _paymentService = paymentService;
            _taxService = taxService;
        }

        public async Task<bool> IsCustomerInTaxableStateAsync(int taxCategoryId, Customer customer)
        {
            //active tax provider
            var activeTaxProvider =
                await _taxPluginManager.LoadPrimaryPluginAsync(customer, (await _storeContext.GetCurrentStoreAsync()).Id);

            var shippingAddress = customer.ShippingAddressId.HasValue ?
                await _addressService.GetAddressByIdAsync(customer.ShippingAddressId.Value) :
                null;
            if (activeTaxProvider != null && shippingAddress != null)
            {
                int countryId = shippingAddress.CountryId ?? 0;
                int stateProvinceId = shippingAddress.StateProvinceId ?? 0;
                string zip = shippingAddress.ZipPostalCode;

                if (zip == null)
                {
                    zip = string.Empty;
                }
                zip = zip.Trim();

                // Super janky - we're going to create provider manually and process values
                var tp = new CountryStateZipTaxProvider(_taxRateService, _storeContext,
                    _staticCacheManager, _settings, _logger, _localizationService, _webHelper,
                    _countryService, _httpContextAccessor,
                    _orderTotalCalculationService, _taxSettings, _genericAttributeService,
                    _paymentService, _taxService
                );
                return await tp.GetCustomerInTaxableStateAsync(taxCategoryId, countryId, stateProvinceId, zip);
            }

            return false;
        }
    }
}
