using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using MovieRaterApi.Infrastructure.Email.Options;

namespace MovieRaterApi.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private readonly EmailConfiguration _emailOptions;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailConfiguration> emailOptions, ILogger<SmtpEmailSender> logger)
    {
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toAddress, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(_emailOptions.FromAddress),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toAddress);

        using var client = new SmtpClient(_emailOptions.SmtpServer, _emailOptions.Port)
        {
            Credentials = new NetworkCredential(_emailOptions.Username, _emailOptions.Password),
            EnableSsl = _emailOptions.EnableSsl,
        };

        await client.SendMailAsync(message, cancellationToken);

        _logger.LogInformation("Sent email '{Subject}' to {ToAddress}", subject, toAddress);
    }
}