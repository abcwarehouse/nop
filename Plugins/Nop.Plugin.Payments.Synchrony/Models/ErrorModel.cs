using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Synchrony.Models
{
    public class ErrorModel
    {
        public string Message { get; set; }

        public bool isBack { get; set; }
    }
}
