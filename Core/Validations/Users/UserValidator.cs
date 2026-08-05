using Core.Entities;
using FluentValidation;

namespace Core.Validations.Users;

/// <summary>User entity doğrulama kuralları.</summary>
public class UserValidator : AbstractValidator<User>
{
    public UserValidator()
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

        RuleFor(x => x.PasswordHash)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MaximumLength(255).WithMessage("Şifre hash en fazla 255 karakter olabilir.");

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage("Geçersiz kullanıcı rolü.");
    }
}
