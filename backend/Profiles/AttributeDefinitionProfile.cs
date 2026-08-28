using AutoMapper;
using backend.DTOs.AttributeDefinitionDTOs;
using backend.Models;

namespace backend.Profiles
{
    /// <summary>
    /// Cấu hình ánh xạ dữ liệu (AutoMapper Profile) cho Từ điển Thuộc tính (Attribute Definition).
    /// </summary>
    public class AttributeDefinitionProfile : Profile
    {
        public AttributeDefinitionProfile()
        {
            #region Entity -> Read DTO
            CreateMap<AttributeDefinition, AttributeDefinitionReadDto>();
            #endregion

            #region Create DTO -> Entity
            CreateMap<AttributeDefinitionCreateDto, AttributeDefinition>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());
            #endregion

            #region Update DTO -> Entity
            CreateMap<AttributeDefinitionUpdateDto, AttributeDefinition>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());
            #endregion
        }
    }
}