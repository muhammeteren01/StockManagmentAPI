using FluentValidation.Results;
using FluentValidation;
using ValidationException = Core.Validations.ValidationException;

namespace Service.Services;

/// <summary>FluentValidation sonuçlarını ValidationException'a çevirir.</summary>
internal static class ValidationHelper
{
    public static async Task EnsureValidAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
    }
}
