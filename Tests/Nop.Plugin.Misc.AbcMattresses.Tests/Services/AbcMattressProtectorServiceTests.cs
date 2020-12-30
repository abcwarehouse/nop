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
    public class AbcMattressProtectorServiceTests
    {
        private IAbcMattressProtectorService _abcMattressProtectorService;

        private Mock<IRepository<AbcMattressProtector>> _abcMattressProtectorRepository;

        private AbcMattressProtector _twin = new AbcMattressProtector()
        {
            Size = "Twin",
            ItemNo = "12345",
            Name = "Good",
            Price = 19.99M
        };
        private AbcMattressProtector _twinXl = new AbcMattressProtector()
        {
            Size = "TwinXL",
            ItemNo = "23456",
            Name = "Better",
            Price = 19.99M
        };

        [SetUp]
        public void Setup()
        {
            _abcMattressProtectorRepository = new Mock<IRepository<AbcMattressProtector>>();
            _abcMattressProtectorRepository.Setup(p => p.Table).Returns(
                new List<AbcMattressProtector>
                {
                    _twin,
                    _twinXl,
                }.AsQueryable());

            _abcMattressProtectorService = new AbcMattressProtectorService(
                _abcMattressProtectorRepository.Object
            );
        }

        [Test]
        public void Gets_Protectors_By_Size()
        {
            var twinProtectors = _abcMattressProtectorService.GetAbcMattressProtectorsBySize("Twin");
            var queenProtectors = _abcMattressProtectorService.GetAbcMattressProtectorsBySize("Queen");

            twinProtectors.Should().HaveCount(1);
            queenProtectors.Should().HaveCount(0);
        }
    }
}