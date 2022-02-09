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
        private readonly ImportFeaturedProductsTask _importFeaturedProductsTask;
        private readonly ImportProductFlagsTask _importProductFlagsTask;
        private readonly ImportLocalPicturesTask _importLocalPicturesTask;
        private readonly CleanDuplicateImagesTask _cleanDuplicateImagesTask;
        private readonly UpdateMetaTagsTask _updateMetaTagsTask;
        private readonly ClearCacheTask _clearCacheTask;

        public ContentUpdateTask(
            ILogger logger,
            ImportSettings importSettings,
            ImportDocumentsTask importDocumentsTask,
            ImportFeaturedProductsTask importFeaturedProductsTask,
            ImportProductFlagsTask importProductFlagsTask,
            ImportLocalPicturesTask importLocalPicturesTask,
            CleanDuplicateImagesTask cleanDuplicateImagesTask,
            UpdateMetaTagsTask updateMetaTagsTask,
            ClearCacheTask clearCacheTask)
        {
            _logger = logger;
            _importSettings = importSettings;

            _importDocumentsTask = importDocumentsTask;
            _importFeaturedProductsTask = importFeaturedProductsTask;
            _importProductFlagsTask = importProductFlagsTask;
            _importLocalPicturesTask = importLocalPicturesTask;
            _cleanDuplicateImagesTask = cleanDuplicateImagesTask;
            _updateMetaTagsTask = updateMetaTagsTask;
            _clearCacheTask = clearCacheTask;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            await _importDocumentsTask.ExecuteAsync();
            await _importFeaturedProductsTask.ExecuteAsync();
            await _importProductFlagsTask.ExecuteAsync();
            await _importLocalPicturesTask.ExecuteAsync();
            await _cleanDuplicateImagesTask.ExecuteAsync();
            await _updateMetaTagsTask.ExecuteAsync();
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
