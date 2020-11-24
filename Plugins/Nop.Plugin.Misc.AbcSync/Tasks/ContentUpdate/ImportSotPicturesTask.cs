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

        public void Execute()
        {
            if (_importSettings.SkipImportSotPicturesTask)
			{
				this.Skipped();
				return;
			}

            this.LogStart();
            _importPictureService.ImportSiteOnTimePictures();
            this.LogEnd();
        }
    }
}