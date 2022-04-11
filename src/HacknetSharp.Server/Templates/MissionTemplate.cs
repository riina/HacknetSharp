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
        public List<string>? Goals { get; set; }

        /// <summary>
        /// Adds a goal as a lua expression that evaluates to a boolean.
        /// </summary>
        /// <param name="goal">Goal expression.</param>
        [Scriptable]
        public void AddGoal(string goal) => (Goals ??= new List<string>()).Add(goal);

        /// <summary>
        /// Objective outcomes.
        /// </summary>
        public List<Outcome>? Outcomes { get; set; }

        /// <summary>
        /// Creates and adds an <see cref="Outcome"/>.
        /// </summary>
        /// <returns>Object.</returns>
        [Scriptable]
        public Outcome CreateOutcome()
        {
            Outcome outcome = new();
            (Outcomes ??= new List<Outcome>()).Add(outcome);
            return outcome;
        }

        /// <summary>
        /// Default constructor for deserialization only.
        /// </summary>
        [Scriptable]
        public MissionTemplate()
        {
        }
    }

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
        /// Adds a goal index.
        /// </summary>
        /// <param name="goal">Goal index.</param>
        [Scriptable]
        public void AddGoal(int goal) => (Goals ??= new List<int>()).Add(goal);

        /// <summary>
        /// Output of mission as lua code.
        /// </summary>
        [Scriptable]
        public string? Next { get; set; }
    }
}
