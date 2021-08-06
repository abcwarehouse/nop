using Nop.Core;
using Nop.Data;
using Nop.Services.Stores;
using Nop.Services.Tasks;
using System.Linq;
using Nop.Plugin.Misc.AbcCore.Extensions;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync.Tasks.CoreUpdate
{
    public class UnmapNonstockClearanceTask : IScheduleTask
    {
        private readonly INopDataProvider _nopDbContext;
        private readonly IStoreService _storeService;
        private readonly ImportSettings _settings;

        public UnmapNonstockClearanceTask(
            INopDataProvider nopDbContext,
            IStoreService storeService,
            ImportSettings settings)
        {
            _nopDbContext = nopDbContext;
            _storeService = storeService;
            _settings = settings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_settings.SkipUnmapNonstockClearanceTask)
            {
                this.Skipped();
                return;
            }

            

            var clearanceStores = (await _storeService.GetAllStoresAsync()).Where(s => s.Name.ToLower().Contains("clearance"));

            if (!clearanceStores.Any())
            {
                throw new NopException("No clearance store found to unmap products/categories from.");
            }

            var stagingDbName = _settings.GetStagingDbConnection().Database;
            foreach (var store in clearanceStores)
            {
                var clearanceStoreId = store.Id;
                var storeColumnPrefix = store.Name.ToLower().Contains("abc") ? "ABC" : "HAW";

                var productUnmapCommand = $@"
                DELETE FROM StoreMapping
                WHERE StoreId = {clearanceStoreId}
                AND EntityName = 'Product'
                AND EntityId IN
                (
                    SELECT id FROM product p
                    JOIN {stagingDbName}.[dbo].[Clearance_Items] ci on ci.Sku = p.Sku
                    WHERE ci.{storeColumnPrefix}_Clearance = 0
                    AND p.Published = 1
                    AND p.Deleted = 0
                )";

                var categoryUnmapCommand = $@"
                DELETE FROM StoreMapping
	            WHERE Id IN
	            (
		            SELECT Id FROM 
		            (
			            SELECT
			            MAX(sm.Id) AS Id, SUM((CASE WHEN sm2.Id IS NOT NULL THEN 1 ELSE 0 END)) AS ProductStoreMappingCount
			            FROM StoreMapping sm
			            JOIN Product_Category_Mapping pcm ON sm.EntityId = pcm.CategoryId AND sm.StoreId = {clearanceStoreId} AND sm.EntityName = 'Category'
			            JOIN Product p ON p.Id = pcm.ProductId
			            LEFT JOIN StoreMapping sm2 ON pcm.ProductId = sm2.EntityId AND sm2.EntityName = 'Product' AND sm2.StoreId = {clearanceStoreId}
			            WHERE p.Deleted = 0
			            GROUP BY sm.EntityId
		            ) AS MappingSummary
		            WHERE ProductStoreMappingCount = 0
	            )
                ";

                await _nopDbContext.ExecuteNonQueryAsync(productUnmapCommand);
                await _nopDbContext.ExecuteNonQueryAsync(categoryUnmapCommand);
            }

            
        }
    }
}