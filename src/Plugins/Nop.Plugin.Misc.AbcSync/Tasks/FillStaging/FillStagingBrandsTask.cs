using System.Data.SqlClient;
using System.Data;
using Nop.Services.Logging;
using System.Collections.Generic;
using Nop.Plugin.Misc.AbcSync.Staging;
using Nop.Core;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Extensions;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    public class FillStagingBrandsTask : IScheduleTask
    {
        private readonly ILogger _logger;
        private readonly ImportSettings _importSettings;
        private readonly CoreSettings _coreSettings;

        public FillStagingBrandsTask(
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
            if (_importSettings.SkipFillStagingBrandsTask)
            {
                this.Skipped();
                return;
            }

            
            using (SqlConnection stagingConn = _importSettings.GetStagingDbConnection() as SqlConnection)
            {
                using (IDbConnection backendConn = _coreSettings.GetBackendDbConnection())
                {
                    await ImportBrandsAsync(stagingConn, backendConn, _logger);
                }
            }
            
        }

        private async System.Threading.Tasks.Task ImportBrandsAsync(
            SqlConnection stagingConn, IDbConnection backendConn,
            ILogger logger)
        {
            SqlCommand stagingManActions = stagingConn.CreateCommand();
            SqlCommand stagingMappingActions = stagingConn.CreateCommand();
            stagingConn.Open();

            // Prepare both tables in the database for staging.
            StagingUtilities.PrepStagingDb(
                stagingManActions, StagingDbConstants.ManufacturerTable);
            StagingUtilities.PrepStagingDb(
                stagingMappingActions, StagingDbConstants.MappingTable);

            // Find all the staged item numbers.
            HashSet<string> stagedItemNums =
                StagingUtilities.GetStagedItemNumbers(stagingMappingActions);

            // Prepare both insert statements.
            PrepStagingManufacturerInsert(stagingManActions);
            PrepStagingMappingInsert(stagingMappingActions);

            IDbCommand backendActions = backendConn.CreateCommand();
            backendConn.Open();

            // Prepare the backend select statement and run the data reader.
            PrepBackendSelect(backendActions);
            using (IDataReader brand = backendActions.ExecuteReader())
            {
                await ImportBrandAsync(brand, stagedItemNums,
                    stagingManActions, stagingMappingActions, logger);
            }
        }

        private async System.Threading.Tasks.Task ImportBrandAsync(
            IDataReader brandReader, HashSet<string> stagedItemNums,
            SqlCommand manInsert, SqlCommand mappingInsert, ILogger logger)
        {
            // These need to be stored so that I can write the brand data
            // after finishing the loop through the DataReader.
            Dictionary<string, BrandData> brandDict = new Dictionary<string, BrandData>();

            // We need to know if there's a brand that we are skipping.
            HashSet<string> skippedBrandCodes = new HashSet<string>();

            while (brandReader.Read())
            {
                // Store the data into the brand class for easy access.
                BackendBrand brand = new BackendBrand(brandReader);

                // First, skip any item numbers that were not staged.
                // This is not an error here, but rather in product staging.
                if (!stagedItemNums.Contains(brand.ItemNumber))
                {
                    continue;
                }

                // If the current read-in brand is the same as one marked to skip,
                // simply continue and skip it. No logging, that has been done.
                if (skippedBrandCodes.Contains(brand.BrandCode))
                {
                    continue;
                }

                // Without certain key values, the mapping cannot take place.
                // This entry is invalid if these keys do not exist.
                string brandCodeToSkip;
                if (!brand.HasKeyValues(logger, out brandCodeToSkip))
                {
                    // If we're skipping a brand, we've already logged
                    // it's brand-wide error, so ignore it for import.
                    if (brandCodeToSkip != null)
                    {
                        skippedBrandCodes.Add(brandCodeToSkip);
                    }
                    continue;
                }

                // Add the brand for insert later.
                if (brandDict.ContainsKey(brand.BrandCode))
                {
                    // Update the stored brand data.
                    brandDict[brand.BrandCode] =
                        brand.UpdateBrandData(brandDict[brand.BrandCode]);
                }
                else
                {
                    BrandData newData = new BrandData();
                    newData.brandCode = null;
                    newData.brandName = null;
                    newData.onAbc = false;
                    newData.onClearance = false;
                    newData.onHawthorne = false;
                    newData.onHawthorneClearance = false;

                    brandDict.Add(brand.BrandCode, brand.UpdateBrandData(newData));
                }

                // Insert the mapping.
                await brand.InsertProdBrandMappingAsync(mappingInsert, logger);
            }

            // Track to see if any brand has been imported.
            bool didImport = false;

            // And finally import the brands.
            foreach (var data in brandDict.Values)
            {
                bool currImport = await InsertBrandAsync(manInsert, data, logger);
                didImport = didImport ? didImport : currImport;
            }

            // Check to see if any imports were made.
            // Error if not.
            if (!didImport)
            {
                string message = "No brands were able to be imported." +
                    " Ensure that the mappings in the web.config are correct" +
                    " and that the backend datasource contains correct data.";
                throw new NopException(message);
            }

            return;
        }

        private static async Task<bool> InsertBrandAsync(
            SqlCommand insert, BrandData data, ILogger logger)
        {
            if (data.brandCode == null)
            {
                return false;
            }

            // If the brand wouldn't exist on any store, don't insert it.
            if (!data.onAbc && !data.onHawthorne && !data.onClearance
                 && !data.onHawthorneClearance)
            {
                string message = "Unable to import this brand." +
                    " The brand with code " + data.brandCode +
                    " does not have any products on" +
                    " the ABC online store, the Hawthorne online store," +
                    " or the ABC Clearance store.";
                await logger.WarningAsync(message);

                return false;
            }

            insert.Parameters["@StagingManCode"].Value = data.brandCode;
            insert.Parameters["@StagingManName"].Value = data.brandName;
            insert.Parameters["@StagingManOnAbc"].Value = data.onAbc;
            insert.Parameters["@StagingManOnHawthorne"].Value = data.onHawthorne;
            insert.Parameters["@StagingManOnClearance"].Value = data.onClearance;
            insert.Parameters["@StagingManOnHawClearance"].Value = data.onHawthorneClearance;

            insert.ExecuteNonQuery();
            return true;
        }

        private static void PrepStagingManufacturerInsert(SqlCommand command)
        {
            command.CommandText =
                "INSERT INTO " + StagingDbConstants.ManufacturerTable +
                " (" +
                    StagingDbConstants.ManufacturerCode + ", " + StagingDbConstants.ManufacturerName +
                    ", " + StagingDbConstants.ManufacturerOnAbc + ", " + StagingDbConstants.ManufacturerOnHawthorne +
                    ", " + StagingDbConstants.ManufacturerOnClearance + ", OnHawthorneClearanceSite" + 
                ")" +
                " VALUES" +
                " (" +
                    "@StagingManCode, @StagingManName" +
                    ", @StagingManOnAbc, @StagingManOnHawthorne" +
                    ", @StagingManOnClearance, @StagingManOnHawClearance" +
                ")";
            command.Parameters.Add("@StagingManCode", SqlDbType.NVarChar);
            command.Parameters.Add("@StagingManName", SqlDbType.NVarChar);
            command.Parameters.Add("@StagingManOnAbc", SqlDbType.Bit);
            command.Parameters.Add("@StagingManOnHawthorne", SqlDbType.Bit);
            command.Parameters.Add("@StagingManOnClearance", SqlDbType.Bit);
            command.Parameters.Add("@StagingManOnHawClearance", SqlDbType.Bit);

            return;
        }

        private static void PrepStagingMappingInsert(SqlCommand command)
        {
            command.CommandText =
                "INSERT INTO " + StagingDbConstants.MappingTable +
                " (" +
                    StagingDbConstants.MappingItemSku + ", " + StagingDbConstants.MappingBrand +
                ")" +
                " VALUES" +
                " (" +
                    "@StagingMappingItemSku, @StagingMappingBrand" +
                ")";
            command.Parameters.Add("@StagingMappingItemSku", SqlDbType.NVarChar);
            command.Parameters.Add("@StagingMappingBrand", SqlDbType.NVarChar);

            return;
        }

        private static void PrepBackendSelect(IDbCommand command)
        {
            command.CommandText =
                "SELECT DISTINCT" +
                    " Inv." + BackendDbConstants.InvItemNumber +
                    ", Inv." + BackendDbConstants.InvProductType +
                    ", Inv." + BackendDbConstants.InvStatusCode +
                    ", Inv." + BackendDbConstants.InvDist +
                    ", Inv." + BackendDbConstants.InvModel +
                    ", Br." + BackendDbConstants.BrandCode +
                    ", Br." + BackendDbConstants.BrandName +
                " FROM " + BackendDbConstants.BrandTable + " Br" +
                    " LEFT JOIN " + BackendDbConstants.DataTable + " Data ON Br." + BackendDbConstants.BrandCode + " = Data." + BackendDbConstants.DataBrand +
                    " LEFT JOIN " + BackendDbConstants.InvTable + " Inv ON Data." + BackendDbConstants.DataItemNumber + " = Inv." + BackendDbConstants.InvItemNumber +
                " WHERE " +
                    " Inv." + BackendDbConstants.InvItemNumber + " IS NULL OR" + // Grab this error case for logging.
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