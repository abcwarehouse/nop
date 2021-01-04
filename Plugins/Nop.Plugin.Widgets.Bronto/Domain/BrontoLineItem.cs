using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Html;
using Nop.Core;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Core.Infrastructure;
using Nop.Plugin.Misc.AbcCore.Services;
using Nop.Services.Catalog;
using Nop.Services.Media;
using Nop.Services.Seo;

namespace Nop.Plugin.Widgets.Bronto.Domain
{
    public class BrontoLineItem
    {
        public string Sku { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string Category { get; private set; }
        public string Other { get; private set; }
        public decimal UnitPrice { get; private set; }
        public decimal SalePrice { get; private set; }
        public int Quantity { get; private set; }
        public decimal TotalPrice { get; private set; }
        public string ImageUrl { get; private set; }
        public string ProductUrl { get; private set; }

        private BrontoLineItem(
            OrderItem orderItem
        ) : this(orderItem.ProductId, orderItem.Quantity)
        {
        }

        private BrontoLineItem(
            ShoppingCartItem cartItem
        ) : this(cartItem.ProductId, cartItem.Quantity)
        {
        }

        private BrontoLineItem(
            int productId,
            int quantity
        )
        {
            var storeContext = EngineContext.Current.Resolve<IStoreContext>();
            var productService = EngineContext.Current.Resolve<IProductService>();
            var urlRecordService = EngineContext.Current.Resolve<IUrlRecordService>();
            var pictureService = EngineContext.Current.Resolve<IPictureService>();

            var product = productService.GetProductById(productId);
            var productUrl = storeContext.CurrentStore.Url +
                             urlRecordService.GetSeName<Product>(product);
            var unitPrice = product.OldPrice != 0.0M ?
                                product.OldPrice :
                                product.Price;

            // so this really should just read from ShortDescription
            // but with ABC we need to consider the ABC description
            var frontEndService = EngineContext.Current.Resolve<FrontEndService>();
            var productAbcDescription = frontEndService.GetProductAbcDescriptionByProductId(product.Id);
            var shortDescription = productAbcDescription != null ?
                                    productAbcDescription.AbcDescription :
                                    product.ShortDescription;

            var productPictures = pictureService.GetPicturesByProductId(product.Id);
            var productPictureUrl = productPictures.Any() ?
                                        pictureService.GetPictureUrl(productPictures[0].Id) :
                                        "";

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
        }

        public static IList<BrontoLineItem> FromCart(IList<ShoppingCartItem> cart)
        {
            if (cart == null) throw new ArgumentNullException(nameof(cart));

            List<BrontoLineItem> result = new List<BrontoLineItem>();
            foreach (var cartItem in cart)
            {
                result.Add(new BrontoLineItem(cartItem));
            }

            return result;
        }

        public static IList<BrontoLineItem> FromOrderItems(IList<OrderItem> orderItems)
        {
            if (orderItems == null) throw new ArgumentNullException(nameof(orderItems));

            List<BrontoLineItem> result = new List<BrontoLineItem>();
            foreach (var orderItem in orderItems)
            {
                result.Add(new BrontoLineItem(orderItem));
            }

            return result;
        }

        private static string GetProductCategoryBreadcrumb(Product product)
        {
            var categoryService = EngineContext.Current.Resolve<ICategoryService>();
            var productCategory = categoryService.GetProductCategoriesByProductId(product.Id).FirstOrDefault();
            if (productCategory == null) return "";

            var category = categoryService.GetCategoryById(productCategory.CategoryId);
            var categoryBreadcrumb = categoryService.GetFormattedBreadCrumb(category, null, ">");
            return categoryBreadcrumb;
        }
    }
}
