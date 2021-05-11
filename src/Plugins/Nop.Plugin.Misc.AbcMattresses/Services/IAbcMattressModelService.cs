using System.Collections.Generic;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public interface IAbcMattressModelService
    {
        IList<AbcMattressModel> GetAllAbcMattressModels();
        Task UpdateAbcMattressModelAsync(AbcMattressModel model);
        AbcMattressModel GetAbcMattressModelByProductId(int productId);
    }
}
