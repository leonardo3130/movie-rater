using FluentValidation;
using MovieRaterApi.Features.Admin.DTOs;

namespace MovieRaterApi.Features.Admin.Validators;

public class AdminActivityQueryValidator : AbstractValidator<AdminActivityQueryDto>
{
    public AdminActivityQueryValidator()
    {
        RuleFor(x => x.Days).InclusiveBetween(7, 365);
    }
}

public class AdminInvitationQueryValidator : AbstractValidator<AdminInvitationQueryDto>
{
    public AdminInvitationQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Search).MaximumLength(255);
        RuleFor(x => x.Limit).InclusiveBetween(1, 500);
    }
}
