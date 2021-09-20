﻿using System;
#if DEBUG
using System.Diagnostics;
#endif
using System.Runtime.Serialization;
using ExMat.VM;

namespace ExMat.Exceptions
{
    /// <summary>
    /// <see cref="ExVM"/> exception class
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExRuntimeException : ExException
    {
        public override ExExceptionType Type => ExExceptionType.RUNTIME;

        public ExRuntimeException()
        {
        }

        public ExRuntimeException(string message) : base(message)
        {
        }

        public ExRuntimeException(ExVM vm, string message) : base(vm, message)
        {
        }

        public ExRuntimeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ExRuntimeException(ExVM vm, string message, Exception innerException) : base(vm, message, innerException)
        {
        }

        protected ExRuntimeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        protected ExRuntimeException(ExVM vm, SerializationInfo info, StreamingContext context) : base(vm, info, context)
        {
        }
    }
}
