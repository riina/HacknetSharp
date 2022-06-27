using HacknetSharp.Server;
using HacknetSharp.Server.Models;
using Microsoft.Extensions.Logging;

namespace HacknetSharp.Test.Server;

internal class SynchronousTestServerConfig : ServerConfigBase
{
    public SynchronousTestDatabase? Database { get; set; }
    public WorldModel? MainWorld { get; set; }

    public SynchronousTestServerConfig WithDatabase(SynchronousTestDatabase database)
    {
        Database = database;
        return this;
    }

    public SynchronousTestServerConfig WithMainWorld(WorldModel defaultWorld)
    {
        MainWorld = defaultWorld;
        return this;
    }

    public new SynchronousTestServerConfig WithTemplates(TemplateGroup templateGroup)
    {
        base.WithTemplates(templateGroup);
        return this;
    }

    public new SynchronousTestServerConfig WithLogger(ILogger logger)
    {
        base.WithLogger(logger);
        return this;
    }
}
