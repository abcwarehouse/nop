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

        public override void Install()
        {
            InstallStoredProcs();
            UpdateLocales();
        }

        public override void Uninstall()
        {
            _localizationService.DeletePluginLocaleResources(CoreLocales.Base);

            DeleteStoredProcs();
        }

        public override void Update(string oldVersion, string currentVersion)
        {
            InstallStoredProcs();
            UpdateLocales();
        }

        public void ManageSiteMap(SiteMapNode rootNode)
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
        }

        private void DeleteStoredProcs()
        {
            _nopDataProvider.ExecuteNonQuery("DROP PROCEDURE IF EXISTS dbo.UpdateAbcPromos");
        }

        private void InstallStoredProcs()
        {
            DeleteStoredProcs();
            string updateAbcPromosStoredProcScript = File.ReadAllText(
                            $"{_webHostEnvironment.ContentRootPath}/Plugins/Misc.AbcCore/UpdateAbcPromos.StoredProc.sql"
                        );
            _nopDataProvider.ExecuteNonQuery(updateAbcPromosStoredProcScript);
        }

        private void UpdateLocales()
        {
            _localizationService.AddPluginLocaleResource(
                new Dictionary<string, string>
                {
                    [CoreLocales.BackendDbConnectionString] = "Backend DB Connection String",
                    [CoreLocales.BackendDbConnectionStringHint] = "Connection string for connecting to ERP database.",
                    ["Admin.Catalog.Products.PLPDescription"] = "PLP description",
                    ["Admin.Catalog.Products.PLPDescriptionHint"] = "Product listing page description."
                }
            );
        }
    }
}
