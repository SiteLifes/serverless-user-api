using Api.Infrastructure.Contract;
using Domain.Dto;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.User.Device;

public class Get : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromRoute] string id,
        [FromServices] IUserDeviceRepository userDeviceRepository,
        CancellationToken cancellationToken)
    {
        var (userDevices, token) = await userDeviceRepository.GetUserDevicesPagedAsync(id, 1, null, cancellationToken);
        var deviceId = userDevices.FirstOrDefault().Id;
        return Results.Ok(deviceId);
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v1/users/{id}/device", Handler)
            .Produces<string>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("User");
    }
}