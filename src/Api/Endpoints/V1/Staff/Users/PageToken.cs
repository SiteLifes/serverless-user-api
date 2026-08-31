using System.Net;
using System.Text;
using System.Text.Json;

namespace Api.Endpoints.V1.Staff.Users;

/// <summary>
/// Translates DynamoDB's continuation key to something that survives a round trip through a client.
///
/// The repository hands the key back as the JSON of the whole AttributeValue — every field of it,
/// including "NULL": false. Sent back as-is, DynamoDB refuses it: "Null attribute value types must
/// have the value of true". It is also url-encoded on the way out but read raw on the way in, so a
/// client that encodes its query parameters corrupts it a second time.
///
/// Carrying only the two key strings, base64url encoded, sidesteps both: no field the key does not
/// need, and no character a url encoder will touch.
/// </summary>
public static class PageToken
{
    private sealed record Key(string Pk, string Sk);

    /// <summary>Every user lives in one partition, so a token built here already knows its pk.</summary>
    private const string UsersPk = "users";

    /// <summary>Repository token to something the client can hand back. Null when there is no next page.</summary>
    public static string? ForClient(string? repositoryToken)
    {
        if (string.IsNullOrWhiteSpace(repositoryToken))
            return null;

        var json = Unescape(repositoryToken);

        try
        {
            using var document = JsonDocument.Parse(json);

            var pk = ReadString(document.RootElement, "pk");
            var sk = ReadString(document.RootElement, "sk");

            // An empty last-evaluated key is DynamoDB saying this was the final page.
            if (pk is null || sk is null)
                return null;

            var payload = JsonSerializer.SerializeToUtf8Bytes(new Key(pk, sk));

            return Base64Url(payload);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Client token back to the shape the repository parses. Only the S field is written, so the
    /// AttributeValue that comes out carries nothing else for DynamoDB to reject.
    /// </summary>
    public static string? ForRepository(string? clientToken)
    {
        var key = Read(clientToken);

        if (key is null)
            return null;

        return JsonSerializer.Serialize(new Dictionary<string, Dictionary<string, string>>
        {
            ["pk"] = new() { ["S"] = key.Pk },
            ["sk"] = new() { ["S"] = key.Sk }
        });
    }

    /// <summary>
    /// The user id a name search should carry on after, read out of a client token.
    ///
    /// Search pages by sort key alone — the partition is fixed — so it needs the id rather than the
    /// whole key, but it travels in the same token as the plain listing's so a client never has to
    /// tell the two apart.
    /// </summary>
    public static string? SkForRepository(string? clientToken) => Read(clientToken)?.Sk;

    /// <summary>A client token that resumes after a user id. Null when there is nothing more to read.</summary>
    public static string? FromSk(string? sk)
    {
        if (string.IsNullOrWhiteSpace(sk))
            return null;

        return Base64Url(JsonSerializer.SerializeToUtf8Bytes(new Key(UsersPk, sk)));
    }

    private static Key? Read(string? clientToken)
    {
        if (string.IsNullOrWhiteSpace(clientToken))
            return null;

        try
        {
            var key = JsonSerializer.Deserialize<Key>(FromBase64Url(clientToken));

            return key is null || string.IsNullOrEmpty(key.Pk) || string.IsNullOrEmpty(key.Sk) ? null : key;
        }
        catch (Exception exception) when (exception is JsonException or FormatException)
        {
            // A token we cannot read is treated as no token: the caller gets the first page rather
            // than an error they can do nothing about.
            return null;
        }
    }

    private static string? ReadString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var attribute)
        && attribute.TryGetProperty("S", out var value)
        && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>
    /// Peels off however many layers of url encoding the token picked up in transit. Decoded the
    /// same way the repository encoded it — WebUtility, which reads "+" as a space — so the two
    /// stay each other's inverse.
    /// </summary>
    private static string Unescape(string value)
    {
        var current = value;
        for (var attempt = 0; attempt < 3 && !current.StartsWith('{'); attempt++)
        {
            current = WebUtility.UrlDecode(current);
        }

        return current;
    }

    private static string Base64Url(byte[] payload) =>
        Convert.ToBase64String(payload).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += (padded.Length % 4) switch
        {
            2 => "==",
            3 => "=",
            _ => string.Empty
        };

        return Convert.FromBase64String(padded);
    }
}
