using Domain.Entities;
using Domain.Services;
using Xunit;

namespace User.Tests;

public class UserStatsTests
{
    private static readonly DateTime Now = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);

    private static UserEntity User(string id, DateTime createdAtUtc) =>
        new() { Id = id, FirstName = "A", LastName = "B", Status = "active", CreatedAt = createdAtUtc };

    private static UserDeviceEntity Device(string userId, string? platform, DateTime modifiedAtUtc) =>
        new() { Id = Guid.NewGuid().ToString("N"), UserId = userId, Platform = platform, ModifiedAt = modifiedAtUtc };

    [Fact]
    public void Build_SplitsUsersByTheirLatestDevicePlatform()
    {
        var result = UserStats.Build(
            [
                User("ios-user", new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)),
                User("switched", new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc)),
                User("web-only", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc))
            ],
            [
                Device("ios-user", "ios", Now),
                Device("switched", "ios", Now.AddDays(-10)),
                Device("switched", "Android", Now.AddDays(-1))
            ],
            null,
            Now);

        Assert.Equal(3, result.Total);
        Assert.Equal(1, result.Ios);
        Assert.Equal(1, result.Android);
        Assert.Equal(1, result.None);

        Assert.Equal(["2026-01", "2026-02", "2026-03"], result.Months.Select(month => month.Month));
        Assert.Equal(new UserStatsMonth("2026-01", 1, 1, 0), result.Months[0]);
        Assert.Equal(new UserStatsMonth("2026-02", 0, 0, 0), result.Months[1]);
        Assert.Equal(new UserStatsMonth("2026-03", 0, 0, 1), result.Months[2]);
    }

    [Fact]
    public void Build_WithUserIds_CountsOnlyThoseUsers()
    {
        var result = UserStats.Build(
            [User("a", Now), User("b", Now), User("c", Now)],
            [],
            ["A", "c", "missing"],
            Now);

        Assert.Equal(2, result.Total);
    }

    [Fact]
    public void Build_WithAnEmptyUserIdList_CountsNobody()
    {
        var result = UserStats.Build([User("a", Now)], [], [], Now);

        Assert.Equal(0, result.Total);
        Assert.Empty(result.Months);
    }

    [Fact]
    public void Build_UsesTurkeyTimeForTheMonth()
    {
        // 31 Ocak 22:00 UTC, Türkiye'de 1 Şubat.
        var result = UserStats.Build(
            [User("a", new DateTime(2026, 1, 31, 22, 0, 0, DateTimeKind.Utc))],
            [],
            null,
            Now);

        Assert.Equal("2026-02", result.Months[0].Month);
        Assert.Equal(1, result.Months[0].None);
    }

    [Fact]
    public void Build_AnUndatedUserIsCountedButDoesNotStretchTheAxis()
    {
        var result = UserStats.Build(
            [User("old", default), User("new", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc))],
            [],
            null,
            Now);

        Assert.Equal(2, result.Total);
        Assert.Equal("2026-03", Assert.Single(result.Months).Month);
    }
}
