using Easy_Logger.Models;
using Easy_Logger.Providers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy_Logger.Loggers
{
    /// <summary>
    /// Logging endpoint that records to the console
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private readonly Func<ConsoleLoggerConfiguration> Configuration;
        private readonly string Source;

        /// <param name="source">Stores the source for the logger</param>
        /// <param name="configuration">A function to return the configuration for the logger</param>
        public ConsoleLogger(string source, Func<ConsoleLoggerConfiguration> configuration)
        {
            Configuration = configuration;
            Source = source;
        }

        /// <param name="source">Stores the source for the logger</param>
        /// <param name="logLevels">A function to return the log levels to record log entries for</param>
        /// <param name="ignoredMessages">Any log messages containing these strings will not be recorded</param>
        /// <param name="logLevelToColorMap">A dictionary containing the colors to ouput log messages in based on severity</param>
        public ConsoleLogger(string source, Func<LogLevel[]> logLevels, List<string> ignoredMessages, Dictionary<LogLevel, ConsoleColor> logLevelToColorMap)
        {
            Configuration = () => new ConsoleLoggerConfiguration()
            {
                LogLevels = logLevels(),
                IgnoredMessages = ignoredMessages,
                LogLevelToColorMap = logLevelToColorMap
            };

            Source = source;
        }

        /// <inheritdoc/>
        public IDisposable BeginScope<TState>(TState state) => default!;

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel) => Configuration().LogLevels.Contains(logLevel);

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel) == false)
                return;

            var entry = new LoggerEntry(formatter(state, exception))
            {
                Id = eventId,
                Severity = logLevel,
                Source = Source
            };

			var current = Configuration();

			if (current.IgnoredMessages.Any(x => entry.Message.Contains(x, StringComparison.OrdinalIgnoreCase)))
                return;

            if (current.Formatter == null)
            {
                if (current.UseColoredMessages)
                    Console.ForegroundColor = current.LogLevelToColorMap.ContainsKey(entry.Severity) ? current.LogLevelToColorMap[entry.Severity] : ConsoleColor.White;

                Console.WriteLine($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}; Severity={entry.Severity}; Source={entry.Source}");

                if (current.UseColoredMessages)
                    Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine(entry.Message);
            }
            else
            {
                if (current.UseColoredMessages)
                    Console.ForegroundColor = current.LogLevelToColorMap.ContainsKey(entry.Severity) ? current.LogLevelToColorMap[entry.Severity] : ConsoleColor.White;

                Console.Write(current.Formatter.Invoke(entry));

				if (current.UseColoredMessages)
					Console.ForegroundColor = ConsoleColor.Gray;
            }
        }
    }
}
