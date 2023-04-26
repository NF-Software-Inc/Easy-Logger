﻿using Easy_Logger.Interfaces;
using Microsoft.Extensions.Logging;
using System;

namespace Easy_Logger.Models
{
    /// <summary>
    /// Default implementation of <see cref="ILoggerEntry"/>
    /// </summary>
    internal class LoggerEntry : ILoggerEntry
    {
        public LoggerEntry()
        {
            Message = string.Empty;
        }

        public LoggerEntry(string message)
        {
            Message = message;
        }

        /// <inheritdoc/>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <inheritdoc/>
        public string? Source { get; set; }

        /// <inheritdoc/>
        public string Message { get; set; }

        /// <inheritdoc/>
        public LogLevel Severity { get; set; }

        /// <inheritdoc/>
        public EventId? Id { get; set; }
    }
}
