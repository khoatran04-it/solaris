using FluentValidation;
using backend.DTOs.ProductCategoryGroupDTOs;

namespace backend.DTOs.Validator
{
    public class ProductCategoryGroupCreateDtoValidator : AbstractValidator<ProductCategoryGroupCreateDto>
    {
        public ProductCategoryGroupCreateDtoValidator() 
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

            RuleFor(x => x.ImagePath)
                .MaximumLength(1000).WithMessage("Link không được vượt quá 1000 ký tự.")
                .When(x => !string.IsNullOrEmpty(x.ImagePath));
        }
    }

    public class ProductCategoryGroupUpdateDtoValidator : AbstractValidator<ProductCategoryGroupUpdateDto>
    {
        public ProductCategoryGroupUpdateDtoValidator()
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

            RuleFor(x => x.ImagePath)
                .MaximumLength(1000).WithMessage("Link không được vượt quá 1000 ký tự.")
                .When(x => !string.IsNullOrEmpty(x.ImagePath));
        }
    }
}
