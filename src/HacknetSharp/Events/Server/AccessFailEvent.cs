namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_AccessFail)]
    public class AccessFailEvent : FailBaseServerEvent
    {
        public AccessFailEvent() => Message = "Access denied.";
    }
}
