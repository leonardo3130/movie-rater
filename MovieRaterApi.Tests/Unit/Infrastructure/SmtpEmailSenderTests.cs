using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MovieRaterApi.Infrastructure.Email;
using MovieRaterApi.Infrastructure.Email.Options;
using MovieRaterApi.Infrastructure.Exceptions;

namespace MovieRaterApi.Tests.Unit.Infrastructure;

public class SmtpEmailSenderTests
{
    private static SmtpEmailSender CreateSut(EmailConfiguration options)
    {
        var logger = new Mock<ILogger<SmtpEmailSender>>();
        return new SmtpEmailSender(Options.Create(options), logger.Object);
    }

    [Fact]
    public async Task SendAsync_ShouldThrowEmailDeliveryException_WhenSmtpUnreachable()
    {
        var sut = CreateSut(
            new EmailConfiguration
            {
                FromAddress = "from@example.com",
                SmtpServer = "127.0.0.1",
                Port = 1,
                Username = "user",
                Password = "pass",
                EnableSsl = false,
            }
        );

        var act = () => sut.SendAsync("to@example.com", "Subject", "<p>Body</p>");

        await act.Should()
            .ThrowAsync<EmailDeliveryException>()
            .WithMessage("The email could not be delivered. Please try again later.");
    }
}
