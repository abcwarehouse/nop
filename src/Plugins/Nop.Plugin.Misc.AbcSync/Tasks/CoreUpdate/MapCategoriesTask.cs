using Nop.Core;
using Nop.Data;
using Nop.Plugin.Misc.AbcSync.Data;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using OfficeOpenXml;
using System;
using System.IO;
using System.Linq;
using Nop.Plugin.Misc.AbcCore.Extensions;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    /// <summary>
    /// Will update staging categories based on the xslx mapping file provided
    /// </summary>
    public class MapCategoriesTask : Nop.Services.Tasks.IScheduleTask
    {
        private readonly ImportSettings _importSettings;
        private readonly INopDataProvider _nopDbContext;
        private readonly ICategoryService _categoryService;
        private readonly ILogger _logger;
        private readonly StagingDb _stagingDb;

        public MapCategoriesTask(
            ImportSettings importSettings,
            INopDataProvider nopDbContext,
            ICategoryService categoryService,
            ILogger logger,
            StagingDb stagingDb
            )
        {
            _importSettings = importSettings;
            _nopDbContext = nopDbContext;
            _categoryService = categoryService;
            _logger = logger;
            _stagingDb = stagingDb;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipMapCategoriesTask)
            {
                this.Skipped();
                return;
            }

            string mappingFile = _importSettings.GetCategoryMappingFile();
            using (var xlPackage = new ExcelPackage(new FileInfo(mappingFile)))
            {
                // get the first worksheet in the workbook
                var isamWorksheet = xlPackage.Workbook.Worksheets[0];
                if (isamWorksheet == null)
                    throw new NopException("No ISAM worksheet");

                _stagingDb.ExecuteNonQuery("ClearCategoryMapping");

                int iRow = 2;
                while (isamWorksheet.Cells[iRow, 1].Value != null && !string.IsNullOrEmpty(isamWorksheet.Cells[iRow, 1].Value.ToString()))
                {
                    var isamId = Convert.ToString(isamWorksheet.Cells[iRow, 1].Value);

                    int nopId = 0;
                    //use id if one is given
                    if (isamWorksheet.Cells[iRow, 4].Value != null && !string.IsNullOrEmpty(isamWorksheet.Cells[iRow, 4].Value.ToString()))
                    {
                        try
                        {
                            nopId = Convert.ToInt32(isamWorksheet.Cells[iRow, 4].Value);
                        }
                        catch (Exception ex)
                        {
                            throw new NopException($"Unable to convert cell {iRow},4 to an integer in {isamWorksheet.Name}", ex);
                        }
                    }
                    else
                    {
                        //else use the id of the category matching the given name (if it is given and one exists)
                        var nopName = Convert.ToString(isamWorksheet.Cells[iRow, 5].Value);
                        if (!string.IsNullOrEmpty(nopName))
                        {
                            var category = (await _categoryService.GetAllCategoriesAsync(categoryName: nopName, showHidden: true))
                                                           .Where(c => c.Name.ToLower().Trim() == nopName.ToLower().Trim()).FirstOrDefault();
                            if (category != null)
                            {
                                nopId = category.Id;
                            }
                            else
                            {
                                await _logger.WarningAsync($"No NopCommerce category exists with name \"{nopName}\", unable to map ISAM category with id = {isamId}");
                            }
                        }

                    }

                    _stagingDb.ExecuteNonQuery($"INSERT INTO [dbo].[ISAMCategory_NopCategory] ([ISAM_Id], [Nop_Id]) VALUES ({isamId}, {nopId})");
                    ++iRow;
                }
            }
        }
    }
}
