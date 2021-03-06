using System;
#if DEBUG
using System.Diagnostics;
#endif
using ExMat.VM;

namespace ExMat.Exceptions
{
    /// <summary>
    /// <see cref="Compiler.ExCompiler"/> exception class
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    internal sealed class ExCompilerException : ExException
    {
        public override ExExceptionType Type => ExExceptionType.COMPILER;

        public ExCompilerException()
        {
        }

        public ExCompilerException(string message) : base(message)
        {
        }

        public ExCompilerException(ExVM vm, string message) : base(vm, message)
        {
        }

        public ExCompilerException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public ExCompilerException(ExVM vm, string message, Exception innerException) : base(vm, message, innerException)
        {
        }
    }
}
