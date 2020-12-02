using Nop.Core;
using Nop.Plugin.Misc.AbcExportOrder.Extensions;
using Nop.Plugin.Misc.AbcExportOrder.Services;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using System;
using System.Linq;

namespace Nop.Plugin.Misc.AbcExportOrder.Tasks
{
    public class ResubmitOrdersTask : IScheduleTask
    {
        private readonly ICustomOrderService _customOrderService;
        private readonly ExportOrderSettings _settings;
        private readonly ILogger _logger;
        private readonly IProductService _productService;

        public ResubmitOrdersTask(
            ICustomOrderService orderService,
            ExportOrderSettings settings,
            ILogger logger,
            IProductService productService
            )
        {
            _customOrderService = orderService;
            _settings = settings;
            _logger = logger;
            _productService = productService;
        }

        public void Execute()
        {
            if (!_settings.IsValid)
            {
                throw new NopException("Unable to resubmit orders, export order settings not valid.");
            }

            var unsubmittedOrders = _customOrderService.GetUnsubmittedOrders();
            if (!unsubmittedOrders.Any())
            {
                return;
            }

            foreach (var order in unsubmittedOrders)
            {
                try
                {
                    order.SubmitToISAM();
                }
                catch (Exception e)
                {
                    _logger.Error($"Failure when resubmitting order #{order.Id}", e);
                    throw;
                }
            }
        }
    }
}
