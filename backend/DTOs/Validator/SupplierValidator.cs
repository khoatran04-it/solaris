using FluentValidation;
using backend.DTOs.SupplierDTOs;

namespace backend.DTOs.Validators
{
    public class SupplierCreateDtoValidator : AbstractValidator<SupplierCreateDto>
    {
        public SupplierCreateDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã nhà cung cấp không được để trống.")
                .MaximumLength(20).WithMessage("Mã không được vượt quá 20 ký tự.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên nhà cung cấp không được để trống.")
                .MaximumLength(200).WithMessage("Tên không được vượt quá 200 ký tự.");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Số điện thoại công ty không được để trống.")
                .MaximumLength(20).WithMessage("Số điện thoại không vượt quá 20 ký tự.")
                .Matches(@"^(0|84)(2(0[3-9]|1[0-6|8|9]|2[0-2|5-9]|3[2-9]|4[0-9]|5[1|2|4-9]|6[9]|7[0-7]|8[0-9]|9[0-4|6-9])|3[2-9]|5[5|6|8|9]|7[0|6-9]|8[0-6|8|9]|9[0-4|6-9])([0-9]{7})$")
                .WithMessage("Số điện thoại không đúng định dạng Việt Nam.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Email không đúng định dạng.")
                .MaximumLength(100).WithMessage("Email không được vượt quá 100 ký tự.");

            RuleFor(x => x.TaxCode)
                .MaximumLength(20).WithMessage("Mã số thuế không được vượt quá 20 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.TaxCode));

            RuleFor(x => x.BankAccount)
                .MaximumLength(50).WithMessage("Số tài khoản không được vượt quá 50 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.BankAccount));

            RuleFor(x => x.BankName)
                .MaximumLength(100).WithMessage("Tên ngân hàng không được vượt quá 100 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.BankName));
        }
    }

    public class SupplierUpdateDtoValidator : AbstractValidator<SupplierUpdateDto>
    {
        public SupplierUpdateDtoValidator()
        {
            RuleFor(x => x.Code).NotEmpty().WithMessage("Mã nhà cung cấp không được để trống.").MaximumLength(20);
            RuleFor(x => x.Name).NotEmpty().WithMessage("Tên nhà cung cấp không được để trống.").MaximumLength(200);
            RuleFor(x => x.Phone).NotEmpty().WithMessage("Số điện thoại không được để trống.").MaximumLength(20).Matches(@"^(0|84)(2(0[3-9]|1[0-6|8|9]|2[0-2|5-9]|3[2-9]|4[0-9]|5[1|2|4-9]|6[9]|7[0-7]|8[0-9]|9[0-4|6-9])|3[2-9]|5[5|6|8|9]|7[0|6-9]|8[0-6|8|9]|9[0-4|6-9])([0-9]{7})$").WithMessage("Số điện thoại không đúng định dạng.");
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email không được để trống.").EmailAddress().WithMessage("Email không đúng định dạng.").MaximumLength(100);
            RuleFor(x => x.TaxCode).MaximumLength(20).When(x => !string.IsNullOrWhiteSpace(x.TaxCode));
            RuleFor(x => x.BankAccount).MaximumLength(50).When(x => !string.IsNullOrWhiteSpace(x.BankAccount));
            RuleFor(x => x.BankName).MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.BankName));
        }
    }
}