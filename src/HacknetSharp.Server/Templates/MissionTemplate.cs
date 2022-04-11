using System.Collections.Generic;
using MoonSharp.StaticGlue.Core;

namespace HacknetSharp.Server.Templates
{
    /// <summary>
    /// Represents a template for a mission.
    /// </summary>
    [Scriptable("mission_t")]
    public class MissionTemplate
    {
        /// <summary>
        /// Campaign name.
        /// </summary>
        [Scriptable]
        public string? Campaign { get; set; }

        /// <summary>
        /// Friendly title of mission.
        /// </summary>
        [Scriptable]
        public string? Title { get; set; }

        /// <summary>
        /// Text content of mission.
        /// </summary>
        [Scriptable]
        public string? Message { get; set; }

        /// <summary>
        /// Lua code to execute when mission starts.
        /// </summary>
        [Scriptable]
        public string? Start { get; set; }

        /// <summary>
        /// Mission goals as lua expressions that evaluate to a boolean.
        /// </summary>
        // TODO scripting layer
        public List<string>? Goals { get; set; }

        /// <summary>
        /// Objective outcomes.
        /// </summary>
        // TODO scripting layer
        public List<Outcome>? Outcomes { get; set; }
    }

    /// <summary>
    /// Objective outcome.
    /// </summary>
    public class Outcome
    {
        /// <summary>
        /// Indices of required goals (if null/empty, all goals are considered).
        /// </summary>
        // TODO scripting layer
        public List<int>? Goals { get; set; }

        /// <summary>
        /// Output of mission as lua code.
        /// </summary>
        [Scriptable]
        public string? Next { get; set; }
    }
}
