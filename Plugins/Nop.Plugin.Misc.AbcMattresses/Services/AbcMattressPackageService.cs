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
    public class AbcMattressPackageService : IAbcMattressPackageService
    {
        private readonly IRepository<AbcMattressPackage> _abcMattressPackageRepository;

        public AbcMattressPackageService(
            IRepository<AbcMattressPackage> abcMattressPackageRepository
        )
        {
            _abcMattressPackageRepository = abcMattressPackageRepository;
        }

        public IList<AbcMattressPackage> GetAllAbcMattressPackages()
        {
            return _abcMattressPackageRepository.Table.ToList();
        }
    }
}
