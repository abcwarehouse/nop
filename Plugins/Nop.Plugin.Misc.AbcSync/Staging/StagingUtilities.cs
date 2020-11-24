using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using Nop.Core;
using Nop.Plugin.Misc.AbcCore;

namespace Nop.Plugin.Misc.AbcSync.Staging
{
    public static class StagingUtilities
	{
		#region SQL Filler Utilities

		/// <summary>
		///		This prepares the staging database for staging.
		///		It will clear out the database to leave a blank slate for staging.
		/// </summary>
		/// <param name="command">
		///		The Command used to execute the preparation commands.
		///	</param>
		public static void PrepStagingDb(SqlCommand command, string stagingTable)
		{
			// Clear the database.
			command.CommandText = "DELETE FROM " + stagingTable;
			command.ExecuteNonQuery();

			// Reset the identity field to start counting at 1.
			command.CommandText = "DBCC CHECKIDENT ('" + stagingTable + "', RESEED, 0)";
			command.ExecuteNonQuery();

			return;
		}

		/// <summary>
		///		Converts an enumerable (list) of strings into a string
		///		that can be inserted into a SQL statement
		///		such as the object of an IN clause.
		/// </summary>
		/// <param name="list">
		///		The list of strings that needs to be used in the SQL statement.
		/// </param>
		/// <returns>
		///		The string that can be directly added to the SQL statement.
		/// </returns>
		public static string GetSqlList(IEnumerable<string> list)
		{
			string ret = string.Empty;
			foreach (string s in list)
			{
				// Only add comma+space if it's not the first item.
				if (!string.IsNullOrWhiteSpace(ret))
				{
					ret += ", ";
				}

				// Add the item, but surround it with single quotes.
				ret += "'" + s + "'";
			}
			return ret.Trim();
		}

		/// <summary>
		///		Get a list of all item numbers that are in the "snap" table.
		/// </summary>
		/// <param name="command">
		///		The Command that can be used to access the backend database
		///		in order to read the "snap" table.
		/// </param>
		/// <returns>
		///		A hash set of all item numbers that are in the "snap" table.
		/// </returns>
		public static HashSet<string> GetSnapList(IDbCommand command)
		{
			HashSet<string> snapSet = new HashSet<string>();

			command.CommandText = $"SELECT DISTINCT {BackendDbConstants.SnapItemNumber} FROM {BackendDbConstants.SnapTable}";
			using (IDataReader snapItem = command.ExecuteReader())
			{
				while (snapItem.Read())
				{
					// Only add non-empty IDs (numbers).
					string snap = snapItem[BackendDbConstants.SnapItemNumber] as string;
					if (!string.IsNullOrWhiteSpace(snap))
					{
						snapSet.Add(snap);
					}
				}
			}

			return snapSet;
		}

		/// <summary>
		///		Get a list of all the item numbers that have been staged.
		/// </summary>
		/// <param name="command">
		///		The command that can be used to access the staging database
		///		in order to read the product table.
		/// </param>
		/// <returns>
		///		A hash set of all item numbers that have been staged.
		/// </returns>
		public static HashSet<string> GetStagedItemNumbers(IDbCommand command)
		{
			HashSet<string> itemNumSet = new HashSet<string>();

			command.CommandText = $"SELECT DISTINCT {StagingDbConstants.ItemNumber} FROM {StagingDbConstants.ProductTable}";
			using (IDataReader stagedProduct = command.ExecuteReader())
			{
				while (stagedProduct.Read())
				{
					// Only add non-empty IDs.
					string itemNum = stagedProduct[StagingDbConstants.ItemNumber] as string;
					if (!string.IsNullOrWhiteSpace(itemNum))
					{
						itemNumSet.Add(itemNum);
					}
				}
			}

			if (!itemNumSet.Any())
			{
				throw new NopException("Did not find any products in Staging table when getting staged item numbers.");
			}

			return itemNumSet;
		}

		#endregion
		#region Staging Logic Utilities

		/// <summary>
		///		Determine whether the passed status codes
		///		denote a clearance product.
		/// </summary>
		/// <param name="productStatusCode">
		///		The product status code from the backend inventory table.
		/// </param>
		public static bool HasClearanceStatus(string productStatusCode, string department = "")
		{
            if (IsMainStoreSpecialItem(department, productStatusCode)) {
                return false;
            }

            if ((productStatusCode == "N") ||
                (productStatusCode == "X") ||
				(productStatusCode == "T") ||
				(productStatusCode == "D"))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		///		Determine whether a product should be on the ABC store.
		/// </summary>
		/// <param name="productDistCode">
		///		The distribution code from the backend inventory table.
		/// </param>
		/// <param name="productStatusCode">
		///		The product status code from the backend inventory table.
		/// </param>
		public static bool IsProductOnAbcStore(
			string productDistCode, string productStatusCode, string department = "")
		{
            if (HasClearanceStatus(productStatusCode, department))
            {
                  return false;
            }

			if (string.IsNullOrWhiteSpace(productDistCode) ||
				productDistCode.Contains("A"))
			{
				return true;
			}
			return false;
		}

        /// <summary>
        ///		Determine whether a product should be on the Hawthorne store.
        /// </summary>
        /// <param name="productDistCode">
        ///		The distribution code from the backend inventory table.
        /// </param>
        /// <param name="productStatusCode">
        ///		The product status code from the backend inventory table.
        /// </param>
        public static bool IsProductOnHawthorneStore(
			string productDistCode, string productStatusCode, string department = "")
		{
			if (HasClearanceStatus(productStatusCode, department))
			{
				return false;
			}
			if (string.IsNullOrWhiteSpace(productDistCode) ||
				productDistCode.Contains("H"))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		///		Determine whether a product should be on the ABC Clearance store.
		/// </summary>
		/// <param name="productDistCode">
		///		The distribution code from the backend inventory table.
		/// </param>
		/// <param name="productStatusCode">
		///		The product status code from the backend inventory table.
		/// </param>
		public static bool IsProductOnABCClearance(
			string productDistCode, string productStatusCode, string department = "")
		{
			if (string.IsNullOrWhiteSpace(productDistCode) ||
				productDistCode.Contains("A"))
			{
				if (HasClearanceStatus(productStatusCode, department))
				{
					return true;
				}
			}
			return false;
		}

        /// <summary>
		///		Determine whether a product should be on the Hawthorne Clearance store.
		/// </summary>
		/// <param name="productDistCode">
		///		The distribution code from the backend inventory table.
		/// </param>
		/// <param name="productStatusCode">
		///		The product status code from the backend inventory table.
		/// </param>
		public static bool IsProductOnHawthorneClearance(
            string productDistCode, string productStatusCode, string department = "")
        {
            if (string.IsNullOrWhiteSpace(productDistCode) ||
                productDistCode.Contains("H"))
            {
                if (HasClearanceStatus(productStatusCode, department))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///		Convert the input string date in the provided format
        ///		into the proper DateTime format for staging (or DBNull).
        /// </summary>
        public static object GetUtcDate(string inDate, string dateFormat)
		{
			DateTime date;
			if (DateTime.TryParseExact(
				inDate, dateFormat, null, DateTimeStyles.None, out date))
			{
				return date.ToUniversalTime();
			}
			return DBNull.Value;
		}

		/// <summary>
		///		Calculate the SKU that is to be used.
		///		This makes it so that packages ('+' product types)
		///		will not match with Site on Time data.
		/// </summary>
		public static string CalculateSku(
			string modelId, string productType, string itemNumber)
		{
			if ((productType == null) ||
				(itemNumber == null))
			{
				return null;
			}

			if ((modelId == null) || productType.Contains("+"))
			{
				return "+" + itemNumber.Trim();
			}
			return modelId.Trim();
		}

        #endregion

        /**
         * Special items from John's request - this specific combination should be on the main stores
         */
        private static bool IsMainStoreSpecialItem(string department, string productStatusCode)
        {
            return department.Contains("T") && productStatusCode.Contains("N");
        }
    }
}