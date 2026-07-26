using FluentValidation.TestHelper;
using MovieRaterApi.Features.Authentication.DTOs;
using MovieRaterApi.Features.Authentication.Validators;

namespace MovieRaterApi.Tests.Unit.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenUsernameIsEmpty()
    {
        var result = _sut.TestValidate(
            new RegisterRequestDto
            {
                Username = "",
                Email = "test@example.com",
                Password = "ValidPass1!",
            }
        );

        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void ShouldHaveError_WhenEmailIsEmpty()
    {
        var result = _sut.TestValidate(
            new RegisterRequestDto
            {
                Username = "testuser",
                Email = "",
                Password = "ValidPass1!",
            }
        );

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ShouldHaveError_WhenEmailIsInvalid()
    {
        var result = _sut.TestValidate(
            new RegisterRequestDto
            {
                Username = "testuser",
                Email = "not-an-email",
                Password = "ValidPass1!",
            }
        );

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ShouldHaveError_WhenPasswordIsTooShort()
    {
        var result = _sut.TestValidate(
            new RegisterRequestDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Ab1!",
            }
        );

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ShouldHaveError_WhenPasswordIsEmpty()
    {
        var result = _sut.TestValidate(
            new RegisterRequestDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "",
            }
        );

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ShouldNotHaveError_WhenValid()
    {
        var result = _sut.TestValidate(
            new RegisterRequestDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "ValidPass1!",
            }
        );

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenEmailIsEmpty()
    {
        var result = _sut.TestValidate(new LoginRequestDto { Email = "", Password = "password" });

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ShouldHaveError_WhenEmailIsInvalid()
    {
        var result = _sut.TestValidate(
            new LoginRequestDto { Email = "invalid", Password = "password" }
        );

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ShouldHaveError_WhenPasswordIsEmpty()
    {
        var result = _sut.TestValidate(
            new LoginRequestDto { Email = "test@example.com", Password = "" }
        );

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void ShouldNotHaveError_WhenValid()
    {
        var result = _sut.TestValidate(
            new LoginRequestDto { Email = "test@example.com", Password = "mypassword" }
        );

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class InvitePartnerRequestValidatorTests
{
    private readonly InvitePartnerRequestValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenEmailIsEmpty()
    {
        var result = _sut.TestValidate(new InvitePartnerRequestDto { InviteeEmail = "" });

        result.ShouldHaveValidationErrorFor(x => x.InviteeEmail);
    }

    [Fact]
    public void ShouldHaveError_WhenEmailIsInvalid()
    {
        var result = _sut.TestValidate(
            new InvitePartnerRequestDto { InviteeEmail = "not-an-email" }
        );

        result.ShouldHaveValidationErrorFor(x => x.InviteeEmail);
    }

    [Fact]
    public void ShouldNotHaveError_WhenValid()
    {
        var result = _sut.TestValidate(
            new InvitePartnerRequestDto { InviteeEmail = "partner@example.com" }
        );

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class AcceptInvitationRequestValidatorTests
{
    private readonly AcceptInvitationRequestValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenTokenIsEmpty()
    {
        var result = _sut.TestValidate(new AcceptInvitationRequestDto { InviteToken = "" });

        result.ShouldHaveValidationErrorFor(x => x.InviteToken);
    }

    [Fact]
    public void ShouldNotHaveError_WhenTokenIsProvided()
    {
        var result = _sut.TestValidate(
            new AcceptInvitationRequestDto { InviteToken = "some-valid-token" }
        );

        result.ShouldNotHaveAnyValidationErrors();
    }
}
