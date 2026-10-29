using FluentValidation.TestHelper;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Admin.DTOs;
using MovieRaterApi.Features.Admin.Validators;

namespace MovieRaterApi.Tests.Unit.Validators;

public class AdminActivityQueryValidatorTests
{
    private readonly AdminActivityQueryValidator _sut = new();

    [Theory]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(365)]
    public void ShouldNotHaveError_WhenDaysInRange(int days)
    {
        var result = _sut.TestValidate(new AdminActivityQueryDto { Days = days });

        result.ShouldNotHaveValidationErrorFor(x => x.Days);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(366)]
    public void ShouldHaveError_WhenDaysOutOfRange(int days)
    {
        var result = _sut.TestValidate(new AdminActivityQueryDto { Days = days });

        result.ShouldHaveValidationErrorFor(x => x.Days);
    }
}

public class AdminInvitationQueryValidatorTests
{
    private readonly AdminInvitationQueryValidator _sut = new();

    [Fact]
    public void ShouldNotHaveErrors_ForDefaults()
    {
        var result = _sut.TestValidate(new AdminInvitationQueryDto());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldHaveError_WhenStatusIsNotDefined()
    {
        var result = _sut.TestValidate(
            new AdminInvitationQueryDto { Status = (InvitationStatus)42 }
        );

        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void ShouldHaveError_WhenSearchIsTooLong()
    {
        var result = _sut.TestValidate(
            new AdminInvitationQueryDto { Search = new string('a', 256) }
        );

        result.ShouldHaveValidationErrorFor(x => x.Search);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public void ShouldHaveError_WhenLimitOutOfRange(int limit)
    {
        var result = _sut.TestValidate(new AdminInvitationQueryDto { Limit = limit });

        result.ShouldHaveValidationErrorFor(x => x.Limit);
    }
}
