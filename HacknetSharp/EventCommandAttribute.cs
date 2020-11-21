using System;

namespace HacknetSharp
{
    /// <summary>
    /// Specifies the network code for an event.
    /// </summary>
    public class EventCommandAttribute : Attribute
    {
        /// <summary>
        /// Network code for events of this type.
        /// </summary>
        public Command Command { get; set; }

        /// <summary>
        /// Specifies the network code for an event.
        /// </summary>
        /// <param name="command">Network code for events of this type.</param>
        public EventCommandAttribute(Command command)
        {
            Command = command;
        }
    }
}
