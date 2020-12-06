using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    public class SystemModel : WorldMember<Guid>
    {
        public virtual string Name { get; set; } = null!;
        public virtual string OsName { get; set; } = null!;
        public virtual uint Address { get; set; }
        public virtual string? ConnectCommandLine { get; set; }
        public virtual double BootTime { get; set; }
        public virtual PersonModel Owner { get; set; } = null!;
        public virtual HashSet<LoginModel> Logins { get; set; } = null!;
        public virtual HashSet<FileModel> Files { get; set; } = null!;
        public virtual HashSet<VulnerabilityModel> Vulnerabilities { get; set; } = null!;
        public virtual HashSet<KnownSystemModel> KnownSystems { get; set; } = null!;
        public virtual HashSet<KnownSystemModel> KnowingSystems { get; set; } = null!;
        public Dictionary<uint, Process> Processes { get; set; } = new Dictionary<uint, Process>();

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) =>
            builder.Entity<SystemModel>(x =>
            {
                x.HasKey(v => v.Key);
                x.HasMany(y => y.Files).WithOne(z => z.System).OnDelete(DeleteBehavior.Cascade);
                x.HasMany(y => y.Logins).WithOne(z => z.System).OnDelete(DeleteBehavior.Cascade);
                x.HasMany(y => y.Vulnerabilities).WithOne(z => z.System).OnDelete(DeleteBehavior.Cascade);
                x.Ignore(y => y.Processes);
            });
#pragma warning restore 1591
        public IEnumerable<Process> Ps(LoginModel? loginModel, uint? pid, uint? parentPid)
        {
            var src = loginModel != null
                ? Processes.Values.Where(p => p.Context is ProgramContext pc && pc.Login == loginModel)
                : Processes.Values;
            src = pid.HasValue ? src.Where(p => p.Context.Pid == pid.Value) : src;
            src = parentPid.HasValue ? src.Where(p => p.Context.ParentPid == parentPid.Value) : src;
            return src;
        }

        public static (string, string) GetDirectoryAndName(string path) => (
            Program.GetNormalized(Program.GetDirectoryName(path) ?? "/"), Program.GetFileName(path));

        private static (string, string) GetDirectoryAndNameInternal(string path) => (
            Program.GetDirectoryName(path) ?? "/", Program.GetFileName(path));

        public bool CanRead(string path, LoginModel login)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            if (nPath == "/" && nName == "") return true;
            var entry = Files
                .FirstOrDefault(f => f.Hidden == false && f.Path == nPath && f.Name == nName);
            var (pPath, pName) = GetDirectoryAndNameInternal(nPath);
            return CanReadInternal(pPath, pName, login) && (entry != null
                ? entry!.CanRead(login)
                : throw new FileNotFoundException($"{path} not found."));
        }

        public bool CanWrite(string path, LoginModel login)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            if (nPath == "/" && nName == "") return true;
            var entry = Files
                .FirstOrDefault(f => f.Hidden == false && f.Path == nPath && f.Name == nName);
            var (pPath, pName) = GetDirectoryAndNameInternal(nPath);
            return CanReadInternal(pPath, pName, login) && (entry != null
                ? entry!.CanWrite(login)
                : throw new FileNotFoundException($"{path} not found."));
        }

        public bool CanExecute(string path, LoginModel login)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            if (nPath == "/" && nName == "") return true;
            var entry = Files
                .FirstOrDefault(f => f.Hidden == false && f.Path == nPath && f.Name == nName);
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

            return Files
                .FirstOrDefault(f => f.Hidden == false && f.Path == nPath && f.Name == nName)?.CanRead(login) ?? false;
        }

        public bool DirectoryExists(string path, bool hidden = false)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            if (nPath == "/" && nName == "") return true;
            return Files
                .Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName &&
                          f.Kind == FileModel.FileKind.Folder);
        }

        public bool FileExists(string path, bool caseInsensitiveFile = false, bool hidden = false)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            var comparison = caseInsensitiveFile
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture;
            return Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name.Equals(nName, comparison) &&
                                  (f.Kind == FileModel.FileKind.FileFile ||
                                   f.Kind == FileModel.FileKind.ProgFile ||
                                   f.Kind == FileModel.FileKind.TextFile));
        }

        public bool TryGetWithAccess(string path, LoginModel login, out ReadAccessResult result,
            [NotNullWhen(true)] out FileModel? closest)
        {
            closest = GetClosestWithReadableParent(path, login);
            result = !closest?.CanRead(login) ?? false ? ReadAccessResult.NotReadable :
                closest == null || closest.FullPath != path ? ReadAccessResult.NoExist : ReadAccessResult.Readable;
#pragma warning disable 8762
            return result == ReadAccessResult.Readable;
#pragma warning restore 8762
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

            var self = Files
                .FirstOrDefault(f => f.Hidden == false && f.Path == nPath && f.Name == nName);
            return self != null && (top == null || top.CanRead(login) && top.FullPath == nPath) ? self : top;
        }

        public FileModel? GetFileSystemEntry(string path, bool hidden = false)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            return Files.FirstOrDefault(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName);
        }

        public IEnumerable<FileModel> EnumerateDirectory(string path, bool hidden = false)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            if (nPath == "/" && nName == "")
                return Files.Where(f => f.Hidden == hidden && f.Path == nPath);
            string bPath = Program.Combine(nPath, nName);
            return Files.Where(f => f.Hidden == hidden && f.Path == nPath)
                .Any(f => f.Name == nName && f.Kind == FileModel.FileKind.Folder)
                ? Files.Where(f => f.Hidden == hidden && f.Path == bPath)
                : throw new DirectoryNotFoundException($"Directory {path} not found.");
        }

        public uint? GetAvailablePid() =>
            (uint?)Enumerable.Range(1, int.MaxValue).FirstOrDefault(v => Processes.Keys.All(k => k != v));
    }
}
