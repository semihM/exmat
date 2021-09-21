using System;
#if DEBUG
using System.Diagnostics;
#endif
using ExMat.VM;

namespace ExMat.Exceptions
{
    /// <summary>
    /// <see cref="ExVM"/> exception class
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    internal sealed class ExRuntimeException : ExException
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
    }
}
