// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;

namespace Microsoft.Extensions.Logging.EventSource
{
    /// <summary>
    /// A logger that writes messages to EventSource instance.
    /// </summary>
    /// <remarks>
    /// On Windows platforms EventSource will deliver messages using Event Tracing for Windows (ETW) events.
    /// On Linux EventSource will use LTTng (http://lttng.org) to deliver messages.
    /// </remarks>
    public class EventSourceLogger : ILogger
    {
        private static IDictionary<LogLevel, EventLevel> LogLevel2EventLevel = new Dictionary<LogLevel, EventLevel>
        {
            { LogLevel.Critical, EventLevel.Critical },
            { LogLevel.Error, EventLevel.Error },
            { LogLevel.Warning, EventLevel.Warning },
            { LogLevel.Information, EventLevel.Informational },
            { LogLevel.Debug, EventLevel.Verbose },
            { LogLevel.Trace, EventLevel.Verbose }
        };

        private readonly string _name;
        private readonly EventSourceLoggerSettings _settings;
        private System.Diagnostics.Tracing.EventSource _eventSource;
        private IFormatProvider _formatProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourceLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        public EventSourceLogger(string name)
            : this(name, settings: new EventSourceLoggerSettings())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourceLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        /// <param name="settings">The <see cref="EventSourceLoggerSettings"/>.</param>
        public EventSourceLogger(string name, EventSourceLoggerSettings settings)
        {
            _name = string.IsNullOrEmpty(name) ? nameof(EventSourceLogger) : name;
            _settings = settings;
            _eventSource = new System.Diagnostics.Tracing.EventSource(_settings.EventSourceName);
            _formatProvider = _settings.FormatProvider == null ? CultureInfo.CurrentCulture : _settings.FormatProvider;
        }

        /// <inheritdoc />
        public IDisposable BeginScopeImpl(object state)
        {
            return new NoopDisposable();
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return _settings.Filter == null || _settings.Filter(_name, logLevel);
        }

        /// <inheritdoc />
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> messageFormatter)
        {
            if (logLevel == LogLevel.None || !IsEnabled(logLevel))
            {
                return;
            }

            if (messageFormatter == null)
            {
                throw new ArgumentNullException(nameof(messageFormatter));
            }

            string eventName;
            EventOpcode opCode;
            Conventions.GetEventNameAndOpCode(eventId, out eventName, out opCode);

            string message = messageFormatter(state, exception);

            EventKeywords keywords;
            IDictionary<string, string> dataBag = Conventions.GetPrimitiveStateData(state, _formatProvider, message, out keywords);

            EventLevel eventLevel = EventSourceLogger.LogLevel2EventLevel[logLevel];            

            EventSourceOptions eventOptions = new EventSourceOptions
            {
                Keywords = keywords,
                Level = eventLevel,
                Opcode = opCode
            };
            _eventSource.Write(eventName, eventOptions, new { dataBag = dataBag });
        }

        private class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
