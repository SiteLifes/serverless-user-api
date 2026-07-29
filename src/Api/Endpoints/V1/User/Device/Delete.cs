using Api.Infrastructure.Contract;
using Api.Infrastructure.Auth;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.User.Device;

public class Delete : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromRoute] string id,
        [FromRoute] string deviceId,
        HttpContext httpContext,
        [FromServices] IUserDeviceRepository userDeviceRepository,
        [FromServices] IUserAccessValidator userAccessValidator,
        CancellationToken cancellationToken)
    {
        var authorizationResult = userAccessValidator.AuthorizeInternalOrSelf(httpContext, id);
        if (authorizationResult != null)
            return authorizationResult;

        var userDevice = await userDeviceRepository.GetUserDeviceAsync(id, deviceId, cancellationToken);
        if (userDevice == null)
        {
            return Results.NotFound();
        }

        await userDeviceRepository.DeleteUserDeviceAsync(id, deviceId, cancellationToken);

        return Results.Ok();
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapDelete("/v1/users/{id}/device/{deviceId}", Handler)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithTags("User");
    }
}
