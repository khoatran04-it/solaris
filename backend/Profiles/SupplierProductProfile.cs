using AutoMapper;
using backend.DTOs.SupplierProductDTOs;
using backend.Models;

namespace backend.Profiles
{
    public class SupplierProductProfile : Profile
    {
        public SupplierProductProfile()
        {
            // GET
            CreateMap<SupplierProduct, SupplierProductReadDto>()
                .ForMember(dest => dest.VariantCode, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Code : null))
                .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.Name : null))
                .ForMember(dest => dest.VariantImagePath, opt => opt.MapFrom(src => src.Variant != null ? src.Variant.ImagePath : null))
                .ForMember(dest => dest.SupplierCode, opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Code : null))
                .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : null))
                .ForMember(dest => dest.PurchaseUoMName, opt => opt.MapFrom(src => src.PurchaseUoM != null ? src.PurchaseUoM.Name : null))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // POST
            CreateMap<SupplierProductCreateDto, SupplierProduct>()
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

            // PUT
            CreateMap<SupplierProductUpdateDto, SupplierProduct>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
        }
    }
}
