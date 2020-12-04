using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents the backing database for a server instance.
    /// </summary>
    public interface IServerDatabase
    {
        /// <summary>
        /// Get an object from the database.
        /// </summary>
        /// <param name="key">Key to search for.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TResult">The object type.</typeparam>
        /// <returns>First located object with key or default for <typeparamref name="TResult"/></returns>
        Task<TResult?> GetAsync<TKey, TResult>(TKey key)
            where TResult : Model<TKey> where TKey : IEquatable<TKey>;

        /// <summary>
        /// Performs a bulk operation to find all objects that have keys matching <paramref name="keys"/>.
        /// </summary>
        /// <param name="keys">Keys to search for.</param>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TResult">The object type.</typeparam>
        /// <returns>Objects with matching keys, not ordered nor one-to-one.</returns>
        Task<List<TResult>> GetBulkAsync<TKey, TResult>(ICollection<TKey> keys)
            where TResult : Model<TKey> where TKey : IEquatable<TKey>;

        /// <summary>
        /// Adds an entity to the database.
        /// </summary>
        /// <param name="entity">Entity to add.</param>
        /// <typeparam name="TEntry">The entity type.</typeparam>
        void Add<TEntry>(TEntry entity) where TEntry : notnull;


        /// <summary>
        /// Adds a group of entities to the database.
        /// </summary>
        /// <param name="entities">Entities to add.</param>
        /// <typeparam name="TEntry">The entity type.</typeparam>
        void AddBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull;

        /// <summary>
        /// Marks an entity as edited in the database.
        /// </summary>
        /// <param name="entity">Entity to mark.</param>
        /// <typeparam name="TEntry">The entity type.</typeparam>
        void Edit<TEntry>(TEntry entity) where TEntry : notnull;

        /// <summary>
        /// Marks a group of entities as edited in the database.
        /// </summary>
        /// <param name="entities">Entities to mark.</param>
        /// <typeparam name="TEntry">The entity type.</typeparam>
        void EditBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull;

        /// <summary>
        /// Removes an entity from the database.
        /// </summary>
        /// <param name="entity">Entity to remove.</param>
        /// <typeparam name="TEntry">The entity type.</typeparam>
        void Delete<TEntry>(TEntry entity) where TEntry : notnull;

        /// <summary>
        /// Removes a group of entities from the database.
        /// </summary>
        /// <param name="entities">Entities to remove.</param>
        /// <typeparam name="TEntry">The entity type.</typeparam>
        void DeleteBulk<TEntry>(IEnumerable<TEntry> entities) where TEntry : notnull;

        /// <summary>
        /// Synchronizes local state with database.
        /// </summary>
        /// <returns>Task representing this operation.</returns>
        Task SyncAsync();
    }
}
