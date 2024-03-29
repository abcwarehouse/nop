using System.Collections.Generic;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcMattresses.Domain;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public interface IAbcMattressPackageService
    {
        IList<AbcMattressPackage> GetAllAbcMattressPackages();
        IList<AbcMattressPackage> GetAbcMattressPackagesByEntryIds(IEnumerable<int> entryIds);
        AbcMattressPackage GetAbcMattressPackageByEntryIdAndBaseId(int entryId, int baseId);
    }
}
