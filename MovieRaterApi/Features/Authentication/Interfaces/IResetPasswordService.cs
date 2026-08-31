using MovieRaterApi.Features.Authentication.DTOs;

namespace MovieRaterApi.Features.Authentication.Interfaces;

public interface IPasswordResetService
{
    Task SendResetPasswordEmail(string email);
    Task ResetPassword(ResetPasswordRequest request);
}
