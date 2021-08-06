using Dapper;
using Nop.Plugin.Misc.AbcSync.Domain.Staging;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSync.Services.Staging
{
    public class ProductDataProductDownloadService : IProductDataProductDownloadService
    {
        private readonly ImportSettings _importSettings;

        public ProductDataProductDownloadService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public IEnumerable<ProductDataProductDownload> GetProductDataProductDownloadsByProductDataProductId(int pdpId)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                return connection.Query<ProductDataProductDownload>(
                    $"SELECT * FROM ProductDataProductDownloads WHERE ProductDataProduct_id = {pdpId}"
                );
            }
        }
    }
}