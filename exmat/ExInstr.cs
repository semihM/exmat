#if DEBUG
using System.Diagnostics;
#endif

namespace ExMat.OPs
{
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public struct ExInstr
    {
        public ExOperationCode op;
        public long arg0;
        public long arg1;
        public long arg2;
        public long arg3;

        public ExInstr(ExInstr other)
        {
            op = other.op;
            arg0 = other.arg0;
            arg1 = other.arg1;
            arg2 = other.arg2;
            arg3 = other.arg3;
        }

        public ExInstr(ExOperationCode o, long a0, long a1, long a2, long a3)
        {
            op = o;
            arg0 = a0;
            arg1 = a1;
            arg2 = a2;
            arg3 = a3;
        }
#if DEBUG
        public string GetDebuggerDisplay()
        {
            return op.ToString() + ": " + arg0 + ", " + arg1 + ", " + arg2 + ", " + arg3;
        }
#endif
    }
}
