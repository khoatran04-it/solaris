using AutoMapper;
using backend.DTOs.AuthDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class IAProfile : Profile
    {
        public IAProfile()
        {
            // =====================================
            // 1. MAP ROLE (VAI TRÒ)
            // =====================================
            CreateMap<IARole, IARoleReadDto>()
                .ForMember(dest => dest.PermissionIds, opt => opt.MapFrom(src => src.RolePermissions.Select(rp => rp.PermissionId).ToList()));

            CreateMap<IARoleCreateDto, IARole>()
                .ForMember(dest => dest.RolePermissions, opt => opt.Ignore()); // Ignore để tự xử lý ở Service

            CreateMap<IARoleUpdateDto, IARole>()
                .ForMember(dest => dest.RolePermissions, opt => opt.Ignore()); // Ignore để tự xử lý ở Service

            // =====================================
            // 2. MAP USER (NHÂN VIÊN)
            // =====================================
            CreateMap<IAUserPermission, CustomPermissionDto>()
                .ForMember(dest => dest.PermissionId, opt => opt.MapFrom(src => src.PermissionId))
                .ForMember(dest => dest.IsGranted, opt => opt.MapFrom(src => src.IsGranted));

            CreateMap<IAUser, IAUserReadDto>()
                .ForMember(dest => dest.RoleIds, opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.RoleId).ToList()))
                .ForMember(dest => dest.WarehouseIds, opt => opt.MapFrom(src => src.UserWarehouses.Select(uw => uw.WarehouseId).ToList()))
                .ForMember(dest => dest.CustomPermissions, opt => opt.MapFrom(src => src.UserPermissions.ToList()));

            CreateMap<IAUserCreateDto, IAUser>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore()) // Tự hash ở Service
                .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
                .ForMember(dest => dest.UserWarehouses, opt => opt.Ignore())
                .ForMember(dest => dest.UserPermissions, opt => opt.Ignore());

            CreateMap<IAUserUpdateDto, IAUser>()
                .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
                .ForMember(dest => dest.UserWarehouses, opt => opt.Ignore())
                .ForMember(dest => dest.UserPermissions, opt => opt.Ignore());
        }
    }
}