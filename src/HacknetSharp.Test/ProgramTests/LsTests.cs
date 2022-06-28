using NUnit.Framework;
using static HacknetSharp.Test.Util.TestsSupport;

namespace HacknetSharp.Test.ProgramTests;

public class LsTests
{
    [Test]
    public void SynchronousServer_Ls_ExistingFolder_Works()
    {
        using var server = ConfigureSimpleNormalSystem(out var world, out var user, out _, out _, out var ctx);
        StartBasicShell(world, ctx);
        QueueAndUpdate(server, ctx, user, "ls /usr", 16);
        Assert.That(ctx.GetClearText(), Is.EqualTo("bin   lib   \nlocal share \n"));
        AssertDisconnect(server, ctx);
    }

    [Test]
    public void SynchronousServer_Ls_MissingFolder_Works()
    {
        using var server = ConfigureSimpleNormalSystem(out var world, out var user, out _, out _, out var ctx);
        StartBasicShell(world, ctx);
        QueueAndUpdate(server, ctx, user, "ls /usrs");
        Assert.That(ctx.GetClearText(), Is.EqualTo("ls: /usrs: No such file or directory\n"));
        AssertDisconnect(server, ctx);
    }

    // TODO bugged. fix.
    /*[Test]
    public void SynchronousServer_Ls_ProtectedFolder_Works()
    {
        using var server = ConfigureSimpleNormalSystem(out var world, out var user, out _, out _, out var ctx);
        StartBasicShell(world, ctx);
        QueueAndUpdate(server, ctx, user, "ls /root/");
        Assert.That(ctx.GetClearText(), Is.EqualTo("ls: /usrs: No such file or directory\n"));
        AssertDisconnect(server, ctx);
    }*/
}
