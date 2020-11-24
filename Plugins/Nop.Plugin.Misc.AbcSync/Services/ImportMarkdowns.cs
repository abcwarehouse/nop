using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Common;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcSync.Data;

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
		public void Import()
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

            _nopDbContext.ExecuteNonQuery(deleteMarkdownsCommand);
            _nopDbContext.ExecuteNonQuery(importMarkdownStartDatesFromStagingCommand);
            _nopDbContext.ExecuteNonQuery(importMarkdownEndDatesFromStagingCommand);
            productManager.Flush();
		}
	}
}