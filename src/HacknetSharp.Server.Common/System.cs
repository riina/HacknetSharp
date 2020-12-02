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
            // TODO establish access control
        }

        public static (string, string) GetDirectoryAndName(string path) => (
            Program.GetNormalized(Program.GetDirectoryName(path) ?? "/"), Program.GetFileName(path));

        private static (string, string) GetDirectoryAndNameInternal(string path) => (
            Program.GetDirectoryName(path) ?? "/", Program.GetFileName(path));


        public bool CanRead(string path, LoginModel login)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            if (nPath == "/" && nName == "") return true;
            var entry = Model.Files.Where(f => f.Hidden == false && f.Path == nPath && f.Name == nName)
                .FirstOrDefault();
            var (pPath, pName) = GetDirectoryAndNameInternal(nPath);
            return CanReadInternal(pPath, pName, login) && (entry != null
                ? entry!.CanRead(login)
                : throw new FileNotFoundException($"{path} not found."));
        }

        public bool CanWrite(string path, LoginModel login)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            if (nPath == "/" && nName == "") return true;
            var entry = Model.Files.Where(f => f.Hidden == false && f.Path == nPath && f.Name == nName)
                .FirstOrDefault();
            var (pPath, pName) = GetDirectoryAndNameInternal(nPath);
            return CanReadInternal(pPath, pName, login) && (entry != null
                ? entry!.CanWrite(login)
                : throw new FileNotFoundException($"{path} not found."));
        }

        public bool CanExecute(string path, LoginModel login)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            if (nPath == "/" && nName == "") return true;
            var entry = Model.Files.Where(f => f.Hidden == false && f.Path == nPath && f.Name == nName)
                .FirstOrDefault();
            var (pPath, pName) = GetDirectoryAndNameInternal(nPath);
            return CanReadInternal(pPath, pName, login) && (entry != null
                ? entry!.CanExecute(login)
                : throw new FileNotFoundException($"{path} not found."));
        }

        private bool CanReadInternal(string nPath, string nName, LoginModel login)
        {
            if (nPath == "/" && nName == "") return true;
            if (nPath != "/")
            {
                var (pPath, pName) = GetDirectoryAndNameInternal(nPath);
                if (!CanReadInternal(pPath, pName, login)) return false;
            }

            return Model.Files.Where(f => f.Hidden == false && f.Path == nPath && f.Name == nName)
                .FirstOrDefault()?.CanRead(login) ?? false;
        }

        public bool DirectoryExists(string path, bool hidden = false)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            if (nPath == "/" && nName == "") return true;
            return Model.Files.Where(f =>
                    f.Hidden == hidden && f.Path == nPath && f.Name == nName && f.Kind == FileModel.FileKind.Folder)
                .Any();
        }

        public bool FileExists(string path, bool caseInsensitiveFile = false, bool hidden = false)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            var comparison = caseInsensitiveFile
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture;
            return Model.Files.Where(f =>
                f.Hidden == hidden && f.Path == nPath && f.Name.Equals(nName, comparison) &&
                (f.Kind == FileModel.FileKind.FileFile ||
                 f.Kind == FileModel.FileKind.ProgFile ||
                 f.Kind == FileModel.FileKind.TextFile)).Any();
        }

        public FileModel? GetFileSystemEntry(string path, bool hidden = false)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            return Model.Files.Where(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName).FirstOrDefault();
        }

        public IEnumerable<FileModel> EnumerateDirectory(string path, bool hidden = false)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            if (nPath == "/" && nName == "")
                return Model.Files.Where(f => f.Path == nPath);
            string bPath = Program.Combine(nPath, nName);
            return Model.Files.Where(f => f.Hidden == hidden && f.Path == nPath)
                .Any(f => f.Name == nName && f.Kind == FileModel.FileKind.Folder)
                ? Model.Files.Where(f => f.Hidden == hidden && f.Path == bPath)
                : throw new DirectoryNotFoundException($"Directory {path} not found.");
        }
    }
}
