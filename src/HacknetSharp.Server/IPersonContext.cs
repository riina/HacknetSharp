using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using HacknetSharp.Events.Client;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents an active player or NPC that has a <see cref="PersonModel"/> for valid worlds and can receive events.
    /// </summary>
    public interface IPersonContext : IOutboundConnection<ServerEvent>
    {
        /// <summary>
        /// Local cache of client responses to operations by operation ID.
        /// </summary>
        ConcurrentDictionary<Guid, ClientResponseEvent> Responses { get; }

        /// <summary>
        /// Gets the entity's <see cref="PersonModel"/> for the specified world.
        /// </summary>
        /// <param name="world">Target world.</param>
        /// <returns>Person model.</returns>
        PersonModel GetPerson(IWorld world);

        /// <summary>
        /// Queue an event for send (or immediately process if applicable).
        /// </summary>
        /// <param name="evt">Event to send.</param>
        public void WriteEventSafe(ServerEvent evt);

        /// <summary>
        /// Flush all queued events to the underlying device.
        /// </summary>
        /// <returns>Task that represents this async operation.</returns>
        public Task FlushSafeAsync();
    }
}
