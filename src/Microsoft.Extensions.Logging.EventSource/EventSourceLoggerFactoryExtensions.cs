// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging.EventSource;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for the <see cref="ILoggerFactory"/> class.
    /// </summary>
    public static class EventSourceLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds an event logger that is enabled for <see cref="LogLevel"/>.Information or higher.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        public static ILoggerFactory AddEventSourceLoger(this ILoggerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return AddEventSourceLogger(factory, LogLevel.Information);
        }

        /// <summary>
        /// Adds an event logger that is enabled for <see cref="LogLevel"/>s of minLevel or higher.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        /// <param name="minLevel">The minimum <see cref="LogLevel"/> to be logged</param>
        public static ILoggerFactory AddEventSourceLogger(this ILoggerFactory factory, LogLevel minLevel)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return AddEventSourceLog(factory, new EventSourceLoggerSettings()
            {
                Filter = (_, logLevel) => logLevel >= minLevel
            });
        }

        /// <summary>
        /// Adds an event logger. Use <paramref name="settings"/> to enable logging for specific <see cref="LogLevel"/>s.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        /// <param name="settings">The <see cref="EventSourceLoggerSettings"/>.</param>
        public static ILoggerFactory AddEventSourceLog(
            this ILoggerFactory factory,
            EventSourceLoggerSettings settings)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            factory.AddProvider(new EventSourceLoggerProvider(settings));
            return factory;
        }
    }
}
