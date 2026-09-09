using backend.DTOs.DashboardDTOs;

namespace backend.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardOverviewDto> GetOverviewAsync(string period, DateTime? fromDate, DateTime? toDate);
        Task<DashboardSalesGeographyDto> GetSalesGeographyAsync(string period, DateTime? fromDate, DateTime? toDate);
        Task<DashboardInventoryCapacityDto> GetInventoryCapacityAsync();
        Task<DashboardQualityExpiryDto> GetQualityExpiryAsync();
        Task<DashboardFinancialPerformanceDto> GetFinancialPerformanceAsync(string period, DateTime? fromDate, DateTime? toDate);
        Task<DashboardPriceVolatilityDto> GetPriceVolatilityAsync(int? variantId, string timeframe, DateTime? fromDate, DateTime? toDate);
        Task<List<SkuSelectItemDto>> GetPriceVolatilitySkusAsync();
    }
}
