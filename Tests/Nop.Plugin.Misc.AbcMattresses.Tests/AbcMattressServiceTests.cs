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
            abcMattresses.Should().HaveCount(5);
        }

        private IQueryable<AbcMattress> GetMockAbcMattresses()
        {
            return new List<AbcMattress>
            {
                new AbcMattress()
                {
                    Model = "Dewitt",
                    Size = AbcMattressSize.Twin,
                    Brand = "Serta",
                    Comfort = "Cushion-Firm",
                    MattressType = "Inner Spring",
                    Foundation = "Low Profile",
                    AdjustableBase = null,
                    PackageItemNo = 66844,
                    PackagePrice = 249M,
                    MattressItemNo = 36844,
                    MattressPrice = 175M,
                    BaseItemNo = 37337,
                    BasePrice = 130,
                    AdjBaseItemNo = null,
                    AdjBasePrice = 0
                },
                new AbcMattress()
                {
                    Model = "Dewitt",
                    Size = AbcMattressSize.TwinXL,
                    Brand = "Serta",
                    Comfort = "Cushion-Firm",
                    MattressType = "Inner Spring",
                    Foundation = "Low Profile",
                    AdjustableBase = null,
                    PackageItemNo = 66845,
                    PackagePrice = 279M,
                    MattressItemNo = 36845,
                    MattressPrice = 199M,
                    BaseItemNo = 37338,
                    BasePrice = 140,
                    AdjBaseItemNo = null,
                    AdjBasePrice = 0
                },
                new AbcMattress()
                {
                    Model = "Dewitt",
                    Size = AbcMattressSize.Full,
                    Brand = "Serta",
                    Comfort = "Cushion-Firm",
                    MattressType = "Inner Spring",
                    Foundation = "Low Profile",
                    AdjustableBase = null,
                    PackageItemNo = 66846,
                    PackagePrice = 359M,
                    MattressItemNo = 36846,
                    MattressPrice = 267M,
                    BaseItemNo = 37339,
                    BasePrice = 170,
                    AdjBaseItemNo = null,
                    AdjBasePrice = 0
                },
                new AbcMattress()
                {
                    Model = "Dewitt",
                    Size = AbcMattressSize.Queen,
                    Brand = "Serta",
                    Comfort = "Cushion-Firm",
                    MattressType = "Inner Spring",
                    Foundation = "Low Profile",
                    AdjustableBase = null,
                    PackageItemNo = 66847,
                    PackagePrice = 399M,
                    MattressItemNo = 36847,
                    MattressPrice = 297M,
                    BaseItemNo = 37341,
                    BasePrice = 180,
                    AdjBaseItemNo = null,
                    AdjBasePrice = 0
                },
                new AbcMattress()
                {
                    Model = "Dewitt",
                    Size = AbcMattressSize.King,
                    Brand = "Serta",
                    Comfort = "Cushion-Firm",
                    MattressType = "Inner Spring",
                    Foundation = "Low Profile",
                    AdjustableBase = null,
                    PackageItemNo = 66848,
                    PackagePrice = 599M,
                    MattressItemNo = 36848,
                    MattressPrice = 397M,
                    BaseItemNo = 37338,
                    BasePrice = 140M,
                    AdjBaseItemNo = null,
                    AdjBasePrice = 0
                }
            }.AsQueryable();
        }
    }
}