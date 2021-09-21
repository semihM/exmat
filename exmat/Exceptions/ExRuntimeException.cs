using System;
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
        /// <summary>
        /// Exception type
        /// </summary>
        public override ExExceptionType Type => ExExceptionType.RUNTIME;

        /// <summary>
        /// Exception constructor
        /// </summary>
        public ExRuntimeException()
        {
        }

        /// <summary>
        /// Exception constructor
        /// </summary>
        public ExRuntimeException(string message) : base(message)
        {
        }

        /// <summary>
        /// Exception constructor
        /// </summary>
        public ExRuntimeException(ExVM vm, string message) : base(vm, message)
        {
        }

        /// <summary>
        /// Exception constructor
        /// </summary>
        public ExRuntimeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Exception constructor
        /// </summary>
        public ExRuntimeException(ExVM vm, string message, Exception innerException) : base(vm, message, innerException)
        {
        }

        /// <summary>
        /// Exception constructor
        /// </summary>
        protected ExRuntimeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Exception constructor
        /// </summary>
        protected ExRuntimeException(ExVM vm, SerializationInfo info, StreamingContext context) : base(vm, info, context)
        {
        }
    }
}
