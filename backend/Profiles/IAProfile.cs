using AutoMapper;
using backend.DTOs.AuthDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho phân hệ Phân quyền & Định danh (Identity & Access).
    /// </summary>
    public class IAProfile : Profile
    {
        public IAProfile()
        {
            #region 1. Ánh xạ Vai trò (Role Mapping)
            CreateMap<IARole, IARoleReadDto>()
                .ForMember(dest => dest.PermissionIds,
                    opt => opt.MapFrom(src => src.RolePermissions.Select(rp => rp.PermissionId).ToList()));

            // Bỏ qua ánh xạ RolePermissions vì quan hệ Many-to-Many được xử lý tập trung tại Service Layer
            CreateMap<IARoleCreateDto, IARole>()
                .ForMember(dest => dest.RolePermissions, opt => opt.Ignore());

            CreateMap<IARoleUpdateDto, IARole>()
                .ForMember(dest => dest.RolePermissions, opt => opt.Ignore());
            #endregion

            #region 2. Ánh xạ Người dùng (User Mapping)
            CreateMap<IAUserPermission, CustomPermissionDto>();

            CreateMap<IAUser, IAUserReadDto>()
                .ForMember(dest => dest.RoleIds,
                    opt => opt.MapFrom(src => src.UserRoles.Select(ur => ur.RoleId).ToList()))
                .ForMember(dest => dest.WarehouseIds,
                    opt => opt.MapFrom(src => src.UserWarehouses.Select(uw => uw.WarehouseId).ToList()))
                .ForMember(dest => dest.CustomPermissions,
                    opt => opt.MapFrom(src => src.UserPermissions));

            // PasswordHash được băm (BCrypt) và các bảng trung gian được đồng bộ thủ công tại Service
            CreateMap<IAUserCreateDto, IAUser>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
                .ForMember(dest => dest.UserWarehouses, opt => opt.Ignore())
                .ForMember(dest => dest.UserPermissions, opt => opt.Ignore());

            CreateMap<IAUserUpdateDto, IAUser>()
                .ForMember(dest => dest.UserRoles, opt => opt.Ignore())
                .ForMember(dest => dest.UserWarehouses, opt => opt.Ignore())
                .ForMember(dest => dest.UserPermissions, opt => opt.Ignore());
            #endregion
        }
    }
}