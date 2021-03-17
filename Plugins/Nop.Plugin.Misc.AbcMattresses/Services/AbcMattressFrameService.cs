using Nop.Data;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public class AbcMattressFrameService : IAbcMattressFrameService
    {
        private readonly IRepository<AbcMattressFrame> _abcMattressFrameRepository;


        public AbcMattressFrameService(
            IRepository<AbcMattressFrame> abcMattressFrameRepository
        )
        {
            _abcMattressFrameRepository = abcMattressFrameRepository;
        }

        public IList<AbcMattressFrame> GetAbcMattressFramesBySize(string size)
        {
            return _abcMattressFrameRepository.Table
                                              .Where(p => p.Size.ToLower() == size.ToLower() &&
                                                          p.Price > 0M)
                                              .ToList();
        }
    }
}
