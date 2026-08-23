using backend.DTOs.PaymentDTOs;
using Microsoft.AspNetCore.Http;

namespace backend.Services.Interfaces
{
    public interface IVnPayService
    {
        Task<VnPayPaymentResponseDto> CreatePaymentUrlAsync(VnPayPaymentRequestDto request, HttpContext httpContext);
        VnPayCallbackResultDto ProcessCallback(IQueryCollection query);
        Task<VnPayIpnResponseDto> ProcessIpnAsync(IQueryCollection query);
    }
}
