using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    /// <summary>
    /// Represents a file or folder in a filesystem.
    /// </summary>
    public class FileModel : WorldMember<Guid>
    {
        /// <summary>
        /// System this file belongs to.
        /// </summary>
        public virtual SystemModel System { get; set; } = null!;

        /// <summary>
        /// Owner of this file.
        /// </summary>
        public virtual LoginModel Owner { get; set; } = null!;

        /// <summary>
        /// File path component (directory).
        /// </summary>
        public virtual string Path { get; set; } = null!;

        /// <summary>
        /// File name component.
        /// </summary>
        public virtual string Name { get; set; } = null!;

        /// <summary>
        /// File content.
        /// </summary>
        public virtual string? Content { get; set; }

        /// <summary>
        /// File type.
        /// </summary>
        public virtual FileKind Kind { get; set; }

        /// <summary>
        /// Minimum access required for read.
        /// </summary>
        public virtual AccessLevel Read { get; set; }

        /// <summary>
        /// Minimum access required for write.
        /// </summary>
        public virtual AccessLevel Write { get; set; }

        /// <summary>
        /// Minimum access required for execute.
        /// </summary>
        public virtual AccessLevel Execute { get; set; }

        /// <summary>
        /// If true, separate existence from non-hidden filesystem, and shouldn't ever be visible to users.
        /// </summary>
        public virtual bool Hidden { get; set; }

        /// <summary>
        /// Checks read access for the given login.
        /// </summary>
        /// <param name="login">Login to check.</param>
        /// <returns>True if read is allowed for specified user.</returns>
        /// <remarks>
        /// This method does not check filesystem hierarchy, it purely checks against this file's self-declared properties.
        /// </remarks>
        public bool CanRead(LoginModel login) =>
            login.Admin || Read == AccessLevel.Owner && Owner == login ||
            Read == AccessLevel.Everyone;


        /// <summary>
        /// Checks write access for the given login.
        /// </summary>
        /// <param name="login">Login to check.</param>
        /// <returns>True if write is allowed for specified user.</returns>
        /// <remarks>
        /// This method does not check filesystem hierarchy, it purely checks against this file's self-declared properties.
        /// </remarks>
        public bool CanWrite(LoginModel login) =>
            login.Admin || Write == AccessLevel.Owner && Owner == login ||
            Write == AccessLevel.Everyone;


        /// <summary>
        /// Checks execute access for the given login.
        /// </summary>
        /// <param name="login">Login to check.</param>
        /// <returns>True if execute is allowed for specified user.</returns>
        /// <remarks>
        /// This method does not check filesystem hierarchy, it purely checks against this file's self-declared properties.
        /// </remarks>
        public bool CanExecute(LoginModel login) =>
            login.Admin || Execute == AccessLevel.Owner && Owner == login ||
            Execute == AccessLevel.Everyone;

        /// <summary>
        /// Full file path with both directory and name.
        /// </summary>
        public string FullPath => Executable.Combine(Path, Name);

        /// <summary>
        /// File type.
        /// </summary>
        public enum FileKind : byte
        {
            /// <summary>
            /// Text content stored in database.
            /// </summary>
            TextFile,

            /// <summary>
            /// Content stored on local filesystem.
            /// </summary>
            FileFile,

            /// <summary>
            /// Program file.
            /// </summary>
            ProgFile,

            /// <summary>
            /// Folder.
            /// </summary>
            Folder
        }

        /// <summary>
        /// Access level for a file operation.
        /// </summary>
        public enum AccessLevel : byte
        {
            /// <summary>
            /// All users are allowed to access.
            /// </summary>
            Everyone,

            /// <summary>
            /// Only the owner or admins are allowed to access.
            /// </summary>
            Owner,

            /// <summary>
            /// Only admins are allowed to access.
            /// </summary>
            Admin
        }

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) => builder.Entity<FileModel>(x => x.HasKey(v => v.Key));
#pragma warning restore 1591
    }
}
