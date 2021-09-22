using Nop.Data;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;
using System.Threading.Tasks;
using Nop.Plugin.Misc.AbcCore.Data;

namespace Nop.Plugin.Misc.AbcSync
{
    public class CleanDuplicateImagesTask : IScheduleTask
    {
        private readonly ImportSettings _importSettings;
        private readonly ICustomNopDataProvider _nopDataProvider;

        public CleanDuplicateImagesTask(
            ImportSettings importSettings,
            ICustomNopDataProvider nopDataProvider
        )
        {
            _importSettings = importSettings;
            _nopDataProvider = nopDataProvider;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipCleanDuplicateImagesTask)
            {
                this.Skipped();
                return;
            }

            

            await _nopDataProvider.ExecuteNonQueryAsync(
                ImportTaskExtensions.GetSqlScript("Clean_Duplicate_Images"),
                600
            );

            
        }
    }
}