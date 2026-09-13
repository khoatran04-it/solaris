using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers.Shop
{
    [ApiController]
    [Route("api/shop/[controller]")]
    public abstract class ShopBaseController : ControllerBase
    {
        protected int GetCurrentCustomerId()
        {
            var customerId = TryGetCurrentCustomerId();
            if (customerId.HasValue && customerId.Value > 0)
            {
                return customerId.Value;
            }

            throw new UnauthorizedAccessException("Vui lòng đăng nhập để thực hiện thao tác này.");
        }

        protected int? TryGetCurrentCustomerId()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("nameid")?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value;

            if (int.TryParse(customerIdClaim, out int customerId) && customerId > 0)
            {
                return customerId;
            }

            return null;
        }
    }
}
