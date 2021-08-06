using Dapper;
using Dapper.Contrib.Extensions;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using System.Collections.Generic;
using Nop.Plugin.Misc.AbcSync;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging
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

        public void InsertProductDataProductDownloads(List<ProductDataProductDownload> downloads, int pdpId)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                foreach (var d in downloads)
                {
                    d.ProductDataProduct_id = pdpId;
                    connection.Insert(d);
                }
            }
        }
    }
}