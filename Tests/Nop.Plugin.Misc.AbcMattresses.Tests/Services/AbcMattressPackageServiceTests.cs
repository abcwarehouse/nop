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
    public class AbcMattressPackageServiceTests
    {
        private IAbcMattressPackageService _abcMattressPackageService;

        [SetUp]
        public void Setup()
        {
            var mockAbcMattressPackageRepository = new Mock<IRepository<AbcMattressPackage>>();
            mockAbcMattressPackageRepository.Setup(p => p.Table).Returns(GetMockAbcMattressPackages);

            _abcMattressPackageService = new AbcMattressPackageService(
                mockAbcMattressPackageRepository.Object
            );
        }

        [Test]
        [TestCase(37325, 37306, 67325)]
        [TestCase(37325, 37300, 77325)]
        [TestCase(37330, null, 67330)]
        [TestCase(37330, 0, 67330)]
        public void Returns_AbcMattressPackage_By_Mattress_And_Base_ItemNos(
            int mattressItemNo,
            int baseItemNo,
            int packageItemNo
        )
        {
            var abcMattressPackage = _abcMattressPackageService.GetAbcMattressPackageByMattressAndBaseItemNos(
                mattressItemNo, baseItemNo
            );

            abcMattressPackage.Should().BeOfType<AbcMattressPackage>();
            abcMattressPackage.ItemNo.Should().Be(packageItemNo);
        }

        private IQueryable<AbcMattressPackage> GetMockAbcMattressPackages()
        {
            return new List<AbcMattressPackage>
            {
                new AbcMattressPackage()
                {
                    MattressItemNo = 37325,
                    BaseItemNo = 37306,
                    ItemNo = 67325,
                    Price = 2688
                },
                new AbcMattressPackage()
                {
                    MattressItemNo = 37325,
                    BaseItemNo = 37300,
                    ItemNo = 77325,
                    Price = 2688
                },
                new AbcMattressPackage()
                {
                    MattressItemNo = 37330,
                    BaseItemNo = 0,
                    ItemNo = 67330,
                    Price = 3888
                }
            }.AsQueryable();
        }
    }
}