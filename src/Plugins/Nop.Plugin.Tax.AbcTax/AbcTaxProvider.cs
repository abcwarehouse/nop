using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Tax;
using Nop.Plugin.Tax.AbcTax.Domain;
using Nop.Plugin.Tax.AbcTax.Infrastructure.Cache;
using Nop.Plugin.Tax.AbcTax.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Services.Tax;
using Nop.Data;

namespace Nop.Plugin.Tax.AbcTax
{
    public class AbcTaxProvider : BasePlugin, ITaxProvider
    {
        private readonly AbcTaxSettings _abcTaxSettings;
        private readonly IAbcTaxService _abcTaxService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocalizationService _localizationService;
        private readonly INopDataProvider _nopDataProvider;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IPaymentService _paymentService;
        private readonly ISettingService _settingService;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly ITaxJarService _taxJarService;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly TaxSettings _taxSettings;

        public AbcTaxProvider(AbcTaxSettings abcTaxSettings,
            IAbcTaxService abcTaxService,
            IGenericAttributeService genericAttributeService,
            IHttpContextAccessor httpContextAccessor,
            ILocalizationService localizationService,
            INopDataProvider nopDataProvider,
            IOrderTotalCalculationService orderTotalCalculationService,
            IPaymentService paymentService,
            ISettingService settingService,
            IStaticCacheManager staticCacheManager,
            ITaxCategoryService taxCategoryService,
            ITaxJarservice taxJarService,
            ITaxService taxService,
            IWebHelper webHelper,
            TaxSettings taxSettings)
        {
            _abcTaxSettings = abcTaxSettings;
            _abcTaxService = abcTaxService;
            _genericAttributeService = genericAttributeService;
            _httpContextAccessor = httpContextAccessor;
            _localizationService = localizationService;
            _nopDataProvider = nopDataProvider;
            _orderTotalCalculationService = orderTotalCalculationService;
            _paymentService = paymentService;
            _settingService = settingService;
            _staticCacheManager = staticCacheManager;
            _taxCategoryService = taxCategoryService;
            _taxJarService = taxJarService;
            _taxService = taxService;
            _webHelper = webHelper;
            _taxSettings = taxSettings;
        }

        public async Task<TaxRateResult> GetTaxRateAsync(TaxRateRequest taxRateRequest)
        {
            var result = new TaxRateResult();

            //the tax rate calculation by country & state & zip 
            if (taxRateRequest.Address == null)
            {
                result.Errors.Add("Address is not set");
                return result;
            }

            //first, load all tax rate records (cached) - loaded only once
            var cacheKey = _staticCacheManager.PrepareKeyForDefaultCache(ModelCacheEventConsumer.ALL_TAX_RATES_MODEL_KEY);
            var allTaxRates = await _staticCacheManager.GetAsync(cacheKey, async () => (await _abcTaxService.GetAllTaxRatesAsync()).Select(taxRate => new AbcTaxRate
            {
                Id = taxRate.Id,
                StoreId = taxRate.StoreId,
                TaxCategoryId = taxRate.TaxCategoryId,
                CountryId = taxRate.CountryId,
                StateProvinceId = taxRate.StateProvinceId,
                Zip = taxRate.Zip,
                Percentage = taxRate.Percentage
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

            //sort from particular to general, more particular cases will be the first
            var foundRecords = matchedByZip.OrderBy(r => r.StoreId == 0).ThenBy(r => r.StateProvinceId == 0).ThenBy(r => string.IsNullOrEmpty(r.Zip));

            var foundRecord = foundRecords.FirstOrDefault();

            if (foundRecord == null) return result;

            // get TaxJar rate if appropriate
            if (foundRecord.IsTaxJarEnabled)
            {
                var taxJarRateResponse = await _taxJarService.GetTaxJarRateAsync(
                    new TaxJarRequest()
                    {
                        Country = (await _countryService.GetCountryByIdAsync(calculateTaxRequest.Address.CountryId.Value))?.TwoLetterIsoCode,
                        City = taxRateRequest.Address.City,
                        State = taxRateRequest.Address.Address1,
                        Zip = zip
                    }
                );
            }
            
            result.TaxRate = foundRecord.Percentage;
            return result;
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

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/AbcTax/Configure";
        }

        public override async Task InstallAsync()
        {
            //settings
            await _settingService.SaveSettingAsync(new AbcTaxSettings());

            //locales
            await _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Tax.AbcTax.Tax.Categories.Manage"] = "Manage tax categories",
                ["Plugins.Tax.AbcTax.TaxCategoriesCanNotLoaded"] = "No tax categories can be loaded. You may manage tax categories by <a href='{0}'>this link</a>",
                ["Plugins.Tax.AbcTax.TaxByCountryStateZip"] = "By Country",
                ["Plugins.Tax.AbcTax.Fields.TaxCategoryName"] = "Tax category",
                ["Plugins.Tax.AbcTax.Fields.Rate"] = "Rate",
                ["Plugins.Tax.AbcTax.Fields.Store"] = "Store",
                ["Plugins.Tax.AbcTax.Fields.Store.Hint"] = "If an asterisk is selected, then this shipping rate will apply to all stores.",
                ["Plugins.Tax.AbcTax.Fields.Country"] = "Country",
                ["Plugins.Tax.AbcTax.Fields.Country.Hint"] = "The country.",
                ["Plugins.Tax.AbcTax.Fields.StateProvince"] = "State / province",
                ["Plugins.Tax.AbcTax.Fields.StateProvince.Hint"] = "If an asterisk is selected, then this tax rate will apply to all customers from the given country, regardless of the state.",
                ["Plugins.Tax.AbcTax.Fields.Zip"] = "Zip",
                ["Plugins.Tax.AbcTax.Fields.Zip.Hint"] = "Zip / postal code. If zip is empty, then this tax rate will apply to all customers from the given country or state, regardless of the zip code.",
                ["Plugins.Tax.AbcTax.Fields.TaxCategory"] = "Tax category",
                ["Plugins.Tax.AbcTax.Fields.TaxCategory.Hint"] = "The tax category.",
                ["Plugins.Tax.AbcTax.Fields.Percentage"] = "Percentage",
                ["Plugins.Tax.AbcTax.Fields.Percentage.Hint"] = "The tax rate.",
                ["Plugins.Tax.AbcTax.Fields.IsTaxJarEnabled"] = "Is TaxJar enabled",
                ["Plugins.Tax.AbcTax.Fields.IsTaxJarEnabled.Hint"] = "Whether the rate is enabled.",
                ["Plugins.Tax.AbcTax.Fields.TaxJarAPIToken"] = "TaxJar API Token",
                ["Plugins.Tax.AbcTax.Fields.TaxJarAPIToken.Hint"] = "Whether the rate is enabled.",
                ["Plugins.Tax.AbcTax.AddRecord"] = "Add tax rate",
                ["Plugins.Tax.AbcTax.AddRecordTitle"] = "New tax rate"
            });

            // If possible, import data from old Tax plugin
            await _nopDataProvider.ExecuteNonQueryAsync($@"
                INSERT INTO AbcTaxRate (StoreId, TaxCategoryId, CountryId, StateProvinceId, Zip, Percentage, IsTaxJarEnabled)
                SELECT
                    StoreId,
                    TaxCategoryId,
                    CountryId,
                    StateProvinceId,
                    Zip,
                    Percentage,
                    EnableTaxState
                FROM
                    TaxRate
            ");

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            //settings
            await _settingService.DeleteSettingAsync<AbcTaxSettings>();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Tax.AbcTax");

            await base.UninstallAsync();
        }
    }
}