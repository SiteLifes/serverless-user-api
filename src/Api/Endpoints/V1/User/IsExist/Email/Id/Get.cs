using Api.Infrastructure.Contract;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Endpoints.V1.User.IsExist.Email.Id;

public class Get : IEndpoint
{
    private const string SensitiveLookupFeatureFlag = "Security:EnableSensitiveUserLookupEndpoints";

    private static async Task<IResult> Handler([FromQuery] string email,
        [FromServices] IUniqueKeyRepository uniqueKeyRepository, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            return Results.BadRequest();

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var value = await uniqueKeyRepository.GetAsync(normalizedEmail, UniqueKeyType.Email, cancellationToken);

        // Do not disclose internal user identifiers from public-facing APIs.
        return Results.Ok(value != null);
    }
    
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        var configuration = endpoints.ServiceProvider.GetRequiredService<IConfiguration>();
        var isEnabled = configuration.GetValue<bool>(SensitiveLookupFeatureFlag);
        if (!isEnabled)
            return;

        endpoints.MapGet("/v1/users/email/id", Handler)
            .Produces<bool>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithTags("User");
    }
}