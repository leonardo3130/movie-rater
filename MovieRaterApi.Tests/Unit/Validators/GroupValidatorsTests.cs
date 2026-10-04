using FluentValidation.TestHelper;
using MovieRaterApi.Features.Groups.DTOs;
using MovieRaterApi.Features.Groups.Validators;

namespace MovieRaterApi.Tests.Unit.Validators;

public class InvitePartnerRequestValidatorTests
{
    private readonly InvitePartnerRequestValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenUsernameIsEmpty()
    {
        var result = _sut.TestValidate(new InvitationRequestDto { InviteeUsername = "" });

        result.ShouldHaveValidationErrorFor(x => x.InviteeUsername);
    }

    [Fact]
    public void ShouldHaveError_WhenUsernameIsTooLong()
    {
        var result = _sut.TestValidate(
            new InvitationRequestDto
            {
                InviteeUsername = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            }
        );

        result.ShouldHaveValidationErrorFor(x => x.InviteeUsername);
    }

    [Fact]
    public void ShouldNotHaveError_WhenValid()
    {
        var result = _sut.TestValidate(
            new InvitationRequestDto { InviteeUsername = "partner", GroupId = Guid.NewGuid() }
        );

        result.ShouldNotHaveAnyValidationErrors();
    }
}