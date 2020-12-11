using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a default backing database for a server instance.
    /// </summary>
    /// <remarks>
    /// This class mainly exists to ensure no parallel database access occurs.
    /// </remarks>
    public class ServerDatabase : IServerDatabase
    {
        private readonly AutoResetEvent _waitHandle;

        /// <summary>
        /// The storage context for this database.
        /// </summary>
        private readonly ServerDatabaseContext _context;

        private readonly HashSet<object> _added;
        private readonly HashSet<object> _updated;
        private readonly HashSet<object> _removed;

        /// <summary>
        /// Creates a new instance of <see cref="ServerDatabase"/> with the provided <see cref="ServerDatabaseContext"/>.
        /// </summary>
        /// <param name="context">Storage context to wrap.</param>
        public ServerDatabase(ServerDatabaseContext context)
        {
            _context = context;
            _waitHandle = new AutoResetEvent(true);
            _added = new HashSet<object>();
            _updated = new HashSet<object>();
            _removed = new HashSet<object>();
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
                return await _context.Set<TResult>().FindAsync(key).Caf();
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
                var res = await Context.Set<TResult>().FindAsync(key).Caf();
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
                return await _context.Set<TResult>().Where(u => keys.Contains(u.Key)).ToListAsync().Caf();
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
                _added.Add(entity);
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
                _added.UnionWith((IEnumerable<object>)entities);
            }
            finally
            {
                _waitHandle.Set();
            }
        }

        /// <inheritdoc/>
        public void Update<TEntry>(TEntry entity) where TEntry : notnull
        {
            _waitHandle.WaitOne();
            try
            {
                _updated.Add(entity);
            }
            finally
            {
                _waitHandle.Set();
            }
        }

        /// <inheritdoc/>
        public void UpdateBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull
        {
            _waitHandle.WaitOne();
            try
            {
                _updated.UnionWith((IEnumerable<object>)entities);
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
                _removed.Add(entity);
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
                _removed.UnionWith((IEnumerable<object>)entities);
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
                _context.AddRange(_added.Except(_removed));
                _context.UpdateRange(_updated.Except(_added).Except(_removed));
                _context.RemoveRange(_removed.Except(_added));
                await _context.SaveChangesAsync().Caf();
                _added.Clear();
                _updated.Clear();
                _removed.Clear();
            }
            finally
            {
                _waitHandle.Set();
            }
        }
    }
}
