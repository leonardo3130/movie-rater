using FluentValidation.TestHelper;
using MovieRaterApi.Features.WatchSessions.DTOs;
using MovieRaterApi.Features.WatchSessions.Validators;

namespace MovieRaterApi.Tests.Unit.Validators;

public class CreateWatchSessionRequestValidatorTests
{
    private readonly CreateWatchSessionRequestValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenMovieIdIsEmpty()
    {
        var result = _sut.TestValidate(
            new CreateWatchSessionRequestDto { MovieId = Guid.Empty, WatchedAt = DateTime.UtcNow }
        );

        result.ShouldHaveValidationErrorFor(x => x.MovieId);
    }

    [Fact]
    public void ShouldHaveError_WhenWatchedAtIsDefault()
    {
        var result = _sut.TestValidate(
            new CreateWatchSessionRequestDto { MovieId = Guid.NewGuid(), WatchedAt = default }
        );

        result.ShouldHaveValidationErrorFor(x => x.WatchedAt);
    }

    [Fact]
    public void ShouldHaveError_WhenLocationExceedsMaxLength()
    {
        var result = _sut.TestValidate(
            new CreateWatchSessionRequestDto
            {
                MovieId = Guid.NewGuid(),
                WatchedAt = DateTime.UtcNow,
                Location = new string('x', 201),
            }
        );

        result.ShouldHaveValidationErrorFor(x => x.Location);
    }

    [Fact]
    public void ShouldHaveError_WhenNotesExceedsMaxLength()
    {
        var result = _sut.TestValidate(
            new CreateWatchSessionRequestDto
            {
                MovieId = Guid.NewGuid(),
                WatchedAt = DateTime.UtcNow,
                Notes = new string('x', 2001),
            }
        );

        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void ShouldNotHaveError_WhenValid()
    {
        var result = _sut.TestValidate(
            new CreateWatchSessionRequestDto
            {
                MovieId = Guid.NewGuid(),
                WatchedAt = DateTime.UtcNow,
                Location = "Home",
                Notes = "Great movie!",
            }
        );

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class HeatmapQueryValidatorTests
{
    private readonly HeatmapQueryValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenDaysIsZero()
    {
        var result = _sut.TestValidate(new HeatmapQueryDto { Days = 0 });

        result.ShouldHaveValidationErrorFor(x => x.Days);
    }

    [Fact]
    public void ShouldHaveError_WhenDaysExceedsMax()
    {
        var result = _sut.TestValidate(new HeatmapQueryDto { Days = 731 });

        result.ShouldHaveValidationErrorFor(x => x.Days);
    }

    [Fact]
    public void ShouldNotHaveError_WhenDaysIsDefault()
    {
        var result = _sut.TestValidate(new HeatmapQueryDto { Days = 365 });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldNotHaveError_WhenDaysIsValid()
    {
        var result = _sut.TestValidate(new HeatmapQueryDto { Days = 30 });

        result.ShouldNotHaveAnyValidationErrors();
    }
}
