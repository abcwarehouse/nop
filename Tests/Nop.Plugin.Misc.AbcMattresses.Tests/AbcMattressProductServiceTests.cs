using NUnit.Framework;
using FluentAssertions;
using Nop.Plugin.Misc.AbcMattresses.Services;
using Moq;
using System.Collections.Generic;
using Nop.Data;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using System.Linq;
using Nop.Core.Domain.Catalog;
using System;
using Nop.Services.Catalog;
using Nop.Services.Seo;
using Nop.Core.Domain.Seo;

namespace Nop.Plugin.Misc.AbcMattresses.Tests
{
    public class AbcMattressProductServiceTests
    {
        private IAbcMattressProductService _abcMattressProductService;

        private Mock<IProductService> _productService;
        private Mock<IUrlRecordService> _urlRecordService;
        private Mock<IAbcMattressService> _abcMattressService;
        private Mock<IAbcMattressEntryService> _abcMattressEntryService;
        private Mock<ICategoryService> _categoryService; 

        private AbcMattressModel _abcMattressModelNoProduct = new AbcMattressModel()
        {
            Name = "Alverson",
            Description = "Alverson is good good",
            ManufacturerId = 1,
            Comfort = "Firm"
        };
        private AbcMattressModel _abcMattressModelWithProduct = new AbcMattressModel()
        {
            Name = "Carrollton",
            Description = "Carrollton is good good",
            ManufacturerId = 2,
            Comfort = "Firm",
            ProductId = 1
        };

        [SetUp]
        public void Setup()
        {
            _abcMattressService = new Mock<IAbcMattressService>();

            _productService = new Mock<IProductService>();
            _productService.Setup(x => x.GetProductById(It.IsAny<int>()))
                                    .Returns(new Product());

            _urlRecordService = new Mock<IUrlRecordService>();

            _abcMattressEntryService = new Mock<IAbcMattressEntryService>();
            _abcMattressEntryService.Setup(x => x.GetAbcMattressEntriesByModelId(It.IsAny<int>()))
                                    .Returns(new List<AbcMattressEntry>()
                                    {
                                        new AbcMattressEntry()
                                        {
                                            AbcMattressModelId = 1,
                                            Size = "Twin",
                                            ItemNo = 12345,
                                            Price = 247.00M
                                        }
                                    });

            _categoryService = new Mock<ICategoryService>();
            _categoryService.Setup(x => x.GetAllCategories(It.IsAny<int>(), It.IsAny<bool>()))
                                    .Returns(new List<Category>()
                                    {
                                        new Category() { Id = 1 }
                                    });
            _categoryService.Setup(x => x.GetProductCategoriesByProductId(It.IsAny<int>(), It.IsAny<bool>()))
                                    .Returns(new List<ProductCategory>()
                                    {
                                        new ProductCategory()
                                        {
                                            ProductId = 1,
                                            CategoryId = 1
                                        }
                                    });

            _abcMattressProductService = new AbcMattressProductService(
                _abcMattressService.Object,
                new Mock<IAbcMattressBaseService>().Object,
                _abcMattressEntryService.Object,
                new Mock<IAbcMattressGiftService>().Object,
                _categoryService.Object,
                new Mock<IManufacturerService>().Object,
                _productService.Object,
                new Mock<IProductAttributeService>().Object,
                _urlRecordService.Object
            );
        }

        // [Test]
        // public void Creates_AbcMattressModel_Product()
        // {
        //     var product = _abcMattressProductService.UpsertAbcMattressProduct(_abcMattressModelNoProduct);

        //     product.Name.Should().Be(_abcMattressModelNoProduct.Description);
        //     product.Sku.Should().Be(_abcMattressModelNoProduct.Name);
        //     product.AllowCustomerReviews.Should().BeFalse();
        //     product.VisibleIndividually.Should().BeTrue();
        //     product.CreatedOnUtc.Should().BeCloseTo(DateTime.UtcNow);
        //     product.ProductType.Should().Be(ProductType.SimpleProduct);
        //     product.OrderMinimumQuantity.Should().Be(1);
        //     product.OrderMaximumQuantity.Should().Be(10000);
        //     product.Published.Should().BeTrue();

        //     _abcMattressModelNoProduct.ProductId.Should().NotBeNull();

        //     _productService.Verify(x => x.InsertProduct(product), Times.Once);
        //     _urlRecordService.Verify(x => x.InsertUrlRecord(It.IsAny<UrlRecord>()), Times.Once);
        // }

        // [Test]
        // public void Updates_AbcMattressModel_Product()
        // {
        //     var product = _abcMattressProductService.UpsertAbcMattressProduct(_abcMattressModelWithProduct);

        //     _productService.Verify(x => x.InsertProduct(product), Times.Never);
        //     _urlRecordService.Verify(x => x.InsertUrlRecord(It.IsAny<UrlRecord>()), Times.Never);
        // }
    }
}