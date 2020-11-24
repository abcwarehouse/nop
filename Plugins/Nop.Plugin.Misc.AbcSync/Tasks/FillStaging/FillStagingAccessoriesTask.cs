using System.Data.SqlClient;
using System.Data;
using Nop.Plugin.Misc.AbcSync.Staging;
using Nop.Services.Logging;
using System.Collections.Generic;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
    class FillStagingAccessoriesTask : IScheduleTask
	{
		private readonly ILogger _logger;
        private readonly ImportSettings _importSettings;
        private readonly CoreSettings _coreSettings;

        public FillStagingAccessoriesTask(
            ILogger logger,
            ImportSettings importSettings,
            CoreSettings coreSettings
        )
        {
            _logger = logger;
            _importSettings = importSettings;
            _coreSettings = coreSettings;
        }

        public void Execute()
		{
            if (_importSettings.SkipFillStagingAccessoriesTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();
            using (SqlConnection stagingConn = _importSettings.GetStagingDbConnection() as SqlConnection)
			{
				using (IDbConnection backendConn = _coreSettings.GetBackendDbConnection())
				{
					ImportAccessories(stagingConn, backendConn, _logger);
				}
			}
            this.LogEnd();
		}

        private void ImportAccessories(SqlConnection stagingConn, IDbConnection backendConn, ILogger logger)
        {
            SqlCommand stagingActions = stagingConn.CreateCommand();
            stagingConn.Open();

            // Find all the staged item numbers.
            HashSet<string> stagedItemNums =
                StagingUtilities.GetStagedItemNumbers(stagingActions);

            // Prepare the database for staging.
            // Prepare the insert command.
            StagingUtilities.PrepStagingDb(stagingActions, "Product_Accessory");
            PrepStagingInsert(stagingActions);

            IDbCommand selectAccessories = backendConn.CreateCommand();
            backendConn.Open();

            // Prepare the backend select statement and run the data reader.
            PrepBackendSelect(selectAccessories);
            using (IDataReader accessory = selectAccessories.ExecuteReader())
            {
                ImportAccessory(accessory, stagedItemNums, stagingActions, logger);
            }
        }

        private static void PrepStagingInsert(SqlCommand command)
        {
            command.CommandText =
                "INSERT INTO Product_Accessory" +
                " (ItemNumber, AccessoryItemNumber" +
                ")" +
                " VALUES" +
                " (" +
                    "@StagingItemNumber, @StagingAccessoryItemNumber" +
                ")";
            command.Parameters.Add("@StagingItemNumber", SqlDbType.NVarChar);
            command.Parameters.Add("@StagingAccessoryItemNumber", SqlDbType.NVarChar);

            return;
        }

        private static void PrepBackendSelect(IDbCommand command)
        {
            command.CommandText =
                $"SELECT IA.ITEM_NUMBER, AI.ACC_ITEM_NUMBER FROM {BackendDbConstants.ItemToAccessoryTable} AS IA inner JOIN {BackendDbConstants.AccessoryToItemTable} AS AI ON IA.ACC_CODE = AI.ACC_CODE";

            return;
        }

        private static void ImportAccessory(IDataReader accessory,
            HashSet<string> stagedItemNums, SqlCommand insert, ILogger logger)
        {
            while (accessory.Read())
            {
                // Map the information to C# variables.
                string itemNumber = accessory["ITEM_NUMBER"] as string;
                string accessoryItemNumber = accessory["ACC_ITEM_NUMBER"] as string;

                // First, skip any item numbers and accessory item numbers that were not staged.
                if (!stagedItemNums.Contains(itemNumber))
                {
                    continue;
                }

                if (!stagedItemNums.Contains(accessoryItemNumber))
                {
                    continue;
                }

                // No item number means that this row is invalid
                // and unreferencable.
                if (string.IsNullOrWhiteSpace(itemNumber))
                {
                    string message = "Unable to import a ." +
                        " No item number could be found relating to it.";
                    logger.Warning(message);

                    continue;
                }

                // Perform the insert.
                insert.Parameters["@StagingItemNumber"].Value = itemNumber;
                insert.Parameters["@StagingAccessoryItemNumber"].Value = accessoryItemNumber;

                insert.ExecuteNonQuery();
            }

            return;
        }
    }
}