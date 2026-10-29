namespace MovieRaterApi.Features.Admin.Interfaces;

public interface IAdminAccessService
{
    bool IsAdmin(Guid userId);
}
