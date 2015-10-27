// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Extensions.Logging.Debug
{
    /// <summary>
    /// A logger that writes messages in the debug output window only when a debugger is attached.
    /// </summary>
    public partial class DebugLogger : ILogger
    {
        private readonly Func<string, LogLevel, bool> _filter;
        private readonly string _name;
        private static readonly string _loglevelPadding = ": ";
        private static readonly string _messagePadding;
        private readonly bool _includeScopes;

        static DebugLogger()
        {
            var logLevelString = GetLogLevelString(LogLevel.Information);
            _messagePadding = new string(' ', logLevelString.Length + _loglevelPadding.Length);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        public DebugLogger(string name)
            : this(name, filter: null, includeScopes: false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DebugLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        /// <param name="filter">The function used to filter events based on the log level.</param>
        public DebugLogger(string name, Func<string, LogLevel, bool> filter, bool includeScopes)
        {
            _name = string.IsNullOrEmpty(name) ? nameof(DebugLogger) : name;
            _filter = filter;
            _includeScopes = includeScopes;
        }

        /// <inheritdoc />
        public IDisposable BeginScopeImpl(object state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return DebugLogScope.Push(_name, state);
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            // If the filter is null, everything is enabled
            // unless the debugger is not attached
            return Debugger.IsAttached &&
                (_filter == null || _filter(_name, logLevel));
        }

        /// <inheritdoc />
        public void Log(
            LogLevel logLevel,
            int eventId,
            object state,
            Exception exception,
            Func<object, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            string message;
            var values = state as ILogValues;
            if (formatter != null)
            {
                message = formatter(state, exception);
            }
            else if (values != null)
            {
                message = LogFormatter.FormatLogValues(values);
                if (exception != null)
                {
                    message += Environment.NewLine + exception;
                }
            }
            else
            {
                message = LogFormatter.Formatter(state, exception);
            }

            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            WriteMessage(logLevel, _name, eventId, message);
        }

        public virtual void WriteMessage(LogLevel logLevel, string logName, int eventId, string message)
        {
            // check if the message has any new line characters in it and provide the padding if necessary
            message = message.Replace(Environment.NewLine, Environment.NewLine + _messagePadding);
            var loglevelString = GetLogLevelString(logLevel);

            // Example:
            // info: ConsoleApp.Program[10]
            //       => Reqeust Id: 100 => Action 'Index selected => Product created
            //       Request received

            // loglevel
            DebugWrite(loglevelString);

            // loggername[eventid]
            DebugWriteLine(_loglevelPadding + logName + $"[{eventId}]");

            // scope ifn
            if (_includeScopes)
            {
                var scopeInformation = GetScopeInformation();
                if (!string.IsNullOrEmpty(scopeInformation))
                {
                    DebugWriteLine(_messagePadding + scopeInformation);
                }
            }

            DebugWriteLine(_messagePadding + message);
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Debug:
                    return "dbug";
                case LogLevel.Verbose:
                    return "verb";
                case LogLevel.Information:
                    return "info";
                case LogLevel.Warning:
                    return "warn";
                case LogLevel.Error:
                    return "fail";
                case LogLevel.Critical:
                    return "crit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        private string GetScopeInformation()
        {
            var current = DebugLogScope.Current;
            var output = new StringBuilder();
            string scopeLog = string.Empty;
            while (current != null)
            {
                if (output.Length == 0)
                {
                    scopeLog = $"=> {current}";
                }
                else
                {
                    scopeLog = $"=> {current} ";
                }

                output.Insert(0, scopeLog);
                current = current.Parent;
            }

            return output.ToString();
        }
    }
}
