using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Provides file-backed login management support functionality.
    /// </summary>
    public static class LoginManager
    {
        /// <summary>
        /// Adds a login to filesystem.
        /// </summary>
        /// <param name="world">World instance.</param>
        /// <param name="login">Local user login.</param>
        /// <param name="address">Target address.</param>
        /// <param name="name">Target username.</param>
        /// <param name="password">Target password.</param>
        /// <exception cref="IOException">Thrown when filesystem access error occurred.</exception>
        public static void AddLogin(IWorld world, LoginModel login, uint address, string name, string password)
        {
            var existingLogins = GetLogins(login, address);
            existingLogins[name] = new Login(address, name, password);
            string loginPath = GetLoginFile(login, address);
            string content = WriteContent(existingLogins);
            if (!login.System.TryGetFile(loginPath, login, out var result, out _, out var readable))
            {
                if (result == ReadAccessResult.NotReadable)
                    throw new IOException("Permission denied");
                world.Spawn.TextFile(login.System, login, loginPath, content);
            }
            else
            {
                readable.Content = content;
                world.Database.Update(readable);
            }
        }

        /// <summary>
        /// Gets a login in a filesystem.
        /// </summary>
        /// <param name="login">Local user login.</param>
        /// <param name="address">Target address.</param>
        /// <returns>Existing logins.</returns>
        /// <exception cref="IOException">Thrown when filesystem access error occurred.</exception>
        public static Dictionary<string, Login> GetLogins(LoginModel login, uint address)
        {
            string loginPath = GetLoginFile(login, address);
            if (!login.System.TryGetFile(loginPath, login, out var result, out _, out var readable))
            {
                if (result == ReadAccessResult.NotReadable)
                    throw new IOException("Permission denied");
                return new Dictionary<string, Login>();
            }

            return ParseContent(address, readable.Content ?? "");
        }

        /// <summary>
        /// Removes logins on a filesystem.
        /// </summary>
        /// <param name="world">World instance.</param>
        /// <param name="login">Local user login.</param>
        /// <param name="address">Address to remove logins for.</param>
        /// <param name="name">Login to remove.</param>
        /// <exception cref="IOException">Thrown when filesystem access error occurred.</exception>
        public static void RemoveLogin(IWorld world, LoginModel login, uint address, string? name = null)
        {
            string loginPath = GetLoginFile(login, address);
            if (!login.System.TryGetFile(loginPath, login, out var result, out _, out var readable))
            {
                if (result == ReadAccessResult.NotReadable)
                    throw new IOException("Permission denied");
                return;
            }

            if (name == null)
            {
                world.Spawn.RemoveFile(readable, login);
                return;
            }

            var logins = ParseContent(address, readable.Content ?? "");
            logins.Remove(name);
            readable.Content = WriteContent(logins);
            world.Database.Update(readable);
        }

        private static Dictionary<string, Login> ParseContent(uint address, string content)
        {
            var res = new Dictionary<string, Login>();
            foreach (var line in content.Split('\n'))
            {
                int splitIdx = line.IndexOf(':');
                if (splitIdx == -1) continue;
                string name = line[..splitIdx];
                string pass = line[(splitIdx + 1)..];
                res[name] = new Login(address, name, pass);
            }

            return res;
        }

        private static string WriteContent(Dictionary<string, Login> logins)
        {
            return new StringBuilder()
                .AppendJoin('\n', logins.Values.Select(l => $"{l.Name}: {l.Pass}")).ToString();
        }

        private static string GetLoginFile(LoginModel login, uint address) =>
            $"/login{login.User}/{Util.UintToAddress(address)}.login";

        /// <summary>
        /// Represents a login.
        /// </summary>
        public readonly struct Login
        {
            /// <summary>
            /// System address.
            /// </summary>
            public readonly uint Address;

            /// <summary>
            /// Login name.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Login password.
            /// </summary>
            public readonly string Pass;

            /// <summary>
            /// Creates a new instance of <see cref="Login"/>.
            /// </summary>
            /// <param name="address">System address.</param>
            /// <param name="name">Login name.</param>
            /// <param name="pass">Login password.</param>
            public Login(uint address, string name, string pass)
            {
                Address = address;
                Name = name;
                Pass = pass;
            }
        }
    }
}
