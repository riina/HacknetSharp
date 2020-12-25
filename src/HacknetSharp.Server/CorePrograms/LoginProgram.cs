using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:login", "login", "login / manage logins",
        "login to system using credentials or\nmanage logins.\n\n" +
        "[name[@server]]: login with name (or $NAME, or\n" +
        "first stored login for target) using stored credentials\n" +
        "(or $PASS or prompted password)\n\n" +
        "-s [name[@server]]: save password for name (or $NAME)\n" +
        "from $PASS or prompted password\n\n" +
        "-l [server]: list passwords for server\n\n" +
        "-d [name[@server]]: delete password for name\n\n" +
        "If server isn't specified, connected server is used.",
        "[-sdl] [name[@server]]", false)]
    public class LoginProgram : Program
    {
        private const string AutoLoginName = "$AUTO_NAME";
        private const string AutoLoginHost = "$AUTO_HOST";

        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            var (flags, _, pargs) = IsolateArgvFlags(Argv);
            if (!ServerUtil.TryParseConString(pargs.Count == 0 ? null : pargs[0], 22, out string? name,
                out string? host, out _, out string? error, AutoLoginName, AutoLoginHost))
            {
                if (pargs.Count == 1)
                    Write("Needs connection target\n");
                else
                    Write($"{error}\n");

                yield break;
            }

            uint hostUint;
            if (host == AutoLoginHost)
            {
                if (Shell.Target != null)
                    hostUint = Shell.Target.Address;
                else
                {
                    Write("No server specified, and not currently connected to a server\n");
                    yield break;
                }
            }
            else if (!IPAddressRange.TryParse(host, false, out var range) ||
                     !range.TryGetIPv4HostAndSubnetMask(out hostUint, out _))
            {
                Write($"Invalid host {host}\n");
                yield break;
            }

            if (flags.Contains("s"))
            {
                if (name == AutoLoginName && !Shell.TryGetVariable("NAME", out name))
                {
                    Write("Login name not specified\n");
                    yield break;
                }

                string password;
                if (Shell.TryGetVariable("PASS", out string? shellPass))
                    password = shellPass;
                else
                {
                    Write("Password:");
                    var input = Input(true);
                    yield return input;
                    password = input.Input!.Input;
                }

                try
                {
                    LoginManager.AddLogin(World, Login, hostUint, name, password);
                }
                catch (IOException e)
                {
                    Write($"{e.Message}\n");
                }
            }
            else if (flags.Contains("l"))
            {
                try
                {
                    var sb = new StringBuilder($"Logins for {Util.UintToAddress(hostUint)}:\n");
                    foreach (var entry in LoginManager.GetLogins(Login, hostUint).Values)
                    {
                        if (name == AutoLoginName || name == entry.Name)
                            sb.Append($"{entry.Name}: {entry.Pass}\n");
                    }

                    if (sb.Length == 0) sb.Append('\n');

                    Write(sb.ToString());
                }
                catch (IOException e)
                {
                    Write($"{e.Message}\n");
                }
            }
            else if (flags.Contains("d"))
            {
                if (name == AutoLoginName)
                {
                    Write("Login name not specified\n");
                    yield break;
                }

                Write($"Are you sure you want to delete logins for {Util.UintToAddress(hostUint)}?\n");
                var confirm = Confirm(false);
                yield return confirm;
                if (!confirm.Confirmed) yield break;

                try
                {
                    LoginManager.RemoveLogin(World, Login, hostUint, name == AutoLoginName ? null : name);
                }
                catch (IOException e)
                {
                    Write($"{e.Message}\n");
                }
            }
            else
            {
                string? password = null;
                try
                {
                    if (name == AutoLoginName && Shell.TryGetVariable("NAME", out string? shellName))
                        name = shellName;
                    if (name == AutoLoginName)
                    {
                        var logins = LoginManager.GetLogins(Login, hostUint);
                        if (logins.Count == 0)
                        {
                            Write($"No known logins for {Util.UintToAddress(hostUint)}\n");
                            yield break;
                        }

                        var id = logins.Values.First();
                        name = id.Name;
                        password = id.Pass;
                    }
                    else if (Shell.TryGetVariable("PASS", out string? shellPass))
                        password = shellPass;
                    else if (LoginManager.GetLogins(Login, hostUint).TryGetValue(name, out var storedLogin))
                        password = storedLogin.Pass;
                }
                catch (IOException e)
                {
                    Write($"{e.Message}\n");
                    yield break;
                }

                // Fallback to asking for password
                if (password == null)
                {
                    Write("Password:");
                    var input = Input(true);
                    yield return input;
                    password = input.Input!.Input;
                }

                Connect(hostUint, name, password);
            }
        }
    }
}
