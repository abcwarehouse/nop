using Nop.Core.Domain.Catalog;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Services.Tasks;
using System.Linq;
using Nop.Plugin.Misc.AbcCore.Extensions;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcSync
{
    public class AddHomeDeliveryAttributesTask : IScheduleTask
    {
        private readonly INopDataProvider _nopDbContext;
        private readonly IImportUtilities _importUtilities;
        private readonly IRepository<ProductAttributeValue> _productAttributeValueRepository;
        private readonly IRepository<ProductAttributeMapping> _productAttributeMappingRepository;

        private readonly ImportSettings _importSettings;

        public AddHomeDeliveryAttributesTask(
            INopDataProvider nopDbContext,
            IImportUtilities importUtilities,
            IRepository<ProductAttributeValue> productAttributeValueRepository,
            IRepository<ProductAttributeMapping> productAttributeMappingRepository,
            ImportSettings importSettings
            )
        {
            _nopDbContext = nopDbContext;
            _importUtilities = importUtilities;
            _productAttributeValueRepository = productAttributeValueRepository;
            _productAttributeMappingRepository = productAttributeMappingRepository;
            _importSettings = importSettings;
        }

        public async System.Threading.Tasks.Task ExecuteAsync()
        {
            if (_importSettings.SkipAddHomeDeliveryAttributesTask)
            {
                this.Skipped();
                return;
            }

            this.LogStart();
            var homeDeliveryAttribute = _importUtilities.GetHomeDeliveryAttributeAsync();
            var homeDeliveryAttributeValue = _importUtilities.GetHomeDeliveryAttributeValueAsync();

            // clear all home delivery values
            string deleteHomeDeliveryAttributeValues =
                $"DELETE FROM {_nopDbContext.GetTable<ProductAttributeValue>().TableName}" +
                $" WHERE ProductAttributeMappingId in " +
                $"(SELECT Id FROM {_nopDbContext.GetTable<ProductAttributeMapping>().TableName} pam " +
                $"WHERE pam.ProductAttributeId = {homeDeliveryAttribute.Id});";

            await _nopDbContext.ExecuteNonQueryAsync(deleteHomeDeliveryAttributeValues);

            // add home delivery product attribute value to all home delivery products
            var attributeValueManager = new EntityManager<ProductAttributeValue>(_productAttributeValueRepository);
            var homeDeliveryAttributeMappings = _productAttributeMappingRepository.Table
                .Where(pam => pam.ProductAttributeId == homeDeliveryAttribute.Id)
                .Select(pam => pam.Id).ToList();
            foreach (var mapping in homeDeliveryAttributeMappings)
            {
                ProductAttributeValue pav = new ProductAttributeValue();
                pav.AttributeValueTypeId = 3; // checkboxes
                pav.Cost = 0;
                pav.DisplayOrder = 0;
                pav.IsPreSelected = homeDeliveryAttributeValue.IsPreSelected;
                pav.Name = homeDeliveryAttributeValue.Name;
                pav.ProductAttributeMappingId = mapping;
                pav.WeightAdjustment = 0;

                attributeValueManager.Insert(pav);
            }
            attributeValueManager.Flush();
            this.LogEnd();
        }
    }
}

