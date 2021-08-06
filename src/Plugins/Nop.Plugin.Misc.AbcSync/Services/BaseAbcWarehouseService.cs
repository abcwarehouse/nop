using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nop.Plugin.Misc.AbcSync
{
    public class BaseAbcWarehouseService
    {
        public bool _isInit;
        protected BaseAbcWarehouseService(bool isInit = false)
        {
            _isInit = isInit;
        }
    }
}