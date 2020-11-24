using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Stores;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcCore.Models;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Stores;
using SevenSpikes.Nop.Plugins.StoreLocator.Domain.Shops;
using SevenSpikes.Nop.Plugins.StoreLocator.Services;
using System.Linq;

namespace Nop.Plugin.Misc.AbcCore.Services
{
    public class AttributeUtilities : IAttributeUtilities
    {
        public readonly string PICKUP_ATTRIBUTE_NAME = "Pickup";
        public readonly string PICKUP_MSG_ATTRIBUTE_NAME = "Pickup Message";
        public readonly string HOME_DELIVERY_ATTRIBUTE_NAME = "Home Delivery";
        public readonly string WARRANTY_ATTRIBUTE_NAME = "Warranty";

        public readonly string HOME_DELIVERY_MESSAGE_ABC = "This item will be delivered to you by ABC";
        public readonly string HOME_DELIVERY_MESSAGE_HAWTHORNE = "This item will be delivered to you by Hawthorne";

        ISettingService _settingService;
        IShopService _shopService;
        IProductAttributeService _productAttributeService;
        IProductAttributeParser _productAttributeParser;

        private readonly IStoreService _storeService;

        IRepository<CustomerShopMapping> _customerShopMappingRepository;

        IWorkContext _workContext;

        public AttributeUtilities(ISettingService settingService,
            IShopService shopService,
            IProductAttributeService productAttributeService,
            IProductAttributeParser productAttributeParser,
            IRepository<CustomerShopMapping> customerShopMappingRepository,
            IWorkContext workContext,
            IStoreService storeService)
        {
            _settingService = settingService;
            _shopService = shopService;
            _productAttributeService = productAttributeService;
            _productAttributeParser = productAttributeParser;
            _customerShopMappingRepository = customerShopMappingRepository;
            _workContext = workContext;
            _storeService = storeService;
        }

        public string InsertPickupAttribute(
            Product product,
            StockResponse stockResponse,
            string attributes,
            Shop customerShop = null
        )
        {
            Shop shop = customerShop;

            if (shop == null)
            {
                CustomerShopMapping csm = _customerShopMappingRepository.Table
                    .Where(c => c.CustomerId == _workContext.CurrentCustomer.Id)
                    .Select(c => c).FirstOrDefault();

                // get the customer shop mapping, get the latest information about the store customer selected
                shop = _shopService.GetShopById(csm.ShopId);
            }

            string shopName;

            if (shop == null)
            {
                // shop is not in the database, choose 'select store'
                shopName = "Select Store";
            }
            else
            {
                shopName = shop.Name;
            }

            ProductAttribute pickupAttribute = GetPickupAttribute();

            // current product potential attribute mappings
            ProductAttributeMapping pickupAttributeMapping =
                _productAttributeService.GetProductAttributeMappingsByProductId(product.Id)
                .Where(pam => pam.ProductAttributeId == pickupAttribute.Id)
                .Select(pam => pam).FirstOrDefault();

            if (pickupAttributeMapping != null)
            {
                string pickupMsg = stockResponse.ProductStocks
                    .Where(ps => ps.Shop.Id == shop.Id)
                    .Select(ps => ps.Message).FirstOrDefault();

                attributes = _productAttributeParser.AddProductAttribute(attributes, pickupAttributeMapping, shop.Name + "\nAvailable: " + pickupMsg);

                // remove home delivery attributes
                var homeDeliveryAttributeMapping = GetHomeDeliveryAttributeMapping(attributes);

                if (homeDeliveryAttributeMapping != null)
                {
                    attributes = _productAttributeParser.RemoveProductAttribute(attributes, homeDeliveryAttributeMapping);
                }
            }
            return attributes;
        }

        public string InsertHomeDeliveryAttribute(Product product, string attributes)
        {
            // get home delivery table
            var hdProductAttribute = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id)
                .Where(pam =>
                    _productAttributeService.GetProductAttributeById(pam.ProductAttributeId) != null &&
                    _productAttributeService.GetProductAttributeById(pam.ProductAttributeId).Name == HOME_DELIVERY_ATTRIBUTE_NAME)
                .Select(pam => pam).FirstOrDefault();
            if (hdProductAttribute != null)
            {
                string homeDeliveryMessage = DetermineHomeDeliveryMessage();

                attributes = _productAttributeParser.AddProductAttribute(attributes, hdProductAttribute,
                    homeDeliveryMessage);
            }
            // remove pickup attributes
            attributes = RemovePickupAttributes(attributes);
            return attributes;
        }

        public ProductAttributeMapping GetPickupAttributeMapping(string attributesXml)
        {
            return GetAttributeMappingByName(attributesXml, PICKUP_ATTRIBUTE_NAME);
        }

        public ProductAttributeMapping GetPickupMsgAttributeMapping(string attributesXml)
        {
            return GetAttributeMappingByName(attributesXml, PICKUP_MSG_ATTRIBUTE_NAME);
        }

        public ProductAttributeMapping GetHomeDeliveryAttributeMapping(string attributesXml)
        {
            return GetAttributeMappingByName(attributesXml, HOME_DELIVERY_ATTRIBUTE_NAME);
        }

        public ProductAttributeMapping GetWarrantyAttributeMapping(string attributesXml)
        {
            return GetAttributeMappingByName(attributesXml, WARRANTY_ATTRIBUTE_NAME);
        }

        public string RemovePickupAttributes(string attributes)
        {
            var pickupAttributeMapping = GetPickupAttributeMapping(attributes);

            if (pickupAttributeMapping != null)
            {
                attributes = _productAttributeParser.RemoveProductAttribute(attributes, pickupAttributeMapping);
            }

            var pickupMsgAttributeMapping = GetPickupMsgAttributeMapping(attributes);
            if (pickupMsgAttributeMapping != null)
            {
                attributes = _productAttributeParser.RemoveProductAttribute(attributes, pickupMsgAttributeMapping);
            }

            return attributes;
        }

        public ProductAttributeMapping GetAttributeMappingByName(string attributesXml, string name)
        {
            return _productAttributeParser.ParseProductAttributeMappings(attributesXml)
                .Where(pam => _productAttributeService.GetProductAttributeById(pam.ProductAttributeId)?.Name == name)
                .Select(pam => pam)
                .SingleOrDefault();
        }

        private string DetermineHomeDeliveryMessage()
        {
            Store[] storeList = _storeService.GetAllStores().ToArray();
            Store abcWarehouseStore
                = storeList.Where(s => s.Name == "ABC Warehouse")
                .Select(s => s).FirstOrDefault();
            Store abcClearanceStore
                = storeList.Where(s => s.Name == "ABC Clearance")
                .Select(s => s).FirstOrDefault();
            Store hawthorneStore
                = storeList.Where(s => s.Name == "Hawthorne Online")
                .Select(s => s).FirstOrDefault();
            Store hawthorneClearanceStore
                = storeList.Where(s => s.Name == "Hawthorne Clearance")
                .Select(s => s).FirstOrDefault();

            if (abcWarehouseStore != null || abcClearanceStore != null)
            {
                return HOME_DELIVERY_MESSAGE_ABC;
            }

            if (hawthorneStore != null || hawthorneClearanceStore != null)
            {
                return HOME_DELIVERY_MESSAGE_HAWTHORNE;
            }

            return "";
        }

        // this adds the pickup in store attribute to the attribute string
        private ProductAttribute GetPickupAttribute()
        {
            ProductAttribute pickupAttribute = _productAttributeService.GetAllProductAttributes()
                .Where(pa => pa.Name == PICKUP_ATTRIBUTE_NAME)
                .Select(pa => pa).SingleOrDefault();

            if (pickupAttribute == null)
            {
                pickupAttribute = new ProductAttribute();
                pickupAttribute.Name = PICKUP_ATTRIBUTE_NAME;
                _productAttributeService.InsertProductAttribute(pickupAttribute);
            }

            return pickupAttribute;
        }
    }
}
