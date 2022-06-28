using HacknetSharp.Server;
using NUnit.Framework;

namespace HacknetSharp.Test
{
    public class PathTests
    {
        [Test]
        public void Test_Paths()
        {
            Assert.AreEqual("/", Executable.GetNormalized("/test/.."));
            Assert.AreEqual("/test/path", Executable.GetNormalized("/test/../test/path/"));
            Assert.AreEqual("/test", Executable.GetDirectoryName("/test/me"));
            Assert.AreEqual("/test", Executable.GetNormalized(Executable.GetDirectoryName("/test/me/../harder")!));
            Assert.AreEqual(null, Executable.GetDirectoryName("/"));
            Assert.AreEqual("", Executable.GetFileName("/"));
            Assert.AreEqual("/", Executable.Combine("/", ""));
            Assert.AreEqual("/", Executable.GetNormalized(Executable.Combine("/", "")));
            Assert.AreEqual(("/", ""), Executable.GetDirectoryAndName("/"));
            Assert.AreEqual(("/", "shadow"), Executable.GetDirectoryAndName("/shadow"));
            Assert.AreEqual(("/shadow", "absorber"),
                Executable.GetDirectoryAndName("/shadow/absorber"));
            Assert.AreEqual("/", Executable.GetPathInCommon("/b", "/"));
            Assert.AreEqual("/", Executable.GetPathInCommon("/b", "/z"));
            Assert.AreEqual("/faber", Executable.GetPathInCommon("/faber/of/will", "/faber/and/might"));
            Assert.AreEqual("/faber", Executable.GetPathInCommon("/faber/and/might", "/faber/of/will"));
            Assert.AreEqual("/dark/ness", Executable.GetPathInCommon("/dark/ness/lalatina", "/dark/ness/overlord"));
            Assert.AreEqual("/well", Executable.GetPathInCommon("/well/waitforit", "/well/waitfor"));
        }
    }
}
