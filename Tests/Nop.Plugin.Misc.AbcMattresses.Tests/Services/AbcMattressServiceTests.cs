using NUnit.Framework;
using FluentAssertions;
using Nop.Plugin.Misc.AbcMattresses.Services;
using Moq;
using System.Collections.Generic;
using Nop.Data;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses.Tests.Services
{
    public class AbcMattressServiceTests
    {
        private IAbcMattressService _abcMattressService;

        [SetUp]
        public void Setup()
        {
            var mockAbcMattressRepository = new Mock<IRepository<AbcMattress>>();
            mockAbcMattressRepository.Setup(p => p.Table).Returns(GetMockAbcMattresses);

            _abcMattressService = new AbcMattressService(
                mockAbcMattressRepository.Object
            );
        }

        [Test]
        public void Returns_All_AbcMattresses()
        {
            var abcMattresses = _abcMattressService.GetAllAbcMattresses();

            abcMattresses.Should().AllBeOfType<AbcMattress>();
            abcMattresses.Should().HaveCount(4);
        }

        private IQueryable<AbcMattress> GetMockAbcMattresses()
        {
            return new List<AbcMattress>
            {
                new AbcMattress()
                {
                    Model = "Alverson",
                    Brand = "Serta",
                    Comfort = "Firm"
                },
                new AbcMattress()
                {
                    Model = "Carrollton",
                    Brand = "Serta",
                    Comfort = "Firm"
                },
                new AbcMattress()
                {
                    Model = "Dewitt",
                    Brand = "Serta",
                    Comfort = "Cushion-Firm"
                },
                new AbcMattress()
                {
                    Model = "TrustII-Hybrid",
                    Brand = "Sealy Hybrid",
                    Comfort = "Cushion-Firm"
                }
            }.AsQueryable();
        }
    }
}