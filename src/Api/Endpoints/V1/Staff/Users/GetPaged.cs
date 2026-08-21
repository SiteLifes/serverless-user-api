using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Dto;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.Staff.Users;

/// <summary>
/// Every user in the system, a page at a time, for the internal staff panel.
///
/// The gateway already restricts this to staff tokens; the check here is the second lock, so the
/// endpoint is not open to anything that reaches the service by another route.
/// </summary>
public class GetPaged : IEndpoint
{
    private const int DefaultLimit = 25;
    private const int MaxLimit = 100;

    private static async Task<IResult> Handler(
        [FromQuery] int? limit,
        [FromQuery] string? nextToken,
        [FromServices] IApiContext apiContext,
        [FromServices] IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        if (!apiContext.IsStaff)
            return Results.Forbid();

        var pageSize = Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);

        var (users, token) = await userRepository.GetPagedAsync(pageSize, nextToken, cancellationToken);

        return Results.Ok(new PagedResponse<UserDto>
        {
            Data = users.Select(user => user.ToDto()).ToList(),
            Limit = pageSize,
            NextToken = token,
            PreviousToken = nextToken
        });
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v1/staff/users", Handler)
            .Produces<PagedResponse<UserDto>>()
            .Produces(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("Staff");
    }
}
