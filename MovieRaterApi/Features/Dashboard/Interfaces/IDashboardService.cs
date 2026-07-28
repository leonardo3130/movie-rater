using MovieRaterApi.Features.Dashboard.DTOs;

namespace MovieRaterApi.Features.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<DashboardResponseDto> GetDashboardAsync(Guid coupleId);
}
