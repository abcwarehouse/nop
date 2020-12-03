using System.Collections.Generic;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using Nop.Data;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public class AbcMattressService : IAbcMattressService
    {
        private readonly IRepository<AbcMattress> _abcMattressRepository;

        public AbcMattressService(
            IRepository<AbcMattress> abcMattressRepository
        )
        {
            _abcMattressRepository = abcMattressRepository;
        }

        public IList<AbcMattress> GetAllAbcMattresses()
        {
            return _abcMattressRepository.Table.ToList();
        }
    }
}
