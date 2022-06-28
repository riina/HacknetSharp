using System;
using System.Linq;
using HacknetSharp.Server;
using NUnit.Framework;
using static HacknetSharp.Test.Util.TestsSupport;

namespace HacknetSharp.Test.ProgramTests;

public class LsTests
{
    [Test]
    public void SynchronousServer_Ls_Works()
    {
        using var server = ConfigureSimpleNormalSystem(out var world, out var user, out var person, out var system, out var ctx);
        world.StartShell(ctx, person, system.Logins.Single(), ServerConstants.GetLoginShellArgv(), true);
        person.DefaultSystem = system.Key;
        server.QueueCommand(ctx, user, Guid.NewGuid(), 16, "ls /usr");
        server.Update(0.0f);
        Assert.That(ctx.GetClearText(), Is.EqualTo("bin   lib   \nlocal share \n"));
        AssertDisconnect(server, ctx);
    }
}
