using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server.Templates
{
    public class SystemTemplate
    {
        public string? NameFormat { get; set; }
        public string? OsName { get; set; }
        public string? AddressRange { get; set; }
        public string? ConnectCommandLine { get; set; }
        public List<string>? Users { get; set; } = new List<string>();
        public List<string>? Filesystem { get; set; } = new List<string>();

        private static readonly Regex _userRegex = new Regex(@"([A-Za-z]+):([\S\s]+)");
        private static readonly Regex _fileRegex = new Regex(@"([A-Za-z0-9]+)([*^+]{3})?:([\S\s]+)");

        public void ApplyTemplate(IServerDatabase database, ISpawn spawn, WorldModel world, SystemModel model,
            PersonModel owner, byte[] hash, byte[] salt)
        {
            model.Name = string.Format(CultureInfo.InvariantCulture,
                NameFormat ?? throw new InvalidOperationException($"{nameof(NameFormat)} is null."), owner.UserName);
            model.OsName = OsName ?? throw new InvalidOperationException($"{nameof(OsName)} is null.");
            model.ConnectCommandLine = ConnectCommandLine;
            spawn.Login(database, world, model, owner.UserName, hash, salt, owner);
            if (Users != null)
                foreach (var user in Users)
                {
                    var match = _userRegex.Match(user);
                    if (!match.Success) throw new ApplicationException($"Failed to parse user:pass for {user}");
                    var (hashSub, saltSub) = ServerUtil.HashPassword(match.Groups[1].Value);
                    spawn.Login(database, world, model, match.Groups[1].Value.ToLowerInvariant(), hashSub, saltSub);
                }

            if (Filesystem != null)
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
                            fileModel = spawn.Folder(database, world, model, name, path);
                            break;
                        case "prog":
                            if (args.Length < 2)
                                throw new ApplicationException($"Not enough arguments to file entry {file}");
                            fileModel = spawn.ProgFile(database, world, model, name, path, args[1]);
                            break;
                        case "text":
                            if (args.Length < 2)
                                throw new ApplicationException($"Not enough arguments to file entry {file}");
                            fileModel = spawn.TextFile(database, world, model, name, path, args[1]);
                            break;
                        case "file":
                            if (args.Length < 2)
                                throw new ApplicationException($"Not enough arguments to file entry {file}");
                            fileModel = spawn.FileFile(database, world, model, name, path, args[1]);
                            break;
                        case "blob":
                            throw new NotImplementedException();
                        default:
                            throw new Exception($"Unknown file model type in file entry {file}");
                    }

                    if (match.Groups[2].Success)
                    {
                        string matchStr = match.Groups[2].Value;
                        fileModel.Read = CharToAccessLevel(matchStr[0]);
                        fileModel.Write = CharToAccessLevel(matchStr[1]);
                        fileModel.Execute = CharToAccessLevel(matchStr[2]);
                    }
                }
        }

        private static FileModel.AccessLevel CharToAccessLevel(char c) => c switch
        {
            '*' => FileModel.AccessLevel.Everyone,
            '^' => FileModel.AccessLevel.Owner,
            '+' => FileModel.AccessLevel.Admin,
            _ => throw new ApplicationException($"Unknown access level character {c}")
        };
    }
}
