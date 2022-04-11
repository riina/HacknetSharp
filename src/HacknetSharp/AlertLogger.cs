using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace HacknetSharp
{
    /// <summary>
    /// Provides basic formatted logging to console output.
    /// </summary>
    public class AlertLogger : ILogger
    {
        /// <summary>
        /// Stores configuration properties for <see cref="AlertLogger"/>.
        /// </summary>
        public class Config
        {
            /// <summary>
            /// Enabled log levels.
            /// </summary>
            public HashSet<LogLevel> EnabledLevels { get; set; }

            /// <summary>
            /// Creates a new instance of <see cref="Config"/> with the specified log levels.
            /// </summary>
            /// <param name="enabledLevels">Enabled log levels.</param>
            public Config(params LogLevel[] enabledLevels)
            {
                EnabledLevels = new HashSet<LogLevel>(enabledLevels);
            }

            /// <summary>
            /// Creates a new instance of <see cref="Config"/> with the specified log levels.
            /// </summary>
            /// <param name="enabledLevels">Enabled log levels.</param>
            public Config(IEnumerable<LogLevel> enabledLevels)
            {
                EnabledLevels = new HashSet<LogLevel>(enabledLevels);
            }
        }


        private readonly Config _config;

        /// <summary>
        /// Creates an instance of <see cref="AlertLogger"/> with the specified configuration.
        /// </summary>
        /// <param name="config">Log configuration.</param>
        public AlertLogger(Config config)
        {
            _config = config;
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var alertFmt = Util.FormatAlert(logLevel.ToString(), $"[{eventId.Id,2}: {logLevel,-12}]",
                formatter(state, exception));
            Console.Write(alertFmt.Insert(0, '\n').ToString());
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel) => _config.EnabledLevels.Contains(logLevel);

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state) => default!;
    }
}
