#if DEBUG
using System.Diagnostics;
#endif

namespace ExMat.OPs
{
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    internal sealed class ExInstr
    {
        public ExOperationCode op;
        public long arg0;
        public long arg1;
        public long arg2;
        public long arg3;

        public ExInstr() { }

        public ExInstr(ExInstr other)
        {
            op = other.op;
            arg0 = other.arg0;
            arg1 = other.arg1;
            arg2 = other.arg2;
            arg3 = other.arg3;
        }

#if DEBUG
        public string GetDebuggerDisplay()
        {
            return op.ToString() + ": " + arg0 + ", " + arg1 + ", " + arg2 + ", " + arg3;
        }
#endif
    }
}
