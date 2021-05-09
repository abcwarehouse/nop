using System.Collections.Generic;
using Nop.Core;
using Nop.Data;
using Nop.Services.Common;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using Nop.Web.Framework.Menu;
using Microsoft.AspNetCore.Routing;
using System.Linq;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcCore
{
    public class CorePlugin : BasePlugin, IMiscPlugin, IAdminMenuPlugin
    {
        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;
        private readonly INopDataProvider _nopDataProvider;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CorePlugin(
            IWebHelper webHelper,
            ILocalizationService localizationService,
            INopDataProvider nopDataProvider,
            IWebHostEnvironment webHostEnvironment
        )
        {
            _webHelper = webHelper;
            _localizationService = localizationService;
            _nopDataProvider = nopDataProvider;
            _webHostEnvironment = webHostEnvironment;
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/AbcCore/Configure";
        }

        public override async Task InstallAsync()
        {
            await InstallStoredProcs();
            await UpdateLocales();

            await base.InstallAsync();
        }

        public override async Task UninstallAsync()
        {
            await _localizationService.DeleteLocaleResourcesAsync(CoreLocales.Base);
            await DeleteStoredProcs();

            await base.UninstallAsync();
        }

        public override async Task UpdateAsync(string oldVersion, string currentVersion)
        {
            await InstallStoredProcs();
            await UpdateLocales();
        }

        private async Task DeleteStoredProcs()
        {
            await _nopDataProvider.ExecuteNonQueryAsync("DROP PROCEDURE IF EXISTS dbo.UpdateAbcPromos");
        }

        private async Task InstallStoredProcs()
        {
            await DeleteStoredProcs();
            string updateAbcPromosStoredProcScript = File.ReadAllText(
                            $"{_webHostEnvironment.ContentRootPath}/Plugins/Misc.AbcCore/UpdateAbcPromos.StoredProc.sql"
                        );
            await _nopDataProvider.ExecuteNonQueryAsync(updateAbcPromosStoredProcScript);
        }

        private async Task UpdateLocales()
        {
            await _localizationService.AddLocaleResourceAsync(
                new Dictionary<string, string>
                {
                    [CoreLocales.BackendDbConnectionString] = "Backend DB Connection String",
                    [CoreLocales.BackendDbConnectionStringHint] = "Connection string for connecting to ERP database.",
                    ["Admin.Catalog.Products.PLPDescription"] = "PLP description",
                    ["Admin.Catalog.Products.PLPDescriptionHint"] = "Product listing page description.",
                    [CoreLocales.IsDebugMode] = "Debug Mode",
                    [CoreLocales.IsDebugModeHint] = "Logs detailed information, useful for debugging issues.",
                    [CoreLocales.AreExternalCallsSkipped] = "Skip External Calls",
                    [CoreLocales.AreExternalCallsSkippedHint] = "Skips calls to ISAM API, useful for local development.",
                    [CoreLocales.FlixId] = "FLIX ID",
                    [CoreLocales.FlixIdHint] = "The ID to use for Flix calls."
                }
            );
        }

        public Task ManageSiteMapAsync(SiteMapNode rootNode)
        {
            return Task.Run(() => 
            {
                var rootMenuItem = new SiteMapNode()
                {
                    SystemName = "ABCWarehouse",
                    Title = "ABC Warehouse",
                    Visible = true,
                    RouteValues = new RouteValueDictionary() { { "area", "Admin" } },
                    ChildNodes = new List<SiteMapNode>()
                    {
                        new SiteMapNode()
                        {
                            SystemName = "ABCWarehouse.Promos",
                            Title = "Promos",
                            Visible = true,
                            ControllerName = "AbcPromo",
                            ActionName = "List"
                        },
                        new SiteMapNode()
                        {
                            SystemName = "ABCWarehouse.MissingImageProducts",
                            Title = "Missing Image Products",
                            Visible = true,
                            ControllerName = "MissingImageProduct",
                            ActionName = "List"
                        }
                    }
                };

                rootNode.ChildNodes.Add(rootMenuItem);
            });
        }
    }
}
