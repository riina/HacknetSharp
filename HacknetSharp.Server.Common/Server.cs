namespace HacknetSharp.Server.Common
{
    public abstract class Server
    {
        public ServerDatabase Database { get; protected set; } = null!;
    }
}
