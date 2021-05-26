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
        BOOL = 1 << 3,
        STRING = 1 << 4,
        SPACE = 1 << 5,
        ARRAY = 1 << 6,
        USERDATA = 1 << 7,
        CLOSURE = 1 << 8,
        NATIVECLOSURE = 1 << 9,
        USERPTR = 1 << 10,
        THREAD = 1 << 11,
        FUNCINFO = 1 << 12,
        CLASS = 1 << 13,
        INSTANCE = 1 << 14,
        WEAKREF = 1 << 15,
        OUTER = 1 << 16,
        FUNCPRO = 1 << 17,
        DICT = 1 << 18,
        DEFAULT = 1 << 19
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

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct ExObjVal
    {
        [FieldOffset(0)] public long i_Int;
        [FieldOffset(0)] public double f_Float;
        [FieldOffset(0)] public bool b_Bool;
        [FieldOffset(8)] public string s_String;
        [FieldOffset(8)] public ExRefC _RefC;

        [FieldOffset(16)] public ExSpace c_Space;
        [FieldOffset(16)] public List<ExObject> l_List;
        [FieldOffset(16)] public Dictionary<string, ExObject> d_Dict;
        [FieldOffset(16)] public MethodInfo _Method;
        [FieldOffset(16)] public ExClosure _Closure;
        [FieldOffset(16)] public ExOuter _Outer;
        [FieldOffset(16)] public ExNativeClosure _NativeClosure;
        [FieldOffset(16)] public ExFuncPro _FuncPro;
        [FieldOffset(16)] public ExDeleg _Deleg;
        [FieldOffset(16)] public ExClass _Class;
        [FieldOffset(16)] public ExInstance _Instance;
        [FieldOffset(16)] public ExWeakRef _WeakRef;
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
