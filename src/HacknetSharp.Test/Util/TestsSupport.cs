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
    internal static string[] DefaultAddressPool = new[] { "192.168.0.32", "192.168.0.33" };

    internal static ShellProcess StartShell(World world, SynchronousTestServerPersonContext ctx)
    {
        var person = ctx.GetPerson(world);
        var system = person.Systems.First();
        return StartShell(world, ctx, person, system);
    }

    internal static ShellProcess StartShell(World world, SynchronousTestServerPersonContext ctx, PersonModel person, SystemModel system)
    {
        return world.StartShell(ctx, person, system.Logins.Single(v => v.Person == person.Key), ServerConstants.GetLoginShellArgv(), true) ?? throw new InvalidOperationException();
    }

    internal static void QueueAndUpdate(SynchronousTestServer server, SynchronousTestServerPersonContext ctx, UserModel user, string command, int consoleWidth = 32)
    {
        server.QueueCommand(ctx, user, Guid.NewGuid(), consoleWidth, command);
        server.Update(0.0f);
    }

    internal static void Queue(SynchronousTestServer server, SynchronousTestServerPersonContext ctx, UserModel user, string command, int consoleWidth = 32)
    {
        server.QueueCommand(ctx, user, Guid.NewGuid(), consoleWidth, command);
    }

    internal static void Update(SynchronousTestServer server, float deltaTime)
    {
        server.Update(deltaTime);
    }

    internal static SynchronousTestServer Configure(Setup options, out World world, out UserModel user,
        out PersonModel person, out SystemModel system, out SynchronousTestServerPersonContext ctx)
    {
        if (options.AddressPool.IsEmpty) options = options with { AddressPool = DefaultAddressPool };
        var worldModel = CreateWorldModel();
        var templateGroup = CreateTemplateGroup();
        var systemTemplate = templateGroup.SystemTemplates[options.SystemTemplateName] = CreateSystemTemplate(options, options.SystemName);
        if (options.Admin) return ConfigureSimpleInternal(options, worldModel, templateGroup, systemTemplate, options.SystemTemplateName, out world, out user, out person, out system, out ctx);
        var adminOptions = options with { Identity = $"{options.Identity}_Admin", Name = $"{options.Name}_Admin", UserName = $"{options.UserName}_Admin", AddressPool = options.AddressPool[1..] };
        var server = ConfigureSimpleInternal(adminOptions, worldModel, templateGroup, systemTemplate, options.SystemTemplateName, out world, out user, out person, out system, out ctx);
        return ConfigureSimpleRegularUserInternal(options, server, system, systemTemplate, options.SystemTemplateName, out user, out person, out ctx);
    }

    private static SynchronousTestServer ConfigureSimpleInternal(Setup options, WorldModel worldModel,
        TemplateGroup templateGroup, SystemTemplate systemTemplate, string systemTemplateName, out World world,
        out UserModel user, out PersonModel person, out SystemModel system, out SynchronousTestServerPersonContext ctx)
    {
        var server = CreateServer(CreateConfig(worldModel, templateGroup));
        server.Start();
        world = server.DefaultWorld;
        var password = ServerUtil.HashPassword(options.Password);
        user = server.Spawn.User(options.Identity, password, false);
        person = world.Spawn.Person(options.Name, options.UserName, user: user);
        Assert.That(person.Systems.Count, Is.EqualTo(0));
        system = world.Spawn.System(systemTemplate, systemTemplateName, person, user.Password, new IPAddressRange(options.AddressPool.Span[0]));
        Assert.That(person.Systems.Count, Is.EqualTo(1));
        person.DefaultSystem = system.Key;
        ctx = new SynchronousTestServerPersonContext(person) { Connected = true };
        return server;
    }

    private static SynchronousTestServer ConfigureSimpleRegularUserInternal(Setup options, SynchronousTestServer server,
        SystemModel system, SystemTemplate systemTemplate, string systemTemplateName,
        out UserModel user, out PersonModel person, out SynchronousTestServerPersonContext ctx)
    {
        var world = server.DefaultWorld;
        var password = ServerUtil.HashPassword(options.Password);
        user = server.Spawn.User(options.Name, password, false);
        person = world.Spawn.Person(options.Name, options.UserName, user: user);
        Assert.That(person.Systems.Count, Is.EqualTo(0));
        world.Spawn.System(systemTemplate, systemTemplateName, person, user.Password, new IPAddressRange(options.AddressPool.Span[0]));
        Assert.That(person.Systems.Count, Is.EqualTo(1));
        world.Spawn.Login(system, options.UserName, password, false, person);
        person.DefaultSystem = system.Key;
        ctx = new SynchronousTestServerPersonContext(person) { Connected = true };
        return server;
    }

    internal static void AssertDisconnect(SynchronousTestServer server, SynchronousTestServerPersonContext ctx)
    {
        ctx.Connected = false;
        server.Update(0.0f);
        Assert.That(ctx.NextText(), Is.EqualTo("[Shell terminated]\n"));
    }
}
