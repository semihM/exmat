using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using ExMat.API;
using ExMat.Exceptions;
using ExMat.Objects;
using ExMat.Utils;
using ExMat.VM;

namespace ExMat.StdLib
{
    [ExStdLibBase(ExStdLibType.BASE)]
    [ExStdLibName(ExMat.StandardBaseLibraryName)]
    [ExStdLibRegister(nameof(Registery))]
    [ExStdLibConstDict(nameof(Constants))]
    public static partial class ExStdBase
    {
        #region FUNCTIONS
        [ExNativeFuncBase("help", ExBaseType.STRING, "Get and print the built-in help information of a native function. Printing can be disabled with the 2nd parameter.")]
        [ExNativeParamBase(1, "func_or_name", "s|Y|e", "Native function itself or the name of the native function", "help")]
        [ExNativeParamBase(2, "print", ".", "Wheter to print the information", true)]
        public static ExFunctionStatus StdHelp(ExVM vm, int nargs)
        {
            ExObject o = nargs >= 1 ? vm.GetArgument(1) : new();
            bool print = nargs < 2 || vm.GetArgument(2).GetBool();
            string docs;
            switch (o.Type)
            {
                case ExObjType.STRING:
                    {
                        string name = o.GetString();
                        if (vm.RootDictionary.GetDict().ContainsKey(name))
                        {
                            ExObject found = vm.RootDictionary.GetDict()[name];
                            if (found.Type != ExObjType.NATIVECLOSURE)
                            {
                                return vm.AddToErrorMessage("Expected a native function. '{0}' is not a native function, it is type '{1}'!", name, found.Type.ToString());
                            }

                            docs = found.GetNClosure().Documentation;

                            if (print)
                            {
                                vm.PrintLine(docs);
                            }
                            return vm.CleanReturn(nargs + 2, docs);
                        }
                        return vm.AddToErrorMessage("Unknown native function: {0}. Perhaps it is a delegate? Try passing the function directly.", name);
                    }
                default:
                    {
                        docs = nargs < 1 ? vm.GetRootClosure().Documentation : o.GetNClosure().Documentation;

                        if (print)
                        {
                            vm.PrintLine(docs);
                        }
                        return vm.CleanReturn(nargs + 2, docs);
                    }
            }
        }

        [ExNativeFuncBase("root", ExBaseType.DICT, "Get the root table.")]
        public static ExFunctionStatus StdRoot(ExVM vm, int nargs)
        {
            vm.Pop(nargs + 2);
            ExApi.PushRootTable(vm);
            return ExFunctionStatus.SUCCESS;
        }

        [ExNativeFuncBase("consts", ExBaseType.DICT, "Get the consts table. This table allows changing constant values!")]
        public static ExFunctionStatus StdConsts(ExVM vm, int nargs)
        {
            vm.Pop(nargs + 2);
            ExApi.PushConstsTable(vm);
            return ExFunctionStatus.SUCCESS;
        }

        [ExNativeFuncBase("sleep", ExBaseType.BOOL, "Sleeps main thread given amount of time. Returns 'true' when thread wakes up.")]
        [ExNativeParamBase(1, "miliseconds", "r", "Miliseconds to sleep the thread")]
        public static ExFunctionStatus StdSleep(ExVM vm, int nargs)
        {
            ExObject sleep = vm.GetArgument(1);
            int time = sleep.Type == ExObjType.FLOAT ? (int)sleep.GetFloat() : (int)sleep.GetInt();

            if (time >= 0)
            {
                ExApi.SleepVM(vm, time);
            }

            return vm.CleanReturn(nargs + 2, true);
        }

        [ExNativeFuncBase("to_base64", ExBaseType.STRING, "Convert given string to it's base64 representation.")]
        [ExNativeParamBase(1, "source", "s", "Source string to convert")]
        [ExNativeParamBase(2, "encoding", "s", "Encoding of the 'source'", "utf-8")]
        public static ExFunctionStatus StdToBase64(ExVM vm, int nargs)
        {
            string str = vm.GetArgument(1).GetString();
            Encoding enc = ExApi.DecideEncodingFromString(nargs > 1 ? vm.GetArgument(2).GetString() : "utf-8");
            return vm.CleanReturn(nargs + 2, Convert.ToBase64String(enc.GetBytes(str)));
        }

        [ExNativeFuncBase("from_base64", ExBaseType.STRING, "Convert given string from it's base64 representation to original.")]
        [ExNativeParamBase(1, "source", "s", "Source string to convert")]
        [ExNativeParamBase(2, "encoding", "s", "Encoding of the 'source'", "utf-8")]
        public static ExFunctionStatus StdFromBase64(ExVM vm, int nargs)
        {
            string str = vm.GetArgument(1).GetString();
            Encoding enc = ExApi.DecideEncodingFromString(nargs > 1 ? vm.GetArgument(2).GetString() : "utf-8");
            return vm.CleanReturn(nargs + 2, enc.GetString(Convert.FromBase64String(str)));
        }

        [ExNativeFuncBase("print", "Print messages or objects to immediate console.")]
        [ExNativeParamBase(1, "message", ".", "Message or object")]
        [ExNativeParamBase(2, "depth", "n", "Printing depth for objects, 1 = Object's string representation, 2 = Stringify first level objects inside lists/dictionaries, ...", 2)]
        public static ExFunctionStatus StdPrint(ExVM vm, int nargs)
        {
            if (!ExApi.ConvertAndGetString(vm, 1, nargs == 2 ? (int)vm.GetPositiveIntegerArgument(2, 1) : 2, out string output))
            {
                return ExFunctionStatus.ERROR;
            }

            vm.Print(output);   // Konsola çıktıyı yazdır
            return ExFunctionStatus.VOID;           // Dönülen değer yok ( boş değer )
        }

        [ExNativeFuncBase("printl", "Print messages or objects to immediate console with a new line '\\n' at the end")]
        [ExNativeParamBase(1, "message", ".", "Message or object")]
        [ExNativeParamBase(2, "depth", "n", "Printing depth for objects, 1 = Object's string representation, 2 = Stringify first level objects inside lists/dictionaries, ...", 2)]
        public static ExFunctionStatus StdPrintl(ExVM vm, int nargs)
        {
            if (!ExApi.ConvertAndGetString(vm, 1, nargs == 2 ? (int)vm.GetPositiveIntegerArgument(2, 1) : 2, out string output))
            {
                return ExFunctionStatus.ERROR;
            }

            vm.PrintLine(output);
            return ExFunctionStatus.VOID;
        }

        [ExNativeFuncBase("printf", ExBaseType.NULL, "Print messages or objects to immediate console with given format string and objects to replace.\n\tExample: printf(\"{0}, {1}\", \"first\", \"second\") == print(\"first, second\")", -2)]
        public static ExFunctionStatus StdPrintf(ExVM vm, int nargs)
        {
            if (ExApi.GetFormatStringAndObjects(vm, nargs, out string format, out object[] ps) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }

            try
            {
                vm.Print(string.Format(CultureInfo.CurrentCulture, format, ps));
                return ExFunctionStatus.VOID;
            }
            catch
            {
                return vm.AddToErrorMessage("not enough arguments given for format string");
            }
        }
        [ExNativeFuncBase("type", ExBaseType.STRING, "Get the type of an object as a string.")]
        [ExNativeParamBase(1, "object", ".", "Object to get the type of")]
        public static ExFunctionStatus StdType(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, new ExObject(vm.GetArgument(1).Type.ToString()));
        }

        [ExNativeFuncBase("time", ExBaseType.FLOAT, "Get how long the VM has been alive in miliseconds.")]
        public static ExFunctionStatus StdTime(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, new ExObject((DateTime.Now - vm.StartingTime).TotalMilliseconds));
        }

        [ExNativeFuncBase("date", ExBaseType.STRING | ExBaseType.ARRAY, "Get current date information.")]
        [ExNativeParamBase(1, "type", "s", ExMat.DateTypeInfoString, "today")]
        [ExNativeParamBase(2, "short_format", ".", "Get the shorter format of date values", false)]
        public static ExFunctionStatus StdDate(ExVM vm, int nargs)
        {
            bool shrt = false;

            switch (nargs)
            {
                case 2:
                    {
                        shrt = vm.GetArgument(2).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        string[] splt = vm.GetArgument(1).GetString().Split("|", StringSplitOptions.RemoveEmptyEntries);
                        List<ExObject> res = new(splt.Length);

                        GetDateFromStringArgument(shrt, splt, res);

                        return res.Count == 1 ? vm.CleanReturn(nargs + 2, new ExObject(res[0])) : vm.CleanReturn(nargs + 2, new ExObject(res));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2,
                            new ExObject(new List<ExObject>()
                                {
                                    new(DateTime.Today.ToLongDateString()),
                                    new(DateTime.Now.ToLongTimeString()),
                                    new(DateTime.Now.Millisecond.ToString(CultureInfo.CurrentCulture))
                                })
                            );
                    }
            }
        }

        [ExNativeFuncBase("assert", "Assert given 'condition' is true as bool. Throws assertion error with given message if 'condition' is false as bool")]
        [ExNativeParamBase(1, "condition", ".", "Condition to assert 'true'")]
        [ExNativeParamBase(2, "message", "s", "Assertion error message", "")]
        public static ExFunctionStatus StdAssert(ExVM vm, int nargs)
        {
            if (nargs == 2)
            {
                if (vm.GetArgument(1).GetBool())
                {
                    vm.Pop(4);
                    return ExFunctionStatus.VOID;
                }
                else
                {
                    string m = vm.GetArgument(2).GetString();
                    vm.Pop(4);
                    return vm.AddToErrorMessage("ASSERT FAILED: " + m);
                }
            }
            bool b = vm.GetArgument(1).GetBool();
            vm.Pop(3);
            vm.AddToErrorMessage("ASSERT FAILED!");
            return b ? ExFunctionStatus.VOID : ExFunctionStatus.ERROR;
        }

        private static void GetDateFromStringArgument(bool shrt, string[] splt, List<ExObject> res)
        {
            DateTime now = DateTime.Now;
            DateTime today = DateTime.Today;
            DateTime utcnow = DateTime.UtcNow;

            foreach (string arg in splt)
            {
                switch (arg.ToLower(CultureInfo.CurrentCulture))
                {
                    case "today":
                        {
                            res.Add(new ExObject(shrt ? today.ToShortDateString() : today.ToLongDateString()));
                            break;
                        }
                    case "now":
                    case "time":
                        {
                            res.Add(new ExObject(shrt ? now.ToShortTimeString() : now.ToLongTimeString()));
                            break;
                        }
                    case "year":
                        {
                            res.Add(new ExObject(now.Year.ToString(CultureInfo.CurrentCulture)));
                            break;
                        }
                    case "month":
                        {
                            res.Add(new ExObject(now.Month.ToString(CultureInfo.CurrentCulture)));
                            break;
                        }
                    case "day":
                    case "wday":
                        {
                            res.Add(new ExObject(now.DayOfWeek.ToString()));
                            break;
                        }
                    case "mday":
                        {
                            res.Add(new ExObject(now.Day.ToString(CultureInfo.CurrentCulture)));
                            break;
                        }
                    case "yday":
                        {
                            res.Add(new ExObject(now.DayOfYear.ToString(CultureInfo.CurrentCulture)));
                            break;
                        }
                    case "hours":
                    case "hour":
                    case "hh":
                    case "h":
                        {
                            res.Add(new ExObject(now.Hour.ToString(CultureInfo.CurrentCulture)));
                            break;
                        }
                    case "minutes":
                    case "minute":
                    case "min":
                    case "mm":
                    case "m":
                        {
                            res.Add(new ExObject(now.Minute.ToString(CultureInfo.CurrentCulture)));
                            break;
                        }
                    case "seconds":
                    case "second":
                    case "sec":
                    case "ss":
                    case "s":
                        {
                            res.Add(new ExObject(now.Second.ToString(CultureInfo.CurrentCulture)));
                            break;
                        }
                    case "miliseconds":
                    case "milisecond":
                    case "ms":
                        {
                            res.Add(new ExObject(now.Millisecond.ToString(CultureInfo.CurrentCulture)));
                            break;
                        }
                    case "utc":
                        {
                            res.Add(new ExObject(shrt ? utcnow.ToShortDateString() : utcnow.ToLongDateString()));
                            res.Add(new ExObject(shrt ? utcnow.ToShortTimeString() : utcnow.ToLongTimeString()));
                            res.Add(new ExObject(utcnow.Millisecond.ToString(CultureInfo.CurrentCulture)));
                            break;
                        }
                    case "utc-today":
                        {
                            res.Add(new ExObject(shrt ? utcnow.ToShortDateString() : utcnow.ToLongDateString()));
                            break;
                        }
                    case "utc-now":
                    case "utc-time":
                        {
                            res.Add(new ExObject(shrt ? utcnow.ToShortTimeString() : utcnow.ToLongTimeString()));
                            break;
                        }
                    case "utc-year":
                        {
                            res.Add(new ExObject(utcnow.Month.ToString(CultureInfo.CurrentCulture)));
                            break;
                        }
                    case "utc-month":
                        {
                            res.Add(new ExObject(utcnow.Month.ToString(CultureInfo.CurrentCulture)));
                            break;
                        }
                    case "utc-day":
                    case "utc-wday":
                        {
                            res.Add(new ExObject(utcnow.DayOfWeek.ToString()));
                            break;
                        }
                    case "utc-mday":
                        {
                            res.Add(new ExObject(utcnow.Day.ToString(CultureInfo.CurrentCulture)));
                            break;
                        }
                    case "utc-yday":
                        {
                            res.Add(new ExObject(utcnow.DayOfYear.ToString(CultureInfo.CurrentCulture)));
                            break;
                        }
                    case "utc-h":
                    case "utc-hh":
                    case "utc-hour":
                    case "utc-hours":
                        {
                            res.Add(new ExObject(utcnow.Hour.ToString(CultureInfo.CurrentCulture)));
                            break;
                        }
                }
            }
        }

        // BASIC CLASS-LIKE FUNCTIONS
        [ExNativeFuncBase("complex", ExBaseType.COMPLEX, "Create a complex number with given magnitute and phase")]
        [ExNativeParamBase(1, "real_part", "n", "Real part of the number", 0.0)]
        [ExNativeParamBase(2, "img_part", "n", "Imaginary part of the number", 0.0)]
        public static ExFunctionStatus StdComplex(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 2:
                    {
                        ExObject o = vm.GetArgument(1);
                        return o.Type == ExObjType.COMPLEX
                            ? vm.CleanReturn(4, new Complex(o.GetComplex().Real, vm.GetArgument(2).GetFloat()))
                            : vm.CleanReturn(4, new Complex(o.GetFloat(), vm.GetArgument(2).GetFloat()));
                    }
                case 1:
                    {
                        ExObject obj = vm.GetArgument(1);
                        return obj.Type == ExObjType.COMPLEX
                            ? vm.CleanReturn(3, new Complex(obj.GetComplex().Real, obj.GetComplex().Imaginary))
                            : vm.CleanReturn(3, new Complex(obj.GetFloat(), 0));
                    }
                default:
                    {
                        return vm.CleanReturn(2, new Complex());
                    }
            }

        }

        [ExNativeFuncBase("complex2", ExBaseType.COMPLEX, "Create a complex number with given real and imaginary parts")]
        [ExNativeParamBase(1, "magnitute", "r", "Magnitute of the number", 0.0)]
        [ExNativeParamBase(2, "phase", "r", "Phase of the number", 0.0)]
        public static ExFunctionStatus StdComplex2(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 2:
                    {
                        return vm.CleanReturn(4, Complex.FromPolarCoordinates(vm.GetArgument(1).GetFloat(), vm.GetArgument(2).GetFloat()));
                    }
                case 1:
                    {
                        return vm.CleanReturn(3, Complex.FromPolarCoordinates(vm.GetArgument(1).GetFloat(), 0));
                    }
                default:
                    {
                        return vm.CleanReturn(2, new Complex());
                    }
            }
        }

        [ExNativeFuncBase("bool", ExBaseType.BOOL, "Get bool value of given object")]
        [ExNativeParamBase(1, "object", ".", "Object to bool value of", true)]
        public static ExFunctionStatus StdBool(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 1:
                    {
                        return vm.CleanReturn(3, vm.GetArgument(1).GetBool());
                    }
                default:
                    {
                        return vm.CleanReturn(2, true);
                    }
            }
        }

        [ExNativeFuncBase("string", ExBaseType.STRING, "Convert given object to a string. Allows character/integer lists to string conversion via 'is_char_array'.")]
        [ExNativeParamBase(1, "object", ".", "Object to stringify", "")]
        [ExNativeParamBase(2, "is_char_array", ".", "Wheter to treat given 'object' as list of characters and join all by using them char values", false)]
        [ExNativeParamBase(3, "depth", "n", "Depth for objects to stringify", 2)]
        public static ExFunctionStatus StdString(ExVM vm, int nargs)
        {
            bool carr = false;
            int depth = 2;
            switch (nargs)
            {
                case 3:
                    {
                        depth = (int)vm.GetArgument(3).GetInt();
                        goto case 2;
                    }
                case 2:
                    {
                        carr = vm.GetArgument(2).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        if (carr)
                        {
                            return HandleCharArrayToString(vm, nargs, vm.GetArgument(1));
                        }
                        else if (!ExApi.ToString(vm, 2, depth, nargs + 2))
                        {
                            return ExFunctionStatus.ERROR;
                        }

                        return ExFunctionStatus.SUCCESS;
                    }
                default:
                    {
                        return vm.CleanReturn(2, string.Empty);
                    }
            }
        }

        [ExNativeFuncBase("float", ExBaseType.FLOAT, "Parse given object as a 64bit float")]
        [ExNativeParamBase(1, "object", ".", "Object to parse as float", 0.0)]
        public static ExFunctionStatus StdFloat(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 1:
                    {
                        return !ExApi.ToFloatFromStack(vm, 2, 3) ? ExFunctionStatus.ERROR : ExFunctionStatus.SUCCESS;
                    }
                default:
                    {
                        return vm.CleanReturn(2, 0.0);
                    }
            }
        }

        [ExNativeFuncBase("integer", ExBaseType.INTEGER, "Parse given object as an integer")]
        [ExNativeParamBase(1, "object", ".", "Object to parse as 64bit integer", 0)]
        public static ExFunctionStatus StdInteger(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 1:
                    {
                        return !ExApi.ToIntegerFromStack(vm, 2, 3) ? ExFunctionStatus.ERROR : ExFunctionStatus.SUCCESS;
                    }
                default:
                    {
                        return vm.CleanReturn(2, 0);
                    }
            }
        }

        [ExNativeFuncBase("bits", ExBaseType.ARRAY, "Get the 32bit representation of a real number as a list of 0 and 1 values")]
        [ExNativeParamBase(1, "number", "r", "32bit number", 0)]
        [ExNativeParamBase(2, "reverse", ".", "Reverse bit order", false)]
        public static ExFunctionStatus StdBits32(ExVM vm, int nargs)
        {
            bool reverse = false;
            switch (nargs)
            {
                case 2:
                    {
                        reverse = vm.GetArgument(2).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        ExObject v = vm.GetArgument(1);
                        long b = v.Type == ExObjType.INTEGER ? v.GetInt() : new DoubleLong() { f = v.GetFloat() }.i;
                        return Handle32BitConversion(vm, nargs, b, reverse);
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, new ExList());
                    }
            }
        }

        [ExNativeFuncBase("bits", ExBaseType.ARRAY, "Get the 64bit representation of a real number as a list of 0 and 1 values")]
        [ExNativeParamBase(1, "number", "r", "64bit number", 0)]
        [ExNativeParamBase(2, "reverse", ".", "Reverse bit order", false)]
        public static ExFunctionStatus StdBits(ExVM vm, int nargs)
        {
            bool reverse = false;
            switch (nargs)
            {
                case 2:
                    {
                        reverse = vm.GetArgument(2).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        ExObject v = vm.GetArgument(1);
                        long b = v.Type == ExObjType.INTEGER ? v.GetInt() : new DoubleLong() { f = v.GetFloat() }.i;
                        return Handle64BitConversion(vm, nargs, b, reverse);
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, new ExList());
                    }
            }
        }

        [ExNativeFuncBase("bytes", ExBaseType.ARRAY, "Get the 8 byte representation of a real number or all bytes of a string in a list")]
        [ExNativeParamBase(1, "object", "r|s", "64bit number or string to get bytes of", 0)]
        [ExNativeParamBase(2, "reverse", ".", "Reverse byte order", false)]
        public static ExFunctionStatus StdBytes(ExVM vm, int nargs)
        {
            bool reverse = true;
            switch (nargs)
            {
                case 2:
                    {
                        reverse = !vm.GetArgument(2).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        ExObject v = vm.GetArgument(1);
                        byte[] bytes = Array.Empty<byte>();
                        switch (v.Type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    bytes = BitConverter.GetBytes(v.GetInt());
                                    goto default;
                                }
                            case ExObjType.FLOAT:
                                {
                                    bytes = BitConverter.GetBytes(v.GetFloat());
                                    goto default;
                                }
                            case ExObjType.STRING:
                                {
                                    char[] chars = v.GetString().ToCharArray();
                                    List<ExObject> b = new(chars.Length);
                                    foreach (char i in chars)
                                    {
                                        b.Add(new(i));
                                    }
                                    if (!reverse)
                                    {
                                        b.Reverse();
                                    }

                                    return vm.CleanReturn(nargs + 2, b);
                                }
                            default:
                                {
                                    List<ExObject> b = new(bytes.Length);
                                    foreach (byte i in bytes)
                                    {
                                        b.Add(new(i));
                                    }
                                    if (!reverse)
                                    {
                                        b.Reverse();
                                    }

                                    return vm.CleanReturn(nargs + 2, b);
                                }
                        }
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, new ExList());
                    }
            }
        }

        [ExNativeFuncBase("binary", ExBaseType.ARRAY, "Get a list of zeros and ones which represent given 64bit number in binary format")]
        [ExNativeParamBase(1, "number", "r", "64bit number", 0)]
        [ExNativeParamBase(2, "add_0B_prefix", ".", "Wheter to add '0' and 'B' as first 2 elements of the list", true)]
        public static ExFunctionStatus StdBinary(ExVM vm, int nargs)
        {
            bool prefix = true;
            switch (nargs)
            {
                case 2:
                    {
                        prefix = vm.GetArgument(2).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        ExObject v = vm.GetArgument(1);
                        long b = 0;
                        switch (v.Type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    b = v.GetInt();
                                    goto default;
                                }
                            case ExObjType.FLOAT:
                                {
                                    b = new DoubleLong() { f = v.GetFloat() }.i;
                                    goto default;
                                }
                            default:
                                {
                                    List<ExObject> lis = new(prefix ? 66 : 64);
                                    if (prefix)
                                    {
                                        lis.Add(new("0"));
                                        lis.Add(new("B"));
                                    }

                                    foreach (int bit in ExApi.GetBits(b, 64))
                                    {
                                        lis.Add(new(bit.ToString(CultureInfo.CurrentCulture)));
                                    }

                                    return vm.CleanReturn(nargs + 2, lis);
                                }
                        }
                    }
                default:
                    {
                        List<ExObject> lis = new(prefix ? 66 : 64);
                        if (prefix)
                        {
                            lis.Add(new("0"));
                            lis.Add(new("B"));
                        }

                        for (int i = 0; i < 64; i++)
                        {
                            lis.Add(new("0"));
                        }
                        return vm.CleanReturn(nargs + 2, lis);
                    }
            }
        }

        [ExNativeFuncBase("binary32", ExBaseType.ARRAY, "Get a list of zeros and ones which represent given 32bit number in binary format")]
        [ExNativeParamBase(1, "number", "r", "32bit number", 0)]
        [ExNativeParamBase(2, "add_0b_prefix", ".", "Wheter to add '0' and 'b' as first 2 elements of the list", true)]
        public static ExFunctionStatus StdBinary32(ExVM vm, int nargs)
        {
            bool prefix = true;
            switch (nargs)
            {
                case 2:
                    {
                        prefix = vm.GetArgument(2).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        ExObject v = vm.GetArgument(1);
                        long b = 0;
                        switch (v.Type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    b = v.GetInt();
                                    goto default;
                                }
                            case ExObjType.FLOAT:
                                {
                                    b = new DoubleLong() { f = v.GetFloat() }.i;
                                    goto default;
                                }
                            default:
                                {
                                    if (b is > int.MaxValue or < int.MinValue)
                                    {
                                        return vm.AddToErrorMessage("64bit value out of range for 32bit use");
                                    }
                                    List<ExObject> lis = new(prefix ? 34 : 32);
                                    if (prefix)
                                    {
                                        lis.Add(new("0"));
                                        lis.Add(new("b"));
                                    }

                                    foreach (int bit in ExApi.GetBits(b, 32))
                                    {
                                        lis.Add(new(bit.ToString(CultureInfo.CurrentCulture)));
                                    }

                                    return vm.CleanReturn(nargs + 2, lis);
                                }
                        }
                    }
                default:
                    {
                        List<ExObject> lis = new(prefix ? 34 : 32);
                        if (prefix)
                        {
                            lis.Add(new("0"));
                            lis.Add(new("b"));
                        }

                        for (int i = 0; i < 32; i++)
                        {
                            lis.Add(new("0"));
                        }
                        return vm.CleanReturn(nargs + 2, lis);
                    }
            }
        }

        [ExNativeFuncBase("hex", ExBaseType.ARRAY, "Get a list of characters which represent given 64bit number in hexadecimal format")]
        [ExNativeParamBase(1, "number", "r", "64bit number", 0)]
        [ExNativeParamBase(2, "add_0x_prefix", ".", "Wheter to add '0' and 'x' as first 2 elements of the list", true)]
        public static ExFunctionStatus StdHex(ExVM vm, int nargs)
        {
            bool prefix = true;
            switch (nargs)
            {
                case 2:
                    {
                        prefix = vm.GetArgument(2).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        ExObject v = vm.GetArgument(1);
                        long b = 0;
                        switch (v.Type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    b = v.GetInt();
                                    goto default;
                                }
                            case ExObjType.FLOAT:
                                {
                                    b = new DoubleLong() { f = v.GetFloat() }.i;
                                    goto default;
                                }
                            default:
                                {
                                    char[] chars = b.ToString("X16", CultureInfo.CurrentCulture).ToCharArray();
                                    List<ExObject> lis = new(chars.Length + (prefix ? 2 : 0));
                                    if (prefix)
                                    {
                                        lis.Add(new("0"));
                                        lis.Add(new("x"));
                                    }
                                    foreach (char i in chars)
                                    {
                                        lis.Add(new(i.ToString(CultureInfo.CurrentCulture)));
                                    }
                                    return vm.CleanReturn(nargs + 2, lis);
                                }
                        }
                    }
                default:
                    {
                        List<ExObject> lis = new(prefix ? 18 : 16);
                        if (prefix)
                        {
                            lis.Add(new("0"));
                            lis.Add(new("x"));
                        }

                        for (int i = 0; i < 16; i++)
                        {
                            lis.Add(new("0"));
                        }
                        return vm.CleanReturn(nargs + 2, lis);
                    }
            }
        }

        // Functional
        [ExNativeFuncBase("map", ExBaseType.ARRAY, "Map a list to a new list with a function. A secondary list can be given to iterate over simultaneously to pass a 2nd parameter to mapping function")]
        [ExNativeParamBase(1, "func", "c|y", "Mapping function, single parameter (2 parameters if 'alt_list' is used)")]
        [ExNativeParamBase(2, "list", "a", "List to iterate over")]
        [ExNativeParamBase(3, "alt_list", "a", "A secondary optional list to iterate with 'list'.", def: null)]
        public static ExFunctionStatus StdMap(ExVM vm, int nargs)
        {
            ExObject cls = vm.GetArgument(1);
            ExObject obj = new(vm.GetArgument(2));
            ExObject obj2 = nargs == 3 ? new(vm.GetArgument(3)) : null;

            vm.Pop(nargs - 1);

            ExObject tmp = new();

            int n = 2;
            int m = 0;

            int argcount = obj.GetList().Count;

            List<ExObject> l = new(argcount);

            switch (cls.Type)
            {
                case ExObjType.CLOSURE:
                    {
                        if (cls.GetClosure().Function.IsSequence())
                        {
                            bool _bm = vm.IsMainCall;
                            vm.IsMainCall = false;
                            List<ExObject> _defs = cls.GetClosure().Function.Parameters;
                            List<string> defs = new(_defs.Count);
                            for (int i = 0; i < _defs.Count; i++)
                            {
                                defs.Add(_defs[i].GetString());
                            }

                            if (obj2 != null)
                            {
                                if (obj2.GetList().Count != argcount)
                                {
                                    return vm.AddToErrorMessage("expected same length for both arrays while mapping");
                                }
                                n++;

                                for (int i = 0; i < argcount; i++)
                                {
                                    ExObject o = obj.GetList()[i];
                                    ExObject o2 = obj2.GetList()[i];
                                    vm.Push(cls);
                                    ExApi.PushRootTable(vm);

                                    vm.Push(o);
                                    vm.Push(o2);
                                    if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                                    {
                                        vm.Pop();
                                        vm.IsMainCall = _bm;
                                        return ExFunctionStatus.ERROR;
                                    }
                                    else if (defs.IndexOf(o.GetInt().ToString(CultureInfo.CurrentCulture)) != -1)  // TO-DO fix this mess
                                    {
                                        l.Add(new(vm.GetAt(vm.StackTop - n - 1)));
                                        vm.Pop(n + 1 + m);
                                    }
                                    else
                                    {
                                        vm.Pop(n + 1 + m);
                                        l.Add(new(tmp));
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < argcount; i++)
                                {
                                    ExObject o = obj.GetList()[i];
                                    vm.Push(cls);
                                    ExApi.PushRootTable(vm);

                                    vm.Push(o);
                                    if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                                    {
                                        vm.Pop();
                                        vm.IsMainCall = _bm;
                                        return ExFunctionStatus.ERROR;
                                    }
                                    else if (defs.IndexOf(o.GetInt().ToString(CultureInfo.CurrentCulture)) != -1)  // TO-DO fix this mess
                                    {
                                        l.Add(new(vm.GetAt(vm.StackTop - n - 1)));
                                        vm.Pop(n + 1 + m);
                                    }
                                    else
                                    {
                                        vm.Pop(n + 1 + m);
                                        l.Add(new(tmp));
                                    }
                                }
                            }

                            vm.IsMainCall = _bm;
                            return vm.CleanReturn(n + m + 1, new ExObject(l));
                        }
                        break;
                    }
                case ExObjType.NATIVECLOSURE:
                    {
                        if (cls.GetNClosure().IsDelegateFunction)
                        {
                            n--;
                            m++;
                        }
                        break;
                    }
            }

            bool bm = vm.IsMainCall;
            vm.IsMainCall = false;

            if (obj2 != null && obj2.Type == ExObjType.ARRAY)
            {
                if (obj2.GetList().Count != argcount)
                {
                    return vm.AddToErrorMessage("expected same length for both arrays while mapping");
                }
                n++;

                for (int i = 0; i < obj.GetList().Count; i++)
                {
                    ExObject o = obj.GetList()[i];
                    ExObject o2 = obj2.GetList()[i];
                    vm.Push(cls);
                    ExApi.PushRootTable(vm);

                    vm.Push(o);
                    vm.Push(o2);
                    if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                    {
                        vm.Pop();
                        vm.IsMainCall = bm;
                        return ExFunctionStatus.ERROR;
                    }
                    else
                    {
                        vm.Pop(n + 1 + m);
                        l.Add(new(tmp));
                    }
                }
            }
            else
            {
                foreach (ExObject o in obj.GetList())
                {
                    vm.Push(cls);
                    ExApi.PushRootTable(vm);

                    vm.Push(o);
                    if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                    {
                        vm.Pop();
                        vm.IsMainCall = bm;
                        return ExFunctionStatus.ERROR;
                    }
                    else
                    {
                        vm.Pop(n + 1 + m);
                        l.Add(new(tmp));
                    }
                }
            }

            vm.IsMainCall = bm;
            return vm.CleanReturn(n + m + 1, new ExObject(l));
        }

        [ExNativeFuncBase("filter", ExBaseType.ARRAY, "Filter a list with a filtering function and return a new list")]
        [ExNativeParamBase(1, "func", "c", "Filter function, single parameter")]
        [ExNativeParamBase(2, "list", "a", "List to iterate over")]
        public static ExFunctionStatus StdFilter(ExVM vm, int nargs)
        {
            ExObject cls = vm.GetArgument(1);
            ExObject obj = new(vm.GetArgument(2));
            List<ExObject> l = new(obj.GetList().Count);

            vm.Pop();

            bool iscls = cls.Type == ExObjType.CLOSURE;

            if (!iscls && cls.Type != ExObjType.NATIVECLOSURE)
            {
                return vm.AddToErrorMessage("can't call non-closure type");
            }

            ExObject tmp = new();

            int n = 2;
            int m = 0;
            if (!iscls && cls.GetNClosure().IsDelegateFunction)
            {
                n--;
                m++;
            }
            bool bm = vm.IsMainCall;
            vm.IsMainCall = false;
            foreach (ExObject o in obj.GetList())
            {
                vm.Push(cls);
                ExApi.PushRootTable(vm);

                vm.Push(o);
                if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                {
                    vm.Pop();
                    vm.IsMainCall = bm;
                    return ExFunctionStatus.ERROR;
                }
                else
                {
                    vm.Pop(n + 1 + m);
                    if (tmp.GetBool())
                    {
                        l.Add(new(o));
                    }
                }
            }

            vm.IsMainCall = bm;
            return vm.CleanReturn(n + m + 1, new ExObject(l));
        }

        [ExNativeFuncBase("call", 0, "Call the function in the first argument with other arguments passed to it", -2)]
        public static ExFunctionStatus StdCall(ExVM vm, int nargs)
        {
            ExObject cls = vm.GetArgument(1);
            ExObject res = new();
            bool is_cluster = false,
                 is_seq = false,
                 is_deleg = false;

            switch (cls.Type)
            {
                case ExObjType.CLOSURE:
                    {
                        is_cluster = cls.GetClosure().Function.IsCluster();
                        is_seq = cls.GetClosure().Function.IsSequence();
                        break;
                    }
                case ExObjType.NATIVECLOSURE:
                    {
                        is_deleg = cls.GetNClosure().IsDelegateFunction;
                        break;
                    }
                default:
                    {
                        return vm.AddToErrorMessage("can't call non-closure type");
                    }
            }

            List<ExObject> args = new();

            vm.FillArgumentArray(args, nargs);
            args.Reverse();

            if (!is_deleg)
            {
                nargs = args.Count + 1;
                ExApi.PushRootTable(vm);
            }
            else
            {
                nargs = args.Count;
            }

            if (is_cluster)
            {
                if (!PushArgsForCluster(vm, cls, args, ref nargs, false))
                {
                    return ExFunctionStatus.ERROR;
                }
            }
            else
            {
                vm.PushParse(args);
            }

            bool bm = vm.IsMainCall;
            vm.IsMainCall = false;

            if (ExApi.Call(vm, nargs, true, true))
            {
                res.Assign(vm.GetAbove(is_seq ? -nargs : -1)); // ExApi.GetFromStack(vm, nargs - (iscls ? 1 : 0))
                vm.Pop();
            }
            else
            {
                vm.IsMainCall = bm;
                return ExFunctionStatus.ERROR;
            }

            vm.IsMainCall = bm;
            return vm.CleanReturn(3, res);
        }

        [ExNativeFuncBase("parse", 0, "Parse a list of arguments to a function or a class")]
        [ExNativeParamBase(1, "func_or_class", "c|y", "Function or class for parsing array of arguments to")]
        [ExNativeParamBase(2, "list", "a", "Array of arguments for the function or class")]
        public static ExFunctionStatus StdParse(ExVM vm, int nargs)
        {
            ExObject cls = vm.GetArgument(1);
            List<ExObject> args = new ExObject(vm.GetArgument(2)).GetList();
            if (args.Count > vm.StackSize - vm.StackTop - 3)
            {
                vm.AddToErrorMessage("stack size is too small for parsing " + args.Count + " arguments! Current size: " + vm.StackSize);
                return ExFunctionStatus.ERROR;
            }

            bool is_cluster = false,
                 is_seq = false,
                 is_deleg = false;

            switch (cls.Type)
            {
                case ExObjType.CLOSURE:
                    {
                        is_cluster = cls.GetClosure().Function.IsCluster();
                        is_seq = cls.GetClosure().Function.IsSequence();
                        break;
                    }
                case ExObjType.NATIVECLOSURE:
                    {
                        is_deleg = cls.GetNClosure().IsDelegateFunction;
                        break;
                    }
                default:
                    {
                        return vm.AddToErrorMessage("can't call non-closure type");
                    }
            }

            vm.Pop();

            if (is_deleg)
            {
                nargs = args.Count;
            }
            else
            {
                nargs = args.Count + 1;
                ExApi.PushRootTable(vm);
            }

            if (is_cluster)
            {
                if (!PushArgsForCluster(vm, cls, args, ref nargs))
                {
                    return ExFunctionStatus.ERROR;
                }
            }
            else
            {
                vm.PushParse(args);
            }

            ExObject tmp = new();
            bool bm = vm.IsMainCall;
            vm.IsMainCall = false;
            if (!ExApi.Call(vm, nargs, true, false))
            {
                vm.Pop(4);
                vm.IsMainCall = bm;
                return ExFunctionStatus.ERROR;
            }

            tmp.Assign(vm.GetAbove(is_seq ? -nargs : -1));
            vm.IsMainCall = bm;

            return vm.CleanReturn(4, tmp);
        }

        [ExNativeFuncBase("iter", 0, "Iterate through a list and reduce with given function. Each iteration gets the returned value from the function on previous iteration.")]
        [ExNativeParamBase(1, "func_or_class", "c|y", "Function or class with 3 parameters: (<int> iteration, <var> previous_value,  <var> current_value). Returned value becomes previous_value for the next iteration")]
        [ExNativeParamBase(2, "list", "a", "List to iterate through")]
        [ExNativeParamBase(3, "start_value", ".", "This value is used as the 'previous_value' for the first iteration")]
        public static ExFunctionStatus StdIter(ExVM vm, int nargs)
        {
            ExObject cls = vm.GetArgument(1);
            ExObject obj = new(vm.GetArgument(2));
            ExObject prev = new(vm.GetArgument(3));

            vm.Pop();

            bool iscls = cls.Type == ExObjType.CLOSURE;

            if (!iscls && cls.Type != ExObjType.NATIVECLOSURE)
            {
                return vm.AddToErrorMessage("can't call non-closure type");
            }

            ExObject tmp = new();

            int n = 4;
            int m = 0;
            if (!iscls && cls.GetNClosure().IsDelegateFunction)
            {
                n--;
                m++;
            }
            bool bm = vm.IsMainCall;
            vm.IsMainCall = false;
            int i = 0;
            foreach (ExObject o in obj.GetList()) // TO-DO use for loop, remove need of 3rd arg in iter
            {
                vm.Push(cls);
                ExApi.PushRootTable(vm);
                vm.Push(o); // curr
                vm.Push(prev);  // prev
                vm.Push(i); // idx
                if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                {
                    vm.Pop();
                    vm.IsMainCall = bm;
                    return ExFunctionStatus.ERROR;
                }
                else
                {
                    vm.Pop(n + 1 + m);
                    prev.Assign(tmp);
                    i++;
                }
            }

            vm.IsMainCall = bm;
            return vm.CleanReturn(n + m, prev);
        }

        [ExNativeFuncBase("first", 0, "Find the first value meeting the given condition in a list")]
        [ExNativeParamBase(1, "func", "c", "Condition to be met. Single parameter function, gets passed list elements to it")]
        [ExNativeParamBase(2, "list", "a", "List to iterate through")]
        public static ExFunctionStatus StdFirst(ExVM vm, int nargs)
        {
            ExObject cls = vm.GetArgument(1);
            ExObject obj = new(vm.GetArgument(2));
            ExObject res = new();

            vm.Pop();

            bool iscls = cls.Type == ExObjType.CLOSURE;

            if (!iscls && cls.Type != ExObjType.NATIVECLOSURE)
            {
                return vm.AddToErrorMessage("can't call non-closure type");
            }

            ExObject tmp = new();

            int n = 2;
            int m = 0;
            if (!iscls && cls.GetNClosure().IsDelegateFunction)
            {
                n--;
                m++;
            }
            bool bm = vm.IsMainCall;
            vm.IsMainCall = false;
            foreach (ExObject o in obj.GetList())
            {
                vm.Push(cls);
                ExApi.PushRootTable(vm);

                vm.Push(o);
                if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                {
                    vm.Pop();
                    vm.IsMainCall = bm;
                    return ExFunctionStatus.ERROR;
                }
                else
                {
                    vm.Pop(n + 1 + m);
                    if (tmp.GetBool())
                    {
                        res.Assign(o);
                        break;
                    }
                }
            }

            vm.IsMainCall = bm;
            return vm.CleanReturn(n + m + 1, res);
        }

        [ExNativeFuncBase("all", ExBaseType.BOOL, "Check if all elements of a list meet the given condition")]
        [ExNativeParamBase(1, "func", "c", "Condition to be met. Single parameter function, gets passed list elements to it")]
        [ExNativeParamBase(2, "list", "a", "List to iterate through")]
        public static ExFunctionStatus StdAll(ExVM vm, int nargs)
        {
            ExObject cls = vm.GetArgument(1);
            ExObject obj = new(vm.GetArgument(2));

            vm.Pop();

            bool iscls = cls.Type == ExObjType.CLOSURE;

            if (!iscls && cls.Type != ExObjType.NATIVECLOSURE)
            {
                return vm.AddToErrorMessage("can't call non-closure type");
            }

            ExObject tmp = new();

            int n = 2;
            int m = 0;
            if (!iscls && cls.GetNClosure().IsDelegateFunction)
            {
                n--;
                m++;
            }

            bool found = obj.GetList().Count > 0;
            ExObject res = new(found);

            bool bm = vm.IsMainCall;
            vm.IsMainCall = false;
            foreach (ExObject o in obj.GetList())
            {
                vm.Push(cls);
                ExApi.PushRootTable(vm);

                vm.Push(o);
                if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                {
                    vm.Pop();
                    vm.IsMainCall = bm;
                    return ExFunctionStatus.ERROR;
                }
                else
                {
                    vm.Pop(n + 1 + m);
                    if (!tmp.GetBool())
                    {
                        res.Assign(false);
                        break;
                    }
                }
            }

            vm.IsMainCall = bm;
            return vm.CleanReturn(n + m + 1, res);
        }

        [ExNativeFuncBase("any", ExBaseType.BOOL, "Check if any element in a list meets the given condition")]
        [ExNativeParamBase(1, "func", "c", "Condition to be met. Single parameter function, gets passed list elements to it")]
        [ExNativeParamBase(2, "list", "a", "List to iterate through")]
        public static ExFunctionStatus StdAny(ExVM vm, int nargs)
        {
            ExObject cls = vm.GetArgument(1);
            ExObject obj = new(vm.GetArgument(2));

            vm.Pop();

            bool iscls = cls.Type == ExObjType.CLOSURE;

            if (!iscls && cls.Type != ExObjType.NATIVECLOSURE)
            {
                return vm.AddToErrorMessage("can't call non-closure type");
            }

            ExObject tmp = new();

            int n = 2;
            int m = 0;
            if (!iscls && cls.GetNClosure().IsDelegateFunction)
            {
                n--;
                m++;
            }

            bool found = false;
            ExObject res = new(false);

            bool bm = vm.IsMainCall;
            vm.IsMainCall = false;
            foreach (ExObject o in obj.GetList())
            {
                vm.Push(cls);
                ExApi.PushRootTable(vm);

                vm.Push(o);
                if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                {
                    vm.Pop();
                    vm.IsMainCall = bm;
                    return ExFunctionStatus.ERROR;
                }
                else
                {
                    vm.Pop(n + 1 + m);
                    if (tmp.GetBool())
                    {
                        found = true;
                        break;
                    }
                }
            }

            res.Assign(found);
            vm.IsMainCall = bm;
            return vm.CleanReturn(n + m + 1, res);
        }

        // List initializer functions
        [ExNativeFuncBase("list", ExBaseType.ARRAY, "Initialize an empty list or convert a string into a list of characters")]
        [ExNativeParamBase(1, "length", "n|s", "Length of the list or string to get the character list of", 0)]
        [ExNativeParamBase(2, "filler", ".", "Filler object to use while initializing the list", def: null)]
        public static ExFunctionStatus StdList(ExVM vm, int nargs)
        {
            ExObject o = vm.GetArgument(1);
            if (o.Type == ExObjType.STRING)
            {
                char[] s = o.GetString().ToCharArray();

                List<ExObject> lis = new(s.Length);

                foreach (char c in s)
                {
                    lis.Add(new(c.ToString(CultureInfo.CurrentCulture)));
                }

                return vm.CleanReturn(nargs + 2, lis);
            }
            else
            {
                ExList l = new();
                int s = (int)o.GetInt();
                if (s < 0)
                {
                    s = 0;
                }
                if (ExApi.GetTopOfStack(vm) > 2)
                {
                    l.ValueCustom.l_List = new(s);
                    ExUtils.InitList(ref l.ValueCustom.l_List, s, vm.GetArgument(2));
                }
                else
                {
                    l.ValueCustom.l_List = new(s);
                    ExUtils.InitList(ref l.ValueCustom.l_List, s);
                }

                return vm.CleanReturn(nargs + 2, l);
            }
        }

        [ExNativeFuncBase("rangei", ExBaseType.ARRAY, "Initialize a number range series with given inclusive start and end values and step information.")]
        [ExNativeParamBase(1, "start", "n", "Inclusive start value")]
        [ExNativeParamBase(2, "end", "n", "Inclusive end value", 0)]
        [ExNativeParamBase(3, "step", "n", "Step size between values", 1)]
        public static ExFunctionStatus StdRangei(ExVM vm, int nargs)
        {
            List<ExObject> l = new();
            ExObject s = vm.GetArgument(1);

            switch (s.Type)
            {
                case ExObjType.INTEGER:
                    {
                        long start = s.GetInt();
                        switch (nargs)
                        {
                            case 3:
                                {
                                    ExObject e = vm.GetArgument(2);
                                    ExObject d = vm.GetArgument(3);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                double end = e.GetFloat();
                                                switch (d.Type)
                                                {
                                                    case ExObjType.INTEGER:
                                                        {
                                                            long step = d.GetInt();
                                                            if (end > start)
                                                            {
                                                                int count = (int)((end - start) / step);

                                                                for (int i = 0; i <= count; i++)
                                                                {
                                                                    l.Add(new(start + (i * step)));
                                                                }
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.FLOAT:
                                                        {
                                                            double step = d.GetFloat();
                                                            if (end > start)
                                                            {
                                                                int count = (int)((end - start) / step);

                                                                for (int i = 0; i <= count; i++)
                                                                {
                                                                    l.Add(new(start + (i * step)));
                                                                }
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.COMPLEX:
                                                        {
                                                            Complex step = d.GetComplex();
                                                            if (end > start)
                                                            {
                                                                if (step.Real == 0)
                                                                {
                                                                    return vm.AddToErrorMessage("can't use real number 'start' and 'end' range with 0 real valued complex number 'step'");
                                                                }
                                                                int count = (int)((end - start) / step.Real);

                                                                for (int i = 0; i <= count; i++)
                                                                {
                                                                    l.Add(new(start + (i * step)));
                                                                }
                                                            }
                                                            break;
                                                        }
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                return vm.AddToErrorMessage("can't create range from real number to complex");
                                            }
                                    }
                                    break;
                                }

                            case 2:
                                {
                                    ExObject e = vm.GetArgument(2);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                double end = e.GetFloat();
                                                if (end > start)
                                                {
                                                    int count = (int)(end - start);

                                                    for (int i = 0; i <= count; i++)
                                                    {
                                                        l.Add(new(start + i));
                                                    }
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                return vm.AddToErrorMessage("can't create range from real number to complex");
                                            }
                                    }
                                    break;
                                }

                            case 1:
                                {
                                    for (int i = 0; i <= start; i++)
                                    {
                                        l.Add(new(i));
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                case ExObjType.FLOAT:
                    {
                        double start = s.GetFloat();
                        switch (nargs)
                        {
                            case 3:
                                {
                                    ExObject e = vm.GetArgument(2);
                                    ExObject d = vm.GetArgument(3);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                double end = e.GetFloat();
                                                switch (d.Type)
                                                {
                                                    case ExObjType.INTEGER:
                                                        {
                                                            double step = d.GetInt();
                                                            if (end > start)
                                                            {
                                                                int count = (int)((end - start) / step);

                                                                for (int i = 0; i <= count; i++)
                                                                {
                                                                    l.Add(new(start + (i * step)));
                                                                }
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.FLOAT:
                                                        {
                                                            double step = d.GetFloat();
                                                            if (end > start)
                                                            {
                                                                int count = (int)((end - start) / step);

                                                                for (int i = 0; i <= count; i++)
                                                                {
                                                                    l.Add(new(start + (i * step)));
                                                                }
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.COMPLEX:
                                                        {
                                                            Complex step = d.GetComplex();
                                                            if (end > start)
                                                            {
                                                                if (step.Real == 0)
                                                                {
                                                                    return vm.AddToErrorMessage("can't use real number 'start' and 'end' range with 0 real valued complex number 'step'");
                                                                }
                                                                int count = (int)((end - start) / step.Real);

                                                                for (int i = 0; i <= count; i++)
                                                                {
                                                                    l.Add(new(start + (i * step)));
                                                                }
                                                            }
                                                            break;
                                                        }
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                return vm.AddToErrorMessage("can't create range from real number to complex");
                                            }
                                    }
                                    break;
                                }

                            case 2:
                                {
                                    ExObject e = vm.GetArgument(2);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                double end = e.GetFloat();
                                                if (end > start)
                                                {
                                                    int count = (int)(end - start);

                                                    for (int i = 0; i <= count; i++)
                                                    {
                                                        l.Add(new(start + i));
                                                    }
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                return vm.AddToErrorMessage("can't create range from real number to complex");
                                            }
                                    }
                                    break;
                                }

                            case 1:
                                {
                                    for (int i = 0; i <= start; i++)
                                    {
                                        l.Add(new(i));
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex start = s.GetComplex();
                        switch (nargs)
                        {
                            case 3:
                                {
                                    ExObject e = vm.GetArgument(2);
                                    ExObject d = vm.GetArgument(3);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                long count = e.GetInt();
                                                switch (d.Type)
                                                {
                                                    case ExObjType.INTEGER:
                                                        {
                                                            double step = d.GetInt();
                                                            for (int i = 0; i <= count; i++)
                                                            {
                                                                l.Add(new(start + (i * step)));
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.FLOAT:
                                                        {
                                                            double step = d.GetFloat();
                                                            for (int i = 0; i <= count; i++)
                                                            {
                                                                l.Add(new(start + (i * step)));
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.COMPLEX:
                                                        {
                                                            Complex step = d.GetComplex();
                                                            for (int i = 0; i <= count; i++)
                                                            {
                                                                l.Add(new(start + (i * step)));
                                                            }
                                                            break;
                                                        }
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                return vm.AddToErrorMessage("expected integer as 2nd argument for complex number range(start, count, step)");
                                            }
                                    }
                                    break;
                                }

                            case 2:
                                {
                                    ExObject e = vm.GetArgument(2);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                long count = e.GetInt();
                                                for (int i = 0; i <= count; i++)
                                                {
                                                    l.Add(new(start + (i * start)));
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                return vm.AddToErrorMessage("expected integer as 2nd argument for complex number range(start, count)");
                                            }
                                    }
                                    break;
                                }

                            case 1:
                                {
                                    for (int i = 1; i < start.Real; i++)
                                    {
                                        l.Add(new(i * start));
                                    }
                                    break;
                                }
                        }
                        break;
                    }
            }
            return vm.CleanReturn(nargs + 2, l);
        }

        [ExNativeFuncBase("range", ExBaseType.ARRAY, "Initialize a number range series with given inclusive start and exclusive end values and step information.")]
        [ExNativeParamBase(1, "start", "n", "Inclusive start value")]
        [ExNativeParamBase(2, "end", "n", "Exclusive end value", 0)]
        [ExNativeParamBase(3, "step", "n", "Step size between values", 1)]
        public static ExFunctionStatus StdRange(ExVM vm, int nargs)
        {
            List<ExObject> l = new();
            ExObject s = vm.GetArgument(1);
            switch (s.Type)
            {
                case ExObjType.INTEGER:
                    {
                        long start = s.GetInt();
                        switch (nargs)
                        {
                            case 3:
                                {
                                    ExObject e = vm.GetArgument(2);
                                    ExObject d = vm.GetArgument(3);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                double end = e.GetFloat();
                                                switch (d.Type)
                                                {
                                                    case ExObjType.INTEGER:
                                                        {
                                                            long step = d.GetInt();
                                                            if (end > start)
                                                            {
                                                                int count = (int)((end - start) / step);

                                                                for (int i = 0; i < count; i++)
                                                                {
                                                                    l.Add(new(start + (i * step)));
                                                                }
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.FLOAT:
                                                        {
                                                            double step = d.GetFloat();
                                                            if (end > start)
                                                            {
                                                                int count = (int)((end - start) / step);

                                                                for (int i = 0; i < count; i++)
                                                                {
                                                                    l.Add(new(start + (i * step)));
                                                                }
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.COMPLEX:
                                                        {
                                                            Complex step = d.GetComplex();
                                                            if (end > start)
                                                            {
                                                                if (step.Real == 0)
                                                                {
                                                                    return vm.AddToErrorMessage("can't use real number 'start' and 'end' range with 0 real valued complex number 'step'");
                                                                }
                                                                int count = (int)((end - start) / step.Real);

                                                                for (int i = 0; i < count; i++)
                                                                {
                                                                    l.Add(new(start + (i * step)));
                                                                }
                                                            }
                                                            break;
                                                        }
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                return vm.AddToErrorMessage("can't create range from real number to complex");
                                            }
                                    }
                                    break;
                                }

                            case 2:
                                {
                                    ExObject e = vm.GetArgument(2);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                double end = e.GetFloat();
                                                if (end > start)
                                                {
                                                    int count = (int)(end - start);

                                                    for (int i = 0; i < count; i++)
                                                    {
                                                        l.Add(new(start + i));
                                                    }
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                return vm.AddToErrorMessage("can't create range from real number to complex");
                                            }
                                    }
                                    break;
                                }

                            case 1:
                                {
                                    for (int i = 0; i < start; i++)
                                    {
                                        l.Add(new(i));
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                case ExObjType.FLOAT:
                    {
                        double start = s.GetFloat();
                        switch (nargs)
                        {
                            case 3:
                                {
                                    ExObject e = vm.GetArgument(2);
                                    ExObject d = vm.GetArgument(3);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                double end = e.GetFloat();
                                                switch (d.Type)
                                                {
                                                    case ExObjType.INTEGER:
                                                        {
                                                            double step = d.GetInt();
                                                            if (end > start)
                                                            {
                                                                int count = (int)((end - start) / step);

                                                                for (int i = 0; i < count; i++)
                                                                {
                                                                    l.Add(new(start + (i * step)));
                                                                }
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.FLOAT:
                                                        {
                                                            double step = d.GetFloat();
                                                            if (end > start)
                                                            {
                                                                int count = (int)((end - start) / step);

                                                                for (int i = 0; i < count; i++)
                                                                {
                                                                    l.Add(new(start + (i * step)));
                                                                }
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.COMPLEX:
                                                        {
                                                            Complex step = d.GetComplex();
                                                            if (end > start)
                                                            {
                                                                if (step.Real == 0)
                                                                {
                                                                    return vm.AddToErrorMessage("can't use real number 'start' and 'end' range with 0 real valued complex number 'step'");
                                                                }
                                                                int count = (int)((end - start) / step.Real);

                                                                for (int i = 0; i < count; i++)
                                                                {
                                                                    l.Add(new(start + (i * step)));
                                                                }
                                                            }
                                                            break;
                                                        }
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                return vm.AddToErrorMessage("can't create range from real number to complex");
                                            }
                                    }
                                    break;
                                }

                            case 2:
                                {
                                    ExObject e = vm.GetArgument(2);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                double end = e.GetFloat();
                                                if (end > start)
                                                {
                                                    int count = (int)(end - start);

                                                    for (int i = 0; i < count; i++)
                                                    {
                                                        l.Add(new(start + i));
                                                    }
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                return vm.AddToErrorMessage("can't create range from real number to complex");
                                            }
                                    }
                                    break;
                                }

                            case 1:
                                {
                                    for (int i = 0; i < start; i++)
                                    {
                                        l.Add(new(i));
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex start = s.GetComplex();
                        switch (nargs)
                        {
                            case 3:
                                {
                                    ExObject e = vm.GetArgument(2);
                                    ExObject d = vm.GetArgument(3);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                long count = e.GetInt();
                                                switch (d.Type)
                                                {
                                                    case ExObjType.INTEGER:
                                                        {
                                                            double step = d.GetInt();
                                                            for (int i = 0; i < count; i++)
                                                            {
                                                                l.Add(new(start + (i * step)));
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.FLOAT:
                                                        {
                                                            double step = d.GetFloat();
                                                            for (int i = 0; i < count; i++)
                                                            {
                                                                l.Add(new(start + (i * step)));
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.COMPLEX:
                                                        {
                                                            Complex step = d.GetComplex();
                                                            for (int i = 0; i < count; i++)
                                                            {
                                                                l.Add(new(start + (i * step)));
                                                            }
                                                            break;
                                                        }
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                return vm.AddToErrorMessage("expected integer as 2nd argument for complex number range(start, count, step)");
                                            }
                                    }
                                    break;
                                }

                            case 2:
                                {
                                    ExObject e = vm.GetArgument(2);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                long count = e.GetInt();
                                                for (int i = 0; i < count; i++)
                                                {
                                                    l.Add(new(start + (i * start)));
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                return vm.AddToErrorMessage("expected integer as 2nd argument for complex number range(start, count)");
                                            }
                                    }
                                    break;
                                }

                            case 1:
                                {
                                    for (int i = 1; i < start.Real; i++)
                                    {
                                        l.Add(new(i * start));
                                    }
                                    break;
                                }
                        }
                        break;
                    }
            }

            return vm.CleanReturn(nargs + 2, l);
        }

        [ExNativeFuncBase("matrix", ExBaseType.ARRAY, "Initialize a matrix with 'rows'x'cols' dimensions.")]
        [ExNativeParamBase(1, "rows", "i", "Row dimension")]
        [ExNativeParamBase(2, "cols", "i", "Column dimension", 1)]
        [ExNativeParamBase(3, "filler", ".", "Filler object", def: null)]
        public static ExFunctionStatus StdMatrix(ExVM vm, int nargs)
        {
            ExList l = new();
            ExObject s = vm.GetArgument(1);

            switch (nargs)
            {
                case 2:
                case 3:
                    {
                        int m = (int)s.GetInt();
                        if (m < 0)
                        {
                            m = 0;
                        }

                        int n = (int)vm.GetArgument(2).GetInt();
                        if (n < 0)
                        {
                            n = 0;
                        }

                        ExObject filler = nargs == 3 ? vm.GetArgument(3) : new();
                        l.ValueCustom.l_List = new(m);

                        switch (filler.Type)
                        {
                            case ExObjType.CLOSURE:
                                {
                                    if (filler.GetClosure().Function.nParams != 3
                                        && (filler.GetClosure().Function.nParams - filler.GetClosure().Function.nDefaultParameters) > 3)
                                    {
                                        return vm.AddToErrorMessage("given function must allow 2-argument calls");
                                    }

                                    bool bm = vm.IsMainCall;
                                    vm.IsMainCall = false;
                                    for (int i = 0; i < m; i++)
                                    {
                                        List<ExObject> lis = new(n);
                                        for (int j = 0; j < n; j++)
                                        {
                                            ExObject res = new();
                                            ExApi.PushRootTable(vm);
                                            vm.Push(i);
                                            vm.Push(j);
                                            if (!vm.Call(ref filler, 3, vm.StackTop - 3, ref res, true))
                                            {
                                                vm.IsMainCall = bm;
                                                return ExFunctionStatus.ERROR;
                                            }
                                            vm.Pop(3);

                                            lis.Add(new(res));
                                        }
                                        l.GetList().Add(new ExObject(lis));
                                    }

                                    vm.IsMainCall = bm;
                                    break;
                                }
                            case ExObjType.NATIVECLOSURE:
                                {
                                    int nparamscheck = filler.GetNClosure().nParameterChecks;
                                    if (((nparamscheck > 0) && (nparamscheck != 3)) ||
                                        ((nparamscheck < 0) && ((-nparamscheck) > 3)))
                                    {
                                        if (nparamscheck < 0)
                                        {
                                            vm.AddToErrorMessage("'" + filler.GetNClosure().Name.GetString() + "' takes minimum " + (-nparamscheck - 1) + " arguments");
                                            return ExFunctionStatus.ERROR;
                                        }
                                        vm.AddToErrorMessage("'" + filler.GetNClosure().Name.GetString() + "' takes exactly " + (nparamscheck - 1) + " arguments");
                                        return ExFunctionStatus.ERROR;
                                    }

                                    bool bm = vm.IsMainCall;
                                    vm.IsMainCall = false;
                                    for (int i = 0; i < m; i++)
                                    {
                                        List<ExObject> lis = new(n);
                                        for (int j = 0; j < n; j++)
                                        {
                                            ExObject res = new();
                                            vm.Push(i);
                                            vm.Push(j);
                                            if (!vm.Call(ref filler, 2, vm.StackTop - 2, ref res, true))
                                            {
                                                return ExFunctionStatus.ERROR;
                                            }
                                            vm.Pop(2);

                                            lis.Add(new(res));
                                        }
                                        l.GetList().Add(new ExObject(lis));
                                    }

                                    vm.IsMainCall = bm;
                                    break;
                                }
                            default:
                                {
                                    for (int i = 0; i < m; i++)
                                    {
                                        List<ExObject> lis = null;
                                        ExUtils.InitList(ref lis, n, filler);
                                        l.GetList().Add(new ExObject(lis));
                                    }
                                    break;
                                }
                        }
                        break;
                    }
            }
            return vm.CleanReturn(nargs + 2, l);
        }

        [ExNativeFuncBase(ExMat.ReloadLibFunc, ExBaseType.BOOL, "Reload a standard library specific function")]
        [ExNativeParamBase(1, "library", "s", "Library to reload: base, io, math, string, net, system, statistics")]
        [ExNativeParamBase(2, "function_name", "s", "A specific function from the library to reload alone")]
        public static ExFunctionStatus StdReloadlib(ExVM vm, int nargs)
        {
            string lname = vm.GetArgument(1).GetString();

            if (GetStdTypeFromString(vm, lname, out Type type) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }

            string fname = vm.GetArgument(2).GetString();
            if (fname != ExMat.ReloadFunc)
            {
                ExApi.PushRootTable(vm);
                if (!ExApi.ReloadNativeFunction(vm, type, fname, true))
                {
                    return vm.AddToErrorMessage("unknown '{0}' function '{1}'", lname, fname);
                }
            }

            return vm.CleanReturn(nargs + 2, true);
        }

        [ExNativeFuncBase(ExMat.ReloadFunc, ExBaseType.BOOL, "Reload a native function from any standard library")]
        [ExNativeParamBase(1, "func", "s", "Function name to search for in standard libraries")]
        public static ExFunctionStatus StdReloadlibfunc(ExVM vm, int nargs)
        {
            string fname = vm.GetArgument(1).GetString();

            List<Type> libs = ExApi.GetStandardLibraryTypes();
            bool found = false;

            foreach (Type lib in libs)
            {
                ExApi.PushRootTable(vm);
                if (ExApi.ReloadNativeFunction(vm, lib, fname, true))
                {
                    found = true;
                    break;
                }
            }

            return found
                    ? vm.CleanReturn(nargs + 2, true)
                    : vm.AddToErrorMessage($"couldn't find a native function named '{fname}'");
        }

        [ExNativeFuncBase("exit", "Exits the virtual machine at the next possible oppurtunity")]
        [ExNativeParamBase(1, "func", "r", "Condition to be met. Single parameter function, gets passed list elements to it", 0)]
        public static ExFunctionStatus StdExit(ExVM vm, int nargs)
        {
            vm.CleanReturn(nargs + 2, vm.GetArgument(1).GetInt());
            return ExFunctionStatus.EXIT;
        }

        [ExNativeFuncBase("is_interactive", ExBaseType.BOOL, "Check if current script is running inside an interactive console")]
        public static ExFunctionStatus StdInteractive(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.IsInteractive);
        }

        [ExNativeFuncBase("collect_garbage", "Force run the garbage collector")]
        public static ExFunctionStatus StdGCCollect(ExVM vm, int nargs)
        {
            vm.Pop(2);
            ExApi.CollectGarbage();
            return ExFunctionStatus.VOID;
        }

        #endregion

        #region UTILITY
        private static bool PushArgsForCluster(ExVM vm, ExObject cls, List<ExObject> args, ref int nargs, bool parsing = true)
        {
            int req = cls.GetClosure().GetAttribute(ExMat.nParams);
            if (req > 1)
            {
                if (req != args.Count)
                {
                    vm.AddToErrorMessage("expected " + req + " arguments for the cluster");
                    return false;
                }
                else
                {
                    nargs = args.Count + 1;
                    vm.PushParse(args);
                }
            }
            else
            {
                if (parsing)
                {
                    nargs = args.Count;
                    vm.Push(args);
                }
                else
                {
                    if (args.Count != 1)
                    {
                        throw new ExException(vm, "args count were not 1");
                    }
                    vm.Push(args[0]);
                }
            }
            return true;
        }
        public static ExFunctionStatus HandleCharArrayToString(ExVM vm, int nargs, ExObject obj)
        {
            if (obj.Type == ExObjType.ARRAY)
            {
                StringBuilder str = new(obj.GetList().Count);

                return !ExApi.ConvertIntegerStringArrayToString(obj.GetList(), str)
                    ? vm.AddToErrorMessage("failed to create string, list must contain all positive integers within 'char' range or strings")
                    : vm.CleanReturn(nargs + 2, str.ToString());
            }
            else if (obj.Type == ExObjType.INTEGER)
            {
                long val = obj.GetInt();

                return val is < char.MinValue or > char.MaxValue
                    ? vm.AddToErrorMessage("integer out of range for char conversion")
                    : vm.CleanReturn(nargs + 2, ((char)val).ToString(CultureInfo.CurrentCulture));
            }
            else
            {
                return vm.AddToErrorMessage("expected INTEGER or ARRAY when 2nd parameter of 'string' is true");
            }
        }
        public static ExFunctionStatus HandleBitConversion(int bits, ExVM vm, int nargs, long b, bool reverse)
        {
            if (bits == 32 && (b > int.MaxValue || b < int.MinValue))
            {
                return vm.AddToErrorMessage("64bit value out of range for 32bit use");
            }

            List<ExObject> l = new(bits);

            foreach (int bit in ExApi.GetBits(b, bits))
            {
                l.Add(new(bit));
            }

            if (reverse)
            {
                l.Reverse();
            }

            return vm.CleanReturn(nargs + 2, l);
        }
        public static ExFunctionStatus Handle32BitConversion(ExVM vm, int nargs, long b, bool reverse)
        {
            return HandleBitConversion(32, vm, nargs, b, reverse);
        }
        public static ExFunctionStatus Handle64BitConversion(ExVM vm, int nargs, long b, bool reverse)
        {
            return HandleBitConversion(64, vm, nargs, b, reverse);
        }

        /// <summary>
        /// Get std library <see cref="Type"/> from it's name.
        /// </summary>
        /// <param name="vm">Virtual machine to report errors to</param>
        /// <param name="lib">Library name to search for. Doesn't allow <see cref="ExMat.StandardBaseLibraryName"/>.</param>
        /// <param name="type">Output type, if nothing matched, gets <see langword="null"/></param>
        /// <returns><see cref="ExFunctionStatus.SUCCESS"/> on success, otherwise <see cref="ExFunctionStatus.ERROR"/> with error messages written to <paramref name="vm"/></returns>
        private static ExFunctionStatus GetStdTypeFromString(ExVM vm, string lib, out Type type) // TO-DO maybe move this to API ?
        {
            type = null;
            List<Type> libs = ExApi.GetStandardLibraryTypes();

            List<string> libnames = new(libs.Select(t => ExApi.GetLibraryNameFromStdClass(t)));

            int idx = libnames.IndexOf(lib);
            if (idx == -1 && (idx = libnames.IndexOf(lib.ToLower(CultureInfo.CurrentCulture))) == -1)
            {
                return vm.AddToErrorMessage("Unknown library: '{0}'", lib);
            }
            else
            {
                type = libs[idx];
                return ExFunctionStatus.SUCCESS;
            }
        }
        #endregion

        /// MAIN

        public static Dictionary<string, ExObject> Constants => new()
        {
            { "_versionnumber_", new(ExMat.VersionNumber) },
            { "_version_", new(ExMat.Version) },
            {
                "_config_",
#if DEBUG
                new("DEBUG")
#else
                new("RELEASE")
#endif
            },
            {
                "COLORS",
                new(new Dictionary<string, ExObject>()
                {
                    { "RED", new("red") },
                    { "DARKRED", new("darkred") },
                    { "GREEN", new("green") },
                    { "DARKGREEN", new("darkgreen") },
                    { "BLUE", new("blue") },
                    { "DARKBLUE", new("darkblue") },
                    { "YELLOW", new("yellow") },
                    { "DARKYELLOW", new("darkyellow") },
                    { "MAGENTA", new("magenta") },
                    { "DARKMAGENTA", new("darkmagenta") },
                    { "CYAN", new("cyan") },
                    { "DARKCYAN", new("darkcyan") }
                })
            },
        };

        public static ExMat.StdLibRegistery Registery => (ExVM vm) =>
        {
            ExApi.PushConstsTable(vm);
            ExApi.CreateConstantInt(vm, "_stacksize_", vm.StackSize);
            vm.Pop();
            return true;
        };
    }
}
