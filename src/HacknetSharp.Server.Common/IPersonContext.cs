using System.Threading.Tasks;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public interface IPersonContext : IOutboundConnection<ServerEvent>
    {
        PersonModel GetPerson(IWorld world);

        public void WriteEventSafe(ServerEvent evt);
        public Task FlushSafeAsync();
    }
}
