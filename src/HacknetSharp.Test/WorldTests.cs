using HacknetSharp.Test.Util;
using NUnit.Framework;
using static HacknetSharp.Test.Util.TestsSupport;

namespace HacknetSharp.Test;

public class WorldTests
{
    [Test]
    public void SynchronousServer_MissingExe_NoBinFolder_Works()
    {
        using var server = Configure(new Setup {Populated = false, Admin = true}, out var world, out var user, out _, out _, out var ctx);
        StartShell(world, ctx);
        QueueAndUpdate(server, ctx, user, "grep");
        Assert.That(ctx.NextText(), Is.EqualTo("grep: command not found\n"));
        AssertDisconnect(server, ctx);
    }

    [Test]
    public void SynchronousServer_MissingExe_WithBinFolder_Works()
    {
        using var server = Configure(new Setup {Populated = true, Admin = true}, out var world, out var user, out _, out _, out var ctx);
        StartShell(world, ctx);
        QueueAndUpdate(server, ctx, user, "grep");
        Assert.That(ctx.NextText(), Is.EqualTo("/bin/grep: not found\n"));
        AssertDisconnect(server, ctx);
    }
}
