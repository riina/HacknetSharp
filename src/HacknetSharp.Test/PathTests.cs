using HacknetSharp.Server;
using NUnit.Framework;

namespace HacknetSharp.Test
{
    public class PathTests
    {
        [TestCase("/alec", ExpectedResult = "/alec", TestName = "Identity")]
        [TestCase("/alec/", ExpectedResult = "/alec", TestName = "Identity with trailing slash")]
        [TestCase("/test/..", ExpectedResult = "/", TestName = "Backtrack to root")]
        [TestCase("/test/../", ExpectedResult = "/", TestName = "Backtrack to root with trailing slash")]
        [TestCase("/test/../..", ExpectedResult = "/", TestName = "Multi backtrack to root")]
        [TestCase("/test/../../", ExpectedResult = "/", TestName = "Multi backtrack to root with trailing slash")]
        [TestCase("/test/../../abec/../../../lol", ExpectedResult = "/lol", TestName = "Mixed backtrack to root")]
        [TestCase("/test/../../abec/../../../lol/", ExpectedResult = "/lol", TestName = "Mixed backtrack to root with trailing slash")]
        [TestCase("/test/../test2/path", ExpectedResult = "/test2/path", TestName = "Middle backtrack")]
        [TestCase("/test/../test2/path/", ExpectedResult = "/test2/path", TestName = "Middle backtrack with trailing slash")]
        public string GetNormalized(string value) => Executable.GetNormalized(value);

        [TestCase("/test/me", ExpectedResult = "/test", TestName = "Direct")]
        [TestCase("/", ExpectedResult = null, TestName = "Root")]
        /*[TestCase("/test/sub/..", ExpectedResult = "/", TestName = "Backtrack")]*/ // TODO evaluate
        public string? GetDirectoryName(string value) => Executable.GetDirectoryName(value);

        [TestCase("/root", ExpectedResult = "root", TestName = "Identity")]
        [TestCase("/root/sub", ExpectedResult = "sub", TestName = "Direct")]
        [TestCase("/", ExpectedResult = "", TestName = "Root")]
        public string GetFileName(string value) => Executable.GetFileName(value);

        [TestCase("/", "", ExpectedResult = "/", TestName = "Empty 2")]
        [TestCase("", "", ExpectedResult = "", TestName = "Empty both")]
        [TestCase("/a", "b", ExpectedResult = "/a/b", TestName = "Singles")]
        [TestCase("a", "b", ExpectedResult = "a/b", TestName = "Singles no root")]
        [TestCase("/a/b", "c", ExpectedResult = "/a/b/c", TestName = "Multi root")]
        [TestCase("/a", "c/d", ExpectedResult = "/a/c/d", TestName = "Multi sub")]
        [TestCase("/a/b", "c/d", ExpectedResult = "/a/b/c/d", TestName = "Doubles")]
        public string Combine(string path1, string path2) => Executable.Combine(path1, path2);

        [TestCase("/", ExpectedResult = new[] { "/", "" }, TestName = "Root")]
        [TestCase("/bin", ExpectedResult = new[] { "/", "bin" }, TestName = "Direct 1 layer")]
        [TestCase("/path1/path2", ExpectedResult = new[] { "/path1", "path2" }, TestName = "Direct 2 layers")]
        public string[] GetDirectoryAndName(string value)
        {
            string[] result = new string[2];
            (result[0], result[1]) = Executable.GetDirectoryAndName(value);
            return result;
        }

        [TestCase("/", "/", ExpectedResult = "/", TestName = "Identity")]
        [TestCase("/b", "/", ExpectedResult = "/", TestName = "One layer depth difference")]
        [TestCase("/b", "/z", ExpectedResult = "/", TestName = "One layer difference")]
        [TestCase("/a/b/c", "/a/c/e", ExpectedResult = "/a", TestName = "Multi layer difference")]
        [TestCase("/a/c/e", "/a/b/c", ExpectedResult = "/a", TestName = "Multi layer difference flip")]
        [TestCase("/a/b/c/d", "/a/c/e", ExpectedResult = "/a", TestName = "Multi layer difference different depth")]
        [TestCase("/a/c/e", "/a/b/c/d", ExpectedResult = "/a", TestName = "Multi layer difference different depth flip")]
        [TestCase("/a/b/e", "/a/b/c", ExpectedResult = "/a/b", TestName = "Two layers")]
        public string GetPathInCommon(string path1, string path2) => Executable.GetPathInCommon(path1, path2);
    }
}
