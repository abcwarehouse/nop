using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace Nop.Plugin.Payments.AbcCard.Models
{
    public class PaymentInfoModel : BaseNopModel
    {
        public string DescriptionText { get; set; }

        [NopResourceDisplayName("Payment.CardNumber")]
        [Required]
        [CreditCard]
        public string CardNumber { get; set; }

    }
}