using Nop.Core.Infrastructure;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcSync.Tasks.CoreUpdate;
using Nop.Services.Caching;
using Nop.Plugin.Misc.AbcCore;

namespace Nop.Plugin.Misc.AbcSync
{
    public class ContentUpdateTask : IScheduleTask
    {
        private readonly ILogger _logger;
        private readonly ImportSettings _importSettings;

        private readonly ImportDocumentsTask _importDocumentsTask;
        private readonly ImportIsamSpecsTask _importIsamSpecsTask;
        private readonly ImportFeaturedProductsTask _importFeaturedProductsTask;
        private readonly ImportProductFlagsTask _importProductFlagsTask;
        private readonly ImportSotPicturesTask _importSotPicturesTask;
        private readonly ImportLocalPicturesTask _importLocalPicturesTask;
        private readonly CleanDuplicateImagesTask _cleanDuplicateImagesTask;
        private readonly ClearCacheTask _clearCacheTask;

        public ContentUpdateTask(
            ILogger logger,
            ImportSettings importSettings,
            ImportDocumentsTask importDocumentsTask,
            ImportIsamSpecsTask importIsamSpecsTask,
            ImportFeaturedProductsTask importFeaturedProductsTask,
            ImportProductFlagsTask importProductFlagsTask,
            ImportSotPicturesTask importSotPicturesTask,
            ImportLocalPicturesTask importLocalPicturesTask,
            CleanDuplicateImagesTask cleanDuplicateImagesTask,
            ClearCacheTask clearCacheTask)
        {
            _logger = logger;
            _importSettings = importSettings;

            _importDocumentsTask = importDocumentsTask;
            _importIsamSpecsTask = importIsamSpecsTask;
            _importFeaturedProductsTask = importFeaturedProductsTask;
            _importProductFlagsTask = importProductFlagsTask;
            _importSotPicturesTask = importSotPicturesTask;
            _importLocalPicturesTask = importLocalPicturesTask;
            _cleanDuplicateImagesTask = cleanDuplicateImagesTask;
            _clearCacheTask = clearCacheTask;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            

            await _importDocumentsTask.ExecuteAsync();
            await _importIsamSpecsTask.ExecuteAsync();
            await _importFeaturedProductsTask.ExecuteAsync();
            await _importProductFlagsTask.ExecuteAsync();
            await _importSotPicturesTask.ExecuteAsync();
            await _importLocalPicturesTask.ExecuteAsync();
            await _cleanDuplicateImagesTask.ExecuteAsync();
            await _clearCacheTask.ExecuteAsync();

            if (!_importSettings.SkipSliExportTask)
            {
                await EngineContext.Current.Resolve<ISliExportTask>().ExecuteAsync();
            }
            else
            {
                await _logger.WarningAsync("SliExportTask skipped.");
            }

            
        }
    }
}
