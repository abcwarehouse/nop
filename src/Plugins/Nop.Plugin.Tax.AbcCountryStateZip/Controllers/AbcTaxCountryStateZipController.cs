using System.ComponentModel;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Core;
using Nop.Plugin.Tax.AbcCountryStateZip.Domain;
using Nop.Plugin.Tax.AbcCountryStateZip.Models;
using Nop.Plugin.Tax.AbcCountryStateZip.Services;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Tax;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Models.Extensions;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Mvc.Filters;
using System.Threading.Tasks;

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

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedView();

            var taxCategories = _taxCategoryService.GetAllTaxCategories();
            if (!taxCategories.Any())
                return Content("No tax categories can be loaded");

            var model = new ConfigurationModel();
            //stores
            model.AvailableStores.Add(new SelectListItem { Text = "*", Value = "0" });
            var stores = _storeService.GetAllStores();
            foreach (var s in stores)
                model.AvailableStores.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
            //tax categories
            foreach (var tc in taxCategories)
                model.AvailableTaxCategories.Add(new SelectListItem { Text = tc.Name, Value = tc.Id.ToString() });
            //countries
            var countries = _countryService.GetAllCountries(showHidden: true);
            foreach (var c in countries)
                model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            //states
            model.AvailableStates.Add(new SelectListItem { Text = "*", Value = "0" });
            var defaultCountry = countries.FirstOrDefault();
            if (defaultCountry != null)
            {
                var states = _stateProvinceService.GetStateProvincesByCountryId(defaultCountry.Id);
                foreach (var s in states)
                    model.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString() });
            }

            model.TaxJarAPIToken = _settings.TaxJarAPIToken;

            return View("~/Plugins/Tax.AbcCountryStateZip/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public IActionResult Configure(ConfigurationModel model)
        {
            _settingService.SaveSetting(AbcCountyStateZipSettings.FromModel(model));

            _notificationService.SuccessNotification(
                _localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [HttpPost]
        public async Task<ActionResult> RatesListAsync(ConfigurationModel searchModel)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return AccessDeniedDataTablesJson();

            var records = _taxRateService.GetAllTaxRates(searchModel.Page - 1, searchModel.PageSize);

            var gridModel = new TaxRateListModel().PrepareToGrid(searchModel, records, () =>
            {
                return records.Select(record => new TaxRateModel
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
                    ZipCode = !string.IsNullOrEmpty(record.ZipCode) ? record.ZipCode : "*",
                    Percentage = record.Percentage,
                    EnableTaxState = record.EnableTaxState
                });
            });

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<ActionResult> RateUpdateAsync(TaxRateModel model)
        {
            if (!(await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageTaxSettings)))
                return Content("Access denied");

            var taxRate = await _taxRateService.GetTaxRateByIdAsync(model.Id);
            taxRate.ZipCode = model.ZipCode == "*" ? null : model.ZipCode;
            taxRate.Percentage = model.Percentage;
            taxRate.EnableTaxState = model.EnableTaxState;
            await _taxRateService.UpdateTaxRateAsync(taxRate);

            return new NullJsonResult();
        }

        [HttpPost]
        public async Task<ActionResult> RateDeleteAsync(int id)
        {
            if (!(await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageTaxSettings)))
                return Content("Access denied");

            var taxRate = await _taxRateService.GetTaxRateByIdAsync(id);
            if (taxRate != null)
                await _taxRateService.DeleteTaxRateAsync(taxRate);

            return new NullJsonResult();
        }

        [HttpPost]
        public async Task<ActionResult> AddTaxRateAsync(ConfigurationModel model)
        {
            if (!(await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManageTaxSettings)))
                return Content("Access denied");

            var taxRate = new TaxRate
            {
                StoreId = model.AddStoreId,
                TaxCategoryId = model.AddTaxCategoryId,
                CountryId = model.AddCountryId,
                StateProvinceId = model.AddStateProvinceId,
                ZipCode = model.AddZip,
                Percentage = model.AddPercentage
            };
            await _taxRateService.InsertTaxRateAsync(taxRate);

            return Json(new { Result = true });
        }
        [HttpPost]
        public async Task<ActionResult> EnableStateAsync(string Id, bool status)
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
