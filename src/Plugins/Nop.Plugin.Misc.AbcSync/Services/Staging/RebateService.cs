using Dapper;
using Nop.Plugin.Misc.AbcSync.Domain.Staging;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSync.Services.Staging
{
    public class RebateService : IRebateService
    {
        private readonly ImportSettings _importSettings;

        public RebateService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public IEnumerable<Rebate> GetRebates()
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                return connection.Query<Rebate>(
                    "SELECT * FROM Rebate"
                );
            }
        }
    }
}