using Nop.Plugin.Misc.AbcSync.Domain.Staging;
using System.Collections.Generic;

namespace Nop.Plugin.Misc.AbcSync.Services.Staging
{
    public interface IIsamProductService
    {
        IEnumerable<IsamProduct> GetIsamProducts();
    }
}