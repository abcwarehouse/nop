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

		public void Execute()
		{
            if (_importSettings.SkipImportIsamSpecsTask)
			{
				this.Skipped();
				return;
			}

            this.LogStart();
            //_logger.Information($"Begin Task: ImportColor");
            //_import.ImportColor();
            //_logger.Information($"End Task: ImportColor");
            _logger.Information($"Begin Task: ImportSOTSpecs");
            _import.ImportSiteOnTimeSpecs();
            _logger.Information($"End Task: ImportSOTSpecs");
            this.LogEnd();
		}
	}
}