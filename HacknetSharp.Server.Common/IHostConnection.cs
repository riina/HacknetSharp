using System;
using System.Collections.Generic;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public interface IHostConnection : IOutboundConnection<ServerEvent>
    {
        Dictionary<Guid, PlayerModel> PlayerModels { get; }
    }
}
