using System;
#if DEBUG
using System.Diagnostics;
#endif
using System.Runtime.Serialization;
using ExMat.VM;

namespace ExMat.Exceptions
{
    /// <summary>
    /// Exception types
    /// </summary>
    public enum ExExceptionType
    {
        /// <summary>
        /// Internal
        /// </summary>
        BASE,
        /// <summary>
        /// Compiler
        /// </summary>
        COMPILER,
        /// <summary>
        /// VM, runtime
        /// </summary>
        RUNTIME
    }

    /// <summary>
    /// Base class for exceptions
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExException : Exception
    {
        /// <summary>
        /// Exception type
        /// </summary>
        public virtual ExExceptionType Type => ExExceptionType.BASE;
        /// <summary>
        /// Exception constructor
        /// </summary>
        public ExException()
        {
        }

        /// <summary>
        /// Exception constructor
        /// </summary>
        public ExException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Exception constructor
        /// </summary>
        public ExException(ExVM vm, string message)
            : base(message)
        {
        }

        /// <summary>
        /// Exception constructor
        /// </summary>
        public ExException(ExVM vm, string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Exception constructor
        /// </summary>
        protected ExException(ExVM vm, SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Exception constructor
        /// </summary>
        protected ExException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        /// <summary>
        /// Exception constructor
        /// </summary>
        public ExException(string message, Exception innerException) : base(message, innerException)
        {
        }

#if DEBUG
        internal string GetDebuggerDisplay()
        {
            return "Exception: " + Type.ToString();
        }
#endif
    }
}
