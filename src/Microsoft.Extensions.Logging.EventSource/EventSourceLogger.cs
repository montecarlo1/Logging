// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging.EventSource.Internal;

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
#if !NET451
        private System.Diagnostics.Tracing.EventSource _eventSource;
#endif

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
#if NET451
            if ((settings.DataFormat & LogDataFormat.PropertyBag) != 0)
            {
                throw new ArgumentException("Property bag data format is not supported with .NET Framework 4.5 series", nameof(settings));
            }
#else
            _eventSource = new System.Diagnostics.Tracing.EventSource(_settings.EventSourceName);
#endif
        }

        /// <inheritdoc />
        public IDisposable BeginScopeImpl(object state)
        {
            // TODO: better implementation using ETW activities 
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

#if !NET451
            LogImplCore(logLevel, eventId, state, exception, messageFormatter);
#else
            LogImpl45(logLevel, eventId, state, exception, messageFormatter);
#endif
        }

#if !NET451
        private void LogImplCore<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> messageFormatter)
        {
            string eventName;
            EventOpcode opCode;
            Conventions.GetEventNameAndOpCode(eventId, out eventName, out opCode);

            string message = messageFormatter(state, exception);

            EventKeywords keywords = EventKeywords.None;
            IEnumerable<KeyValuePair<string, string>> dataBag = null;

            if ((_settings.DataFormat & LogDataFormat.PropertyBag) != 0)
            {
                dataBag = Conventions.GetDataBag(state, _settings.FormatProvider, message, exception, out keywords);
            }

            string jsonData = GetJsonData(eventId, state, exception);

            EventLevel eventLevel = EventSourceLogger.LogLevel2EventLevel[logLevel];            

            var eventOptions = new EventSourceOptions
            {
                Keywords = keywords,
                Level = eventLevel,
                Opcode = opCode
            };
            _eventSource.Write(eventName, eventOptions, new { data = dataBag, jsonData = jsonData });
        }
#else
        private void LogImpl45<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> messageFormatter)
        {
            Debug.Assert((_settings.DataFormat & LogDataFormat.PropertyBag) == 0, "Property bag format is not supported with 4.5 framework series");

            string jsonData = GetJsonData(state, exception);
        }
#endif
        private string GetJsonData<TState>(EventId eventId, TState state, Exception exception)
        {
            if ((_settings.DataFormat & LogDataFormat.JSON) != 0)
            {
                var data = new { id = eventId.Id, name = eventId.Name, state = state, exception = exception };
                return JsonConvert.SerializeObject(data);
            }
            else
            {
                return null;
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
