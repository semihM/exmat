using System.Collections.Generic;
using System.Runtime.InteropServices;
using ExMat.Class;
using ExMat.Closure;
using ExMat.FuncPrototype;
using ExMat.Objects;

namespace ExMat
{
    public static class ExMat
    {
        public const char EndChar = '\0';

        public const string VersionID = "0.0.1";

        public const string ConstructorName = "init";

        public const string ThisName = "this";

        public const string VargsName = "vargs";

        public const int InvalidArgument = 985;


        public static ExBaseType GetRawType(ExObjType typ)
        {
            return (ExBaseType)((int)typ & 0x00FFFFFF);
        }
    }

    public enum ExBaseType
    {
        NULL = 1 << 0,              // Boş değer

        INTEGER = 1 << 1,           // Tamsayı
        FLOAT = 1 << 2,             // Ondalıklı sayı
        COMPLEX = 1 << 3,           // Kompleks sayı

        BOOL = 1 << 4,              // Boolean değeri
        STRING = 1 << 5,            // Yazı dizisi

        SPACE = 1 << 6,             // Uzay
        ARRAY = 1 << 7,             // Liste
        DICT = 1 << 8,              // Tablo / kütüphane

        CLOSURE = 1 << 9,           // Kullanıcı fonksiyonu
        NATIVECLOSURE = 1 << 10,    // Yerel fonksiyon

        CLASS = 1 << 11,            // Sınıf
        INSTANCE = 1 << 12,         // Sınıfa ait obje

        WEAKREF = 1 << 13,          // Obje referansı
        DEFAULT = 1 << 14,          // Varsayılan parametre değeri

        OUTER = 1 << 15,            // (Dahili tip) Dışardan değişken referansı
        FUNCPRO = 1 << 16,          // (Dahili tip) Fonksiyon prototipi
    }

    public enum ExObjFlag
    {
        CANBEFALSE = 0x01000000,        // Koşullu ifadede False değeri alabilir
        NUMERIC = 0x02000000,           // Sayısal bir değer
        COUNTREFERENCES = 0x04000000,   // Referansları say
        DELEGABLE = 0x08000000          // Temsilci 
    }

    public enum ExObjType
    {
        NULL = ExBaseType.NULL | ExObjFlag.CANBEFALSE,

        INTEGER = ExBaseType.INTEGER | ExObjFlag.NUMERIC | ExObjFlag.CANBEFALSE,
        FLOAT = ExBaseType.FLOAT | ExObjFlag.NUMERIC | ExObjFlag.CANBEFALSE,
        COMPLEX = ExBaseType.COMPLEX | ExObjFlag.NUMERIC | ExObjFlag.CANBEFALSE,

        BOOL = ExBaseType.BOOL | ExObjFlag.CANBEFALSE,
        STRING = ExBaseType.STRING,

        SPACE = ExBaseType.SPACE | ExObjFlag.COUNTREFERENCES,
        ARRAY = ExBaseType.ARRAY | ExObjFlag.COUNTREFERENCES,
        DICT = ExBaseType.DICT | ExObjFlag.COUNTREFERENCES,

        CLOSURE = ExBaseType.CLOSURE | ExObjFlag.COUNTREFERENCES,
        NATIVECLOSURE = ExBaseType.NATIVECLOSURE | ExObjFlag.COUNTREFERENCES,

        CLASS = ExBaseType.CLASS | ExObjFlag.COUNTREFERENCES,
        INSTANCE = ExBaseType.INSTANCE | ExObjFlag.COUNTREFERENCES | ExObjFlag.DELEGABLE,
        WEAKREF = ExBaseType.WEAKREF | ExObjFlag.COUNTREFERENCES,

        FUNCPRO = ExBaseType.FUNCPRO | ExObjFlag.COUNTREFERENCES,
        OUTER = ExBaseType.OUTER | ExObjFlag.COUNTREFERENCES,

        DEFAULT = ExBaseType.DEFAULT
    }


    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct DoubleLong
    {
        [FieldOffset(0)] public double f;
        [FieldOffset(0)] public long i;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct ExObjVal
    {
        public bool b_Bool;     // Boolean
        public long i_Int;      // Tamsayı
        public double f_Float;  // Ondalıklı
        public double c_Float;  // Karmaşık sayı katsayısı
        public string s_String; // Yazı dizisi

        public ExSpace c_Space;                     // Uzay
        public List<ExObject> l_List;               // Liste
        public Dictionary<string, ExObject> d_Dict; // Tablo

        public ExClosure _Closure;              // Kullanıcı fonksiyonu
        public ExNativeClosure _NativeClosure;  // Yerel fonksiyon

        public ExClass _Class;                  // Sınıf
        public ExInstance _Instance;            // Sınıfa ait obje

        public ExRefC _RefC;                    // Referans sayacı
        public ExWeakRef _WeakRef;              // Obje referansı

        public ExOuter _Outer;                  // (Dahili)
        public ExPrototype _FuncPro;            // (Dahili)
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
        LAST
    }

}
