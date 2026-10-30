using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using MovieRaterApi.Features.Admin.Options;
using MovieRaterApi.Features.Admin.Services;

namespace MovieRaterApi.Tests.Unit.Services;

public class AdminAccessServiceTests
{
    private static AdminAccessService CreateSut(params string[] userIds)
    {
        var optionsMock = new Mock<IOptionsMonitor<AdminOptions>>();
        optionsMock
            .Setup(o => o.CurrentValue)
            .Returns(new AdminOptions { UserIds = userIds.ToList() });
        return new AdminAccessService(optionsMock.Object);
    }

    [Fact]
    public void IsAdmin_ReturnsTrue_WhenUserIdIsConfigured()
    {
        var adminId = Guid.NewGuid();
        var sut = CreateSut(adminId.ToString());

        sut.IsAdmin(adminId).Should().BeTrue();
    }

    [Fact]
    public void IsAdmin_IsCaseInsensitive()
    {
        var adminId = Guid.NewGuid();
        var sut = CreateSut(adminId.ToString().ToUpperInvariant());

        sut.IsAdmin(adminId).Should().BeTrue();
    }

    [Fact]
    public void IsAdmin_ReturnsFalse_WhenUserIdIsNotConfigured()
    {
        var sut = CreateSut(Guid.NewGuid().ToString());

        sut.IsAdmin(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void IsAdmin_IgnoresEmptyAndInvalidEntries()
    {
        var adminId = Guid.NewGuid();
        var sut = CreateSut("", "not-a-guid", adminId.ToString());

        sut.IsAdmin(adminId).Should().BeTrue();
        sut.IsAdmin(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void IsAdmin_ReturnsFalse_ForEmptyGuid()
    {
        var sut = CreateSut(Guid.Empty.ToString());

        sut.IsAdmin(Guid.Empty).Should().BeFalse();
    }
}
