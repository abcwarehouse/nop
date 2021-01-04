using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Plugin.Tax.AbcCountryStateZip.Infrastructure.Cache;
using Nop.Plugin.Tax.AbcCountryStateZip.Services;
using Nop.Services.Tax;
using Nop.Services.Logging;
using Nop.Services.Plugins;
using Nop.Core.Caching;
using Nop.Services.Localization;
using Nop.Services.Caching;
using Nop.Services.Directory;
using Microsoft.AspNetCore.Http;
using Nop.Services.Orders;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Tax;
using Nop.Services.Common;
using Nop.Services.Payments;

namespace Nop.Plugin.Tax.AbcCountryStateZip
{
    /// <summary>
    /// Fixed rate tax provider
    /// </summary>
    public class CountryStateZipTaxProvider : BasePlugin, ITaxProvider
    {
        /// <summary>
        /// {0} - Zip postal code
        /// {1} - Country id
        /// {2} - City
        /// </summary>
        private const string TAXRATE_KEY = "Nop.plugins.Tax.AbcCountryStateZip.taxjar.taxratebyaddress-{0}-{1}-{2}";

        private readonly ITaxRateService _taxRateService;
        private readonly IStoreContext _storeContext;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly AbcCountyStateZipSettings _settings;
        private readonly ILogger _logger;
        private readonly ILocalizationService _localizationService;
        private readonly IWebHelper _webHelper;
        private readonly ICacheKeyService _cacheKeyService;
        private readonly ICountryService _countryService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly TaxSettings _taxSettings;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IPaymentService _paymentService;
        private readonly ITaxService _taxService;

        public CountryStateZipTaxProvider(
            ITaxRateService taxRateService,
            IStoreContext storeContext,
            IStaticCacheManager staticCacheManager,
            AbcCountyStateZipSettings settings,
            ILogger logger,
            ILocalizationService localizationService,
            IWebHelper webHelper,
            ICacheKeyService cacheKeyService,
            ICountryService countryService,
            IHttpContextAccessor httpContextAccessor,
            IOrderTotalCalculationService orderTotalCalculationService,
            TaxSettings taxSettings,
            IGenericAttributeService genericAttributeService,
            IPaymentService paymentService,
            ITaxService taxService
        )
        {
            _taxRateService = taxRateService;
            _storeContext = storeContext;
            _staticCacheManager = staticCacheManager;
            _settings = settings;
            _logger = logger;
            _localizationService = localizationService;
            _webHelper = webHelper;
            _cacheKeyService = cacheKeyService;
            _countryService = countryService;
            _httpContextAccessor = httpContextAccessor;
            _orderTotalCalculationService = orderTotalCalculationService;
            _taxSettings = taxSettings;
            _genericAttributeService = genericAttributeService;
            _paymentService = paymentService;
            _taxService = taxService;
        }

        /// <summary>
        /// Gets tax rate
        /// </summary>
        /// <param name="calculateTaxRequest">Tax calculation request</param>
        /// <returns>Tax</returns>
        public TaxRateResult GetTaxRate(TaxRateRequest calculateTaxRequest)
        {
            var result = new TaxRateResult();

            if (calculateTaxRequest.Address == null)
            {
                return result;
            }

            //first, load all tax rate records (cached) - loaded only once
            var cacheKey = _cacheKeyService.PrepareKeyForDefaultCache(ModelCacheEventConsumer.ALL_TAX_RATES_MODEL_KEY);
            var allTaxRates = _staticCacheManager.Get(cacheKey, () =>
                _taxRateService
                .GetAllTaxRates()
                .Select(x => new TaxRateForCaching
                {
                    Id = x.Id,
                    StoreId = x.StoreId,
                    TaxCategoryId = x.TaxCategoryId,
                    CountryId = x.CountryId,
                    StateProvinceId = x.StateProvinceId,
                    Zip = x.Zip,
                    Percentage = x.Percentage,
                    EnableTaxState = x.EnableTaxState,
                }
                )
                .ToList()
                );

            int storeId = _storeContext.CurrentStore.Id;
            int taxCategoryId = calculateTaxRequest.TaxCategoryId;
            int countryId = calculateTaxRequest.Address.CountryId ?? 0;
            int stateProvinceId = calculateTaxRequest.Address.StateProvinceId ?? 0;
            string zip = calculateTaxRequest.Address.ZipPostalCode;


            if (zip == null)
                zip = string.Empty;
            zip = zip.Trim();

            var existingRates = new List<TaxRateForCaching>();
            foreach (var taxRate in allTaxRates)
            {
                if (taxRate.CountryId == countryId && taxRate.TaxCategoryId == taxCategoryId)
                    existingRates.Add(taxRate);
            }

            //filter by store
            // if check box is checked then this list  will contains checked items ,here we will call taxjar api 
            var matchedByStore = new List<TaxRateForCaching>();
            //first, find by a store ID
            foreach (var taxRate in existingRates)
                if (storeId == taxRate.StoreId)
                    matchedByStore.Add(taxRate);
            //not found? use the default ones (ID == 0)
            if (!matchedByStore.Any())
                foreach (var taxRate in existingRates)
                    if (taxRate.StoreId == 0)
                        matchedByStore.Add(taxRate);


            //filter by state/province
            var matchedByStateProvince = new List<TaxRateForCaching>();
            //first, find by a state ID
            foreach (var taxRate in matchedByStore)
                if (stateProvinceId == taxRate.StateProvinceId)
                    matchedByStateProvince.Add(taxRate);
            //not found? use the default ones (ID == 0)
            if (!matchedByStateProvince.Any())
                foreach (var taxRate in matchedByStore)
                    if (taxRate.StateProvinceId == 0)
                        matchedByStateProvince.Add(taxRate);


            //filter by zip
            var matchedByZip = new List<TaxRateForCaching>();
            foreach (var taxRate in matchedByStateProvince)
                if ((String.IsNullOrEmpty(zip) && String.IsNullOrEmpty(taxRate.Zip)) ||
                    (zip.Equals(taxRate.Zip, StringComparison.InvariantCultureIgnoreCase)))
                    matchedByZip.Add(taxRate);
            if (!matchedByZip.Any())
                foreach (var taxRate in matchedByStateProvince)
                    if (String.IsNullOrWhiteSpace(taxRate.Zip))
                        matchedByZip.Add(taxRate);

            if (matchedByZip.Any())
            {
                if (matchedByZip[0].EnableTaxState)
                {
                    var response = CalculateTaxJarRate(calculateTaxRequest);
                    if (response != null && response.Success && response.TaxRate != 0)
                    {
                        return response;
                    }
                }
                result.TaxRate = matchedByZip[0].Percentage;
            }
            return result;
        }

        private TaxRateResult CalculateTaxJarRate(TaxRateRequest calculateTaxRequest)
        {
            if (string.IsNullOrWhiteSpace(_settings.TaxJarAPIToken))
            {
                _logger.Warning("Unable to get TaxJar Rate, no API token defined.");
                return null;
            }

            try
            {
                var taxJarCacheKeyString = string.Format(TAXRATE_KEY,
                    !string.IsNullOrEmpty(calculateTaxRequest.Address.ZipPostalCode) ? calculateTaxRequest.Address.ZipPostalCode : string.Empty,
                    calculateTaxRequest.Address.CountryId ?? 0,
                    !string.IsNullOrEmpty(calculateTaxRequest.Address.City) ? calculateTaxRequest.Address.City : string.Empty);
                var taxJarCacheKey = new CacheKey(taxJarCacheKeyString, "Nop.plugins.Tax.AbcCountryStateZip.taxjar.");

                // we don't use standard way _cacheManager.Get() due the need write errors to CalculateTaxResult
                if (_staticCacheManager.IsSet(taxJarCacheKey))
                {
                    return new TaxRateResult
                    {
                        TaxRate = _staticCacheManager.Get<decimal>(
                        taxJarCacheKey,
                        () => CalculateTaxJarRate(calculateTaxRequest).TaxRate)
                    };
                }


                var taxJarManager = new TaxJarManager { Api = _settings.TaxJarAPIToken };
                var taxJarResult = taxJarManager.GetTaxRate(
                _countryService.GetCountryById(calculateTaxRequest.Address.CountryId.Value)?.TwoLetterIsoCode,
                calculateTaxRequest.Address.City,
                calculateTaxRequest.Address.Address1,
                calculateTaxRequest.Address.ZipPostalCode);
                if (!taxJarResult.IsSuccess)
                    return new TaxRateResult { Errors = new List<string> { taxJarResult.ErrorMessage } };

                _staticCacheManager.Set(taxJarCacheKey, taxJarResult.Rate.TaxRate * 100);
                return new TaxRateResult { TaxRate = taxJarResult.Rate.TaxRate * 100 };
            }
            catch
            {
                return null;
            }
        }

        public bool GetCustomerInTaxableState(int taxCategoryId, int countryId, int stateProvinceId, string zip)
        {
            var cacheKey = _cacheKeyService.PrepareKeyForDefaultCache(ModelCacheEventConsumer.ALL_TAX_RATES_MODEL_KEY);
            var allTaxRates = _staticCacheManager.Get(cacheKey, () =>
                _taxRateService
                .GetAllTaxRates()
                .Select(x => new TaxRateForCaching
                {
                    Id = x.Id,
                    StoreId = x.StoreId,
                    TaxCategoryId = x.TaxCategoryId,
                    CountryId = x.CountryId,
                    StateProvinceId = x.StateProvinceId,
                    Zip = x.Zip,
                    Percentage = x.Percentage,
                    EnableTaxState = x.EnableTaxState,
                }
                )
                .ToList()
                );

            var existingRates = new List<TaxRateForCaching>();
            foreach (var taxRate in allTaxRates)
            {
                if (taxRate.CountryId == countryId && taxRate.TaxCategoryId == taxCategoryId)
                    existingRates.Add(taxRate);
            }


            //filter by state/province
            var matchedByStateProvince = new List<TaxRateForCaching>();
            //first, find by a state ID
            foreach (var taxRate in existingRates)
                if (stateProvinceId == taxRate.StateProvinceId)
                    matchedByStateProvince.Add(taxRate);
            //not found? use the default ones (ID == 0)
            if (!matchedByStateProvince.Any())
                foreach (var taxRate in existingRates)
                    if (taxRate.StateProvinceId == 0)
                        matchedByStateProvince.Add(taxRate);


            //filter by zip
            var matchedByZip = new List<TaxRateForCaching>();
            foreach (var taxRate in matchedByStateProvince)
                if ((String.IsNullOrEmpty(zip) && String.IsNullOrEmpty(taxRate.Zip)) ||
                    (zip.Equals(taxRate.Zip, StringComparison.InvariantCultureIgnoreCase)))
                    matchedByZip.Add(taxRate);
            if (!matchedByZip.Any())
                foreach (var taxRate in matchedByStateProvince)
                    if (String.IsNullOrWhiteSpace(taxRate.Zip))
                        matchedByZip.Add(taxRate);

            return matchedByZip.Any();
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                ["Plugins.Tax.AbcCountryStateZip.Fields.TaxCategoryName"] = "Tax category",
                ["Plugins.Tax.AbcCountryStateZip.Fields.Rate"] = "Rate",
                ["Plugins.Tax.AbcCountryStateZip.Fields.Store"] = "Store",
                ["Plugins.Tax.AbcCountryStateZip.Fields.Store.Hint"] = "If an asterisk is selected, then this shipping rate will apply to all stores.",
                ["Plugins.Tax.AbcCountryStateZip.Fields.Country"] = "Country",
                ["Plugins.Tax.AbcCountryStateZip.Fields.Country.Hint"] = "The country.",
                ["Plugins.Tax.AbcCountryStateZip.Fields.StateProvince"] = "State / province",
                ["Plugins.Tax.AbcCountryStateZip.Fields.StateProvince.Hint"] = "If an asterisk is selected, then this tax rate will apply to all customers from the given country, regardless of the state.",
                ["Plugins.Tax.AbcCountryStateZip.Fields.Zip"] = "Zip",
                ["Plugins.Tax.AbcCountryStateZip.Fields.Zip.Hint"] = "Zip / postal code. If zip is empty, then this tax rate will apply to all customers from the given country or state, regardless of the zip code.",
                ["Plugins.Tax.AbcCountryStateZip.Fields.TaxCategory"] = "Tax category",
                ["Plugins.Tax.AbcCountryStateZip.Fields.TaxCategory.Hint"] = "The tax category.",
                ["Plugins.Tax.AbcCountryStateZip.Fields.Percentage"] = "Percentage",
                ["Plugins.Tax.AbcCountryStateZip.Fields.Percentage.Hint"] = "The tax rate.",
                ["Plugins.Tax.AbcCountryStateZip.AddRecord"] = "Add tax rate",
                ["Plugins.Tax.AbcCountryStateZip.AddRecordTitle"] = "New tax rate",
                ["Plugins.Tax.AbcCountryStateZip.Fields.TaxJarAPIToken"] = "TaxJar API Token",
                ["Plugins.Tax.AbcCountryStateZip.Fields.TaxJarAPIToken.Hint"] = "The TaxJar API Token to use.",
                ["Plugins.Tax.AbcCountryStateZip.Fields.EnableTaxState"] = "Enable tax state",
                ["Plugins.Tax.AbcCountryStateZip.Fields.EnableTaxState.Hint"] = "Whether tax state is enabled.",
            });

            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            _localizationService.DeletePluginLocaleResources("Plugins.Tax.AbcCountryStateZip");

            base.Uninstall();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/AbcTaxCountryStateZip/Configure";
        }

        public TaxTotalResult GetTaxTotal(TaxTotalRequest taxTotalRequest)
        {
            if (!(_httpContextAccessor.HttpContext.Items.TryGetValue("nop.TaxTotal", out var result) && result is TaxTotalResult taxTotalResult))
            {
                var taxRates = new SortedDictionary<decimal, decimal>();

                //order sub total (items + checkout attributes)
                _orderTotalCalculationService
                    .GetShoppingCartSubTotal(taxTotalRequest.ShoppingCart, false, out _, out _, out _, out _, out var orderSubTotalTaxRates);
                var subTotalTaxTotal = decimal.Zero;
                foreach (var kvp in orderSubTotalTaxRates)
                {
                    var taxRate = kvp.Key;
                    var taxValue = kvp.Value;
                    subTotalTaxTotal += taxValue;

                    if (taxRate > decimal.Zero && taxValue > decimal.Zero)
                    {
                        if (!taxRates.ContainsKey(taxRate))
                            taxRates.Add(taxRate, taxValue);
                        else
                            taxRates[taxRate] = taxRates[taxRate] + taxValue;
                    }
                }

                //shipping
                var shippingTax = decimal.Zero;
                if (_taxSettings.ShippingIsTaxable)
                {
                    var shippingExclTax = _orderTotalCalculationService
                        .GetShoppingCartShippingTotal(taxTotalRequest.ShoppingCart, false, out _);
                    var shippingInclTax = _orderTotalCalculationService
                        .GetShoppingCartShippingTotal(taxTotalRequest.ShoppingCart, true, out var taxRate);
                    if (shippingExclTax.HasValue && shippingInclTax.HasValue)
                    {
                        shippingTax = shippingInclTax.Value - shippingExclTax.Value;
                        if (shippingTax < decimal.Zero)
                            shippingTax = decimal.Zero;

                        if (taxRate > decimal.Zero && shippingTax > decimal.Zero)
                        {
                            if (!taxRates.ContainsKey(taxRate))
                                taxRates.Add(taxRate, shippingTax);
                            else
                                taxRates[taxRate] = taxRates[taxRate] + shippingTax;
                        }
                    }
                }

                //add at least one tax rate (0%)
                if (!taxRates.Any())
                    taxRates.Add(decimal.Zero, decimal.Zero);

                var taxTotal = subTotalTaxTotal + shippingTax;

                if (taxTotal < decimal.Zero)
                    taxTotal = decimal.Zero;

                taxTotalResult = new TaxTotalResult { TaxTotal = taxTotal, TaxRates = taxRates };
                _httpContextAccessor.HttpContext.Items.TryAdd("nop.TaxTotal", taxTotalResult);
            }

            //payment method additional fee
            if (taxTotalRequest.UsePaymentMethodAdditionalFee && _taxSettings.PaymentMethodAdditionalFeeIsTaxable)
            {
                var paymentMethodSystemName = taxTotalRequest.Customer != null
                    ? _genericAttributeService.GetAttribute<string>(taxTotalRequest.Customer,
                        NopCustomerDefaults.SelectedPaymentMethodAttribute, taxTotalRequest.StoreId)
                    : string.Empty;
                var paymentMethodAdditionalFee = _paymentService.GetAdditionalHandlingFee(taxTotalRequest.ShoppingCart, paymentMethodSystemName);
                var paymentMethodAdditionalFeeExclTax = _taxService
                    .GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, false, taxTotalRequest.Customer, out _);
                var paymentMethodAdditionalFeeInclTax = _taxService
                    .GetPaymentMethodAdditionalFee(paymentMethodAdditionalFee, true, taxTotalRequest.Customer, out var taxRate);
                var paymentMethodAdditionalFeeTax = paymentMethodAdditionalFeeInclTax - paymentMethodAdditionalFeeExclTax;

                if (paymentMethodAdditionalFeeTax < decimal.Zero)
                    paymentMethodAdditionalFeeTax = decimal.Zero;

                taxTotalResult.TaxTotal += paymentMethodAdditionalFeeTax;

                if (taxRate > decimal.Zero && paymentMethodAdditionalFeeTax > decimal.Zero)
                {
                    if (!taxTotalResult.TaxRates.ContainsKey(taxRate))
                        taxTotalResult.TaxRates.Add(taxRate, paymentMethodAdditionalFeeTax);
                    else
                        taxTotalResult.TaxRates[taxRate] = taxTotalResult.TaxRates[taxRate] + paymentMethodAdditionalFeeTax;
                }
            }

            return taxTotalResult;
        }
    }
}
