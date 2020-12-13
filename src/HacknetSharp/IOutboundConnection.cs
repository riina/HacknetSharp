using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HacknetSharp
{
    /// <summary>
    /// Represents an event sender that can be used to send events.
    /// </summary>
    /// <typeparam name="TSend"></typeparam>
    public interface IOutboundConnection<in TSend> : IConnection where TSend : Event
    {
        /// <summary>
        /// Queues event for send.
        /// </summary>
        /// <param name="evt">Event.</param>
        void WriteEvent(TSend evt);

        /// <summary>
        /// Queues events for send.
        /// </summary>
        /// <param name="events">Events.</param>
        void WriteEvents(IEnumerable<TSend> events);

        /// <summary>
        /// Flushes pending events to underlying output device.
        /// </summary>
        /// <returns>Task for this async operation.</returns>
        Task FlushAsync();

        /// <summary>
        /// Flushes pending events to underlying output device.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task for this async operation.</returns>
        Task FlushAsync(CancellationToken cancellationToken);
    }
}
