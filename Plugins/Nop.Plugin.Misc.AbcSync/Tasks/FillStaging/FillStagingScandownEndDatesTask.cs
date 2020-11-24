using System.Data.SqlClient;
using System.Data;
using Nop.Plugin.Misc.AbcSync.Staging;
using Nop.Services.Logging;
using System.Collections.Generic;
using System;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
    class FillStagingScandownEndDatesTask : IScheduleTask
	{
        private const string _backendDateFormat = "yyyyMMdd";
        private readonly ILogger _logger;
        private readonly ImportSettings _importSettings;
        private readonly CoreSettings _coreSettings;

        public FillStagingScandownEndDatesTask(
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
            if (_importSettings.SkipFillStagingScandownEndDatesTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();
			using (SqlConnection stagingConn = _importSettings.GetStagingDbConnection() as SqlConnection)
			{
				using (IDbConnection backendConn = _coreSettings.GetBackendDbConnection())
				{
					Import(stagingConn, backendConn, _logger);
				}
			}
            this.LogEnd();
		}

        private void Import(
            SqlConnection stagingConn, IDbConnection backendConn,
            ILogger logger)
        {
            SqlCommand stagingActions = stagingConn.CreateCommand();
            stagingConn.Open();

            // Find all the staged item numbers.
            HashSet<string> stagedItemNums =
                StagingUtilities.GetStagedItemNumbers(stagingActions);

            // Prepare the database for staging.
            // Prepare the insert command.
            StagingUtilities.PrepStagingDb(stagingActions, StagingDbConstants.ScandownTable);
            PrepStagingInsert(stagingActions);

            IDbCommand selectScandowns = backendConn.CreateCommand();
            backendConn.Open();

            // Prepare the backend select statement and run the data reader.
            PrepBackendSelect(selectScandowns);
            using (IDataReader scandown = selectScandowns.ExecuteReader())
            {
                ImportScandown(scandown, stagedItemNums, stagingActions, logger);
            }

            return;
        }

        private static void ImportScandown(IDataReader scandown,
            HashSet<string> stagedItemNums, SqlCommand insert, ILogger logger)
        {
            while (scandown.Read())
            {
                // Map the information to C# variables.
                string prodType = scandown[BackendDbConstants.InvProductType] as string;
                string modelId = scandown[BackendDbConstants.InvModel] as string;
                var vend_Markdown = scandown["VEND_MARKDOWN_AMT"] as decimal?;
                var comm_Markdown = scandown["COMM_MARKDOWN_AMT"] as decimal?;
                var sale_Markdown = scandown["SALE_MARKDOWN_AMT"] as decimal?;
                string itemNum = scandown[BackendDbConstants.ScandownItemNumber] as string;
                string beginDate = scandown[BackendDbConstants.ScandownBeginDate] as string;
                string endDate = scandown[BackendDbConstants.ScandownEndDate] as string;

                string sku = StagingUtilities.CalculateSku(modelId, prodType, itemNum);

                // First, skip any item numbers that were not staged.
                // This is not an error here, but rather in product staging.
                if (!stagedItemNums.Contains(itemNum))
                {
                    continue;
                }

                // No item number means that this row is invalid
                // and unreferencable.
                if (string.IsNullOrWhiteSpace(itemNum))
                {
                    string message = "Unable to import a scandown date." +
                        " No item number could be found relating to it.";
                    logger.Warning(message);

                    continue;
                }
                // A model ID is needed for mapping.
                if (string.IsNullOrWhiteSpace(sku))
                {
                    string message = "Unable to import a scandown date." +
                        " The associated model ID could not be found" +
                        " for item number " + itemNum;
                    logger.Warning(message);

                    continue;
                }
                // The end date is needed because that is the reason for having this.
                if (string.IsNullOrWhiteSpace(endDate))
                {
                    string message = "Unable to import the scandown date" +
                        " for model ID " + sku + "." +
                        " The end date is not provided.";
                    logger.Warning(message);

                    continue;
                }

                // Perform the insert.
                insert.Parameters["@StagingSku"].Value = sku;
                insert.Parameters["@Vend_Markdown"].Value = vend_Markdown;
                insert.Parameters["@Comm_Markdown"].Value = comm_Markdown;
                insert.Parameters["@Sale_Markdown"].Value = sale_Markdown;
                insert.Parameters["@StagingStartDate"].Value =
                    StagingUtilities.GetUtcDate(beginDate, _backendDateFormat);
                insert.Parameters["@StagingEndDate"].Value =
                    StagingUtilities.GetUtcDate(endDate, _backendDateFormat);

                insert.ExecuteNonQuery();
            }

            return;
        }

        private static void PrepStagingInsert(SqlCommand command)
        {
            command.CommandText =
                "INSERT INTO " + StagingDbConstants.ScandownTable +
                " (" +
                    StagingDbConstants.ScandownSku + ", " + "[Vend_Markdown], [Comm_Markdown] ,[Sale_Markdown],"
                    + StagingDbConstants.ScandownStartDate + ", " + StagingDbConstants.ScandownEndDate +
                ")" +
                " VALUES" +
                " (" +
                    "@StagingSku, @Vend_Markdown, @Comm_Markdown, @Sale_Markdown, @StagingStartDate, @StagingEndDate" +
                ")";
            command.Parameters.Add("@StagingSku", SqlDbType.NVarChar);
            command.Parameters.Add("@Vend_Markdown", SqlDbType.Decimal);
            command.Parameters.Add("@Comm_Markdown", SqlDbType.Decimal);
            command.Parameters.Add("@Sale_Markdown", SqlDbType.Decimal);
            command.Parameters.Add("@StagingStartDate", SqlDbType.Date);
            command.Parameters.Add("@StagingEndDate", SqlDbType.Date);

            return;
        }

        private static void PrepBackendSelect(IDbCommand command)
        {
            command.CommandText =
                "SELECT " +
                    " Inv." + BackendDbConstants.InvProductType +
                    ", Inv." + BackendDbConstants.InvModel +
                    ", Sd.VEND_MARKDOWN_AMT" +
                    ", Sd.COMM_MARKDOWN_AMT" +
                    ", Sd.SALE_MARKDOWN_AMT" +
                    ", Sd." + BackendDbConstants.ScandownItemNumber +
                    ", Sd." + BackendDbConstants.ScandownBeginDate +
                    ", Sd." + BackendDbConstants.ScandownEndDate +
                " FROM " + BackendDbConstants.ScandownTable + " Sd" +
                    " LEFT JOIN " + BackendDbConstants.InvTable + " Inv ON Sd." + BackendDbConstants.ScandownItemNumber + " = Inv." + BackendDbConstants.InvItemNumber +
                " WHERE" +
                    " (" +
                        " Sd." + BackendDbConstants.ScandownEndDate + " >= " + "'" + DateTime.Now/*.AddMonths(-1)*/.ToString(_backendDateFormat) + "'" +
                        " AND " + " Sd." + BackendDbConstants.ScandownBeginDate + " <= " + "'" + DateTime.Now/*.AddMonths(-1)*/.ToString(_backendDateFormat) + "'" +
                        " OR" + " Sd." + BackendDbConstants.ScandownEndDate + " IS NULL" +
                    ")" +
                    " AND" +
                    " (" +
                        " Inv." + BackendDbConstants.InvProductType + " IN (" + BackendDbConstants.InvAlwaysAllowProdTypes + ") OR" +
                        " (" +
                            "Inv." + BackendDbConstants.InvWebEnable + " IN (" + BackendDbConstants.InvWebEnableYes + ") AND" +
                            " (" +
                                "Inv." + BackendDbConstants.InvStockFlag + " IN (" + BackendDbConstants.InvAllowedStockCodes + ") OR" +
                                " Inv." + BackendDbConstants.InvStatusCode + " IN (" + BackendDbConstants.InvAllowedStatusCodes + ")" +
                            ")" +
                        ")" +
                    ")" +
                    " AND" +
                    " (" +
                        "Sd.SALE_MARKDOWN_AMT <> 0" +
                    ")";
            return;
        }
    }
}