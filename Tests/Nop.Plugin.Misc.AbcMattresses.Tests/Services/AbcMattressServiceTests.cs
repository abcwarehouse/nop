using NUnit.Framework;
using FluentAssertions;
using Nop.Plugin.Misc.AbcMattresses.Services;
using Moq;
using System.Collections.Generic;
using Nop.Data;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses.Tests
{
    public class AbcMattressModelServiceTests
    {
        private IAbcMattressModelService _abcMattressService;

        private Mock<IRepository<AbcMattressModel>> _abcMattressModelRepository;

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
            _abcMattressModelRepository = new Mock<IRepository<AbcMattressModel>>();
            _abcMattressModelRepository.Setup(p => p.Table).Returns(
                new List<AbcMattressModel>
                {
                    _abcMattressModelNoProduct,
                    _abcMattressModelWithProduct,
                }.AsQueryable());

            _abcMattressService = new AbcMattressModelService(
                _abcMattressModelRepository.Object
            );
        }

        [Test]
        public void Gets_All_Models()
        {
            var abcMattressModels = _abcMattressService.GetAllAbcMattressModels();

            abcMattressModels.Should().HaveCount(2);
        }

        [Test]
        public void Get_Model_By_Product_Id()
        {
            var abcMattressModel = _abcMattressService.GetAbcMattressModelByProductId(
                _abcMattressModelWithProduct.ProductId.Value
            );

            abcMattressModel.Should().BeEquivalentTo(_abcMattressModelWithProduct);
        }

        [Test]
        public void Updates()
        {
            var newModel = _abcMattressModelNoProduct;
            newModel.ProductId = 1;
            _abcMattressService.UpdateAbcMattressModel(_abcMattressModelNoProduct);

            _abcMattressModelRepository.Verify(x => x.Update(newModel), Times.Once);
        }
    }
}