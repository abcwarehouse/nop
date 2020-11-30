using NUnit.Framework;
using FluentAssertions;
using Nop.Plugin.Misc.AbcMattresses.Services;
using Moq;
using Nop.Services.Catalog;
using Nop.Core.Domain.Catalog;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcMattresses.Tests
{
    public class AbcMattressListingPriceServiceTests
    {
        private IAbcMattressListingPriceService _abcMattressListingPriceService;

        private Product productWithNoMattresses = new Product
        {
            Id = 1
        };

        private Product productWithTwinAndQueenMattresses = new Product
        {
            Id = 2
        };

        private Product productWithTwinMattress = new Product
        {
            Id = 3,
        };

        [SetUp]
        public void Setup()
        {
            var mockProductAttributeService = new Mock<IProductAttributeService>();
            SetupProductWithNoMattresses(mockProductAttributeService);
            SetupProductWithTwinAndQueenMattresses(mockProductAttributeService);
            SetupProductWithTwinMattress(mockProductAttributeService);

            mockProductAttributeService.Setup(
                x => x.GetProductAttributeById(1))
                .Returns(new ProductAttribute()
                {
                    Name = "Mattress Size"
                }
            );

            _abcMattressListingPriceService = new AbcMattressListingPriceService(
                mockProductAttributeService.Object
            );
        }

        private static void SetupProductWithTwinMattress(Mock<IProductAttributeService> mockProductAttributeService)
        {
            mockProductAttributeService.Setup(
                            x => x.GetProductAttributeMappingsByProductId(3))
                            .Returns(new List<ProductAttributeMapping>()
                            {
                    new ProductAttributeMapping()
                    {
                        Id = 2,
                        ProductId = 3,
                        ProductAttributeId = 1
                    }
                            }
                        );

            mockProductAttributeService.Setup(
                x => x.GetProductAttributeValues(2))
                .Returns(new List<ProductAttributeValue>()
                {
                    new ProductAttributeValue() { Name = "Twin", PriceAdjustment = 100 }
                }
            );
        }

        private static void SetupProductWithTwinAndQueenMattresses(Mock<IProductAttributeService> mockProductAttributeService)
        {
            mockProductAttributeService.Setup(
                            x => x.GetProductAttributeMappingsByProductId(2))
                            .Returns(new List<ProductAttributeMapping>()
                            {
                    new ProductAttributeMapping()
                    {
                        Id = 1,
                        ProductId = 2,
                        ProductAttributeId = 1
                    }
                            }
                        );
            mockProductAttributeService.Setup(
                x => x.GetProductAttributeValues(1))
                .Returns(new List<ProductAttributeValue>()
                {
                    new ProductAttributeValue() { Name = "Twin", PriceAdjustment = 100 },
                    new ProductAttributeValue() { Name = "Queen", PriceAdjustment = 200 }
                }
            );
        }

        private static void SetupProductWithNoMattresses(Mock<IProductAttributeService> mockProductAttributeService)
        {
            mockProductAttributeService.Setup(
                            x => x.GetProductAttributeMappingsByProductId(1))
                            .Returns(new List<ProductAttributeMapping>()
                        );
        }

        [Test]
        public void Returns_null_if_product_has_no_mattress_sizes()
        {
            var price = _abcMattressListingPriceService.GetListingPriceForMattressProduct
            (
                productWithNoMattresses.Id, "twin"
            );

            price.Should().Be(null);
        }

        [Test]
        public void Returns_matched_price_for_mattress_product()
        {
            var price = _abcMattressListingPriceService.GetListingPriceForMattressProduct
            (
                productWithTwinAndQueenMattresses.Id, "twin"
            );

            price.Should().Be(100);
        }

        [Test]
        public void Returns_queen_price_for_unmatched_mattress_product()
        {
            var price = _abcMattressListingPriceService.GetListingPriceForMattressProduct
            (
                productWithTwinAndQueenMattresses.Id, "king"
            );

            price.Should().Be(200);
        }

        [Test]
        public void Returns_null_for_unmatched_mattress_product_with_no_default()
        {
            var price = _abcMattressListingPriceService.GetListingPriceForMattressProduct
            (
                productWithTwinMattress.Id, "king"
            );

            price.Should().Be(null);
        }
    }
}