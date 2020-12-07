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
    public class AbcMattressServiceTests
    {
        private IAbcMattressService _abcMattressService;

        private Mock<IRepository<AbcMattressModel>> _abcMattressModelRepository;

        private Mock<IProductService> _productService;
        private Mock<IUrlRecordService> _urlRecordService;

        [SetUp]
        public void Setup()
        {
            _abcMattressModelRepository = new Mock<IRepository<AbcMattressModel>>();
            _abcMattressModelRepository.Setup(p => p.Table).Returns(GetMockAbcMattressModels);

            _productService = new Mock<IProductService>();
            _urlRecordService = new Mock<IUrlRecordService>();

            _abcMattressService = new AbcMattressService(
                _abcMattressModelRepository.Object,
                _productService.Object,
                _urlRecordService.Object
            );
        }

        [Test]
        public void Gets_All_Models()
        {
            var abcMattressModels = _abcMattressService.GetAllAbcMattressModels();

            abcMattressModels.Should().HaveCount(2);
        }

        [Test]
        public void Creates_AbcMattressModel_Product()
        {
            var abcMattressModel = new AbcMattressModel()
            {
                Name = "Alverson",
                Description = "Alverson is good good",
                ManufacturerId = 1,
                Comfort = "Firm"
            };
            var product = _abcMattressService.CreateAbcMattressProduct(abcMattressModel);

            product.Name.Should().Be(abcMattressModel.Description);
            product.Sku.Should().Be(abcMattressModel.Name);
            product.AllowCustomerReviews.Should().BeFalse();
            product.VisibleIndividually.Should().BeTrue();
            product.CreatedOnUtc.Should().BeCloseTo(DateTime.UtcNow);
            product.ProductType.Should().Be(ProductType.SimpleProduct);
            product.OrderMinimumQuantity.Should().Be(1);
            product.OrderMaximumQuantity.Should().Be(10000);
            product.Published.Should().BeTrue();

            _productService.Verify(x => x.InsertProduct(product), Times.Once);
            _urlRecordService.Verify(x => x.InsertUrlRecord(It.IsAny<UrlRecord>()), Times.Once);
        }

        private IQueryable<AbcMattressModel> GetMockAbcMattressModels()
        {
            return new List<AbcMattressModel>
            {
                new AbcMattressModel()
                {
                    Name = "Alverson",
                    Description = "Alverson is good",
                    ManufacturerId = 1,
                    Comfort = "Firm",
                    ProductId = null
                },
                new AbcMattressModel()
                {
                    Name = "Carrollton",
                    Description = "Carrollton is bad",
                    ManufacturerId = 1,
                    Comfort = "Firm",
                    ProductId = 1
                },
            }.AsQueryable();
        }
    }
}