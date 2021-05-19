using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using System;
using Nop.Plugin.Misc.AbcCore.Extensions;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync.Tasks
{
    public class MigrateAbcWarehouseContentTask : IScheduleTask
    {
        private readonly INopDataProvider _nopDbContext;

        public MigrateAbcWarehouseContentTask(INopDataProvider nopDbContext)
        {
            _nopDbContext = nopDbContext;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            try
            {
                await MigrateContentAsync();
                await RenameAbcWarehouseToHawthorneAsync();
            }
            catch (Exception e)
            {
                await EngineContext.Current.Resolve<ILogger>().WarningAsync("Unable to migrate content from ABCWarehouse.com", e);
            }
        }

        private async System.Threading.Tasks.Task MigrateContentAsync()
        {
            var dbName = "NOPCommerce";
            int affectedRowCount = await EngineContext.Current.Resolve<INopDataProvider>().ExecuteNonQueryAsync(
                ImportTaskExtensions.GetSqlScript("Nopcommerce_Migrate_Primary_Content",
                    dbName));
            await EngineContext.Current.Resolve<ILogger>().InformationAsync(
                $"{affectedRowCount} rows migrated from ABCWarehouse to Hawthorne content.");
        }

        private static async System.Threading.Tasks.Task RenameAbcWarehouseToHawthorneAsync()
        {
            int affectedRowCount = await EngineContext.Current.Resolve<INopDataProvider>().ExecuteNonQueryAsync(
                ImportTaskExtensions.GetSqlScript("RenameAbcWarehouseToHawthorne"));
            await EngineContext.Current.Resolve<ILogger>().InformationAsync(
                $"{affectedRowCount} rows renamed from ABCWarehouse to Hawthorne content.");
        }
    }
}

