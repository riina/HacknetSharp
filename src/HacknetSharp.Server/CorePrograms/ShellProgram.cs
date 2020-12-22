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
        public override IEnumerator<YieldToken?> Run()
        {
            var chain = Person.ShellChain;
            int idx = chain.IndexOf(Shell);
            var hostShell = idx < 1 ? Shell : chain[idx - 1]; // Use parent shell or self
            if (hostShell.Remotes.ContainsKey(System.Address)) yield break; // Don't start duplicate shells
            var shell = World.StartShell(User, Person, Login,
                new StringBuilder().AppendJoin(' ', Argv.Skip(1).Prepend(ServerConstants.ShellName))
                    .ToString(), false);
            if (shell != null)
            {
                var proxy = World.StartProgram(hostShell,
                    $"{ServerConstants.ShellName} {Util.UintToAddress(System.Address)}",
                    ShellProxyProgram.Singleton);
                if (proxy != null)
                {
                    hostShell.Remotes[System.Address] = proxy;
                    proxy.ProgramContext.Remote = shell;
                    shell.RemoteParent = hostShell;
                    yield break;
                }

                hostShell.Remotes.Remove(System.Address);
                Write(Output("Host process creation failed: out of memory on host\n")).Flush();
                World.CompleteRecurse(shell, Process.CompletionKind.KillRemote);
            }
            else
                Write(Output("Process creation failed: out of memory\n"));

            Flush();
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

        public override IEnumerator<YieldToken?> Run()
        {
            while (Remote != null) yield return null;
        }

        public override bool OnShutdown()
        {
            // If this is a context controlling a remote shell, remove locally then terminate the remote
            if (Remote != null)
            {
                // Remove ensures CompleteRecurse won't try to cancel this process again
                Shell.Remotes.Remove(Remote.ProcessContext.System.Address);
                World.CompleteRecurse(Remote, Process.CompletionKind.KillRemote);
            }

            return true;
        }
    }
}
