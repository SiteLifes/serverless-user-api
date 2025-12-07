using Api.Infrastructure.Contract;
using Domain.Dto;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.User.Device;

public class Post : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromBody] List<string> userIds,
        [FromServices] IUserDeviceRepository userDeviceRepository,
        CancellationToken cancellationToken)
    {
        List<string> deviceIds = new List<string>();
        foreach (var id in userIds)
        {
            var (userDevices, token) = await userDeviceRepository.GetUserDevicesPagedAsync(id, 1000, null, cancellationToken);
            if (userDevices == null || userDevices.Count < 1)
                continue;
            deviceIds.Add(userDevices.OrderByDescending(x => x.ModifiedAt).FirstOrDefault().Id);
        }

        return Results.Ok(deviceIds);
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