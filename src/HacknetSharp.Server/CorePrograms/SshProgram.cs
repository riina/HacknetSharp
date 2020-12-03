using System.Collections.Generic;
using HacknetSharp.Server.Common;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:ssh", "connect to remote machine",
        "opens an authenticated connection\n" +
        "to a remote machine and opens a shell\n\n" +
        "ssh username@server")]
    public class SshProgram : Program
    {
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var argv = context.Argv;
            if (argv.Length == 1)
            {
                user.WriteEventSafe(Output("ssh: Needs connection target\n"));
                yield break;
            }

            if (!Util.TryParseConString(argv[1], 22, out string? name, out string? host, out ushort port,
                out string? error))
            {
                user.WriteEventSafe(Output($"ssh: {error}\n"));
                yield break;
            }

            user.WriteEventSafe(Output("Password:"));
            var input = Input(user, true);
            yield return input;
            // TODO test remote connection with credentials
            user.WriteEventSafe(Output("Connecting...\n"));
            user.FlushSafeAsync();
        }
    }
}
