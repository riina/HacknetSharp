using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.Models
{
    public class FileModel : WorldMember<Guid>
    {
        public virtual SystemModel System { get; set; } = null!;
        public virtual LoginModel Owner { get; set; } = null!;
        public virtual string Path { get; set; } = null!;
        public virtual string Name { get; set; } = null!;
        public virtual string? Content { get; set; }
        public virtual FileKind Kind { get; set; }
        public virtual AccessLevel Read { get; set; }
        public virtual AccessLevel Write { get; set; }
        public virtual AccessLevel Execute { get; set; }
        public virtual bool Hidden { get; set; }

        public bool CanRead(LoginModel login) =>
            login.Admin || Read == AccessLevel.Owner && Owner == login ||
            Read == AccessLevel.Everyone;

        public bool CanWrite(LoginModel login) =>
            login.Admin || Write == AccessLevel.Owner && Owner == login ||
            Write == AccessLevel.Everyone;

        public bool CanExecute(LoginModel login) =>
            login.Admin || Execute == AccessLevel.Owner && Owner == login ||
            Execute == AccessLevel.Everyone;

        public string FullPath => Program.Combine(Path, Name);

        public enum FileKind : byte
        {
            TextFile,
            FileFile,
            ProgFile,
            Folder
        }

        public enum AccessLevel : byte
        {
            Everyone,
            Owner,
            Admin
        }

        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) => builder.Entity<FileModel>(x => x.HasKey(v => v.Key));
#pragma warning restore 1591
    }
}
