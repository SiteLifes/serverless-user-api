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

        var (users, token) = await userRepository.GetPagedAsync(
            pageSize,
            NormalizeIncoming(nextToken),
            cancellationToken);

        return Results.Ok(new PagedResponse<UserDto>
        {
            Data = users.Select(user => user.ToDto()).ToList(),
            Limit = pageSize,
            NextToken = NormalizeOutgoing(token),
            PreviousToken = nextToken
        });
    }

    /// <summary>
    /// The repository hands the continuation token back url-encoded but expects it raw. A token
    /// that makes a full round trip therefore arrives encoded a second time, fails to deserialize,
    /// and — because that failure is swallowed — resolves to "no start key", which serves page one
    /// forever. Unwrapping however many layers of encoding are on it closes that.
    /// </summary>
    private static string? NormalizeIncoming(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var current = token;
        for (var attempt = 0; attempt < 3 && !current.StartsWith('{'); attempt++)
        {
            current = Uri.UnescapeDataString(current);
        }

        return current.StartsWith('{') ? current : null;
    }

    /// <summary>Raw, so the caller can hand it straight back. An empty key means this was the last page.</summary>
    private static string? NormalizeOutgoing(string? token)
    {
        var decoded = NormalizeIncoming(token);

        return decoded is null or "{}" ? null : decoded;
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
