using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using MovieRaterApi.Infrastructure.Email.Options;
using MovieRaterApi.Infrastructure.Exceptions;

namespace MovieRaterApi.Infrastructure.Email;

public class SmtpEmailSender : IEmailSender
{
    private const int SendTimeoutMs = 30_000;

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
            From = new MailAddress(_emailOptions.FromAddress.Trim()),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toAddress);

        using var client = new SmtpClient(_emailOptions.SmtpServer.Trim(), _emailOptions.Port)
        {
            Credentials = new NetworkCredential(
                _emailOptions.Username.Trim(),
                _emailOptions.Password.Trim()
            ),
            EnableSsl = _emailOptions.EnableSsl,
            UseDefaultCredentials = false,
            Timeout = SendTimeoutMs,
        };

        try
        {
            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Sent email '{Subject}' to {ToAddress}", subject, toAddress);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                ex,
                "Failed to send email '{Subject}' to {ToAddress} via SMTP host {SmtpServer}:{Port}",
                subject,
                toAddress,
                _emailOptions.SmtpServer,
                _emailOptions.Port
            );

            throw new EmailDeliveryException(
                "The email could not be delivered. Please try again later.",
                ex
            );
        }
    }
}
