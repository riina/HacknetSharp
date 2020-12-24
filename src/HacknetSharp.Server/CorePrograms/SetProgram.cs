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
        private static readonly Regex _exportRegex = new(@"([A-Za-z0-9]+)=([\S\s]*)");

        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            var sb = new StringBuilder();
            if (Argv.Length == 1)
            {
                foreach (var kvp in Shell.GetVariables()) sb.Append(kvp.Key).Append('=').Append(kvp.Value).Append('\n');

                Write(sb.ToString()).Flush();
            }
            else
            {
                var match = _exportRegex.Match(Argv[1]);
                if (match.Success)
                {
                    string key = match.Groups[1].Value;
                    string value = match.Groups[2].Value;
                    if (value.Length == 0)
                        Shell.RemoveVariable(key);
                    else
                        Shell.SetVariable(key, SanitizeBody(value));
                }
                else
                {
                    Write("Invalid format, need [<name>[=[<value>]]]\n").Flush();
                }
            }

            yield break;
        }

        private static string SanitizeBody(string str)
        {
            str = str.Trim();
            if (str.Length > 1 && str.StartsWith('"') && str.EndsWith('"') && str[^2] != '\\')
                str = str[1..^1].Replace("\\\"", "\"");
            return str;
        }
    }
}
