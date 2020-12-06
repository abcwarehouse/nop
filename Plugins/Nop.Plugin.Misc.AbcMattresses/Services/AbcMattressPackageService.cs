using System.Collections.Generic;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using Nop.Data;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public class AbcMattressPackageService : IAbcMattressPackageService
    {
        private readonly IRepository<AbcMattressPackage> _abcMattressPackageRepository;

        public AbcMattressPackageService(
            IRepository<AbcMattressPackage> abcMattressPackageRepository
        )
        {
            _abcMattressPackageRepository = abcMattressPackageRepository;
        }

        public AbcMattressPackage GetAbcMattressPackageByMattressAndBaseItemNos(
            int mattressItemNo, int baseItemNo
        )
        {
            return _abcMattressPackageRepository.Table
                                                .Where(amp => amp.MattressItemNo == mattressItemNo &&
                                                              amp.BaseItemNo == baseItemNo)
                                                .FirstOrDefault();
        }
    }
}
