using Nop.Plugin.Misc.AbcSync.Services;
using Nop.Services.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
    public class ImportSotPicturesTask : IScheduleTask
    {
        private readonly IImportPictureService _importPictureService;
        private readonly ImportSettings _importSettings;

        public ImportSotPicturesTask(
            IImportPictureService importPictureService,
            ImportSettings importSettings
        )
        {
            _importPictureService = importPictureService;
            _importSettings = importSettings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipImportSotPicturesTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();
            await _importPictureService.ImportSiteOnTimePicturesAsync();
            this.LogEnd();
        }
    }
}