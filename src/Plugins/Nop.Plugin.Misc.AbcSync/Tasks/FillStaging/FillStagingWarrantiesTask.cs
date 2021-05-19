using Nop.Plugin.Misc.AbcSync.Staging;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Extensions;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    public class FillStagingWarrantiesTask : IScheduleTask
    {
        private readonly ILogger _logger;
        private readonly ImportSettings _importSettings;
        private readonly CoreSettings _coreSettings;

        public FillStagingWarrantiesTask(
            ILogger logger,
            ImportSettings importSettings,
            CoreSettings coreSettings
        )
        {
            _logger = logger;
            _importSettings = importSettings;
            _coreSettings = coreSettings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipFillStagingWarrantiesTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();
            using (SqlConnection stagingConn = _importSettings.GetStagingDbConnection() as SqlConnection)
            {
                using (IDbConnection backendConn = _coreSettings.GetBackendDbConnection())
                {
                    stagingConn.Open();
                    backendConn.Open();

                    await ImportAsync(stagingConn,
                        backendConn, _logger);
                }
            }
            this.LogEnd();
        }

        private static async System.Threading.Tasks.Task ImportAsync(SqlConnection stagingConn,
            IDbConnection backendConn, ILogger logger)
        {
            await ImportWarrantyGroupsAsync(stagingConn, backendConn, logger);
            await ImportWarrantyItemsAsync(stagingConn, backendConn, logger);
            await ImportProductWarrantyMappingsAsync(stagingConn, backendConn, logger);
        }

        private static async System.Threading.Tasks.Task ImportWarrantyGroupsAsync(SqlConnection stagingConn,
            IDbConnection backendConn, ILogger logger)
        {
            SqlCommand stagingActions = stagingConn.CreateCommand();
            IDbCommand backendActions = backendConn.CreateCommand();

            StagingUtilities.PrepStagingDb(stagingActions, StagingDbConstants.WarrantyGroupTable);

            PrepBackendWarrantyGroupSelect(backendActions, backendConn is SqlConnection);
            PrepStagingWarrantyGroupInsert(stagingActions);

            using (IDataReader attrReader = backendActions.ExecuteReader())
            {
                await ImportWarrantyGroupHelperAsync(attrReader, stagingActions, logger);
            }
        }

        private static async System.Threading.Tasks.Task ImportWarrantyItemsAsync(SqlConnection stagingConn,
            IDbConnection backendConn, ILogger logger)
        {
            SqlCommand stagingActions = stagingConn.CreateCommand();
            IDbCommand backendActions = backendConn.CreateCommand();

            StagingUtilities.PrepStagingDb(stagingActions, StagingDbConstants.WarrantyItemTable);

            PrepbackendWarrantyItemsSelect(backendActions);
            PrepStagingWarrantyItemsInsert(stagingActions);

            using (IDataReader attrValReader = backendActions.ExecuteReader())
            {
                await ImportWarrantyItemsHelperAsync(attrValReader, stagingActions, logger);
            }

        }

        private static async System.Threading.Tasks.Task ImportProductWarrantyMappingsAsync(
            SqlConnection stagingConn, IDbConnection backendConn, ILogger logger)
        {
            SqlCommand stagingActions = stagingConn.CreateCommand();
            IDbCommand backendActions = backendConn.CreateCommand();

            // Find all the staged item numbers.
            HashSet<string> stagedItemNums =
                StagingUtilities.GetStagedItemNumbers(stagingActions);

            StagingUtilities.PrepStagingDb(stagingActions, StagingDbConstants.ProductWarrantyGroupMappingTable);

            PrepBackendProdWarrMapSelect(backendActions);
            PrepStagingProdWarrMapInsert(stagingActions);

            using (IDataReader prodWarMapReader = backendActions.ExecuteReader())
            {
                await ImportProdWarrantyMappingsHelperAsync(
                    prodWarMapReader, stagedItemNums, stagingActions, logger);
            }
        }

        private static async System.Threading.Tasks.Task ImportWarrantyGroupHelperAsync(IDataReader attrReader,
            SqlCommand insert, ILogger logger)
        {
            while (attrReader.Read())
            {
                string warrCode = attrReader[BackendDbConstants.WarrantyCode] as string;
                string warrName = attrReader[BackendDbConstants.WarrantyDescription] as string;

                if (string.IsNullOrWhiteSpace(warrCode))
                {
                    string message = "Unable to import a warranty group." +
                        " The warranty group code is empty.";
                    await logger.WarningAsync(message);

                    continue;
                }
                if (string.IsNullOrWhiteSpace(warrName))
                {
                    string message = "Unablet o import the warranty group" +
                        " with code " + warrCode + "." +
                        " The warranty group does not have a name.";
                    await logger.WarningAsync(message);

                    continue;
                }

                insert.Parameters["@SqlWarrCode"].Value = warrCode;
                insert.Parameters["@SqlWarrName"].Value = warrName;
                insert.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Read values from reader into staging databse for product attribute values
        /// </summary>
        /// <param name="attrValReader">Reader connected to ISAM</param>
        /// <param name="insert">Sql command to be executed for insert</param>
        private static async System.Threading.Tasks.Task ImportWarrantyItemsHelperAsync(
            IDataReader attrValReader, SqlCommand insert, ILogger logger)
        {
            while (attrValReader.Read())
            {
                string name = attrValReader[BackendDbConstants.InvDescription] as string;
                decimal? price = attrValReader[BackendDbConstants.InvSellPrice] as decimal?;
                string warrGroup = attrValReader[BackendDbConstants.WarrantyCode] as string;
                string warrItemNum = attrValReader[BackendDbConstants.InvItemNumber] as string;

                if (string.IsNullOrWhiteSpace(warrGroup))
                {
                    string message = "Unable to import a warranty item." +
                        " A warranty has no group code.";
                    await logger.WarningAsync(message);

                    continue;
                }
                if (string.IsNullOrWhiteSpace(warrItemNum))
                {
                    continue;
                }
                if (string.IsNullOrWhiteSpace(name))
                {
                    string message = "Unable to import the warranty item" +
                        " with UPC " + warrItemNum + "." +
                        " The item has no name/description.";
                    await logger.WarningAsync(message);

                    continue;
                }
                if ((price == null) || (price < 0))
                {
                    string message = "Unable to import the warranty item" +
                        " with UPC " + warrItemNum + "." +
                        " Its price is empty or negative.";
                    await logger.WarningAsync(message);

                    continue;
                }

                insert.Parameters["@SqlWarrItemName"].Value = name;
                insert.Parameters["@SqlWarrItemPrice"].Value = price;
                insert.Parameters["@SqlWarrItemGroup"].Value = warrGroup;
                insert.Parameters["@SqlWarrItemNumber"].Value = warrItemNum;
                insert.ExecuteNonQuery();
            }
        }

        private static async System.Threading.Tasks.Task ImportProdWarrantyMappingsHelperAsync(
            IDataReader prodWarMapReader, HashSet<string> stagedItemNums,
            SqlCommand insert, ILogger logger)
        {
            while (prodWarMapReader.Read())
            {
                string warrCode = prodWarMapReader[BackendDbConstants.InvWarrantyCode] as string;
                string itemNum = prodWarMapReader[BackendDbConstants.InvItemNumber] as string;
                string prodType = prodWarMapReader[BackendDbConstants.InvProductType] as string;
                string modelId = prodWarMapReader[BackendDbConstants.InvModel] as string;

                string prodSku = StagingUtilities.CalculateSku(modelId, prodType, itemNum);

                // First, skip any item numbers that were not staged.
                // This is not an error here, but rather in product staging.
                if (!stagedItemNums.Contains(itemNum))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(warrCode))
                {
                    string message = "Unable to import a warranty-product mapping." +
                        " The warranty does not have a warranty code.";
                    await logger.WarningAsync(message);

                    continue;
                }
                if (string.IsNullOrWhiteSpace(prodSku))
                {
                    string message = "Unable to import a warranty-product mapping" +
                        " for the warranty group code " + warrCode + "." +
                        " The product does not have a UPC.";
                    await logger.WarningAsync(message);

                    continue;
                }

                insert.Parameters["@SqlProdWarrWarrCode"].Value = warrCode;
                insert.Parameters["@SqlProdWarrProdSku"].Value = prodSku;
                insert.ExecuteNonQuery();
            }
        }

        private static void PrepStagingWarrantyGroupInsert(SqlCommand command)
        {
            command.CommandText = "INSERT INTO " + StagingDbConstants.WarrantyGroupTable + "("
                                                 + StagingDbConstants.WarrantyGroupWarrantyGroupCode +
                                                 ", " + StagingDbConstants.WarrantyGroupName +
                                    ") VALUES (@SqlWarrCode, @SqlWarrName)";

            command.Parameters.Add("@SqlWarrCode", SqlDbType.NVarChar);
            command.Parameters.Add("@SqlWarrName", SqlDbType.NVarChar);
        }

        /// <summary>
        /// Fill CommandText of Sql command with insert statement needed for product attribute value
        /// </summary>
        /// <param name="command">Sql command</param>
        private static void PrepStagingWarrantyItemsInsert(SqlCommand command)
        {
            command.CommandText = "INSERT INTO " + StagingDbConstants.WarrantyItemTable + "(" +
                                                   StagingDbConstants.WarrantyItemName + ", " +
                                                   StagingDbConstants.WarrantyItemPriceAdjustment + ", " +
                                                   StagingDbConstants.WarrantyGroupWarrantyGroupCode + ", " +
                                                   StagingDbConstants.WarrantyItemWarrantySku +
                                    ") VALUES (@SqlWarrItemName, @SqlWarrItemPrice, @SqlWarrItemGroup" +
                                    ", @SqlWarrItemNumber)";

            command.Parameters.Add("@SqlWarrItemName", SqlDbType.NVarChar);
            command.Parameters.Add("@SqlWarrItemPrice", SqlDbType.Decimal);
            command.Parameters.Add("@SqlWarrItemGroup", SqlDbType.NVarChar);
            command.Parameters.Add("@SqlWarrItemNumber", SqlDbType.NVarChar);
        }

        private static void PrepStagingProdWarrMapInsert(SqlCommand command)
        {
            command.CommandText = "INSERT INTO " + StagingDbConstants.ProductWarrantyGroupMappingTable + "(" +
                                                   StagingDbConstants.ProductWarrantyGroupWarrantyGroupCode + ", " +
                                                   StagingDbConstants.ProductWarrantyGroupProductSku +
                                                   ") VALUES (@SqlProdWarrWarrCode, @SqlProdWarrProdSku)";

            command.Parameters.Add("@SqlProdWarrWarrCode", SqlDbType.NVarChar);
            command.Parameters.Add("@SqlProdWarrProdSku", SqlDbType.NVarChar);
        }

        /// <summary>
        /// Fill CommandText of Db command with select statement needed for product attribute 
        /// </summary>
        /// <param name="command">Db command</param>
        private static void PrepBackendWarrantyGroupSelect(
            IDbCommand command, bool isSqlServerConn)
        {
            string backendWarrDesc = BackendDbConstants.WarrantyDescription;
            if (isSqlServerConn)
            {
                backendWarrDesc = "[" + backendWarrDesc + "]";
            }
            else
            {
                backendWarrDesc = '"' + BackendDbConstants.WarrantyDescription + '"';
            }

            command.CommandText = "SELECT DISTINCT " + BackendDbConstants.WarrantyCode + ", " +
                                                        backendWarrDesc +
                                    " FROM " + BackendDbConstants.WarrantyTable;
        }


        /// <summary>
        /// Fill CommandText of Db command with select statement needed for product attribute value
        /// </summary>
        /// <param name="command">Db command</param>
        private static void PrepbackendWarrantyItemsSelect(IDbCommand command)
        {
            command.CommandText = "SELECT warranty." + BackendDbConstants.InvDescription +
                                    ", " + "warranty." + BackendDbConstants.InvSellPrice +
                                    ", " + "wid." + BackendDbConstants.WarrantyCode +
                                    ", " + "warranty." + BackendDbConstants.InvItemNumber +
                                    " FROM " + BackendDbConstants.WarrantyTable + " wid" +
                                    " LEFT JOIN " + BackendDbConstants.InvTable + " warranty ON warranty.ITEM_NUMBER = wid.KEY_WARR_ITEM";
        }

        private static void PrepBackendProdWarrMapSelect(IDbCommand command)
        {
            command.CommandText = "SELECT prod." + BackendDbConstants.InvWarrantyCode +
                                    ", prod." + BackendDbConstants.InvItemNumber +
                                    ", prod." + BackendDbConstants.InvProductType +
                                    ", prod." + BackendDbConstants.InvModel +
                                    " FROM " + BackendDbConstants.InvTable + " prod " +
                                    //filters out products with NULL/empty string/whitespace for WARRANTY_CODE
                                    " INNER JOIN " + BackendDbConstants.WarrantyTable + " warr ON warr.KEY_WARR_ID = prod.WARRANTY_CODE";
        }
    }
}