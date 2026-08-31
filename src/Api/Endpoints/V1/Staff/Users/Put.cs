using Api.Infrastructure.Context;
using Api.Infrastructure.Contract;
using Domain.Dto;
using Domain.Entities;
using Domain.Enums;
using Domain.Options;
using Domain.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Api.Endpoints.V1.Staff.Users;

/// <summary>
/// Corrects a user's name and email address from the internal staff panel.
///
/// Deliberately narrow. The resident-facing update rebuilds the whole record — it is the register
/// form as much as an edit — and sending a user through it from the back office would reset their
/// verification state and their phone. This one reads the record, changes three fields and writes
/// it back, so everything else survives a correction to a misspelt name.
///
/// The gateway restricts this to admin staff tokens; the check here is the second lock, so the
/// endpoint is not open to anything that reaches the service by another route.
/// </summary>
public class Put : IEndpoint
{
    private static async Task<IResult> Handler(
        [FromRoute] string id,
        [FromBody] StaffUserUpdateRequest request,
        [FromServices] IApiContext apiContext,
        [FromServices] IUserRepository userRepository,
        [FromServices] IUniqueKeyRepository uniqueKeyRepository,
        [FromServices] IOptionsSnapshot<UniqueKeySettings> uniqueKeySettings,
        [FromServices] IValidator<StaffUserUpdateRequest> validator,
        CancellationToken cancellationToken)
    {
        if (!apiContext.IsStaff)
            return Results.Forbid();

        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return Results.ValidationProblem(validationResult.ToDictionary());

        var user = await userRepository.GetAsync(id, cancellationToken);
        if (user == null)
            return Results.NotFound();

        // Emails are stored and indexed lowercase, and a blank one means the user has none rather
        // than an empty address.
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim().ToLowerInvariant();
        var currentEmail = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email.Trim().ToLowerInvariant();
        var emailChanged = email != currentEmail;

        if (emailChanged && email != null && uniqueKeySettings.Value.EmailShouldBeUnique)
        {
            var owner = await uniqueKeyRepository.GetAsync(email, UniqueKeyType.Email, cancellationToken);
            if (owner != null && owner.UserId != user.Id)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    { "Email", new[] { "Email already exists" } }
                });
            }
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.UpdatedAt = DateTime.UtcNow;

        // A new address has not been confirmed by whoever owns it, whatever the old one's state was.
        if (emailChanged)
            user.EmailIsValid = false;

        await userRepository.SaveAsync(user, cancellationToken);

        if (emailChanged && uniqueKeySettings.Value.EmailShouldBeUnique)
            await MoveEmailKey(uniqueKeyRepository, user.Id, currentEmail, email, cancellationToken);

        return Results.Ok(user.ToDto());
    }

    /// <summary>
    /// Points the email index at the new address. Written after the user, so a failure here leaves
    /// an index entry to clean up rather than an address nobody can sign in with.
    /// </summary>
    private static async Task MoveEmailKey(IUniqueKeyRepository uniqueKeyRepository, string userId, string? oldEmail,
        string? email, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(oldEmail))
            await uniqueKeyRepository.DeleteAsync(oldEmail, UniqueKeyType.Email, cancellationToken);

        if (string.IsNullOrEmpty(email))
            return;

        await uniqueKeyRepository.SaveAsync(new UniqueKeyEntity
        {
            Value = email,
            CreatedAt = DateTime.UtcNow,
            UserId = userId,
            Type = UniqueKeyType.Email
        }, cancellationToken);
    }

    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("v1/staff/users/{id}", Handler)
            .Produces<UserDto>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithTags("Staff");
    }
}

public class StaffUserUpdateRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;

    /// <summary>Empty clears the address: some users have none, and staff must be able to say so.</summary>
    public string? Email { get; set; }

    public class StaffUserUpdateRequestValidator : AbstractValidator<StaffUserUpdateRequest>
    {
        public StaffUserUpdateRequestValidator()
        {
            RuleFor(request => request.FirstName).NotEmpty().MaximumLength(100);
            RuleFor(request => request.LastName).NotEmpty().MaximumLength(100);
            RuleFor(request => request.Email)
                .EmailAddress()
                .MaximumLength(200)
                .When(request => !string.IsNullOrWhiteSpace(request.Email));
        }
    }
}
