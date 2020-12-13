using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    /// <summary>
    /// Represents a networked device in a world.
    /// </summary>
    public class SystemModel : WorldMember<Guid>
    {
        /// <summary>
        /// Device name.
        /// </summary>
        public virtual string Name { get; set; } = null!;

        /// <summary>
        /// Operating system name.
        /// </summary>
        public virtual string OsName { get; set; } = null!;

        /// <summary>
        /// Device network address.
        /// </summary>
        public virtual uint Address { get; set; }

        /// <summary>
        /// Command line to execute on user login.
        /// </summary>
        public virtual string? ConnectCommandLine { get; set; }

        /// <summary>
        /// Time this system was last booted at.
        /// </summary>
        /// <remarks>
        /// Can be a time in the future, which would signify a reboot that will complete at the specified time.
        /// </remarks>
        public virtual double BootTime { get; set; }

        /// <summary>
        /// Number of required exploits for a successful hack.
        /// </summary>
        public virtual int RequiredExploits { get; set; }

        /// <summary>
        /// Owner of this system.
        /// </summary>
        public virtual PersonModel Owner { get; set; } = null!;

        /// <summary>
        /// Valid logins on this system.
        /// </summary>
        public virtual HashSet<LoginModel> Logins { get; set; } = null!;

        /// <summary>
        /// Filesystem.
        /// </summary>
        public virtual HashSet<FileModel> Files { get; set; } = null!;

        /// <summary>
        /// Vulnerabilities.
        /// </summary>
        public virtual HashSet<VulnerabilityModel> Vulnerabilities { get; set; } = null!;

        /// <summary>
        /// Systems this system knows.
        /// </summary>
        public virtual HashSet<KnownSystemModel> KnownSystems { get; set; } = null!;

        /// <summary>
        /// Systems this system is known by.
        /// </summary>
        public virtual HashSet<KnownSystemModel> KnowingSystems { get; set; } = null!;

        /// <summary>
        /// Processes currently running on this system.
        /// </summary>
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
        /// <summary>
        /// Enumerates processes visible by login or all processes if none is specified.
        /// </summary>
        /// <param name="loginModel">Login to restrict view to or null.</param>
        /// <param name="pid">PID to limit search by.</param>
        /// <param name="parentPid">Parent PID to limit search by.</param>
        /// <returns>Enumeration over matching processes.</returns>
        public IEnumerable<Process> Ps(LoginModel? loginModel, uint? pid, uint? parentPid)
        {
            var src = loginModel != null
                ? Processes.Values.Where(p => p.Context is ProgramContext pc && pc.Login == loginModel)
                : Processes.Values;
            src = pid.HasValue ? src.Where(p => p.Context.Pid == pid.Value) : src;
            src = parentPid.HasValue ? src.Where(p => p.Context.ParentPid == parentPid.Value) : src;
            return src;
        }

        /// <summary>
        /// Splits a path into its directory and filename.
        /// </summary>
        /// <param name="path">Path to split.</param>
        /// <returns>Tuple containing directory and filename.</returns>
        public static (string, string) GetDirectoryAndName(string path) => (
            Program.GetNormalized(Program.GetDirectoryName(path) ?? "/"), Program.GetFileName(path));

        private static (string, string) GetDirectoryAndNameInternal(string path) => (
            Program.GetDirectoryName(path) ?? "/", Program.GetFileName(path));

        /*public bool CanRead(string path, LoginModel login)
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
        }*/

        /// <summary>
        /// Checks if a directory exists.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <param name="hidden">If true, check for hidden files.</param>
        /// <returns>True if specified directory exists.</returns>
        public bool DirectoryExists(string path, bool hidden = false)
        {
            // TODO replace use with TryGetWithAccess
            var (nPath, nName) = GetDirectoryAndName(path);
            if (nPath == "/" && nName == "") return true;
            return Files
                .Any(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName &&
                          f.Kind == FileModel.FileKind.Folder);
        }

        /// <summary>
        /// Checks if a file exists.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <param name="caseInsensitiveFile"></param>
        /// <param name="hidden">If true, check for hidden files.</param>
        /// <returns>True if specified file exists.</returns>
        public bool FileExists(string path, bool caseInsensitiveFile = false, bool hidden = false)
        {
            // TODO replace use with TryGetWithAccess
            var (nPath, nName) = GetDirectoryAndName(path);
            var comparison = caseInsensitiveFile
                ? StringComparison.InvariantCultureIgnoreCase
                : StringComparison.InvariantCulture;
            return Files.Any(f => f.Hidden == hidden && f.Path == nPath && f.Name.Equals(nName, comparison) &&
                                  (f.Kind == FileModel.FileKind.FileFile ||
                                   f.Kind == FileModel.FileKind.ProgFile ||
                                   f.Kind == FileModel.FileKind.TextFile));
        }

        /// <summary>
        /// Attempts to get a file with a specified login.
        /// </summary>
        /// <param name="path">Desired file path.</param>
        /// <param name="login">Login to check with.</param>
        /// <param name="result">Access result.</param>
        /// <param name="closest">Closest matching parent, null, or file if matched.</param>
        /// <param name="hidden">If true, checks for hidden files.</param>
        /// <returns>True if file and all parents are readable.</returns>
        public bool TryGetWithAccess(string path, LoginModel login, out ReadAccessResult result,
            [NotNullWhen(true)] out FileModel? closest, bool hidden = false)
        {
            // TODO add case insensitivity for file
            closest = GetClosestWithReadableParent(path, login, hidden);
            result = !closest?.CanRead(login) ?? false ? ReadAccessResult.NotReadable :
                closest == null || closest.FullPath != path ? ReadAccessResult.NoExist : ReadAccessResult.Readable;
#pragma warning disable 8762
            return result == ReadAccessResult.Readable;
#pragma warning restore 8762
        }

        /// <summary>
        /// Gets the closest file that has a readable parent.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <param name="login">Login to test against.</param>
        /// <param name="hidden">If true, checks for hidden files.</param>
        /// <returns>Closest file with a readable parent (may be the file itself) or null.</returns>
        public FileModel? GetClosestWithReadableParent(string path, LoginModel login, bool hidden = false)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            return GetClosestWithReadableParentInternal(nPath, nName, login, hidden);
        }

        private FileModel? GetClosestWithReadableParentInternal(string nPath, string nName, LoginModel login,
            bool hidden)
        {
            if (nPath == "/" && nName == "") return null;
            FileModel? top;
            if (nPath != "/")
            {
                var (pPath, pName) = GetDirectoryAndNameInternal(nPath);
                top = GetClosestWithReadableParentInternal(pPath, pName, login, hidden);
            }
            else
                top = null;

            var self = Files
                .FirstOrDefault(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName);
            return self != null && (top == null || top.CanRead(login) && top.FullPath == nPath) ? self : top;
        }

        /// <summary>
        /// Attempts to get file/folder with specified path.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <param name="hidden">If true, checks for hidden files.</param>
        /// <returns>File or null if not found.</returns>
        public FileModel? GetFileSystemEntry(string path, bool hidden = false)
        {
            var (nPath, nName) = GetDirectoryAndName(path);
            return Files.FirstOrDefault(f => f.Hidden == hidden && f.Path == nPath && f.Name == nName);
        }

        /// <summary>
        /// Enumerates all files in a specified directory.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <param name="hidden">If true, checks for hidden files.</param>
        /// <returns>Enumeration over files in directory.</returns>
        /// <exception cref="DirectoryNotFoundException">Thrown if specified directory does not exist.</exception>
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

        /// <summary>
        /// Gets first available PID in range from 1 to <see cref="int.MaxValue"/>. Should realistically never be null.
        /// </summary>
        /// <returns>Process ID or null if all are exhausted.</returns>
        public uint? GetAvailablePid() =>
            (uint?)Enumerable.Range(1, int.MaxValue).FirstOrDefault(v => Processes.Keys.All(k => k != v));
    }
}
