using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HacknetSharp.Server;

namespace HacknetSharp.Test.Server;

internal class SynchronousTestDatabase : IServerDatabase
{
    public Task<TResult?> GetAsync<TKey, TResult>(TKey key)
        where TKey : IEquatable<TKey> where TResult : Model<TKey> => throw new NotSupportedException();

    public Task<List<TResult>> GetBulkAsync<TKey, TResult>(ICollection<TKey> keys)
        where TKey : IEquatable<TKey> where TResult : Model<TKey> => throw new NotSupportedException();

    public Task<List<TResult>> WhereAsync<TResult>(Expression<Func<TResult, bool>> predicate)
        where TResult : class => throw new NotSupportedException();

    public void Add<TEntry>(TEntry entity) where TEntry : notnull
    {
    }

    public void AddBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull
    {
    }

    public void Update<TEntry>(TEntry entity) where TEntry : notnull
    {
    }

    public void UpdateBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull
    {
    }

    public void Delete<TEntry>(TEntry entity) where TEntry : notnull
    {
    }

    public void DeleteBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull
    {
    }

    public void Sync()
    {
    }

    public Task SyncAsync() => Task.CompletedTask;
}
