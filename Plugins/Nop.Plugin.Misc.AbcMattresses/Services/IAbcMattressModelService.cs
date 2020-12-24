using System.Collections.Generic;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcMattresses.Domain;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public interface IAbcMattressModelService
    {
        IList<AbcMattressModel> GetAllAbcMattressModels();
        void UpdateAbcMattressModel(AbcMattressModel model);
        AbcMattressModel GetAbcMattressModelByProductId(int productId);
    }
}
