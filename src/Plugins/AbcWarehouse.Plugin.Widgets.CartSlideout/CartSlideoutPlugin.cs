using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Tasks;
using Nop.Services.Catalog;
using Nop.Services.Cms;
using Nop.Services.Plugins;
using Nop.Services.Tasks;
using Nop.Web.Framework.Infrastructure;

namespace AbcWarehouse.Plugin.Widgets.CartSlideout
{
    public class CartSlideoutPlugin : BasePlugin, IWidgetPlugin
    {
        private readonly string _taskType =
            $"{typeof(UpdateDeliveryOptionsTask).FullName}, {typeof(CartSlideoutPlugin).Namespace}";

        private readonly IProductAttributeService _productAttributeService;
        private readonly IScheduleTaskService _scheduleTaskService;

        public CartSlideoutPlugin(
            IProductAttributeService productAttributeService,
            IScheduleTaskService scheduleTaskService)
        {
            _productAttributeService = productAttributeService;
            _scheduleTaskService = scheduleTaskService;
        }

        public bool HideInWidgetList => false;

        public string GetWidgetViewComponentName(string widgetZone)
        {
            return "CartSlideout";
        }

        public System.Threading.Tasks.Task<IList<string>> GetWidgetZonesAsync()
        {
            return System.Threading.Tasks.Task.FromResult<IList<string>>(new List<string> { PublicWidgetZones.BodyStartHtmlTagAfter });
        }

        public override async System.Threading.Tasks.Task InstallAsync()
        {
            await RemoveTaskAsync();
            await AddTaskAsync();

            await RemoveProductAttributes();
            await AddProductAttributes();
        }

        public override async System.Threading.Tasks.Task UninstallAsync()
        {
            await RemoveTaskAsync();
            await RemoveProductAttributes();
        }

        private async System.Threading.Tasks.Task AddTaskAsync()
        {
            ScheduleTask task = new ScheduleTask
            {
                Name = $"Update Delivery Options",
                Seconds = 14400,
                Type = _taskType,
                Enabled = true,
                StopOnError = false,
            };

            await _scheduleTaskService.InsertTaskAsync(task);
        }

        private async System.Threading.Tasks.Task RemoveTaskAsync()
        {
            var task = await _scheduleTaskService.GetTaskByTypeAsync(_taskType);
            if (task != null)
            {
                await _scheduleTaskService.DeleteTaskAsync(task);
            }
        }

        private async System.Threading.Tasks.Task RemoveProductAttributes()
        {
            var attributes = (await _productAttributeService.GetAllProductAttributesAsync()).Where(pa =>
                pa.Name == CartSlideoutConsts.DeliveryPickupOptions ||
                pa.Name == CartSlideoutConsts.HaulAwayDelivery ||
                pa.Name == CartSlideoutConsts.HaulAwayDeliveryInstall);

            foreach (var attribute in attributes)
            {
                await _productAttributeService.DeleteProductAttributeAsync(attribute);
            }
        }

        private async System.Threading.Tasks.Task AddProductAttributes()
        {
            var pas = new ProductAttribute[]
            {
                new ProductAttribute() { Name = CartSlideoutConsts.DeliveryPickupOptions },
                new ProductAttribute() { Name = CartSlideoutConsts.HaulAwayDelivery },
                new ProductAttribute() { Name = CartSlideoutConsts.HaulAwayDeliveryInstall },
            };

            foreach (var pa in pas)
            {
                await _productAttributeService.InsertProductAttributeAsync(pa);
            }
        }
    }
}
