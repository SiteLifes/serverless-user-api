using Api.Infrastructure.Contract;
using Api.Infrastructure.Auth;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.User.IsExist.Email;

public class Get : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromQuery] string email,
        HttpContext httpContext,
        [FromServices] IUniqueKeyRepository uniqueKeyRepository,
        [FromServices] IUserAccessValidator userAccessValidator,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Results.BadRequest();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var value = await uniqueKeyRepository.GetAsync(normalizedEmail, UniqueKeyType.Email, cancellationToken);

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
        endpoints.MapGet("/v1/users/email/check", Handler)
            .Produces<bool>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithTags("User");
    }
}