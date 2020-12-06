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
    public class AbcMattressEntryServiceTests
    {
        private IAbcMattressEntryService _abcMattressEntryService;

        [SetUp]
        public void Setup()
        {
            var mockAbcMattressEntryRepository = new Mock<IRepository<AbcMattressEntry>>();
            mockAbcMattressEntryRepository.Setup(p => p.Table).Returns(GetMockAbcMattressEntries);

            _abcMattressEntryService = new AbcMattressEntryService(
                mockAbcMattressEntryRepository.Object
            );
        }

        [Test]
        [TestCase("Alverson", 1)]
        [TestCase("Carrollton", 1)]
        [TestCase("Non-existent", 0)]
        public void Returns_AbcMattressEntries_By_Model(string model, int count)
        {
            var abcMattressEntries = _abcMattressEntryService.GetAbcMattressEntriesByModel(model);

            abcMattressEntries.Should().AllBeOfType<AbcMattressEntry>();
            abcMattressEntries.Should().HaveCount(count);
        }

        private IQueryable<AbcMattressEntry> GetMockAbcMattressEntries()
        {
            return new List<AbcMattressEntry>
            {
                new AbcMattressEntry()
                {
                    Model = "Alverson",
                    Size = "Twin",
                    MattressItemNo = 37092,
                    MattressPrice = 397,
                    MattressType = "Plush"
                },
                new AbcMattressEntry()
                {
                    Model = "Carrollton",
                    Size = "Twin",
                    MattressItemNo = 36750,
                    MattressPrice = 249,
                    MattressType = "Cushion-Firm"
                }
            }.AsQueryable();
        }
    }
}