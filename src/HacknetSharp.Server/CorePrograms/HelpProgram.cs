using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HacknetSharp.Server.CorePrograms
{
    [ProgramInfo("core:help", "help", "show command listing",
        "show help information for all commands\nor details on specific command",
        "[command]", true)]
    public class HelpProgram : Program
    {
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var system = context.System;
            var argv = context.Argv;
            var login = context.Login;
            var world = context.World;

            if (system.TryGetWithAccess("/bin", login, out var result, out _))
            {
                var sb = new StringBuilder();
                if (argv.Length == 1)
                {
                    sb.Append("\n««  Intrinsic commands  »»\n\n");
                    foreach (var intrinsic in world.IntrinsicPrograms)
                        sb.Append($"{intrinsic.Item2.Name,-12} {intrinsic.Item2.Description}\n");
                    sb.Append("\n««  Programs  »»\n\n");
                    foreach (var program in system.Files.Where(f => f.Path == "/bin" && !f.Hidden && f.CanRead(login))
                        .OrderBy(f => f.Name))
                    {
                        var info = world.GetProgramInfo(program.Content);
                        if (info == null) continue;
                        sb.Append($"{info.Name,-12} {info.Description}\n");
                    }
                }
                else
                {
                    string name = argv[1];
                    var program = system.Files.FirstOrDefault(f => f.Path == "/bin" && f.Name == name && !f.Hidden);
                    if (program == null)
                    {
                        user.WriteEventSafe(Output("Unknown program\n"));
                        user.FlushSafeAsync();
                        yield break;
                    }

                    if (!program.CanRead(login))
                    {
                        user.WriteEventSafe(Output("Permission denied\n"));
                        user.FlushSafeAsync();
                        yield break;
                    }

                    var info = world.GetProgramInfo(program.Content);
                    if (info == null) yield break;

                    sb.Append("\n««  ").Append(info.Name).Append("  »»").Append("\n\n").Append(info.LongDescription)
                        .Append("\n\nUsage:\n\t").Append(name);
                    if (!string.IsNullOrWhiteSpace(info.Usage))
                        sb.Append(' ').Append(info.Usage);
                    sb.Append("\n\n");
                }

                user.WriteEventSafe(Output(sb.ToString()));
                user.FlushSafeAsync();
                yield break;
            }

            switch (result)
            {
                case ReadAccessResult.Readable:
                    break;
                case ReadAccessResult.NotReadable:
                    user.WriteEventSafe(Output("/bin: Permission denied\n"));
                    user.FlushSafeAsync();
                    yield break;
                case ReadAccessResult.NoExist:
                    user.WriteEventSafe(Output("/bin: No such file or directory\n"));
                    user.FlushSafeAsync();
                    yield break;
            }
        }
    }
}
