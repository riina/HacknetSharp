using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:help", "help", "show command listing",
        "show help information for all commands\nor details on specific command",
        "[command]", true)]
    public class HelpProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var system = context.System;
            string[] argv = context.Argv;
            var login = context.Login;
            var world = context.World;
            var replacements = new Dictionary<string, string>();

            if (system.TryGetFile("/bin", login, out var result, out var closestStr, out _))
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
                        GenReplacements(replacements, program.Content!);
                        if (info == null) continue;
                        sb.Append($"{program.Name,-12} {info.Description.ApplyReplacements(replacements)}\n");
                    }
                }
                else
                {
                    string name = argv[1];
                    ProgramInfoAttribute? info;
                    if (system.TryGetFile($"/bin/{name}", login, out _, out _, out var program, true))
                    {
                        info = world.GetProgramInfo(program.Content);
                        GenReplacements(replacements, program.Content ?? name);
                    }
                    else
                    {
                        info = world.IntrinsicPrograms.Select(p => p.Item2).FirstOrDefault(p => p.Name == name);
                        if (info != null)
                            name = info.Name;
                    }

                    if (info == null)
                    {
                        user.WriteEventSafe(Output("Unknown program\n"));
                        user.FlushSafeAsync();
                        yield break;
                    }

                    sb.Append("\n««  ").Append(name).Append("  »»").Append("\n\n")
                        .Append(info.LongDescription.ApplyReplacements(replacements))
                        .Append("\n\nUsage:\n\t").Append(name);
                    if (!string.IsNullOrWhiteSpace(info.Usage))
                        sb.Append(' ').Append(info.Usage.ApplyReplacements(replacements));
                    sb.Append("\n\n");
                }

                user.WriteEventSafe(Output(sb.ToString()));
                user.FlushSafeAsync();
                yield break;
            }

            switch (result)
            {
                case ReadAccessResult.NotReadable:
                    user.WriteEventSafe(Output($"{closestStr}: Permission denied\n"));
                    user.FlushSafeAsync();
                    yield break;
                case ReadAccessResult.NoExist:
                    user.WriteEventSafe(Output("/bin: No such file or directory\n"));
                    user.FlushSafeAsync();
                    yield break;
            }
        }

        private static void GenReplacements(Dictionary<string, string> replacements, string content)
        {
            replacements.Clear();
            string[] line = ServerUtil.SplitCommandLine(content);
            for (int i = 0; i < line.Length; i++) replacements[$"HARG:{i}"] = line[i];
        }
    }
}
