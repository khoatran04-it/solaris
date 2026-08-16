using FluentValidation;
using backend.DTOs.CustomerDTOs;

namespace backend.DTOs.Validators
{
    public class CustomerCreateDtoValidator : AbstractValidator<CustomerCreateDto>
    {
        public CustomerCreateDtoValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Mã khách hàng không được để trống.")
                .MaximumLength(20).WithMessage("Mã không được vượt quá 20 ký tự.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên khách hàng không được để trống.")
                .MaximumLength(200).WithMessage("Tên không được vượt quá 200 ký tự.");

            RuleFor(x => x.PhoneNumber)
                .NotEmpty().WithMessage("Số điện thoại không được để trống.")
                .MaximumLength(20).WithMessage("Số điện thoại không được vượt quá 20 ký tự.")
                .Matches(@"^(0|84)(2(0[3-9]|1[0-6|8|9]|2[0-2|5-9]|3[2-9]|4[0-9]|5[1|2|4-9]|6[9]|7[0-7]|8[0-9]|9[0-4|6-9])|3[2-9]|5[5|6|8|9]|7[0|6-9]|8[0-6|8|9]|9[0-4|6-9])([0-9]{7})$")
                .WithMessage("Số điện thoại không đúng định dạng.");

            // Email chỉ kiểm tra định dạng NẾU người dùng có nhập vào
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Email không đúng định dạng.")
                .MaximumLength(150).WithMessage("Email không được vượt quá 150 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.Email));

            RuleFor(x => x.TaxCode)
                .MaximumLength(20).WithMessage("Mã số thuế không được vượt quá 20 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.TaxCode));

            /* 
             * Nếu DTO của sếp có: public List<int> GroupIds { get; set; }
             * Sếp có thể bắt lỗi nếu cần:
             * RuleFor(x => x.GroupIds)
             *     .NotEmpty().WithMessage("Khách hàng phải thuộc ít nhất một nhóm.");
             */
        }
    }

    public class CustomerUpdateDtoValidator : AbstractValidator<CustomerUpdateDto>
    {
        public CustomerUpdateDtoValidator()
        {
            // Logic Update tương tự Create
            RuleFor(x => x.Code).NotEmpty().WithMessage("Mã khách hàng không được để trống.").MaximumLength(20);
            RuleFor(x => x.Name).NotEmpty().WithMessage("Tên khách hàng không được để trống.").MaximumLength(200);
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("Số điện thoại không được để trống.").MaximumLength(20).Matches(@"^(0|84)(2(0[3-9]|1[0-6|8|9]|2[0-2|5-9]|3[2-9]|4[0-9]|5[1|2|4-9]|6[9]|7[0-7]|8[0-9]|9[0-4|6-9])|3[2-9]|5[5|6|8|9]|7[0|6-9]|8[0-6|8|9]|9[0-4|6-9])([0-9]{7})$").WithMessage("Số điện thoại không đúng định dạng.");
            RuleFor(x => x.Email).EmailAddress().WithMessage("Email không đúng định dạng.").MaximumLength(150).When(x => !string.IsNullOrWhiteSpace(x.Email));
            RuleFor(x => x.TaxCode).MaximumLength(20).When(x => !string.IsNullOrWhiteSpace(x.TaxCode));
        }
    }
}