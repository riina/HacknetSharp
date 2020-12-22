using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:login", "login", "login / manage logins",
        "login to system using credentials or\nmanage logins.\n\n" +
        "[name@server]: login to specified target (or\n" +
        "$NAME/$TARGET or first stored login for $TARGET)\n" +
        "using stored credentials\n" +
        "(or $PASS or prompted password)\n\n" +
        "-s [name@server]: save password for specified target (or\n" +
        "$NAME/$TARGET) from $PASS or prompted password\n\n" +
        "-l [server]: list passwords for specified target (or\n" +
        "$TARGET)\n\n" +
        "-d [name@server]: delete password for specified target (or\n" +
        "$NAME/$TARGET)",
        "[-sdl] [name@host]", false)]
    public class LoginProgram : Program
    {
        private const string AutoLoginName = "$AUTO_NAME";

        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            var (flags, _, pargs) = IsolateArgvFlags(Argv);
            if (!ServerUtil.TryParseConString(pargs.Count == 0 ? null : pargs[0], 22, out string? name,
                out string? host, out _, out string? error,
                Shell.TryGetVariable("NAME", out string? shellUser) ? shellUser : AutoLoginName,
                Shell.TryGetVariable("TARGET", out string? shellTarget) ? shellTarget : null))
            {
                if (pargs.Count == 1)
                    Write(Output("Needs connection target\n")).Flush();
                else
                    Write(Output($"{error}\n")).Flush();

                yield break;
            }

            if (!IPAddressRange.TryParse(host, false, out var range) ||
                !range.TryGetIPv4HostAndSubnetMask(out uint hostUint, out _))
            {
                Write(Output($"Invalid host {host}\n")).Flush();
                yield break;
            }

            if (flags.Contains("s"))
            {
                if (name == AutoLoginName)
                {
                    Write(Output("Login name not specified\n")).Flush();
                    yield break;
                }

                string password;
                if (Shell.TryGetVariable("PASS", out string? shellPass))
                    password = shellPass;
                else
                {
                    Write(Output("Password:"));
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
                    Write(Output($"{e.Message}\n")).Flush();
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

                    Write(Output(sb.ToString())).Flush();
                }
                catch (IOException e)
                {
                    Write(Output($"{e.Message}\n")).Flush();
                }
            }
            else if (flags.Contains("d"))
            {
                Write(Output($"Are you sure you want to delete logins for {Util.UintToAddress(hostUint)}?\n"));
                var confirm = Confirm(false);
                yield return confirm;
                if (!confirm.Confirmed) yield break;

                try
                {
                    LoginManager.RemoveLogin(World, Login, hostUint, name == AutoLoginName ? null : name);
                }
                catch (IOException e)
                {
                    Write(Output($"{e.Message}\n")).Flush();
                }
            }
            else
            {
                string? password = null;
                try
                {
                    if (name == AutoLoginName)
                    {
                        var logins = LoginManager.GetLogins(Login, hostUint);
                        if (logins.Count == 0)
                        {
                            Write(Output($"No known logins for {Util.UintToAddress(hostUint)}\n")).Flush();
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
                    Write(Output($"{e.Message}\n")).Flush();
                    yield break;
                }

                // Fallback to asking for password
                if (password == null)
                {
                    Write(Output("Password:"));
                    var input = Input(true);
                    yield return input;
                    password = input.Input!.Input;
                }

                Connect(hostUint, name, password);
            }
        }
    }
}
