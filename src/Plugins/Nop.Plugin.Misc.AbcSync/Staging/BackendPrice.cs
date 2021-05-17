using System.Collections.Generic;
using System.Data;
using Nop.Services.Logging;
using Nop.Plugin.Misc.AbcCore;

namespace Nop.Plugin.Misc.AbcSync.Staging
{
    class BackendPrice
    {
        private readonly string _itemNum;
        private readonly string _saleUnit;
        private readonly decimal? _unitCost;
        private readonly string _department;
        private readonly decimal? _sellPrice;
        private readonly decimal? _listPrice;
        private readonly string _inStock;
        private readonly decimal? _webPrice;
        private readonly string _status;
        private readonly string _distCode;
        private readonly string _priceCode;
        private readonly decimal? _cartPrice;
        private readonly string _sku;
        private readonly HashSet<string> _snapList;

        /// <summary>
        ///		Populate this class with all actual backend data
        ///		here in the constructor.
        /// </summary>
        /// <param name="product">
        ///		The DataReader connected to the backend selecting product data.
        /// </param>
        /// <param name="snapList">
        ///		A list of all items that were listed in the "snap" table.
        /// </param>
        public BackendPrice(IDataReader productPrice, HashSet<string> snapList)
        {
            _itemNum = productPrice[BackendDbConstants.InvItemNumber] as string;
            _saleUnit = productPrice[BackendDbConstants.InvSaleUnit] as string;
            _unitCost = productPrice[BackendDbConstants.InvUnitCost] as decimal?;
            _department = productPrice[BackendDbConstants.InvDept] as string;
            _sellPrice = productPrice[BackendDbConstants.InvSellPrice] as decimal?;
            _listPrice = productPrice[BackendDbConstants.InvListPrice] as decimal?;
            _inStock = productPrice[BackendDbConstants.InvStockFlag] as string;
            _webPrice = productPrice[BackendDbConstants.InvWebPrice] as decimal?;
            _status = productPrice[BackendDbConstants.InvStatusCode] as string;
            _distCode = productPrice[BackendDbConstants.InvDist] as string;
            _priceCode = productPrice[BackendDbConstants.InvPriceCode] as string;
            _cartPrice = productPrice[BackendDbConstants.DataCartPrice] as decimal?;
            _snapList = snapList;

            string prodType = productPrice[BackendDbConstants.InvProductType] as string;
            string modelId = productPrice[BackendDbConstants.InvModel] as string;
            _sku = StagingUtilities.CalculateSku(modelId, prodType, _itemNum);

            return;
        }

        #region Utility Accessor Methods

        /// <summary>
        ///		Determine whether the pricing data
        ///		has a model ID and an item number.
        ///		The model ID is needed for matching to a product,
        ///		and the item number is needed for backend checks.
        ///		A warning is logged if this returns false.
        /// </summary>
        public bool HasKeyValues(ILogger logger,
            out string skipItemNum, out string skipSku)
        {
            skipItemNum = null;
            skipSku = null;

            bool emptySku = string.IsNullOrWhiteSpace(_sku);
            bool emptyItemNum = string.IsNullOrWhiteSpace(_itemNum);

            if (emptySku && emptyItemNum)
            {
                string message = "Unable to import this product price data." +
                    " Neither the model ID or item number could be found.";
                logger.Warning(message);

                return false;
            }
            if (emptySku && !emptyItemNum)
            {
                string message = "Unable to import this product price data." +
                    " The model ID could not be found" +
                    " for item number " + _itemNum + ".";
                logger.Warning(message);

                skipItemNum = _itemNum;
                return false;
            }
            if (emptyItemNum && !emptySku)
            {
                string message = "Unable to import this product price data." +
                    " The item number could not be found for the product" +
                    " with model ID " + _sku + ".";
                logger.Warning(message);

                skipSku = _sku;
                return false;
            }

            return true;
        }

        /// <summary>
        ///		Determines if the related product
        ///		is not on any of the online stores.
        ///		A warning is logged if this returns true.
        /// </summary>
        public bool NotOnAnyStore(ILogger logger)
        {
            if (!StagingUtilities.IsProductOnAbcStore(_distCode, _status) &&
                !StagingUtilities.IsProductOnHawthorneStore(_distCode, _status) &&
                !StagingUtilities.IsProductOnABCClearance(_distCode, _status) &&
                !StagingUtilities.IsProductOnHawthorneClearance(_distCode, _status))
            {
                string message = "Unable to import this product price data." +
                    " Product with model ID " + _sku + " does not exist on" +
                    " the ABC online store, the Hawthorne online store," +
                    " the ABC Clearance store, or the Hawthorne Clearance store.";
                logger.Warning(message);

                return true;
            }

            return false;
        }

        #endregion
        #region Private Helper Accessors

        private PriceBucketCode CalculationPriceBucket
        {
            get
            {
                PriceBucketCode priceBucket = PriceBucketCodeUtilities
                    .SetBucketByPriceCode(_priceCode);
                priceBucket = PriceBucketCodeUtilities
                    .ChangeBucketByStatusCode(priceBucket, _status, _department);
                priceBucket = PriceBucketCodeUtilities
                    .ChangeBucketBySnapList(priceBucket, _inStock, _itemNum, _snapList);

                return priceBucket;
            }
        }

        /// <summary>
        ///		This will give the standard (often list) price of the product.
        /// </summary>
        private decimal StandardSellPrice
        {
            get
            {
                return _sellPrice ?? 0;
            }
        }

        /// <summary>
        ///		This returns the given web price from the backend.
        /// </summary>
        private decimal ListedWebPrice
        {
            get
            {
                return _webPrice.HasValue && _webPrice != 0 ? _webPrice.Value : BasePrice;
            }
        }

        /// <summary>
        ///		This gives the specific cart price
        ///		(for the "add to cart" price buckets).
        ///		It defaults to web price.
        /// </summary>
        private decimal InCartOnlyMapDiscountPrice
        {
            get
            {
                decimal ret = _webPrice ?? 0;

                if ((CalculationPriceBucket ==
                        PriceBucketCode.AddToCartForCurrentPricing) ||
                    (CalculationPriceBucket ==
                        PriceBucketCode.AddToCartForCurrentPricingRequireLogin))
                {
                    if ((_cartPrice != null) && (_cartPrice > 0))
                    {
                        ret = _cartPrice.Value;
                    }
                }

                if (ret <= 0)
                {
                    ret = FinalBaseDisplayPrice;
                }
                return ret;
            }
        }

        /// <summary>
        ///		This runs a lot of logic based on the price bucket
        ///		in order to determine the final base display price.
        /// </summary>
        private decimal FinalBaseDisplayPrice
        {
            get
            {
                // It starts at the web price.
                decimal ret = _webPrice ?? 0;

                // If no web price is given, we need a different basis.
                if (ret <= 0)
                {
                    if ((CalculationPriceBucket ==
                            PriceBucketCode.AddToCartForCurrentPricing) ||
                        (CalculationPriceBucket ==
                            PriceBucketCode.AddToCartForCurrentPricingRequireLogin))
                    {
                        ret = _listPrice ?? 0;
                    }
                    else
                    {
                        // According to the WSC, if it is OnlineOnly,
                        // getting here is an error,
                        // but it continues by setting this value.
                        ret = _sellPrice ?? 0;
                    }

                    // Hard minimum here of unit cost.
                    if (ret < (_unitCost ?? 0))
                    {
                        ret = _unitCost ?? 0;
                    }
                }
                else
                {
                    // Code C Authorized reseller is sell price
                    // with the unit cost hard minimum.
                    if (CalculationPriceBucket == PriceBucketCode.OnlineOnlyFreeShipping)
                    {
                        ret = _sellPrice ?? 0;
                        if (ret < (_unitCost ?? 0))
                        {
                            ret = _unitCost ?? 0;
                        }
                    }

                    // Add to cart must always be based on list price.
                    // For some reason, there's no hard minimum here.
                    if ((CalculationPriceBucket ==
                            PriceBucketCode.AddToCartForCurrentPricing) ||
                        (CalculationPriceBucket ==
                            PriceBucketCode.AddToCartForCurrentPricingRequireLogin))
                    {
                        ret = _listPrice ?? 0;
                    }
                }

                // If there's still no price, the WSC quietly warns and continues.
                if (ret <= 0)
                {
                    if ((CalculationPriceBucket ==
                            PriceBucketCode.AddToCartForCurrentPricing) ||
                        (CalculationPriceBucket ==
                            PriceBucketCode.AddToCartForCurrentPricingRequireLogin))
                    {
                        ret = 0;
                    }
                    else
                    {
                        ret = _sellPrice ?? 0;
                    }
                }

                return ret;
            }
        }

        #endregion
        #region Mapping Accessors

        public string ItemNum
        {
            get
            {
                return _itemNum;
            }
        }

        public string Sku
        {
            get
            {
                return _sku;
            }
        }

        public decimal BasePrice
        {
            get
            {
                return StandardSellPrice;
            }
        }

        public decimal DisplayPrice
        {
            get
            {
                if ((PriceBucket ==
                        PriceBucketCode.AddToCartForCurrentPricing) ||
                    (PriceBucket ==
                        PriceBucketCode.AddToCartForCurrentPricingRequireLogin))
                {
                    if (BackendDbConstants.InvWebPriceDepartments.Contains(_department))
                    {
                        return ListedWebPrice;
                    }
                }

                return FinalBaseDisplayPrice;
            }
        }

        public decimal CartPrice
        {
            get
            {
                return InCartOnlyMapDiscountPrice;
            }
        }

        public bool UsePairPricing
        {
            get
            {
                if ((_saleUnit == "PR") && (_department == "S"))
                {
                    return true;
                }
                return false;
            }
        }

        public PriceBucketCode PriceBucket
        {
            get
            {
                PriceBucketCode priceBucket = PriceBucketCodeUtilities
                    .SetBucketByPriceCode(_priceCode);
                priceBucket = PriceBucketCodeUtilities
                    .ChangeBucketByStatusCode(priceBucket, _status, _department);
                priceBucket = PriceBucketCodeUtilities
                    .ChangeBucketByDepartment(priceBucket, _department);
                priceBucket = PriceBucketCodeUtilities
                    .ChangeBucketBySnapList(priceBucket, _inStock, _itemNum, _snapList);
                priceBucket = PriceBucketCodeUtilities
                    .ChangeBucketByClearance(priceBucket, _status, _department);
                priceBucket = PriceBucketCodeUtilities
                    .ChangeBucketByMapPrice(priceBucket,
                        FinalBaseDisplayPrice, InCartOnlyMapDiscountPrice);

                return priceBucket;
            }
        }

        #endregion
    }
}