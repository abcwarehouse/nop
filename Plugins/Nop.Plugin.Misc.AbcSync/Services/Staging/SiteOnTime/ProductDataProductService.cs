using Dapper;
using Dapper.Contrib.Extensions;
using Nop.Plugin.Misc.AbcSync.Domain.Staging;
using Nop.Plugin.Misc.AbcSync.Domain.Staging.SiteOnTime;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSync.Services.Staging
{
    public class ProductDataProductService : IProductDataProductService
    {
        private readonly ImportSettings _importSettings;

        public ProductDataProductService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public IEnumerable<ProductDataProduct> GetProductDataProducts()
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                return connection.Query<ProductDataProduct>(
                    "SELECT * FROM ProductDataProducts"
                );
            }
        }

        // returns the ID for later use
        public int InsertProductDataProduct(ProductDataProduct pdp)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                return (int)connection.Insert(pdp);
            }
        }

        public void InsertProductDataProductDimension(ProductDataProductDimension pdpDimension)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                connection.Insert(pdpDimension);
            }
        }

        public void InsertProductDataProductDownload(ProductDataProductDownload pdpDownload)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                connection.Insert(pdpDownload);
            }
        }

        public void InsertProductDataProductFeature(ProductDataProductFeature pdpFeature)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                connection.Insert(pdpFeature);
            }
        }

        public void InsertProductDataProductFilter(ProductDataProductFilter pdpFilter)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                connection.Insert(pdpFilter);
            }
        }

        public void InsertProductDataProductImage(ProductDataProductImage pdpImage)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                connection.Insert(pdpImage);
            }
        }

        public void InsertProductDataProductpmap(ProductDataProductpmap pdpPmap)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                connection.Query(@"
                INSERT INTO ProductDataProductpmaps (Price, Discount, Startdate, Enddate, ProductDataProduct_id)
                VALUES (@Price, @Discount, @StartDate, @EndDate, @ProductDataProductId);",
                new
                {
                    Price = pdpPmap.price,
                    Discount = pdpPmap.discount,
                    StartDate = pdpPmap.startDate,
                    EndDate = pdpPmap.endDate,
                    ProductDataProductId = pdpPmap.ProductDataProduct_id
                }
                );
            }
        }

        public void InsertProductDataProductRelatedItem(ProductDataProductRelatedItem pdpRelatedItem)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                connection.Insert(pdpRelatedItem);
            }
        }
    }
}