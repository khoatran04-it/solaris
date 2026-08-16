using FluentValidation;
using backend.DTOs.CustomerTypeDTOs;

namespace backend.DTOs.Validators
{
    public class CustomerTypeCreateDtoValidator : AbstractValidator<CustomerTypeCreateDto>
    {
        public CustomerTypeCreateDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã phân loại không được để trống.")
                .MaximumLength(20).WithMessage("Mã không được vượt quá 20 ký tự.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên phân loại không được để trống.")
                .MaximumLength(200).WithMessage("Tên không được vượt quá 200 ký tự.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự.")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }

    public class CustomerTypeUpdateDtoValidator : AbstractValidator<CustomerTypeUpdateDto>
    {
        public CustomerTypeUpdateDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã phân loại không được để trống.")
                .MaximumLength(20).WithMessage("Mã không được vượt quá 20 ký tự.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên phân loại không được để trống.")
                .MaximumLength(200).WithMessage("Tên không được vượt quá 200 ký tự.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự.")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}