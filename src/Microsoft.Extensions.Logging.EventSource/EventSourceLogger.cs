// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace Microsoft.Extensions.Logging.EventSourceLogger
{
    /// <summary>
    /// A logger that writes messages to EventSource instance.
    /// </summary>
    /// <remarks>
    /// On Windows platforms EventSource will deliver messages using Event Tracing for Windows (ETW) events.
    /// On Linux EventSource will use LTTng (http://lttng.org) to deliver messages.
    /// </remarks>
    internal class EventSourceLogger : ILogger
    {
        private int _factoryID;
        private static int s_activityIds;

        public EventSourceLogger(string categoryName, int factoryID, EventSourceLogger next)
        {
            CategoryName = categoryName;
            Level = LoggingEventSource.LoggingDisabled;     // Default is to turn off logging
            _factoryID = factoryID;
            Next = next;
        }

        public readonly string CategoryName;
        public LogLevel Level;
        public readonly EventSourceLogger Next;     // Loggers created by a single provider form a linked list

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= Level;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            // See if they want the formatted message
            if (LoggingEventSource.Instance.IsEnabled(EventLevel.Critical, LoggingEventSource.Keywords.FormattedMessage))
            {
                string message = formatter(state, exception);
                LoggingEventSource.Instance.FormattedMessage(
                    logLevel,
                    _factoryID,
                    CategoryName,
                    eventId.ToString(),
                    message);
            }

#if !NO_EVENTSOURCE_COMPLEX_TYPE_SUPPORT
            // See if they want the message as its component parts.  
            if (LoggingEventSource.Instance.IsEnabled(EventLevel.Critical, LoggingEventSource.Keywords.Message))
            {
                ExceptionInfo exceptionInfo = GetExceptionInfo(exception);
                IEnumerable<KeyValuePair<string, string>> arguments = GetProperties(state);

                LoggingEventSource.Instance.Message(
                    logLevel,
                    _factoryID,
                    CategoryName,
                    eventId.ToString(),
                    exceptionInfo,
                    arguments);
            }
#endif
            // See if they want the json message
            if (LoggingEventSource.Instance.IsEnabled(EventLevel.Critical, LoggingEventSource.Keywords.JsonMessage))
            {
                string exceptionJson = "{}";
                if (exception != null)
                {
                    ExceptionInfo exceptionInfo = GetExceptionInfo(exception);
                    var exceptionInfoData = new KeyValuePair<string, string>[] {
                                new KeyValuePair<string, string>("TypeName", exceptionInfo.TypeName),
                                new KeyValuePair<string, string>("Message", exceptionInfo.Message),
                                new KeyValuePair<string, string>("HResult", exceptionInfo.HResult.ToString()),
                                new KeyValuePair<string, string>("VerboseMessage", exceptionInfo.VerboseMessage),
                            };
                    exceptionJson = ToJson(exceptionInfoData);
                }
                IEnumerable<KeyValuePair<string, string>> arguments = GetProperties(state);
                LoggingEventSource.Instance.MessageJson(
                    logLevel,
                    _factoryID,
                    CategoryName,
                    eventId.ToString(),
                    exceptionJson,
                    ToJson(arguments));
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (!IsEnabled(LogLevel.Critical))
            {
                return NoopDisposable.Instance;
            }

            var id = Interlocked.Increment(ref s_activityIds);

            // If JsonMessage is on, use JSON format
            if (LoggingEventSource.Instance.IsEnabled(EventLevel.Critical, LoggingEventSource.Keywords.JsonMessage))
            {
                IEnumerable<KeyValuePair<string, string>> arguments = GetProperties(state);
                LoggingEventSource.Instance.ActivityJsonStart(id, _factoryID, CategoryName, ToJson(arguments));
                return new ActivityScope(CategoryName, id, _factoryID, true);
            }
            else
            {
#if !NO_EVENTSOURCE_COMPLEX_TYPE_SUPPORT
                IEnumerable<KeyValuePair<string, string>> arguments = GetProperties(state);
                LoggingEventSource.Instance.ActivityStart(id, _factoryID, CategoryName, arguments);
#else
                LoggingEventSource.Instance.ActivityStart(id, _factoryID, CategoryName);
#endif
                return new ActivityScope(CategoryName, id, _factoryID, false);
            }
        }

        /// <summary>
        /// ActivityScope is just a IDisposable that knows how to send the ActivityStop event when it is 
        /// desposed.  It is part of the BeginScope() support.  
        /// </summary>
        private class ActivityScope : IDisposable
        {
            private string _categoryName;
            private int _activityID;
            private int _factoryID;
            private bool _isJsonStop;

            public ActivityScope(string categoryName, int activityID, int factoryID, bool isJsonStop)
            {
                _categoryName = categoryName;
                _activityID = activityID;
                _factoryID = factoryID;
                _isJsonStop = isJsonStop;
            }

            public void Dispose()
            {
                if (_isJsonStop)
                {
                    LoggingEventSource.Instance.ActivityJsonStop(_activityID, _factoryID, _categoryName);
                }
                else
                {
                    LoggingEventSource.Instance.ActivityStop(_activityID, _factoryID, _categoryName);
                }
            }
        }

        private class NoopDisposable : IDisposable
        {
            public static NoopDisposable Instance = new NoopDisposable();

            public void Dispose()
            {
            }
        }

        /// <summary>
        /// 'serializes' a given exception into an ExceptionInfo (that EventSource knows how to serialize)
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        private ExceptionInfo GetExceptionInfo(Exception exception)
        {
            if (exception == null)
            {
                return null;
            }

            var exceptionInfo = new ExceptionInfo();
            if (exception != null)
            {
                exceptionInfo.TypeName = exception.GetType().FullName;
                exceptionInfo.Message = exception.Message;
                exceptionInfo.HResult = exception.HResult;
                exceptionInfo.VerboseMessage = exception.ToString();
            }
            return exceptionInfo;
        }

        /// <summary>
        /// Converts an ILogger state object into a set of key-value pairs (That can be send to a EventSource) 
        /// </summary>
        private IEnumerable<KeyValuePair<string, string>> GetProperties(object state)
        {
            var arguments = new List<KeyValuePair<string, string>>();
            var asKeyValues = state as IEnumerable<KeyValuePair<string, object>>;
            if (asKeyValues != null)
            {
                foreach (var keyValue in asKeyValues)
                    arguments.Add(new KeyValuePair<string, string>(keyValue.Key, keyValue.Value.ToString()));
            }
            return arguments;
        }

        private string ToJson(IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            StringWriter sw = new StringWriter();
            JsonTextWriter writer = new JsonTextWriter(sw);
            writer.DateFormatString = "O"; // ISO 8601

            writer.WriteStartObject();
            foreach (var keyValue in keyValues)
            {
                writer.WritePropertyName(keyValue.Key, true);
                writer.WriteValue(keyValue.Value);
            }
            writer.WriteEndObject();
            return sw.ToString();
        }
    }
}
