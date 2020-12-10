using System.Collections.Generic;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcMattresses.Domain;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public interface IAbcMattressBaseService
    {
        IList<AbcMattressBase> GetAbcMattressBasesByModelId(int modelId);
    }
}
