using Dapper;
using Nop.Plugin.Misc.AbcSync.Domain.Staging;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSync.Services.Staging
{
    public class PromoService : IPromoService
    {
        private readonly ImportSettings _importSettings;

        public PromoService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public IEnumerable<Promo> GetPromos()
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                return connection.Query<Promo>(
                    "SELECT * FROM Promo"
                );
            }
        }
    }
}