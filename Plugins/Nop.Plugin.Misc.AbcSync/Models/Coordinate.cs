using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nop.Plugin.Misc.AbcSync.Models
{
    public class Coordinate
    {
        public float Latitude { get; private set; }
        public float Longitude { get; private set; }

        public Coordinate(float latitude, float longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}