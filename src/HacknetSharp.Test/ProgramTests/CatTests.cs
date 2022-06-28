using NUnit.Framework;
using static HacknetSharp.Test.Util.TestsSupport;

namespace HacknetSharp.Test.ProgramTests;

public class CatTests
{
    [Test]
    public void NormalFile_Works()
    {
        using var server = ConfigureSimplePopulatedAdmin(out var world, out var user, out var person, out var system, out var ctx);
        StartBasicShell(world, ctx, person, system);
        QueueAndUpdate(server, ctx, user, $"cat /home/{person.UserName}/test1.txt");
        Assert.That(ctx.GetClearText(), Is.EqualTo("ouf\n"));
        AssertDisconnect(server, ctx);
    }

    [Test]
    public void BinaryFile_Disabled()
    {
        using var server = ConfigureSimplePopulatedAdmin(out var world, out var user, out var person, out var system, out var ctx);
        StartBasicShell(world, ctx, person, system);
        QueueAndUpdate(server, ctx, user, "cat /bin/cat");
        Assert.That(ctx.GetClearText(), Is.EqualTo("cat: /bin/cat: Is a binary file\n"));
        AssertDisconnect(server, ctx);
    }

    [Test]
    public void Directory_Disabled()
    {
        using var server = ConfigureSimplePopulatedAdmin(out var world, out var user, out var person, out var system, out var ctx);
        StartBasicShell(world, ctx, person, system);
        QueueAndUpdate(server, ctx, user, "cat /bin");
        Assert.That(ctx.GetClearText(), Is.EqualTo("cat: /bin: Is a directory\n"));
        AssertDisconnect(server, ctx);
    }

    [Test]
    public void MissingFile_Works()
    {
        using var server = ConfigureSimplePopulatedAdmin(out var world, out var user, out var person, out var system, out var ctx);
        StartBasicShell(world, ctx, person, system);
        QueueAndUpdate(server, ctx, user, $"cat /home/{person.UserName}/test1nonexistent.txt");
        Assert.That(ctx.GetClearText(), Is.EqualTo("cat: /home/person1username/test1nonexistent.txt: No such file or directory\n"));
        AssertDisconnect(server, ctx);
    }

    [Test]
    public void Protected_FromAdmin_Visible()
    {
        using var server = ConfigureSimplePopulatedAdmin(out var world, out var user, out var person, out var system, out var ctx);
        StartBasicShell(world, ctx, person, system);
        QueueAndUpdate(server, ctx, user, "cat /root/jazzco_firmware_v2");
        Assert.That(ctx.GetClearText(), Is.EqualTo("thanks for the tech tip\n"));
        AssertDisconnect(server, ctx);
    }

    [Test]
    public void Protected_FromRegular_Protected()
    {
        using var server = ConfigureSimplePopulatedRegular(out var world, out var user, out var person, out var system, out var ctx);
        StartBasicShell(world, ctx, person, system);
        QueueAndUpdate(server, ctx, user, "cat /root/jazzco_firmware_v2");
        Assert.That(ctx.GetClearText(), Is.EqualTo("cat: /root/jazzco_firmware_v2: No such file or directory\n"));
        AssertDisconnect(server, ctx);
    }
}
