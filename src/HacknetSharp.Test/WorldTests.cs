using NUnit.Framework;
using static HacknetSharp.Test.Util.TestsSupport;

namespace HacknetSharp.Test;

public class WorldTests
{
    [Test]
    public void SynchronousServer_MissingExe_NoBinFolder_Works()
    {
        using var server = ConfigureSimpleEmptyAdmin(out var world, out var user, out _, out _, out var ctx);
        StartBasicShell(world, ctx);
        QueueAndUpdate(server, ctx, user, "grep");
        Assert.That(ctx.GetClearText(), Is.EqualTo("grep: command not found\n"));
        AssertDisconnect(server, ctx);
    }

    [Test]
    public void SynchronousServer_MissingExe_WithBinFolder_Works()
    {
        using var server = ConfigureSimplePopulatedAdmin(out var world, out var user, out _, out _, out var ctx);
        StartBasicShell(world, ctx);
        QueueAndUpdate(server, ctx, user, "grep");
        Assert.That(ctx.GetClearText(), Is.EqualTo("/bin/grep: not found\n"));
        AssertDisconnect(server, ctx);
    }
}
