using Nop.Data;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public class AbcMattressProtectorService : IAbcMattressProtectorService
    {
        private readonly IRepository<AbcMattressProtector> _abcMattressProtectorRepository;


        public AbcMattressProtectorService(
            IRepository<AbcMattressProtector> abcMattressProtectorRepository
        )
        {
            _abcMattressProtectorRepository = abcMattressProtectorRepository;
        }

        public IList<AbcMattressProtector> GetAbcMattressProtectorsBySize(string size)
        {
            return _abcMattressProtectorRepository.Table
                                                  .Where(p => p.Size.ToLower() == size.ToLower())
                                                  .ToList();
        }
    }
}
