using System;
using System.Linq;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;
using NUnit.Framework;
using static HacknetSharp.Test.Util.SynchronousTestServerTemplateConfiguration;

namespace HacknetSharp.Test.Util;

internal static class TestsSupport
{
    internal static void StartBasicShell(World world, SynchronousTestServerPersonContext ctx)
    {
        var person = ctx.GetPerson(world);
        var system = person.Systems.First();
        StartBasicShell(world, ctx, person, system);
    }

    internal static void QueueAndUpdate(SynchronousTestServer server, SynchronousTestServerPersonContext ctx, UserModel user, string command, int consoleWidth = 32)
    {
        server.QueueCommand(ctx, user, Guid.NewGuid(), consoleWidth, command);
        server.Update(0.0f);
    }

    internal static void StartBasicShell(World world, SynchronousTestServerPersonContext ctx, PersonModel person, SystemModel system)
    {
        world.StartShell(ctx, person, system.Logins.Single(v => v.Person == person.Key), ServerConstants.GetLoginShellArgv(), true);
    }

    internal static SynchronousTestServer ConfigureSimpleEmptyAdmin(out World world, out UserModel user,
        out PersonModel person, out SystemModel system, out SynchronousTestServerPersonContext ctx)
    {
        var worldModel = CreateWorldModel();
        var templateGroup = CreateTemplateGroup();
        var systemTemplate = templateGroup.SystemTemplates["systemtemplate1"] = CreateEmptySystemTemplate("jacobian");
        return ConfigureSimpleInternal(worldModel, templateGroup, systemTemplate, "systemtemplate1", out world, out user, out person, out system, out ctx);
    }

    internal static SynchronousTestServer ConfigureSimpleEmptyRegular(out World world, out UserModel user,
        out PersonModel person, out SystemModel system, out SynchronousTestServerPersonContext ctx)
    {
        var worldModel = CreateWorldModel();
        var templateGroup = CreateTemplateGroup();
        var systemTemplate = templateGroup.SystemTemplates["systemtemplate1"] = CreateEmptySystemTemplate("jacobian");
        var server = ConfigureSimpleInternal(worldModel, templateGroup, systemTemplate, "systemtemplate1", out world, out user, out person, out system, out ctx);
        return ConfigureSimpleRegularUserInternal(server, system, systemTemplate, "systemtemplate1", out user, out person, out ctx);
    }

    internal static SynchronousTestServer ConfigureSimplePopulatedAdmin(out World world, out UserModel user,
        out PersonModel person, out SystemModel system, out SynchronousTestServerPersonContext ctx)
    {
        var worldModel = CreateWorldModel();
        var templateGroup = CreateTemplateGroup();
        var systemTemplate = templateGroup.SystemTemplates["systemtemplate1"] = CreateNormalSystemTemplate("jacobian");
        return ConfigureSimpleInternal(worldModel, templateGroup, systemTemplate, "systemtemplate1", out world, out user, out person, out system, out ctx);
    }

    internal static SynchronousTestServer ConfigureSimplePopulatedRegular(out World world, out UserModel user,
        out PersonModel person, out SystemModel system, out SynchronousTestServerPersonContext ctx)
    {
        var worldModel = CreateWorldModel();
        var templateGroup = CreateTemplateGroup();
        var systemTemplate = templateGroup.SystemTemplates["systemtemplate1"] = CreateNormalSystemTemplate("jacobian");
        var server = ConfigureSimpleInternal(worldModel, templateGroup, systemTemplate, "systemtemplate1", out world, out user, out person, out system, out ctx);
        return ConfigureSimpleRegularUserInternal(server, system, systemTemplate, "systemtemplate1", out user, out person, out ctx);
    }

    private static SynchronousTestServer ConfigureSimpleInternal(WorldModel worldModel, TemplateGroup templateGroup,
        SystemTemplate systemTemplate, string systemTemplateName, out World world, out UserModel user,
        out PersonModel person, out SystemModel system, out SynchronousTestServerPersonContext ctx)
    {
        var server = CreateServer(CreateConfig(worldModel, templateGroup));
        server.Start();
        world = server.DefaultWorld;
        var password = ServerUtil.HashPassword("rosebud");
        user = server.Spawn.User("user1", password, false);
        person = world.Spawn.Person("person1", "person1username", user: user);
        Assert.That(person.Systems.Count, Is.EqualTo(0));
        system = world.Spawn.System(systemTemplate, systemTemplateName, person, user.Password, new IPAddressRange("192.168.0.32"));
        Assert.That(person.Systems.Count, Is.EqualTo(1));
        person.DefaultSystem = system.Key;
        ctx = new SynchronousTestServerPersonContext(person) { Connected = true };
        return server;
    }

    private static SynchronousTestServer ConfigureSimpleRegularUserInternal(SynchronousTestServer server,
        SystemModel system, SystemTemplate systemTemplate, string systemTemplateName,
        out UserModel user, out PersonModel person, out SynchronousTestServerPersonContext ctx)
    {
        var world = server.DefaultWorld;
        var password = ServerUtil.HashPassword("sangheili");
        user = server.Spawn.User("user2", password, false);
        person = world.Spawn.Person("person2", "person2username", user: user);
        Assert.That(person.Systems.Count, Is.EqualTo(0));
        world.Spawn.System(systemTemplate, systemTemplateName, person, user.Password, new IPAddressRange("192.168.0.33"));
        Assert.That(person.Systems.Count, Is.EqualTo(1));
        world.Spawn.Login(system, "person2login1", password, false, person);
        person.DefaultSystem = system.Key;
        ctx = new SynchronousTestServerPersonContext(person) { Connected = true };
        return server;
    }

    internal static void AssertDisconnect(SynchronousTestServer server, SynchronousTestServerPersonContext ctx)
    {
        ctx.Connected = false;
        server.Update(0.0f);
        Assert.That(ctx.GetClearText(), Is.EqualTo("[Shell terminated]\n"));
    }
}
