using System.Collections.Generic;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using Nop.Data;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public class AbcMattressGiftService : IAbcMattressGiftService
    {
        private readonly IRepository<AbcMattressGift> _abcMattressGiftRepository;

        public AbcMattressGiftService(
            IRepository<AbcMattressGift> abcMattressGiftRepository
        )
        {
            _abcMattressGiftRepository = abcMattressGiftRepository;
        }

        public IList<AbcMattressGift> GetAbcMattressGiftsByModel(string model)
        {
            return _abcMattressGiftRepository.Table.Where(amg => amg.Model == model).ToList();
        }
    }
}
