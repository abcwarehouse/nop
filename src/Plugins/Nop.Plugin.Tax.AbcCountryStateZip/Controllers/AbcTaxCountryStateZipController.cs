using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Plugin.Tax.AbcCountryStateZip.Domain;
using Nop.Plugin.Tax.AbcCountryStateZip.Models;
using Nop.Plugin.Tax.AbcCountryStateZip.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Services.Messages;

namespace Nop.Plugin.Tax.AbcCountryStateZip.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class AbcTaxCountryStateZipController : BasePluginController
    {
        private readonly ITaxCategoryService _taxCategoryService;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ITaxRateService _taxRateService;
        private readonly IPermissionService _permissionService;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly AbcCountyStateZipSettings _settings;

        public AbcTaxCountryStateZipController(ITaxCategoryService taxCategoryService,
            ICountryService countryService,
            IStateProvinceService stateProvinceService,
            ITaxRateService taxRateService,
            IPermissionService permissionService,
            IStoreService storeService,
            ISettingService settingService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            AbcCountyStateZipSettings settings
        )
        {
            _taxCategoryService = taxCategoryService;
            _countryService = countryService;
            _stateProvinceService = stateProvinceService;
            _taxRateService = taxRateService;
            _permissionService = permissionService;
            _storeService = storeService;
            _settingService = settingService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _settings = settings;
        }

        public async Task<IActionResult> Configure()
        {
            if (!(await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageTaxSettings)))
                return AccessDeniedView();

            var taxCategories = await _taxCategoryService.GetAllTaxCategoriesAsync();
            if (!taxCategories.Any())
                return Content("No tax categories can be loaded");

            var model = new ConfigurationModel();
            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = "0" });
            var stores = await _storeService.GetAllStoresAsync();
            foreach (var s in stores)
                model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
            //tax categories
            foreach (var tc in taxCategories)
                model.AvailableTaxCategories.Add(new SelectListItem { Text = tc.Name, Value = tc.Id.ToString() });
            //countries
            var countries = await _countryService.GetAllCountriesAsync(showHidden: true);
            foreach (var c in countries)
                model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            //states
            model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "0" });
            var defaultCountry = countries.FirstOrDefault();
            if (defaultCountry != null)
            {
                var states = await _stateProvinceService.GetStateProvincesByCountryIdAsync(defaultCountry.Id);
                foreach (var s in states)
                    model.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
            }

            model.TaxJarAPIToken = _settings.TaxJarAPIToken;

            return View("~/Plugins/Tax.AbcCountryStateZip/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            await _settingService.SaveSettingAsync(AbcCountyStateZipSettings.FromModel(model));

            _notificationService.SuccessNotification(
                await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        [HttpPost]
        public async Task<ActionResult> RatesList(ConfigurationModel searchModel)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageTaxSettings))
                return await AccessDeniedDataTablesJson();

            var records = await _taxRateService.GetAllTaxRatesAsync(searchModel.Page - 1, searchModel.PageSize);

            var gridModel = await new TaxRateListModel().PrepareToGridAsync(searchModel, records, () =>
            {
                return records.SelectAwait(async record => new TaxRateModel
                {
                    Id = record.Id,
                    StoreId = record.StoreId,
                    StoreName = (await _storeService.GetStoreByIdAsync(record.StoreId))?.Name ?? "*",
                    TaxCategoryId = record.TaxCategoryId,
                    TaxCategoryName = (await _taxCategoryService.GetTaxCategoryByIdAsync(record.TaxCategoryId))?.Name ?? string.Empty,
                    CountryId = record.CountryId,
                    CountryName = (await _countryService.GetCountryByIdAsync(record.CountryId))?.Name ?? "Unavailable",
                    StateProvinceId = record.StateProvinceId,
                    StateProvinceName = (await _stateProvinceService.GetStateProvinceByIdAsync(record.StateProvinceId))?.Name ?? "*",
                    Zip = !string.IsNullOrEmpty(record.Zip) ? record.Zip : "*",
                    Percentage = record.Percentage,
                    EnableTaxState = record.EnableTaxState
                });
            });

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<ActionResult> RateUpdate(TaxRateModel model)
        {
            if (!(await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageTaxSettings)))
                return Content("Access denied");

            var taxRate = await _taxRateService.GetTaxRateByIdAsync(model.Id);
            taxRate.Zip = model.Zip == "*" ? null : model.Zip;
            taxRate.Percentage = model.Percentage;
            taxRate.EnableTaxState = model.EnableTaxState;
            await _taxRateService.UpdateTaxRateAsync(taxRate);

            return new NullJsonResult();
        }

        [HttpPost]
        public async Task<ActionResult> RateDelete(int id)
        {
            if (!(await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageTaxSettings)))
                return Content("Access denied");

            var taxRate = await _taxRateService.GetTaxRateByIdAsync(id);
            if (taxRate != null)
                await _taxRateService.DeleteTaxRateAsync(taxRate);

            return new NullJsonResult();
        }

        [HttpPost]
        public async Task<ActionResult> AddTaxRate(ConfigurationModel model)
        {
            if (!(await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageTaxSettings)))
                return Content("Access denied");

            var taxRate = new TaxRate
            {
                StoreId = model.AddStoreId,
                TaxCategoryId = model.AddTaxCategoryId,
                CountryId = model.AddCountryId,
                StateProvinceId = model.AddStateProvinceId,
                Zip = model.AddZip,
                Percentage = model.AddPercentage
            };
            await _taxRateService.InsertTaxRateAsync(taxRate);

            return Json(new { Result = true });
        }
        [HttpPost]
        public async Task<ActionResult> EnableState(string Id, bool status)
        {
            var taxRate = await _taxRateService.GetTaxRateByIdAsync(Convert.ToInt32(Id));
            if (taxRate != null)
            {
                taxRate.EnableTaxState = status;
                await _taxRateService.UpdateTaxRateAsync(taxRate);
            }
            return Json(new { Result = true });
        }
    }
}
