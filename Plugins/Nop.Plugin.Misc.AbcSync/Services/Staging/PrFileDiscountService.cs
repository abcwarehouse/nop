using Dapper;
using Nop.Plugin.Misc.AbcSync.Domain.Staging;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSync.Services.Staging
{
    public class PrFileDiscountService : IPrFileDiscountService
    {
        private readonly ImportSettings _importSettings;

        public PrFileDiscountService(
            ImportSettings importSettings
        )
        {
            _importSettings = importSettings;
        }

        public IEnumerable<PrFileDiscount> GetPrFileDiscounts()
        {
            using (var connection = _importSettings.GetStagingDbConnection())
            {
                return connection.Query<PrFileDiscount>(
                    @"SELECT * FROM PrFileDiscounts"
                );
            }
        }
    }
}