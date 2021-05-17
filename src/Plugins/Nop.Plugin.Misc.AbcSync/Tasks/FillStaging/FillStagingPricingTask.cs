using System.Data.SqlClient;
using System.Data;
using Nop.Plugin.Misc.AbcSync.Staging;
using Nop.Services.Logging;
using System.Collections.Generic;
using System;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Extensions;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    class FillStagingPricingTask : IScheduleTask
    {
        private const string _backendDateFormat = "yyMMdd";

        private readonly ILogger _logger;
        private readonly ImportSettings _importSettings;
        private readonly CoreSettings _coreSettings;

        public FillStagingPricingTask(
            ILogger logger,
            ImportSettings importSettings,
            CoreSettings coreSettings
        )
        {
            _logger = logger;
            _importSettings = importSettings;
            _coreSettings = coreSettings;
        }

        public System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipFillStagingPricingTask)
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

        public void Import(
            SqlConnection stagingConn, IDbConnection backendConn,
            ILogger logger)
        {
            SqlCommand stagingPriceActions = stagingConn.CreateCommand();
            SqlCommand stagingPrFileActions = stagingConn.CreateCommand();
            stagingConn.Open();

            // Find all the staged item numbers.
            HashSet<string> stagedItemNums =
                StagingUtilities.GetStagedItemNumbers(stagingPriceActions);

            // Prepare the database for staging.
            // Prepare the insert and update commands.
            StagingUtilities.PrepStagingDb(stagingPrFileActions, StagingDbConstants.PrFileDiscountsTable);
            PrepStagingPriceUpdate(stagingPriceActions);
            PrepStagingPrFileInsert(stagingPrFileActions);

            IDbCommand backendActions = backendConn.CreateCommand();
            backendConn.Open();

            // Get a list of all item numbers that are in the "snap" table.
            HashSet<string> snapList =
                StagingUtilities.GetSnapList(backendActions);

            // Prepare the backend select statement and run the DataReader.
            PrepBackendSelect(backendActions);
            using (IDataReader productPriceReader = backendActions.ExecuteReader())
            {
                await ImportProductPriceAsync(productPriceReader, snapList, stagedItemNums,
                    stagingPriceActions, stagingPrFileActions, logger);
            }

            return;
        }

        private static void PrepBackendSelect(IDbCommand command)
        {
            command.CommandText =
                "SELECT DISTINCT" +
                    " Inv." + BackendDbConstants.InvItemNumber +
                    ", Inv." + BackendDbConstants.InvSaleUnit +
                    ", Inv." + BackendDbConstants.InvUnitCost +
                    ", Inv." + BackendDbConstants.InvProductType +
                    ", Inv." + BackendDbConstants.InvDept +
                    ", Inv." + BackendDbConstants.InvSellPrice +
                    ", Inv." + BackendDbConstants.InvListPrice +
                    ", Inv." + BackendDbConstants.InvStockFlag +
                    ", Inv." + BackendDbConstants.InvWebPrice +
                    ", Inv." + BackendDbConstants.InvStatusCode +
                    ", Inv." + BackendDbConstants.InvDist +
                    ", Inv." + BackendDbConstants.InvPriceCode +
                    ", Inv." + BackendDbConstants.InvModel +
                    ", Data." + BackendDbConstants.DataCartPrice +
                    ", Pr." + BackendDbConstants.PromoFileBranch +
                    ", Pr." + BackendDbConstants.PromoFileBeginDate +
                    ", Pr." + BackendDbConstants.PromoFileEndDate +
                    ", Pr." + BackendDbConstants.PromoFilePrice +
                    ", Pr." + BackendDbConstants.PromoFileName +
                    ", Pr." + BackendDbConstants.PromoFileAltFlag +
                " FROM " + BackendDbConstants.InvTable + " Inv" +
                    " LEFT JOIN " + BackendDbConstants.DataTable + " Data ON Inv." + BackendDbConstants.InvItemNumber + " = Data." + BackendDbConstants.DataItemNumber +
                    " LEFT JOIN " + BackendDbConstants.PromoFileTable + " Pr ON Inv." + BackendDbConstants.InvItemNumber + " = Pr." + BackendDbConstants.PromoFileItemNum +
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

        private static void PrepStagingPrFileInsert(SqlCommand command)
        {
            command.CommandText =
                "INSERT INTO " + StagingDbConstants.PrFileDiscountsTable +
                " (" +
                    StagingDbConstants.PrFileDiscountsSku +
                    ", " + StagingDbConstants.PrFileDiscountsOnAbc + ", " + StagingDbConstants.PrFileDiscountsOnHawthorne +
                    ", " + StagingDbConstants.PrFileDiscountsName + ", " + StagingDbConstants.PrFileDiscountsAmount +
                    ", " + StagingDbConstants.PrFileDiscountsStartDate + ", " + StagingDbConstants.PrFileDiscountsEndDate +
                ")" +
                " VALUES" +
                " (" +
                    "@StagingPrFileSku" +
                    ", @StagingPrFileOnAbc, @StagingPrFileOnHawthorne" +
                    ", @StagingPrFileName, @StagingPrFileAmount" +
                    ", @StagingPrFileStartDate, @StagingPrFileEndDate" +
                ")";
            command.Parameters.Add("@StagingPrFileSku", SqlDbType.NVarChar);
            command.Parameters.Add("@StagingPrFileOnAbc", SqlDbType.Bit);
            command.Parameters.Add("@StagingPrFileOnHawthorne", SqlDbType.Bit);
            command.Parameters.Add("@StagingPrFileName", SqlDbType.NVarChar);
            command.Parameters.Add("@StagingPrFileAmount", SqlDbType.Decimal);
            command.Parameters.Add("@StagingPrFileStartDate", SqlDbType.Date);
            command.Parameters.Add("@StagingPrFileEndDate", SqlDbType.Date);

            return;
        }

        private static void PrepStagingPriceUpdate(SqlCommand command)
        {
            command.CommandText =
                "UPDATE " + StagingDbConstants.ProductTable +
                " SET" +
                    " " + StagingDbConstants.ProductBasePrice + " = @StagingProductBasePrice" +
                    ", " + StagingDbConstants.ProductDisplayPrice + " = @StagingProductDisplayPrice" +
                    ", " + StagingDbConstants.ProductCartPrice + " = @StagingProductCartPrice" +
                    ", " + StagingDbConstants.ProductPairPricing + " = @StagingProductPairPricing" +
                    ", " + StagingDbConstants.ProductPriceBucket + " = @StagingProductPriceBucket" +
                " WHERE " + StagingDbConstants.ProductSku + " = @StagingProductSku";
            command.Parameters.Add("@StagingProductBasePrice", SqlDbType.Decimal);
            command.Parameters.Add("@StagingProductDisplayPrice", SqlDbType.Decimal);
            command.Parameters.Add("@StagingProductCartPrice", SqlDbType.Decimal);
            command.Parameters.Add("@StagingProductPairPricing", SqlDbType.Bit);
            command.Parameters.Add("@StagingProductPriceBucket", SqlDbType.Int);
            command.Parameters.Add("@StagingProductSku", SqlDbType.NVarChar);

            return;
        }

        private static async Task<bool> PrFilePriceIsValidAsync(BackendPrice prodPrice,
            decimal? price, string branch, ILogger logger)
        {
            // The amount is needed for it to be an actual discount.
            if ((price == null) || (price <= 0))
            {
                string message = "Unable to import the PrFile discount" +
                    " for model ID " + prodPrice.Sku +
                    " using branch " + branch + "." +
                    " The promotion price does not exist, is zero, or is negative.";
                await logger.WarningAsync(message);

                return false;
            }

            if (price > prodPrice.DisplayPrice) { return false; }

            if (prodPrice.PriceBucket.UsesMAPPricing()) { return false; }

            return true;
        }

        private static async System.Threading.Tasks.Task InsertPrFileDiscountAsync(
            IDataReader productPriceReader, BackendPrice prodPrice,
            SqlCommand insert, ILogger logger)
        {
            // Map the information to C# variables.
            string branch = productPriceReader[BackendDbConstants.PromoFileBranch] as string;
            string beginDate = productPriceReader[BackendDbConstants.PromoFileBeginDate] as string;
            string endDate = productPriceReader[BackendDbConstants.PromoFileEndDate] as string;
            decimal? price = productPriceReader[BackendDbConstants.PromoFilePrice] as decimal?;
            string name = productPriceReader[BackendDbConstants.PromoFileName] as string ?? string.Empty;
            string altFlag = productPriceReader[BackendDbConstants.PromoFileAltFlag] as string;

            // Valid PrFile discounts have a NULL alt flag.
            // We also want to limit it so that only "current" promos are added.
            if (altFlag != null)
            {
                return;
            }

            bool onAbc = (branch == "WEB");
            bool onHawthorne = (branch == "WEBH");

            // The promo needs to at least apply to one of the web stores.
            if (!onAbc && !onHawthorne)
            {
                return;
            }

            if (!await PrFilePriceIsValidAsync(prodPrice, price, branch, logger))
            {
                return;
            }

            // Perform the insert.
            insert.Parameters["@StagingPrFileSku"].Value = prodPrice.Sku;
            insert.Parameters["@StagingPrFileOnAbc"].Value = onAbc;
            insert.Parameters["@StagingPrFileOnHawthorne"].Value = onHawthorne;
            insert.Parameters["@StagingPrFileName"].Value = name;
            insert.Parameters["@StagingPrFileAmount"].Value = prodPrice.DisplayPrice - price;
            insert.Parameters["@StagingPrFileStartDate"].Value =
                StagingUtilities.GetUtcDate(beginDate, _backendDateFormat);
            insert.Parameters["@StagingPrFileEndDate"].Value =
                StagingUtilities.GetUtcDate(endDate, _backendDateFormat);

            insert.ExecuteNonQuery();
        }

        private static bool InsertBaseProductPrice(
            BackendPrice productPrice, SqlCommand update, ILogger logger)
        {
            if (productPrice == null)
            {
                return false;
            }

            // Key checks are done elsewhere.
            // Here, we need to see if it actually belongs on any store.
            if (productPrice.NotOnAnyStoreAsync(logger))
            {
                return false;
            }

            update.Parameters["@StagingProductBasePrice"].Value = productPrice.BasePrice;
            update.Parameters["@StagingProductDisplayPrice"].Value = productPrice.DisplayPrice;
            update.Parameters["@StagingProductCartPrice"].Value = productPrice.CartPrice;
            update.Parameters["@StagingProductPairPricing"].Value = productPrice.UsePairPricing;
            update.Parameters["@StagingProductPriceBucket"].Value = productPrice.PriceBucket;
            update.Parameters["@StagingProductSku"].Value = productPrice.Sku;

            update.ExecuteNonQuery();
            return true;
        }

        private static async System.Threading.Tasks.Task ImportProductPriceAsync(IDataReader productPriceReader,
            HashSet<string> snapList, HashSet<string> stagedItemNums,
            SqlCommand priceUpdate, SqlCommand prFileInsert, ILogger logger)
        {
            // To avoid duplicate inserts,
            // we need to keep track of products we've already imported.
            HashSet<string> importedProductSkus = new HashSet<string>();

            // We need to keep track of bad model IDs or item numbers
            // so that we skip them and do not double up on logs.
            HashSet<string> skusToSkip = new HashSet<string>();
            HashSet<string> itemNumsToSkip = new HashSet<string>();

            while (productPriceReader.Read())
            {
                BackendPrice productPrice =
                    new BackendPrice(productPriceReader, snapList);

                // First, skip any item numbers that were not staged.
                // This is not an error here, but rather in product staging.
                if (!stagedItemNums.Contains(productPrice.ItemNum))
                {
                    continue;
                }

                // Skip the item if needed, otherwise begin the import.
                if (skusToSkip.Contains(productPrice.Sku) ||
                    itemNumsToSkip.Contains(productPrice.ItemNum))
                {
                    continue;
                }

                // If key values are missing, then matching is not possible.
                // All key values are needed, if they are not present,
                // we cannot import anything for this product,
                // so the skip values are assigned.
                string skipItemNum;
                string skipSku;
                if (!productPrice.HasKeyValues(logger, out skipItemNum, out skipSku))
                {
                    if (skipItemNum != null)
                    {
                        itemNumsToSkip.Add(skipItemNum);
                    }
                    if (skipSku != null)
                    {
                        skusToSkip.Add(skipSku);
                    }

                    continue;
                }

                // Insert the price data as long as it's not a duplicate.
                if (!importedProductSkus.Contains(productPrice.Sku))
                {
                    bool currImport = InsertBaseProductPrice(
                        productPrice, priceUpdate, logger);
                }

                // Insert the sale data.
                await InsertPrFileDiscountAsync(
                    productPriceReader, productPrice, prFileInsert, logger);
            }
        }
    }
}