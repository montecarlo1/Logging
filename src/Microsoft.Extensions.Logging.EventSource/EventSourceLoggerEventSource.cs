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
    [EventSource(Name ="")]
    public sealed class EventSourceLoggerEventSource
    {

    }
#endif
}
