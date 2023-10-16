using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Enums;
using Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints.V1.User.Me.Phone;

public class Post : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromBody] UserUpdatePhoneRequest request,
        [FromServices] IApiContext apiContext,
        [FromServices] IUserRepository userRepository,
        [FromServices] IOtpCodeRepository otpCodeRepository,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetAsync(apiContext.CurrentUserId, cancellationToken);
        if (user == null)
        {
            return Results.NotFound();
        }

        var otpCodeIsValid = await otpCodeRepository.CheckOtpCodeAsync(apiContext.CurrentUserId, request.Code, UniqueKeyType.PhoneUpdateRequest, cancellationToken);
        if (!otpCodeIsValid)
        {
            return Results.NotFound();
        }

        user.Phone = request.Phone;
        await userRepository.SaveAsync(user, cancellationToken);

        return Results.Ok();
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/v1/users/me/email", Handler)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status500InternalServerError)
            .WithTags("User");
    }

    public class UserUpdatePhoneRequest
    {
        public string Code { get; set; } = default!;
        public string Phone { get; set; } = default!;
    }
}