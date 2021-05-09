﻿using Nop.Core.Domain.Catalog;
using Nop.Core.Infrastructure;
using Nop.Services.Catalog;
using Nop.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace Nop.Plugin.Misc.AbcCore.Extensions
{
    public static class ProductExtensions
    {
        public const string IsAddToCartKey = "IsAddToCart";
        public const string IsAddToCartWithUserInfoKey = "IsAddToCartWithUserInfo";

        public static async Task<bool> IsCallOnly(this Product product)
        {
            var manufacturerService = EngineContext.Current.Resolve<IManufacturerService>();
            var productManufacturers = await manufacturerService.GetProductManufacturersByProductIdAsync(product.Id);

            foreach (var pm in productManufacturers)
            {
                var manufacturer = await manufacturerService.GetManufacturerByIdAsync(pm.ManufacturerId);
                if (new string[]{"asko","subzero","wolf"}.Contains(manufacturer.Name.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> IsAddToCart(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            return await genericAttributeService.GetAttributeAsync<bool>(product, IsAddToCartKey);
        }

        public static async Task EnableAddToCart(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            await genericAttributeService.SaveAttributeAsync(product, IsAddToCartKey, true);
        }

        public static async Task DisableAddToCart(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            var attributes = await genericAttributeService.GetAttributesForEntityAsync(product.Id, "Product");
            var attribute = attributes.Where(ga => ga.Key == IsAddToCartKey).FirstOrDefault();
            

            if (attribute != null)
            {
                await genericAttributeService.DeleteAttributeAsync(attribute);
            }
        }

        public static async Task<bool> IsAddToCartWithUserInfo(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            return await genericAttributeService.GetAttributeAsync<bool>(product, IsAddToCartWithUserInfoKey);
        }

        public static async Task EnableAddToCartWithUserInfo(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            await genericAttributeService.SaveAttributeAsync(product, IsAddToCartWithUserInfoKey, true);
        }

        public static async Task DisableAddToCartWithUserInfo(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            var attributes = await genericAttributeService.GetAttributesForEntityAsync(product.Id, "Product");
            var attribute = attributes.Where(ga => ga.Key == IsAddToCartWithUserInfoKey).FirstOrDefault();

            if (attribute != null)
            {
                await genericAttributeService.DeleteAttributeAsync(attribute);
            }
        }

        public static async Task<bool> IsPickupAsync(this Product product)
        {
            var productAttributeService = EngineContext.Current.Resolve<IProductAttributeService>();
            var productAttributeMappings = await productAttributeService.GetProductAttributeMappingsByProductIdAsync(product.Id);

            foreach (var pam in productAttributeMappings)
            {
                var productAttribute = await productAttributeService.GetProductAttributeByIdAsync(pam.ProductAttributeId);
                if (productAttribute != null && productAttribute.Name == "Pickup")
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<DateTime?> GetSpecialPriceEndDateAsync(this Product product)
        {
            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            return await genericAttributeService.GetAttributeAsync<DateTime?>(product, "SpecialPriceEndDate");
        }
    }
}