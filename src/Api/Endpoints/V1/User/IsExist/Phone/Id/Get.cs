using Api.Infrastructure.Contract;
using Api.Infrastructure.Auth;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.User.IsExist.Phone.Id;

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
        if (value == null)
            return Results.NotFound();

        var authorizationResult = userAccessValidator.AuthorizeInternalOrSelf(httpContext, value.UserId);
        if (authorizationResult != null)
            return authorizationResult;

        return Results.Ok(value.UserId);
    }
    
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/v1/users/phone/id", Handler)
            .Produces<string>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithTags("User");
    }
}