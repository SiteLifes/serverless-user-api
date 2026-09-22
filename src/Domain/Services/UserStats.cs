using System.Globalization;
using Domain.Entities;

namespace Domain.Services;

/// <summary>Kullanıcının hangi uygulamayla geldiği: son kaydettiği cihazın platformu.</summary>
public static class UserPlatform
{
    public const string Ios = "ios";
    public const string Android = "android";

    /// <summary>Hiç cihaz kaydı yok: uygulamayı açmamış, bildirim izni vermemiş ya da yalnız web.</summary>
    public const string None = "none";

    public static string Normalize(string? platform) =>
        platform?.Trim().ToLowerInvariant() switch
        {
            "ios" => Ios,
            "android" => Android,
            _ => None
        };
}

public sealed record UserStatsMonth(string Month, int Ios, int Android, int None);

/// <param name="Months">İlk kaydın ayından bu aya kadar her ay, boş aylar dahil.</param>
public sealed record UserStatsResult(int Total, int Ios, int Android, int None, IReadOnlyList<UserStatsMonth> Months);

/// <summary>
/// Staff dashboard'unun kullanıcı grafiği: ay ay yeni kayıt, uygulamaya göre bölünmüş.
///
/// Cihaz kaydı FCM token'ı alınınca yazılıyor, yani "uygulamasız" sayısı web'den giren ya da bildirim
/// izni vermeyen kullanıcıları da içeriyor. Birden fazla cihazı olanın platformu en son güncellenen
/// cihazından alınıyor.
/// </summary>
public static class UserStats
{
    private const string EarliestMonth = "2020-01";

    private static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static UserStatsResult Build(
        IEnumerable<UserEntity> users,
        IEnumerable<UserDeviceEntity> devices,
        IReadOnlyCollection<string>? onlyUserIds,
        DateTime utcNow)
    {
        var platformByUser = devices
            .Where(device => !string.IsNullOrWhiteSpace(device.UserId))
            .GroupBy(device => device.UserId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => UserPlatform.Normalize(group
                    .OrderByDescending(device => device.ModifiedAt ?? device.CreatedAt)
                    .First()
                    .Platform),
                StringComparer.OrdinalIgnoreCase);

        HashSet<string>? filter = onlyUserIds is null
            ? null
            : new HashSet<string>(onlyUserIds, StringComparer.OrdinalIgnoreCase);

        var rows = users
            .Where(user => !string.IsNullOrWhiteSpace(user.Id))
            .Where(user => filter is null || filter.Contains(user.Id))
            .Select(user => (
                Month: MonthOf(user.CreatedAt),
                Platform: platformByUser.GetValueOrDefault(user.Id, UserPlatform.None)))
            .ToList();

        var months = new List<UserStatsMonth>();
        var byMonth = rows
            .GroupBy(row => row.Month, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        // Tarihi boş (0001-01) bir kayıt ekseni iki bin yıla yaymasın; toplamda yine sayılıyor.
        var firstMonth = byMonth.Keys
            .Where(month => string.CompareOrdinal(month, EarliestMonth) >= 0)
            .Min(StringComparer.Ordinal);

        if (firstMonth is not null)
        {
            var cursor = DateTime.ParseExact(firstMonth, "yyyy-MM", CultureInfo.InvariantCulture);
            var last = DateTime.ParseExact(MonthOf(utcNow), "yyyy-MM", CultureInfo.InvariantCulture);

            for (; cursor <= last; cursor = cursor.AddMonths(1))
            {
                var key = cursor.ToString("yyyy-MM", CultureInfo.InvariantCulture);
                var inMonth = byMonth.GetValueOrDefault(key) ?? new List<(string Month, string Platform)>();
                months.Add(new UserStatsMonth(
                    key,
                    inMonth.Count(row => row.Platform == UserPlatform.Ios),
                    inMonth.Count(row => row.Platform == UserPlatform.Android),
                    inMonth.Count(row => row.Platform == UserPlatform.None)));
            }
        }

        return new UserStatsResult(
            rows.Count,
            rows.Count(row => row.Platform == UserPlatform.Ios),
            rows.Count(row => row.Platform == UserPlatform.Android),
            rows.Count(row => row.Platform == UserPlatform.None),
            months);
    }

    private static string MonthOf(DateTime value)
    {
        var utc = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return TimeZoneInfo.ConvertTimeFromUtc(utc, TurkeyTimeZone).ToString("yyyy-MM", CultureInfo.InvariantCulture);
    }
}
