using backend.DTOs.VehicleDTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    public interface IDeliveryTripService
    {
        Task<List<DeliveryTripReadDto>> GetAllTripsAsync(string? status = null, string? tripType = null);
        Task<DeliveryTripReadDto?> GetTripByIdAsync(int id);
        Task<DeliveryTripReadDto> CreateTripAsync(DeliveryTripCreateDto dto);
        Task<DeliveryTripReadDto> DispatchSingleOrderInternalAsync(int orderId, DispatchInternalOrderDto dto);
        Task<DeliveryTripReadDto> StartTripAsync(int tripId);
        Task<DeliveryTripReadDto> CompleteTripAsync(int tripId);
        Task<DeliveryTripReadDto> CancelTripAsync(int tripId, string reason);
        Task<DeliveryTripReadDto> MarkTripOrderDeliveredAsync(int tripId, int orderId, string? note);
        Task<DeliveryTripReadDto> MarkTripOrderFailedAsync(int tripId, int orderId, string reason);
        Task<DeliveryTripReadDto> MarkTripReturnPickedUpAsync(int tripId, int returnId, string? note);
        Task<DeliveryTripReadDto> MarkTripReturnFailedAsync(int tripId, int returnId, string reason);
        Task<TransportationDashboardStatsDto> GetTransportationDashboardStatsAsync();
    }
}
