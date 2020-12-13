namespace HacknetSharp.Events.Server
{
    /// <summary>
    /// Event sent when server has failed to authenticate a user.
    /// </summary>
    [EventCommand(Command.SC_LoginFail)]
    public class LoginFailEvent : FailBaseServerEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="LoginFailEvent"/> with the default message.
        /// </summary>
        public LoginFailEvent() => Message = "Login failed. Invalid credentials.";
    }
}
