using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HacknetSharp
{
    /// <summary>
    /// Represents an event listener that can be used to obtain / wait for received events.
    /// </summary>
    /// <typeparam name="TReceive"></typeparam>
    public interface IInboundConnection<TReceive> : IConnection where TReceive : Event
    {
        /// <summary>
        /// Waits for an event matching the specified predicate, removing it from the
        /// event processing queue and returning it.
        /// </summary>
        /// <param name="predicate">Predicate to match events.</param>
        /// <param name="pollMillis">Millisecond poll delay</param>
        /// <returns>The first matched event or null.</returns>
        Task<TReceive?> WaitForAsync(Func<TReceive, bool> predicate, int pollMillis);

        /// <summary>
        /// Waits for an event matching the specified predicate, removing it from the
        /// event processing queue and returning it.
        /// </summary>
        /// <param name="predicate">Predicate to match events.</param>
        /// <param name="pollMillis">Millisecond poll delay</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The first matched event or null.</returns>
        Task<TReceive?> WaitForAsync(Func<TReceive, bool> predicate, int pollMillis,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets available events from the input queue and clears input queue.
        /// </summary>
        /// <param name="output">Existing collection to add events to.</param>
        /// <returns>Events.</returns>
        public IEnumerable<TReceive> GetEvents(ICollection<TReceive>? output = null);

        /// <summary>
        /// Clears input queue.
        /// </summary>
        public void DiscardEvents();
    }
}
