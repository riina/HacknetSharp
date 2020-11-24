namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_LoginFail)]
    public class LoginFailEvent : FailBaseServerEvent
    {
        public LoginFailEvent() => Message = "Login failed. Invalid credentials.";
    }
}
