using Domain.Entities;

namespace Domain.Dto;

public class UserDeviceDto
{
    public string Id { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string? Platform { get; set; }
    public DateTime ? CreatedAt { get; set; }
    public DateTime ? ModifiedAt { get; set; }

    public Dictionary<string, string> AdditionalData { get; set; } = new();
}

public static class UserDeviceDtoMapper
{
    public static UserDeviceDto ToDto(this UserDeviceEntity entity)
    {
        return new UserDeviceDto
        {
            Platform = entity.Platform,
            AdditionalData = entity.AdditionalData,
            UserId = entity.UserId,
            Id = entity.Id,
            CreatedAt = entity.CreatedAt,
            ModifiedAt = entity.ModifiedAt
        };
    }
}