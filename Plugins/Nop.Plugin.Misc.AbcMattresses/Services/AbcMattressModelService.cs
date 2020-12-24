using Nop.Data;
using Nop.Plugin.Misc.AbcMattresses.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public class AbcMattressModelService : IAbcMattressModelService
    {
        private readonly IRepository<AbcMattressModel> _abcMattressModelRepository;

        public AbcMattressModelService(
            IRepository<AbcMattressModel> abcMattressModelRepository
        )
        {
            _abcMattressModelRepository = abcMattressModelRepository;
        }

        public AbcMattressModel GetAbcMattressModelByProductId(int productId)
        {
            return GetAllAbcMattressModels().Where(amm => amm.ProductId == productId).FirstOrDefault();
        }

        public IList<AbcMattressModel> GetAllAbcMattressModels()
        {
            return _abcMattressModelRepository.Table.ToList();
        }

        public void UpdateAbcMattressModel(AbcMattressModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            _abcMattressModelRepository.Update(model);
        }
    }
}
