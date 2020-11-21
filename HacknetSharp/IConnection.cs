using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HacknetSharp
{
    public interface IConnection<in TSend, TReceive> where TSend : Event where TReceive : Event
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
        /// Reads events in the processing queue, removing them as they are returned.
        /// </summary>
        /// <returns>Events.</returns>
        IEnumerable<TReceive> ReadEvents();

        /// <summary>
        /// Writes event to the underlying stream.
        /// </summary>
        /// <param name="evt">Event.</param>
        void WriteEvent(TSend evt);

        /// <summary>
        /// Writes events to the underlying stream.
        /// </summary>
        /// <param name="events">Events.</param>
        void WriteEvents(IEnumerable<TSend> events);

        Task FlushAsync();
        Task FlushAsync(CancellationToken cancellationToken);
    }
}
