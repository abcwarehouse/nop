using System.Collections.Generic;
using System.Linq;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Misc.AbcCore.Extensions;
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
        private readonly IProductAttributeFormatter _productAttributeFormatter;

        public HomeDeliveryCostService(
            ICategoryService categoryService,
            IProductAttributeService productAttributeService,
            IProductService productService,
            IProductAttributeParser productAttributeParser,
            IProductAttributeFormatter productAttributeFormatter
        )
        {
            _categoryService = categoryService;
            _productAttributeService = productAttributeService;
            _productService = productService;
            _productAttributeParser = productAttributeParser;
            _productAttributeFormatter = productAttributeFormatter;
        }

        public decimal GetHomeDeliveryCost(IList<OrderItem> orderItems)
        {
            var result = 0M;
            // skip any mattresses after finding one
            var skipMattress = false;

            foreach (var orderItem in orderItems.OrderByDescending(oi => oi.PriceExclTax))
            {
                var isMattress = orderItem.GetMattressSize() != null;
                if (isMattress && skipMattress) { continue; }

                result += GetHomeDeliveryCostOfItem(orderItem);

                if (isMattress) { skipMattress = true; }
            }

            return result;
        }

        public decimal GetHomeDeliveryCost(IList<GetShippingOptionRequest.PackageItem> packageItems)
        {
            var result = 0M;
            // skip any mattresses after finding one
            var skipMattress = false;

            foreach (var packageItem in packageItems.OrderByDescending(
                pi => GetMattressItemCost(pi.Product.Id, pi.ShoppingCartItem.AttributesXml)
              )
            )
            {
                var productAttributes = _productAttributeFormatter.FormatAttributes(
                    packageItem.Product, packageItem.ShoppingCartItem.AttributesXml
                );
                var isMattress =
                    productAttributes.Contains("Mattress Size:");
                if (isMattress && skipMattress) { continue; }

                result += GetHomeDeliveryCostOfItem(packageItem);

                if (isMattress) { skipMattress = true; }
            }

            return result;
        }

        private decimal GetHomeDeliveryCostOfItem(OrderItem orderItem)
        {
            return GetCost(orderItem.ProductId, orderItem.AttributesXml, orderItem.Quantity);
        }

        private decimal GetHomeDeliveryCostOfItem(GetShippingOptionRequest.PackageItem packageItem)
        {
            return GetCost(packageItem.Product.Id, packageItem.ShoppingCartItem.AttributesXml, packageItem.ShoppingCartItem.Quantity);
        }

        private decimal GetCost(int productId, string attributesXml, int quantity)
        {
            var categories = _categoryService.GetProductCategoriesByProductId(productId)
                                                    .Select(pc => _categoryService.GetCategoryById(pc.CategoryId));
            if (categories.Any(c => new string[]
                { "recliners", "lift chairs", "massage chairs" }.Contains(c.Name.ToLower()))
            )
            {
                return 49.00M * quantity;
            }

            // Since that Package Item doesn't carry the actual item 
            if (categories.Any(c => new string[]
                { "twin", "twin extra long", "full", "queen", "king", "california king" }
                .Contains(c.Name.ToLower()))
            )
            {
                // Mattresses do not include quantity
                return GetMattressHomeDeliveryCost(attributesXml, productId);
            }

            // default
            return 14.75M * quantity;
        }

        private decimal GetMattressHomeDeliveryCost(string attributesXml, int productId)
        {
            return GetMattressItemCost(productId, attributesXml) >= 697.00M ?
                0 :
                99M;
        }

        // If non-mattress passed in, you'll get the normal price
        private decimal GetMattressItemCost(int productId, string attributesXml)
        {
            var product = _productService.GetProductById(productId);
            var mattressProductAttributeIds =
                _productAttributeService.GetAllProductAttributes()
                .Where(pa => pa.Name.Contains("Mattress Size") || pa.Name.Contains("Base ("))
                .Select(pa => pa.Id);

            return product.Price +
                _productAttributeParser.ParseProductAttributeMappings(attributesXml)
                .Where(pam => mattressProductAttributeIds.Contains(pam.ProductAttributeId))
                .Select(pam => _productAttributeParser.ParseValues(attributesXml, pam.Id).FirstOrDefault())
                .Select(idasstring => _productAttributeService.GetProductAttributeValueById(int.Parse(idasstring)))
                .Sum(pav => pav.PriceAdjustment);
        }
    }
}
