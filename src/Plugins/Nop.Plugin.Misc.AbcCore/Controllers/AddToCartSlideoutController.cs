using LinqToDB;
using LinqToDB.Data;
using Microsoft.AspNetCore.Mvc;
using Nop.Data;
using Nop.Web.Framework.Controllers;
using System.Data;
using System.Threading.Tasks;

namespace Nop.Plugin.Misc.AbcCore.Controllers
{
    public class AddToCartSlideoutController : BasePluginController
    {
        private readonly INopDataProvider _nopDataProvider;

        public AddToCartSlideoutController(
            INopDataProvider nopDataProvider
        ) {
            _nopDataProvider = nopDataProvider;
        }

        public async Task<IActionResult> GetDeliveryOptions(int? productId, int? zip)
        {
            if (zip == null || zip.ToString().Length != 5)
            {
                return BadRequest("Zip code must be a 5 digit number provided as a query parameter 'zip'.");
            }

            var returnCode = new DataParameter { Name = "ReturnCode", DataType = DataType.Int32, Direction = ParameterDirection.Output };
            var parameters = new DataParameter[] { returnCode, new DataParameter { Name = "zip", DataType = DataType.Int32, Value = zip } };
            await _nopDataProvider.ExecuteNonQueryAsync("EXEC @ReturnCode = ZipIsHomeDelivery @zip", dataParameters: parameters);

            return Json(new {
                isDeliveryAvailable = returnCode.Value.Equals(1)
            });
        }
    }
}
