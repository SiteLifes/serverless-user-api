using Api.Infrastructure.Contract;
using Api.Infrastructure.Auth;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.User.IsExist.Phone.Ids;

/// <summary>
/// Telefon numaralarından kullanıcı kimliklerine, tek istekte. Yalnız iç servisler çağırır.
///
/// WhatsApp gelen kutusu yüzlerce numarayı birden çözmek zorunda; numara başına
/// <c>/v1/users/phone/id</c> çağırmak (gateway üzerinden her biri ayrı Lambda turu) ekranı
/// saniyelerce bekletiyordu. Bulunmayan numara cevapta yok.
/// </summary>
public class Post : IEndpoint
{
    public const int MaxPhones = 500;

    public sealed record PhoneIdsRequest(List<string>? Phones);

    private static async Task<IResult> Handler(
        [FromBody] PhoneIdsRequest? request,
        HttpContext httpContext,
        [FromServices] IUniqueKeyRepository uniqueKeyRepository,
        [FromServices] IUserAccessValidator userAccessValidator,
        CancellationToken cancellationToken)
    {
        if (!userAccessValidator.IsInternalCaller(httpContext))
            return Results.StatusCode(StatusCodes.Status403Forbidden);

        var phones = (request?.Phones ?? new List<string>())
            .Where(phone => !string.IsNullOrWhiteSpace(phone))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (phones.Count > MaxPhones)
            return Results.BadRequest($"phones must not contain more than {MaxPhones} numbers");

        // Tekil uçla aynı biçim: ülke kodu ve boşluk olmadan.
        var normalizedByPhone = phones.ToDictionary(phone => phone, Normalize, StringComparer.Ordinal);
        var userIdByKey = (await uniqueKeyRepository.GetManyAsync(
                normalizedByPhone.Values.ToList(), UniqueKeyType.Phone, cancellationToken))
            .Where(key => !string.IsNullOrWhiteSpace(key.UserId))
            .GroupBy(key => key.Value, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().UserId, StringComparer.Ordinal);

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (phone, normalized) in normalizedByPhone)
        {
            if (userIdByKey.TryGetValue(normalized, out var userId))
                result[phone] = userId;
        }

        return Results.Ok(result);
    }

    public static string Normalize(string phone) =>
        phone.Trim().Replace("+90", string.Empty).Replace(" ", string.Empty);

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/v1/users/phone/ids", Handler)
            .Produces<Dictionary<string, string>>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithTags("User");
    }
}
