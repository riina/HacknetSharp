using System;
using HacknetSharp.Server;

namespace HacknetSharp.Test.Util;

internal class SynchronousTestServer : ServerBaseSynchronous
{
    public SynchronousTestServer(SynchronousTestServerConfig config) : base(config)
    {
        if (config.Database == null) throw new ArgumentException("Null database");
        if (config.MainWorld == null) throw new ArgumentException("Null main world");
        ConfigureDatabase(config.Database);
        var world = new World(this, config.MainWorld);
        Worlds[world.Model.Key] = world;
        ConfigureDefaultWorld(world);
    }
}
