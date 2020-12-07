using Nop.Core;
using Nop.Services.Catalog;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using System;
using System.Linq;
using Nop.Core.Domain.Catalog;
using Nop.Services.Seo;
using Nop.Core.Domain.Seo;
using Nop.Plugin.Misc.AbcCore.Extensions;

namespace Nop.Plugin.Misc.AbcMattresses.Tasks
{
    public class UpdateMattressesTask : IScheduleTask
    {
        private readonly ILogger _logger;

        private readonly ICategoryService _categoryService;
        private readonly IProductService _productService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IUrlRecordService _urlRecordService;

        public UpdateMattressesTask(
            ILogger logger,
            ICategoryService categoryService,
            IProductService productService,
            IProductAttributeService productAttributeService,
            IUrlRecordService urlRecordService
        )
        {
            _logger = logger;
            _categoryService = categoryService;
            _productService = productService;
            _productAttributeService = productAttributeService;
            _urlRecordService = urlRecordService;
        }

        public void Execute()
        {
            // Since I don't know what the backend table will look like yet,
            // I'm just scaffolding a lot of stuff

            var product = _productService.GetProductBySku("mattress-product");
            if (product == null)
            {
                //product = CreateProduct();
            }
            MergeProductCategories(product);
            MergeProductAttributes(product);
        }

        private void MergeProductAttributes(Product product)
        {
            var productAttributes = _productAttributeService.GetAllProductAttributes()
                                                            .Where(pa => pa.Name == AbcMattressesConsts.MattressSizeName ||
                                                                         pa.Name == AbcMattressesConsts.BaseName ||
                                                                         pa.Name == AbcMattressesConsts.FreeGiftName);

            foreach (var productAttribute in productAttributes)
            {
                // attribute mapping
                var productAttributeMapping = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id)
                                                                              .Where(pam => pam.ProductAttributeId == productAttribute.Id)
                                                                              .FirstOrDefault();

                if (productAttributeMapping == null)
                {
                    productAttributeMapping = new ProductAttributeMapping()
                    {
                        ProductId = product.Id,
                        ProductAttributeId = productAttribute.Id,
                        IsRequired = true,
                        AttributeControlType = AttributeControlType.DropdownList,
                        DisplayOrder = GetDisplayOrder(productAttribute.Name)
                    };
                    _productAttributeService.InsertProductAttributeMapping(productAttributeMapping);
                }
                    
                // values
                var predefinedValues = _productAttributeService.GetPredefinedProductAttributeValues(productAttribute.Id);
                foreach (var predefinedValue in predefinedValues)
                {
                    var existingPav = _productAttributeService.GetProductAttributeValues(productAttributeMapping.Id)
                                                               .Where(pav =>
                                                                    pav.ProductAttributeMappingId == productAttributeMapping.Id &&
                                                                    pav.Name.Equals(predefinedValue.Name)
                                                               )
                                                               .FirstOrDefault();
                    if (existingPav == null)
                    {
                        var pav = new ProductAttributeValue()
                        {
                            ProductAttributeMappingId = productAttributeMapping.Id,
                            Name = predefinedValue.Name,
                            PriceAdjustment = GetPriceAdjustment(predefinedValue.Name),
                            IsPreSelected = predefinedValue.Name == "Queen" ||
                                            predefinedValue.Name == "No"
                        };
                        _productAttributeService.InsertProductAttributeValue(pav);
                    }
                }
            }
        }

        private decimal GetPriceAdjustment(string name)
        {
            switch (name)
            {
                case "Twin":
                    return 100.0M;
                case "Twin XL":
                    return 200.0M;
                case "Full":
                    return 300.0M;
                case "Queen":
                    return 400.0M;
                case "King":
                    return 500.0M;
                case "California King":
                    return 600.0M;
                case "Yes":
                    return 150.0M;
                default:
                    return 0.0M;
            }
        }

        private int GetDisplayOrder(string productAttributeName)
        {
            switch (productAttributeName)
            {
                case AbcMattressesConsts.MattressSizeName:
                    return 0;
                case AbcMattressesConsts.BaseName:
                    return 1;
                case AbcMattressesConsts.FreeGiftName:
                    return 2;
            }

            return -1;
        }

        private void MergeProductCategories(Product product)
        {
            // determine this based on the mattresses
            var categories = _categoryService.GetAllCategories()
                .Where(c => c.Name == "TEMPUR-PEDIC Material" ||
                            c.Name == "Cushion Firm" ||
                            c.Name == "TEMPUR-PEDIC Adapt" ||
                            c.Name == "California King" ||
                            c.Name == "King" ||
                            c.Name == "Queen" ||
                            c.Name == "Full" ||
                            c.Name == "Twin Extra Long" ||
                            c.Name == "Twin");

            foreach (var category in categories)
            {
                var existingProductCategory = _categoryService.GetProductCategoriesByProductId(product.Id).Where(pc => pc.CategoryId == category.Id).FirstOrDefault();
                if (existingProductCategory == null)
                {
                    var productCategory = new ProductCategory()
                    {
                        ProductId = product.Id,
                        CategoryId = category.Id
                    };
                    _categoryService.InsertProductCategory(productCategory);
                }
            }
        }

        private string GetShortDescription()
        {
            return "<p><br /><br /><img width=\"72\" height=\"62\" src=\"/ContentAdmin/UserFiles/Image/Brand logos/Tempur-Pedic-logo.gif\" alt=\"\" /></p><p><table width=\"245\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" style=\"width:183.75pt;\"><tbody><tr><td style=\"padding:0in 0in 0in 0in\"><p><strong><span style=\"Times New Roman&quot;\">Adapt Medium Twin Hybrid Mattress</span></strong></p></td></tr><tr style=\"height:7.5pt\"><td style=\"padding:0in 0in 0in 0in;height:7.5pt\"><div>&nbsp;</div></td></tr><tr><td style=\"padding:0in 0in 0in 0in\"><p><span style=\"Times New Roman&quot;\">The technology that started it all, redesigned for today. Superior cool-to-touch comfort. Two layers of premium TEMPUR® Technology. Working together to continually adapt and conform to your body’s changing needs throughout the night — relieving pressure, reducing motion transfer, relaxing you while you sleep, and rejuvenating you for your day.</span></p></td></tr></tbody></table></p>";
        }

        private string GetFullDescription()
        {
            return "<p><strong>Cool-to-Touch Cover</strong><br />Premium knit technology for superior cool-to-touch feel.<br /><strong>TEMPUR-ES® Comfort Layer</strong><br />Softer feel works in combination with other materials to support and relax.<br /><strong>Original TEMPUR® Support Layer</strong><br />Advanced adaptability for truly personalized comfort and support. <br /><strong>Hybrid Technology</strong><br />1,000+ premium spring coils designed in-house to work with our material.<br /><br /><strong>Twin  (38\" x 74\") <br />Profile Approximately 12\"</strong></p><p><img src=\"/Content/Images/uploaded/73923_Adapt_MediumHybrid_Layer_Benefit.jpg\" alt=\"Tempur Pedic Adapt Medium Hybrid Benefits\" width=\"100%\" /></p>";
        }
    }
}
