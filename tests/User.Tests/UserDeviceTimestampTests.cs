using Amazon.DynamoDBv2.Model;
using Domain.Entities;
using Infrastructure.Extensions;
using Xunit;

namespace User.Tests;

public sealed class UserDeviceTimestampTests
{
    [Fact]
    public void LegacyDynamoRowWithoutTimestamps_LeavesDatesUnknown()
    {
        var row = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new() { S = "userDevices#user-1" },
            ["sk"] = new() { S = "token-1" },
            ["id"] = new() { S = "token-1" },
            ["userId"] = new() { S = "user-1" },
            ["platform"] = new() { S = "ios" }
        };

        var device = row.ToEntity<UserDeviceEntity>();

        Assert.Null(device.CreatedAt);
        Assert.Null(device.ModifiedAt);
    }

    [Fact]
    public void DynamoRowWithTimestamps_PreservesRecordedDates()
    {
        const string createdAt = "2026-01-05T10:00:00Z";
        const string modifiedAt = "2026-02-06T11:30:00Z";
        var row = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new() { S = "userDevices#user-1" },
            ["sk"] = new() { S = "token-1" },
            ["id"] = new() { S = "token-1" },
            ["userId"] = new() { S = "user-1" },
            ["platform"] = new() { S = "android" },
            ["createdAt"] = new() { S = createdAt },
            ["modifiedAt"] = new() { S = modifiedAt }
        };

        var device = row.ToEntity<UserDeviceEntity>();

        Assert.Equal(DateTime.Parse(createdAt).ToUniversalTime(), device.CreatedAt);
        Assert.Equal(DateTime.Parse(modifiedAt).ToUniversalTime(), device.ModifiedAt);
    }
}
