using Domain.Entities;

namespace Domain.Repositories;

public interface IUserDeviceRepository
{
    Task<UserDeviceEntity?> GetUserDeviceAsync(string userId, string deviceId, CancellationToken cancellationToken = default);
    Task<bool> SaveUserDeviceAsync(UserDeviceEntity entity, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserDeviceAsync(string userId, string deviceId, CancellationToken cancellationToken = default);
    Task<(List<UserDeviceEntity>, string)> GetUserDevicesPagedAsync(string userId, int limit, string? nextToken, CancellationToken cancellationToken = default);
    Task<bool> DeleteUserDevicesAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Bütün kullanıcıların cihaz kayıtları, yalnızca kullanıcı, platform ve tarihlerle. Tablo taranıyor:
    /// cihazlar kullanıcı başına partition'da, hepsini birden veren bir indeks yok.
    /// </summary>
    Task<List<UserDeviceEntity>> GetAllDevicePlatformsAsync(CancellationToken cancellationToken);
}