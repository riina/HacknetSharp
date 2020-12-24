using System.Collections.Generic;
using System.Linq;
using System.Text;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.CorePrograms
{
    /// <inheritdoc />
    [ProgramInfo("core:map", "map", "map known systems",
        "list all known systems",
        "[filter]", false)]
    public class MapProgram : Program
    {
        /// <inheritdoc />
        public override IEnumerator<YieldToken?> Run()
        {
            if (!Login.Admin)
            {
                Write("Permission denied.").Flush();
                yield break;
            }

            IEnumerable<SystemModel> systems;
            if (Argv.Length != 1)
            {
                var filter = PathFilter.GenerateFilter(Argv.Skip(1));
                systems = System.KnownSystems.Select(s => s.To)
                    .Where(s => filter.Test(Util.UintToAddress(s.Address)) || filter.Test(s.Name));
            }
            else
                systems = System.KnownSystems.Select(s => s.To);

            var sb = new StringBuilder();
            foreach (var s in systems)
            {
                sb.Append($"{Util.UintToAddress(s.Address),16}")
                    .Append(' ')
                    .Append(s.Name)
                    .Append('\n');
            }

            Write(sb.ToString()).Flush();
        }
    }
}
