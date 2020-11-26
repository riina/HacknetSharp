using System;

namespace HacknetSharp.Client
{
    public class LoginException : Exception
    {
        public LoginException(string message) : base(message)
        {
        }
    }
}
