using System.Collections.Generic;
using System.Runtime.InteropServices;
using ExMat.Class;
using ExMat.Closure;
using ExMat.FuncPrototype;
using ExMat.Objects;

namespace ExMat
{
    /// <summary>
    /// Definitions of commonly used constants
    /// </summary>
    public static class ExMat
    {
        /// <summary>
        /// String and file terminator character
        /// </summary>
        public const char EndChar = '\0';

        /// <summary>
        /// Current version no
        /// </summary>
        public const string VersionID = "0.0.1";

        /// <summary>
        /// Constructor method name for classes
        /// </summary>
        public const string ConstructorName = "init";

        /// <summary>
        /// Self reference keyword name
        /// </summary>
        public const string ThisName = "this";

        /// <summary>
        /// Variable parameter count functions' argument list keyword name
        /// <para>Example function where this is available: <see langword="function"/><c> Foo(...){}</c></para>
        /// </summary>
        public const string VargsName = "vargs";

        /// <summary>
        /// INTERNAL: Used to check for some special cases
        /// </summary>
        public const int InvalidArgument = 985;
    }

    /// <summary>
    /// Base/raw object types, use one of these to create <see cref="ExObjType"/> types with <see cref="ExObjFlag"/> flags
    /// </summary>
    public enum ExBaseType
    {
        /// <summary>
        /// null value
        /// </summary>
        NULL = 1 << 0,              // Boş değer

        /// <summary>
        /// Integer value
        /// </summary>
        INTEGER = 1 << 1,           // Tamsayı
        /// <summary>
        /// Float value
        /// </summary>
        FLOAT = 1 << 2,             // Ondalıklı sayı
        /// <summary>
        /// Complex value
        /// </summary>
        COMPLEX = 1 << 3,           // Kompleks sayı

        /// <summary>
        /// Boolean value
        /// </summary>
        BOOL = 1 << 4,              // Boolean değeri
        /// <summary>
        /// String value
        /// </summary>
        STRING = 1 << 5,            // Yazı dizisi

        /// <summary>
        /// Space value
        /// </summary>
        SPACE = 1 << 6,             // Uzay
        /// <summary>
        /// Array value
        /// </summary>
        ARRAY = 1 << 7,             // Liste
        /// <summary>
        /// Dictionary value
        /// </summary>
        DICT = 1 << 8,              // Tablo / kütüphane

        /// <summary>
        /// Closure value
        /// </summary>
        CLOSURE = 1 << 9,           // Kullanıcı fonksiyonu
        /// <summary>
        /// Native closure value
        /// </summary>
        NATIVECLOSURE = 1 << 10,    // Yerel fonksiyon

        /// <summary>
        /// Class value
        /// </summary>
        CLASS = 1 << 11,            // Sınıf
        /// <summary>
        /// Instance value
        /// </summary>
        INSTANCE = 1 << 12,         // Sınıfa ait obje

        /// <summary>
        /// Weak reference value
        /// </summary>
        WEAKREF = 1 << 13,          // Obje referansı
        /// <summary>
        /// Default value
        /// </summary>
        DEFAULT = 1 << 14,          // Varsayılan parametre değeri

        /// <summary>
        /// INTERNAL: Outer value
        /// </summary>
        OUTER = 1 << 15,            // (Dahili tip) Dışardan değişken referansı
        /// <summary>
        /// INTERNAL: Function prototype value
        /// </summary>
        FUNCPRO = 1 << 16,          // (Dahili tip) Fonksiyon prototipi
    }

    /// <summary>
    /// Object type flags for <see cref="ExObjType"/> types, used to define how a type should be treated
    /// </summary>
    public enum ExObjFlag
    {
        /// <summary>
        /// Allow to be used as a <see langword="false"/> value in certain cases(defined in comparison methods)
        /// </summary>
        CANBEFALSE = 0x01000000,        // Koşullu ifadede False değeri alabilir
        /// <summary>
        /// A numeric type
        /// </summary>
        NUMERIC = 0x02000000,           // Sayısal bir değer
        /// <summary>
        /// Keep track of references made to this
        /// </summary>
        COUNTREFERENCES = 0x04000000,   // Referansları say
        /// <summary>
        /// Allow access delegate methods
        /// </summary>
        DELEGABLE = 0x08000000          // Temsilci 
    }

    /// <summary>
    /// Available object types for <see cref="ExObject"/> objects
    /// </summary>
    public enum ExObjType
    {
        /// <summary>
        /// null value object
        /// </summary>
        NULL = ExBaseType.NULL | ExObjFlag.CANBEFALSE,

        /// <summary>
        /// Integer value object
        /// </summary>
        INTEGER = ExBaseType.INTEGER | ExObjFlag.NUMERIC | ExObjFlag.CANBEFALSE,
        /// <summary>
        /// Float value object
        /// </summary>
        FLOAT = ExBaseType.FLOAT | ExObjFlag.NUMERIC | ExObjFlag.CANBEFALSE,
        /// <summary>
        /// Complex value object
        /// </summary>
        COMPLEX = ExBaseType.COMPLEX | ExObjFlag.NUMERIC | ExObjFlag.CANBEFALSE,

        /// <summary>
        /// Bool value object
        /// </summary>
        BOOL = ExBaseType.BOOL | ExObjFlag.CANBEFALSE,
        /// <summary>
        /// String value object
        /// </summary>
        STRING = ExBaseType.STRING,

        /// <summary>
        /// Space value object
        /// </summary>
        SPACE = ExBaseType.SPACE | ExObjFlag.COUNTREFERENCES,
        /// <summary>
        /// Array object
        /// </summary>
        ARRAY = ExBaseType.ARRAY | ExObjFlag.COUNTREFERENCES,
        /// <summary>
        /// Dictionary object
        /// </summary>
        DICT = ExBaseType.DICT | ExObjFlag.COUNTREFERENCES,

        /// <summary>
        /// Closure object
        /// </summary>
        CLOSURE = ExBaseType.CLOSURE | ExObjFlag.COUNTREFERENCES,
        /// <summary>
        /// Native closure object
        /// </summary>
        NATIVECLOSURE = ExBaseType.NATIVECLOSURE | ExObjFlag.COUNTREFERENCES,

        /// <summary>
        /// Class object
        /// </summary>
        CLASS = ExBaseType.CLASS | ExObjFlag.COUNTREFERENCES,
        /// <summary>
        /// Instance object
        /// </summary>
        INSTANCE = ExBaseType.INSTANCE | ExObjFlag.COUNTREFERENCES | ExObjFlag.DELEGABLE,
        /// <summary>
        /// Weak reference
        /// </summary>
        WEAKREF = ExBaseType.WEAKREF | ExObjFlag.COUNTREFERENCES,

        /// <summary>
        /// Default value placer for ".." token
        /// </summary>
        DEFAULT = ExBaseType.DEFAULT,

        /// <summary>
        /// INTERNAL: Function prototype
        /// </summary>
        FUNCPRO = ExBaseType.FUNCPRO | ExObjFlag.COUNTREFERENCES,
        /// <summary>
        /// INTERNAL: Outer value
        /// </summary>
        OUTER = ExBaseType.OUTER | ExObjFlag.COUNTREFERENCES

    }

    /// <summary>
    /// Change between 64bit integer and 64bit floats
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct DoubleLong
    {
        [FieldOffset(0)] public double f;
        [FieldOffset(0)] public long i;
    }

    /// <summary>
    /// Values stored for <see cref="ExObject"/> objects
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct ExObjVal
    {
        /// <summary>
        /// Boolean value for <see cref="ExObjType.BOOL"/>
        /// </summary>
        public bool b_Bool;     // Boolean
        /// <summary>
        /// Integer value for <see cref="ExObjType.INTEGER"/>
        /// </summary>
        public long i_Int;      // Tamsayı
        /// <summary>
        /// Float value for <see cref="ExObjType.FLOAT"/>, also stores real part of <see cref="ExObjType.COMPLEX"/>
        /// </summary>
        public double f_Float;  // Ondalıklı
        /// <summary>
        /// Imaginary part value for <see cref="ExObjType.COMPLEX"/>
        /// </summary>
        public double c_Float;  // Karmaşık sayı katsayısı
        /// <summary>
        /// String value for <see cref="ExObjType.STRING"/>
        /// </summary>
        public string s_String; // Yazı dizisi

        /// <summary>
        /// Space value for <see cref="ExObjType.SPACE"/>
        /// </summary>
        public ExSpace c_Space;                     // Uzay
        /// <summary>
        /// Array for <see cref="ExObjType.ARRAY"/>
        /// </summary>
        public List<ExObject> l_List;               // Liste
        /// <summary>
        /// Dictionary for <see cref="ExObjType.DICT"/>
        /// </summary>
        public Dictionary<string, ExObject> d_Dict; // Tablo

        /// <summary>
        /// Closure for <see cref="ExObjType.CLOSURE"/>
        /// </summary>
        public ExClosure _Closure;              // Kullanıcı fonksiyonu
        /// <summary>
        /// Native closure for <see cref="ExObjType.NATIVECLOSURE"/>
        /// </summary>
        public ExNativeClosure _NativeClosure;  // Yerel fonksiyon

        /// <summary>
        /// Class for <see cref="ExObjType.CLASS"/>
        /// </summary>
        public ExClass _Class;                  // Sınıf
        /// <summary>
        /// Instance for <see cref="ExObjType.INSTANCE"/>
        /// </summary>
        public ExInstance _Instance;            // Sınıfa ait obje
        /// <summary>
        /// Weak reference for <see cref="ExObjType.WEAKREF"/>
        /// </summary>
        public ExWeakRef _WeakRef;              // Obje referansı

        /// <summary>
        /// INTERNAL: Reference counter to keep track of references
        /// </summary>
        public ExRefC _RefC;                    // Referans sayacı

        /// <summary>
        /// INTERNAL: Outer value for <see cref="ExObjType.OUTER"/>
        /// </summary>
        public ExOuter _Outer;                  // (Dahili)
        /// <summary>
        /// INTERNAL: Function prototype value for <see cref="ExObjType.FUNCPRO"/>
        /// </summary>
        public ExPrototype _FuncPro;            // (Dahili)
    }

    /// <summary>
    /// Meta methods
    /// </summary>
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

    /// <summary>
    /// Return status of native functions
    /// </summary>
    public enum ExFunctionStatus
    {
        /// <summary>
        /// An error occured
        /// </summary>
        ERROR,
        /// <summary>
        /// Nothing is returned
        /// </summary>
        VOID,
        /// <summary>
        /// Return value pushed to stack
        /// </summary>
        SUCCESS,
        /// <summary>
        /// Special case: 'exit' function called, closes the interactive console. 
        /// </summary>
        EXIT = 985
    }

    /// <summary>
    /// Error types used for messages
    /// </summary>
    public enum ExErrorType
    {
        /// <summary>
        /// Compiler error
        /// </summary>
        COMPILE,
        /// <summary>
        /// Runtime-execution error
        /// </summary>
        RUNTIME
    }
}
