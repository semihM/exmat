using System;
using System.Diagnostics;
using ExMat.Objects;

namespace ExMat.OPs
{
    public enum BitOP
    {
        AND,
        OR = 2,
        XOR,
        SHIFTL,
        SHIFTR
    }

    public enum CmpOP
    {
        GRT,
        GET = 2,
        LST,
        LET
    }

    public enum OPC
    {
        LINE,
        LOAD,
        DLOAD,
        LOAD_INT,
        LOAD_FLOAT,
        LOAD_BOOL,
        UNLOAD,
        CALL,
        PREPCALL,
        PREPCALLK,
        CALL_TAIL,
        GETK,
        MOVE,
        DELETE,
        SET,
        GET,
        EQ,
        NEQ,
        DEC,
        DECL,
        CMP,
        EXISTS,
        INSTANCEOF,
        TRAPPOP,
        ARRAY_APPEND,
        RETURN,
        LOAD_NULL,
        LOAD_ROOT,
        DMOVE,
        JMP,
        JCMP,
        JZ,
        SETOUTER,
        GETOUTER,
        NEW_OBJECT,
        NEWSLOT,
        NEWSLOTA,
        MOD,
        CMP_ARTH,
        INC,
        INCL,
        PINC,
        MLT,
        ADD,
        PINCL,
        SUB,
        BITWISE,
        DIV,
        EXP,
        AND,
        OR,
        NEGATE,
        NOT,
        BNOT,
        CLOSURE,
        FOREACH,
        POSTFOREACH,
        TYPEOF,
        GETBASE,
        RETURNBOOL,
        LOAD_SPACE,
        JZS,
        RETURNMACRO,
        MMLT,
        TRANSPOSE,
        DEFAULT,
        CARTESIAN,
        LOAD_COMPLEX,
        CLOSE = 984
    }

    public enum ExNOT
    {
        DICT,
        ARRAY,
        CLASS,
        RULE
    }

    public enum ArrayAType
    {
        INVALID = -1,
        STACK,
        LITERAL,
        INTEGER,
        FLOAT,
        BOOL
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExInstr : IDisposable
    {
        public OPC op;
        public ExObject arg0;
        public ExObject arg2;
        public ExObject arg3;
        public long arg1;
        private bool disposedValue;

        public ExInstr() { }
        public string GetDebuggerDisplay()
        {
            if (op == OPC.LINE)
            {
                return "LINE";
            }

            return op.ToString() + ": " + arg0.GetInt() + ", " + arg1 + ", " + arg2.GetInt() + ", " + arg3.GetInt();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    arg0.Dispose();
                    arg2.Dispose();
                    arg3.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExInstr()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExTrap
    {
        public int _sbase;
        public int _ssize;
        public ExInstr instr;
        public int _target;

        public ExTrap() { }
        public ExTrap(ExTrap e) { _sbase = e._sbase; _ssize = e._ssize; instr = e.instr; _target = e._target; }
        private string GetDebuggerDisplay()
        {
            return "TRAP(" + instr.GetDebuggerDisplay() + "): " + _sbase + ", " + _ssize + ", " + _target;
        }
    }

    public enum ExNewSlotFlag
    {
        ATTR = 0x01,
        STATIC = 0x02
    }
}
