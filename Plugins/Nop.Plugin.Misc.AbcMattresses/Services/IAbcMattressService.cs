using System.Collections.Generic;
using Nop.Plugin.Misc.AbcMattresses.Domain;

namespace Nop.Plugin.Misc.AbcMattresses.Services
{
    public interface IAbcMattressService
    {
        IList<AbcMattress> GetAllAbcMattresses(); 
    }
}
