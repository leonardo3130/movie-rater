using MovieRaterApi.Features.WatchSessions.DTOs;

namespace MovieRaterApi.Features.WatchSessions.Interfaces;

public interface IWatchSessionService
{
    Task<WatchSessionResponseDto> CreateAsync(
        CreateWatchSessionRequestDto request,
        Guid userId,
        Guid? groupId
    );
    Task<WatchSessionListResponseDto> GetAllAsync(WatchSessionQueryDto query);
    Task<WatchSessionResponseDto> GetByIdAsync(Guid id);
    Task DeleteAsync(Guid id, Guid userId);
    Task<HeatmapResponseDto> GetHeatmapAsync(int days, Guid userId, Guid? groupId);
}
