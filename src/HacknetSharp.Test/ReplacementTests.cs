using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace HacknetSharp.Test
{
    public class ReplacementTests
    {
        [Test]
        public void Replacement_Simple_Works()
        {
            var dict = new Dictionary<string, string>();
            string str = "daisy johnson {skye}";
            Assert.AreEqual(str, str.ApplyReplacements(dict));
            dict["skye"] = "quake";
            Assert.AreEqual("daisy johnson quake", str.ApplyReplacements(dict));
            dict["blergh1"] = "shots";
            dict["blergh2"] = "fired";
            Assert.AreEqual("shots fired", "{blergh1} {blergh2}".ApplyReplacements(dict));
            const string sourceStr = "{theta}redvs{blue}";
            var splits = sourceStr.SplitReplacements()
                .Select(v => (v.replacement, sourceStr[v.start..(v.start + v.count)])).ToList();
            Assert.AreEqual(3, splits.Count);
            Assert.AreEqual(new[] { (true, "theta"), (false, "redvs"), (true, "blue") }, splits
            );
            const string sourceStr2 = "sigma\\\\{theta}redvs";
            var splits2 = sourceStr2.SplitReplacements()
                .Select(v => (v.replacement, sourceStr2[v.start..(v.start + v.count)])).ToList();
            Assert.AreEqual(3, splits2.Count);
            Assert.AreEqual(new[] { (false, "sigma\\\\"), (true, "theta"), (false, "redvs") }, splits2
            );
            const string sourceStr3 = "\\\\{theta}redvs";
            var splits3 = sourceStr3.SplitReplacements()
                .Select(v => (v.replacement, sourceStr3[v.start..(v.start + v.count)])).ToList();
            Assert.AreEqual(3, splits3.Count);
            Assert.AreEqual(new[] { (false, "\\\\"), (true, "theta"), (false, "redvs") }, splits3
            );
        }
    }
}
