using Nop.Core.Domain.Catalog;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;
using Nop.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nop.Plugin.Misc.AbcCore.Extensions
{
    public static class ProductExtensions
    {
        public const string IsAddToCartKey = "IsAddToCart";
        public const string IsAddToCartWithUserInfoKey = "IsAddToCartWithUserInfo";

        public static bool IsCallOnly(this Product product)
        {
            var manufacturerService = EngineContext.Current.Resolve<IManufacturerService>();
            var manufacturers = manufacturerService.GetProductManufacturersByProductId(product.Id);
            return manufacturers.Where(pm => manufacturerService.GetManufacturerById(pm.ManufacturerId).Name.ToLower() == "asko").Any() ||
                   manufacturers.Where(pm => manufacturerService.GetManufacturerById(pm.ManufacturerId).Name.ToLower() == "subzero").Any() ||
                   manufacturers.Where(pm => manufacturerService.GetManufacturerById(pm.ManufacturerId).Name.ToLower() == "wolf").Any();
        }

        public static bool IsAddToCart(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            return genericAttributeService.GetAttribute<bool>(product, IsAddToCartKey);
        }

        public static void EnableAddToCart(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            genericAttributeService.SaveAttribute(product, IsAddToCartKey, true);
        }

        public static void DisableAddToCart(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            var attribute = genericAttributeService.GetAttributesForEntity(product.Id, "Product").Where(ga => ga.Key == IsAddToCartKey).FirstOrDefault();

            if (attribute != null)
            {
                genericAttributeService.DeleteAttribute(attribute);
            }
        }

        public static bool IsAddToCartWithUserInfo(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            return genericAttributeService.GetAttribute<bool>(product, IsAddToCartWithUserInfoKey);
        }

        public static void EnableAddToCartWithUserInfo(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            genericAttributeService.SaveAttribute(product, IsAddToCartWithUserInfoKey, true);
        }

        public static void DisableAddToCartWithUserInfo(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            var attribute = genericAttributeService.GetAttributesForEntity(product.Id, "Product").Where(ga => ga.Key == IsAddToCartWithUserInfoKey).FirstOrDefault();

            if (attribute != null)
            {
                genericAttributeService.DeleteAttribute(attribute);
            }
        }

        public static bool IsPickup(this Product product)
        {
            var productAttributeService = EngineContext.Current.Resolve<IProductAttributeService>();
            return productAttributeService.GetProductAttributeMappingsByProductId(product.Id)
                   .Where(pam => productAttributeService.GetProductAttributeById(pam.ProductAttributeId) != null &&
                                 productAttributeService.GetProductAttributeById(pam.ProductAttributeId).Name == "Pickup").Any();
        }

        public static DateTime? GetSpecialPriceEndDate(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            return genericAttributeService.GetAttribute<DateTime?>(product, "SpecialPriceEndDate");
        }
    }
}