using System.Threading.Tasks;
using Nop.Plugin.Widgets.Bronto.Domain;
using System.Collections.Generic;
using Nop.Core.Domain.Orders;
using Nop.Services.Catalog;
using Nop.Core;
using Nop.Plugin.Misc.AbcCore.Services;
using System;

namespace Nop.Plugin.Widgets.Bronto.Services
{
    public class BrontoLineItemService : IBrontoLineItemService
    {
        private readonly FrontEndService _frontendService;
        private readonly ICategoryService _categoryService;
        private readonly IProductService _productService;
        private readonly IStoreContext _storeContext;

        public BrontoLineItemService(
            FrontEndService frontendService,
            ICategoryService categoryService,
            IProductService productService,
            IStoreContext storeContext
        ) {
            _frontendService = frontendService;
            _categoryService = categoryService;
            _productService = productService;
            _storeContext = storeContext;
        }

        public async Task<IList<BrontoLineItem>> GetBrontoLineItemsAsync(IList<ShoppingCartItem> cart)
        {
            if (cart == null) throw new ArgumentNullException(nameof(cart));

            var result = new List<BrontoLineItem>();
            foreach (var cartItem in cart)
            {
                result.Add(await GetBrontoLineItemAsync(cartItem.ProductId, cartItem.Quantity));
            }

            return result;
        }

        public async Task<IList<BrontoLineItem>> GetBrontoLineItemsAsync(IList<OrderItem> orderItems)
        {
            if (orderItems == null) throw new ArgumentNullException(nameof(orderItems));

            var result = new List<BrontoLineItem>();
            foreach (var orderItem in orderItems)
            {
                result.Add(await GetBrontoLineItemAsync(orderItem.ProductId, orderItem.Quantity));
            }

            return result;
        }

        private async Task<BrontoLineItem> GetBrontoLineItemAsync(int productId, int quantity)
        {
            var product = await _productService.GetProductByIdAsync(productId);
            var productUrl = await _storeContext.GetCurrentStoreAsync().Url +
                             await _urlRecordService.GetSeNameAsync<Product>(product);
            var unitPrice = product.OldPrice != 0.0M ?
                                product.OldPrice :
                                product.Price;

            // so this really should just read from ShortDescription
            // but with ABC we need to consider the ABC description
            var productAbcDescription = frontEndService.GetProductAbcDescriptionByProductId(product.Id);
            var shortDescription = productAbcDescription != null ?
                                    productAbcDescription.AbcDescription :
                                    product.ShortDescription;

            var productPictures = await _pictureService.GetPicturesByProductIdAsync(product.Id);
            var productPictureUrl = productPictures.Any() ?
                                        await _pictureService.GetPictureUrlAsync(productPictures[0].Id) :
                                        "";
            return new BrontoLineItem()
            {
                Sku = product.Sku;
                Name = product.Name;
                Description = shortDescription;
                Category = GetProductCategoryBreadcrumb(product);
                //Other = "", - not currently in use
                UnitPrice = unitPrice;
                SalePrice = product.Price;
                Quantity = quantity;
                TotalPrice = product.Price * quantity;
                ImageUrl = productPictureUrl;
                ProductUrl = productUrl;
            };
        }

        private Task<string> GetProductCategoryBreadcrumbAsync(Product product)
        {
            var productCategory = _categoryService.GetProductCategoriesByProductId(product.Id).FirstOrDefault();
            if (productCategory == null) return "";

            var category = _categoryService.GetCategoryById(productCategory.CategoryId);
            var categoryBreadcrumb = _categoryService.GetFormattedBreadCrumb(category, null, ">");
            return categoryBreadcrumb;
        }
    }
}
