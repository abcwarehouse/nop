using System.Collections.Generic;
using Dapper;
using Nop.Plugin.Misc.AbcSync.Domain.Staging;

namespace Nop.Plugin.Misc.AbcSync.Data
{
    public class StagingDb
    {
        private readonly ImportSettings _importSettings;

        public StagingDb(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public void ExecuteNonQuery(string sqlQuery)
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                connection.Query(sqlQuery);
            }
        }

        public IEnumerable<StagingProduct> GetProducts()
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                return connection.Query<StagingProduct>("[GetProducts]");
            }
        }
    }
}
