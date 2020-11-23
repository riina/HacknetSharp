namespace HacknetSharp.Events.Server
{
    [EventCommand(Command.SC_LoginFail)]
    public class LoginFailEvent : FailBaseServerEvent
    {
        public static readonly LoginFailEvent Singleton = new LoginFailEvent();

        public LoginFailEvent()
        {
            Message = "Login failed. Invalid credentials.";
        }
    }
}
