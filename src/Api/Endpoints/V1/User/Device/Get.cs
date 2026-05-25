using Api.Infrastructure.Contract;
using Api.Infrastructure.Auth;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.User.Device;

public class Get : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromRoute] string id,
        HttpContext httpContext,
        [FromServices] IUserDeviceRepository userDeviceRepository,
        [FromServices] IUserAccessValidator userAccessValidator,
        CancellationToken cancellationToken)
    {
        var authorizationResult = userAccessValidator.AuthorizeInternalOrSelf(httpContext, id);
        if (authorizationResult != null)
            return authorizationResult;

        var (userDevices, _) = await userDeviceRepository.GetUserDevicesPagedAsync(id, 1000, null, cancellationToken);

        var latestDevice = userDevices.OrderByDescending(x => x.ModifiedAt).FirstOrDefault();
        if (latestDevice == null)
            return Results.NotFound();

        return Results.Ok(latestDevice.Id);
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v1/users/{id}/device", Handler)
            .Produces<string>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("User");
    }
}