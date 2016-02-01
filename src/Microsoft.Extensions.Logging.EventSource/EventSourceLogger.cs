// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;

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
            Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.None || !IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            string eventName;
            EventOpcode opCode;
            Conventions.GetEventNameAndOpCode(eventId, out eventName, out opCode);

            IDictionary<string, object> stateData = Conventions.GetPrimitiveStateData(state);

            EventLevel eventLevel = EventSourceLogger.LogLevel2EventLevel[logLevel];

            EventKeywords keywords = EventKeywords.None;
            object candidateKeywords;
            if (stateData.TryGetValue("Keywords", out candidateKeywords) && candidateKeywords.GetType().IsEnum)
            {
                keywords = (EventKeywords) candidateKeywords;
            }

            string message = formatter(state, exception);
            _eventSource.Write()
        }

        // category '0' translates to 'None' in event log
        private void WriteMessage(string message, int eventId)
        {

            //if (message.Length <= EventLog.MaxMessageSize)
            //{
            //    EventLog.WriteEntry(message, eventLogEntryType, eventId, category: 0);
            //    return;
            //}

            //var startIndex = 0;
            //string messageSegment = null;
            //while (true)
            //{
            //    // Begin segment
            //    // Example: An error occu...
            //    if (startIndex == 0)
            //    {
            //        messageSegment = message.Substring(startIndex, _beginOrEndMessageSegmentSize) + ContinuationString;
            //        startIndex += _beginOrEndMessageSegmentSize;
            //    }
            //    else
            //    {
            //        // Check if rest of the message can fit within the maximum message size
            //        // Example: ...esponse stream
            //        if ((message.Length - (startIndex + 1)) <= _beginOrEndMessageSegmentSize)
            //        {
            //            messageSegment = ContinuationString + message.Substring(startIndex);
            //            EventLog.WriteEntry(messageSegment, eventLogEntryType, eventId, category: 0);
            //            break;
            //        }
            //        else
            //        {
            //            // Example: ...rred while writ...
            //            messageSegment =
            //                ContinuationString
            //                + message.Substring(startIndex, _intermediateMessageSegmentSize)
            //                + ContinuationString;
            //            startIndex += _intermediateMessageSegmentSize;
            //        }
            //    }

            //    EventLog.WriteEntry(messageSegment, eventLogEntryType, eventId, category: 0);
            //}
        }

        private EventLogEntryType GetEventLogEntryType(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Information:
                case LogLevel.Debug:
                case LogLevel.Trace:
                    return EventLogEntryType.Information;
                case LogLevel.Warning:
                    return EventLogEntryType.Warning;
                case LogLevel.Critical:
                case LogLevel.Error:
                    return EventLogEntryType.Error;
                default:
                    return EventLogEntryType.Information;
            }
        }

        private class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }

        
    }
}
