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
    public class AbcMattressEntryServiceTests
    {
        private IAbcMattressEntryService _abcMattressEntryService;

        private Mock<IRepository<AbcMattressEntry>> _abcMattressEntryRepository;

        private AbcMattressEntry _abcMattressEntry1 = new AbcMattressEntry()
        {
            AbcMattressModelId = 1,
            Size = "Twin",
            ItemNo = 12345,
            Price = 247.00M
        };
        private AbcMattressEntry _abcMattressEntry2 = new AbcMattressEntry()
        {
            AbcMattressModelId = 2,
            Size = "Twin",
            ItemNo = 12345,
            Price = 247.00M
        };

        [SetUp]
        public void Setup()
        {
            _abcMattressEntryRepository = new Mock<IRepository<AbcMattressEntry>>();
            _abcMattressEntryRepository.Setup(p => p.Table).Returns(
                new List<AbcMattressEntry>
                {
                    _abcMattressEntry1,
                    _abcMattressEntry2,
                }.AsQueryable());

            _abcMattressEntryService = new AbcMattressEntryService(
                _abcMattressEntryRepository.Object
            );
        }

        [Test]
        [TestCase(1, 1)]
        [TestCase(2, 1)]
        [TestCase(3, 0)]
        public void Gets_By_AbcMattressModelId(int modelId, int count)
        {
            var abcMattressModels = _abcMattressEntryService.GetAbcMattressEntriesByModelId(modelId);

            abcMattressModels.Should().HaveCount(count);
        }

        [Test]
        public void Gets_All()
        {
            var abcMattressModels = _abcMattressEntryService.GetAllAbcMattressEntries();

            abcMattressModels.Should().HaveCount(2);
        }
    }
}