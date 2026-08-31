using System.Text.Json;
using System.Web;
using Amazon.DynamoDBv2.Model;
using Api.Endpoints.V1.Staff.Users;
using Xunit;

namespace User.Tests;

/// <summary>
/// The staff user list pages through DynamoDB, and the continuation key has to survive a round trip
/// through a browser. Two things break that on the way, and both fail in ways that are hard to read
/// from the outside — one silently serves page one forever, the other is a validation error from
/// DynamoDB. These tests pin the shape of the token at both ends.
/// </summary>
public class PageTokenTests
{
    /// <summary>Exactly what the repository produces: the whole AttributeValue, url-encoded.</summary>
    private static string RepositoryToken(string pk, string sk)
    {
        var key = new Dictionary<string, AttributeValue>
        {
            ["pk"] = new() { S = pk },
            ["sk"] = new() { S = sk }
        };

        return HttpUtility.UrlEncode(JsonSerializer.Serialize(key));
    }

    [Fact]
    public void The_repository_token_really_does_carry_a_null_field()
    {
        // The premise of this whole class. If this ever stops being true the translation can go.
        var raw = JsonSerializer.Serialize(new Dictionary<string, AttributeValue>
        {
            ["pk"] = new() { S = "users" }
        });

        Assert.Contains("\"NULL\"", raw);
    }

    [Fact]
    public void A_client_token_carries_no_character_a_url_encoder_would_touch()
    {
        var token = PageToken.ForClient(RepositoryToken("users", "44aae00c-2997"));

        Assert.NotNull(token);
        Assert.Equal(token, Uri.EscapeDataString(token!));
    }

    [Fact]
    public void A_client_token_round_trips_back_to_a_key_dynamo_accepts()
    {
        var token = PageToken.ForClient(RepositoryToken("users", "44aae00c-2997"));

        var repositoryShape = PageToken.ForRepository(token);

        Assert.NotNull(repositoryShape);
        var key = JsonSerializer.Deserialize<Dictionary<string, AttributeValue>>(repositoryShape!);

        Assert.NotNull(key);
        Assert.Equal("users", key!["pk"].S);
        Assert.Equal("44aae00c-2997", key["sk"].S);

        // The reason page two failed: "Null attribute value types must have the value of true".
        Assert.DoesNotContain("\"NULL\"", repositoryShape!);
        Assert.DoesNotContain("\"BOOL\"", repositoryShape!);
    }

    [Fact]
    public void A_token_that_arrives_url_encoded_is_still_read()
    {
        var token = PageToken.ForClient(RepositoryToken("users", "abc"));

        Assert.Equal(PageToken.ForRepository(token), PageToken.ForRepository(Uri.EscapeDataString(token!)));
    }

    [Fact]
    public void The_last_page_produces_no_token()
    {
        // DynamoDB signals the end with an empty LastEvaluatedKey.
        var empty = HttpUtility.UrlEncode(JsonSerializer.Serialize(new Dictionary<string, AttributeValue>()));

        Assert.Null(PageToken.ForClient(empty));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void No_token_means_start_from_the_beginning(string? token)
    {
        Assert.Null(PageToken.ForClient(token));
        Assert.Null(PageToken.ForRepository(token));
    }

    [Theory]
    [InlineData("not-a-token")]
    [InlineData("!!!!")]
    [InlineData("eyJub3RBS2V5IjoxfQ")]
    public void An_unreadable_token_falls_back_to_the_first_page_rather_than_throwing(string token)
    {
        // A stale token in a bookmarked url should not turn into a 500.
        Assert.Null(PageToken.ForRepository(token));
    }

    [Theory]
    [InlineData("44aae00c-2997-40a4-b558-d38179b1d31d")]
    [InlineData("a/b=c")]
    [InlineData("boşluklu anahtar")]
    [InlineData("ĞÜŞİÖÇ")]
    [InlineData("a+b")]
    public void Keys_containing_awkward_characters_survive(string sk)
    {
        var token = PageToken.ForClient(RepositoryToken("users", sk));

        var repositoryShape = PageToken.ForRepository(token);
        var key = JsonSerializer.Deserialize<Dictionary<string, AttributeValue>>(repositoryShape!);

        Assert.Equal(sk, key!["sk"].S);
    }

    [Theory]
    [InlineData("44aae00c-2997-40a4-b558-d38179b1d31d")]
    [InlineData("boşluklu anahtar")]
    [InlineData("a+b")]
    public void A_search_cursor_round_trips_through_the_same_token(string sk)
    {
        // Name search pages by user id alone, but through the token the plain listing uses, so the
        // panel carries one kind of token whichever way it is reading the list.
        Assert.Equal(sk, PageToken.SkForRepository(PageToken.FromSk(sk)));
    }

    [Fact]
    public void No_search_cursor_means_no_token()
    {
        Assert.Null(PageToken.FromSk(null));
        Assert.Null(PageToken.FromSk(" "));
        Assert.Null(PageToken.SkForRepository("not-a-token"));
    }

    [Fact]
    public void A_listing_token_can_be_read_as_a_search_cursor()
    {
        // The panel hands back whatever token it was given; switching from the full list to a
        // search reuses it rather than failing on it.
        var token = PageToken.ForClient(RepositoryToken("users", "user-42"));

        Assert.Equal("user-42", PageToken.SkForRepository(token));
    }
}
