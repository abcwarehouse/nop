using Dapper;
using Nop.Plugin.Misc.AbcSync.Domain.Staging;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSync.Services.Staging
{
    public class SiteOnTimeProductService : ISiteOnTimeProductService
    {
        private readonly ImportSettings _importSettings;

        public SiteOnTimeProductService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public IEnumerable<SiteOnTimeProduct> GetSiteOnTimeProducts()
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                return connection.Query<SiteOnTimeProduct>(
                    "SELECT * FROM SiteOnTimeProduct"
                );
            }
        }
    }
}