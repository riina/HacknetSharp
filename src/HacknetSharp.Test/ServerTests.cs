using System.Linq;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;
using NUnit.Framework;
using static HacknetSharp.Test.Util.TestsSupport;
using static HacknetSharp.Test.Util.SynchronousTestServerTemplateConfiguration;

namespace HacknetSharp.Test;

public class ServerTests
{
    [Test]
    public void SynchronousServer_Empty_SingleUpdate_Works()
    {
        using var server = CreateServer();
        server.Start();
        server.Update(1.0f);
    }

    [Test]
    public void SynchronousServer_ConnectionState_Works()
    {
        using var server = ConfigureSimpleNormalSystem(out var world, out UserModel _, out var person, out var system, out var ctx);
        world.StartShell(ctx, person, system.Logins.Single(), new[] { ServerConstants.ShellName }, true);
        person.DefaultSystem = system.Key;
        server.Update(0.0f);
        Assert.That(ctx.GetClearText(), Is.EqualTo(""));
        AssertDisconnect(server, ctx);
    }
}
