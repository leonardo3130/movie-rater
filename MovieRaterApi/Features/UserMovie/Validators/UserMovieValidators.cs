using FluentValidation;
using MovieRaterApi.Features.UserMovie.DTOs;

namespace MovieRaterApi.Features.UserMovie.Validators;

public class UserMovieListRequestValidator : AbstractValidator<UserMovieListRequestDto>
{
    public UserMovieListRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100);
    }
}