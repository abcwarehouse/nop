using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Orders;
using Nop.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Misc.AbcCore.Domain;
using Nop.Plugin.Misc.AbcCore.Models;
using Microsoft.AspNetCore.Http;

namespace Nop.Plugin.Misc.AbcFrontend.Controllers
{
    public partial class CustomShoppingCartController : BasePublicController
    {
        //add product to cart using AJAX
        //currently we use this method on the product details pages
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult AddProductToCart_Pickup(
            int productId,
            int shoppingCartTypeId,
            IFormCollection form
        )
        {
            var product = _productService.GetProductById(productId);
            if (product == null)
            {
                return Json(new
                {
                    redirect = Url.RouteUrl("Homepage")
                });
            }

            //we can add only simple products
            if (product.ProductType != ProductType.SimpleProduct)
            {
                return Json(new
                {
                    success = false,
                    message = "Only simple products could be added to the cart"
                });
            }
            //-------------------------------------CUSTOM CODE------------------------------------------
            // force customer to select a store if store not already selected
            //TODO: This could be cached, would have to be cleared when the order is complete
            CustomerShopMapping csm = _customerShopMappingRepository.Table
                .Where(c => c.CustomerId == _workContext.CurrentCustomer.Id)
                .Select(c => c).FirstOrDefault();
            if (csm == null)
            {
                // return & force them to select a store
                return Json(new
                {
                    success = false,
                    message = "Select a store to pick the item up"
                });
            }

            // check if item is available
            StockResponse stockResponse = _backendStockService.GetApiStock(product.Id);
            if (stockResponse == null)
            {
                bool availableAtStore = stockResponse.ProductStocks
                    .Where(ps => ps.Shop.Id == csm.ShopId)
                    .Select(ps => ps.Available).FirstOrDefault();
                if (!availableAtStore)
                {
                    return Json(new
                    {
                        success = false,
                        message = "This item is not available at this store"
                    });
                }
            }
            //----------------------------------END CUSTOM CODE------------------------------------------


            //update existing shopping cart item
            var updatecartitemid = 0;
            foreach (var formKey in form.Keys)
                if (formKey.Equals($"addtocart_{productId}.UpdatedShoppingCartItemId", StringComparison.InvariantCultureIgnoreCase))
                {
                    int.TryParse(form[formKey], out updatecartitemid);
                    break;
                }

            ShoppingCartItem updatecartitem = null;
            if (_shoppingCartSettings.AllowCartItemEditing && updatecartitemid > 0)
            {
                //search with the same cart type as specified
                var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, (ShoppingCartType)shoppingCartTypeId, _storeContext.CurrentStore.Id);

                updatecartitem = cart.FirstOrDefault(x => x.Id == updatecartitemid);
                //not found? let's ignore it. in this case we'll add a new item
                //if (updatecartitem == null)
                //{
                //    return Json(new
                //    {
                //        success = false,
                //        message = "No shopping cart item found to update"
                //    });
                //}
                //is it this product?
                if (updatecartitem != null && product.Id != updatecartitem.ProductId)
                {
                    return Json(new
                    {
                        success = false,
                        message = "This product does not match a passed shopping cart item identifier"
                    });
                }
            }

            
            var addToCartWarnings = new List<string>();

            //customer entered price
            var customerEnteredPriceConverted = _productAttributeParser.ParseCustomerEnteredPrice(product, form);

            //entered quantity
            var quantity = _productAttributeParser.ParseEnteredQuantity(product, form);

            //product and gift card attributes
            var attributes = _productAttributeParser.ParseProductAttributes(product, form, addToCartWarnings);

            //---------------------------------------------START CUSTOM CODE-----------------------------------------------------
            if (stockResponse != null)
            {
                attributes = _attributeUtilities.InsertPickupAttribute(product, stockResponse, attributes);
            }
            //----------------------------------------------END CUSTOM CODE------------------------------------------------------

            //rental attributes
            _productAttributeParser.ParseRentalDates(product, form, out var rentalStartDate, out var rentalEndDate);

            var cartType = updatecartitem == null ? (ShoppingCartType)shoppingCartTypeId :
                //if the item to update is found, then we ignore the specified "shoppingCartTypeId" parameter
                updatecartitem.ShoppingCartType;

            SaveItem(updatecartitem, addToCartWarnings, product, cartType, attributes, customerEnteredPriceConverted, rentalStartDate, rentalEndDate, quantity);

            //return result
            return GetProductToCartDetails(addToCartWarnings, cartType, product);
        }

        // See if we can just inherit these from the base controller
        protected virtual void SaveItem(ShoppingCartItem updatecartitem, List<string> addToCartWarnings, Product product,
           ShoppingCartType cartType, string attributes, decimal customerEnteredPriceConverted, DateTime? rentalStartDate,
           DateTime? rentalEndDate, int quantity)
        {
            if (updatecartitem == null)
            {
                //add to the cart
                addToCartWarnings.AddRange(_shoppingCartService.AddToCart(_workContext.CurrentCustomer,
                    product, cartType, _storeContext.CurrentStore.Id,
                    attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate, quantity, true));
            }
            else
            {
                var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, updatecartitem.ShoppingCartType, _storeContext.CurrentStore.Id);

                var otherCartItemWithSameParameters = _shoppingCartService.FindShoppingCartItemInTheCart(
                    cart, updatecartitem.ShoppingCartType, product, attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate);
                if (otherCartItemWithSameParameters != null &&
                    otherCartItemWithSameParameters.Id == updatecartitem.Id)
                {
                    //ensure it's some other shopping cart item
                    otherCartItemWithSameParameters = null;
                }
                //update existing item
                addToCartWarnings.AddRange(_shoppingCartService.UpdateShoppingCartItem(_workContext.CurrentCustomer,
                    updatecartitem.Id, attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate, quantity + (otherCartItemWithSameParameters?.Quantity ?? 0), true));
                if (otherCartItemWithSameParameters != null && !addToCartWarnings.Any())
                {
                    //delete the same shopping cart item (the other one)
                    _shoppingCartService.DeleteShoppingCartItem(otherCartItemWithSameParameters);
                }
            }
        }

        protected virtual IActionResult GetProductToCartDetails(List<string> addToCartWarnings, ShoppingCartType cartType,
            Product product)
        {
            if (addToCartWarnings.Any())
            {
                //cannot be added to the cart/wishlist
                //let's display warnings
                return Json(new
                {
                    success = false,
                    message = addToCartWarnings.ToArray()
                });
            }

            //added to the cart/wishlist
            switch (cartType)
            {
                case ShoppingCartType.Wishlist:
                    {
                        //activity log
                        _customerActivityService.InsertActivity("PublicStore.AddToWishlist",
                            string.Format(_localizationService.GetResource("ActivityLog.PublicStore.AddToWishlist"), product.Name), product);

                        if (_shoppingCartSettings.DisplayWishlistAfterAddingProduct)
                        {
                            //redirect to the wishlist page
                            return Json(new
                            {
                                redirect = Url.RouteUrl("Wishlist")
                            });
                        }

                        //display notification message and update appropriate blocks
                        var shoppingCarts = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.Wishlist, _storeContext.CurrentStore.Id);

                        var updatetopwishlistsectionhtml = string.Format(
                            _localizationService.GetResource("Wishlist.HeaderQuantity"),
                            shoppingCarts.Sum(item => item.Quantity));

                        return Json(new
                        {
                            success = true,
                            message = string.Format(
                                _localizationService.GetResource("Products.ProductHasBeenAddedToTheWishlist.Link"),
                                Url.RouteUrl("Wishlist")),
                            updatetopwishlistsectionhtml
                        });
                    }

                case ShoppingCartType.ShoppingCart:
                default:
                    {
                        //activity log
                        _customerActivityService.InsertActivity("PublicStore.AddToShoppingCart",
                            string.Format(_localizationService.GetResource("ActivityLog.PublicStore.AddToShoppingCart"), product.Name), product);

                        if (_shoppingCartSettings.DisplayCartAfterAddingProduct)
                        {
                            //redirect to the shopping cart page
                            return Json(new
                            {
                                redirect = Url.RouteUrl("ShoppingCart")
                            });
                        }

                        //display notification message and update appropriate blocks
                        var shoppingCarts = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);

                        var updatetopcartsectionhtml = string.Format(
                            _localizationService.GetResource("ShoppingCart.HeaderQuantity"),
                            shoppingCarts.Sum(item => item.Quantity));

                        var updateflyoutcartsectionhtml = _shoppingCartSettings.MiniShoppingCartEnabled
                            ? RenderViewComponentToString("FlyoutShoppingCart")
                            : string.Empty;

                        return Json(new
                        {
                            success = true,
                            message = string.Format(_localizationService.GetResource("Products.ProductHasBeenAddedToTheCart.Link"),
                                Url.RouteUrl("ShoppingCart")),
                            updatetopcartsectionhtml,
                            updateflyoutcartsectionhtml
                        });
                    }
            }
        }
    }
}
