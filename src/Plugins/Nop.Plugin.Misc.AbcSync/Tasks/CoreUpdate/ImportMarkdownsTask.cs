using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    public class ImportMarkdownsTask : Nop.Services.Tasks.IScheduleTask
    {
        private readonly IImportMarkdowns _importService;
        private readonly ImportSettings _importSettings;

        public ImportMarkdownsTask(
            IImportMarkdowns importService,
            ImportSettings importSettings
        )
        {
            _importService = importService;
            _importSettings = importSettings;
        }

        public Task ExecuteAsync()
        {
            if (_importSettings.SkipImportMarkdownsTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();
            _importService.Import();
            this.LogEnd();
        }
    }
}