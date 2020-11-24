using Nop.Data;
using Nop.Plugin.Misc.AbcCore.Services;

namespace Nop.Plugin.Misc.AbcSync
{
    class ImportRelatedProducts : BaseAbcWarehouseService, IImportRelatedProducts
	{
        private readonly INopDataProvider _nopDbContext;
        private readonly IImportUtilities _importUtilities;

        public ImportRelatedProducts(
			INopDataProvider nopDbContext,
            IImportUtilities importUtilities
		)
		{
            _nopDbContext = nopDbContext;
            _importUtilities = importUtilities;
		}

		/// <summary>
		///		Begin the import process for product's specifications.
		/// </summary>
		public void Import()
		{
            _nopDbContext.ExecuteNonQuery("EXECUTE [dbo].[ImportRelatedProducts];");
        }
	}
}