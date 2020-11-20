using System.Threading.Tasks;

namespace HacknetSharp.Server
{
    public abstract class AccessController
    {
        public Common.Server Server { get; set; } = null!;
        public abstract Task<bool> AuthenticateAsync(string user, string pass);
        public abstract Task<bool> RegisterAsync(string user, string pass, string registrationToken);
        public abstract Task<bool> ChangePasswordAsync(string user, string pass, string newPass);
        public abstract Task<bool> AdminChangePasswordAsync(string user, string newPass);
        public abstract Task<bool> DeregisterAsync(string user, string pass, bool purge);
    }
}
