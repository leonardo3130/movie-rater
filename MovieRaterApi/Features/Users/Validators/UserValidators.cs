using FluentValidation;
using MovieRaterApi.Features.Users.DTOs;

namespace MovieRaterApi.Features.Users.Validators;

public class UserSuggestionQueryValidator : AbstractValidator<UserSuggestionQuery>
{
    public UserSuggestionQueryValidator()
    {
        RuleFor(x => x.Prefix).MaximumLength(100);
        RuleFor(x => x.Limit).InclusiveBetween(1, 20);
    }
}