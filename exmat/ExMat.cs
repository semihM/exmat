using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using ExMat.Class;
using ExMat.Closure;
using ExMat.FuncPrototype;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat
{
    public class ExMat
    {
        public static readonly string _VERSION = "0.0.1";

        public const char _END = '\0';
        public static ExRawType GetRawType(ExObjType typ)
        {
            return (ExRawType)((int)typ & 0x00FFFFFF);
        }
    }

    public enum ExRawType
    {
        NULL = 1 << 0,
        INTEGER = 1 << 1,
        FLOAT = 1 << 2,
        BOOL = 1 << 3,
        STRING = 1 << 4,
        ARRAY = 1 << 5,
        USERDATA = 1 << 6,
        CLOSURE = 1 << 7,
        NATIVECLOSURE = 1 << 8,
        USERPTR = 1 << 9,
        THREAD = 1 << 10,
        FUNCINFO = 1 << 11,
        CLASS = 1 << 12,
        INSTANCE = 1 << 13,
        WEAKREF = 1 << 14,
        OUTER = 1 << 15,
        FUNCPRO = 1 << 16,
        DICT = 1 << 17
    }

    public enum ExObjFlag
    {
        BOOLFALSEABLE = 0x01000000,
        NUMERIC = 0x02000000,
        REF_COUNTED = 0x04000000,
        DELEGABLE = 0x08000000
    }

    public enum ExObjType
    {
        NULL = ExRawType.NULL | ExObjFlag.BOOLFALSEABLE,
        INTEGER = ExRawType.INTEGER | ExObjFlag.NUMERIC | ExObjFlag.BOOLFALSEABLE,
        FLOAT = ExRawType.FLOAT | ExObjFlag.NUMERIC | ExObjFlag.BOOLFALSEABLE,
        BOOL = ExRawType.BOOL | ExObjFlag.BOOLFALSEABLE,
        STRING = ExRawType.STRING, // | ExObjFlag.REF_COUNTED,
        DICT = ExRawType.DICT, //| ExObjFlag.REF_COUNTED,

        ARRAY = ExRawType.ARRAY | ExObjFlag.BOOLFALSEABLE, // | ExObjFlag.REF_COUNTED,

        USERDATA = ExRawType.USERDATA | ExObjFlag.REF_COUNTED | ExObjFlag.DELEGABLE,
        USERPTR = ExRawType.USERPTR,

        CLOSURE = ExRawType.CLOSURE | ExObjFlag.REF_COUNTED,
        NATIVECLOSURE = ExRawType.NATIVECLOSURE | ExObjFlag.REF_COUNTED,

        THREAD = ExRawType.THREAD | ExObjFlag.REF_COUNTED,
        FUNCINFO = ExRawType.FUNCINFO | ExObjFlag.REF_COUNTED,
        FUNCPRO = ExRawType.FUNCPRO | ExObjFlag.REF_COUNTED,

        CLASS = ExRawType.CLASS | ExObjFlag.REF_COUNTED,
        INSTANCE = ExRawType.INSTANCE | ExObjFlag.REF_COUNTED | ExObjFlag.DELEGABLE,
        WEAKREF = ExRawType.WEAKREF | ExObjFlag.REF_COUNTED,

        OUTER = ExRawType.OUTER | ExObjFlag.REF_COUNTED
    }


    [StructLayout(LayoutKind.Explicit)]
    public struct FloatInt
    {
        [FieldOffset(0)] public float f;
        [FieldOffset(0)] public int i;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ExObjVal
    {
        [FieldOffset(0)] public int i_Int;
        [FieldOffset(0)] public float f_Float;
        [FieldOffset(0)] public bool b_Bool;
        [FieldOffset(8)] public string s_String;
        [FieldOffset(24)] public List<ExObjectPtr> l_List;
        [FieldOffset(24)] public Dictionary<string, ExObjectPtr> d_Dict;
        [FieldOffset(40)] public ExRefC _RefC;
        [FieldOffset(56)] public MethodInfo _Method;
        [FieldOffset(56)] public ExClosure _Closure;
        [FieldOffset(56)] public ExOuter _Outer;
        [FieldOffset(56)] public ExNativeClosure _NativeClosure;
        [FieldOffset(56)] public ExUserData _UserData;
        [FieldOffset(56)] public ExUserP _UserPointer;
        [FieldOffset(56)] public ExFuncPro _FuncPro;
        [FieldOffset(56)] public ExDeleg _Deleg;
        [FieldOffset(56)] public ExVM _Thread;
        [FieldOffset(56)] public ExClass _Class;
        [FieldOffset(56)] public ExInstance _Instance;
        [FieldOffset(56)] public ExWeakRef _WeakRef;
    }

    public enum ExMetaM
    {
        ADD,
        SUB,
        MLT,
        DIV,
        NEG,
        MOD,
        SET,
        GET,
        TYP,
        NXT,
        CMP,
        CALL,
        NEWS,
        DELS,
        NEWM,
        INH,
        _LAST
    }

}
