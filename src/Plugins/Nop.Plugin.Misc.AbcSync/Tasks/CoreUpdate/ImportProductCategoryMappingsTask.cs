using LinqToDB;
using LinqToDB.Data;
using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Services.Catalog;
using Nop.Services.Security;
using Nop.Services.Stores;
using Nop.Services.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcSync.Data;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync.Tasks.CoreUpdate
{
    public class ImportProductCategoryMappingsTask : IScheduleTask
    {
        private readonly ImportSettings _importSettings;
        private readonly IImportUtilities _importUtilities;
        private readonly INopDataProvider _nopDbContext;
        private readonly ICategoryService _categoryService;
        private readonly IAclService _aclService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IRepository<ProductCategory> _productCategoryRepository;
        private readonly IProductService _productService;

        public ImportProductCategoryMappingsTask(
            ImportSettings importSettings,
            IImportUtilities importUtilities,
            INopDataProvider nopDbContext,
            ICategoryService categoryService,
            IAclService aclService,
            IStoreMappingService storeMappingService,
            IRepository<ProductCategory> productCategoryRepository,
            IProductService productService
        )
        {
            _importSettings = importSettings;
            _importUtilities = importUtilities;
            _nopDbContext = nopDbContext;
            _categoryService = categoryService;
            _aclService = aclService;
            _storeMappingService = storeMappingService;
            _productCategoryRepository = productCategoryRepository;
            _productService = productService;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipImportProductCategoryMappingsTask)
            {
                this.Skipped();
                return;
            }

            

            SpecificationAttribute categoryAttribute = await _importUtilities.GetCategorySpecificationAttributeAsync();

            //1. clear category attribute options, we are going to re-import them
            string deleteCategoryAttributeOptionsSql
                = $@"DELETE FROM {_nopDbContext.GetTable<SpecificationAttributeOption>().TableName}
                     WHERE SpecificationAttributeId = {categoryAttribute.Id}";
            await _nopDbContext.ExecuteNonQueryAsync(deleteCategoryAttributeOptionsSql);

            //2. import new PCs (preserving pc display order and featured products)
            await ImportProductCategoryMappingsAsync();

            //3. remake the spec attribute options. after name is calculated in nop, send to sproc to create attributes and mappings
            var categoryList = await _categoryService.GetAllCategoriesAsync(showHidden: true);
            // was sorting here pre-4.40, but this was removed in NOP code.
            //var sortedCategories = _categoryService.SortCategoriesForTree(categoryList);
            var categoryToDisplayOrder = new Dictionary<int, int>();
            categoryToDisplayOrder = categoryList.Zip(Enumerable.Range(0, categoryList.Count), (c, i) => new { c.Id, i }).ToDictionary(x => x.Id, x => x.i);
            //specification attribute data
            foreach (var category in categoryList)
            {
                // build dictionary of category -> all category parents
                List<int> categoryBreadCrumbIds
                    = (await _categoryService.GetCategoryBreadCrumbAsync(category))
                    .Select(c => c.Id).ToList();

                // Categories might be syncing wrong - this allows for a case with no breadcrumbs
                if (!categoryBreadCrumbIds.Any())
                {
                    continue;
                }

                // get rid of top-level categories
                categoryBreadCrumbIds.RemoveAt(0);

                // add a specification attribute option for each category
                string spaces = "";
                for (int i = 0; i < (categoryBreadCrumbIds.Count - 1) * 4; ++i)
                {
                    spaces += " ";
                }

                //adding a new spec attr option for the category and mapping all products in the category to it
                var parameters = new DataParameter[] {
                    new DataParameter { Name = "specAttrId", DataType = DataType.Int32, Value = categoryAttribute.Id},
                    new DataParameter { Name = "categoryId", DataType = DataType.Int32, Value = category.Id},
                    new DataParameter { Name = "saoName", DataType = DataType.NVarChar, Value = spaces + category.Name},
                    new DataParameter { Name = "saoDisplayOrder", DataType = DataType.Int32, Value = categoryToDisplayOrder[category.Id] }
                };
                await _nopDbContext.ExecuteNonQueryAsync(
                    "EXEC [dbo].[AddCategorySpecificationAttributeOption] @specAttrId, @categoryId, @saoName, @saoDisplayOrder",
                    parameters
                );

            }

            //4. unpublish all products not mapped to a category
            await _nopDbContext.ExecuteNonQueryAsync("UPDATE p SET p.Published = 0 FROM Product p left join Product_Category_Mapping pcm ON p.Id = pcm.ProductId WHERE pcm.Id IS NULL;");


            //5. set the product store mappings recursively based on products
            await _nopDbContext.ExecuteNonQueryAsync(
                $"DELETE FROM StoreMapping where EntityName = 'Category'; UPDATE Category set LimitedToStores = 1;"
            );

            var categoryToStores = new Dictionary<int, HashSet<int>>();
            foreach (var pc in _productCategoryRepository.Table.ToList())
            {
                var categoryToMap = await _categoryService.GetCategoryByIdAsync(pc.CategoryId);
                var product = await _productService.GetProductByIdAsync(pc.ProductId);

                var productStoreMappings =
                    await _storeMappingService.GetStoreMappingsAsync(
                        product
                    );

                //for each store mapping, add it to all categories in the tree, up to root
                while (categoryToMap != null)
                {
                    if (!categoryToStores.ContainsKey(categoryToMap.Id))
                    {
                        categoryToStores[categoryToMap.Id] = new HashSet<int>();
                    }

                    foreach (var productStoreMapping in productStoreMappings)
                    {
                        categoryToStores[categoryToMap.Id].Add(productStoreMapping.StoreId);
                    }

                    categoryToMap = await _categoryService.GetCategoryByIdAsync(categoryToMap.ParentCategoryId);
                }
            }

            //Performing the insert of the new store mappings
            foreach (var kvp in categoryToStores)
            {
                foreach (var storeId in kvp.Value.ToList())
                {
                    await _nopDbContext.ExecuteNonQueryAsync(
                        $"INSERT INTO [dbo].[StoreMapping]([EntityId],[EntityName],[StoreId]) VALUES ({kvp.Key},'Category', {storeId});");
                }
            }

            
        }

        private async System.Threading.Tasks.Task ImportProductCategoryMappingsAsync()
        {
            var stagingDbName = _importSettings.GetStagingDbConnection().Database;
            var mattressItemAddition = _importSettings.SkipOldMattressesImport ?
                " WHERE ProductId NOT IN (SELECT ProductId FROM [AbcMattressModel])" :
                "";
            await _nopDbContext.ExecuteNonQueryAsync($@"
                If(OBJECT_ID('tempdb..#tempProductCategoryMap') Is Not Null)
                Begin
                    Drop Table #tempProductCategoryMap;
                End

                CREATE TABLE #tempProductCategoryMap (ProductId int, CategoryId int, IsFeatured bit not null, DisplayOrder int);
                INSERT INTO #tempProductCategoryMap SELECT DISTINCT ProductId, CategoryId, IsFeaturedProduct, DisplayOrder FROM Product_Category_Mapping;

                DECLARE @tPCM TABLE (pSku nvarchar(max) COLLATE SQL_Latin1_General_CP1_CS_AS, cId int);
                INSERT @tPCM EXEC {stagingDbName}.dbo.GetProductCategoryMappings;

                DELETE FROM [dbo].[Product_Category_Mapping] {mattressItemAddition};
                --inserting new product category mappings
                WITH TranslatedPCMs as (SELECT DISTINCT p.Id as prodId, c.Id as catId FROM Product p join @tPCM on  p.Sku = pSku join Category c on cId = c.Id),
                    PreserveredPCMs as (SELECT DISTINCT prodId, catId, ISNULL(IsFeatured, 0) as IsFeatured, ISNULL(DisplayOrder,0) as DisplayOrder FROM TranslatedPCMs left join #tempProductCategoryMap on prodId = ProductId AND catId = CategoryId)
                INSERT INTO Product_Category_Mapping (ProductId, CategoryId, IsFeaturedProduct, DisplayOrder)
                    SELECT prodId, catId, IsFeatured, DisplayOrder FROM PreserveredPCMs;

                Drop Table #tempProductCategoryMap;
            ");
        }
    }
}