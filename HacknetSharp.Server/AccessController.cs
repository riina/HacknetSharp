namespace HacknetSharp.Server
{
    public abstract class AccessController
    {
        public abstract bool Authenticate(string user, string pass);
        public abstract void Register(string user, string pass, string adminUser);
        public abstract void Deregister(string user, string pass, string adminUser, bool purge);
    }
}
