using System;
using System.Collections.Generic;
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
        public List<string>? Users { get; set; }
        public Dictionary<string, List<string>>? Filesystem { get; set; }

        private static readonly Regex _userRegex = new Regex(@"([A-Za-z]+):([\S\s]+)(\+)?");
        private static readonly Regex _fileRegex = new Regex(@"([A-Za-z0-9]+)([*^+]{3})?:([\S\s]+)");

        public virtual void ApplyTemplate(IServerDatabase database, Spawn spawn, WorldModel world, SystemModel model,
            PersonModel owner, byte[] hash, byte[] salt, Dictionary<string, string>? configuration = null)
        {
            var repDict = configuration != null
                ? new Dictionary<string, string>(configuration)
                : new Dictionary<string, string>();
            repDict.Add("Owner.Name", owner.Name);
            repDict.Add("Owner.UserName", owner.UserName);
            model.Name = (NameFormat ?? throw new InvalidOperationException($"{nameof(NameFormat)} is null."))
                .ApplyReplacements(repDict);
            model.OsName = OsName ?? throw new InvalidOperationException($"{nameof(OsName)} is null.");
            model.ConnectCommandLine = ConnectCommandLine?.ApplyReplacements(repDict);
            var unameToLoginDict = new Dictionary<string, LoginModel>
            {
                {owner.UserName, spawn.Login(database, world, model, owner.UserName, hash, salt, true, owner)}
            };
            if (Users != null)
                foreach (var user in Users)
                {
                    var match = _userRegex.Match(user);
                    if (!match.Success) throw new Exception($"Failed to parse user:pass for {user}");
                    var (hashSub, saltSub) = ServerUtil.HashPassword(match.Groups[2].Value);
                    string uname = match.Groups[1].Value;
                    unameToLoginDict.Add(uname, spawn.Login(database, world, model, uname.ToLowerInvariant(), hashSub,
                        saltSub, match.Groups[3].Success));
                }

            if (Filesystem != null)
                foreach (var kvp in Filesystem)
                {
                    string uname = kvp.Key.ApplyReplacements(repDict);
                    if (!unameToLoginDict.TryGetValue(uname, out var fsLogin))
                        throw new Exception($"No login for uname {uname}");
                    repDict["Name"] = uname;
                    repDict["UserName"] = uname;
                    foreach (var file in kvp.Value)
                    {
                        var match = _fileRegex.Match(file);
                        if (!match.Success) throw new ApplicationException($"Failed to parse file entry for {match}");
                        var args = Arguments.SplitCommandLine(match.Groups[3].Value);
                        if (args.Length == 0)
                            throw new Exception($"Not enough arguments to file entry {file}");
                        string mainPath = Program.GetNormalized(args[0]).ApplyReplacements(repDict);
                        string path = Program.GetDirectoryName(mainPath) ??
                                      throw new Exception($"Path cannot be {mainPath}");
                        string name = Program.GetFileName(mainPath);
                        FileModel fileModel;
                        switch (match.Groups[1].Value.ToLowerInvariant())
                        {
                            case "fold":
                                fileModel = spawn.Folder(database, world, model, fsLogin, name, path);
                                break;
                            case "prog":
                                if (args.Length < 2)
                                    throw new Exception($"Not enough arguments to file entry {file}");
                                fileModel = spawn.ProgFile(database, world, model, fsLogin, name, path,
                                    args[1].ApplyReplacements(repDict));
                                break;
                            case "text":
                                if (args.Length < 2)
                                    throw new Exception($"Not enough arguments to file entry {file}");
                                fileModel = spawn.TextFile(database, world, model, fsLogin, name, path,
                                    args[1].ApplyReplacements(repDict));
                                break;
                            case "file":
                                if (args.Length < 2)
                                    throw new Exception($"Not enough arguments to file entry {file}");
                                fileModel = spawn.FileFile(database, world, model, fsLogin, name, path,
                                    args[1].ApplyReplacements(repDict));
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
