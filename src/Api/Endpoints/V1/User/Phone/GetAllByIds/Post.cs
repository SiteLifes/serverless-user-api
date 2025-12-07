using Api.Infrastructure.Contract;
using Domain.Dto;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.User.Phone.GetByIds;

public class Post : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromBody] List<string> userIds,
        [FromServices] IUserRepository repository,
        CancellationToken cancellationToken)
    {
        var users = await repository.GetUsersAsync(userIds, cancellationToken);

        if (users == null || !users.Any())
            return Results.NotFound();
        var userPhones = users.Where(u => !string.IsNullOrEmpty(u.Phone)).Select(u => u.Phone).ToList();

        return Results.Ok(userPhones);
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("v1/users/phones/GetAllByIds", Handler)
            .Produces<string>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("User");
    }
}