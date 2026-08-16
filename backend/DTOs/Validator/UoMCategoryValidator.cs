using backend.DTOs.UoMCategoryDTOs;
using FluentValidation;

namespace backend.DTOs.Validators
{
    public class UoMCategoryCreateDtoValidator : AbstractValidator<UoMCategoryCreateDto>
    {
        public UoMCategoryCreateDtoValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Mã nhóm đơn vị không được trống.").MaximumLength(50);
            RuleFor(x => x.Name).NotEmpty().WithMessage("Tên nhóm đơn vị không được trống.").MaximumLength(100);
        }
    }

    public class UoMCategoryUpdateDtoValidator : AbstractValidator<UoMCategoryUpdateDto>
    {
        public UoMCategoryUpdateDtoValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Mã nhóm đơn vị không được trống.").MaximumLength(50);
            RuleFor(x => x.Name).NotEmpty().WithMessage("Tên nhóm đơn vị không được trống.").MaximumLength(100);
        }
    }
}