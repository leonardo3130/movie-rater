using FluentValidation.TestHelper;
using MovieRaterApi.Features.MovieLists.DTOs;
using MovieRaterApi.Features.MovieLists.Validators;

namespace MovieRaterApi.Tests.Unit.Validators;

public class CreateMovieListRequestValidatorTests
{
    private readonly CreateMovieListRequestValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenNameIsEmpty()
    {
        var result = _sut.TestValidate(new CreateMovieListRequestDto { Name = " " });

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var result = _sut.TestValidate(
            new CreateMovieListRequestDto { Name = new string('x', 101) }
        );

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldHaveError_WhenDescriptionExceedsMaxLength()
    {
        var result = _sut.TestValidate(
            new CreateMovieListRequestDto { Name = "List", Description = new string('x', 501) }
        );

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void ShouldHaveError_WhenSharedButNoGroupIds()
    {
        var result = _sut.TestValidate(
            new CreateMovieListRequestDto { Name = "List", IsPrivate = false }
        );

        result.ShouldHaveValidationErrorFor(x => x.GroupIds);
    }

    [Fact]
    public void ShouldNotHaveError_WhenPrivateListIsValid()
    {
        var result = _sut.TestValidate(
            new CreateMovieListRequestDto { Name = "My list", Description = "desc" }
        );

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldNotHaveError_WhenSharedListHasGroupIds()
    {
        var result = _sut.TestValidate(
            new CreateMovieListRequestDto
            {
                Name = "Shared list",
                IsPrivate = false,
                GroupIds = [Guid.NewGuid()],
            }
        );

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class UpdateMovieListRequestValidatorTests
{
    private readonly UpdateMovieListRequestValidator _sut = new();

    [Fact]
    public void ShouldHaveError_WhenNameIsEmpty()
    {
        var result = _sut.TestValidate(new UpdateMovieListRequestDto { Name = " " });

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldHaveError_WhenNameExceedsMaxLength()
    {
        var result = _sut.TestValidate(
            new UpdateMovieListRequestDto { Name = new string('x', 101) }
        );

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldHaveError_WhenDescriptionExceedsMaxLength()
    {
        var result = _sut.TestValidate(
            new UpdateMovieListRequestDto { Name = "List", Description = new string('x', 501) }
        );

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void ShouldHaveError_WhenSharedButNoGroupIds()
    {
        var result = _sut.TestValidate(
            new UpdateMovieListRequestDto { Name = "List", IsPrivate = false }
        );

        result.ShouldHaveValidationErrorFor(x => x.GroupIds);
    }

    [Fact]
    public void ShouldNotHaveError_WhenPrivateListIsValid()
    {
        var result = _sut.TestValidate(new UpdateMovieListRequestDto { Name = "Renamed" });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldNotHaveError_WhenSharedListHasGroupIds()
    {
        var result = _sut.TestValidate(
            new UpdateMovieListRequestDto
            {
                Name = "Shared list",
                IsPrivate = false,
                GroupIds = [Guid.NewGuid()],
            }
        );

        result.ShouldNotHaveAnyValidationErrors();
    }
}
