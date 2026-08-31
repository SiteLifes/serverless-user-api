using Amazon.DynamoDBv2;
using Domain.Entities;
using Domain.Extensions;
using Domain.Repositories;
using Domain.Services;
using Infrastructure.Repositories.Base;

namespace Infrastructure.Repositories;

public class UserRepository : DynamoRepository, IUserRepository
{
    /// <summary>How much of the partition one name search reads before handing back a cursor.</summary>
    private const int SearchPageSize = 200;

    private const int MaxSearchPages = 10;

    private readonly IEventBusManager _eventBusManager;

    public UserRepository(IAmazonDynamoDB dynamoDb, IEventBusManager eventBusManager) : base(dynamoDb)
    {
        _eventBusManager = eventBusManager;
    }

    protected override string GetTableName()
    {
        return GetEnvironmentTableName("users");
    }

    public async Task<UserEntity?> GetAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await base.GetAsync<UserEntity>("users", userId, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var response = await base.DeleteAsync("users", userId, cancellationToken);
        return response;
    }

    public async Task<bool> SaveAsync(UserEntity entity, CancellationToken cancellationToken = default)
    {
        var response = await base.SaveAsync(entity, cancellationToken);
        await _eventBusManager.UserModifiedAsync(entity, cancellationToken);
        return response;
    }

    public async Task<(IList<UserEntity> users, string nextToken)> GetPagedAsync(int limit, string? nextToken, CancellationToken cancellationToken)
    {
        var (users, token, _) = await GetPagedAsync<UserEntity>($"users", nextToken, limit, cancellationToken);
        return (users, token);
    }

    /// <summary>
    /// Name search over the user partition.
    ///
    /// There is no index on names — every user sits in one partition keyed by id — so the matching
    /// happens here, over pages of that partition. DynamoDB's own `contains` filter would save the
    /// transfer but not the read, and it compares bytes: it misses "Şefer" for "sefer", which is
    /// most of what gets typed. The scan is capped per request, and whoever asked gets a cursor to
    /// carry on with rather than a request that runs until it times out.
    /// </summary>
    public async Task<(IList<UserEntity> users, string? nextSk)> SearchByNameAsync(string term, int limit, string? afterSk, CancellationToken cancellationToken)
    {
        var terms = term.ToSearchTerms();
        var matches = new List<UserEntity>();

        if (terms.Count == 0)
            return (matches, null);

        var cursor = afterSk;

        for (var page = 0; page < MaxSearchPages; page++)
        {
            var (users, lastSk) = await QueryPageAsync<UserEntity>("users", cursor, SearchPageSize, cancellationToken);

            foreach (var user in users)
            {
                if (!$"{user.FirstName} {user.LastName}".MatchesSearchTerms(terms))
                    continue;

                matches.Add(user);

                // The page is abandoned mid-way, so the cursor is the user just handed out rather
                // than the end of the page: the rest of it is still unread.
                if (matches.Count >= limit)
                    return (matches, user.Id);
            }

            cursor = lastSk;

            if (cursor == null)
                return (matches, null);
        }

        return (matches, cursor);
    }

    public async Task<IList<UserEntity>> GetUsersAsync(IList<string> userIds, CancellationToken cancellationToken)
    {
        return await BatchGetAsync(userIds.Select(q => new UserEntity
        {
            Id = q
        }).ToList(), cancellationToken);
    }

    public async Task<IEnumerable<UserEntity>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await GetAllAsync<UserEntity>("users", cancellationToken);
    }
}