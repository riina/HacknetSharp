using System;
using HacknetSharp.Server.Models;

namespace HacknetSharp.Server
{
    /// <summary>
    /// Represents a mission queued for assignment.
    /// </summary>
    public class QueuedMission
    {
        /// <summary>
        /// Target person.
        /// </summary>
        public PersonModel Person { get; }

        /// <summary>
        /// Mission template path.
        /// </summary>
        public string MissionPath { get; }

        /// <summary>
        /// Campaign key.
        /// </summary>
        public Guid CampaignKey { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="QueuedMission"/>.
        /// </summary>
        /// <param name="person">Target person.</param>
        /// <param name="missionPath">Mission template path.</param>
        /// <param name="campaignKey">Campaign key.</param>
        public QueuedMission(PersonModel person, string missionPath, Guid campaignKey)
        {
            Person = person;
            MissionPath = missionPath;
            CampaignKey = campaignKey;
        }
    }
}
