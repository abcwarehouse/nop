using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nop.Web.Models.Checkout;

namespace Nop.Plugin.Payments.Synchrony.Models
{
    public class CustomCheckoutConfirmModel : CheckoutConfirmModel
    {
        public AuthenticationTokenResponse SynchronyAuthTokenResponse { get; set; }
    }
}
