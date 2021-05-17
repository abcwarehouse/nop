using Nop.Services.Logging;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
    class ImportIsamSpecsTask : IScheduleTask
    {
        private readonly IImportIsamSpecs _import;
        private readonly ILogger _logger;
        private readonly ImportSettings _importSettings;

        public ImportIsamSpecsTask(
            IImportIsamSpecs import,
            ILogger logger,
            ImportSettings importSettings
        )
        {
            _import = import;
            _logger = logger;
            _importSettings = importSettings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipImportIsamSpecsTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();
            //await _logger.InformationAsync($"Begin Task: ImportColor");
            //_import.ImportColor();
            //await _logger.InformationAsync($"End Task: ImportColor");
            await _logger.InformationAsync($"Begin Task: ImportSOTSpecs");
            _import.ImportSiteOnTimeSpecs();
            await _logger.InformationAsync($"End Task: ImportSOTSpecs");
            this.LogEnd();
        }
    }
}