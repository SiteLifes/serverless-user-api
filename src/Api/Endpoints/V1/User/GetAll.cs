using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Dto;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.User;

public class GetAll : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromServices] IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var users = await userRepository.GetAllAsync(cancellationToken);
        return Results.Ok(new PagedResponse<UserDto>
        {
            Data = users.Select(q => q.ToDto()).ToList(),
        });
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v1/all-users", Handler)
            .Produces<PagedResponse<UserDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("User");
    }
}