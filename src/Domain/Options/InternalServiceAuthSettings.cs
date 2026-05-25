namespace Domain.Options;

public class InternalServiceAuthSettings
{
    public string? ApiKey { get; set; }
    public string HeaderName { get; set; } = "x-internal-api-key";
}

