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
        public string? FromWarehouseName { get; set; }
        public string? ToWarehouseName { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<DeliveryTripOrderReadDto> Orders { get; set; } = new();
        public List<DeliveryTripReturnReadDto> Returns { get; set; } = new();
    }

    public class DeliveryTripReturnReadDto
    {
        public int ReturnId { get; set; }
        public string ReturnCode { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string PickupAddress { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public decimal RefundAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
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
        /// <summary>Danh sách ID phiếu trả hàng cần thu hồi (nếu TripType == B2C_Return).</summary>
        public List<int> CustomerReturnIds { get; set; } = new();
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
        public string PaymentMethodName { get; set; } = string.Empty;
        public string PaymentStatusName { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        public decimal CodAmount { get; set; }
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
        public List<TransferWaitingDispatchDto> TransfersWaitingDispatch { get; set; } = new();
        public List<ReturnWaitingDispatchDto> ReturnsWaitingDispatch { get; set; } = new();
    }

    public class ReturnWaitingDispatchDto
    {
        public int ReturnId { get; set; }
        public string ReturnCode { get; set; } = string.Empty;
        public int OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string PickupAddress { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public decimal TotalRefundEstimated { get; set; }
        public int TotalItems { get; set; }
        public DateTime ReturnDate { get; set; }
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
        public int? WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public bool HasCompletedIssue { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TransferWaitingDispatchDto
    {
        public int TransferId { get; set; }
        public string TransferCode { get; set; } = string.Empty;
        public int FromWarehouseId { get; set; }
        public string FromWarehouseName { get; set; } = string.Empty;
        public int ToWarehouseId { get; set; }
        public string ToWarehouseName { get; set; } = string.Empty;
        public int TotalItems { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Note { get; set; }
    }
}
