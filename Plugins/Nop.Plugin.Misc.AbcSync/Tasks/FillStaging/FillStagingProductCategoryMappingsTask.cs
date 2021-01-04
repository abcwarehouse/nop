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
    class FillStagingProductCategoryMappingsTask : IScheduleTask
    {
        private readonly ILogger _logger;
        private readonly ImportSettings _importSettings;
        private readonly CoreSettings _coreSettings;

        public FillStagingProductCategoryMappingsTask(
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
            if (_importSettings.SkipFillStagingProductCategoryMappingsTask)
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
            StagingUtilities.PrepStagingDb(stagingActions, StagingDbConstants.ProdCatMappingTable);
            PrepStagingInsert(stagingActions);

            IDbCommand backendActions = backendConn.CreateCommand();
            backendConn.Open();

            // Prepare the backend select statement and run the data reader.
            PrepBackendSelect(backendActions);
            using (IDataReader prodCatMapping = backendActions.ExecuteReader())
            {
                ImportProdCatMapping(
                    prodCatMapping, stagedItemNums, stagingActions, logger);
            }

            return;
        }

        private static void ImportProdCatMapping(
            IDataReader prodCatMapping, HashSet<string> stagedItemNums,
            SqlCommand insert, ILogger logger)
        {
            while (prodCatMapping.Read())
            {
                // Map the information to C# variables.
                string itemNum = prodCatMapping[BackendDbConstants.InvItemNumber] as string;
                string prodType = prodCatMapping[BackendDbConstants.InvProductType] as string;
                string desc = prodCatMapping[BackendDbConstants.InvDescription] as string;
                string mattCodeList = prodCatMapping[BackendDbConstants.InvSecondDesc] as string;
                string modelId = prodCatMapping[BackendDbConstants.InvModel] as string;
                string catId = prodCatMapping[BackendDbConstants.CategoryId] as string;
                string mattCode = prodCatMapping[BackendDbConstants.CategoryMattressCode] as string;
                string brand = prodCatMapping[BackendDbConstants.InvBrand] as string;

                string sku = StagingUtilities.CalculateSku(modelId, prodType, itemNum);

                // First, skip any item numbers that were not staged.
                // This is not an error here, but rather in product staging.
                if (!stagedItemNums.Contains(itemNum))
                {
                    continue;
                }

                // Both identifiers must exist for there to be a proper mapping.
                // Ignore the selected entry if one or both do not exist.
                if (string.IsNullOrWhiteSpace(catId) || string.IsNullOrWhiteSpace(sku))
                {
                    continue;
                }

                int catId_int;
                if (!int.TryParse(catId, out catId_int))
                {
                    continue;
                }

                //be sure matt code list is valid if it is going to be used
                if ((IsSizeCategory(catId_int) || IsBrandCategory(catId_int) || IsComfortCategory(catId_int) || IsMaterialTypeCategory(catId_int)) &&
                     (string.IsNullOrEmpty(mattCodeList) || mattCodeList.Length < 4))
                {
                    continue;
                }

                //skipping imports based on mattress category restrictions
                if (IsSizeCategory(catId_int) && (mattCodeList.Substring(0, 1) != mattCode))
                {
                    continue;
                }
                if (IsBrandCategory(catId_int) && (mattCodeList.Substring(1, 1) != mattCode))
                {
                    continue;
                }
                if (IsComfortCategory(catId_int) && (mattCodeList.Substring(2, 1) != mattCode))
                {
                    continue;
                }
                if (IsMaterialTypeCategory(catId_int) && (mattCodeList.Substring(3, 1) != mattCode))
                {
                    continue;
                }

                //only outdoor tv category
                if (catId_int == 254 || catId_int == 255)
                {
                    if (brand != "SBT")
                    {
                        continue;
                    }
                }

                if (catId_int == 288 || catId_int == 294)
                {
                    if (string.IsNullOrEmpty(desc))
                    {
                        continue;
                    }
                    if (!(desc.Contains("UHD") || desc.Contains("4K")))
                    {
                        continue;
                    }
                }

                if (catId_int == 987)
                {
                    if (string.IsNullOrEmpty(desc))
                    {
                        continue;
                    }
                    if (!(desc.Contains("8K")))
                    {
                        continue;
                    }
                }

                // Perform the insert;
                insert.Parameters["@StagingItemSku"].Value = sku;
                insert.Parameters["@StagingCatId"].Value = catId;

                insert.ExecuteNonQuery();
            }

            return;
        }

        private static bool IsMaterialTypeCategory(int catId_int)
        {
            return (catId_int >= 939 && catId_int <= 943);
        }

        private static bool IsComfortCategory(int catId_int)
        {
            return (catId_int >= 932 && catId_int <= 936);
        }

        private static bool IsSizeCategory(int catId_int)
        {
            return (catId_int >= 916 && catId_int <= 921);
        }

        private static bool IsBrandCategory(int catId_int)
        {
            return (catId_int >= 923 && catId_int <= 930) ||
                      (catId_int >= 975 && catId_int <= 976) ||
                      (catId_int >= 978 && catId_int <= 982) ||
                      (catId_int >= 984 && catId_int <= 986) ||
                      (catId_int >= 988 && catId_int <= 989) ||
                      (catId_int >= 992 && catId_int <= 993);
        }

        private static void PrepStagingInsert(SqlCommand command)
        {
            command.CommandText =
                "INSERT INTO " + StagingDbConstants.ProdCatMappingTable +
                " (" +
                    StagingDbConstants.ProdCatMappingItemSku + ", " + StagingDbConstants.ProdCatMappingCategoryAbcId +
                ")" +
                " VALUES" +
                " (" +
                    "@StagingItemSku, @StagingCatId" +
                ")";
            command.Parameters.Add("@StagingItemSku", SqlDbType.NVarChar);
            command.Parameters.Add("@StagingCatId", SqlDbType.NVarChar);

            return;
        }

        /// <summary>
        ///		This fills in the CommandText for the provided Command
        ///		so that it will be able to select all needed information
        ///		regarding product-category mapping from the backend database.
        /// </summary>
        /// <param name="command">
        ///		The Command that will be used to select from the backend.
        /// </param>
        private static void PrepBackendSelect(IDbCommand command)
        {
            command.CommandText =
                "SELECT DISTINCT" +
                    " Inv." + BackendDbConstants.InvItemNumber +
                    ", Inv." + BackendDbConstants.InvProductType +
                    ", Inv." + BackendDbConstants.InvDescription +
                    ", Inv." + BackendDbConstants.InvBrand +
                    ", Inv." + BackendDbConstants.InvSecondDesc +
                    ", Inv." + BackendDbConstants.InvModel +
                    ", Cat." + BackendDbConstants.CategoryId +
                    ", Cat." + BackendDbConstants.CategoryMattressCode +
                " FROM " + BackendDbConstants.InvTable + " Inv" +
                    " LEFT JOIN " + BackendDbConstants.CategoryTable + " Cat ON Inv." + BackendDbConstants.InvProductType + " = Cat." + BackendDbConstants.CategoryProductType +
                " WHERE" +
                    " Inv." + BackendDbConstants.InvProductType + " IN (" + BackendDbConstants.InvAlwaysAllowProdTypes + ") OR" +
                    " (" +
                        "Inv." + BackendDbConstants.InvWebEnable + " IN (" + BackendDbConstants.InvWebEnableYes + ") AND" +
                        " (" +
                            "Inv." + BackendDbConstants.InvStockFlag + " IN (" + BackendDbConstants.InvAllowedStockCodes + ") OR" +
                            " Inv." + BackendDbConstants.InvStatusCode + " IN (" + BackendDbConstants.InvAllowedStatusCodes + ")" +
                        ")" +
                    ")";
            return;
        }
    }
}