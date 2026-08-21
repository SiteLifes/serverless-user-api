using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Dto;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.Staff.Users.ByPhone;

/// <summary>
/// Finds one user by phone number for the staff panel.
///
/// Phone numbers are indexed exactly, so this is a lookup rather than a search: the panel cannot
/// scan every user, and staff searching by phone is the case that has to be fast.
/// </summary>
public class Get : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromQuery] string? phone,
        [FromServices] IApiContext apiContext,
        [FromServices] IUniqueKeyRepository uniqueKeyRepository,
        [FromServices] IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        if (!apiContext.IsStaff)
            return Results.Forbid();

        if (string.IsNullOrWhiteSpace(phone))
            return Results.BadRequest();

        // Stored without the country code or spacing, so both "+90 555 111 22 33" and
        // "5551112233" find the same person.
        var normalizedPhone = phone.Trim().Replace("+90", string.Empty).Replace(" ", string.Empty);

        var key = await uniqueKeyRepository.GetAsync(normalizedPhone, UniqueKeyType.Phone, cancellationToken);
        if (key == null)
            return Results.NotFound();

        var user = await userRepository.GetAsync(key.UserId, cancellationToken);
        if (user == null)
            return Results.NotFound();

        return Results.Ok(user.ToDto());
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("v1/staff/users/by-phone", Handler)
            .Produces<UserDto>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("Staff");
    }
}
