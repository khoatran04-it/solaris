using FluentValidation;
using backend.DTOs.CustomerAddressDTOs;

namespace backend.DTOs.Validators
{
    public class CustomerAddressCreateDtoValidator : AbstractValidator<CustomerAddressCreateDto>
    {
        public CustomerAddressCreateDtoValidator()
        {
            RuleFor(x => x.ReceiverName)
                .NotEmpty().WithMessage("Tên người nhận không được để trống.")
                .MaximumLength(100).WithMessage("Tên người nhận không vượt quá 100 ký tự.");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Số điện thoại không được để trống.")
                .MaximumLength(20).WithMessage("Số điện thoại không vượt quá 20 ký tự.")
                .Matches(@"^(0|84)(2(0[3-9]|1[0-6|8|9]|2[0-2|5-9]|3[2-9]|4[0-9]|5[1|2|4-9]|6[9]|7[0-7]|8[0-9]|9[0-4|6-9])|3[2-9]|5[5|6|8|9]|7[0|6-9]|8[0-6|8|9]|9[0-4|6-9])([0-9]{7})$")
                .WithMessage("Số điện thoại không đúng định dạng.");

            RuleFor(x => x.Province).NotEmpty().WithMessage("Tỉnh/Thành phố không được để trống.").MaximumLength(100);
            RuleFor(x => x.District).NotEmpty().WithMessage("Quận/Huyện không được để trống.").MaximumLength(100);
            RuleFor(x => x.Ward).NotEmpty().WithMessage("Phường/Xã không được để trống.").MaximumLength(100);
            RuleFor(x => x.StreetAddress).NotEmpty().WithMessage("Địa chỉ cụ thể không được để trống.").MaximumLength(200);
        }
    }

    public class CustomerAddressUpdateDtoValidator : AbstractValidator<CustomerAddressUpdateDto>
    {
        public CustomerAddressUpdateDtoValidator()
        {
            RuleFor(x => x.ReceiverName).NotEmpty().WithMessage("Tên người nhận không được để trống.").MaximumLength(100);
            RuleFor(x => x.Phone).NotEmpty().WithMessage("Số điện thoại không được để trống.").MaximumLength(20).Matches(@"^(0|84)(2(0[3-9]|1[0-6|8|9]|2[0-2|5-9]|3[2-9]|4[0-9]|5[1|2|4-9]|6[9]|7[0-7]|8[0-9]|9[0-4|6-9])|3[2-9]|5[5|6|8|9]|7[0|6-9]|8[0-6|8|9]|9[0-4|6-9])([0-9]{7})$").WithMessage("Số điện thoại không đúng định dạng.");
            RuleFor(x => x.Province).NotEmpty().WithMessage("Tỉnh/Thành phố không được để trống.").MaximumLength(100);
            RuleFor(x => x.District).NotEmpty().WithMessage("Quận/Huyện không được để trống.").MaximumLength(100);
            RuleFor(x => x.Ward).NotEmpty().WithMessage("Phường/Xã không được để trống.").MaximumLength(100);
            RuleFor(x => x.StreetAddress).NotEmpty().WithMessage("Địa chỉ cụ thể không được để trống.").MaximumLength(200);
        }
    }
}