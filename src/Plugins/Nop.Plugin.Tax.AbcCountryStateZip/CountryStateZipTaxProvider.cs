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
using System.Threading.Tasks;
using Nop.Plugin.Tax.AbcCountryStateZip.Domain;

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
        public async Task<TaxRateResult> GetTaxRateAsync(TaxRateRequest taxRateRequest)
        {
            var result = new TaxRateResult();

            //the tax rate calculation by fixed rate
            if (!_countryStateZipSettings.CountryStateZipEnabled)
            {
                result.TaxRate = await _settingService.GetSettingByKeyAsync<decimal>(string.Format(FixedOrByCountryStateZipDefaults.FixedRateSettingsKey, taxRateRequest.TaxCategoryId));
                return result;
            }

            //the tax rate calculation by country & state & zip 
            if (taxRateRequest.Address == null)
            {
                result.Errors.Add("Address is not set");
                return result;
            }

            //first, load all tax rate records (cached) - loaded only once
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ModelCacheEventConsumer.ALL_TAX_RATES_MODEL_KEY);
            var allTaxRates = await _staticCacheManager.GetAsync(cacheKey, async () => (await _taxRateService.GetAllTaxRatesAsync()).Select(taxRate => new TaxRate
            {
                Id = taxRate.Id,
                StoreId = taxRate.StoreId,
                TaxCategoryId = taxRate.TaxCategoryId,
                CountryId = taxRate.CountryId,
                StateProvinceId = taxRate.StateProvinceId,
                Zip = taxRate.Zip,
                Percentage = taxRate.Percentage,
                EnableTaxState = taxRate.EnableTaxState
            }).ToList());

            var storeId = taxRateRequest.CurrentStoreId;
            var taxCategoryId = taxRateRequest.TaxCategoryId;
            var countryId = taxRateRequest.Address.CountryId;
            var stateProvinceId = taxRateRequest.Address.StateProvinceId;
            var zip = taxRateRequest.Address.ZipPostalCode?.Trim() ?? string.Empty;

            var existingRates = allTaxRates.Where(taxRate => taxRate.CountryId == countryId && taxRate.TaxCategoryId == taxCategoryId);

            //filter by store
            var matchedByStore = existingRates.Where(taxRate => storeId == taxRate.StoreId || taxRate.StoreId == 0);

            //filter by state/province
            var matchedByStateProvince = matchedByStore.Where(taxRate => stateProvinceId == taxRate.StateProvinceId || taxRate.StateProvinceId == 0);

            //filter by zip
            var matchedByZip = matchedByStateProvince.Where(taxRate => string.IsNullOrWhiteSpace(taxRate.Zip) || taxRate.Zip.Equals(zip, StringComparison.InvariantCultureIgnoreCase));

            if (matchedByZip.Any())
            {
                if (matchedByZip[0].EnableTaxState)
                {
                    var response = CalculateTaxJarRateAsync(calculateTaxRequest);
                    if (response != null && response.Success && response.TaxRate != 0)
                    {
                        return response;
                    }
                }
                result.TaxRate = matchedByZip[0].Percentage;
            }

            return result;
        }

        private async Task<TaxRateResult> CalculateTaxJarRateAsync(TaxRateRequest calculateTaxRequest)
        {
            if (string.IsNullOrWhiteSpace(_settings.TaxJarAPIToken))
            {
                await _logger.WarningAsync("Unable to get TaxJar Rate, no API token defined.");
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
                        () => CalculateTaxJarRateAsync(calculateTaxRequest).TaxRate)
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

                await _staticCacheManager.SetAsync(taxJarCacheKey, taxJarResult.Rate.TaxRate * 100);
                return new TaxRateResult { TaxRate = taxJarResult.Rate.TaxRate * 100 };
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> GetCustomerInTaxableStateAsync(int taxCategoryId, int countryId, int stateProvinceId, string zip)
        {
            //first, load all tax rate records (cached) - loaded only once
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ModelCacheEventConsumer.ALL_TAX_RATES_MODEL_KEY);
            var allTaxRates = await _staticCacheManager.GetAsync(cacheKey, async () =>
            (await _taxRateService.GetAllTaxRatesAsync()).Select(taxRate => new TaxRate
            {
                Id = taxRate.Id,
                StoreId = taxRate.StoreId,
                TaxCategoryId = taxRate.TaxCategoryId,
                CountryId = taxRate.CountryId,
                StateProvinceId = taxRate.StateProvinceId,
                ZipCode = taxRate.ZipCode,
                Percentage = taxRate.Percentage,
                EnableTaxState = taxRate.EnableTaxState
            }).ToList());

            var existingRates = allTaxRates.Where(taxRate => taxRate.CountryId == countryId && taxRate.TaxCategoryId == taxCategoryId);

            //filter by state/province
            var matchedByStateProvince = existingRates.Where(taxRate => stateProvinceId == taxRate.StateProvinceId || taxRate.StateProvinceId == 0);

            //filter by zip
            var matchedByZip = matchedByStateProvince.Where(taxRate => string.IsNullOrWhiteSpace(taxRate.ZipCode) || taxRate.ZipCode.Equals(zip, StringComparison.InvariantCultureIgnoreCase));

            return matchedByZip.Any();
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override async Task InstallAsync()
        {
            _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
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

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override async Task UninstallAsync()
        {
            await _localizationService.DeleteLocaleResourceAsync("Plugins.Tax.AbcCountryStateZip");

            await base.UninstallAsync();
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/AbcTaxCountryStateZip/Configure";
        }

        public async Task<TaxTotalResult> GetTaxTotalAsync(TaxTotalRequest taxTotalRequest)
        {
            if (_httpContextAccessor.HttpContext.Items.TryGetValue("nop.TaxTotal", out var result)
                && result is (TaxTotalResult taxTotalResult, decimal paymentTax))
            {
                //short-circuit to avoid circular reference when calculating payment method additional fee during the checkout process
                if (!taxTotalRequest.UsePaymentMethodAdditionalFee)
                    return new TaxTotalResult { TaxTotal = taxTotalResult.TaxTotal - paymentTax };

                return taxTotalResult;
            }

            var taxRates = new SortedDictionary<decimal, decimal>();
            var taxTotal = decimal.Zero;

            //order sub total (items + checkout attributes)
            var (_, _, _, _, orderSubTotalTaxRates) = await _orderTotalCalculationService
                .GetShoppingCartSubTotalAsync(taxTotalRequest.ShoppingCart, false);
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
            taxTotal += subTotalTaxTotal;

            //shipping
            var shippingTax = decimal.Zero;
            if (_taxSettings.ShippingIsTaxable)
            {
                var (shippingExclTax, _, _) = await _orderTotalCalculationService
                    .GetShoppingCartShippingTotalAsync(taxTotalRequest.ShoppingCart, false);
                var (shippingInclTax, taxRate, _) = await _orderTotalCalculationService
                    .GetShoppingCartShippingTotalAsync(taxTotalRequest.ShoppingCart, true);
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
            taxTotal += shippingTax;

            //short-circuit to avoid circular reference when calculating payment method additional fee during the checkout process
            if (!taxTotalRequest.UsePaymentMethodAdditionalFee)
                return new TaxTotalResult { TaxTotal = taxTotal };

            //payment method additional fee
            var paymentMethodAdditionalFeeTax = decimal.Zero;
            if (_taxSettings.PaymentMethodAdditionalFeeIsTaxable)
            {
                var paymentMethodSystemName = taxTotalRequest.Customer != null
                    ? await _genericAttributeService
                        .GetAttributeAsync<string>(taxTotalRequest.Customer, NopCustomerDefaults.SelectedPaymentMethodAttribute, taxTotalRequest.StoreId)
                    : string.Empty;

                var paymentMethodAdditionalFee = await _paymentService
                    .GetAdditionalHandlingFeeAsync(taxTotalRequest.ShoppingCart, paymentMethodSystemName);
                var (paymentMethodAdditionalFeeExclTax, _) = await _taxService
                    .GetPaymentMethodAdditionalFeeAsync(paymentMethodAdditionalFee, false, taxTotalRequest.Customer);
                var (paymentMethodAdditionalFeeInclTax, taxRate) = await _taxService
                    .GetPaymentMethodAdditionalFeeAsync(paymentMethodAdditionalFee, true, taxTotalRequest.Customer);

                paymentMethodAdditionalFeeTax = paymentMethodAdditionalFeeInclTax - paymentMethodAdditionalFeeExclTax;
                if (paymentMethodAdditionalFeeTax < decimal.Zero)
                    paymentMethodAdditionalFeeTax = decimal.Zero;

                if (taxRate > decimal.Zero && paymentMethodAdditionalFeeTax > decimal.Zero)
                {
                    if (!taxRates.ContainsKey(taxRate))
                        taxRates.Add(taxRate, paymentMethodAdditionalFeeTax);
                    else
                        taxRates[taxRate] = taxRates[taxRate] + paymentMethodAdditionalFeeTax;
                }
            }
            taxTotal += paymentMethodAdditionalFeeTax;

            //add at least one tax rate (0%)
            if (!taxRates.Any())
                taxRates.Add(decimal.Zero, decimal.Zero);

            if (taxTotal < decimal.Zero)
                taxTotal = decimal.Zero;

            taxTotalResult = new TaxTotalResult { TaxTotal = taxTotal, TaxRates = taxRates, };

            //store values within the scope of the request to avoid duplicate calculations
            _httpContextAccessor.HttpContext.Items.TryAdd("nop.TaxTotal", (taxTotalResult, paymentMethodAdditionalFeeTax));

            return taxTotalResult;
        }
    }
}
