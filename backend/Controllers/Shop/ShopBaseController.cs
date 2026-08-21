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
            var customerIdClaim = User.FindFirst("CustomerId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(ClaimTypes.Name)?.Value;

            if (int.TryParse(customerIdClaim, out int customerId))
            {
                return customerId;
            }

            throw new UnauthorizedAccessException("Vui lòng đăng nhập để thực hiện thao tác này.");
        }

        protected int? TryGetCurrentCustomerId()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(customerIdClaim, out int customerId))
            {
                return customerId;
            }

            return null;
        }
    }
}
