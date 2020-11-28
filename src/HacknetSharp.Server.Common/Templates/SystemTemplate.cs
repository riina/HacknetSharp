using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common.Templates
{
    public class SystemTemplate
    {
        public string? NameFormat { get; set; }
        public string? OsName { get; set; }
        public List<string> Users { get; set; } = new List<string>();
        public List<string> Filesystem { get; set; } = new List<string>();

        private static readonly Regex _userRegex = new Regex(@"([A-Za-z]+):([\S\s]+)");
        private static readonly Regex _fileRegex = new Regex(@"([A-Za-z0-9]+)([*+]{3})?:([\S\s]+)");

        public void ApplyTemplate(ISpawn spawn, WorldModel world, SystemModel model, PersonModel owner, byte[] hash,
            byte[] salt)
        {
            model.Name = string.Format(CultureInfo.InvariantCulture,
                NameFormat ?? throw new InvalidOperationException($"{nameof(NameFormat)} is null."), owner.UserName);
            model.OsName = OsName ?? throw new InvalidOperationException($"{nameof(OsName)} is null.");
            spawn.Login(world, model, owner.UserName, hash, salt);
            foreach (var user in Users)
            {
                var match = _userRegex.Match(user);
                if (!match.Success) throw new ApplicationException($"Failed to parse user:pass for {user}");
                var (hashSub, saltSub) = CommonUtil.HashPassword(match.Groups[1].Value);
                spawn.Login(world, model, match.Groups[1].Value.ToLowerInvariant(), hashSub, saltSub);
            }

            foreach (var file in Filesystem)
            {
                var match = _fileRegex.Match(file);
                if (!match.Success) throw new ApplicationException($"Failed to parse file entry for {match}");
                var args = Arguments.SplitCommandLine(match.Groups[3].Value);
                if (args.Length == 0) throw new ApplicationException($"Not enough arguments to file entry {file}");
                string mainPath = Program.GetNormalized(args[0]);
                string path = Program.GetDirectoryName(mainPath) ??
                              throw new ApplicationException($"Path cannot be {mainPath}");
                string name = Program.GetFileName(mainPath);
                FileModel fileModel;
                switch (match.Groups[1].Value.ToLowerInvariant())
                {
                    case "fold":
                        fileModel = spawn.Folder(world, model, name, path);
                        break;
                    case "prog":
                        if (args.Length < 2)
                            throw new ApplicationException($"Not enough arguments to file entry {file}");
                        fileModel = spawn.ProgFile(world, model, name, path, args[1]);
                        break;
                    case "text":
                        if (args.Length < 2)
                            throw new ApplicationException($"Not enough arguments to file entry {file}");
                        fileModel = spawn.TextFile(world, model, name, path, args[1]);
                        break;
                    case "file":
                        if (args.Length < 2)
                            throw new ApplicationException($"Not enough arguments to file entry {file}");
                        fileModel = spawn.FileFile(world, model, name, path, args[1]);
                        break;
                    case "blob":
                        throw new NotImplementedException();
                    default:
                        throw new Exception($"Unknown file model type in file entry {file}");
                }

                if (match.Groups[2].Success)
                {
                    string matchStr = match.Groups[2].Value;
                    fileModel.AdminRead = matchStr[0] == '+';
                    fileModel.AdminWrite = matchStr[1] == '+';
                    fileModel.AdminExecute = matchStr[2] == '+';
                }
            }
        }
    }
}
