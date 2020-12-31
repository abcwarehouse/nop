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
        private readonly IProductService _productService;

        public HomeDeliveryCostService(
            ICategoryService categoryService,
            IProductService productService
        )
        {
            _categoryService = categoryService;
            _productService = productService;
        }

        public decimal GetHomeDeliveryCost(OrderItem orderItem)
        {
            return GetCost(orderItem.ProductId);
        }

        public decimal GetHomeDeliveryCost(GetShippingOptionRequest.PackageItem packageItem)
        {
            return GetCost(packageItem.Product.Id);
        }

        private decimal GetCost(int productId)
        {
            var categories = _categoryService.GetProductCategoriesByProductId(productId)
                                                    .Select(pc => _categoryService.GetCategoryById(pc.CategoryId));

            if (categories.Any(c => new string[]
                { "recliners", "lift chairs", "massage chairs" }.Contains(c.Name.ToLower()))
            )
            {
                return 49.00M;
            }

            // default
            return 14.75M;
        }
    }
}
