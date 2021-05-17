using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
    /// <summary>
    /// Will update staging categories based on the xslx mapping specified in the web.config
    /// </summary>
    public class ImportFeaturedProductsTask : Nop.Services.Tasks.IScheduleTask
    {
        private readonly IImportService _importService;
        private readonly ImportSettings _importSettings;

        public ImportFeaturedProductsTask(
            IImportService importService,
            ImportSettings importSettings
        )
        {
            this._importService = importService;
            _importSettings = importSettings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipImportFeaturedProductsTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();
            _importService.ImportFeaturedProducts();
            this.LogEnd();
        }
    }
}
