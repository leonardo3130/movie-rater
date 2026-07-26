using FluentValidation.TestHelper;
using MovieRaterApi.Features.Ratings.DTOs;
using MovieRaterApi.Features.Ratings.Validators;

namespace MovieRaterApi.Tests.Unit.Validators;

public class CreateRatingRequestValidatorTests
{
    private readonly CreateRatingRequestValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenRatingValueIsBelow1()
    {
        var result = _sut.TestValidate(
            new CreateRatingRequestDto { RatingValue = 0, Review = "ok" }
        );

        result.ShouldHaveValidationErrorFor(x => x.RatingValue);
    }

    [Fact]
    public void ShouldHaveError_WhenRatingValueIsAbove10()
    {
        var result = _sut.TestValidate(
            new CreateRatingRequestDto { RatingValue = 11, Review = "ok" }
        );

        result.ShouldHaveValidationErrorFor(x => x.RatingValue);
    }

    [Fact]
    public void ShouldHaveError_WhenReviewExceedsMaxLength()
    {
        var result = _sut.TestValidate(
            new CreateRatingRequestDto { RatingValue = 5, Review = new string('x', 5001) }
        );

        result.ShouldHaveValidationErrorFor(x => x.Review);
    }

    [Fact]
    public void ShouldNotHaveError_WhenValid()
    {
        var result = _sut.TestValidate(
            new CreateRatingRequestDto { RatingValue = 8, Review = "Great movie!" }
        );

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldNotHaveError_WhenReviewIsNull()
    {
        var result = _sut.TestValidate(
            new CreateRatingRequestDto { RatingValue = 5, Review = null }
        );

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class UpdateRatingRequestValidatorTests
{
    private readonly UpdateRatingRequestValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenRatingValueIsBelow1()
    {
        var result = _sut.TestValidate(new UpdateRatingRequestDto { RatingValue = 0 });

        result.ShouldHaveValidationErrorFor(x => x.RatingValue);
    }

    [Fact]
    public void ShouldHaveError_WhenRatingValueIsAbove10()
    {
        var result = _sut.TestValidate(new UpdateRatingRequestDto { RatingValue = 11 });

        result.ShouldHaveValidationErrorFor(x => x.RatingValue);
    }

    [Fact]
    public void ShouldNotHaveError_WhenValid()
    {
        var result = _sut.TestValidate(
            new UpdateRatingRequestDto { RatingValue = 7, Review = "Updated review" }
        );

        result.ShouldNotHaveAnyValidationErrors();
    }
}
