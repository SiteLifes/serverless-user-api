using Api.Infrastructure.Contract;
using Domain.Dto;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.User.Device.GetLatestByIds;

public class Post : IEndpoint
{
    private const int MaxConcurrentDeviceQueries = 8;

    private static async Task<IResult> Handler(
        [FromBody] List<string> userIds,
        [FromServices] IUserDeviceRepository userDeviceRepository,
        CancellationToken cancellationToken)
    {
        var uniqueUserIds = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToArray();
        var latestDevices = new UserDeviceDto?[uniqueUserIds.Length];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, uniqueUserIds.Length),
            new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxConcurrentDeviceQueries,
                CancellationToken = cancellationToken
            },
            async (index, token) =>
            {
                var (userDevices, _) = await userDeviceRepository.GetUserDevicesPagedAsync(
                    uniqueUserIds[index],
                    1000,
                    null,
                    token);

                latestDevices[index] = userDevices?
                    .OrderByDescending(device => device.ModifiedAt)
                    .FirstOrDefault()?
                    .ToDto();
            });

        return Results.Ok(latestDevices.OfType<UserDeviceDto>().ToList());
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("v1/users/devices/GetLatestByIds", Handler)
            .Produces<List<UserDeviceDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("User");
    }
}
