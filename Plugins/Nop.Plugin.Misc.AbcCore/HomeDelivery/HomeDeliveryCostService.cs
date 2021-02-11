using System.Linq;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Services.Shipping;

namespace Nop.Plugin.Misc.AbcCore.HomeDelivery
{
    public class HomeDeliveryCostService : IHomeDeliveryCostService
    {
        private readonly ICategoryService _categoryService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductService _productService;
        private readonly IProductAttributeParser _productAttributeParser;

        public HomeDeliveryCostService(
            ICategoryService categoryService,
            IProductAttributeService productAttributeService,
            IProductService productService,
            IProductAttributeParser productAttributeParser
        )
        {
            _categoryService = categoryService;
            _productAttributeService = productAttributeService;
            _productService = productService;
            _productAttributeParser = productAttributeParser;
        }

        public decimal GetHomeDeliveryCost(OrderItem orderItem)
        {
            return GetCost(orderItem.ProductId, orderItem.AttributesXml);
        }

        public decimal GetHomeDeliveryCost(GetShippingOptionRequest.PackageItem packageItem)
        {
            return GetCost(packageItem.Product.Id, packageItem.ShoppingCartItem.AttributesXml);
        }

        private decimal GetCost(int productId, string attributesXml)
        {
            var categories = _categoryService.GetProductCategoriesByProductId(productId)
                                                    .Select(pc => _categoryService.GetCategoryById(pc.CategoryId));
            if (categories.Any(c => new string[]
                { "recliners", "lift chairs", "massage chairs" }.Contains(c.Name.ToLower()))
            )
            {
                return 49.00M;
            }

            // Since that Package Item doens't carry the actual item 
            if (categories.Any(c => new string[]
                { "twin", "twin extra long", "full", "queen", "king", "california king" }
                .Contains(c.Name.ToLower()))
            )
            {
                return GetMattressCost(attributesXml);
            }

            // default
            return 14.75M;
        }

        private decimal GetMattressCost(string attributesXml)
        {
            var mattressProductAttributeIds =
                _productAttributeService.GetAllProductAttributes()
                .Where(pa => pa.Name.Contains("Mattress Size") || pa.Name.Contains("Base ("))
                .Select(pa => pa.Id);

            var mattressItemCost =
                _productAttributeParser.ParseProductAttributeMappings(attributesXml)
                .Where(pam => mattressProductAttributeIds.Contains(pam.ProductAttributeId))
                .Select(pam => _productAttributeParser.ParseValues(attributesXml, pam.Id).FirstOrDefault())
                .Select(idasstring => _productAttributeService.GetProductAttributeValueById(int.Parse(idasstring)))
                .Sum(pav => pav.PriceAdjustment);

            return mattressItemCost >= 697.00M ?
                0 :
                99M;
        }
    }
}
