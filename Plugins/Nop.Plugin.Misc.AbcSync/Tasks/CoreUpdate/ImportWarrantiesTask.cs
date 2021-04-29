using Nop.Data;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcSync
{
    class ImportWarrantiesTask : IScheduleTask
    {
        private readonly INopDataProvider _nopDbContext;
        private readonly ImportSettings _importSettings;

        public ImportWarrantiesTask(
            INopDataProvider nopDbContext,
            ImportSettings importSettings)
        {
            _nopDbContext = nopDbContext;
            _importSettings = importSettings;
        }

        public void Execute()
        {
            if (_importSettings.SkipImportWarrantiesTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();

            var stagingDbName = _importSettings.GetStagingDbConnection().Database;
            var command = $@"
				MERGE ProductAttribute AS T
				USING (SELECT DISTINCT [Description] FROM {stagingDbName}.dbo.WarrantyGroup) AS S
				ON (T.[Description] = S.[Description]) 
				WHEN NOT MATCHED BY SOURCE AND (T.Name = 'Warranty')
					THEN DELETE
				WHEN NOT MATCHED BY TARGET
					THEN INSERT([Name], [Description]) VALUES('Warranty', S.[Description]);
				--OUTPUT $action, inserted.*, deleted.*;

				--Merge warranty items 
				MERGE PredefinedProductAttributeValue AS T
				USING (SELECT DISTINCT wi.Name, wi.PriceAdjustment, pa.Id as ProdAttrId
						FROM {stagingDbName}.dbo.WarrantyItem wi left join {stagingDbName}.dbo.WarrantyGroup wg on wi.WarrantyGroupCode = wg.WarrantyGroupCode
							left join ProductAttribute pa on wg.[Description] = pa.[Description]) AS S
				ON (T.[ProductAttributeId] = S.[ProdAttrId] AND T.[Name] = S.[Name])
				WHEN MATCHED
					THEN UPDATE SET T.PriceAdjustment = S.PriceAdjustment
				WHEN NOT MATCHED BY TARGET
					THEN INSERT(ProductAttributeId, [Name], PriceAdjustment, WeightAdjustment, Cost, IsPreSelected, DisplayOrder, PriceAdjustmentUsePercentage) VALUES (S.ProdAttrId, S.[Name], S.[PriceAdjustment],0,0,0,1,0);
				--OUTPUT $action, inserted.*, deleted.*;

				--Clear and repopulate WarrantySku
				TRUNCATE TABLE WarrantySku;
				INSERT INTO WarrantySku (Name, Sku) SELECT DISTINCT Name, WarrantyItemSku from {stagingDbName}.dbo.WarrantyItem;

				-- Clear product warranty mappings
				-- will need to move this back into merge when we move from CoreUpdate
				DELETE ppm FROM Product_ProductAttribute_Mapping ppm
				JOIN ProductAttribute pa ON pa.Id = ppm.ProductAttributeId
				WHERE pa.Name = 'Warranty'

				--Import product warranty mappings
				MERGE Product_ProductAttribute_Mapping AS T
				USING (SELECT DISTINCT p.Id as ProdId, pa.Id as ProdAttrId
						FROM {stagingDbName}.dbo.ProductWarrantyGroupMapping pwgm left join {stagingDbName}.dbo.WarrantyGroup wg on pwgm.WarrantyGroupCode = wg.WarrantyGroupCode
							left join ProductAttribute pa on wg.[Description] = pa.[Description] left join Product p on pwgm.ProductSku = p.Sku WHERE p.Id IS NOT NULL) AS S
				ON (T.ProductId = S.ProdId AND T.ProductAttributeId = S.ProdAttrId) 
				WHEN NOT MATCHED BY TARGET
					THEN INSERT(ProductId, ProductAttributeId, IsRequired, AttributeControlTypeId, DisplayOrder) 
					VALUES (S.ProdId, S.ProdAttrId, 0, 2, 1);
				--OUTPUT $action, inserted.*, deleted.*;

				--Add product attribute values
				MERGE ProductAttributeValue AS T
				USING (SELECT DISTINCT ppam.Id as MappingId, prepav.* FROM (SELECT * FROM ProductAttribute WHERE Name = 'Warranty') pa join PredefinedProductAttributeValue prepav on prepav.ProductAttributeId = pa.Id join Product_ProductAttribute_Mapping ppam on pa.Id = ppam.ProductAttributeId) AS S
				ON (T.Name = S.Name AND T.ProductAttributeMappingId = S.MappingId)
				WHEN MATCHED 
					THEN UPDATE SET T.PriceAdjustment = S.PriceAdjustment, T.WeightAdjustment = S.WeightAdjustment, T.IsPreSelected = S.IsPreSelected, T.DisplayOrder = S.DisplayOrder
				WHEN NOT MATCHED BY TARGET
					THEN INSERT(ProductAttributeMappingId,AttributeValueTypeId,AssociatedProductId, Name, PriceAdjustment,WeightAdjustment,Cost,Quantity,IsPreSelected,DisplayOrder,PictureId, ImageSquaresPictureId, CustomerEntersQty, PriceAdjustmentUsePercentage) 
					VALUES (S.MappingId, 0, 0, S.Name, S.PriceAdjustment, S.WeightAdjustment, S.Cost, 0 , S.IsPreSelected, S.DisplayOrder, 0, 0, 0, 0);
				--OUTPUT $action, inserted.*, deleted.*;
			";

            _nopDbContext.ExecuteNonQuery(command);

            this.LogEnd();
        }
    }
}
