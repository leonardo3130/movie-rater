using MovieRaterApi.Features.Users.DTOs;

namespace MovieRaterApi.Features.Users.Interfaces;

public interface IUserSuggestionService
{
    Task<ICollection<UserSuggestionDto>> SuggestAsync(Guid currentUserId, UserSuggestionQuery query);
}