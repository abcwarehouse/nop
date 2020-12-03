using Nop.Services.Plugins;
using Nop.Services.Common;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcMattresses.Tasks;
using Nop.Core.Domain.Tasks;
using Nop.Services.Catalog;
using Nop.Core.Domain.Catalog;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses
{
    public class AbcMattressesPlugin : BasePlugin, IMiscPlugin
    {
        private readonly string TaskType =
            $"{typeof(UpdateMattressesTask).Namespace}.{typeof(UpdateMattressesTask).Name}, " +
            $"{typeof(UpdateMattressesTask).Assembly.GetName().Name}";

        private readonly IProductAttributeService _productAttributeService;
        private readonly IScheduleTaskService _scheduleTaskService;

        public AbcMattressesPlugin(
            IProductAttributeService productAttributeService,
            IScheduleTaskService scheduleTaskService
        )
        {
            _productAttributeService = productAttributeService;
            _scheduleTaskService = scheduleTaskService;
        }

        public override void Install()
        {
            RemoveTasks();
            AddTask();

            RemoveProductAttributes();
            AddProductAttributes();

            base.Install();
        }

        public override void Uninstall()
        {
            RemoveTasks();
            RemoveProductAttributes();

            base.Uninstall();
        }
        private void AddTask()
        {
            ScheduleTask task = new ScheduleTask();
            task.Name = $"Update Mattresses";
            task.Seconds = 14400;
            task.Type = TaskType;
            task.Enabled = false;
            task.StopOnError = false;

            _scheduleTaskService.InsertTask(task);
        }

        private void RemoveTasks()
        {
            var task = _scheduleTaskService.GetTaskByType(TaskType);
            if (task != null)
            {
                _scheduleTaskService.DeleteTask(task);
            }
        }

        private void RemoveProductAttributes()
        {
            var productAttributes = _productAttributeService.GetAllProductAttributes()
                                                            .Where(pa => pa.Name == AbcMattressesConsts.MattressSizeName ||
                                                                         pa.Name == AbcMattressesConsts.BaseName ||
                                                                         pa.Name == AbcMattressesConsts.FreeGiftName).ToList();

            _productAttributeService.DeleteProductAttributes(productAttributes);
        }

        private void AddProductAttributes()
        {
            ProductAttribute[] productAttributes =
            {
                new ProductAttribute() { Name = AbcMattressesConsts.MattressSizeName },
                new ProductAttribute() { Name = AbcMattressesConsts.BaseName },
                new ProductAttribute() { Name = AbcMattressesConsts.FreeGiftName }
            };

            foreach (var productAttribute in productAttributes)
            {
                _productAttributeService.InsertProductAttribute(productAttribute);
            }
        }
    }
}