// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.Test
{
    public class TestLoggerFactory : ILoggerFactory
    {
        private readonly TestSink _sink;
        private readonly bool _enabled;

        public TestLoggerFactory(TestSink sink, bool enabled)
        {
            _sink = sink;
            _enabled = enabled;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(categoryName, _sink, _enabled);
        }

        public void AddProvider(ILoggerProvider provider)
        {
        }
    }
}