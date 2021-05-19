using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
    public class ImportRelatedProductsTask : IScheduleTask
    {
        private readonly IImportRelatedProducts _importService;
        private readonly ImportSettings _importSettings;

        public ImportRelatedProductsTask(
            IImportRelatedProducts importService,
            ImportSettings importSettings
        )
        {
            _importService = importService;
            _importSettings = importSettings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipImportRelatedProductsTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();
            await _importService.ImportAsync();
            this.LogEnd();
        }
    }
}