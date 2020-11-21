using System;
using System.Collections.Generic;

namespace HacknetSharp
{
    public interface IConnection<in TSend, out TReceive> where TSend : Event where TReceive : Event
    {
        /// <summary>
        /// Waits for an event matching the specified predicate, removing it from the
        /// event processing queue and returning it.
        /// </summary>
        /// <param name="predicate">Predicate to match events.</param>
        /// <returns>The first matched event or null.</returns>
        TReceive WaitFor(Predicate<TReceive> predicate);

        /// <summary>
        /// Reads events in the processing queue, removing them as they are returned.
        /// </summary>
        /// <returns>Events.</returns>
        IEnumerable<TReceive> ReadEvents();

        /// <summary>
        /// Writes events to the underlying stream.
        /// </summary>
        /// <param name="events">Events.</param>
        void WriteEvents(IEnumerable<TSend> events);
    }
}
