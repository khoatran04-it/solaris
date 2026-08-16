using FluentValidation;
using backend.DTOs.UoMDTOs;

namespace backend.DTOs.Validators
{
    public class UoMCreateDtoValidator : AbstractValidator<UoMCreateDto>
    {
        public UoMCreateDtoValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Mã đơn vị không được để trống.").MaximumLength(50);
            RuleFor(x => x.Name).NotEmpty().WithMessage("Tên đơn vị không được để trống.").MaximumLength(100);
            RuleFor(x => x.Synonyms).MaximumLength(200);
            RuleFor(x => x.CategoryId).NotNull().WithMessage("Phải chọn Nhóm đơn vị đo lường.");
        }
    }

    public class UoMUpdateDtoValidator : AbstractValidator<UoMUpdateDto>
    {
        public UoMUpdateDtoValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Mã đơn vị không được để trống.").MaximumLength(50);
            RuleFor(x => x.Name).NotEmpty().WithMessage("Tên đơn vị không được để trống.").MaximumLength(100);
            RuleFor(x => x.Synonyms).MaximumLength(200);
            RuleFor(x => x.CategoryId).NotNull().WithMessage("Phải chọn Nhóm đơn vị đo lường.");
        }
    }


}