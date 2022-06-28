using System.IO;
using System.Text;
using HacknetSharp.Server.Lua;
using HacknetSharp.Server.Templates;
using NUnit.Framework;

namespace HacknetSharp.Test
{
    public class MsTemplatingTests
    {
        [Test]
        public void MsTemplate_Import_Works()
        {
            const string src = @"
local x = system_t.__new()
x.Name = ""rock""
x.AddFiles(""grind"", {""/path1"", ""/path2""})
return x";
            var loader = new LuaContentImporter();
            var template = loader.Import<SystemTemplate>(new MemoryStream(Encoding.UTF8.GetBytes(src)));
            Assert.That(template, Is.Not.Null);
            Assert.That(template!.Name, Is.EqualTo("rock"));
            var files = template.Filesystem;
            Assert.That(files, Contains.Key("grind"));
            Assert.That(files!["grind"], Is.EquivalentTo(new[] { "/path1", "/path2" }));
        }
    }
}
