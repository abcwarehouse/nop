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

            this.LogStart();

            string mappingFile = _importSettings.GetCategoryMappingFile();
            using (var xlPackage = new ExcelPackage(new FileInfo(mappingFile)))
            {
                // get the first worksheet in the workbook
                var isamWorksheet = xlPackage.Workbook.Worksheets[0];
                if (isamWorksheet == null)
                    throw new NopException("No ISAM worksheet");

                var sotWorksheet = xlPackage.Workbook.Worksheets[1];
                if (sotWorksheet == null)
                    throw new NopException("No Site on Time Worksheet");

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

                iRow = 2;
                while (sotWorksheet.Cells[iRow, 1].Value != null && !string.IsNullOrEmpty(sotWorksheet.Cells[iRow, 1].Value.ToString()))
                {
                    var sotId = Convert.ToInt32(sotWorksheet.Cells[iRow, 1].Value);

                    int nopId = 0;
                    //use id if one is given
                    if (sotWorksheet.Cells[iRow, 5].Value != null && !string.IsNullOrEmpty(sotWorksheet.Cells[iRow, 5].Value.ToString()))
                    {
                        try
                        {
                            nopId = Convert.ToInt32(sotWorksheet.Cells[iRow, 5].Value);
                        }
                        catch (Exception ex)
                        {
                            throw new NopException($"Unable to convert cell {iRow},5 to an integer in {sotWorksheet.Name}", ex);
                        }
                    }
                    else
                    {
                        //else use the id of the category matching the given name (if it is given and one exists)
                        var nopName = Convert.ToString(sotWorksheet.Cells[iRow, 6].Value);
                        if (!string.IsNullOrEmpty(nopName))
                        {
                            var category = (await _categoryService.GetAllCategoriesAsync(categoryName: nopName, showHidden: true))
                                .Where(c => c.Name.ToLower().Trim() == nopName.ToLower().Trim())
                                .FirstOrDefault();
                            if (category != null)
                            {
                                if (category.Deleted)
                                {
                                    await _logger.WarningAsync($"Site on time category with id {sotId} has been mapped to a deleted category (name = {nopName}, id = {nopId})");
                                }
                                nopId = category.Id;
                            }
                            else
                            {
                                await _logger.WarningAsync($"No NopCommerce category exists with name \"{nopName}\", unable to map Site on Time category with id = {sotId}");
                            }
                        }

                    }

                    _stagingDb.ExecuteNonQuery($"INSERT INTO [dbo].[SoTCategory_NopCategory] ([SoT_Id], [Nop_Id]) VALUES ({sotId}, {nopId})");
                    ++iRow;
                }
            }

            this.LogEnd();
        }
    }
}
