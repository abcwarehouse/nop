using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Plugin.Misc.AbcSync.Services;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Seo;
using Nop.Services.Tasks;
using SevenSpikes.Nop.Plugins.StoreLocator.Domain.Shops;
using System.Data;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync.Tasks.CoreUpdate
{
    public partial class ImportShopsTask : IScheduleTask
    {
        private readonly ImportSettings _settings;
        private readonly INopDataProvider _nopDbContext;
        private readonly ICustomShopService _customShopService;
        private readonly IImportUtilities _importUtilities;
        private readonly IUrlRecordService _urlRecordService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILogger _logger;
        private readonly IGeocodingService _geocodingService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly CoreSettings _coreSettings;

        public ImportShopsTask(
            ImportSettings settings,
            INopDataProvider nopDbContext,
            ICustomShopService customShopService,
            IImportUtilities importUtilities,
            IUrlRecordService urlRecordService,
            IGenericAttributeService genericAttributeService,
            ILogger logger,
            IGeocodingService geocodingService,
            IStateProvinceService stateProvinceService,
            CoreSettings coreSettings
        )
        {
            _settings = settings;
            _nopDbContext = nopDbContext;
            _customShopService = customShopService;
            _importUtilities = importUtilities;
            _urlRecordService = urlRecordService;
            _genericAttributeService = genericAttributeService;
            _logger = logger;
            _geocodingService = geocodingService;
            _stateProvinceService = stateProvinceService;
            _coreSettings = coreSettings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (!_settings.ImportABCStores && !_settings.ImportHawthorneStores)
            {
                throw new NopException("Neither store is selected for import, cannot import physical stores.");
            }

            this.LogStart();

            var shopAbcTableName = "ShopAbc";
            IDbConnection backendConn = _coreSettings.GetBackendDbConnection();
            IDbCommand backendSelect = backendConn.CreateCommand();
            backendSelect.CommandText = BackendPhysicalStore.SelectStmt;
            backendConn.Open();

            using (IDataReader reader = backendSelect.ExecuteReader())
            {
                while (reader.Read())
                {
                    var backendPhysicalStore = BackendPhysicalStore.FromDataReader(reader);

                    var isAbcStore = backendPhysicalStore.StoreType.Equals(PhysicalStoreType.IsAbc);
                    var isHawStore = backendPhysicalStore.StoreType.Equals(PhysicalStoreType.IsHaw);
                    if (!((_settings.ImportABCStores && isAbcStore) || (_settings.ImportHawthorneStores && isHawStore)))
                    {
                        continue;
                    }

                    var shortDescription = $"<p>{backendPhysicalStore.Address}</p> <p>{backendPhysicalStore.City}, {backendPhysicalStore.State} {backendPhysicalStore.Zip}</p> <p>{backendPhysicalStore.Phone}</p>";
                    var fullDescription = $"<p>{(isAbcStore ? "ABC Warehouse - " : "Hawthorne - ")} {backendPhysicalStore.BranchName}</p>" + shortDescription;

                    var existingShop = _customShopService.GetShopByAbcBranchId(backendPhysicalStore.BranchId);

                    // Create new record if not existing
                    if (existingShop == null)
                    {
                        var stateProvince = _stateProvinceService.GetStateProvinceByAbbreviation(backendPhysicalStore.State);
                        var nopAddress = new Address()
                        {
                            Address1 = backendPhysicalStore.Address,
                            City = backendPhysicalStore.City,
                            ZipPostalCode = backendPhysicalStore.Zip,
                            StateProvinceId = stateProvince.Id,
                        };

                        // Skip geocoding for now
                        //var coordinates = _geocodingService.GeocodeAddress(nopAddress);

                        Shop shop = new Shop()
                        {
                            Name = backendPhysicalStore.BranchName,
                            IsVisible = true,
                            ShortDescription = shortDescription,
                            FullDescription = fullDescription,
                            //Latitude = coordinates.Latitude.ToString(),
                            //Longitude = coordinates.Longitude.ToString()
                        };
                        _customShopService.InsertShop(shop);
                        await _nopDbContext.ExecuteNonQueryAsync($"INSERT INTO {shopAbcTableName} (ShopId, AbcId, AbcEmail) VALUES ({shop.Id}, '{backendPhysicalStore.BranchId}', '{backendPhysicalStore.Email}')");
                    }
                    // Update the existing shop
                    else
                    {
                        existingShop.Name = backendPhysicalStore.BranchName;
                        existingShop.ShortDescription = shortDescription;
                        existingShop.FullDescription = fullDescription;

                        _customShopService.UpdateShop(existingShop);
                        await _nopDbContext.ExecuteNonQueryAsync($"UPDATE {shopAbcTableName} SET AbcId = '{backendPhysicalStore.BranchId}', AbcEmail = '{backendPhysicalStore.Email}' WHERE ShopId = {existingShop.Id}");
                    }
                }
            }


            this.LogEnd();
        }
    }
}