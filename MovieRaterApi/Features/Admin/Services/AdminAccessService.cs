using Microsoft.Extensions.Options;
using MovieRaterApi.Features.Admin.Interfaces;
using MovieRaterApi.Features.Admin.Options;

namespace MovieRaterApi.Features.Admin.Services;

public class AdminAccessService : IAdminAccessService
{
    private readonly IOptionsMonitor<AdminOptions> _options;

    public AdminAccessService(IOptionsMonitor<AdminOptions> options)
    {
        _options = options;
    }

    public bool IsAdmin(Guid userId)
    {
        if (userId == Guid.Empty)
            return false;

        return _options.CurrentValue.UserIds.Any(id =>
            Guid.TryParse(id, out var adminId) && adminId == userId
        );
    }
}
