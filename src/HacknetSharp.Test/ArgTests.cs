using System.Linq;
using HacknetSharp.Server;
using NUnit.Framework;

namespace HacknetSharp.Test
{
    public class ArgTests
    {
        [Test]
        public void Arg_Simple_Works()
        {
            const string src = "li\\\"yue \"\\\"ki \\nda\\\" \" \\\"sucks bro\\\" \\\"lm\\\"ao\\\"";
            string[] expected = { "li\"yue", "\"ki \\nda\" ", "\"sucks", "bro\"", "\"lm\"ao\"" };
            Assert.AreEqual(expected, src.SplitCommandLine());
            Assert.AreEqual(expected,
                src.DivideCommandLineElements().Select(sp => src.SliceCommandLineElement(sp)));
            Assert.AreEqual(src, expected.UnsplitCommandLine());
        }
    }
}
