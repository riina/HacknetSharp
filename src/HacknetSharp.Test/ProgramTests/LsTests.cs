using HacknetSharp.Test.Util;
using NUnit.Framework;

namespace HacknetSharp.Test.ProgramTests;

public class LsTests : ProgramTestBase
{
    // TODO cwd test

    // TODO .. pathing test

    [Test]
    public void ExistingFolder_NoTrailingSlash_Works()
    {
        UnixFs().Admin().Files("fold:/test/bin", "fold:/test/lib", "fold:/test/local", "fold:/test/share").Build().Shell();
        Assert.That(Run("ls /test", 16).NextText(), Is.EqualTo("bin   lib   \nlocal share \n"));
        Assert.That(ProcessCount(), Is.EqualTo(0));
    }

    [Test]
    public void ExistingFolder_WithTrailingSlash_Works()
    {
        UnixFs().Admin().Files("fold:/test/bin", "fold:/test/lib", "fold:/test/local", "fold:/test/share").Build().Shell();
        Assert.That(Run("ls /test/", 16).NextText(), Is.EqualTo("bin   lib   \nlocal share \n"));
        Assert.That(ProcessCount(), Is.EqualTo(0));
    }

    [Test]
    public void MissingFolder_MarkedMissing()
    {
        UnixFs().Admin().Build().Shell();
        Assert.That(Run("ls /usrs").NextText(), Is.EqualTo("ls: /usrs: No such file or directory\n"));
        Assert.That(ProcessCount(), Is.EqualTo(0));
    }

    [Test]
    public void ProtectedFolder_FromAdmin_Visible()
    {
        UnixFs().Admin().Files("text+++:/prot/file \"DATA\"").Build().Shell();
        Assert.That(Run("ls /prot/").NextText(), Is.EqualTo("file \n"));
        Assert.That(ProcessCount(), Is.EqualTo(0));
    }

    [Test]
    public void ProtectedFile_FromRegular_Visible()
    {
        UnixFs().Unelevated().Files("fold:/prot/", "text+++:/prot/file \"DATA\"").Build().Shell();
        Assert.That(Run("ls /prot/").NextText(), Is.EqualTo("file \n"));
        Assert.That(ProcessCount(), Is.EqualTo(0));
    }

    [Test]
    public void ProtectedFolder_FromRegular_Protected()
    {
        UnixFs().Unelevated().Files("fold+++:/prot/", "text+++:/prot/file \"DATA\"").Build().Shell();
        Assert.That(Run("ls /prot/").NextText(), Is.EqualTo("ls: /prot/: No such file or directory\n"));
        Assert.That(ProcessCount(), Is.EqualTo(0));
    }
}
