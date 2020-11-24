using Nop.Core.Domain.Common;
using Nop.Plugin.Misc.AbcSync.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nop.Plugin.Misc.AbcSync.Services
{
    public interface IGeocodingService
    {
        Coordinate GeocodeAddress(Address address);
    }
}