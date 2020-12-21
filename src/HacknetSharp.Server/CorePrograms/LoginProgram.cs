using System;
using System.Collections.Generic;
using System.IO;
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
        "-l [name@server]: list passwords for specified target (or\n" +
        "$NAME/$TARGET)\n\n" +
        "-d [name@server]: delete password for specified target (or\n" +
        "$NAME/$TARGET)",
        "[-sdl] [name@host]", false)]
    public class LoginProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private const string AutoLoginName = "$AUTO_NAME";

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var (flags, _, pargs) =
                ServerUtil.IsolateFlags(new ArraySegment<string>(context.Argv, 1, context.Argv.Length - 1));
            if (!ServerUtil.TryParseConString(pargs.Count == 0 ? null : pargs[0], 22, out string? name,
                out string? host, out _, out string? error,
                context.Shell.TryGetVariable("NAME", out string? shellUser) ? shellUser : AutoLoginName,
                context.Shell.TryGetVariable("TARGET", out string? shellTarget) ? shellTarget : null))
            {
                if (pargs.Count == 1)
                {
                    user.WriteEventSafe(Output("Needs connection target\n"));
                    user.FlushSafeAsync();
                }
                else
                {
                    user.WriteEventSafe(Output($"{error}\n"));
                    user.FlushSafeAsync();
                }

                yield break;
            }

            if (!IPAddressRange.TryParse(host, false, out var range) ||
                !range.TryGetIPv4HostAndSubnetMask(out uint hostUint, out _))
            {
                user.WriteEventSafe(Output($"Invalid host {host}\n"));
                user.FlushSafeAsync();
                yield break;
            }

            var world = context.World;
            var login = context.Login;

            if (flags.Contains("s"))
            {
                if (name == AutoLoginName)
                {
                    user.WriteEventSafe(Output("login: Login name not specified\n"));
                    user.FlushSafeAsync();
                    yield break;
                }

                string password;
                if (context.Shell.TryGetVariable("PASS", out string? shellPass))
                    password = shellPass;
                else
                {
                    user.WriteEventSafe(Output("Password:"));
                    var input = Input(user, true);
                    yield return input;
                    password = input.Input!.Input;
                }

                try
                {
                    LoginManager.AddLogin(world, login, hostUint, name, password);
                }
                catch (IOException e)
                {
                    user.WriteEventSafe(Output($"{e.Message}\n"));
                    user.FlushSafeAsync();
                }
            }
            else if (flags.Contains("l"))
            {
                try
                {
                    var sb = new StringBuilder();
                    foreach (var entry in LoginManager.GetLogins(login, hostUint).Values)
                    {
                        if (name == AutoLoginName || name == entry.Name)
                            sb.Append($"{entry.Name}: {entry.Pass}\n");
                    }

                    user.WriteEventSafe(Output($"{sb}\n"));
                    user.FlushSafeAsync();
                }
                catch (IOException e)
                {
                    user.WriteEventSafe(Output($"{e.Message}\n"));
                    user.FlushSafeAsync();
                }
            }
            else if (flags.Contains("d"))
            {
                user.WriteEventSafe(
                    Output($"Are you sure you want to delete logins for {Util.UintToAddress(hostUint)}?\n"));
                var confirm = Confirm(user, true);
                yield return confirm;
                if (!confirm.Confirmed) yield break;

                try
                {
                    LoginManager.RemoveLogin(world, login, hostUint, name == AutoLoginName ? null : name);
                }
                catch (IOException e)
                {
                    user.WriteEventSafe(Output($"{e.Message}\n"));
                    user.FlushSafeAsync();
                }
            }
            else
            {
                if (name == AutoLoginName)
                {
                    user.WriteEventSafe(Output("login: Login name not specified\n"));
                    user.FlushSafeAsync();
                    yield break;
                }

                string? password = null;
                if (context.Shell.TryGetVariable("PASS", out string? shellPass))
                    password = shellPass;
                else
                {
                    try
                    {
                        if (LoginManager.GetLogins(login, hostUint).TryGetValue(name, out var storedLogin))
                            password = storedLogin.Pass;
                    }
                    catch (IOException e)
                    {
                        user.WriteEventSafe(Output($"{e.Message}\n"));
                        user.FlushSafeAsync();
                        yield break;
                    }

                    if (password == null)
                    {
                        user.WriteEventSafe(Output("Password:"));
                        var input = Input(user, true);
                        yield return input;
                        password = input.Input!.Input;
                    }
                }

                Connect(context, hostUint, name, password);
            }
        }
    }
}
