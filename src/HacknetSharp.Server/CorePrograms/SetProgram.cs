using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:set", "set", "set variable",
        "set environment variable for this shell and its processes",
        "[<name>[=[<value>]]]", true)]
    public class SetProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run(ProgramContext context) => InvokeStatic(context);

        private static readonly Regex _exportRegex = new Regex(@"([A-Za-z0-9]+)=([\S\s]*)");

        private static string SanitizeBody(string str)
        {
            str = str.Trim();
            if (str.Length > 1 && str.StartsWith('"') && str.EndsWith('"') && str[^2] != '\\')
                str = str[1..^1].Replace("\\\"", "\"");
            return str;
        }

        private static IEnumerator<YieldToken?> InvokeStatic(ProgramContext context)
        {
            var user = context.User;
            if (!user.Connected) yield break;
            var argv = context.Argv;
            var shell = context.Shell;
            var sb = new StringBuilder();
            if (argv.Length == 1)
            {
                foreach (var kvp in shell.Variables)
                {
                    sb.Append(kvp.Key).Append('=').Append(kvp.Value).Append('\n');
                }

                if (shell.Variables.Count == 0)
                    sb.Append('\n');

                user.WriteEventSafe(Output(sb.ToString()));
                user.FlushSafeAsync();
            }
            else
            {
                var match = _exportRegex.Match(argv[1]);
                if (match.Success)
                {
                    string key = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    if (value.Length == 0)
                        shell.Variables.Remove(key);
                    else
                        shell.Variables[key] = SanitizeBody(value);
                }
                else
                {
                    user.WriteEventSafe(Output("Invalid format, need [<name>[=[<value>]]]\n"));
                    user.FlushSafeAsync();
                }
            }
        }
    }
}
