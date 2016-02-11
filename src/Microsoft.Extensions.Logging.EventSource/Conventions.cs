// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;

using Microsoft.Extensions.Logging.Internal;
using System.Diagnostics;

namespace Microsoft.Extensions.Logging.EventSource.Internal
{
    internal static class Conventions
    {
        /// <summary>
        /// Computes ETW event name and opcode based on event id and message passed to the logger.
        /// </summary>
        /// <remarks>
        /// The ETW event name is taken from event id's Name property if the latter is not empty (the recommended practice is to set the name explicitly).
        /// If the Name property is not set, the event name is serialized value of the (numeric) event id. 
        /// 
        /// The EventSourceLogger recognizes the names in the {event name}/{opCode} format  such as 'Request/Start'. 
        /// If this convention is used, the part after the backslash character will be interpreted as ETW opcode.
        /// </remarks>
        public static void GetEventNameAndOpCode(EventId id, out string name, out EventOpcode opCode)
        {
            string candidateName = id.Name;
            if (string.IsNullOrEmpty(candidateName))
            {
                candidateName = id.Id.ToString(CultureInfo.InvariantCulture);
            }

            opCode = EventOpcode.Info;

            int i = candidateName.LastIndexOf('/');
            if (i < 0)
            {
                name = candidateName;
            }
            else
            {
                name = candidateName.Substring(0, i);
                Enum.TryParse<EventOpcode>(candidateName.Substring(i + 1), out opCode);
            }
        }

        /// <summary>
        /// Gets the values of all primitive, string and Guid properties of the passed state object
        /// </summary>
        /// <param name="state">State object to be examined</param>
        /// <param name="formatProvider">The format provider to use for data serialization</param>
        /// <param name="message">Event message</param>
        /// <param name="exception">Exception associated with the event (if any)</param>
        /// <param name="keywords">Event keywords extracted from the state</param>
        /// <returns>A list of property name-value pairs</returns>
        /// <remarks>Value extraction will be attempted only if the passed state is a FormattedLogValues instance, otherwise null will be returned</remarks>
        public static IEnumerable<KeyValuePair<string, string>> GetDataBag(
            object state, 
            IFormatProvider formatProvider, 
            string message, 
            Exception exception, 
            out EventKeywords keywords)
        {
            keywords = EventKeywords.None;

            var logValues = state as FormattedLogValues;
            if (logValues == null)
            {
                return null;
            }

            var result = new Dictionary<string, string>();
            foreach (KeyValuePair<string, object> kvPair in logValues)
            {
                EventKeywords tempKeywords;
                if (TryExtractKeywords(kvPair, out tempKeywords))
                {
                    keywords = tempKeywords;
                }

                string formatString = "{0}";
                if (kvPair.Value is DateTime || kvPair.Value is DateTimeOffset)
                {
                    formatString = "{0:o}"; // Ensure that we use use ISO 8601 time format
                }

                string serializedValue = kvPair.Value == null ? string.Empty : string.Format(formatProvider, formatString, kvPair.Value);
                result.Add(kvPair.Key, serializedValue);
            }

            if (!string.IsNullOrEmpty(message))
            {
                result["Message"] = message;
            }
            if (exception != null)
            {
                result["Exception"] = exception.ToString();
            }

            return result;
        }

        public static EventKeywords GetKeywords(object state)
        {
            var logValues = state as FormattedLogValues;
            if (logValues == null)
            {
                return EventKeywords.None;
            }

            foreach (KeyValuePair<string, object> kvPair in logValues)
            {
                EventKeywords keywords;
                if (TryExtractKeywords(kvPair, out keywords))
                {
                    return keywords;
                }
            }

            return EventKeywords.None;
        }

        private static bool TryExtractKeywords(KeyValuePair<string, object> kvPair, out EventKeywords keywords)
        {
            if ("Keywords".Equals(kvPair.Key, StringComparison.OrdinalIgnoreCase) && kvPair.Value != null && kvPair.Value is long)
            {
                keywords = (EventKeywords)kvPair.Value;
                return true;
            }
            else
            {
                keywords = EventKeywords.None;
                return false;
            }
        }
    }
}
