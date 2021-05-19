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
    public class FillStagingProductsTask : IScheduleTask
    {
        private readonly ILogger _logger;
        private readonly ImportSettings _importSettings;
        private readonly CoreSettings _coreSettings;

        public FillStagingProductsTask(
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
            if (_importSettings.SkipFillStagingProductsTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();
            await ImportProductsAsync();
            this.LogEnd();
        }

        private async System.Threading.Tasks.Task ImportProductsAsync()
        {
            var stagingConn = _importSettings.GetStagingDbConnection();
            SqlCommand stagingActions = stagingConn.CreateCommand();
            stagingConn.Open();

            // Prepare the database for staging.
            // Prepare the insert command.
            StagingUtilities.PrepStagingDb(stagingActions, "dbo.Product");
            PrepStagingInsert(stagingActions);

            var backendConn = _coreSettings.GetBackendDbConnection();
            IDbCommand backendActions = backendConn.CreateCommand();
            backendConn.Open();

            // Get a list of all item numbers that are in the "snap" table.
            // Then prepare the backend select statement.
            HashSet<string> snapList =
                StagingUtilities.GetSnapList(backendActions);
            PrepBackendSelect(backendActions);

            using (IDataReader productReader = backendActions.ExecuteReader())
            {
                await ImportProductAsync(productReader, snapList, stagingActions);
            }

            return;
        }

        private async System.Threading.Tasks.Task ImportProductAsync(
            IDataReader productReader,
            HashSet<string> snapList,
            SqlCommand stagingActions)
        {
            // These are needed to warn about duplicate model IDs
            // and to skip them properly.
            HashSet<string> stagedModels = new HashSet<string>();
            HashSet<string> modelsToSkip = new HashSet<string>();

            while (productReader.Read())
            {
                BackendProduct product =
                    new BackendProduct(productReader, snapList);

                // Skip certain model numbers.
                if (modelsToSkip.Contains(product.Sku))
                {
                    continue;
                }

                // If it something with this model ID has already been imported,
                // but it has not yet been flagged to skip, log it and skip it.
                if (stagedModels.Contains(product.Sku))
                {
                    modelsToSkip.Add(product.Sku);
                    continue;
                }

                // If key values are missing, then matching will be difficult.
                // All key values are needed, if they are not present,
                // we ignore the product.
                if (!(await product.HasKeyValuesAsync(_logger)))
                {
                    continue;
                }

                if (!product.OnAnyStore)
                {
                    string message = "Unable to import the product with model ID " +
                        product.Sku + ". It does not belong on" +
                        " the ABC online store, the Hawthorne online store," +
                        "  the ABC clearance store, or the Hawthorne Clearance store.";
                    await _logger.WarningAsync(message);

                    continue;
                }

                stagedModels.Add(product.Sku);

                // Get the rest of the data and perform the insert.
                stagingActions.Parameters["@StagingItemNum"].Value = product.ItemNumber;
                stagingActions.Parameters["@StagingName"].Value = product.ItemName;
                stagingActions.Parameters["@StagingShortDesc"].Value = product.ShortDescription;
                stagingActions.Parameters["@StagingColor"].Value = product.Color;
                stagingActions.Parameters["@StagingOnAbc"].Value = product.OnAbc;
                stagingActions.Parameters["@StagingOnHawthorne"].Value = product.OnHawthorne;
                stagingActions.Parameters["@StagingOnClearance"].Value = product.OnClearance;
                stagingActions.Parameters["@StagingOnHawthorneClearance"].Value = product.OnHawthorneClearance;
                stagingActions.Parameters["@StagingSku"].Value = product.Sku;
                stagingActions.Parameters["@StagingManNum"].Value = product.ManufacturerNumber;
                stagingActions.Parameters["@StagingDisableBuy"].Value = product.BuyButtonDisabled;
                stagingActions.Parameters["@StagingWeight"].Value = product.Weight;
                stagingActions.Parameters["@StagingLength"].Value = product.Length;
                stagingActions.Parameters["@StagingWidth"].Value = product.Width;
                stagingActions.Parameters["@StagingHeight"].Value = product.Height;
                stagingActions.Parameters["@StagingPickupInStore"].Value = product.PickupInStore;
                stagingActions.Parameters["@StagingInstockFlag"].Value = product.InstockFlagValue;
                stagingActions.Parameters["@StagingNew"].Value = product.IsNew;
                stagingActions.Parameters["@StagingNewDate"].Value = product.NewEndDate;
                stagingActions.Parameters["@StagingLimitedDate"].Value = product.LimitedStockEndDate;
                stagingActions.Parameters["@StagingChoosePrice"].Value = product.CustomerEntersPrice;
                stagingActions.Parameters["@StagingUpsFlag"].Value = product.CanUseUps;
                stagingActions.Parameters["@StagingUpc"].Value = product.Upc;
                stagingActions.Parameters["@StagingFactTag"].Value = product.FactTag;

                stagingActions.ExecuteNonQuery();
            }
        }

        private void PrepBackendSelect(IDbCommand command)
        {
            command.CommandText =
                "SELECT DISTINCT" +
                    " Inv.ITEM_NUMBER" +
                    ", Inv.DESCRIPTION" +
                    ", Inv.UNIT_OF_MEASURE" +
                    ", Inv.UNIT_COST" +
                    ", Inv.PRODUCT_TYPE" +
                    ", Inv.DEPARTMENT" +
                    ", Inv.SELL_PRICE" +
                    ", Inv.LIST_PRICE" +
                    ", Inv.UPS_FLAG" +
                    $", Inv.{BackendDbConstants.InvStockFlag}" +
                    ", Inv.WEB_PRICE" +
                    ", Inv.SECOND_DESC_SIZE" +
                    $", Inv.{BackendDbConstants.InvStatusCode}" +
                    ", Inv.DIST_CODE" +
                    ", Inv.UNI_PRICE_CODE" +
                    ", Inv.MODEL" +
                    ", Data.MISC_LINE_INFO" +
                    ", Data.HEADER_FACT_CODE" +
                    ", Data.WITH_FLAG" +
                    ", Data.EXPECTED_DATE" +
                    ", Data.WEIGHT" +
                    ", Data.DEPTH" +
                    ", Data.WIDTH" +
                    ", Data.HEIGHT" +
                    ", Data.CART_PRICE" +
                    ", Br.BRAND_DESC" +
                    ", Ix.KEY_UPC_BARCODE" +
                " FROM " + BackendDbConstants.InvTable + " Inv" +
                    $" LEFT JOIN DA1_INV_FACT_TAG Data ON Inv.{BackendDbConstants.InvItemNumber} = Data.ITEM_NUMBER" +
                    " LEFT JOIN DA6_BRAND_MASTER Br ON Data.BRAND_CODE = Br.BRAND" +
                    $" LEFT JOIN DA1_INVENTORY_IXREF Ix ON Inv.{BackendDbConstants.InvItemNumber} = Ix.ITEM_NUMBER" +
                " WHERE" +
                    // may end up needing to reintroduce this capability to update the IN values
                    "( Inv.PRODUCT_TYPE IN ('XDR') OR" +
                    " (" +
                        "Inv.WEB_ENABLE IN ('Y') AND" +
                        " (" +
                            $"Inv.{BackendDbConstants.InvStockFlag} IN ('Y', '1', '2', '3', '4', '5', '6', '8', 'A', 'B') OR" +
                            $" Inv.{BackendDbConstants.InvStatusCode} IN ('X', 'T', 'D', 'N')" +
                        ")" +
                    " )) " +
                    " and ( " + //this filters out items with no stock or that are discontinued
                    " Inv.INSTOCK_FLG <> '7'" +
                    " ) " +
                $" ORDER BY Inv.{BackendDbConstants.InvItemNumber}";

        }

        private void PrepStagingInsert(SqlCommand command)
        {
            command.CommandText =
                "INSERT INTO dbo.Product " +
                "(ItemNumber, Name, ShortDescription, Color, OnAbcSite, OnHawthorneSite, OnAbcClearanceSite, OnHawthorneClearanceSite, " +
                "Sku, ManufacturerNumber, DisableBuying, Weight, Length, Width, Height, AllowInStorePickup, InstockFlag, IsNew, " +
                "NewExpectedDate, LimitedStockDate, BasePrice, DisplayPrice, CartPrice, UsePairPricing, PriceBucketCode, " +
                "CustomerEntersPrice, CanUseUps, Upc, FactTag)" +
                " VALUES" +
                " (" +
                    "@StagingItemNum" +
                    ", @StagingName, @StagingShortDesc, @StagingColor" +
                    ", @StagingOnAbc, @StagingOnHawthorne, @StagingOnClearance, @StagingOnHawthorneClearance" +
                    ", @StagingSku, @StagingManNum" +
                    ", @StagingDisableBuy" +
                    ", @StagingWeight, @StagingLength, @StagingWidth, @StagingHeight" +
                    ", @StagingPickupInStore" +
                    ", @StagingInstockFlag, @StagingNew" +
                    ", @StagingNewDate, @StagingLimitedDate" +
                    ", 0, 0, 0, 0, 0" + // These are updated in price import.
                    ", @StagingChoosePrice, @StagingUpsFlag" +
                    ", @StagingUpc, @StagingFactTag" +
                ")";
            command.Parameters.Add("@StagingItemNum", SqlDbType.NVarChar);
            command.Parameters.Add("@StagingName", SqlDbType.NVarChar);
            command.Parameters.Add("@StagingShortDesc", SqlDbType.NVarChar);
            command.Parameters.Add("@StagingColor", SqlDbType.NVarChar);
            command.Parameters.Add("@StagingOnAbc", SqlDbType.Bit);
            command.Parameters.Add("@StagingOnHawthorne", SqlDbType.Bit);
            command.Parameters.Add("@StagingOnClearance", SqlDbType.Bit);
            command.Parameters.Add("@StagingOnHawthorneClearance", SqlDbType.Bit);
            command.Parameters.Add("@StagingSku", SqlDbType.NVarChar);
            command.Parameters.Add("@StagingManNum", SqlDbType.NVarChar);
            command.Parameters.Add("@StagingDisableBuy", SqlDbType.Bit);
            command.Parameters.Add("@StagingWeight", SqlDbType.Decimal);
            command.Parameters.Add("@StagingLength", SqlDbType.Decimal);
            command.Parameters.Add("@StagingWidth", SqlDbType.Decimal);
            command.Parameters.Add("@StagingHeight", SqlDbType.Decimal);
            command.Parameters.Add("@StagingPickupInStore", SqlDbType.Bit);
            command.Parameters.Add("@StagingInstockFlag", SqlDbType.Int);
            command.Parameters.Add("@StagingNew", SqlDbType.Bit);
            command.Parameters.Add("@StagingNewDate", SqlDbType.Date);
            command.Parameters.Add("@StagingLimitedDate", SqlDbType.Date);
            command.Parameters.Add("@StagingChoosePrice", SqlDbType.Bit);
            command.Parameters.Add("@StagingUpsFlag", SqlDbType.Bit);
            command.Parameters.Add("@StagingUpc", SqlDbType.NVarChar);
            command.Parameters.Add("@StagingFactTag", SqlDbType.NVarChar);
        }
    }
}