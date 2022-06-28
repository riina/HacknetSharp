using System.Collections.Generic;
using System.Linq;
using HacknetSharp.Server;
using HacknetSharp.Server.Models;
using HacknetSharp.Server.Templates;
using NUnit.Framework;

namespace HacknetSharp.Test
{
    public class FilesystemTests
    {
        [Test]
        public void Filesystem_Simple_Works()
        {
            var tg = new TemplateGroup();
            var wt = new WorldTemplate("system");
            tg.WorldTemplates.Add("world", wt);
            var st = new SystemTemplate("{Owner.Name}'s Home Base", "EncomOS");
            tg.SystemTemplates.Add("system", st);
            var db = new DummyServerDatabase();
            var s = new Spawn(db);
            var w = s.World("Kawahara", tg, wt);
            var ws = new WorldSpawn(db, w);
            var person = ws.Person("Barney from Black Mesa", "barney");
            var password = ServerUtil.HashPassword("password");
            var sys = ws.System(st, "system", person, password, new IPAddressRange("69.69.0.0/16"));
            var li = ws.Login(sys, "jacob", password, true, person);
            ws.Folder(sys, li, "/etc");
            ws.Folder(sys, li, "/bin");
            ws.ProgFile(sys, li, "/bin/porthack", "core:porthack");
            Assert.AreEqual(Basic(sys), Basic(new[] { "/bin", "/bin/porthack", "/etc" }));
            Assert.IsTrue(sys.TryGetFile("/bin", li, out _, out _, out var bin1));
            ws.CopyFile(bin1!, sys, li, "/etc/bin");
            Assert.AreEqual(Basic(sys),
                Basic(new[] { "/bin", "/bin/porthack", "/etc", "/etc/bin", "/etc/bin/porthack" }));
            ws.RemoveFile(bin1!, li);
            Assert.AreEqual(Basic(sys), Basic(new[] { "/etc", "/etc/bin", "/etc/bin/porthack" }));
        }

        private static IList<string> Basic(SystemModel system) =>
            system.Files.Select(f => f.FullPath).OrderBy(s => s).ToList();

        private static IList<string> Basic(string[] arr) =>
            arr.OrderBy(s => s).ToList();
    }
}
