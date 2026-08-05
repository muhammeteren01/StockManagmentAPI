using Core.DTOs.Auth;
using FluentValidation;

namespace Core.Validations.Auth;

/// <summary>RegisterRequest doğrulama kuralları.</summary>
public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Ad zorunludur.")
            .MaximumLength(100).WithMessage("Ad en fazla 100 karakter olabilir.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Soyad zorunludur.")
            .MaximumLength(100).WithMessage("Soyad en fazla 100 karakter olabilir.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta giriniz.")
            .MaximumLength(255).WithMessage("E-posta en fazla 255 karakter olabilir.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Geçersiz kullanıcı rolü.");

        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("SuperAdmin dışındaki kullanıcılar için şirket zorunludur.")
            .When(x => x.Role != Enums.UserRole.SuperAdmin);
    }
}
