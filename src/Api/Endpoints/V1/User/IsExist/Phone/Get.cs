using Api.Infrastructure.Contract;
using Api.Infrastructure.Auth;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.User.IsExist.Phone;

public class Get : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromQuery] string phone,
        HttpContext httpContext,
        [FromServices] IUniqueKeyRepository uniqueKeyRepository,
        [FromServices] IUserAccessValidator userAccessValidator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return Results.BadRequest();

        var normalizedPhone = phone.Trim().Replace("+90", string.Empty).Replace(" ", string.Empty);
        var value = await uniqueKeyRepository.GetAsync(normalizedPhone, UniqueKeyType.Phone, cancellationToken);

        if (userAccessValidator.IsInternalCaller(httpContext))
            return Results.Ok(value != null);

        if (!userAccessValidator.TryGetRequesterUserId(httpContext, out var requesterUserId))
            return Results.Unauthorized();

        if (value == null)
            return Results.NotFound();

        // Hide existence of other users from non-internal callers.
        if (!userAccessValidator.IsSelf(requesterUserId, value.UserId))
            return Results.NotFound();

        return Results.Ok(true);
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/v1/users/phone/check", Handler)
            .Produces<bool>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithTags("User");
    }
}