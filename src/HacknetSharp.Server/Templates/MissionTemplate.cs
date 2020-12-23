using System.Collections.Generic;

namespace HacknetSharp.Server.Templates
{
    /// <summary>
    /// Represents a template for a mission.
    /// </summary>
    public class MissionTemplate
    {
        /// <summary>
        /// Lua code to execute when mission starts.
        /// </summary>
        public string? Start { get; set; }

        /// <summary>
        /// Mission goals as lua expressions that evaluate to a boolean.
        /// </summary>
        public List<string>? Goals { get; set; }

        /// <summary>
        /// Objective outcomes.
        /// </summary>
        public List<Outcome>? Outcomes { get; set; }

        /// <summary>
        /// Objective outcome.
        /// </summary>
        public class Outcome
        {
            /// <summary>
            /// Indices of required goals (if null/empty, all goals are considered).
            /// </summary>
            public List<int>? Goals { get; set; }

            /// <summary>
            /// Output of mission as lua code.
            /// </summary>
            public string? Next { get; set; }
        }
    }
}
