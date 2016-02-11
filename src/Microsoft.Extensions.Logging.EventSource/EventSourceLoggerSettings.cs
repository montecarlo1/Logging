// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.Extensions.Logging.EventSource
{
    /// <summary>
    /// Used to indicate the format of the log data emitted by <see cref="EventSourceLogger"/>
    /// </summary>    
    [Flags]
    public enum LogDataFormat
    {
        /// <summary>
        /// Emit log data as <![CDATA[IEnumerable<KeyValuePair<string,string>>]]>
        /// </summary>
        PropertyBag = 0x1,

        /// <summary>
        /// Emit log data as JSON
        /// </summary>
        JSON = 0x2
    }

    /// <summary>
    /// Settings for <see cref="EventSourceLogger"/>.
    /// </summary>
    public class EventSourceLoggerSettings
    {
        /// <summary>
        /// Name of the EventSource that will be used by the logger.
        /// </summary>
        public string EventSourceName { get; set; }

        /// <summary>
        /// The function used to filter events based on the log level.
        /// </summary>
        public Func<string, LogLevel, bool> Filter { get; set; }

        /// <summary>
        /// The format provider used to format the data being logged.
        /// </summary>
        public IFormatProvider FormatProvider { get; set; }

        public LogDataFormat DataFormat { get; set; }

        public EventSourceLoggerSettings()
        {
            FormatProvider = CultureInfo.CurrentCulture;
            DataFormat = LogDataFormat.JSON;
        }
    }
}
