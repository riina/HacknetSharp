using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace HacknetSharp.Server.EF.Models
{
    /// <summary>
    /// Represents a file or folder in a filesystem.
    /// </summary>
    public class FileModel : EFModelHelper
    {
        [ModelBuilderCallback]
        [SuppressMessage("ReSharper", "UnusedMember.Local")]
#pragma warning disable 1591
        public static void ConfigureModel(ModelBuilder builder) => builder.Entity<HacknetSharp.Server.Models.FileModel>(x => x.HasKey(v => v.Key));
#pragma warning restore 1591
    }
}
