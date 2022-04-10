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
        public override IEnumerator<YieldToken?> Run()
        {
            var replacements = new Dictionary<string, string>();

            if (System.TryGetFile("/bin", Login, out var result, out var closestStr, out _))
            {
                var sb = new StringBuilder();
                if (Argv.Length == 1)
                {
                    sb.Append("\n««  Intrinsic commands  »»\n\n");
                    foreach (var intrinsic in World.IntrinsicPrograms)
                        sb.Append($"{intrinsic.Item2.Name,-12} {intrinsic.Item2.Description}\n");
                    sb.Append("\n««  Programs  »»\n\n");
                    foreach (var program in System.Files.Where(f => f.Path == "/bin" && !f.Hidden && f.CanRead(Login))
                                 .OrderBy(f => f.Name))
                    {
                        var info = World.GetProgramInfo(program.Content);
                        GenReplacements(replacements, program.Content!);
                        if (info == null) continue;
                        sb.Append($"{program.Name,-12} {info.Description.ApplyReplacements(replacements)}\n");
                    }
                }
                else
                {
                    string name = Argv[1];
                    ProgramInfoAttribute? info;
                    if (System.TryGetFile($"/bin/{name}", Login, out _, out _, out var program, true))
                    {
                        info = World.GetProgramInfo(program.Content);
                        GenReplacements(replacements, program.Content ?? name);
                    }
                    else
                    {
                        info = World.IntrinsicPrograms.Select(p => p.Item2).FirstOrDefault(p => p.Name == name);
                        if (info != null)
                            name = info.Name;
                    }

                    if (info == null)
                    {
                        Write("Unknown program\n");
                        yield break;
                    }

                    sb.Append("\n««  ").Append(name).Append("  »»").Append("\n\n")
                        .Append(info.LongDescription.ApplyReplacements(replacements))
                        .Append("\n\nUsage:\n\t").Append(name);
                    if (!string.IsNullOrWhiteSpace(info.Usage))
                        sb.Append(' ').Append(info.Usage.ApplyReplacements(replacements));
                    sb.Append("\n\n");
                }

                Write(sb.ToString());
                yield break;
            }

            switch (result)
            {
                case ReadAccessResult.NotReadable:
                    Write($"{closestStr}: Permission denied\n");
                    yield break;
                case ReadAccessResult.NoExist:
                    Write("/bin: No such file or directory\n");
                    yield break;
            }
        }

        private static void GenReplacements(Dictionary<string, string> replacements, string content)
        {
            replacements.Clear();
            string[] line = content.SplitCommandLine();
            for (int i = 0; i < line.Length; i++) replacements[$"HARG:{i}"] = line[i];
        }
    }
}
