using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HacknetSharp.Server.Common.Models;

namespace HacknetSharp.Server.Common
{
    public class System
    {
        public IWorld World { get; }
        public SystemModel Model { get; }

        public System(IWorld world, SystemModel model)
        {
            World = world;
            Model = model;
        }

        public static (string, string) GetDirectoryAndName(string path) => (
            Program.GetNormalized(Program.GetDirectoryName(path) ?? "/"), Program.GetFileName(path));

        public bool DirectoryExists(string path)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            if (nPath == "/" && nName == "") return true;
            return Model.Files.Where(f => f.Path == nPath && f.Name == nName && f.Kind == FileModel.FileKind.Folder)
                .Any();
        }

        public bool FileExists(string path, bool caseInsensitiveFile = false)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            var comparison = caseInsensitiveFile
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture;
            return Model.Files.Where(f =>
                f.Path == nPath && f.Name.Equals(nName, comparison) && (f.Kind == FileModel.FileKind.BlobFile ||
                                                                        f.Kind == FileModel.FileKind.ProgFile ||
                                                                        f.Kind == FileModel.FileKind.TextFile)).Any();
        }

        public FileModel? GetFileSystemEntry(string path)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            return Model.Files.Where(f => f.Path == nPath && f.Name == nName).FirstOrDefault();
        }

        public IEnumerable<FileModel> EnumerateDirectory(string path)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            string bPath = Program.Combine(nPath, nName);
            return Model.Files.Where(f => f.Path == nPath)
                .Any(f => f.Name == nName && f.Kind == FileModel.FileKind.Folder)
                ? Model.Files.Where(f => f.Path == bPath)
                : throw new DirectoryNotFoundException();
        }
    }
}
