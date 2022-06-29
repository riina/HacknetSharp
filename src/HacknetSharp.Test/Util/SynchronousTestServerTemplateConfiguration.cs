using System.Collections.Generic;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;
using Microsoft.Extensions.Logging;

namespace HacknetSharp.Test.Util;

internal static class SynchronousTestServerTemplateConfiguration
{
    internal static SystemTemplate CreateSystemTemplate(Setup options, string name, string osName = "EridanusOS")
    {
        SystemTemplate template = new() { Name = name, OsName = osName };
        if (options.Populated)
            template.AddFiles("{Owner.UserName}", new HashSet<string>()
            {
                "fold*+*:/bin",
                "fold:/etc",
                "fold:/home",
                "fold*+*:/lib",
                "fold:/mnt",
                "fold+++:/root",
                "fold:/usr",
                "fold:/usr/bin",
                "fold:/usr/lib",
                "fold:/usr/local",
                "fold:/usr/share",
                "fold:/var",
                "fold:/var/spool",
                "prog:/bin/cat core:cat",
                "prog:/bin/cd core:cd",
                "prog:/bin/ls core:ls",
                "prog:/bin/scan core:scan",
                "prog:/bin/map core:map",
                "prog:/bin/cp core:cp",
                "prog:/bin/mv core:mv",
                "prog:/bin/rm core:rm",
                "prog:/bin/mkdir core:mkdir",
                "prog:/bin/scp core:scp",
                "prog:/bin/edit core:edit"
            });
        if (options.AdditionalFiles is { Length: > 0 } f) template.AddFiles("{Owner.UserName}", f);
        return template;
    }

    internal static SystemTemplate CreateEmptySystemTemplate(string name, string osName = "EridanusOS")
    {
        SystemTemplate template = new() { Name = name, OsName = osName };
        return template;
    }

    internal static WorldModel CreateWorldModel() => WorldModel.CreateEmpty("A world", "Wait what", "player_system_template");

    internal static TemplateGroup CreateTemplateGroup(SystemTemplate? playerSystemTemplate = null) => new() { SystemTemplates = { ["player_system_template"] = playerSystemTemplate ?? new SystemTemplate() } };

    internal static SynchronousTestServerConfig CreateConfig(WorldModel worldModel, TemplateGroup templateGroup, bool withLogger = false)
    {
        var cfg = new SynchronousTestServerConfig()
            .WithDatabase(new SynchronousTestDatabase())
            .WithMainWorld(worldModel)
            .WithTemplates(templateGroup);
        if (withLogger)
            cfg = cfg.WithLogger(new AlertLogger(new AlertLogger.Config(LogLevel.Critical, LogLevel.Debug, LogLevel.Error,
                LogLevel.Information, LogLevel.Trace, LogLevel.Warning)));
        return cfg;
    }

    internal static SynchronousTestServer CreateServer(SynchronousTestServerConfig? config = null)
    {
        return new SynchronousTestServer(config ?? CreateConfig(CreateWorldModel(), CreateTemplateGroup()));
    }
}
