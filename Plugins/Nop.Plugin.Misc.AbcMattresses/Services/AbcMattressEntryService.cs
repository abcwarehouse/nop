using System.Collections.Generic;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using Nop.Data;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public class AbcMattressEntryService : IAbcMattressEntryService
    {
        private readonly IRepository<AbcMattressEntry> _abcMattressEntryRepository;

        public AbcMattressEntryService(
            IRepository<AbcMattressEntry> abcMattressEntryRepository
        )
        {
            _abcMattressEntryRepository = abcMattressEntryRepository;
        }

        public IList<AbcMattressEntry> GetAbcMattressEntriesByModel(string model)
        {
            return _abcMattressEntryRepository.Table.Where(ame => ame.Model == model).ToList();
        }
    }
}
