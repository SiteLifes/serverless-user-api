using Api.Infrastructure.Contract;
using Domain.Dto;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.User.Device;

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
        var deviceIds = new string?[uniqueUserIds.Length];

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

                deviceIds[index] = userDevices?
                    .OrderByDescending(device => device.ModifiedAt)
                    .FirstOrDefault()?
                    .Id;
            });

        return Results.Ok(deviceIds.OfType<string>().ToList());
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("v1/users/devices/GetAllByIds", Handler)
            .Produces<List<string>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("User");
    }
}
