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
        ADD,
        SUB,
        MLT,
        DIV,
        MOD,
        BITWISE,
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
        ARRAY_APPEND,
        CMP_ARTH,
        INC,
        INCL,
        PINC,
        PINCL,
        DEC,
        DECL,
        CMP,
        EXISTS,
        INSTANCE_OF,
        AND,
        OR,
        NEGATE,
        NOT,
        BNOT,
        TRAPPOP,
        CLOSURE,
        FOREACH,
        POSTFOREACH,
        TYPEOF,
        INSTANCEOF,
        GETBASE,
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
    public class ExInstr
    {
        public OPC op;
        public ExInt arg0;
        public ExInt arg2;
        public ExInt arg3;
        public int arg1;

        public ExInstr() { }
        public string GetDebuggerDisplay()
        {
            if (op == OPC.LINE)
                return "LINE";
            return op.ToString() + ": " + arg0.GetInt() + ", " + arg1 + ", " + arg2.GetInt() + ", " + arg3.GetInt();
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
