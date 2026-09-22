using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Entities;
using Domain.Repositories;
using Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace Api.Endpoints.V1.Staff.Users;

/// <summary>
/// Staff dashboard'unun kullanıcı grafiği: ay ay yeni kayıt, iOS / Android / uygulamasız.
///
/// POST, çünkü site filtresi kullanıcı id'leriyle geliyor — bu servis siteleri bilmiyor, bir sitenin
/// sakinlerini panel Property'den alıp buraya veriyor ve yüzlerce id bir sorgu dizesine sığmıyor.
/// Bir şey yazmıyor; panelde salt okuma yetkili personel de çağırabiliyor.
///
/// Bütün kullanıcılar ve cihazlar birkaç dakika bellekte tutuluyor: site değiştirmek her seferinde
/// tabloyu yeniden taramasın.
/// </summary>
public class Stats : IEndpoint
{
    private const string CacheKey = "staff-user-stats-source";
    private const int MaxUserIds = 20000;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private static async Task<IResult> Handler(
        [FromBody] UserStatsRequest? request,
        [FromServices] IApiContext apiContext,
        [FromServices] IUserRepository userRepository,
        [FromServices] IUserDeviceRepository userDeviceRepository,
        [FromServices] IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        if (!apiContext.IsStaff)
            return Results.Forbid();

        if (request?.UserIds is { Count: > MaxUserIds })
            return Results.BadRequest($"userIds must not contain more than {MaxUserIds} ids");

        var source = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            var usersTask = userRepository.GetAllAsync(cancellationToken);
            var devicesTask = userDeviceRepository.GetAllDevicePlatformsAsync(cancellationToken);

            return new StatsSource((await usersTask).ToList(), await devicesTask);
        });

        var result = UserStats.Build(source!.Users, source.Devices, request?.UserIds, DateTime.UtcNow);
        return Results.Ok(result);
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("v1/staff/users/stats", Handler)
            .Produces<UserStatsResult>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("Staff");
    }

    /// <param name="UserIds">Verilirse yalnız bu kullanıcılar sayılıyor; boş liste "kimse" demek.</param>
    public sealed record UserStatsRequest(List<string>? UserIds);

    private sealed record StatsSource(List<UserEntity> Users, List<UserDeviceEntity> Devices);
}
