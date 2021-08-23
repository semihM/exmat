using System.Diagnostics;

namespace ExMat.OPs
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExInstr
    {
        public OPC op;
        public long arg0;
        public long arg1;
        public long arg2;
        public long arg3;

        public ExInstr() { }
        public string GetDebuggerDisplay()
        {
            return op.ToString() + ": " + arg0 + ", " + arg1 + ", " + arg2 + ", " + arg3;
        }
    }
}
