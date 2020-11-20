using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    public class ServerDatabaseInstance : ServerDatabase
    {
        private readonly AutoResetEvent _waitHandle;

        /// <summary>
        /// The storage context for this database.
        /// </summary>
        public ServerStorageContext Context { get; }

        /// <summary>
        /// Creates a new instance of <see cref="ServerDatabaseInstance"/> with the provided <see cref="ServerStorageContext"/>.
        /// </summary>
        /// <param name="context">Storage context to wrap.</param>
        public ServerDatabaseInstance(ServerStorageContext context)
        {
            Context = context;
            _waitHandle = new AutoResetEvent(true);
        }

        /// <summary>
        /// Get an object from the database.
        /// </summary>
        /// <param name="key">Key to search for.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TResult">The object type.</typeparam>
        /// <returns>First located object with key or default for <typeparamref name="TResult"/></returns>
        public override async Task<TResult> GetAsync<TKey, TResult>(TKey key)
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

        /// <summary>
        /// Performs a bulk operation to find all objects that have keys matching <paramref name="keys"/>.
        /// </summary>
        /// <param name="keys">Keys to search for.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TResult">The object type.</typeparam>
        /// <returns>Objects with matching keys, not ordered nor one-to-one.</returns>
        public override async Task<List<TResult>> GetBulkAsync<TKey, TResult>(ICollection<TKey> keys)
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

        /// <summary>
        /// Adds an entity to the database.
        /// </summary>
        /// <param name="entity">Entity to add.</param>
        /// <typeparam name="TEntry">The entity type.</typeparam>
        public override void Add<TEntry>(TEntry entity)
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


        /// <summary>
        /// Adds a group of entities to the database.
        /// </summary>
        /// <param name="entities">Entities to add.</param>
        /// <typeparam name="TEntry">The entity type.</typeparam>
        public override void AddBulk<TEntry>(IEnumerable<TEntry> entities)
        {
            _waitHandle.WaitOne();
            try
            {
                Context.AddRange(entities.ToArray());
            }
            finally
            {
                _waitHandle.Set();
            }
        }

        /// <summary>
        /// Marks an entity as edited in the database.
        /// </summary>
        /// <param name="entity">Entity to mark.</param>
        /// <typeparam name="TEntry">The entity type.</typeparam>
        public override void Edit<TEntry>(TEntry entity)
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

        /// <summary>
        /// Marks a group of entities as edited in the database.
        /// </summary>
        /// <param name="entities">Entities to mark.</param>
        /// <typeparam name="TEntry">The entity type.</typeparam>
        public override void EditBulk<TEntry>(IEnumerable<TEntry> entities)
        {
            _waitHandle.WaitOne();
            try
            {
                Context.UpdateRange(entities.ToArray());
            }
            finally
            {
                _waitHandle.Set();
            }
        }


        /// <summary>
        /// Removes an entity from the database.
        /// </summary>
        /// <param name="entity">Entity to remove.</param>
        /// <typeparam name="TEntry">The entity type.</typeparam>
        public override void Delete<TEntry>(TEntry entity)
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


        /// <summary>
        /// Removes a group of entities from the database.
        /// </summary>
        /// <param name="entities">Entities to remove.</param>
        /// <typeparam name="TEntry">The entity type.</typeparam>
        public override void DeleteBulk<TEntry>(IEnumerable<TEntry> entities)
        {
            _waitHandle.WaitOne();
            try
            {
                Context.RemoveRange(entities.ToArray());
            }
            finally
            {
                _waitHandle.Set();
            }
        }

        /// <summary>
        /// Synchronizes local state with database.
        /// </summary>
        /// <returns>Task representing this operation.</returns>
        public override async Task SyncAsync()
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
