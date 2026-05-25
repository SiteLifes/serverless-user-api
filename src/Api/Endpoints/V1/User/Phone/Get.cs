using Api.Infrastructure.Contract;
using Api.Infrastructure.Auth;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.User.Phone;

public class Get : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromRoute] string id,
        HttpContext httpContext,
        [FromServices] IUserRepository repository,
        [FromServices] IUserAccessValidator userAccessValidator,
        CancellationToken cancellationToken)
    {
        var authorizationResult = userAccessValidator.AuthorizeInternalOrSelf(httpContext, id);
        if (authorizationResult != null)
            return authorizationResult;

        var user = await repository.GetAsync(id, cancellationToken);
        
        if (user == null)
            return Results.NotFound();
        
        return Results.Ok(user.Phone);
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v1/users/{id}/phone", Handler)
            .Produces<string>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("User");
    }
}