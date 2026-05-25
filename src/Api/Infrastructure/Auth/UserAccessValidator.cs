using Domain.Options;
using Microsoft.Extensions.Options;

namespace Api.Infrastructure.Auth;

public interface IUserAccessValidator
{
    bool IsInternalCaller(HttpContext httpContext);
    bool TryGetRequesterUserId(HttpContext httpContext, out string requesterUserId);
    bool IsSelf(string requesterUserId, string ownerUserId);
    IResult? AuthorizeInternalOrSelf(HttpContext httpContext, string ownerUserId);
}

public class UserAccessValidator : IUserAccessValidator
{
    private readonly IOptionsSnapshot<InternalServiceAuthSettings> _internalServiceAuthSettings;

    public UserAccessValidator(IOptionsSnapshot<InternalServiceAuthSettings> internalServiceAuthSettings)
    {
        _internalServiceAuthSettings = internalServiceAuthSettings;
    }

    public bool IsInternalCaller(HttpContext httpContext)
    {
        var authSettings = _internalServiceAuthSettings.Value;
        if (string.IsNullOrWhiteSpace(authSettings.ApiKey))
            return false;

        var internalHeaderName = string.IsNullOrWhiteSpace(authSettings.HeaderName)
            ? "x-internal-api-key"
            : authSettings.HeaderName;

        return httpContext.Request.Headers.TryGetValue(internalHeaderName, out var internalApiKeyHeader) &&
               string.Equals(internalApiKeyHeader.ToString(), authSettings.ApiKey, StringComparison.Ordinal);
    }

    public bool TryGetRequesterUserId(HttpContext httpContext, out string requesterUserId)
    {
        if (httpContext.Request.Headers.TryGetValue("x-user-id", out var userIdHeader) &&
            !string.IsNullOrWhiteSpace(userIdHeader))
        {
            requesterUserId = userIdHeader.ToString();
            return true;
        }

        requesterUserId = string.Empty;
        return false;
    }

    public bool IsSelf(string requesterUserId, string ownerUserId)
    {
        return string.Equals(requesterUserId, ownerUserId, StringComparison.Ordinal);
    }

    public IResult? AuthorizeInternalOrSelf(HttpContext httpContext, string ownerUserId)
    {
        if (IsInternalCaller(httpContext))
            return null;

        if (!TryGetRequesterUserId(httpContext, out var requesterUserId))
            return Results.Unauthorized();

        return IsSelf(requesterUserId, ownerUserId) ? null : Results.NotFound();
    }
}

