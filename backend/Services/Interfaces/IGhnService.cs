using backend.DTOs.ShippingDTOs;

namespace backend.Services.Interfaces
{
    public interface IGhnService
    {
        Task<List<GhnProvinceDto>> GetProvincesAsync();
        Task<List<GhnDistrictDto>> GetDistrictsAsync(int provinceId);
        Task<List<GhnWardDto>> GetWardsAsync(int districtId);
        Task<GhnCalculateFeeResponseDto> CalculateShippingFeeAsync(GhnCalculateFeeRequestDto request);
        Task<GhnCreateOrderResponseDto> CreateShippingOrderAsync(int orderId);
    }
}
