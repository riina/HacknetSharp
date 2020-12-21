using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:" + ServerConstants.ShellName, ServerConstants.ShellName, "start shell",
        "starts remote shell connected to host system",
        "", true)]
    public class ShellProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var chain = context.Person.ShellChain;
            int idx = chain.IndexOf(context.Shell);
            if (idx < 1) yield break; // Shell requires a host shell
            var hostShell = chain[idx - 1];
            if (hostShell.Remotes.ContainsKey(context.System.Address)) yield break; // Don't start duplicate shells
            var shell = context.World.StartShell(user, context.Person, context.Login,
                new StringBuilder().AppendJoin(' ', context.Argv.Skip(1).Prepend(ServerConstants.ShellName))
                    .ToString(), false);
            if (shell != null)
            {
                var proxy = context.World.StartProgram(hostShell,
                    $"{ServerConstants.ShellName} {Util.UintToAddress(context.System.Address)}",
                    ShellProxyProgram.Singleton);
                if (proxy != null)
                {
                    hostShell.Remotes[context.System.Address] = proxy;
                    proxy.ProgramContext.Remote = shell;
                    shell.RemoteParent = hostShell;
                    yield break;
                }

                user.WriteEventSafe(Output("Host process creation failed: out of memory on host\n"));
                hostShell.Remotes.Remove(context.System.Address);
                context.World.CompleteRecurse(shell, Process.CompletionKind.KillRemote);
            }
            else
                user.WriteEventSafe(Output("Process creation failed: out of memory\n"));

            user.FlushSafeAsync();
        }
    }

    [ProgramInfo("lucina is basically bae",
        "sometimes lucina is legitimately the best",
        "don't worry about it",
        "https://www.pixiv.net/en/artworks/76838039" +
        "https://www.pixiv.net/en/artworks/77110769" +
        "https://www.pixiv.net/en/artworks/75155688" +
        "https://www.pixiv.net/en/artworks/76326946"
        , "daijobu", false)]
    internal class ShellProxyProgram : Program
    {
        public static readonly ShellProxyProgram Singleton = new();

        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            while (context.Remote != null) yield return null;
        }

        public override bool OnShutdown(ProgramContext context)
        {
            var remote = context.Remote;
            // If this is a context controlling a remote shell, remove locally then terminate the remote
            if (remote != null)
            {
                // Remove ensures CompleteRecurse won't try to cancel this process again
                context.Shell.Remotes.Remove(remote.Context.System.Address);
                context.World.CompleteRecurse(remote, Process.CompletionKind.KillRemote);
            }

            return true;
        }
    }
}
