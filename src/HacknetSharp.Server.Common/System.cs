using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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


        public enum ReadAccessResult
        {
            Readable,
            NotReadable,
            NoExist
        }

        public bool TryGetWithAccess(string path, LoginModel login, out ReadAccessResult result,
            [NotNullWhen(true)] out FileModel? closest)
        {
            closest = GetClosestWithReadableParent(path, login);
            result = !closest?.CanRead(login) ?? false ? ReadAccessResult.NotReadable :
                closest == null || closest.FullPath != path ? ReadAccessResult.NoExist : ReadAccessResult.Readable;
            return result == ReadAccessResult.Readable;
        }

        public FileModel? GetClosestWithReadableParent(string path, LoginModel login)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            return GetClosestWithReadableParentInternal(nPath, nName, login);
        }

        private FileModel? GetClosestWithReadableParentInternal(string nPath, string nName, LoginModel login)
        {
            if (nPath == "/" && nName == "") return null;
            FileModel? top;
            if (nPath != "/")
            {
                var (pPath, pName) = GetDirectoryAndNameInternal(nPath);
                top = GetClosestWithReadableParentInternal(pPath, pName, login);
            }
            else
                top = null;

            var self = Model.Files
                .Where(f => f.Hidden == false && f.Path == nPath && f.Name == nName)
                .FirstOrDefault();
            return self != null && (top == null || top.CanRead(login) && top.FullPath == nPath) ? self : top;
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
