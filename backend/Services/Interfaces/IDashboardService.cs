using backend.DTOs.DashboardDTOs;

namespace backend.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardOverviewDto> GetOverviewAsync(string period, DateTime? fromDate, DateTime? toDate, List<int>? allowedWarehouseIds = null);
        Task<DashboardSalesGeographyDto> GetSalesGeographyAsync(string period, DateTime? fromDate, DateTime? toDate, List<int>? allowedWarehouseIds = null);
        Task<DashboardInventoryCapacityDto> GetInventoryCapacityAsync(List<int>? allowedWarehouseIds = null);
        Task<DashboardQualityExpiryDto> GetQualityExpiryAsync(List<int>? allowedWarehouseIds = null);
        Task<DashboardFinancialPerformanceDto> GetFinancialPerformanceAsync(string period, DateTime? fromDate, DateTime? toDate, List<int>? allowedWarehouseIds = null);
        Task<DashboardPriceVolatilityDto> GetPriceVolatilityAsync(int? variantId, string timeframe, DateTime? fromDate, DateTime? toDate, List<int>? allowedWarehouseIds = null);
        Task<List<SkuSelectItemDto>> GetPriceVolatilitySkusAsync(List<int>? allowedWarehouseIds = null);
    }
}
