﻿using System.Diagnostics;

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

        public ExInstr(OPC o, long a0, long a1, long a2, long a3)
        {
            op = o;
            arg0 = a0;
            arg1 = a1;
            arg2 = a2;
            arg3 = a3;
        }
        public string GetDebuggerDisplay()
        {
            return op.ToString() + ": " + arg0 + ", " + arg1 + ", " + arg2 + ", " + arg3;
        }
    }
}
