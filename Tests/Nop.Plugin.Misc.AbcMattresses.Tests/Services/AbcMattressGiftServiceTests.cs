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
    public class AbcMattressGiftServiceTests
    {
        private IAbcMattressGiftService _abcMattressGiftService;

        [SetUp]
        public void Setup()
        {
            var mockAbcMattressGiftRepository = new Mock<IRepository<AbcMattressGift>>();
            mockAbcMattressGiftRepository.Setup(p => p.Table).Returns(GetMockAbcMattressEntries);

            _abcMattressGiftService = new AbcMattressGiftService(
                mockAbcMattressGiftRepository.Object
            );
        }

        [Test]
        [TestCase("TrustII-Hybrid", 1)]
        [TestCase("KelburnII", 1)]
        [TestCase("Non-existent", 0)]
        public void Returns_AbcMattressEntries_By_Model(string model, int count)
        {
            var abcMattressEntries = _abcMattressGiftService.GetAbcMattressGiftsByModel(model);

            abcMattressEntries.Should().AllBeOfType<AbcMattressGift>();
            abcMattressEntries.Should().HaveCount(count);
        }

        private IQueryable<AbcMattressGift> GetMockAbcMattressEntries()
        {
            return new List<AbcMattressGift>
            {
                new AbcMattressGift()
                {
                    Model = "TrustII-Hybrid",
                    ItemNo = 80538,
                    Description = "FREE-BOSE-SLEEPBUDS-ALLOWANCE",
                    Amount = 208
                },
                new AbcMattressGift()
                {
                    Model = "KelburnII",
                    ItemNo = 80570,
                    Description = "FREE-43\"-4K-SMART-TV-ALLOWANCE",
                    Amount = 208
                },
            }.AsQueryable();
        }
    }
}