using System;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public interface IPlayerContext : IOutboundConnection<ServerEvent>
    {
        PlayerModel GetPlayerModel(Guid world);
    }
}
