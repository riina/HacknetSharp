namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when access to a server resource is denied.
    /// </summary>
    [EventCommand(Command.SC_AccessFail)]
    public class AccessFailEvent : FailBaseServerEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="AccessFailEvent"/> with default message.
        /// </summary>
        public AccessFailEvent() => Message = "Access denied.";
    }
}
