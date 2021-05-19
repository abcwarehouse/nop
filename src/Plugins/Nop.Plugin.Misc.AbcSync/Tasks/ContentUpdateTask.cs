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

        public ContentUpdateTask(
            ILogger logger,
            ImportSettings importSettings)
        {
            _logger = logger;
            _importSettings = importSettings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            this.LogStart();

            await EngineContext.Current.Resolve<ImportDocumentsTask>().ExecuteAsync();
            await EngineContext.Current.Resolve<ImportIsamSpecsTask>().ExecuteAsync();
            await EngineContext.Current.Resolve<ImportFeaturedProductsTask>().ExecuteAsync();
            await EngineContext.Current.Resolve<ImportProductFlagsTask>().ExecuteAsync();
            await EngineContext.Current.Resolve<ImportSotPicturesTask>().ExecuteAsync();
            await EngineContext.Current.Resolve<ImportLocalPicturesTask>().ExecuteAsync();
            await EngineContext.Current.Resolve<CleanDuplicateImagesTask>().ExecuteAsync();
            await EngineContext.Current.Resolve<ClearCacheTask>().ExecuteAsync();

            if (!_importSettings.SkipSliExportTask)
            {
                await EngineContext.Current.Resolve<ISliExportTask>().ExecuteAsync();
            }
            else
            {
                await _logger.WarningAsync("SliExportTask skipped.");
            }



            this.LogEnd();
        }
    }
}
