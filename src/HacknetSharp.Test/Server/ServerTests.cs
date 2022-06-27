using System;
using System.Linq;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace HacknetSharp.Test.Server;

public class ServerTests
{
    [Test]
    public void SynchronousServer_Empty_Boots()
    {
        using var server = CreateServer();
        server.Start();
    }

    [Test]
    public void SynchronousServer_Empty_SingleUpdate_Works()
    {
        using var server = CreateServer();
        server.Start();
        server.Update(1.0f);
    }

    [Test]
    public void SynchronousServer_CommandInvocation_Works()
    {
        var worldModel = CreateWorldModel();
        var templateGroup = CreateTemplateGroup();
        var systemTemplate = templateGroup.SystemTemplates["systemtemplate1"] = CreateSystemTemplate("jacobian");
        using var server = CreateServer(CreateConfig(worldModel, templateGroup));
        server.Start();
        var world = server.DefaultWorld;
        var password = ServerUtil.HashPassword("rosebud");
        var user = server.Spawn.User("user1", password, false);
        var person = world.Spawn.Person("person1", "person1username", user: user);
        var system = world.Spawn.System(systemTemplate, "systemtemplate1", person, user.Password, new IPAddressRange("192.168.0.32"));
        var ctx = new SynchronousTestServerPersonContext(person);
        world.StartShell(ctx, person, system.Logins.Single(), new[] { ServerConstants.ShellName }, true);
        person.DefaultSystem = system.Key;
        server.QueueCommand(ctx, user, Guid.NewGuid(), 16, "grep");
        server.Update(1.0f);
        Assert.That(ctx.Text.ToString(), Is.EqualTo("grep: command not found\n"));
    }

    private static SystemTemplate CreateSystemTemplate(string name, string osName = "EridanusOS") => new() { Name = name, OsName = osName };

    private static WorldModel CreateWorldModel() => WorldModel.CreateEmpty("A world", "Wait what", "player_system_template");

    private static TemplateGroup CreateTemplateGroup(SystemTemplate? playerSystemTemplate = null) => new() { SystemTemplates = { ["player_system_template"] = playerSystemTemplate ?? new SystemTemplate() } };

    private static SynchronousTestServerConfig CreateConfig(WorldModel worldModel, TemplateGroup templateGroup, bool withLogger = false)
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

    private SynchronousTestServer CreateServer(SynchronousTestServerConfig? config = null)
    {
        return new SynchronousTestServer(config ?? CreateConfig(CreateWorldModel(), CreateTemplateGroup()));
    }
}
