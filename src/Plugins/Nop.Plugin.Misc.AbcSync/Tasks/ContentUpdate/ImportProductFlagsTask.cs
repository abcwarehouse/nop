using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
    public class ImportProductFlagsTask : IScheduleTask
    {
        private readonly IImportProductFlags _import;
        private readonly ImportSettings _importSettings;

        public ImportProductFlagsTask(
            IImportProductFlags import,
            ImportSettings importSettings
        )
        {
            _import = import;
            _importSettings = importSettings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipImportProductFlagsTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();
            await _import.ImportAsync();
            this.LogEnd();
        }
    }
}