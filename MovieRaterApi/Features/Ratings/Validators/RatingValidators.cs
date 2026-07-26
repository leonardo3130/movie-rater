using FluentValidation;
using MovieRaterApi.Features.Ratings.DTOs;

namespace MovieRaterApi.Features.Ratings.Validators;

public class CreateRatingRequestValidator : AbstractValidator<CreateRatingRequestDto>
{
    public CreateRatingRequestValidator()
    {
        RuleFor(x => x.RatingValue).InclusiveBetween(1, 10);
        RuleFor(x => x.Review).MaximumLength(5000).When(x => x.Review is not null);
    }
}

public class UpdateRatingRequestValidator : AbstractValidator<UpdateRatingRequestDto>
{
    public UpdateRatingRequestValidator()
    {
        RuleFor(x => x.RatingValue).InclusiveBetween(1, 10);
        RuleFor(x => x.Review).MaximumLength(5000).When(x => x.Review is not null);
    }
}
