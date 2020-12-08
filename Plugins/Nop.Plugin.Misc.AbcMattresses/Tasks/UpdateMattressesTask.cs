using Nop.Services.Logging;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcMattresses.Services;

namespace Nop.Plugin.Misc.AbcMattresses.Tasks
{
    public class UpdateMattressesTask : IScheduleTask
    {
        private readonly ILogger _logger;

        private readonly IAbcMattressService _abcMattressService;
        private readonly IAbcMattressProductService _abcMattressProductService;

        public UpdateMattressesTask(
            ILogger logger,
            IAbcMattressService abcMattressService,
            IAbcMattressProductService abcMattressProductService
        )
        {
            _logger = logger;
            _abcMattressService = abcMattressService;
            _abcMattressProductService = abcMattressProductService;
        }

        public void Execute()
        {
            var models = _abcMattressService.GetAllAbcMattressModels();

            foreach (var model in models)
            {
                _abcMattressProductService.UpsertAbcMattressProduct(model);
            }
        }
    }
}
