using MovieRaterApi.Features.WatchSessions.DTOs;

namespace MovieRaterApi.Features.WatchSessions.Interfaces;

public interface IWatchSessionService
{
    Task<WatchSessionResponseDto> CreateAsync(
        CreateWatchSessionRequestDto request,
        Guid userId,
        Guid coupleId
    );
    Task<WatchSessionListResponseDto> GetAllAsync(WatchSessionQueryDto query, Guid coupleId);
    Task<WatchSessionResponseDto> GetByIdAsync(Guid id, Guid coupleId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<HeatmapResponseDto> GetHeatmapAsync(int days, Guid coupleId);
}
