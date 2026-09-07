using backend.DTOs.VehicleDTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace backend.Services.Interfaces
{
    public interface IVehicleService
    {
        Task<List<DeliveryVehicleReadDto>> GetAllAsync(string? vehicleType = null, string? status = null);
        Task<DeliveryVehicleReadDto?> GetByIdAsync(int id);
        Task<DeliveryVehicleReadDto> CreateAsync(DeliveryVehicleCreateDto dto);
        Task<DeliveryVehicleReadDto?> UpdateAsync(int id, DeliveryVehicleUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }
}
