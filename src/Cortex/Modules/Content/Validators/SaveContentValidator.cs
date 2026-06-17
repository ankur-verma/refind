using Cortex.Modules.Content.DTOs;
using FluentValidation;

namespace Cortex.Modules.Content.Validators;

public class SaveContentValidator : AbstractValidator<SaveContentRequest>
{
    public SaveContentValidator()
    {
        RuleFor(x => x.Url)
            .NotEmpty().WithMessage("URL is required.")
            .Must(BeAValidUrl).WithMessage("Invalid URL format.");
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}
