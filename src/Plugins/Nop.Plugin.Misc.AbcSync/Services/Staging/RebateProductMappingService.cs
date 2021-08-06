using Dapper;
using Nop.Plugin.Misc.AbcSync.Domain.Staging;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSync.Services.Staging
{
    public class RebateProductMappingService : IRebateProductMappingService
    {
        private readonly ImportSettings _importSettings;

        public RebateProductMappingService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public IEnumerable<RebateProductMapping> GetProductRebateMappings()
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                return connection.Query<RebateProductMapping>(
                    "SELECT * FROM RebateProductMapping"
                );
            }
        }
    }
}