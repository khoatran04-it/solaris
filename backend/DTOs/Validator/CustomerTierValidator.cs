using FluentValidation;
using backend.DTOs.CustomerTierDTOs;

namespace backend.DTOs.Validators
{
    public class CustomerTierCreateDtoValidator : AbstractValidator<CustomerTierCreateDto>
    {
        public CustomerTierCreateDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã bậc hạng không được để trống.")
                .MaximumLength(20).WithMessage("Mã không được vượt quá 20 ký tự.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên bậc hạng không được để trống.")
                .MaximumLength(100).WithMessage("Tên không được vượt quá 100 ký tự.");

            RuleFor(x => x.DiscountPercent)
                .InclusiveBetween(0, 100).WithMessage("Phần trăm chiết khấu phải từ 0 đến 100.");

            RuleFor(x => x.MinSpending)
                .GreaterThanOrEqualTo(0).WithMessage("Mức chi tiêu tối thiểu không được âm.");
        }
    }

    public class CustomerTierUpdateDtoValidator : AbstractValidator<CustomerTierUpdateDto>
    {
        public CustomerTierUpdateDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã bậc hạng không được để trống.")
                .MaximumLength(20).WithMessage("Mã không được vượt quá 20 ký tự.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên bậc hạng không được để trống.")
                .MaximumLength(100).WithMessage("Tên không được vượt quá 100 ký tự.");

            RuleFor(x => x.DiscountPercent)
                .InclusiveBetween(0, 100).WithMessage("Phần trăm chiết khấu phải từ 0 đến 100.");

            RuleFor(x => x.MinSpending)
                .GreaterThanOrEqualTo(0).WithMessage("Mức chi tiêu tối thiểu không được âm.");
        }
    }
}