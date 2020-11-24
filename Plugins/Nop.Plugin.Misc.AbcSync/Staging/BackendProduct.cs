using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Nop.Services.Logging;
using System.Collections.ObjectModel;
using Nop.Plugin.Misc.AbcCore;

namespace Nop.Plugin.Misc.AbcSync.Staging
{
    class BackendProduct
	{
		private readonly string _itemNum;
		private readonly string _desc;
		private readonly string _saleUnit;
		private readonly decimal? _unitCost;
		private readonly string _prodType;
		private readonly string _department;
		private readonly decimal? _sellPrice;
		private readonly decimal? _listPrice;
		private readonly string _upsFlag;
		private readonly string _inStock;
		private readonly decimal? _webPrice;
		private readonly string _secondDesc;
		private readonly string _status;
		private readonly string _distCode;
		private readonly string _priceCode;
		private readonly string _desc1;
		private readonly string _desc2;
		private readonly string _descWith;
		private readonly string _date;
		private readonly decimal? _weight;
		private readonly decimal? _length;
		private readonly decimal? _width;
		private readonly decimal? _height;
		private readonly decimal? _cartPrice;
		private readonly string _sku;
		private readonly string _brand;
        private readonly string _upc;
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
		public BackendProduct(IDataReader product, HashSet<string> snapList)
		{
			_itemNum = product[BackendDbConstants.InvItemNumber] as string;
			_desc = product[BackendDbConstants.InvDescription] as string;
			_saleUnit = product[BackendDbConstants.InvSaleUnit] as string;
			_unitCost = product[BackendDbConstants.InvUnitCost] as decimal?;
			_prodType = product[BackendDbConstants.InvProductType] as string;
			_department = product[BackendDbConstants.InvDept] as string;
			_sellPrice = product[BackendDbConstants.InvSellPrice] as decimal?;
			_listPrice = product[BackendDbConstants.InvListPrice] as decimal?;
			_upsFlag = product[BackendDbConstants.InvUpsFlag] as string;
			_inStock = product[BackendDbConstants.InvStockFlag] as string;
			_webPrice = product[BackendDbConstants.InvWebPrice] as decimal?;
			_secondDesc = product[BackendDbConstants.InvSecondDesc] as string;
			_status = product[BackendDbConstants.InvStatusCode] as string;
			_distCode = product[BackendDbConstants.InvDist] as string;
			_priceCode = product[BackendDbConstants.InvPriceCode] as string;
			_desc1 = product[BackendDbConstants.DataDesc1] as string;
			_desc2 = product[BackendDbConstants.DataDesc2] as string;
			_descWith = product[BackendDbConstants.DataDescWithFlag] as string;
			_date = product[BackendDbConstants.DataDate] as string;
			_weight = product[BackendDbConstants.DataWeight] as decimal?;
			_length = product[BackendDbConstants.DataLength] as decimal?;
			_width = product[BackendDbConstants.DataWidth] as decimal?;
			_height = product[BackendDbConstants.DataHeight] as decimal?;
			_cartPrice = product[BackendDbConstants.DataCartPrice] as decimal?;
			_brand = product[BackendDbConstants.BrandName] as string;
            _upc = product["KEY_UPC_BARCODE"] as string;
            _snapList = snapList;

			string modelId = product[BackendDbConstants.InvModel] as string;
			_sku = StagingUtilities.CalculateSku(modelId, _prodType, _itemNum);

			return;
		}

		/// <summary>
		///		Determine whether the product has a model ID and item number.
		///		It needs to have both of these key values.
		/// </summary>
		public bool HasKeyValues(ILogger logger)
		{
			if (string.IsNullOrWhiteSpace(_itemNum))
			{
				string message = "Unable to import the product" +
					" because there is no item number." +
					" It cannot be mapped to any data.";
				logger.Warning(message);

				return false;
			}
			if (string.IsNullOrWhiteSpace(Sku))
			{
				string message = "Unable to import the product with item number " +
					_itemNum + ". The model ID is missing" +
					" and is needed for proper mapping.";
				logger.Warning(message);

				return false;
			}

			return true;
		}

		private PriceBucketCode PriceBucket
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

				return priceBucket;
			}
		}

		#region Mapping Accessors

		public string ItemNumber
		{
			get
			{
				return _itemNum.Trim();
			}
		}

		public string ItemName
		{
			get
			{
				string name =
					string.IsNullOrWhiteSpace(_brand) ? string.Empty : _brand.Trim();
				name += " " + ManufacturerNumber;
				return name.Trim();
			}
		}

        public object Upc
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_upc))
                {
                    return DBNull.Value;
                }
                return _upc;
            }
        }

        public object ShortDescription
		{
			get
			{
				string shortDesc =
					string.IsNullOrWhiteSpace(_desc1) ? string.Empty : _desc1.Trim();
				shortDesc += ((_descWith != null) && (_descWith == "Y")) ? " with " : " ";
				shortDesc += string.IsNullOrWhiteSpace(_desc2) ? string.Empty : _desc2.Trim();

				return string.IsNullOrWhiteSpace(shortDesc) ?
					(object)DBNull.Value : shortDesc.Trim();
			}
		}

        public object FactTag
        {
            get
            {
                return string.IsNullOrWhiteSpace(_desc1) ?
                    (object)DBNull.Value : _desc1.Trim();
            }
        }

        public object Color
		{
			get
			{
                // If the current department is not in the list
                // of departments with colors, skip it.
                var _backendInvColorDepartments = new List<string>() { "A", "D", "K", "M", "L", "O", "OB", "R", "RB" };
                if (!_backendInvColorDepartments.Contains(_department))
				{
					return DBNull.Value;
				}

				// If it was in the given list, the color is the second description.
				// If the color is not given, then ignore it,
				// otherwise, we return the color.
				if (string.IsNullOrWhiteSpace(_secondDesc))
				{
					return DBNull.Value;
				}
				return _secondDesc;
			}
		}

		public bool OnAbc
		{
			get
			{
				return StagingUtilities.IsProductOnAbcStore(_distCode, _status, _department);
			}
		}

		public bool OnHawthorne
		{
			get
			{
				return StagingUtilities.IsProductOnHawthorneStore(_distCode, _status, _department);
			}
		}

		public bool OnClearance
		{
			get
			{
				return StagingUtilities.IsProductOnABCClearance(_distCode, _status, _department);
			}
		}

        public bool OnHawthorneClearance
        {
            get
            {
                return StagingUtilities.IsProductOnHawthorneClearance(_distCode, _status, _department);
            }
        }

        public string Sku
		{
			get
			{
				return _sku;
			}
		}

		public object ManufacturerNumber
		{
			get
			{
				if (string.IsNullOrWhiteSpace(_desc))
				{
					return null;
				}
				string ret = _desc.Trim().Split(' ')[0].Trim();

				return string.IsNullOrWhiteSpace(ret) ? (object)DBNull.Value : ret;
			}
		}

		public bool BuyButtonDisabled
		{
			get
			{
				if (PriceBucket == PriceBucketCode.InStoreOnly ||
					(_inStock == "3") ||
					(_inStock == "5") ||
					(_inStock == "7") ||
					(_inStock == "8") ||
					StagingUtilities.HasClearanceStatus(_status, _department))
				{
					return true;
				}
				return false;
			}
		}

		public decimal Weight
		{
			get
			{
				return _weight ?? 0;
			}
		}

		public decimal Length
		{
			get
			{
				return _length ?? 0;
			}
		}

		public decimal Width
		{
			get
			{
				return _width ?? 0;
			}
		}

		public decimal Height
		{
			get
			{
				return _height ?? 0;
			}
		}

		public bool PickupInStore
		{
			get
			{
				if ((_inStock == "A") || (_inStock == "B"))
				{
					return true;
				}
				return false;
			}
		}

		public int InstockFlagValue
		{
			get
			{
				switch (_inStock)
				{
				case "2":
				{
					return (int)InstockFlag.ShipsIn2To3Weeks;
				}

				case "5":
				case "6":
				{
					return (int)InstockFlag.LowQuantity;
				}

				case "7":
				{
					return (int)InstockFlag.ItemDiscontinued;
				}

				case "8":
				{
					return (int)InstockFlag.SeeStoreForDetails;
				}
				}

				return (int)InstockFlag.NoValue;
			}
		}

		public bool IsNew
		{
			get
			{
				if ((_inStock == "3") || (_inStock == "4"))
				{
					return true;
				}
				return false;
			}
		}

		public object NewEndDate
		{
			get
			{
				if (_inStock != "4")
				{
					return DBNull.Value;
				}

				DateTime date;
				if (DateTime.TryParseExact(
					_date, "yyyyMMdd", null, DateTimeStyles.None, out date))
				{
					return date.ToUniversalTime();
				}

				return DBNull.Value;
			}
		}

		public object LimitedStockEndDate
		{
			get
			{
				if (_inStock != "6")
				{
					return DBNull.Value;
				}

				DateTime date;
				if (DateTime.TryParseExact(
					_date, "yyyyMMdd", null, DateTimeStyles.None, out date))
				{
					return date.ToUniversalTime();
				}

				return DBNull.Value;
			}
		}

		public bool CustomerEntersPrice
		{
			get
			{
				return (_prodType == "@GF");
			}
		}

		public bool CanUseUps
		{
			get
			{
				return (_upsFlag == "Y");
			}
		}

        public bool OnAnyStore
        {
            get
            {
                return (OnAbc || OnHawthorne || OnClearance || OnHawthorneClearance);
            }
        }

		#endregion
	}
}