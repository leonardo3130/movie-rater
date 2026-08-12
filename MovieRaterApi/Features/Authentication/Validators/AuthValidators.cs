using FluentValidation;
using MovieRaterApi.Features.Authentication.DTOs;

namespace MovieRaterApi.Features.Authentication.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);

        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(255);

        RuleFor(x => x.Password).NotEmpty();
    }
}

public class RefreshRequestValidator : AbstractValidator<RefreshRequestDto>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
