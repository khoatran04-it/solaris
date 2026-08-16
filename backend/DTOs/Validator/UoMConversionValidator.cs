using backend.DTOs.UoMConversionDTOs;
using backend.DTOs.UoMDTOs;
using FluentValidation;

namespace backend.DTOs.Validators
{
    public class UoMConversionCreateDtoValidator : AbstractValidator<UoMConversionCreateDto>
    {
        public UoMConversionCreateDtoValidator()
        {
            RuleFor(x => x.FromUoMId).NotNull().WithMessage("Đơn vị gốc (FromUoM) không được trống.");

            RuleFor(x => x.ToUoMId)
                .NotNull().WithMessage("Đơn vị quy đổi (ToUoM) không được trống.")
                .NotEqual(x => x.FromUoMId).WithMessage("Đơn vị gốc và đơn vị đích không được trùng nhau.");

            RuleFor(x => x.ConversionFactor)
                .GreaterThan(0).WithMessage("Tỷ lệ quy đổi phải lớn hơn 0 (VD: 1 Thùng = 10 Cái).");
        }
    }

    public class UoMConversionUpdateDtoValidator : AbstractValidator<UoMConversionUpdateDto>
    {
        public UoMConversionUpdateDtoValidator()
        {
            RuleFor(x => x.FromUoMId).NotNull().WithMessage("Đơn vị gốc (FromUoM) không được trống.");

            RuleFor(x => x.ToUoMId)
                .NotNull().WithMessage("Đơn vị quy đổi (ToUoM) không được trống.")
                .NotEqual(x => x.FromUoMId).WithMessage("Đơn vị gốc và đơn vị đích không được trùng nhau.");

            RuleFor(x => x.ConversionFactor)
                .GreaterThan(0).WithMessage("Tỷ lệ quy đổi phải lớn hơn 0 (VD: 1 Thùng = 10 Cái).");
        }
    }
}