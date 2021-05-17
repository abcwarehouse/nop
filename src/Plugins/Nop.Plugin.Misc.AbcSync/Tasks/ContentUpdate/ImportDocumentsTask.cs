using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
    public class ImportDocumentsTask : Nop.Services.Tasks.IScheduleTask
    {
        private readonly IDocumentImportService _importService;
        private readonly ImportSettings _importSettings;

        public ImportDocumentsTask(
            IDocumentImportService importService,
            ImportSettings importSettings
        )
        {
            this._importService = importService;
            _importSettings = importSettings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipImportDocumentsTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();
            _importService.ImportDocuments();
            this.LogEnd();
        }
    }
}