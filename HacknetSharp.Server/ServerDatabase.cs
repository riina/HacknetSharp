using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp.Server.Common;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents the backing database for a <see cref="Server"/> instance.
    /// </summary>
    public class ServerDatabase : IServerDatabase
    {
        private readonly AutoResetEvent _waitHandle;

        /// <summary>
        /// The storage context for this database.
        /// </summary>
        public ServerStorageContext Context { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ServerDatabase"/> with the provided <see cref="ServerStorageContext"/>.
        /// </summary>
        /// <param name="context">Storage context to wrap.</param>
        public ServerDatabase(ServerStorageContext context)
        {
            Context = context;
            _waitHandle = new AutoResetEvent(true);
        }

        /// <inheritdoc/>
#pragma warning disable 8609
        public async Task<TResult?> GetAsync<TKey, TResult>(TKey key)
            where TKey : IEquatable<TKey> where TResult : Model<TKey>
#pragma warning restore 8609
        {
            _waitHandle.WaitOne();
            try
            {
                return await Context.Set<TResult>().SingleOrDefaultAsync(r => r.Key.Equals(key)).Caf();
            }
            finally
            {
                _waitHandle.Set();
            }
        }

        /*public async Task<TResult> GetOrCreateAsync<TKey, TResult>(DbSet<TResult> set, TKey key,
            Func<TKey, TResult> creator) where TResult : ModelBase<TKey> where TKey : IEquatable<TKey>
        {
            _waitHandle.WaitOne();
            try
            {
                var res = await AsyncEnumerable.SingleOrDefaultAsync(set, r => r.Key.Equals(key)).Caf();
                if (res == null)
                {
                    res = creator(key);
                    Add(res);
                    await SyncAsync();
                }

                return res;
            }
            finally
            {
                _waitHandle.Set();
            }
        }*/

        /// <inheritdoc/>
        public async Task<List<TResult>> GetBulkAsync<TKey, TResult>(ICollection<TKey> keys)
            where TKey : IEquatable<TKey> where TResult : Model<TKey>
        {
            _waitHandle.WaitOne();
            try
            {
                return await Context.Set<TResult>().Where(u => keys.Contains(u.Key)).ToListAsync().Caf();
            }
            finally
            {
                _waitHandle.Set();
            }
        }

        /// <inheritdoc/>
        public void Add<TEntry>(TEntry entity) where TEntry : notnull
        {
            _waitHandle.WaitOne();
            try
            {
                Context.Add(entity);
            }
            finally
            {
                _waitHandle.Set();
            }
        }

        /// <inheritdoc/>
        public void AddBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull
        {
            _waitHandle.WaitOne();
            try
            {
                Context.AddRange((IEnumerable<object>)entities);
            }
            finally
            {
                _waitHandle.Set();
            }
        }

        /// <inheritdoc/>
        public void Edit<TEntry>(TEntry entity) where TEntry : notnull
        {
            _waitHandle.WaitOne();
            try
            {
                Context.Update(entity);
            }
            finally
            {
                _waitHandle.Set();
            }
        }

        /// <inheritdoc/>
        public void EditBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull
        {
            _waitHandle.WaitOne();
            try
            {
                Context.UpdateRange((IEnumerable<object>)entities);
            }
            finally
            {
                _waitHandle.Set();
            }
        }

        /// <inheritdoc/>
        public void Delete<TEntry>(TEntry entity) where TEntry : notnull
        {
            _waitHandle.WaitOne();
            try
            {
                Context.Remove(entity);
            }
            finally
            {
                _waitHandle.Set();
            }
        }


        /// <inheritdoc/>
        public void DeleteBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull
        {
            _waitHandle.WaitOne();
            try
            {
                Context.RemoveRange((IEnumerable<object>)entities);
            }
            finally
            {
                _waitHandle.Set();
            }
        }

        /// <inheritdoc/>
        public async Task SyncAsync()
        {
            _waitHandle.WaitOne();
            try
            {
                await Context.SaveChangesAsync().Caf();
            }
            finally
            {
                _waitHandle.Set();
            }
        }
    }
}
