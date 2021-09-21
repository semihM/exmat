using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using ExMat.Attributes;
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
        public const string Version = "ExMat v0.0.16";

        /// <summary>
        /// Version number
        /// </summary>
        public const int VersionNumber = 16;

        /// <summary>
        /// Title of the interactive console
        /// </summary>
        public const string ConsoleTitle = "[] ExMat Interactive";

        /// <summary>
        /// Help information to print at the beginning
        /// </summary>
        public static readonly string[] HelpInfoString = new string[2]
        {
            "Use 'help' for function information, 'root' for global variables, 'consts' for constants",
            "Use 'exm --help' in your terminal to view startup options",
        };

        internal static int PreferredStackSize { get; set; } = 1 << 15;

        internal static string PreferredConsoleTitle { get; set; } = ConsoleTitle;

        /// <summary>
        /// Time in ms to delay output so CTRL+C doesn't mess up
        /// </summary>
        public const int CANCELKEYTHREADTIMER = 50;

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

        /// <summary>
        /// Foreach single index variable name
        /// </summary>
        public const string ForeachSingleIdxName = ".foreach.idx.";

        /// <summary>
        /// Foreach single index variable name
        /// </summary>
        public const string ForeachIteratorName = ".foreach.iterator.";

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
        public const int InvalidArgument = int.MinValue;

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
        /// Printer method delegate, no line terminator at the end
        /// </summary>
        /// <param name="message">Message to print</param>
        public delegate void PrinterMethod(string message);

        /// <summary>
        /// Input line reader method delegate
        /// </summary>
        /// <returns>Line read</returns>
        public delegate string LineReaderMethod();

        /// <summary>
        /// Input key reader method delegate
        /// </summary>
        /// <returns>Key read</returns>
        public delegate ConsoleKeyInfo KeyReaderMethod(bool intercept);

        /// <summary>
        /// Input key reader method delegate
        /// </summary>
        /// <returns>Key read as integer</returns>
        public delegate int IntKeyReaderMethod();

        /// <summary>
        /// Method name of delegate <see cref="StdLibFunction"/> to get standard library method signature pattern from
        /// </summary>
        private const string StdLibFunctionPatternMethodName = "Invoke"; // TO-DO find a better way!

        /// <summary>
        /// Standard library method signature pattern, this is for matching <see cref="StdLibFunction"/>'s signature
        /// </summary>
        private static string StdLibFunctionPattern => Regex.Escape(
                                                            typeof(StdLibFunction)
                                                            .GetMethod(StdLibFunctionPatternMethodName)
                                                            .ToString())
                                                        .Replace(StdLibFunctionPatternMethodName, "[\\w\\d]+")
                                                        .Replace("\\ ", " ");

        /// <summary>
        /// Assembly type to decide wheter to load directly or from a file
        /// </summary>
        public enum ExAssemblyType
        {
            /// <summary>
            /// Built-in assembly
            /// </summary>
            NATIVE,
            /// <summary>
            /// External plugin
            /// </summary>
            PLUGIN
        }

        internal static Dictionary<string, ExAssemblyType> Assemblies { get; } = new()
        {
            { "exm", ExAssemblyType.NATIVE },
            { "exmat", ExAssemblyType.NATIVE },
            { "exmatstdlib", ExAssemblyType.NATIVE }
        };

        /// <summary>
        /// Regex object to use while finding standard library functions
        /// </summary>
        public static readonly Regex StdLibFunctionRegex = new(StdLibFunctionPattern);

        /// <summary>
        /// Expected names for the console flags
        /// <para>Don't display information banner: <code>--no-info</code></para>
        /// <para>Don't use custom console title: <code>--no-title</code></para>
        /// <para>Don't wait post exit: <code>--no-exit-hold</code></para>
        /// <para>Don't write IN and OUT prefix: <code>--no-inout</code></para>
        /// <para>Delete the given file post execution: <code>--delete-onpost</code></para>
        /// <para>Print help string: <code>--help</code></para>
        /// </summary>
        [ExConsoleHelper("--help", "Print help information")]
        [ExConsoleHelper("--no-info", "Don't display information banner")]
        [ExConsoleHelper("--no-title", "Don't use custom console title")]
        [ExConsoleHelper("--no-exit-hold", "Don't wait for input post exit (after an exit function call or an internal error)")]
        [ExConsoleHelper("--no-inout", "Don't write IN and OUT prefixes")]
        [ExConsoleHelper("--delete-onpost", "Delete the given file post execution")]
        public static readonly Dictionary<string, ExConsoleFlag> ConsoleFlags = new()
        {
            { "--help", ExConsoleFlag.HELP },
            { "--no-info", ExConsoleFlag.NOINFO },
            { "--no-title", ExConsoleFlag.NOTITLE },
            { "--no-exit-hold", ExConsoleFlag.DONTKEEPOPEN },
            { "--no-inout", ExConsoleFlag.NOINOUT },
            { "--delete-onpost", ExConsoleFlag.DELETEONPOST }
        };

        /// <summary>
        /// Expected names for the console flags. Make <see cref="ExConsoleParameter"/> invoke return <see langword="null"/> to keep the parameter in the referenced array
        /// <para>Custom stack size: <code>-stacksize:"?(.*)"?</code></para>
        /// <para>Custom console title: <code>-title:"?(.*)"?</code></para>
        /// <para>Plugin library path: <code>-plugin:"?(.*)"?</code></para>
        /// </summary>
        [ExConsoleHelper("-title:", "Custom console title")]
        [ExConsoleHelper("-stacksize:", "Custom virtual stack size")]
        [ExConsoleHelper("-plugin:", "Plugin dll path to include")]
        public static readonly Dictionary<string, ExConsoleParameter> ConsoleParameters = new()
        {
            {
                "-title:",
                a => PreferredConsoleTitle = a.TrimEnd('\"')
            },

            {
                "-stacksize:",
                a => int.TryParse(a.TrimEnd('\"'), out int res)
                    ? PreferredStackSize = res
                    : null
            },

            {
                "-plugin:",
                a => Assemblies.TryAdd(a.TrimEnd('\"'), ExAssemblyType.PLUGIN)
                    ? a
                    : null
            }
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

        /// <summary>
        /// Generator
        /// </summary>
        GENERATOR = 1 << 17,
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
        HASDELEGATES = 0x08000000          // Temsilci 
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
        INTEGER = ExBaseType.INTEGER | ExObjFlag.NUMERIC | ExObjFlag.CANBEFALSE | ExObjFlag.HASDELEGATES,
        /// <summary>
        /// Float value object
        /// </summary>
        FLOAT = ExBaseType.FLOAT | ExObjFlag.NUMERIC | ExObjFlag.CANBEFALSE | ExObjFlag.HASDELEGATES,
        /// <summary>
        /// Complex value object
        /// </summary>
        COMPLEX = ExBaseType.COMPLEX | ExObjFlag.NUMERIC | ExObjFlag.CANBEFALSE | ExObjFlag.HASDELEGATES,

        /// <summary>
        /// Bool value object
        /// </summary>
        BOOL = ExBaseType.BOOL | ExObjFlag.CANBEFALSE,
        /// <summary>
        /// String value object
        /// </summary>
        STRING = ExBaseType.STRING | ExObjFlag.HASDELEGATES,

        /// <summary>
        /// Space value object
        /// </summary>
        SPACE = ExBaseType.SPACE | ExObjFlag.COUNTREFERENCES,
        /// <summary>
        /// Array object
        /// </summary>
        ARRAY = ExBaseType.ARRAY | ExObjFlag.COUNTREFERENCES | ExObjFlag.HASDELEGATES,
        /// <summary>
        /// Dictionary object
        /// </summary>
        DICT = ExBaseType.DICT | ExObjFlag.COUNTREFERENCES | ExObjFlag.HASDELEGATES,

        /// <summary>
        /// Closure object
        /// </summary>
        CLOSURE = ExBaseType.CLOSURE | ExObjFlag.COUNTREFERENCES | ExObjFlag.HASDELEGATES,
        /// <summary>
        /// Native closure object
        /// </summary>
        NATIVECLOSURE = ExBaseType.NATIVECLOSURE | ExObjFlag.COUNTREFERENCES | ExObjFlag.HASDELEGATES,

        /// <summary>
        /// Class object
        /// </summary>
        CLASS = ExBaseType.CLASS | ExObjFlag.COUNTREFERENCES | ExObjFlag.HASDELEGATES,
        /// <summary>
        /// Instance object
        /// </summary>
        INSTANCE = ExBaseType.INSTANCE | ExObjFlag.COUNTREFERENCES | ExObjFlag.HASDELEGATES,
        /// <summary>
        /// Weak reference
        /// </summary>
        WEAKREF = ExBaseType.WEAKREF | ExObjFlag.COUNTREFERENCES | ExObjFlag.HASDELEGATES,

        /// <summary>
        /// Generator
        /// </summary>
        GENERATOR = ExObjFlag.COUNTREFERENCES,

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
        /// <summary>
        /// Float value
        /// </summary>
        [FieldOffset(0)] public double f;
        /// <summary>
        /// Integer value
        /// </summary>
        [FieldOffset(0)] public long i;
    }

    [Flags]
    internal enum ExMemberFlag
    {
        METHOD = 0x01000000,
        FIELD = 0x02000000
    }

    /// <summary>
    /// Custom type(plus <see cref="string"/>) values stored for <see cref="ExObject"/> objects
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public struct ExObjValCustom
    {
        /// <summary>
        /// String value for <see cref="ExObjType.STRING"/>
        /// </summary>
        public string s_String; // Yazı dizisi

        /// <summary>
        /// Array for <see cref="ExObjType.ARRAY"/>
        /// </summary>
        public List<ExObject> l_List;               // Liste

        /// <summary>
        /// Dictionary for <see cref="ExObjType.DICT"/>
        /// </summary>
        public Dictionary<string, ExObject> d_Dict; // Tablo

        /// <summary>
        /// Space value for <see cref="ExObjType.SPACE"/>
        /// </summary>
        public ExSpace c_Space;                     // Uzay

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
    /// .NET type values stored for <see cref="ExObject"/> objects
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public struct ExObjVal
    {
        /// <summary>
        /// Boolean value for <see cref="ExObjType.BOOL"/>
        /// </summary>
        [FieldOffset(0)]
        public bool b_Bool;     // Boolean

        /// <summary>
        /// Integer value for <see cref="ExObjType.INTEGER"/>
        /// </summary>
        [FieldOffset(0)]
        public long i_Int;      // Tamsayı

        /// <summary>
        /// Float value for <see cref="ExObjType.FLOAT"/>, also stores real part of <see cref="ExObjType.COMPLEX"/>
        /// </summary>
        [FieldOffset(0)]
        public double f_Float;  // Ondalıklı

        /// <summary>
        /// Imaginary part value for <see cref="ExObjType.COMPLEX"/>
        /// </summary>
        [FieldOffset(8)]
        public double c_Float;  // Karmaşık sayı katsayısı

    }

    /// <summary>
    /// Types for standard libraries
    /// </summary>
    public enum ExStdLibType
    {
        /// <summary>
        /// Unknown library, for internal use only
        /// </summary>
        UNKNOWN,
        /// <summary>
        /// External custom library
        /// </summary>
        EXTERNAL,
        /// <summary>
        /// Base library
        /// </summary>
        BASE,
        /// <summary>
        /// Math library
        /// </summary>
        MATH,
        /// <summary>
        /// Input-output library
        /// </summary>
        IO,
        /// <summary>
        /// String library
        /// </summary>
        STRING,
        /// <summary>
        /// Networking library
        /// </summary>
        NETWORK,
        /// <summary>
        /// System library
        /// </summary>
        SYSTEM,
        /// <summary>
        /// Statistics library
        /// </summary>
        STATISTICS
    }

    /// <summary>
    /// Meta methods
    /// <para>Meta methods' names are derived from this enum class as<code>ENUMVAL -&gt; function _ENUMVAL(...)</code></para>
    /// </summary>
    public enum ExMetaMethod
    {
        /// <summary>
        /// Addition ( other )
        /// </summary>
        ADD,    // +
        /// <summary>
        /// Subtraction ( other )
        /// </summary>
        SUB,    // -
        /// <summary>
        /// Multiplication ( other )
        /// </summary>
        MLT,    // *
        /// <summary>
        /// Division ( other )
        /// </summary>
        DIV,    // /
        /// <summary>
        /// Exponential ( other )
        /// </summary>
        EXP,    // **
        /// <summary>
        /// Modulo ( other )
        /// </summary>
        MOD,    // %
        /// <summary>
        /// Negate ( )
        /// </summary>
        NEG,    // -
        /// <summary>
        /// Setter ( key, value )
        /// </summary>
        SET,    // []
        /// <summary>
        /// Getter ( key )
        /// </summary>
        GET,    // []
        /// <summary>
        /// typeof ( )
        /// </summary>
        TYPEOF,    // typeof
        /// <summary>
        /// WIP
        /// </summary>
        NEXT,       // TO-DO Add remaining methods
        /// <summary>
        /// WIP
        /// </summary>
        COMPARE,
        /// <summary>
        /// WIP
        /// </summary>
        CALL,
        /// <summary>
        /// WIP
        /// </summary>
        NEWSLOT,
        /// <summary>
        /// WIP
        /// </summary>
        DELSLOT,
        /// <summary>
        /// WIP
        /// </summary>
        NEWMEMBER,
        /// <summary>
        /// WIP
        /// </summary>
        INHERIT,
        /// <summary>
        /// WIP
        /// </summary>
        STRING
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
        /// Don't use custom console title
        /// </summary>
        NOTITLE = 1 << 1,
        /// <summary>
        /// Don't print In and Out
        /// </summary>
        NOINOUT = 1 << 2,
        /// <summary>
        /// Wheter to delete file read after attempting to execute
        /// </summary>
        DELETEONPOST = 1 << 3,
        /// <summary>
        /// Don't print version information
        /// </summary>
        NOINFO = 1 << 4,
        /// <summary>
        /// Short console help
        /// </summary>
        HELP = 1 << 5
    }

    /// <summary>
    /// Delegate for methods to process console parameters
    /// </summary>
    /// <param name="arg">Argument to process</param>
    /// <returns>Numeric, bool or string value</returns>
    public delegate dynamic ExConsoleParameter(string arg);

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
        INTERRUPTEDINSLEEP = 1 << 5,
        /// <summary>
        /// Wheter to block printing OUT[n] prefix
        /// </summary>
        DONTPRINTOUTPREFIX = 1 << 6
    }

    /// <summary>
    /// Outer variable types
    /// </summary>
    public enum ExOuterType
    {
        /// <summary>
        /// Local var
        /// </summary>
        LOCAL,
        /// <summary>
        /// Outer val
        /// </summary>
        OUTER
    }

    /// <summary>
    /// Arithmetic operation masks
    /// </summary>
    public enum ArithmeticMask
    {
        /// <summary>
        /// Integers
        /// </summary>
        INT = ExObjType.INTEGER,
        /// <summary>
        /// Integer-complex
        /// </summary>
        INTCOMPLEX = ExObjType.COMPLEX | ExObjType.INTEGER,

        /// <summary>
        /// Floats
        /// </summary>
        FLOAT = ExObjType.FLOAT,
        /// <summary>
        /// Float-integer
        /// </summary>
        FLOATINT = ExObjType.INTEGER | ExObjType.FLOAT,
        /// <summary>
        /// Float-complex
        /// </summary>
        FLOATCOMPLEX = ExObjType.COMPLEX | ExObjType.FLOAT,

        /// <summary>
        /// Complex numbers
        /// </summary>
        COMPLEX = ExObjType.COMPLEX,

        /// <summary>
        /// Strings
        /// </summary>
        STRING = ExObjType.STRING,
        /// <summary>
        /// String-integer
        /// </summary>
        STRINGINT = ExObjType.STRING | ExObjType.INTEGER,
        /// <summary>
        /// String-float
        /// </summary>
        STRINGFLOAT = ExObjType.STRING | ExObjType.FLOAT,
        /// <summary>
        /// String-complex
        /// </summary>
        STRINGCOMPLEX = ExObjType.STRING | ExObjType.COMPLEX,
        /// <summary>
        /// String-bool
        /// </summary>
        STRINGBOOL = ExObjType.STRING | ExObjType.BOOL,
        /// <summary>
        /// String-null
        /// </summary>
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

    /// <summary>
    /// Internal, for safer process data getter
    /// </summary>
    public enum ExProcessInfo
    {
        /// <summary>
        /// Start date of the process
        /// </summary>
        DATE,
        /// <summary>
        /// Main module of the process
        /// </summary>
        MODULE,
        /// <summary>
        /// Start arguments of the process
        /// </summary>
        ARGS
    }

    /// <summary>
    /// Types of functions
    /// </summary>
    public enum ExClosureType
    {
        /// <summary>
        /// Default function type
        /// </summary>
        DEFAULT,   // Varsayılan fonksiyon türü
        /// <summary>
        /// Rule definition
        /// </summary>
        RULE,       // Kural, her zaman boolean dönen tür
        /// <summary>
        /// Cluster definition
        /// </summary>
        CLUSTER,    // Küme, tanım kümesindeki bir değerin görüntü kümesi karşılığını dönen tür 
        /// <summary>
        /// Sequence definition
        /// </summary>
        SEQUENCE    // Dizi, optimize edilmiş tekrarlı fonksiyon türü
    }

    /// <summary>
    /// <see cref="ExClosureType.DEFAULT"/> closure's function declaration type
    /// </summary>
    public enum ExFuncType
    {
        /// <summary>
        /// Default, nothing special
        /// </summary>
        DEFAULT,
        /// <summary>
        /// Lambda function
        /// </summary>
        LAMBDA,
        /// <summary>
        /// Class constructor method
        /// </summary>
        CONSTRUCTOR
    }
}
