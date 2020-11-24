using Nop.Core.Infrastructure;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcSync.Tasks.CoreUpdate;
using Nop.Services.Caching;

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

        public void Execute()
        {
            this.LogStart();

            EngineContext.Current.Resolve<ImportDocumentsTask>().Execute();
            EngineContext.Current.Resolve<ImportIsamSpecsTask>().Execute();
            EngineContext.Current.Resolve<ImportFeaturedProductsTask>().Execute();
            EngineContext.Current.Resolve<ImportProductFlagsTask>().Execute();
            EngineContext.Current.Resolve<ImportSotPicturesTask>().Execute();
            EngineContext.Current.Resolve<ImportLocalPicturesTask>().Execute();
            EngineContext.Current.Resolve<UnmapEmptyCategoriesTask>().Execute();
            EngineContext.Current.Resolve<ClearCacheTask>().Execute();

            this.LogEnd();
        }
    }
}
