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
        public ActionResult RatesList(ConfigurationModel searchModel)
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
                    StoreName = _storeService.GetStoreById(record.StoreId)?.Name ?? "*",
                    TaxCategoryId = record.TaxCategoryId,
                    TaxCategoryName = _taxCategoryService.GetTaxCategoryById(record.TaxCategoryId)?.Name ?? string.Empty,
                    CountryId = record.CountryId,
                    CountryName = _countryService.GetCountryById(record.CountryId)?.Name ?? "Unavailable",
                    StateProvinceId = record.StateProvinceId,
                    StateProvinceName = _stateProvinceService.GetStateProvinceById(record.StateProvinceId)?.Name ?? "*",
                    Zip = !string.IsNullOrEmpty(record.Zip) ? record.Zip : "*",
                    Percentage = record.Percentage,
                    EnableTaxState = record.EnableTaxState
                });
            });

            return Json(gridModel);
        }

        [HttpPost]
        public ActionResult RateUpdate(TaxRateModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return Content("Access denied");

            var taxRate = _taxRateService.GetTaxRateById(model.Id);
            taxRate.Zip = model.Zip == "*" ? null : model.Zip;
            taxRate.Percentage = model.Percentage;
            taxRate.EnableTaxState = model.EnableTaxState;
            _taxRateService.UpdateTaxRate(taxRate);

            return new NullJsonResult();
        }

        [HttpPost]
        public ActionResult RateDelete(int id)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
                return Content("Access denied");

            var taxRate = _taxRateService.GetTaxRateById(id);
            if (taxRate != null)
                _taxRateService.DeleteTaxRate(taxRate);

            return new NullJsonResult();
        }

        [HttpPost]
        public ActionResult AddTaxRate(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageTaxSettings))
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
            _taxRateService.InsertTaxRate(taxRate);

            return Json(new { Result = true });
        } 
        [HttpPost]
        public ActionResult EnableState(string Id, bool status)
        {
            var taxRate = _taxRateService.GetTaxRateById(Convert.ToInt32(Id));
            if (taxRate != null)
            { 
                taxRate.EnableTaxState = status;
                _taxRateService.UpdateTaxRate(taxRate);
            } 
            return Json(new { Result = true });
        }
    }
}
