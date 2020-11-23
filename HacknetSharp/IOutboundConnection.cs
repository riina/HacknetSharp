using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HacknetSharp
{
    public interface IOutboundConnection<in TSend> : IConnection where TSend : Event
    {
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
