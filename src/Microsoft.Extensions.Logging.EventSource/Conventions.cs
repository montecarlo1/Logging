// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Reflection;
using System.Reflection.Metadata;

namespace Microsoft.Extensions.Logging.EventSource
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
        /// <returns>A dictionary of property values indexed by property name</returns>
        public static IDictionary<string, object> GetPrimitiveStateData(object state)
        {
            if (state == null)
            {
                return null;
            }

            var result = new Dictionary<string, object>();

            foreach (PropertyInfo p in state.GetType().GetProperties())
            {
                if (p.PropertyType == typeof(string) || p.PropertyType.GetTypeInfo().IsPrimitive || p.PropertyType == typeof(Guid))
                {
                    result[p.Name] = p.GetValue(state);
                }
            }

            return result;
        }
    }
}
