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
    public class AbcMattressService : IAbcMattressService
    {
        private readonly IRepository<AbcMattressModel> _abcMattressModelRepository;

        public AbcMattressService(
            IRepository<AbcMattressModel> abcMattressModelRepository
        )
        {
            _abcMattressModelRepository = abcMattressModelRepository;
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
