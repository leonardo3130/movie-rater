using FluentValidation.TestHelper;
using MovieRaterApi.Features.Users.DTOs;
using MovieRaterApi.Features.Users.Validators;

namespace MovieRaterApi.Tests.Unit.Validators;

public class UserSuggestionQueryValidatorTests
{
    private readonly UserSuggestionQueryValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenPrefixIsTooLong()
    {
        var result = _sut.TestValidate(
            new UserSuggestionQuery { Prefix = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" }
        );

        result.ShouldHaveValidationErrorFor(x => x.Prefix);
    }

    [Fact]
    public void ShouldNotHaveError_WhenPrefixIsNull()
    {
        var result = _sut.TestValidate(new UserSuggestionQuery { Prefix = null });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldHaveError_WhenLimitIsBelowMinimum()
    {
        var result = _sut.TestValidate(new UserSuggestionQuery { Limit = 0 });

        result.ShouldHaveValidationErrorFor(x => x.Limit);
    }

    [Fact]
    public void ShouldHaveError_WhenLimitExceedsMaximum()
    {
        var result = _sut.TestValidate(new UserSuggestionQuery { Limit = 21 });

        result.ShouldHaveValidationErrorFor(x => x.Limit);
    }

    [Fact]
    public void ShouldNotHaveError_WhenValid()
    {
        var result = _sut.TestValidate(new UserSuggestionQuery { Prefix = "al", Limit = 8 });

        result.ShouldNotHaveAnyValidationErrors();
    }
}