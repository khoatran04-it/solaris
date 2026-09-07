using AutoMapper;
using backend.DTOs.VehicleDTOs;
using backend.Models;
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
                .ForMember(dest => dest.TotalOrders, opt => opt.MapFrom(src => src.TripOrders != null ? src.TripOrders.Count : 0))
                .ForMember(dest => dest.DeliveredOrders, opt => opt.MapFrom(src => src.TripOrders != null ? src.TripOrders.Count(o => o.Status == "Delivered") : 0))
                .ForMember(dest => dest.TransferCode, opt => opt.MapFrom(src => src.InventoryTransfer != null ? src.InventoryTransfer.TransferCode : null))
                .ForMember(dest => dest.Orders, opt => opt.MapFrom(src => src.TripOrders));

            CreateMap<DeliveryTripOrder, DeliveryTripOrderReadDto>()
                .ForMember(dest => dest.OrderCode, opt => opt.MapFrom(src => src.Order != null ? src.Order.OrderCode : string.Empty))
                .ForMember(dest => dest.ReceiverName, opt => opt.MapFrom(src => src.Order != null ? src.Order.ReceiverName : string.Empty))
                .ForMember(dest => dest.ReceiverPhone, opt => opt.MapFrom(src => src.Order != null ? src.Order.ReceiverPhone : string.Empty))
                .ForMember(dest => dest.DeliveryAddress, opt => opt.MapFrom(src => src.Order != null ? src.Order.DeliveryAddress : string.Empty))
                .ForMember(dest => dest.TotalAmount, opt => opt.MapFrom(src => src.Order != null ? src.Order.TotalAmount : 0));
        }
    }
}
