using AutoMapper;
using backend.Data;
using backend.DTOs.VehicleDTOs;
using backend.Models;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly SolarisDbContext _context;
        private readonly IMapper _mapper;

        public VehicleService(SolarisDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<List<DeliveryVehicleReadDto>> GetAllAsync(string? vehicleType = null, string? status = null)
        {
            var query = _context.DeliveryVehicles
                .Include(v => v.HomeWarehouse)
                .Where(v => !v.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(vehicleType))
            {
                query = query.Where(v => v.VehicleType.ToLower() == vehicleType.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(v => v.Status.ToLower() == status.ToLower());
            }

            var list = await query.OrderByDescending(v => v.CreatedAt).ToListAsync();
            return _mapper.Map<List<DeliveryVehicleReadDto>>(list);
        }

        public async Task<DeliveryVehicleReadDto?> GetByIdAsync(int id)
        {
            var vehicle = await _context.DeliveryVehicles
                .Include(v => v.HomeWarehouse)
                .FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);

            return vehicle == null ? null : _mapper.Map<DeliveryVehicleReadDto>(vehicle);
        }

        public async Task<DeliveryVehicleReadDto> CreateAsync(DeliveryVehicleCreateDto dto)
        {
            // Kiểm tra trùng mã xe hoặc biển số
            if (await _context.DeliveryVehicles.AnyAsync(v => v.Code.ToLower() == dto.Code.ToLower() && !v.IsDeleted))
            {
                throw new InvalidOperationException($"Mã phương tiện '{dto.Code}' đã tồn tại trong hệ thống.");
            }

            if (await _context.DeliveryVehicles.AnyAsync(v => v.LicensePlate.ToLower() == dto.LicensePlate.ToLower() && !v.IsDeleted))
            {
                throw new InvalidOperationException($"Biển số xe '{dto.LicensePlate}' đã tồn tại trong hệ thống.");
            }

            var vehicle = _mapper.Map<DeliveryVehicle>(dto);
            vehicle.CreatedAt = DateTime.UtcNow;
            vehicle.UpdatedAt = DateTime.UtcNow;

            _context.DeliveryVehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(vehicle.Id) ?? _mapper.Map<DeliveryVehicleReadDto>(vehicle);
        }

        public async Task<DeliveryVehicleReadDto?> UpdateAsync(int id, DeliveryVehicleUpdateDto dto)
        {
            var vehicle = await _context.DeliveryVehicles.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
            if (vehicle == null) return null;

            // Kiểm tra trùng biển số xe khác
            if (await _context.DeliveryVehicles.AnyAsync(v => v.Id != id && v.LicensePlate.ToLower() == dto.LicensePlate.ToLower() && !v.IsDeleted))
            {
                throw new InvalidOperationException($"Biển số xe '{dto.LicensePlate}' đã được sử dụng bởi xe khác.");
            }

            _mapper.Map(dto, vehicle);
            vehicle.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var vehicle = await _context.DeliveryVehicles.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted);
            if (vehicle == null) return false;

            // Không cho xóa nếu đang chạy chuyến
            if (vehicle.Status == "OnTrip")
            {
                throw new InvalidOperationException("Không thể xóa phương tiện đang trong lộ trình giao hàng.");
            }

            vehicle.IsDeleted = true;
            vehicle.DeletedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
