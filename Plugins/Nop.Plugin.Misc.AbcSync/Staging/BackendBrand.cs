using System.Data;
using System.Data.SqlClient;
using Nop.Services.Logging;
using Nop.Plugin.Misc.AbcCore;

namespace Nop.Plugin.Misc.AbcSync.Staging
{
    class BackendBrand
    {
        private readonly string _itemNum;
        private readonly string _status;
        private readonly string _distCode;
        private readonly string _sku;
        private readonly string _brandCode;
        private readonly string _brandName;

        /// <summary>
        ///		Populate this class with all actual backend data
        ///		here in the constructor.
        /// </summary>
        /// <param name="brand">
        ///		The DataReader connected to the backend selecting brand data.
        /// </param>
        public BackendBrand(IDataReader brand)
        {
            _itemNum = brand[BackendDbConstants.InvItemNumber] as string;
            _status = brand[BackendDbConstants.InvStatusCode] as string;
            _distCode = brand[BackendDbConstants.InvDist] as string;
            _brandCode = brand[BackendDbConstants.BrandCode] as string;
            _brandName = brand[BackendDbConstants.BrandName] as string;

            string modelId = brand[BackendDbConstants.InvModel] as string;
            string prodType = brand[BackendDbConstants.InvProductType] as string;

            _sku = StagingUtilities.CalculateSku(modelId, prodType, _itemNum);

            return;
        }

        #region Utility Accessor Methods

        /// <summary>
        ///		Determine whether the brand has a model ID, brand code, and a name.
        ///		It needs to have both of these key values.
        ///		A warning is logged if this returns false.
        /// </summary>
        public bool HasKeyValues(ILogger logger, out string brandCodeToSkip)
        {
            brandCodeToSkip = null;

            if (string.IsNullOrWhiteSpace(_brandCode))
            {
                string message = "Unable to import this brand." +
                    " No brand code is found, so distinct matching cannot be done.";
                logger.Warning(message);

                return false;
            }
            if (string.IsNullOrWhiteSpace(_brandName))
            {
                string message = "Unable to import this brand." +
                    " No name is associated with brand code " + _brandCode + ".";
                logger.Warning(message);
                brandCodeToSkip = _brandCode;

                return false;
            }
            if (string.IsNullOrWhiteSpace(_itemNum))
            {
                string message = "Unable to import this product-brand mapping." +
                    " An item number cannot be read for the brand with code "
                    + _brandCode + ".";
                logger.Warning(message);

                return false;
            }
            if (string.IsNullOrWhiteSpace(_sku))
            {
                string message = "Unable to import this product-brand mapping." +
                    " An entry with item number " + _itemNum +
                    " was found without a model ID relating to brand code " +
                    _brandCode + ".";
                logger.Warning(message);

                return false;
            }

            return true;
        }

        /// <summary>
        ///		Insert a product-brand mapping into the staging mapping table.
        /// </summary>
        /// <param name="insert">
        ///		The Command to be used for inserting
        ///		into the staging product-manufacturer mapping table.
        /// </param>
        public void InsertProdBrandMapping(SqlCommand insert, ILogger logger)
        {
            if (!OnAbc && !OnHawthorne && !OnAbcClearance && !OnHawthorneClearance)
            {
                string message = "Unable to import a product-brand mapping." +
                    " Product with model ID " + _sku + " is not on" +
                    " the ABC online store, the Hawthorne online store," +
                    " or either clearance store.";
                logger.Warning(message);

                return;
            }

            insert.Parameters["@StagingMappingItemSku"].Value = _sku;
            insert.Parameters["@StagingMappingBrand"].Value = _brandCode;

            insert.ExecuteNonQuery();
            return;
        }

        /// <summary>
        ///		Return an updated BrandData
        ///		with the new brand code and name
        ///		as well as with updated, more permissive, store codes.
        /// </summary>
        /// <param name="brand">
        ///		The original BrandData that is to be updated.
        /// </param>
        public BrandData UpdateBrandData(BrandData brand)
        {
            brand.brandCode = _brandCode;
            brand.brandName = _brandName;
            brand.onAbc = brand.onAbc ? brand.onAbc : OnAbc;
            brand.onHawthorne = brand.onHawthorne ? brand.onHawthorne : OnHawthorne;
            brand.onClearance = brand.onClearance ? brand.onClearance : OnAbcClearance;
            brand.onHawthorneClearance = brand.onHawthorneClearance ?
                brand.onHawthorneClearance :
                OnHawthorneClearance;

            return brand;
        }

        #endregion

        public string ItemNumber
        {
            get
            {
                return _itemNum;
            }
        }

        public string BrandCode
        {
            get
            {
                return _brandCode;
            }
        }

        public bool OnAbc
        {
            get
            {
                // No model ID means it cannot be on any store.
                if (_sku == null)
                {
                    return false;
                }
                return StagingUtilities.IsProductOnAbcStore(_distCode, _status);
            }
        }

        public bool OnHawthorne
        {
            get
            {
                // No model ID means it cannot be on any store.
                if (_sku == null)
                {
                    return false;
                }
                return StagingUtilities.IsProductOnHawthorneStore(_distCode, _status);
            }
        }

        public bool OnAbcClearance
        {
            get
            {
                // No model ID means it cannot be on any store.
                if (_sku == null)
                {
                    return false;
                }
                return StagingUtilities.IsProductOnABCClearance(_distCode, _status);
            }
        }

        public bool OnHawthorneClearance
        {
            get
            {
                // No model ID means it cannot be on any store.
                if (_sku == null)
                {
                    return false;
                }
                return StagingUtilities.IsProductOnHawthorneClearance(_distCode, _status);
            }
        }
    }
}