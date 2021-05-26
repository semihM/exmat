using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using ExMat.Class;
using ExMat.Closure;
using ExMat.FuncPrototype;
using ExMat.Objects;

namespace ExMat
{
    public static class ExMat
    {
        public const char _END = '\0';

        public static readonly string _VERSION = "0.0.1";

        public static readonly string _CONSTRUCTOR = "init";

        public static readonly string _THIS = "this";

        public static readonly string _VARGS = "vargs";

        public static ExBaseType GetRawType(ExObjType typ)
        {
            return (ExBaseType)((int)typ & 0x00FFFFFF);
        }
    }

    public enum ExBaseType
    {
        NULL = 1 << 0,
        INTEGER = 1 << 1,
        FLOAT = 1 << 2,
        COMPLEX = 1 << 3,
        BOOL = 1 << 4,
        STRING = 1 << 5,
        SPACE = 1 << 6,
        ARRAY = 1 << 7,
        USERDATA = 1 << 8,
        CLOSURE = 1 << 9,
        NATIVECLOSURE = 1 << 10,
        USERPTR = 1 << 11,
        THREAD = 1 << 12,
        FUNCINFO = 1 << 13,
        CLASS = 1 << 14,
        INSTANCE = 1 << 15,
        WEAKREF = 1 << 16,
        OUTER = 1 << 17,
        FUNCPRO = 1 << 18,
        DICT = 1 << 19,
        DEFAULT = 1 << 20
    }

    public enum ExObjFlag
    {
        CANBEFALSE = 0x01000000,
        NUMERIC = 0x02000000,
        COUNTREFERENCES = 0x04000000,
        DELEGABLE = 0x08000000
    }

    public enum ExObjType
    {
        DEFAULT = ExBaseType.DEFAULT | ExObjFlag.CANBEFALSE,
        NULL = ExBaseType.NULL | ExObjFlag.CANBEFALSE,
        INTEGER = ExBaseType.INTEGER | ExObjFlag.NUMERIC | ExObjFlag.CANBEFALSE,
        FLOAT = ExBaseType.FLOAT | ExObjFlag.NUMERIC | ExObjFlag.CANBEFALSE,
        COMPLEX = ExBaseType.COMPLEX | ExObjFlag.NUMERIC | ExObjFlag.CANBEFALSE,
        BOOL = ExBaseType.BOOL | ExObjFlag.CANBEFALSE,
        STRING = ExBaseType.STRING,

        SPACE = ExBaseType.SPACE | ExObjFlag.COUNTREFERENCES,
        ARRAY = ExBaseType.ARRAY | ExObjFlag.COUNTREFERENCES,
        DICT = ExBaseType.DICT | ExObjFlag.COUNTREFERENCES | ExObjFlag.DELEGABLE,

        CLOSURE = ExBaseType.CLOSURE | ExObjFlag.COUNTREFERENCES,
        NATIVECLOSURE = ExBaseType.NATIVECLOSURE | ExObjFlag.COUNTREFERENCES,

        CLASS = ExBaseType.CLASS | ExObjFlag.COUNTREFERENCES,
        INSTANCE = ExBaseType.INSTANCE | ExObjFlag.COUNTREFERENCES | ExObjFlag.DELEGABLE,
        WEAKREF = ExBaseType.WEAKREF | ExObjFlag.COUNTREFERENCES,

        FUNCPRO = ExBaseType.FUNCPRO | ExObjFlag.COUNTREFERENCES,
        OUTER = ExBaseType.OUTER | ExObjFlag.COUNTREFERENCES
    }


    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct FloatInt
    {
        [FieldOffset(0)] public double f;
        [FieldOffset(0)] public long i;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct ExObjVal
    {
        public bool b_Bool;    // 4 
        public long i_Int;     // 8 
        public double f_Float; // 8 
        public double c_Float;  // 8 
        public string s_String;

        public ExRefC _RefC;  // 40
        public ExSpace c_Space;   // 48
        public List<ExObject> l_List; // 40
        public Dictionary<string, ExObject> d_Dict;   // 88

        public MethodInfo _Method;
        public ExClosure _Closure;    // 144
        public ExOuter _Outer;    // 152
        public ExNativeClosure _NativeClosure;    // 280
        public ExFuncPro _FuncPro;    // 200
        public ExClass _Class;    // 1640
        public ExInstance _Instance;  // 168
        public ExWeakRef _WeakRef;    // 104
    }

    public enum ExMetaM
    {
        ADD,    // +
        SUB,    // -
        MLT,    // *
        DIV,    // /
        EXP,    // **
        MOD,    // %
        NEG,    // -
        SET,    // []
        GET,    // []
        TYPEOF,    // typeof
        NEXT,
        COMPARE,
        CALL,
        NEWSLOT,
        DELSLOT,
        NEWMEMBER,
        INHERIT,
        STRING,
        _LAST
    }

}
