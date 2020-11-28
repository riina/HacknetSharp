using System;
using System.Collections.Generic;
using System.Globalization;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common.Templates
{
    public class SystemTemplate
    {
        public string? NameFormat { get; set; }
        public string? OsName { get; set; }
        public List<string> Users { get; set; } = new List<string>();
        public List<string> Filesystem { get; set; } = new List<string>();

        public void ApplyTemplate(ISpawn spawn, SystemModel model, PersonModel owner, string base64Hash, string base64Salt)
        {
            model.Name = string.Format(CultureInfo.InvariantCulture,
                NameFormat ?? throw new InvalidOperationException($"{nameof(NameFormat)} is null."), owner);
            model.OsName = OsName ?? throw new InvalidOperationException($"{nameof(OsName)} is null.");
            if (Users.Count == 0) throw new InvalidOperationException($"{nameof(Users)} is empty.");
            // TODO apply template
        }
    }
}
