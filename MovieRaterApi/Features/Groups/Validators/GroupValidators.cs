using FluentValidation;
using MovieRaterApi.Features.Groups.DTOs;

namespace MovieRaterApi.Features.Groups.Validators;

public class InvitePartnerRequestValidator : AbstractValidator<InvitationRequestDto>
{
    public InvitePartnerRequestValidator()
    {
        RuleFor(x => x.InviteeEmail).NotEmpty().EmailAddress().MaximumLength(255);
    }
}

public class AcceptInvitationRequestValidator : AbstractValidator<AcceptInvitationRequestDto>
{
    public AcceptInvitationRequestValidator()
    {
        RuleFor(x => x.InviteToken).NotEmpty();
    }
}
