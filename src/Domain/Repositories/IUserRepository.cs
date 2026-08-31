using Domain.Entities;

namespace Domain.Repositories;

public interface IUserRepository
{
    Task<UserEntity?> GetAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> SaveAsync(UserEntity entity, CancellationToken cancellationToken = default);
    Task<(IList<UserEntity> users, string nextToken)> GetPagedAsync(int limit, string? nextToken, CancellationToken cancellationToken);

    /// <summary>
    /// Users whose name matches every word of <paramref name="term"/>.
    ///
    /// <paramref name="afterSk"/> resumes a search after a user id; the returned id is where the
    /// next call should carry on, and null means there is nothing left to look through.
    /// </summary>
    Task<(IList<UserEntity> users, string? nextSk)> SearchByNameAsync(string term, int limit, string? afterSk, CancellationToken cancellationToken);

    Task<IList<UserEntity>> GetUsersAsync(IList<string> userIds, CancellationToken cancellationToken);
    Task<IEnumerable<UserEntity>> GetAllAsync(CancellationToken cancellationToken);
}