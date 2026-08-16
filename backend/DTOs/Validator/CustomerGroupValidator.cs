using FluentValidation;
using backend.DTOs.CustomerGroupDTOs;

namespace backend.DTOs.Validators
{
    public class CustomerGroupCreateDtoValidator : AbstractValidator<CustomerGroupCreateDto>
    {
        public CustomerGroupCreateDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã nhóm khách hàng không được để trống.")
                .MaximumLength(20).WithMessage("Mã không được vượt quá 20 ký tự.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên nhóm khách hàng không được để trống.")
                .MaximumLength(200).WithMessage("Tên không được vượt quá 200 ký tự.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự.")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }

    public class CustomerGroupUpdateDtoValidator : AbstractValidator<CustomerGroupUpdateDto>
    {
        public CustomerGroupUpdateDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã nhóm khách hàng không được để trống.")
                .MaximumLength(20).WithMessage("Mã không được vượt quá 20 ký tự.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên nhóm khách hàng không được để trống.")
                .MaximumLength(200).WithMessage("Tên không được vượt quá 200 ký tự.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự.")
                .When(x => !string.IsNullOrEmpty(x.Description));
        }
    }
}