using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MovieRaterApi.Data;
using MovieRaterApi.Data.Entities;
using MovieRaterApi.Features.Authentication.Infrastructure;
using MovieRaterApi.Features.Groups.DTOs;
using MovieRaterApi.Features.Groups.Services;
using MovieRaterApi.Infrastructure.Exceptions;

namespace MovieRaterApi.Tests.Unit.Services;

public class GroupServiceTests
{
    private readonly ApplicationDbContext _db;
    private readonly Mock<ILogger<GroupService>> _loggerMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly GroupService _sut;

    public GroupServiceTests()
    {
        _db = TestHelpers.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<GroupService>>();
        _currentUserMock = new Mock<ICurrentUser>();
        _sut = new GroupService(_db, _loggerMock.Object, _currentUserMock.Object);
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.UserId).Returns(Guid.NewGuid());
        _currentUserMock.Setup(u => u.Email).Returns("user@test.com");
        _currentUserMock.Setup(u => u.Username).Returns("user");
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToBase64String(bytes);
    }

    [Fact]
    public async Task CreateGroup_ShouldCreateGroup_WhenValid()
    {
        var userId = _currentUserMock.Object.UserId;

        var request = new CreateGroupRequest { GroupName = "group1" };

        var result = await _sut.CreateGroup(request);

        result.Name.Should().Be("group1");
        _db.UserGroups.Should().Contain(ug => ug.UserId == userId && ug.GroupId == result.Id);
    }

    [Fact]
    public async Task ChangeGroupName_ShouldChangeGroupName_WhenGroupFound()
    {
        var groupId = Guid.NewGuid();
        _db.Groups.Add(
            new Group
            {
                Id = groupId,
                Name = "group",
                CreatedAt = DateTime.UtcNow,
            }
        );
        _db.Users.Add(
            new User
            {
                Id = _currentUserMock.Object.UserId,
                Username = "user",
                Email = "user@test.com",
            }
        );
        _db.UserGroups.Add(
            new UserGroup
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = _currentUserMock.Object.UserId,
            }
        );
        _db.SaveChanges();

        var result = await _sut.ChangeGroupName(groupId, "newName");

        result.Name.Should().Be("newName");
    }

    [Fact]
    public async Task ChangeGroupName_ShouldThrow_WhenGroupNotFound()
    {
        var nonExistingGroupId = Guid.NewGuid();

        await FluentActions
            .Awaiting(() => _sut.ChangeGroupName(Guid.NewGuid(), "newName"))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Group not found");
    }

    [Fact]
    public async Task ChangeGroupName_ShouldThrowNotFoundException_WhenGroupNotFound()
    {
        var groupId = Guid.NewGuid();

        var act = async () => await _sut.ChangeGroupName(groupId, "newName");

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Group not found");
    }

    [Fact]
    public async Task DeleteGroup_ShouldDeleteGroup_WhenGroupFound()
    {
        var groupId = Guid.NewGuid();

        _db.Groups.Add(
            new Group
            {
                Id = groupId,
                Name = "group",
                CreatedAt = DateTime.UtcNow,
            }
        );

        await _db.SaveChangesAsync();

        await _sut.DeleteGroup(groupId);

        var group = await _db.Groups.FindAsync(groupId);

        group.Should().BeNull();
    }

    [Fact]
    public async Task DeleteGroup_ShouldThrowNotFoundException_WhenGroupNotFound()
    {
        var groupId = Guid.NewGuid();

        var act = async () => await _sut.DeleteGroup(groupId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Group not found");
    }

    [Fact]
    public async Task GetGroup_ShouldReturnGroup_WhenUserIsMember()
    {
        var groupId = Guid.NewGuid();
        var userId = _currentUserMock.Object.UserId;

        var user = new User
        {
            Id = userId,
            Username = "user",
            Email = "user@test.com",
        };

        var group = new Group
        {
            Id = groupId,
            Name = "group",
            CreatedAt = DateTime.UtcNow,
        };

        _db.Users.Add(user);
        _db.Groups.Add(group);
        _db.UserGroups.Add(
            new UserGroup
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = userId,
                User = user,
                Group = group,
            }
        );

        await _db.SaveChangesAsync();

        var result = await _sut.GetGroup(groupId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(groupId);
        result.Name.Should().Be("group");
        result.Users.Should().ContainSingle();
        result.Users.First().Id.Should().Be(userId);
        result.Users.First().Username.Should().Be("user");
    }

    [Fact]
    public async Task GetGroup_ShouldThrowNotFoundException_WhenUserIsNotMember()
    {
        var groupId = Guid.NewGuid();

        _db.Groups.Add(
            new Group
            {
                Id = groupId,
                Name = "group",
                CreatedAt = DateTime.UtcNow,
            }
        );

        await _db.SaveChangesAsync();

        var act = async () => await _sut.GetGroup(groupId);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage("Group not found");
    }

    [Fact]
    public async Task GetGroups_ShouldReturnOnlyGroupsWhereUserIsMember()
    {
        var userId = _currentUserMock.Object.UserId;

        var user = new User
        {
            Id = userId,
            Username = "user",
            Email = "user@test.com",
        };

        var memberGroup1 = new Group
        {
            Id = Guid.NewGuid(),
            Name = "member-group-1",
            CreatedAt = DateTime.UtcNow,
        };

        var memberGroup2 = new Group
        {
            Id = Guid.NewGuid(),
            Name = "member-group-2",
            CreatedAt = DateTime.UtcNow,
        };

        var unrelatedGroup = new Group
        {
            Id = Guid.NewGuid(),
            Name = "unrelated-group",
            CreatedAt = DateTime.UtcNow,
        };

        _db.Users.Add(user);
        _db.Groups.AddRange(memberGroup1, memberGroup2, unrelatedGroup);

        _db.UserGroups.AddRange(
            new UserGroup
            {
                Id = Guid.NewGuid(),
                GroupId = memberGroup1.Id,
                UserId = userId,
                User = user,
                Group = memberGroup1,
            },
            new UserGroup
            {
                Id = Guid.NewGuid(),
                GroupId = memberGroup2.Id,
                UserId = userId,
                User = user,
                Group = memberGroup2,
            }
        );

        await _db.SaveChangesAsync();

        var result = await _sut.GetGroups();

        result.Should().HaveCount(2);
        result.Should().Contain(g => g.Id == memberGroup1.Id);
        result.Should().Contain(g => g.Id == memberGroup2.Id);
        result.Should().NotContain(g => g.Id == unrelatedGroup.Id);
    }

    [Fact]
    public async Task GetGroups_ShouldReturnEmptyCollection_WhenUserHasNoGroups()
    {
        var userId = _currentUserMock.Object.UserId;

        _db.Users.Add(
            new User
            {
                Id = userId,
                Username = "user",
                Email = "user@test.com",
            }
        );

        _db.Groups.Add(
            new Group
            {
                Id = Guid.NewGuid(),
                Name = "group",
                CreatedAt = DateTime.UtcNow,
            }
        );

        await _db.SaveChangesAsync();

        var result = await _sut.GetGroups();

        result.Should().BeEmpty();
    }
}
