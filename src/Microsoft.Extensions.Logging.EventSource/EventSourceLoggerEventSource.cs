// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;

namespace Microsoft.Extensions.Logging.EventSource.Internal
{
#if NET451
    [EventSource(Name ="Microsoft-Extensions-Logging")]
    public sealed class EventSourceLoggerEventSource: System.Diagnostics.Tracing.EventSource
    {
        private const int CriticalEventId = 1;
        private const int ErrorEventId = 2;
        private const int WarningEventId = 3;
        private const int InformationalEventId = 4;
        private const int VerboseEventId = 5;

        [Event(CriticalEventId, Level = EventLevel.Critical, Message = "{0}")]
        public void Critical(string message, string data)
        {
            WriteEvent(CriticalEventId, message, data);
        }

        [Event(ErrorEventId, Level = EventLevel.Error, Message = "{0}")]
        public void Error(string message, string data)
        {
            WriteEvent(ErrorEventId, message, data);
        }

        [Event(WarningEventId, Level = EventLevel.Warning, Message = "{0}")]
        public void Warning(string message, string data)
        {
            WriteEvent(WarningEventId, message, data);
        }

        [Event(InformationalEventId, Level = EventLevel.Informational, Message = "{0}")]
        public void Informational(string message, string data)
        {
            WriteEvent(InformationalEventId, message, data);
        }

        [Event(VerboseEventId, Level = EventLevel.Verbose, Message = "{0}")]
        public void Verbose(string message, string data)
        {
            WriteEvent(VerboseEventId, message, data);
        } 
    }
#endif
}
