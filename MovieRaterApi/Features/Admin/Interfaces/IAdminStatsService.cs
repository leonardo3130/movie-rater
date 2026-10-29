using MovieRaterApi.Features.Admin.DTOs;

namespace MovieRaterApi.Features.Admin.Interfaces;

public interface IAdminStatsService
{
    Task<AdminOverviewResponseDto> GetOverviewAsync(Guid adminUserId, AdminOverviewQueryDto query);
    Task<AdminActivityResponseDto> GetActivityAsync(Guid adminUserId, AdminActivityQueryDto query);
    Task<List<AdminGroupDto>> GetGroupsAsync(Guid adminUserId);
    Task<AdminInvitationListResponseDto> GetInvitationsAsync(
        Guid adminUserId,
        AdminInvitationQueryDto query
    );
}
