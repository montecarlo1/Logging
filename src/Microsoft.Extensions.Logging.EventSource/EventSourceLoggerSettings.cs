// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.EventSource
{
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
    }
}
