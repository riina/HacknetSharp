using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HacknetSharp;
using HacknetSharp.Server;
using Microsoft.EntityFrameworkCore;

namespace hss.Core
{
    /// <summary>
    /// Represents the backing database for a <see cref="Server"/> instance.
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
        private readonly ServerStorageContext _context;

        /// <summary>
        /// Creates a new instance of <see cref="ServerDatabase"/> with the provided <see cref="ServerStorageContext"/>.
        /// </summary>
        /// <param name="context">Storage context to wrap.</param>
        public ServerDatabase(ServerStorageContext context)
        {
            _context = context;
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
                _context.Add(entity);
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
                _context.AddRange((IEnumerable<object>)entities);
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
                _context.Update(entity);
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
                _context.UpdateRange((IEnumerable<object>)entities);
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
                _context.Remove(entity);
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
                _context.RemoveRange((IEnumerable<object>)entities);
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
                await _context.SaveChangesAsync().Caf();
            }
            finally
            {
                _waitHandle.Set();
            }
        }
    }
}
