using AutoMapper;
using backend.DTOs.VehicleDTOs;
using backend.Enums;
using backend.Models;
using backend.Models.Enums;
using System.Linq;

namespace backend.Profiles
{
    public class VehicleProfile : Profile
    {
        public VehicleProfile()
        {
            // DeliveryVehicle Mappings
            CreateMap<DeliveryVehicle, DeliveryVehicleReadDto>()
                .ForMember(dest => dest.HomeWarehouseName, opt => opt.MapFrom(src => src.HomeWarehouse != null ? src.HomeWarehouse.Name : null));

            CreateMap<DeliveryVehicleCreateDto, DeliveryVehicle>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.HomeWarehouse, opt => opt.Ignore())
                .ForMember(dest => dest.Trips, opt => opt.Ignore());

            CreateMap<DeliveryVehicleUpdateDto, DeliveryVehicle>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore())
                .ForMember(dest => dest.HomeWarehouse, opt => opt.Ignore())
                .ForMember(dest => dest.Trips, opt => opt.Ignore());

            // DeliveryTrip Mappings
            CreateMap<DeliveryTrip, DeliveryTripReadDto>()
                .ForMember(dest => dest.VehicleCode, opt => opt.MapFrom(src => src.Vehicle != null ? src.Vehicle.Code : string.Empty))
                .ForMember(dest => dest.VehicleType, opt => opt.MapFrom(src => src.Vehicle != null ? src.Vehicle.VehicleType : string.Empty))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse != null ? src.Warehouse.Name : string.Empty))
                .ForMember(dest => dest.TotalOrders, opt => opt.MapFrom(src => src.TripType == "B2C_Return" ? (src.CustomerReturns != null ? src.CustomerReturns.Count : 0) : (src.TripOrders != null ? src.TripOrders.Count : 0)))
                .ForMember(dest => dest.DeliveredOrders, opt => opt.MapFrom(src => src.TripType == "B2C_Return" ? (src.CustomerReturns != null ? src.CustomerReturns.Count(r => r.Status == CustomerReturnStatus.Inspecting || r.Status == CustomerReturnStatus.Completed) : 0) : (src.TripOrders != null ? src.TripOrders.Count(o => o.Status == "Delivered") : 0)))
                .ForMember(dest => dest.TransferCode, opt => opt.MapFrom(src => src.InventoryTransfer != null ? src.InventoryTransfer.TransferCode : null))
                .ForMember(dest => dest.FromWarehouseName, opt => opt.MapFrom(src => src.InventoryTransfer != null && src.InventoryTransfer.FromWarehouse != null ? src.InventoryTransfer.FromWarehouse.Name : null))
                .ForMember(dest => dest.ToWarehouseName, opt => opt.MapFrom(src => src.InventoryTransfer != null && src.InventoryTransfer.ToWarehouse != null ? src.InventoryTransfer.ToWarehouse.Name : null))
                .ForMember(dest => dest.Orders, opt => opt.MapFrom(src => src.TripOrders))
                .ForMember(dest => dest.Returns, opt => opt.MapFrom(src => src.CustomerReturns));

            CreateMap<DeliveryTripOrder, DeliveryTripOrderReadDto>()
                .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderCode : string.Empty))
                .ForMember(dest => dest.ReceiverName, opt => opt.MapFrom(src => src.Order != null ? src.Order.ReceiverName : string.Empty))
                .ForMember(dest => dest.ReceiverPhone, opt => opt.MapFrom(src => src.Order != null ? src.Order.ReceiverPhone : string.Empty))
                .ForMember(dest => dest.DeliveryAddress, opt => opt.MapFrom(src => src.Order != null ? src.Order.DeliveryAddress : string.Empty))
                .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.Order != null ? src.Order.TotalAmount : 0))
                .ForMember(dest => dest.PaymentMethodName, opt => opt.MapFrom(src => src.Order != null ? src.Order.PaymentMethod.ToString() : string.Empty))
                .ForMember(dest => dest.PaymentStatusName, opt => opt.MapFrom(src => src.Order != null ? src.Order.PaymentStatus.ToString() : string.Empty))
                .ForMember(dest => dest.IsPaid, opt => opt.MapFrom(src => src.Order != null && (src.Order.PaymentStatus == PaymentStatus.Paid || src.Order.PaymentMethod != PaymentMethod.COD)))
                .ForMember(dest => dest.CodAmount, opt => opt.MapFrom(src => (src.Status != "Failed" && src.Order != null && src.Order.PaymentMethod == PaymentMethod.COD && src.Order.PaymentStatus != PaymentStatus.Paid) ? src.Order.TotalAmount : 0));

            CreateMap<CustomerReturn, DeliveryTripReturnReadDto>()
                .ForMember(dest => dest.ReturnId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderCode : string.Empty))
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.Name : (src.Order != null ? src.Order.ReceiverName : string.Empty)))
                .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Customer != null ? src.Customer.PhoneNumber : (src.Order != null ? src.Order.ReceiverPhone : string.Empty)))
                .ForMember(dest => dest.PickupAddress, opt => opt.MapFrom(src => src.Order != null ? src.Order.DeliveryAddress : string.Empty))
                .ForMember(dest => dest.TotalItems, opt => opt.MapFrom(src => src.Details != null ? src.Details.Count : 0))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status == CustomerReturnStatus.PickingUp ? "Đang thu hồi" : src.Status == CustomerReturnStatus.Inspecting ? "Chờ kiểm định QC" : src.Status.ToString()));
        }
    }
}
