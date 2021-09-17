using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ExMat.Closure;
using ExMat.ExClass;
using ExMat.FuncPrototype;
using ExMat.Objects;
using ExMat.Outer;

namespace ExMat
{
    /// <summary>
    /// Definitions of commonly used constants
    /// </summary>
    public static class ExMat
    {
        /// <summary>
        /// Full version
        /// </summary>
        public const string Version = "ExMat v0.0.13";

        /// <summary>
        /// Version number
        /// </summary>
        public const int VersionNumber = 13;

        /// <summary>
        /// Title of the interactive console
        /// </summary>
        public const string ConsoleTitle = "[] ExMat Interactive";

        /// <summary>
        /// Help information to print at the beginning
        /// </summary>
        public const string HelpInfoString = "Use 'help' for function information, 'root' for global variables, 'consts' for constants";

        /// <summary>
        /// String and file terminator character
        /// </summary>
        public const char EndChar = '\0';

        /// <summary>
        /// Constructor method name for classes
        /// </summary>
        public const string ConstructorName = "init";

        /// <summary>
        /// Base reference keyword name
        /// </summary>
        public const string BaseName = "base";

        /// <summary>
        /// Self reference keyword name
        /// </summary>
        public const string ThisName = "this";

        /// <summary>
        /// Function attribute, returns name of the function
        /// </summary>
        public const string FuncName = "name";

        /// <summary>
        /// Function attribute, returns the documentation about the function
        /// </summary>
        public const string HelpName = "help";

        /// <summary>
        /// Function attribute, returns the summary of the function
        /// </summary>
        public const string DocsName = "docs";

        /// <summary>
        /// Function attribute, returns the return type information string
        /// </summary>
        public const string ReturnsName = "returns";

        /// <summary>
        /// String representation of a null value
        /// </summary>
        public const string NullName = "null";

        //TO-DO Add function attribute for parameter info

        /// <summary>
        /// Variable parameter count functions' argument list keyword name
        /// <para>Example function where this is available: <see langword="function"/><c> Foo(...){}</c></para>
        /// </summary>
        public const string VargsName = "vargs";

        /// <summary>
        /// Function attribute, returns wheter the function is a delegate function
        /// </summary>
        public const string DelegName = "deleg";

        /// <summary>
        /// INTERNAL: Used to check for some special cases
        /// </summary>
        public const int InvalidArgument = 985;

        /// <summary>
        /// Default parameter name for sequences
        /// </summary>
        public const string SequenceParameter = "n";

        /// <summary>
        /// Function attribute, returns named parameter count
        /// </summary>
        public const string nParams = "n_params";

        /// <summary>
        /// Function attribute, returns default valued parameter count
        /// </summary>
        public const string nDefParams = "n_defparams";

        /// <summary>
        /// Function attribute, returns minimum amount of arguments required
        /// </summary>
        public const string nMinArgs = "n_minargs";

        /// <summary>
        /// Function attribute, returns dictionary of default values for parameters
        /// </summary>
        public const string DefParams = "defparams";

        /// <summary>
        /// Function name, reload a function 
        /// </summary>
        public const string ReloadFunc = "reload_func";

        /// <summary>
        /// Function name, reload a standard library specific function
        /// </summary>
        public const string ReloadLibFunc = "reload_lib_func";

        /// <summary>
        /// Library reloading keyword name
        /// </summary>
        public const string ReloadName = "reload";

        /// <summary>
        /// Maximum length of the echo'd string, used for native functions which calls 'echo' in external terminals
        /// </summary>
        public const int ECHOLIMIT = 8000;

        /// <summary>
        /// Garbage collection run count after execution
        /// </summary>
        public const int GCCOLLECTCOUNT = 1 << 3;

        /// <summary>
        /// Informational multi-line string about available types for 'date' function
        /// </summary>
        public const string DateTypeInfoString = @"Type(s) of date. Use '|' to combine and get a list. Available types:
        today
        now = time,
        year,
        month,
        day = wday,
        mday,
        yday,
        hours = hour = hh = h,
        minutes = minute = min = mm = m,
        seconds = second = sec = ss = s,
        miliseconds = milisecond = ms,
        utc,
        utc-today,
        utc-time = utc-now,
        utc-year,
        utc-month,
        utc-day = utc-wday,
        utc-mday,
        utc-yday,
        utc-hours = utc-hour = utc-hh = utc-h";

        /// <summary>
        /// Type mask information
        /// <para>  Integer   |   Mask     |   Expected Type</para>
        /// <para>-------------------------------------</para>
        /// <para>   -1       |    .       |   Any type</para>
        /// <para>    0       |    .       |   Any type</para>
        /// <para>    1       |    e       |   NULL</para>
        /// <para>    2       |    i       |   INTEGER</para>
        /// <para>    4       |    f       |   FLOAT</para>
        /// <para>    8       |    C       |   COMPLEX</para>
        /// <para>    16      |    b       |   BOOL</para>
        /// <para>    32      |    s       |   STRING</para>
        /// <para>    64      |    S       |   SPACE</para>
        /// <para>    128     |    a       |   ARRAY</para>
        /// <para>    256     |    d       |   DICT</para>
        /// <para>    512     |    f       |   CLOSURE</para>
        /// <para>    1024    |    Y       |   NATIVECLOSURE</para>
        /// <para>    2048    |    y       |   CLASS</para>
        /// <para>    4096    |    x       |   INSTANCE</para>
        /// <para>    8192    |    w       |   WEAKREF</para>
        /// <para>    6       |    r       |   INTEGER|FLOAT</para>
        /// <para>    14      |    n       |   INTEGER|FLOAT|COMPLEX</para>
        /// <para>    1536    |    c       |   CLOSURE|NATIVECLOSURE</para>
        /// </summary>
        public static readonly Dictionary<int, char> TypeMasks = new()
        {
            { -1, '.' },
            { 0, '.' },
            { (int)ExBaseType.NULL, 'e' },
            { (int)ExBaseType.INTEGER, 'i' },
            { (int)ExBaseType.FLOAT, 'f' },
            { (int)ExBaseType.COMPLEX, 'C' },
            { (int)ExBaseType.BOOL, 'b' },
            { (int)ExBaseType.STRING, 's' },
            { (int)ExBaseType.SPACE, 'S' },
            { (int)ExBaseType.ARRAY, 'a' },
            { (int)ExBaseType.DICT, 'd' },
            { (int)ExBaseType.CLOSURE, 'f' },
            { (int)ExBaseType.NATIVECLOSURE, 'Y' },
            { (int)ExBaseType.CLASS, 'y' },
            { (int)ExBaseType.INSTANCE, 'x' },
            { (int)ExBaseType.WEAKREF, 'w' },
            { (int)ExBaseType.CLOSURE | (int)ExBaseType.NATIVECLOSURE, 'c' },
            { (int)ExBaseType.INTEGER | (int)ExBaseType.FLOAT, 'r' },
            { (int)ExBaseType.INTEGER | (int)ExBaseType.FLOAT | (int)ExBaseType.COMPLEX, 'n' },
        };

        /// <summary>
        /// Standard base library name
        /// </summary>
        public const string StandardBaseLibraryName = "base";

        /// <summary>
        /// Namespace required for a class to be checked for being a standard library 
        /// </summary>
        public const string StandardLibraryNameSpace = "ExMat.StdLib";

        /// <summary>
        /// Delegate registery method for std libs
        /// </summary>
        /// <param name="vm">Virtual machine for registery</param>
        /// <returns><see langword="true"/> if registery was successful, otherwise <see langword="false"/></returns>
        public delegate bool StdLibRegistery(VM.ExVM vm);

        /// <summary>
        /// Delegate, a native function template
        /// </summary>
        /// <param name="vm">VM to use the stack of</param>
        /// <param name="nargs">Number of arguments passed to the function</param>
        /// <returns>If a value was pushed to stack: <see cref="ExFunctionStatus.SUCCESS"/>
        /// <para>If nothing was pushed to stack: <see cref="ExFunctionStatus.VOID"/></para>
        /// <para>If there was an error: <see cref="ExFunctionStatus.ERROR"/></para>
        /// <para>In the special case of 'exit': <see cref="ExFunctionStatus.EXIT"/></para></returns>
        public delegate ExFunctionStatus StdLibFunction(VM.ExVM vm, int nargs);

        /// <summary>
        /// Method name of delegate <see cref="StdLibFunction"/> to get standard library method signature pattern from
        /// </summary>
        private const string StdLibFunctionPatternMethodName = "Invoke"; // TO-DO find a better way!

        /// <summary>
        /// Standard library method signature pattern, this is for matching <see cref="StdLibFunction"/>'s signature
        /// </summary>
        private static string StdLibFunctionPattern => System.Text.RegularExpressions.Regex.Escape(
                                                            typeof(StdLibFunction)
                                                            .GetMethod(StdLibFunctionPatternMethodName)
                                                            .ToString())
                                                        .Replace(StdLibFunctionPatternMethodName, "[\\w\\d]+")
                                                        .Replace("\\ ", " ");

        /// <summary>
        /// Regex object to use while finding standard library functions
        /// </summary>
        public static readonly System.Text.RegularExpressions.Regex StdLibFunctionRegex = new(StdLibFunctionPattern);

        public static readonly Dictionary<string, string> Assemblies = new()
        {
            { "exm", "Entering assembly project" },
            { "exmat", "Core project" },
            { "exmatstdlib", "Built-in standard libraries" },
        };

    }

    /// <summary>
    /// Base/raw object types, use one of these to create <see cref="ExObjType"/> types with <see cref="ExObjFlag"/> flags
    /// </summary>
    [Flags]
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
    [Flags]
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

    [Flags]
    public enum ExMemberFlag
    {
        METHOD = 0x01000000,
        FIELD = 0x02000000
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
        public ExClass.ExClass _Class;                  // Sınıf
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
    public enum ExMetaMethod
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
        NEXT,       // TO-DO Add remaining methods
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
    /// Getter method status
    /// </summary>
    public enum ExGetterStatus
    {
        /// <summary>
        /// Base object had issues
        /// </summary>
        ERROR,

        /// <summary>
        /// Object was found in base object
        /// </summary>
        FOUND,

        /// <summary>
        /// Object was not found in base object
        /// </summary>
        NOTFOUND
    }

    /// <summary>
    /// Getter method status
    /// </summary>
    public enum ExSetterStatus
    {
        /// <summary>
        /// Base object had issues
        /// </summary>
        ERROR,

        /// <summary>
        /// Setter succeeded
        /// </summary>
        SET,

        /// <summary>
        /// A delegate was tried to be set
        /// </summary>
        NOTSETDELEGATE,

        /// <summary>
        /// Unknown index or delegate tried to be set
        /// </summary>
        NOTSETUNKNOWN
    }

    /// <summary>
    /// Error types used for messages
    /// </summary>
    public enum ExErrorType
    {
        /// <summary>
        /// INTERNAL: No overrides on error type, used for handling post-interruption
        /// </summary>
        DEFAULT,
        /// <summary>
        /// INTERNAL: Duh
        /// </summary>
        INTERNAL,
        /// <summary>
        /// Compiler error
        /// </summary>
        COMPILE,
        /// <summary>
        /// Runtime-execution error
        /// </summary>
        RUNTIME,
        /// <summary>
        /// Interruption by CTRLC or CTRLBREAK
        /// </summary>
        INTERRUPT,
        /// <summary>
        /// Interruption of input stream by CTRLC or CTRLBREAK
        /// </summary>
        INTERRUPTINPUT
    }

    /// <summary>
    /// Console flags
    /// </summary>
    [Flags]
    public enum ExConsoleFlag
    {
        /// <summary>
        /// Don't read key input on exit
        /// </summary>
        DONTKEEPOPEN = 1 << 0,
        /// <summary>
        /// Don't print version information
        /// </summary>
        NOTITLE = 1 << 1
    }

    /// <summary>
    /// Interactive console flags
    /// </summary>
    [Flags]
    public enum ExInteractiveConsoleFlag
    {
        /// <summary>
        /// User input was empty
        /// </summary>
        EMPTYINPUT = 1 << 0,
        /// <summary>
        /// Multi-line code state
        /// </summary>
        LINECARRY = 1 << 1,
        /// <summary>
        /// Prevented CTRL+C or CTRL+BREAK
        /// </summary>
        CANCELEVENT = 1 << 2,
        /// <summary>
        /// Is the active VM currently in the execution process ?
        /// </summary>
        CURRENTLYEXECUTING = 1 << 3,
        /// <summary>
        /// Has the VM just been interrupted ?
        /// </summary>
        RECENTLYINTERRUPTED = 1 << 4,
        /// <summary>
        /// Did the recent interruption occured during the thread's sleep ?
        /// </summary>
        INTERRUPTEDINSLEEP = 1 << 5
    }

    /// <summary>
    /// Outer variable types
    /// </summary>
    public enum ExOuterType
    {
        LOCAL,
        OUTER
    }

    /// <summary>
    /// Arithmetic operation masks
    /// </summary>
    public enum ArithmeticMask
    {
        INT = ExObjType.INTEGER,
        INTCOMPLEX = ExObjType.COMPLEX | ExObjType.INTEGER,

        FLOAT = ExObjType.FLOAT,
        FLOATINT = ExObjType.INTEGER | ExObjType.FLOAT,
        FLOATCOMPLEX = ExObjType.COMPLEX | ExObjType.FLOAT,

        COMPLEX = ExObjType.COMPLEX,

        STRING = ExObjType.STRING,
        STRINGINT = ExObjType.STRING | ExObjType.INTEGER,
        STRINGFLOAT = ExObjType.STRING | ExObjType.FLOAT,
        STRINGCOMPLEX = ExObjType.STRING | ExObjType.COMPLEX,
        STRINGBOOL = ExObjType.STRING | ExObjType.BOOL,
        STRINGNULL = ExObjType.STRING | ExObjType.NULL
    }

    /// <summary>
    /// Fallback types
    /// </summary>
    public enum ExFallback
    {
        /// <summary>
        /// Match found, no need of further search
        /// </summary>
        OK,
        /// <summary>
        /// No match found, continue search
        /// </summary>
        NOMATCH,
        /// <summary>
        /// Error during search
        /// </summary>
        ERROR,
        /// <summary>
        /// Don't search
        /// </summary>
        DONT = 999
    }

    public enum ExProcessInfo
    {
        DATE,
        MODULE,
        ARGS
    }
}
