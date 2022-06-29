using HacknetSharp.Test.Util;
using NUnit.Framework;

namespace HacknetSharp.Test.ProgramTests;

public class CatTests : ProgramTestBase
{
    [Test]
    public void NormalFile_Works()
    {
        UnixFs().UserName("a_user").Files("text:\"/home/{UserName}/test1.txt\" \"ouf\"").Build().Shell();
        Assert.That(Run("cat /home/a_user/test1.txt").NextText(), Is.EqualTo("ouf\n"));
        Assert.That(ProcessCount(), Is.EqualTo(0));
    }

    [Test]
    public void BinaryFile_Disabled()
    {
        UnixFs().Build().Shell();
        Assert.That(Run("cat /bin/cat").NextText(), Is.EqualTo("cat: /bin/cat: Is a binary file\n"));
        Assert.That(ProcessCount(), Is.EqualTo(0));
    }

    [Test]
    public void Directory_Disabled()
    {
        UnixFs().Build().Shell();
        Assert.That(Run("cat /bin").NextText(), Is.EqualTo("cat: /bin: Is a directory\n"));
        Assert.That(ProcessCount(), Is.EqualTo(0));
    }

    [Test]
    public void MissingFile_Works()
    {
        UnixFs().UserName("usao").Build().Shell();
        Assert.That(Run("cat /home/usao/abec.txt").NextText(), Is.EqualTo("cat: /home/usao/abec.txt: No such file or directory\n"));
        Assert.That(ProcessCount(), Is.EqualTo(0));
    }

    [Test]
    public void Protected_FromAdmin_Visible()
    {
        UnixFs().Admin().Files("text+++:/prot/file \"DATA\"").Build().Shell();
        Assert.That(Run("cat /prot/file").NextText(), Is.EqualTo("DATA\n"));
        Assert.That(ProcessCount(), Is.EqualTo(0));
    }

    [Test]
    public void ProtectedFolder_FromRegular_Protected()
    {
        UnixFs().Unelevated().Files("fold+++:/prot/", "text+++:/prot/file \"DATA\"").Build().Shell();
        Assert.That(Run("cat /prot/file").NextText(), Is.EqualTo("cat: /prot/file: No such file or directory\n"));
        Assert.That(ProcessCount(), Is.EqualTo(0));
    }

    [Test]
    public void ProtectedFile_FromRegular_Protected()
    {
        UnixFs().Unelevated().Files("fold:/prot/", "text+++:/prot/file \"DATA\"").Build().Shell();
        Assert.That(Run("cat /prot/file").NextText(), Is.EqualTo("cat: /prot/file: Permission denied\n"));
        Assert.That(ProcessCount(), Is.EqualTo(0));
    }

    [Test]
    public void ProtectedFile2_FromRegular_Protected()
    {
        UnixFs().Unelevated().Files("text+++:/prot/file \"DATA\"").Build().Shell();
        Assert.That(Run("cat /prot/file").NextText(), Is.EqualTo("cat: /prot/file: Permission denied\n"));
        Assert.That(ProcessCount(), Is.EqualTo(0));
    }
}
