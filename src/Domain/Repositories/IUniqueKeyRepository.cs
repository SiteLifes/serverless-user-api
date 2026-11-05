using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

public interface IUniqueKeyRepository
{
    Task<UniqueKeyEntity?> GetAsync(string key, UniqueKeyType keyType, CancellationToken cancellationToken = default);

    /// <summary>Birden çok anahtarı tek seferde (100'lük toplu okumalarla) getirir; bulunmayanlar listede yok.</summary>
    Task<List<UniqueKeyEntity>> GetManyAsync(IReadOnlyCollection<string> keys, UniqueKeyType keyType, CancellationToken cancellationToken = default);

    Task<bool> SaveAsync(UniqueKeyEntity entity, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string key, UniqueKeyType keyType, CancellationToken cancellationToken = default);
    Task<string> FormatMobilePhone(string phone, CancellationToken cancellationToken = default);

}