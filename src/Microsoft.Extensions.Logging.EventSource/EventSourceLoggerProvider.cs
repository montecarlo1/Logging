// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.EventSource
{
    /// <summary>
    /// The provider for the <see cref="EventSourceLogger"/>.
    /// </summary>
    public class EventSourceLoggerProvider : ILoggerProvider
    {
        private readonly EventSourceLoggerSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourceLoggerProvider"/> class.
        /// </summary>
        public EventSourceLoggerProvider()
            : this(settings: null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventSourceLoggerProvider"/> class.
        /// </summary>
        /// <param name="settings">The <see cref="EventSourceLoggerSettings"/>.</param>
        public EventSourceLoggerProvider(EventSourceLoggerSettings settings)
        {
            _settings = settings;
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string name)
        {
            return new EventSourceLogger(name, _settings ?? new EventSourceLoggerSettings());
        }

        public void Dispose()
        {
        }
    }
}
