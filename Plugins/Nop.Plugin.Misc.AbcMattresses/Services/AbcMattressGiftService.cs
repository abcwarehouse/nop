using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Seo;
using Nop.Data;
using Nop.Plugin.Misc.AbcCore.Extensions;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using Nop.Services.Catalog;
using Nop.Services.Seo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public class AbcMattressGiftService : IAbcMattressGiftService
    {
        private readonly IRepository<AbcMattressGift> _abcMattressGiftRepository;

        private readonly IAbcMattressModelGiftMappingService _abcMattressModelGiftMappingService;

        public AbcMattressGiftService(
            IRepository<AbcMattressGift> abcMattressGiftRepository,
            IAbcMattressModelGiftMappingService abcMattressModelGiftMappingService
        )
        {
            _abcMattressGiftRepository = abcMattressGiftRepository;
            _abcMattressModelGiftMappingService = abcMattressModelGiftMappingService;
        }

        public IList<AbcMattressGift> GetAbcMattressGiftsByModelId(int modelId)
        {
            var mappings = _abcMattressModelGiftMappingService.GetAbcMattressModelGiftMappingsByModelId(modelId);
            var giftIds = mappings.Select(m => m.AbcMattressGiftId);

            return _abcMattressGiftRepository.Table.Where(amb => giftIds.Contains(amb.Id)).ToList();
        }
    }
}
