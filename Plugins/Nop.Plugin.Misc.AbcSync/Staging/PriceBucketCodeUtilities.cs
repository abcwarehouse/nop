using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcSync.Staging
{
	/// <summary>
	///		Price bucket codes to determine various on-site flags.
	/// </summary>
	enum PriceBucketCode
	{
		NoValue,
		OrLess,
		BestDeal,
		CallUs,
		InStoreOnly,
		OnlineOnly,
		AddToCartForCurrentPricingRequireLogin,
		OnlineOnlyFreeShipping,
		AddToCartForCurrentPricing,
        OpenBox15Percent,
        OpenBox10Percent,
        OpenBox20Percent
    }

	static class ProductBucketCodeExtensions
	{
		public static bool UsesMAPPricing(this PriceBucketCode pbc)
		{
			return pbc == PriceBucketCode.AddToCartForCurrentPricingRequireLogin || pbc == PriceBucketCode.AddToCartForCurrentPricing;
		}
	}

	static class PriceBucketCodeUtilities
	{
		/// <summary>
		///		Set the PriceBucketCode using the price code.
		///		The price code determines the return wholly
		///		and does not modify an already given price bucket.
		/// </summary>
		public static PriceBucketCode SetBucketByPriceCode(string priceCode)
		{
			if (!string.IsNullOrWhiteSpace(priceCode))
			{
				switch (priceCode)
				{
				    case "O":
				    {
					    return PriceBucketCode.OrLess;
				    }
				    case "U":
				    {
					    return PriceBucketCode.BestDeal;
				    }
				    case "M":
				    {
					    return PriceBucketCode.CallUs;
				    }
				    case "A":
				    {
					    return PriceBucketCode.InStoreOnly;
				    }
				    case "W":
				    {
					    return PriceBucketCode.OnlineOnly;
				    }
				    case "L":
				    {
					    return PriceBucketCode.AddToCartForCurrentPricingRequireLogin;
				    }
				    case "X":
				    {
					    return PriceBucketCode.AddToCartForCurrentPricing;
				    }
				    case "B":
                    case "C":
				    {
					    return PriceBucketCode.OnlineOnlyFreeShipping;
				    }
                    case "Y":
                    {
                        return PriceBucketCode.OpenBox10Percent;
                    }
                    case "Z":
                    {
                        return PriceBucketCode.OpenBox15Percent;
                    }
                    case "V":
                    {
                        return PriceBucketCode.OpenBox20Percent;
                    }
                }
			}
			return PriceBucketCode.NoValue;
		}

		/// <summary>
		///		Update the PriceBucketCode given the item's status code.
		/// </summary>
		/// <param name="priceBucket">
		///		The current PriceBucketCode.
		/// </param>
		/// <returns>
		///		Either the previous PriceBucketCode value (priceBucket)
		///		or an updated PriceBucketCode value.
		/// </returns>
		public static PriceBucketCode ChangeBucketByStatusCode(
			PriceBucketCode priceBucket, string statusCode, string department )
		{
			if (((statusCode == "A") || (statusCode == "AB")) && (department != "T" && department != "S" ))
			{
				priceBucket = PriceBucketCode.InStoreOnly;
			}
			return priceBucket;
		}

		/// <summary>
		///		Update the PriceBucketCode given the item's department.
		/// </summary>
		/// <param name="priceBucket">
		///		The current PriceBucketCode.
		/// </param>
		/// <returns>
		///		Either the previous PriceBucketCode value (priceBucket)
		///		or an updated PriceBucketCode value.
		/// </returns>
		public static PriceBucketCode ChangeBucketByDepartment(
			PriceBucketCode priceBucket, string department)
		{
			if ((department == "I"))
			{
				priceBucket = PriceBucketCode.InStoreOnly;
			}
			return priceBucket;
		}

		/// <summary>
		///		Update the PriceBucketCode by checking
		///		whether this item is on the "snap" list.
		///		The snap list is only checked on certain in-stock flags.
		/// </summary>
		/// <param name="priceBucket">
		///		The current PriceBucketCode.
		/// </param>
		/// <returns>
		///		Either the previous PriceBucketCode value (priceBucket)
		///		or an updated PriceBucketCode value.
		/// </returns>
		public static PriceBucketCode ChangeBucketBySnapList(
			PriceBucketCode priceBucket, string inStockFlag,
			string itemNum, HashSet<string> snapList)
		{
			if (inStockFlag.Contains("1") || inStockFlag.Contains("2") ||
				inStockFlag.Contains("4") || inStockFlag.Contains("6") ||
				inStockFlag.Contains("A") || inStockFlag.Contains("B"))
			{
				if (snapList.Contains(itemNum))
				{
					priceBucket = PriceBucketCode.AddToCartForCurrentPricingRequireLogin;
				}
			}
			return priceBucket;
		}

		/// <summary>
		///		Clearance items cannot have MAP pricing (there is no cart).
		/// </summary>
		/// <param name="priceBucket">
		///		The current PriceBucketCode.
		/// </param>
		/// <returns>
		///		Either the previous PriceBucketCode value (priceBucket)
		///		or an updated PriceBucketCode value.
		/// </returns>
		public static PriceBucketCode ChangeBucketByClearance(
			PriceBucketCode priceBucket, string statusCode, string department)
		{
			if ((priceBucket ==
					PriceBucketCode.AddToCartForCurrentPricing) ||
				(priceBucket ==
					PriceBucketCode.AddToCartForCurrentPricingRequireLogin))
			{
				if (StagingUtilities.HasClearanceStatus(statusCode, department))
				{
					return PriceBucketCode.NoValue;
				}
			}

			return priceBucket;
		}

		/// <summary>
		///		Forcing the cart pricing (MAP) is only needed when
		///		the MAP discount is actually less than the standard price.
		/// </summary>
		/// <param name="priceBucket">
		///		The current PriceBucketCode.
		/// </param>
		/// <returns>
		///		Either the previous PriceBucketCode value (priceBucket)
		///		or an updated PriceBucketCode value.
		/// </returns>
		public static PriceBucketCode ChangeBucketByMapPrice(
			PriceBucketCode priceBucket, decimal displayPrice, decimal mapPrice)
		{
			if ((priceBucket ==
					PriceBucketCode.AddToCartForCurrentPricing) ||
				(priceBucket ==
					PriceBucketCode.AddToCartForCurrentPricingRequireLogin))
			{
				if (displayPrice <= mapPrice)
				{
					return PriceBucketCode.NoValue;
				}
			}

			return priceBucket;
		}
	}
}