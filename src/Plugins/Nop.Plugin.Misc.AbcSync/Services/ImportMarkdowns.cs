using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcSync.Data;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    class ImportMarkdowns : BaseAbcWarehouseService, IImportMarkdowns
    {
        private readonly INopDataProvider _nopDbContext;
        private readonly ImportSettings _importSettings;

        public ImportMarkdowns(
            INopDataProvider nopDbContext,
            ImportSettings importSettings
        )
        {
            _nopDbContext = nopDbContext;
            _importSettings = importSettings;
        }

        /// <summary>
        ///		Begin the import process for product's specifications.
        /// </summary>
        public async Task ImportAsync()
        {
            var stagingDbName = _importSettings.GetStagingDbConnection().Database;
            var productManager = new EntityManager<Product>();
            var genericAttributeTableName = _nopDbContext.GetTable<GenericAttribute>().TableName;

            var deleteMarkdownsCommand = $@"DELETE FROM {genericAttributeTableName}
                                            WHERE KeyGroup = 'Product' AND ([Key] IN ('SpecialPrice', 'SpecialPriceStartDate', 'SpecialPriceEndDate'));";

            var importMarkdownStartDatesFromStagingCommand = $@"INSERT INTO {genericAttributeTableName} (EntityId, KeyGroup, [Key], Value, StoreId)
                                                                SELECT p.Id, 'Product', 'SpecialPriceStartDate', MIN(sd.StartDate), 0
                                                                FROM {stagingDbName}.dbo.ScandownDates sd
                                                                JOIN Product p on p.Sku = sd.Sku
                                                                GROUP BY p.Id;";

            var importMarkdownEndDatesFromStagingCommand = $@"INSERT INTO {genericAttributeTableName} (EntityId, KeyGroup, [Key], Value, StoreId)
                                                                SELECT p.Id, 'Product', 'SpecialPriceEndDate', MIN(sd.EndDate), 0
                                                                FROM {stagingDbName}.dbo.ScandownDates sd
                                                                JOIN Product p on p.Sku = sd.Sku
                                                                GROUP BY p.Id;";

            await _nopDbContext.ExecuteNonQueryAsync(deleteMarkdownsCommand);
            await _nopDbContext.ExecuteNonQueryAsync(importMarkdownStartDatesFromStagingCommand);
            await _nopDbContext.ExecuteNonQueryAsync(importMarkdownEndDatesFromStagingCommand);
            await productManager.FlushAsync();
        }
    }
}