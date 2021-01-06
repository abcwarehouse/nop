using Nop.Services.Plugins;
using Nop.Services.Common;
using Nop.Services.Tasks;
using Nop.Plugin.Misc.AbcMattresses.Tasks;
using Nop.Core.Domain.Tasks;
using Nop.Services.Catalog;
using Nop.Core.Domain.Catalog;
using System.Linq;
using Nop.Data;
using Nop.Plugin.Misc.AbcMattresses.Services;
using System;

namespace Nop.Plugin.Misc.AbcMattresses
{
    public class AbcMattressesPlugin : BasePlugin, IMiscPlugin
    {
        private readonly string TaskType =
            $"{typeof(UpdateMattressesTask).Namespace}.{typeof(UpdateMattressesTask).Name}, " +
            $"{typeof(UpdateMattressesTask).Assembly.GetName().Name}";

        private readonly IAbcMattressModelService _abcMattressModelService;
        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IScheduleTaskService _scheduleTaskService;
        private readonly INopDataProvider _nopDataProvider;

        public AbcMattressesPlugin(
            IAbcMattressModelService abcMattressModelService,
            IProductService productService,
            IProductAttributeService productAttributeService,
            IScheduleTaskService scheduleTaskService,
            INopDataProvider nopDataProvider
        )
        {
            _abcMattressModelService = abcMattressModelService;
            _productService = productService;
            _productAttributeService = productAttributeService;
            _scheduleTaskService = scheduleTaskService;
            _nopDataProvider = nopDataProvider;
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

            var productsToDelete = _abcMattressModelService.GetAllAbcMattressModels()
                                                           .Where(m => m.ProductId != null)
                                                           .Select(m => _productService.GetProductById(m.ProductId.Value))
                                                           .Where(p => !p.Deleted);
            foreach (var product in productsToDelete)
            {
                // create a random string for the Sku to satisfy the index
                product.Sku = new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789", 24)
                                .Select(s => s[new Random().Next(s.Length)]).ToArray());
                _productService.UpdateProduct(product);
            }
            _productService.DeleteProducts(productsToDelete.ToList());

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
                                                                         AbcMattressesConsts.IsBase(pa.Name) ||
                                                                         AbcMattressesConsts.IsMattressProtector(pa.Name) ||
                                                                         AbcMattressesConsts.IsFrame(pa.Name) ||
                                                                         pa.Name == AbcMattressesConsts.FreeGiftName)
                                                            .ToList();

            _productAttributeService.DeleteProductAttributes(productAttributes);
        }

        private void AddProductAttributes()
        {
            ProductAttribute[] productAttributes =
            {
                new ProductAttribute() { Name = AbcMattressesConsts.MattressSizeName },
                new ProductAttribute() { Name = AbcMattressesConsts.BaseNameTwin },
                new ProductAttribute() { Name = AbcMattressesConsts.BaseNameTwinXL },
                new ProductAttribute() { Name = AbcMattressesConsts.BaseNameFull },
                new ProductAttribute() { Name = AbcMattressesConsts.BaseNameQueen },
                new ProductAttribute() { Name = AbcMattressesConsts.BaseNameKing },
                new ProductAttribute() { Name = AbcMattressesConsts.BaseNameCaliforniaKing },
                new ProductAttribute() { Name = AbcMattressesConsts.MattressProtectorTwin },
                new ProductAttribute() { Name = AbcMattressesConsts.MattressProtectorTwinXL },
                new ProductAttribute() { Name = AbcMattressesConsts.MattressProtectorFull },
                new ProductAttribute() { Name = AbcMattressesConsts.MattressProtectorQueen },
                new ProductAttribute() { Name = AbcMattressesConsts.MattressProtectorKing },
                new ProductAttribute() { Name = AbcMattressesConsts.MattressProtectorCaliforniaKing },
                new ProductAttribute() { Name = AbcMattressesConsts.FrameTwin },
                new ProductAttribute() { Name = AbcMattressesConsts.FrameTwinXL },
                new ProductAttribute() { Name = AbcMattressesConsts.FrameFull },
                new ProductAttribute() { Name = AbcMattressesConsts.FrameQueen },
                new ProductAttribute() { Name = AbcMattressesConsts.FrameKing },
                new ProductAttribute() { Name = AbcMattressesConsts.FrameCaliforniaKing },
                new ProductAttribute() { Name = AbcMattressesConsts.FreeGiftName }
            };

            foreach (var productAttribute in productAttributes)
            {
                _productAttributeService.InsertProductAttribute(productAttribute);
            }
        }
    }
}