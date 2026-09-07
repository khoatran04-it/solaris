using System;
using System.Collections.Generic;

namespace backend.DTOs.VehicleDTOs
{
    // ==========================================
    // DTOs CHO PHƯƠNG TIỆN (VEHICLES)
    // ==========================================

    public class DeliveryVehicleReadDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty; // "Motorbike" hoặc "RefrigeratedTruck"
        public decimal MaxWeightKg { get; set; }
        public bool IsColdChainEquipped { get; set; }
        public string Status { get; set; } = "Available"; // "Available", "OnTrip", "Maintenance"
        public string? DriverName { get; set; }
        public string? DriverPhone { get; set; }
        public int? HomeWarehouseId { get; set; }
        public string? HomeWarehouseName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class DeliveryVehicleCreateDto
    {
        public string Code { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string VehicleType { get; set; } = "Motorbike";
        public decimal MaxWeightKg { get; set; } = 40;
        public bool IsColdChainEquipped { get; set; } = true;
        public string Status { get; set; } = "Available";
        public string? DriverName { get; set; }
        public string? DriverPhone { get; set; }
        public int? HomeWarehouseId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DeliveryVehicleUpdateDto
    {
        public string LicensePlate { get; set; } = string.Empty;
        public string VehicleType { get; set; } = "Motorbike";
        public decimal MaxWeightKg { get; set; }
        public bool IsColdChainEquipped { get; set; } = true;
        public string Status { get; set; } = "Available";
        public string? DriverName { get; set; }
        public string? DriverPhone { get; set; }
        public int? HomeWarehouseId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // ==========================================
    // DTOs CHO CHUYẾN XE (DELIVERY TRIPS)
    // ==========================================

    public class DeliveryTripReadDto
    {
        public int Id { get; set; }
        public string TripCode { get; set; } = string.Empty;
        public string TripType { get; set; } = string.Empty; // "B2C_Delivery" hoặc "B2B_Transfer"
        public string Status { get; set; } = "Preparing"; // "Preparing", "InTransit", "Completed", "Cancelled"
        public int VehicleId { get; set; }
        public string VehicleCode { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public string DriverPhone { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Note { get; set; }
        public int TotalOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int? InventoryTransferId { get; set; }
        public string? TransferCode { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<DeliveryTripOrderReadDto> Orders { get; set; } = new();
    }

    public class DeliveryTripCreateDto
    {
        public int VehicleId { get; set; }
        public int WarehouseId { get; set; }
        public string TripType { get; set; } = "B2C_Delivery"; // "B2C_Delivery" hoặc "B2B_Transfer"
        public string? Note { get; set; }
        /// <summary>Danh sách ID đơn hàng cần gộp vào chuyến (nếu TripType == B2C_Delivery).</summary>
        public List<int> OrderIds { get; set; } = new();
        /// <summary>ID phiếu điều chuyển liên kết (nếu TripType == B2B_Transfer).</summary>
        public int? InventoryTransferId { get; set; }
    }

    public class DeliveryTripOrderReadDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int DeliverySequence { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? DeliveredAt { get; set; }
        public string? FailureReason { get; set; }
        public string? Note { get; set; }
    }

    public class DispatchInternalOrderDto
    {
        public int VehicleId { get; set; }
        public string? Note { get; set; }
    }

    // ==========================================
    // DTOs THỐNG KÊ CHO DASHBOARD VẬN TẢI
    // ==========================================

    public class TransportationDashboardStatsDto
    {
        public int AvailableBikes { get; set; }
        public int TotalBikes { get; set; }
        public int ActiveTrucks { get; set; }
        public int TotalTrucks { get; set; }
        public int PendingColdChainOrders { get; set; }
        public int ActiveTripsCount { get; set; }
        public List<DeliveryTripReadDto> RecentActiveTrips { get; set; } = new();
        public List<OrderWaitingDispatchDto> OrdersWaitingDispatch { get; set; } = new();
    }

    public class OrderWaitingDispatchDto
    {
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public bool RequiresColdChain { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
