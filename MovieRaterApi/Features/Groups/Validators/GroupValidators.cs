using FluentValidation;
using MovieRaterApi.Features.Groups.DTOs;

namespace MovieRaterApi.Features.Groups.Validators;

public class InvitePartnerRequestValidator : AbstractValidator<InvitationRequestDto>
{
    public InvitePartnerRequestValidator()
    {
        RuleFor(x => x.InviteeUsername).NotEmpty().MaximumLength(100);
    }
}

public class AcceptInvitationRequestValidator : AbstractValidator<AcceptInvitationRequestDto>
{
    public AcceptInvitationRequestValidator()
    {
        RuleFor(x => x.InviteToken).NotEmpty();
    }
}

public class CreateGroupRequestValidator : AbstractValidator<CreateGroupRequest>
{
    public CreateGroupRequestValidator()
    {
        RuleFor(x => x.GroupName).NotEmpty().MinimumLength(4).MaximumLength(100);
    }
}
