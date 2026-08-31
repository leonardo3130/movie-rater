namespace MovieRaterApi.Infrastructure.Email.Options;

public class EmailConfiguration
{
    public const string SectionName = "EmailSettings";

    public string FromAddress { get; set; } = string.Empty;
    public string FrontendBaseUrl { get; set; } = string.Empty;
    public string PasswordResetPath { get; set; } = "reset-password";
    public string SmtpServer { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; }
}
