using System.Collections.Generic;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcMattresses.Domain;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public interface IAbcMattressService
    {
        IList<AbcMattressModel> GetAllAbcMattressModels();

        Product CreateAbcMattressProduct(AbcMattressModel abcMattressModel);
    }
}
