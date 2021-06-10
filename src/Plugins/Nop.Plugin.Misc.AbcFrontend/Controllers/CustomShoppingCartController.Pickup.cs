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
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcFrontend.Controllers
{
    public partial class CustomShoppingCartController : BasePublicController
    {
        //add product to cart using AJAX
        //currently we use this method on the product details pages
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> AddProductToCart_Pickup(
            int productId,
            int shoppingCartTypeId,
            IFormCollection form
        )
        {
            var product = await _productService.GetProductByIdAsync(productId);
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
            var customerId = (await _workContext.GetCurrentCustomerAsync()).Id;
            CustomerShopMapping csm = _customerShopMappingRepository.Table
                .Where(c => c.CustomerId == customerId)
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
            StockResponse stockResponse = await _backendStockService.GetApiStockAsync(product.Id);
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
                var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), (ShoppingCartType)shoppingCartTypeId, (await _storeContext.GetCurrentStoreAsync()).Id);

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
            var customerEnteredPriceConverted = await _productAttributeParser.ParseCustomerEnteredPriceAsync(product, form);

            //entered quantity
            var quantity = _productAttributeParser.ParseEnteredQuantity(product, form);

            //product and gift card attributes
            var attributes = await _productAttributeParser.ParseProductAttributesAsync(product, form, addToCartWarnings);

            //---------------------------------------------START CUSTOM CODE-----------------------------------------------------
            if (stockResponse != null)
            {
                attributes = await _attributeUtilities.InsertPickupAttributeAsync(product, stockResponse, attributes);
            }
            //----------------------------------------------END CUSTOM CODE------------------------------------------------------

            //rental attributes
            _productAttributeParser.ParseRentalDates(product, form, out var rentalStartDate, out var rentalEndDate);

            var cartType = updatecartitem == null ? (ShoppingCartType)shoppingCartTypeId :
                //if the item to update is found, then we ignore the specified "shoppingCartTypeId" parameter
                updatecartitem.ShoppingCartType;

            SaveItem(updatecartitem, addToCartWarnings, product, cartType, attributes, customerEnteredPriceConverted, rentalStartDate, rentalEndDate, quantity);

            //return result
            return await GetProductToCartDetails(addToCartWarnings, cartType, product);
        }

        // See if we can just inherit these from the base controller
        protected async virtual Task SaveItem(ShoppingCartItem updatecartitem, List<string> addToCartWarnings, Product product,
           ShoppingCartType cartType, string attributes, decimal customerEnteredPriceConverted, DateTime? rentalStartDate,
           DateTime? rentalEndDate, int quantity)
        {
            if (updatecartitem == null)
            {
                //add to the cart
                addToCartWarnings.AddRange(await _shoppingCartService.AddToCartAsync(await _workContext.GetCurrentCustomerAsync(),
                    product, cartType, (await _storeContext.GetCurrentStoreAsync()).Id,
                    attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate, quantity, true));
            }
            else
            {
                var cart = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), updatecartitem.ShoppingCartType, (await _storeContext.GetCurrentStoreAsync()).Id);

                var otherCartItemWithSameParameters = await _shoppingCartService.FindShoppingCartItemInTheCartAsync(
                    cart, updatecartitem.ShoppingCartType, product, attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate);
                if (otherCartItemWithSameParameters != null &&
                    otherCartItemWithSameParameters.Id == updatecartitem.Id)
                {
                    //ensure it's some other shopping cart item
                    otherCartItemWithSameParameters = null;
                }
                //update existing item
                addToCartWarnings.AddRange(await _shoppingCartService.UpdateShoppingCartItemAsync(await _workContext.GetCurrentCustomerAsync(),
                    updatecartitem.Id, attributes, customerEnteredPriceConverted,
                    rentalStartDate, rentalEndDate, quantity + (otherCartItemWithSameParameters?.Quantity ?? 0), true));
                if (otherCartItemWithSameParameters != null && !addToCartWarnings.Any())
                {
                    //delete the same shopping cart item (the other one)
                    await _shoppingCartService.DeleteShoppingCartItemAsync(otherCartItemWithSameParameters);
                }
            }
        }

        protected async virtual Task<IActionResult> GetProductToCartDetails(List<string> addToCartWarnings, ShoppingCartType cartType,
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
                        await _customerActivityService.InsertActivityAsync("PublicStore.AddToWishlist",
                            string.Format(await _localizationService.GetResourceAsync( "ActivityLog.PublicStore.AddToWishlist"), product.Name), product);

                        if (_shoppingCartSettings.DisplayWishlistAfterAddingProduct)
                        {
                            //redirect to the wishlist page
                            return Json(new
                            {
                                redirect = Url.RouteUrl("Wishlist")
                            });
                        }

                        //display notification message and update appropriate blocks
                        var shoppingCarts = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.Wishlist, (await _storeContext.GetCurrentStoreAsync()).Id);

                        var updatetopwishlistsectionhtml = string.Format(
                            await _localizationService.GetResourceAsync( "Wishlist.HeaderQuantity"),
                            shoppingCarts.Sum(item => item.Quantity));

                        return Json(new
                        {
                            success = true,
                            message = string.Format(
                                await _localizationService.GetResourceAsync( "Products.ProductHasBeenAddedToTheWishlist.Link"),
                                Url.RouteUrl("Wishlist")),
                            updatetopwishlistsectionhtml
                        });
                    }

                case ShoppingCartType.ShoppingCart:
                default:
                    {
                        //activity log
                        await _customerActivityService.InsertActivityAsync("PublicStore.AddToShoppingCart",
                            string.Format(await _localizationService.GetResourceAsync( "ActivityLog.PublicStore.AddToShoppingCart"), product.Name), product);

                        if (_shoppingCartSettings.DisplayCartAfterAddingProduct)
                        {
                            //redirect to the shopping cart page
                            return Json(new
                            {
                                redirect = Url.RouteUrl("ShoppingCart")
                            });
                        }

                        //display notification message and update appropriate blocks
                        var shoppingCarts = await _shoppingCartService.GetShoppingCartAsync(await _workContext.GetCurrentCustomerAsync(), ShoppingCartType.ShoppingCart, (await _storeContext.GetCurrentStoreAsync()).Id);

                        var updatetopcartsectionhtml = string.Format(
                            await _localizationService.GetResourceAsync( "ShoppingCart.HeaderQuantity"),
                            shoppingCarts.Sum(item => item.Quantity));

                        var updateflyoutcartsectionhtml = _shoppingCartSettings.MiniShoppingCartEnabled
                            ? await RenderViewComponentToStringAsync("FlyoutShoppingCart")
                            : string.Empty;

                        return Json(new
                        {
                            success = true,
                            message = string.Format(await _localizationService.GetResourceAsync( "Products.ProductHasBeenAddedToTheCart.Link"),
                                Url.RouteUrl("ShoppingCart")),
                            updatetopcartsectionhtml,
                            updateflyoutcartsectionhtml
                        });
                    }
            }
        }
    }
}
