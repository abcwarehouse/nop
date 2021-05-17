using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using System;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync.Tasks
{
    public class MigrateAbcWarehouseContentTask : IScheduleTask
    {
        private readonly INopDataProvider _nopDbContext;

        public MigrateAbcWarehouseContentTask(INopDataProvider nopDbContext)
        {
            _nopDbContext = nopDbContext;
        }

        public void Execute()
        {
            try
            {
                MigrateContent();
                RenameAbcWarehouseToHawthorne();
            }
            catch (Exception e)
            {
                EngineContext.Current.Resolve<ILogger>().Warning("Unable to migrate content from ABCWarehouse.com", e);
            }
        }

        private void MigrateContent()
        {
            var dbName = "NOPCommerce";
            int affectedRowCount = EngineContext.Current.Resolve<INopDataProvider>().ExecuteNonQuery(
                ImportTaskExtensions.GetSqlScript("Nopcommerce_Migrate_Primary_Content",
                    dbName));
            EngineContext.Current.Resolve<ILogger>().Information(
                $"{affectedRowCount} rows migrated from ABCWarehouse to Hawthorne content.");
        }

        private static void RenameAbcWarehouseToHawthorne()
        {
            int affectedRowCount = EngineContext.Current.Resolve<INopDataProvider>().ExecuteNonQuery(
                ImportTaskExtensions.GetSqlScript("RenameAbcWarehouseToHawthorne"));
            EngineContext.Current.Resolve<ILogger>().Information(
                $"{affectedRowCount} rows renamed from ABCWarehouse to Hawthorne content.");
        }
    }
}

