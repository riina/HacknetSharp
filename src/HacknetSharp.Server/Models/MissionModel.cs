using System;
using HacknetSharp.Server.Templates;

namespace HacknetSharp.Server.Models
{
    /// <summary>
    /// Represents an active mission.
    /// </summary>
    public class MissionModel : WorldMember<Guid>
    {
        /// <summary>
        /// Campaign this mission belongs to.
        /// </summary>
        public virtual Guid CampaignKey { get; set; }

        /// <summary>
        /// Objective completion flags.
        /// </summary>
        public virtual long Flags { get; set; }

        /// <summary>
        /// Source template.
        /// </summary>
        public virtual string Template { get; set; } = null!;

        /// <summary>
        /// Person undertaking this mission.
        /// </summary>
        public virtual PersonModel Person { get; set; } = null!;

        /// <summary>
        /// Mission data.
        /// </summary>
        public MissionTemplate Data { get; set; } = null!;
    }
}
