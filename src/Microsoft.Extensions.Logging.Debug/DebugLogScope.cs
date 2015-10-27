// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

#if NET451
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
#else
using System.Threading;
#endif

namespace Microsoft.Extensions.Logging.Debug
{
    public class DebugLogScope
    {
        private readonly string _name;
        private readonly object _state;

        internal DebugLogScope(string name, object state)
        {
            _name = name;
            _state = state;
        }

        public DebugLogScope Parent { get; private set; }

#if NET451
        private static string FieldKey = typeof(DebugLogScope).FullName + ".Value";
        public static DebugLogScope Current
        {
            get
            {
                var handle = CallContext.LogicalGetData(FieldKey) as ObjectHandle;
                if (handle == null)
                {
                    return default(DebugLogScope);
                }

                return (DebugLogScope)handle.Unwrap();
            }
            set
            {
                CallContext.LogicalSetData(FieldKey, new ObjectHandle(value));
            }
        }
#else
        private static AsyncLocal<DebugLogScope> _value = new AsyncLocal<DebugLogScope>();
        public static DebugLogScope Current
        {
            set
            {
                _value.Value = value;
            }
            get
            {
                return _value.Value;
            }
        }
#endif

        public static IDisposable Push(string name, object state)
        {
            var temp = Current;
            Current = new DebugLogScope(name, state);
            Current.Parent = temp;

            return new DisposableScope();
        }

        public override string ToString()
        {
            return _state?.ToString();
        }

        private class DisposableScope : IDisposable
        {
            public void Dispose()
            {
                Current = Current.Parent;
            }
        }
    }
}
