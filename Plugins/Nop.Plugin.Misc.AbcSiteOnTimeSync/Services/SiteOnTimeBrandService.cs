using Dapper;
using Nop.Plugin.Misc.AbcSiteOnTimeSync.Domain.Staging;
using System.Collections.Generic;
using Nop.Plugin.Misc.AbcSync;

namespace Nop.Plugin.Misc.AbcSiteOnTimeSync.Services.Staging
{
    public class SiteOnTimeBrandService : ISiteOnTimeBrandService
    {
        private readonly ImportSettings _importSettings;

        public SiteOnTimeBrandService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public IEnumerable<SiteOnTimeBrand> GetSiteOnTimeBrands()
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                return connection.Query<SiteOnTimeBrand>(
                    "SELECT * FROM SiteOnTimeBrand"
                );
            }
        }
    }
}