using System;
using System.Linq;
using HacknetSharp.Server;
using NUnit.Framework;
using static HacknetSharp.Test.Util.TestsSupport;

namespace HacknetSharp.Test;

public class WorldTests
{
    [Test]
    public void SynchronousServer_MissingExe_NoBinFolder_Works()
    {
        using var server = ConfigureSimpleEmptySystem(out var world, out var user, out var person, out var system, out var ctx);
        world.StartShell(ctx, person, system.Logins.Single(), new[] { ServerConstants.ShellName }, true);
        person.DefaultSystem = system.Key;
        server.QueueCommand(ctx, user, Guid.NewGuid(), 16, "grep");
        server.Update(0.0f);
        Assert.That(ctx.GetClearText(), Is.EqualTo("grep: command not found\n"));
        AssertDisconnect(server, ctx);
    }

    [Test]
    public void SynchronousServer_MissingExe_WithBinFolder_Works()
    {
        using var server = ConfigureSimpleNormalSystem(out var world, out var user, out var person, out var system, out var ctx);
        world.StartShell(ctx, person, system.Logins.Single(), new[] { ServerConstants.ShellName }, true);
        person.DefaultSystem = system.Key;
        server.QueueCommand(ctx, user, Guid.NewGuid(), 16, "grep");
        server.Update(0.0f);
        Assert.That(ctx.GetClearText(), Is.EqualTo("/bin/grep: not found\n"));
        AssertDisconnect(server, ctx);
    }
}
