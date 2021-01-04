using Nop.Data;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
    public class CleanDuplicateImagesTask : IScheduleTask
    {
        private readonly ImportSettings _importSettings;
        private readonly INopDataProvider _nopDataProvider;

        public CleanDuplicateImagesTask(
            ImportSettings importSettings,
            INopDataProvider nopDataProvider
        )
        {
            _importSettings = importSettings;
            _nopDataProvider = nopDataProvider;
        }

        public void Execute()
        {
            if (_importSettings.SkipCleanDuplicateImagesTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();

            _nopDataProvider.ExecuteNonQuery(
                ImportTaskExtensions.GetSqlScript("Clean_Duplicate_Images"),
                300
            );

            this.LogEnd();
        }
    }
}