using Dapper;
using Nop.Plugin.Misc.AbcSync.Domain.Staging;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSync.Services.Staging
{
    public class IsamProductService : IIsamProductService
    {
        private readonly ImportSettings _importSettings;

        public IsamProductService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public IEnumerable<IsamProduct> GetIsamProducts()
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                return connection.Query<IsamProduct>(
                    "SELECT * FROM Product"
                );
            }
        }
    }
}