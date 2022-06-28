using NUnit.Framework;
using static HacknetSharp.Test.Util.TestsSupport;

namespace HacknetSharp.Test.ProgramTests;

public class LsTests
{
    // TODO cwd test

    // TODO .. pathing test

    [Test]
    public void ExistingFolder_NoTrailingSlash_Works()
    {
        using var server = ConfigureSimplePopulatedAdmin(out var world, out var user, out _, out _, out var ctx);
        StartBasicShell(world, ctx);
        QueueAndUpdate(server, ctx, user, "ls /usr", 16);
        Assert.That(ctx.GetClearText(), Is.EqualTo("bin   lib   \nlocal share \n"));
        AssertDisconnect(server, ctx);
    }

    [Test]
    public void ExistingFolder_WithTrailingSlash_Works()
    {
        using var server = ConfigureSimplePopulatedAdmin(out var world, out var user, out _, out _, out var ctx);
        StartBasicShell(world, ctx);
        QueueAndUpdate(server, ctx, user, "ls /usr/", 16);
        Assert.That(ctx.GetClearText(), Is.EqualTo("bin   lib   \nlocal share \n"));
        AssertDisconnect(server, ctx);
    }

    [Test]
    public void MissingFolder_MarkedMissing()
    {
        using var server = ConfigureSimplePopulatedAdmin(out var world, out var user, out _, out _, out var ctx);
        StartBasicShell(world, ctx);
        QueueAndUpdate(server, ctx, user, "ls /usrs");
        Assert.That(ctx.GetClearText(), Is.EqualTo("ls: /usrs: No such file or directory\n"));
        AssertDisconnect(server, ctx);
    }

    [Test]
    public void ProtectedFolder_FromAdmin_Visible()
    {
        using var server = ConfigureSimplePopulatedAdmin(out var world, out var user, out var person, out var system, out var ctx);
        StartBasicShell(world, ctx, person, system);
        QueueAndUpdate(server, ctx, user, "ls /root/");
        Assert.That(ctx.GetClearText(), Is.EqualTo("jazzco_firmware_v2 \n"));
        AssertDisconnect(server, ctx);
    }

    [Test]
    public void ProtectedFolder_FromRegular_Protected()
    {
        using var server = ConfigureSimplePopulatedRegular(out var world, out var user, out var person, out var system, out var ctx);
        StartBasicShell(world, ctx, person, system);
        QueueAndUpdate(server, ctx, user, "ls /root/");
        Assert.That(ctx.GetClearText(), Is.EqualTo("ls: /root/: No such file or directory\n"));
        AssertDisconnect(server, ctx);
    }
}
