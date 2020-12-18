using System.Collections.Generic;
using Nop.Core.Domain.Catalog;
using Nop.Plugin.Misc.AbcMattresses.Domain;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public interface IAbcMattressProductService
    {
        Product UpsertAbcMattressProduct(AbcMattressModel model);
        void SetManufacturer(AbcMattressModel model, Product product);
        void SetCategories(AbcMattressModel model, Product product);
        void SetProductAttributes(AbcMattressModel model, Product product);
    }
}
