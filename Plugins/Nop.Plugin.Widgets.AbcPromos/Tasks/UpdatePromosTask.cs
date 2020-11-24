using System.Collections.Generic;
using System.Linq;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Widgets.AbcPromos.Tasks.LegacyTasks;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using Nop.Services.Seo;
using Nop.Services.Tasks;

namespace Nop.Plugin.Widgets.AbcPromos.Tasks
{
    public class UpdatePromosTask : IScheduleTask
    {
        private readonly CoreSettings _coreSettings;

        private readonly ILogger _logger;

        private readonly INopDataProvider _nopDataProvider;

        private readonly IAbcPromoService _abcPromoService;
        private readonly IManufacturerService _manufacturerService;
        private readonly IUrlRecordService _urlRecordService;

        public UpdatePromosTask(
            CoreSettings coreSettings,
            AbcPromosSettings settings,
            ILogger logger,
            INopDataProvider nopDataProvider,
            IAbcPromoService abcPromoService,
            IManufacturerService manufacturerService,
            IUrlRecordService urlRecordService
        )
        {
            _coreSettings = coreSettings;
            _logger = logger;
            _nopDataProvider = nopDataProvider;
            _abcPromoService = abcPromoService;
            _manufacturerService = manufacturerService;
            _urlRecordService = urlRecordService;
        }

        public void Execute()
        {
            if (_coreSettings.AreExternalCallsSkipped)
            {
                _logger.Warning("Widgets.AbcPromos: External calls skipped, will not update from backend.");
            }
            else {
                _nopDataProvider.ExecuteStoredProcedure("UpdateAbcPromos", 300);
            }

            var promos = _abcPromoService.GetAllPromos();
            SetPromoManufacturers(promos);
            SetPromoSlugs(promos);

            EngineContext.Current.Resolve<GenerateRebatePromoPageTask>().Execute();
        }

        private void SetPromoManufacturers(IList<AbcPromo> promos)
        {
            foreach (var promo in promos)
            {
                var manufacturerIds = new HashSet<int>();
                var products = _abcPromoService.GetProductsByPromoId(promo.Id);
                foreach (var product in products)
                {
                    var productManufacturers =
                        _manufacturerService.GetProductManufacturersByProductId(product.Id);

                    foreach (var productManufacturer in productManufacturers)
                    {
                        manufacturerIds.Add(productManufacturer.ManufacturerId);
                    }
                }

                if (manufacturerIds.Count == 1)
                {
                    promo.ManufacturerId = manufacturerIds.FirstOrDefault();
                    _abcPromoService.UpdatePromo(promo);
                }
            }
        }

        private void SetPromoSlugs(IList<AbcPromo> promos)
        {
            foreach (var promo in promos)
            {
                var name = promo.Name.Replace("_", "-") + "-" + promo.Description;

                if (promo.ManufacturerId != null) {
                    var manufacturer =
                        _manufacturerService.GetManufacturerById(promo.ManufacturerId.Value);
                    var manufacturerName = manufacturer.Name;

                    name = name.Insert(0, $"{manufacturerName} - ");
                }

                var seName = _urlRecordService.ValidateSeName(
                    promo,
                    string.Empty,
                    name,
                    true
                );
                _urlRecordService.SaveSlug(promo, seName, 0);
            }
        }
    }
}
