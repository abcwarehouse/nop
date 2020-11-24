using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcCore.Models;
using SevenSpikes.Nop.Plugins.StoreLocator.Domain.Shops;

namespace Nop.Plugin.Misc.AbcCore.Services
{
    public interface IAttributeUtilities
    {
        string InsertHomeDeliveryAttribute(Product product, string attributes);
        string InsertPickupAttribute(Product product, StockResponse stockResponse, string attributes, Shop currentCsm = null);
        ProductAttributeMapping GetPickupAttributeMapping(string attributesXml);
        ProductAttributeMapping GetHomeDeliveryAttributeMapping(string attributesXml);
        ProductAttributeMapping GetWarrantyAttributeMapping(string attributesXml);
        string RemovePickupAttributes(string attributes);
        ProductAttributeMapping GetAttributeMappingByName(string attributesXml, string name);
    }
}