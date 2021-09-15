using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using ExMat.Closure;
using ExMat.Compiler;
using ExMat.Exceptions;
using ExMat.Interfaces;
using ExMat.Objects;
using ExMat.States;
using ExMat.Utils;
using ExMat.VM;
using Microsoft.CodeAnalysis.CSharp;

namespace ExMat.API
{
    /// <summary>
    /// A middle-ground class for <see cref="ExVM"/> and standard library registeration related methods
    /// </summary>
    public static class ExApi
    {
        /// <summary>
        /// Check wheter given method has the same signature as <see cref="ExMat.StdLibFunction"/> delegate
        /// </summary>
        /// <param name="m">Method to check</param>
        /// <returns><see langword="true"/> if <paramref name="m"/> has the same signature as <see cref="ExMat.StdLibFunction"/>, otherwise <see langword="false"/></returns>
        public static bool IsValidStdLibFunction(MethodInfo m)
        {
            return ExMat.StdLibFunctionRegex.IsMatch(m.ToString());
        }

        /// <summary>
        /// Find all methods with <see cref="ExNativeFuncBase"/> attribute an not the <see cref="ExNativeFuncDelegate"/> attribute, in the given type of standard library
        /// </summary>
        /// <param name="type">Standard library type</param>
        /// <returns>List of native non-delegate functions found</returns>
        public static List<ExNativeFunc> GetNonDelegateNativeFunctions(Type type)
        {
            return new(type
                        .GetMethods()
                        .Where(m => Attribute.IsDefined(m, typeof(ExNativeFuncBase))
                                    && !Attribute.IsDefined(m, typeof(ExNativeFuncDelegate))
                                    && IsValidStdLibFunction(m))
                        .Select(n => new ExNativeFunc((ExMat.StdLibFunction)Delegate.CreateDelegate(typeof(ExMat.StdLibFunction), n))));
        }

        /// <summary>
        /// Find all methods with <see cref="ExNativeFuncDelegate"/> attribute defined in the current assembly
        /// </summary>
        /// <returns>List of native delegate functions found in the assembly</returns>
        public static List<ExNativeFunc> FindDelegateNativeFunctions()
        {
            List<ExNativeFunc> funcs = new();
            foreach (Type lib in GetStandardLibraryTypes(GetAllAssemblies()))
            {
                funcs.AddRange(GetDelegateNativeFunctions(lib));
            }
            return funcs;
        }

        /// <summary>
        /// Find all methods with <see cref="ExNativeFuncDelegate"/> attribute in the given type of standard library
        /// </summary>
        /// <param name="type">Standard library type</param>
        /// <returns>List of native delegate functions found</returns>
        public static List<ExNativeFunc> GetDelegateNativeFunctions(Type type)
        {
            List<ExNativeFunc> funcs = new();
            foreach (MethodInfo m in type
                                    .GetMethods()
                                    .Where(m => Attribute.IsDefined(m, typeof(ExNativeFuncDelegate))
                                                && IsValidStdLibFunction(m)))
            {
                ExNativeFuncDelegate[] basedelegs = (ExNativeFuncDelegate[])m.GetCustomAttributes(typeof(ExNativeFuncDelegate), false);
                foreach (ExNativeFuncDelegate deleg in basedelegs)
                {
                    funcs.Add(new ExNativeFunc((ExMat.StdLibFunction)Delegate.CreateDelegate(typeof(ExMat.StdLibFunction), m), deleg.BaseTypeMask));
                }
            }

            return funcs;
        }

        /// <summary>
        /// Get the base type of given object type
        /// </summary>
        /// <param name="type">Object type</param>
        /// <returns>Raw base type of given object type, stripped from flags</returns>
        public static ExBaseType GetBaseType(ExObjType type)
        {
            int raw = (int)type;
            foreach (ExObjFlag flg in Enum.GetValues(typeof(ExObjFlag)))
            {
                raw &= ~(int)flg;
            }
            return (ExBaseType)raw;
        }

        /// <summary>
        /// Create a cryptographically safe random string using characters from [a-zA-Z0-9]
        /// </summary>
        /// <param name="length">Length of the string</param>
        /// <returns>A random string of given <paramref name="length"/></returns>
        public static string RandomString(int length)
        {
            using RandomNumberGenerator randomGenerator = RandomNumberGenerator.Create();
            byte[] data = new byte[length];
            randomGenerator.GetBytes(data);

            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            StringBuilder str = new(length);
            for (int i = 0; i < length; i++)
            {
                str.Append(chars[data[i] % 62]);
            }
            return str.ToString();
        }

        public static List<ExObject> ListObjFromStringArray(IEnumerable<string> arr)
        {
            List<ExObject> lis = new();
            foreach (string s in arr)
            {
                lis.Add(new(s));
            }
            return lis;
        }

        public static List<ExObject> ListObjFromStringArray(string[] arr)
        {
            List<ExObject> lis = new(arr.Length);
            foreach (string s in arr)
            {
                lis.Add(new(s));
            }
            return lis;
        }

        /// <summary>
        /// Get an object from stack and assert a certain <paramref name="type"/>
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="idx">Object index: <see cref="ExVM.StackBase"/><c> + <paramref name="idx"/> - 1</c></param>
        /// <param name="type">Expected object type</param>
        /// <param name="res">Reference to a <see cref="ExObjType.NULL"/> object to store the object from stack</param>
        /// <returns><see langword="true"/> if object was passed to <paramref name="res"/> 
        /// <para><see langword="false"/> if object type didn't match <paramref name="type"/></para></returns>
        public static bool GetSafeObject(ExVM vm, int idx, ExObjType type, ref ExObject res)
        {
            res.Assign(GetFromStack(vm, idx));
            if (res.Type != type)
            {
                vm.AddToErrorMessage("wrong argument type, expected " + type.ToString() + " got " + res.Type.ToString());
                return false;
            }
            return true;
        }

        /// <summary>
        /// Attempt to convert an argument passed to a native function to string and return it as given string
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="idx">Argument index</param>
        /// <param name="maxdepth">Stringification maximum depth</param>
        /// <param name="output">Output string</param>
        /// <returns><see langword="true"/> if stringfied successfully and assigned to <paramref name="output"/>, otherwise <see langword="false"/></returns>
        public static bool ConvertAndGetString(ExVM vm, int idx, int maxdepth, out string output)
        {
            output = string.Empty;
            return ToString(vm, idx + 1, maxdepth) && GetString(vm, -1, ref output);
        }

        private static Thread Beeper;

        public static bool CanBeep()
        {
            return Beeper == null || !Beeper.IsAlive;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public static bool BeepAsync(int freq, int dur)
        {
            if (!CanBeep())
            {
                return false;
            }
            Beeper = new(() =>
            {
                Console.Beep(freq, dur);
                Thread.Sleep((int)(dur * 0.9));
            });

            Beeper.Start();
            return true;
        }

        public static bool BeepAsync()
        {
            Beeper = new(() =>
            {
                Console.Beep();
            });

            Beeper.Start();
            return true;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public static bool Beep(int freq, int dur)
        {
            Console.Beep(freq, dur);
            return true;
        }

        public static bool Beep()
        {
            Console.Beep();
            return true;
        }

        /// <summary>
        /// Stringify an object in stack
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="idx">Object index to stringify: <see cref="ExVM.StackBase"/><c> + <paramref name="idx"/> - 1</c></param>
        /// <param name="maxdepth">Printing depth. 
        /// <para><c>1</c> means <c>Array[i]</c> elements getting printed</para> <code>[1,"abc",[4,4]] -> [ 1, "abc", ARRAY(2)]</code>
        /// <c>2</c> for <c>Array[i][j]</c> getting printed <code>[1,"abc",[4,4]] -> [ 1, "abc", [4, 4]]</code></param>
        /// <param name="pop">Amount of objects to pop from stack, used for returning values in interactive console</param>
        /// <param name="beauty">Wheter to indent values in arrays and dictionaries, it may look too spaced out for larger objects</param>
        /// <returns><see langword="true"/> if object was successfully stringified
        /// <para><see langword="false"/> if there was an error stringifying the object</para></returns>
        public static bool ToString(ExVM vm, int idx, int maxdepth = 1, int pop = 0, bool beauty = false)
        {
            ExObject o = GetFromStack(vm, idx);
            ExObject res = new(string.Empty);
            if (!vm.ToString(o, ref res, maxdepth, beauty: beauty))
            {
                return false;
            }
            vm.CleanReturn(pop, res);
            return true;
        }

        public static bool ToFloat(ExVM vm, ExObject obj, ref ExObject res)
        {
            switch (obj.Type)
            {
                case ExObjType.COMPLEX:
                    {
                        if (obj.GetComplex().Imaginary != 0.0)
                        {
                            vm.AddToErrorMessage("can't parse non-zero imaginary part complex number as float");
                            return false;
                        }
                        res = new(obj.GetComplex().Real);
                        break;
                    }
                case ExObjType.INTEGER:
                    {
                        res = new((double)obj.GetInt());
                        break;
                    }
                case ExObjType.FLOAT:
                    {
                        res = new(obj.GetFloat());
                        break;
                    }
                case ExObjType.STRING:
                    {
                        if (ParseStringToFloat(obj.GetString(), ref res))
                        {
                            return true;
                        }
                        else
                        {
                            vm.AddToErrorMessage("failed to parse string as double");
                            return false;
                        }
                    }
                case ExObjType.BOOL:
                    {
                        res = new(obj.GetBool() ? 1.0 : 0.0);
                        break;
                    }
                default:
                    {
                        vm.AddToErrorMessage("failed to parse " + obj.Type.ToString() + " as double");
                        return false;
                    }
            }
            return true;
        }


        public static bool ToInteger(ExVM vm, ExObject obj, ref ExObject res)
        {
            switch (obj.Type)
            {
                case ExObjType.COMPLEX:
                    {
                        if (obj.GetComplex().Imaginary != 0.0)
                        {
                            vm.AddToErrorMessage("can't parse non-zero imaginary part complex number as integer");
                            return false;
                        }
                        res = new((long)obj.GetComplex().Real);
                        break;
                    }
                case ExObjType.INTEGER:
                    {
                        res = new(obj.GetInt());
                        break;
                    }
                case ExObjType.FLOAT:
                    {
                        res = new((long)obj.GetFloat());
                        break;
                    }
                case ExObjType.STRING:
                    {
                        if (ParseStringToInteger(obj.GetString(), ref res))
                        {
                            return true;
                        }
                        else
                        {
                            vm.AddToErrorMessage("failed to parse string as integer");
                            return false;
                        }
                    }
                case ExObjType.BOOL:
                    {
                        res = new(obj.GetBool() ? 1 : 0);
                        break;
                    }
                default:
                    {
                        vm.AddToErrorMessage("failed to parse " + obj.Type.ToString() + " as integer");
                        return false;
                    }
            }
            return true;
        }

        /// <summary>
        /// Parse an object as 64bit floating point value
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="idx">Object index to stringify: <see cref="ExVM.StackBase"/><c> + <paramref name="idx"/> - 1</c></param>
        /// <param name="pop">Amount of objects to pop from stack, used for returning values in interactive console</param>
        /// <returns><see langword="true"/> if object was successfully parsed as float
        /// <para><see langword="false"/> if there was an error parsing the object</para></returns>
        public static bool ToFloatFromStack(ExVM vm, int idx, int pop = 0)
        {
            ExObject o = GetFromStack(vm, idx);
            ExObject res = null;
            if (!ToFloat(vm, o, ref res))
            {
                return false;
            }
            vm.CleanReturn(pop, res);
            return true;
        }

        /// <summary>
        /// Parse an object as 64bit integer
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="idx">Object index to stringify: <see cref="ExVM.StackBase"/><c> + <paramref name="idx"/> - 1</c></param>
        /// <param name="pop">Amount of objects to pop from stack, used for returning values in interactive console</param>
        /// <returns><see langword="true"/> if object was successfully parsed as integer
        /// <para><see langword="false"/> if there was an error parsing the object</para></returns>
        public static bool ToIntegerFromStack(ExVM vm, int idx, int pop = 0)
        {
            ExObject o = GetFromStack(vm, idx);
            ExObject res = null;
            if (!ToInteger(vm, o, ref res))
            {
                return false;
            }
            vm.CleanReturn(pop, res);
            return true;
        }

        /// <summary>
        /// Returns string format of a float
        /// </summary>
        /// <param name="a">Value to get the string of</param>
        /// <returns>A formatted string</returns>
        public static string GetFloatString(ExObject obj)
        {
            double r = obj.GetFloat();
            return r % 1 == 0.0
                ? r < 1e+14 ? obj.GetFloat().ToString(CultureInfo.CurrentCulture) : obj.GetFloat().ToString("E14", CultureInfo.CurrentCulture)
                : r >= 1e-14
                    ? obj.GetFloat().ToString("0.00000000000000", CultureInfo.CurrentCulture)
                    : r < 1e+14 ? obj.GetFloat().ToString(CultureInfo.CurrentCulture) : obj.GetFloat().ToString("E14", CultureInfo.CurrentCulture);
        }

        public static bool CheckEqualFloat(double x, double y)
        {
            return double.IsNaN(x) ? double.IsNaN(y) : x == y;
        }

        public static bool CheckEqualArray(List<ExObject> x, List<ExObject> y)
        {
            if (x.Count != y.Count)
            {
                return false;
            }

            bool res = true;
            for (int i = 0; i < x.Count; i++)
            {
                ExObject r = x[i];
                if (!res)
                {
                    return false;
                }
                if (!CheckEqual(r, y[i], ref res))
                {
                    return false;
                }
            }

            return true;
        }

        public static void CheckEqualForSameType(ExObject x, ExObject y, ref bool res)
        {
            switch (x.Type)
            {
                case ExObjType.INTEGER:
                    res = x.GetInt() == y.GetInt();
                    break;
                case ExObjType.FLOAT:
                    res = CheckEqualFloat(x.GetFloat(), y.GetFloat());
                    break;
                case ExObjType.COMPLEX:
                    res = x.GetComplex() == y.GetComplex();
                    break;
                case ExObjType.BOOL:
                    res = x.GetBool() == y.GetBool();
                    break;
                case ExObjType.STRING:
                    res = x.GetString() == y.GetString();
                    break;
                case ExObjType.NATIVECLOSURE: // TO-DO Need better checks
                    res = x.GetNClosure().GetHashCode() == y.GetNClosure().GetHashCode();
                    break;
                case ExObjType.CLOSURE:
                    res = x.GetClosure().GetHashCode() == y.GetClosure().GetHashCode();
                    break;
                case ExObjType.ARRAY:
                    res = CheckEqualArray(x.GetList(), y.GetList());
                    break;
                case ExObjType.DICT:
                    res = x.GetDict().GetHashCode() == y.GetDict().GetHashCode();
                    break;
                case ExObjType.SPACE:
                    res = string.Equals(x.GetSpace().GetSpaceString(), y.GetSpace().GetSpaceString(), StringComparison.Ordinal);
                    break;
                case ExObjType.CLASS:
                    res = x.GetClass().Hash == y.GetClass().Hash;
                    break;
                case ExObjType.INSTANCE:
                    res = x.GetInstance().Class.Hash == y.GetInstance().Class.Hash && x.GetInstance().Hash == y.GetInstance().Hash;
                    break;
                case ExObjType.WEAKREF:
                    CheckEqual(x.GetWeakRef().ReferencedObject, y.GetWeakRef().ReferencedObject, ref res);
                    break;
                case ExObjType.NULL:
                case ExObjType.DEFAULT:
                    res = true;
                    break;
                default:
                    res = false;
                    break;
            }
        }

        public static void CheckEqualForDifferingTypes(ExObject x, ExObject y, ref bool res)
        {
            bool bx = ExTypeCheck.IsNumeric(x);
            bool by = ExTypeCheck.IsNumeric(y);
            res = by && x.Type == ExObjType.COMPLEX
                ? x.GetComplex() == y.GetFloat()
                : bx && y.Type == ExObjType.COMPLEX
                    ? x.GetFloat() == y.GetComplex()
                    : bx && by && CheckEqualFloat(x.GetFloat(), y.GetFloat());
        }

        /// <summary>
        /// Checks equality of given two objects, store result in given argument <paramref name="res"/>
        /// </summary>
        /// <param name="x">First object</param>
        /// <param name="y">Second object</param>
        /// <param name="res">Result of equality check</param>
        /// <returns>Always returns <see langword="true"/>, stores result in <paramref name="res"/></returns>
        public static bool CheckEqual(ExObject x, ExObject y, ref bool res)
        {
            if (x.Type == y.Type)
            {
                CheckEqualForSameType(x, y, ref res);
            }
            else
            {
                CheckEqualForDifferingTypes(x, y, ref res);
            }
            return true;
        }

        /// <summary>
        /// Checks equality of given two objects, returns the result
        /// </summary>
        /// <param name="x">First object</param>
        /// <param name="y">Second object</param>
        /// <returns><see langword="true"/> if <paramref name="x"/> and <paramref name="y"/> hold the same value, otherwise <see langword="false"/></returns>
        public static bool CheckEqualReturnRes(ExObject x, ExObject y)
        {
            bool res = false;
            if (x.Type == y.Type)
            {
                CheckEqualForSameType(x, y, ref res);
            }
            else
            {
                CheckEqualForDifferingTypes(x, y, ref res);
            }
            return res;
        }

        /// <summary>
        /// Checks if given object exists in given list
        /// </summary>
        /// <param name="lis">List to iterate over</param>
        /// <param name="key">Object to match</param>
        /// <returns><see langword="true"/> if <paramref name="key"/> has an equal object in <paramref name="lis"/>, otherwise <see langword="false"/></returns>
        public static bool FindInArray(List<ExObject> lis, ExObject key)
        {
            bool found = false;
            foreach (ExObject o in lis)
            {
                CheckEqual(o, key, ref found);
                if (found)
                {
                    return true;
                }
            }
            return false;
        }

        public static ConsoleColor GetColorFromName(string name, ConsoleColor def = ConsoleColor.Black)
        {
            switch (name.Trim().ToLower(CultureInfo.CurrentCulture))
            {
                case "black":
                    {
                        return ConsoleColor.Black;
                    }
                case "white":
                    {
                        return ConsoleColor.White;
                    }
                case "darkred":
                    {
                        return ConsoleColor.DarkRed;
                    }
                case "red":
                    {
                        return ConsoleColor.Red;
                    }
                case "darkgreen":
                    {
                        return ConsoleColor.DarkGreen;
                    }
                case "green":
                    {
                        return ConsoleColor.Green;
                    }
                case "darkyellow":
                    {
                        return ConsoleColor.DarkYellow;
                    }
                case "yellow":
                    {
                        return ConsoleColor.Yellow;
                    }
                case "darkgrey":
                case "darkgray":
                    {
                        return ConsoleColor.DarkGray;
                    }
                case "grey":
                case "gray":
                    {
                        return ConsoleColor.Gray;
                    }
                case "darkblue":
                    {
                        return ConsoleColor.DarkBlue;
                    }
                case "blue":
                    {
                        return ConsoleColor.Blue;
                    }
                case "darkcyan":
                    {
                        return ConsoleColor.DarkCyan;
                    }
                case "cyan":
                    {
                        return ConsoleColor.Cyan;
                    }
                case "darkmagenta":
                    {
                        return ConsoleColor.DarkMagenta;
                    }
                case "magenta":
                    {
                        return ConsoleColor.Magenta;
                    }
                default:
                    {
                        return def;
                    }
            }
        }

        public static Encoding DecideEncodingFromString(string enc)
        {
            Encoding e;
            if (string.IsNullOrEmpty(enc))
            {
                e = Encoding.Default;
            }
            else
            {
                switch (enc.ToLower(CultureInfo.CurrentCulture))
                {
                    case "utf-8":
                    case "utf8":
                        {
                            e = Encoding.UTF8;
                            break;
                        }
                    case "utf32":
                    case "utf-32":
                        {
                            e = Encoding.UTF32;
                            break;
                        }
                    case "latin":
                    case "latin1":
                        {
                            e = Encoding.Latin1;
                            break;
                        }
                    case "be-unicode":
                        {
                            e = Encoding.BigEndianUnicode;
                            break;
                        }
                    case "unicode":
                        {
                            e = Encoding.Unicode;
                            break;
                        }
                    case "ascii":
                        {
                            e = Encoding.ASCII;
                            break;
                        }
                    default:
                        {
                            e = Encoding.Default;
                            break;
                        }
                }
            }

            return e;
        }

        /// <summary>
        /// Get bits representation of an integer
        /// </summary>
        /// <param name="i">Integer to use</param>
        /// <param name="bits">Bits to pad to left</param>
        /// <returns>An array of zero and ones</returns>
        public static int[] GetBits(long i, int bits)
        {
            if (bits == 32)
            {
                string s = Convert.ToString((int)i, 2);
                return s.PadLeft(bits, '0').Select(c => int.Parse(c.ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture)).ToArray();
            }
            return Convert.ToString(i, 2).PadLeft(bits, '0').Select(c => int.Parse(c.ToString(CultureInfo.CurrentCulture), CultureInfo.CurrentCulture)).ToArray();
        }

        /// <summary>
        /// Get <paramref name="n"/> objects in an array from stack
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="n">Object index to stringify: <see cref="ExVM.StackBase"/><c> + <paramref name="start"/> + {0 to <paramref name="n"/>} - 1</c></param>
        /// <param name="start">Starting index above <see cref="ExVM.StackBase"/> base index<para>
        /// Most of the time first 2 above base are references for <see langword="this"/> and roottable</para></param>
        /// <returns>Array of objects in desired stack locations</returns>
        public static ExObject[] GetNObjects(ExVM vm, int n, int start = 2)
        {
            ExObject[] arr = new ExObject[n];
            int i = 0;
            while (i < n)
            {
                arr[i] = GetFromStack(vm, i + start);
                i++;
            }
            return arr;
        }

        /// <summary>
        /// Get the string value stored in a <see cref="ExObjType.STRING"/> object to a referenced string <paramref name="s"/>
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="idx">Object index to stringify: <see cref="ExVM.StackBase"/><c> + <paramref name="idx"/> - 1</c></param>
        /// <param name="s">String object to store the result in</param>
        /// <returns><see langword="true"/> if object was a <see cref="ExObjType.STRING"/> object
        /// <para><see langword="false"/> if there was not a <see cref="ExObjType.STRING"/> object</para></returns>
        public static bool GetString(ExVM vm, int idx, ref string s)
        {
            ExObject o = new();
            if (!GetSafeObject(vm, idx, ExObjType.STRING, ref o))
            {
                return false;
            }
            s = o.GetString();
            return true;
        }

        /// <summary>
        /// Find a native function by name in a list of functions
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="fs">Function list</param>
        /// <param name="name">Name to search for</param>
        /// <returns>A <see cref="ExNativeFunc"/> if <paramref name="name"/> was found in <paramref name="fs"/> list
        /// <para><see langword="null"/> if there was no functions named <paramref name="name"/> in <paramref name="fs"/></para></returns>
        public static ExNativeFunc FindNativeFunction(ExVM vm, List<ExNativeFunc> fs, string name)
        {
            foreach (ExNativeFunc f in fs)
            {
                if (f.Name == name && f.Name != string.Empty)
                {
                    return f;
                }
            }
            return null;
        }

        /// <summary>
        /// Reload a native function in roottable
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="fs">List of native functions</param>
        /// <param name="name">Name of the function to reload</param>
        /// <param name="libtype">Library the <paramref name="name"/> function is based on</param>
        /// <param name="pop">Wheter to pop the root from stack after searching</param>
        /// <returns><see langword="true"/> if <paramref name="name"/> was found in <paramref name="fs"/> and reloaded
        /// <para><see langword="false"/> if there was no function named <paramref name="name"/> in <paramref name="fs"/></para></returns>
        public static bool ReloadNativeFunction(ExVM vm, Type lib, string name, bool pop = false)
        {
            List<ExNativeFunc> fs = GetNonDelegateNativeFunctions(lib);

            ExStdLibType libtype = GetLibraryTypeFromStdClass(lib);

            ExNativeFunc r;
            if ((r = FindNativeFunction(vm, fs, name)) != null)
            {
                RegisterNativeFunction(vm, r, libtype);
                if (pop)
                {
                    vm.Pop();
                }
                return true;
            }
            if (pop)
            {
                vm.Pop();
            }
            return false;
        }

        /// <summary>
        /// Register a native function to a virtual machines roottable
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="func">Native function</param>
        /// <param name="libtype">Library the <paramref name="func"/> is based on</param>
        public static void RegisterNativeFunction(ExVM vm, ExNativeFunc func, ExStdLibType libtype)
        {
            func.Base = libtype;

            PushString(vm, func.Name, -1);              // Fonksiyon ismi
            CreateClosure(vm, func.Function);           // Fonksiyonun temeli
            SetNativeClosureName(vm, -1, func.Name);    // İsmi fonksiyon temeline ekle
            SetParamCheck(vm, func.NumberOfParameters, func.ParameterMask);   // Parametre kontrolü
            SetDefaultValues(vm, func.Parameters);   // Varsayılan parametre değerleri
            SetDocumentation(vm, func);
            CreateNewSlot(vm, -3);                      // Tabloya kaydet
        }

        /// <summary>
        /// Invoke <see cref="IExStdLib.Register(ExVM)"/> of given type on given virtual machine
        /// </summary>
        /// <param name="vm">Virtual machine to invoke the method with</param>
        /// <param name="type">Standard library type</param>
        /// <returns><see cref="ExFunctionStatus.SUCCESS"/> on success, <see cref="ExFunctionStatus.ERROR"/> otherwise</returns>
        public static ExFunctionStatus InvokeRegisterOnStdLib(ExVM vm, Type type)
        {
            if (!Attribute.IsDefined(type, typeof(ExStdLibRegister)))
            {
                return vm.AddToErrorMessage("Library '{0}' doesn't define a ExStdLibRegister attribute!", ((ExStdLibName)type.GetCustomAttribute(typeof(ExStdLibName))).Name);
            }

            ExMat.StdLibRegistery registery = GetStdLibraryRegisteryMethod(type);
            if (registery == null)
            {
                return vm.AddToErrorMessage("Library '{0}' registery method named '{1}' is not valid. It has to use the delegate 'ExMat.StdLibRegistery(ExVM vm)'", ((ExStdLibName)type.GetCustomAttribute(typeof(ExStdLibName))).Name, GetStdLibraryRegisteryName(type));
            }
            else
            {
                if (registery(vm))
                {
                    RegisterNativeFunctions(vm, type);

                    return ExFunctionStatus.SUCCESS;
                }
                return vm.AddToErrorMessage("something went wrong...");
            }
        }

        /// <summary>
        /// Get all assemblies defined in <see cref="ExMat.Assemblies"/> into an array
        /// </summary>
        /// <returns>An array of assembly information</returns>
        public static Assembly[] GetAllAssemblies()
        {
            return ExMat.Assemblies.Select(p => Assembly.Load(p.Key)).ToArray();
        }

        /// <summary>
        /// Register a standard library
        /// </summary>
        /// <param name="vm">Virtual machine to register the library to<param>
        public static bool RegisterStdLibrary(ExVM vm, Type lib)
        {
            if (lib == null)
            {
                vm.AddToErrorMessage("unknown library");
                return false;
            }
            ExFunctionStatus status = InvokeRegisterOnStdLib(vm, lib);

            if (status == ExFunctionStatus.ERROR)
            {
                return false;
            }

            Dictionary<string, ExObject> consts = GetStdLibraryConstsDict(lib);
            if (consts != null)
            {
                RegisterConstantsFromDict(consts, vm);
            }
            return true;
        }

        /// <summary>
        /// Register std libs from given assembly
        /// </summary>
        /// <param name="vm">Virtual machine to register libraries to<param>
        public static bool RegisterStdLibraries(ExVM vm) // TO-DO Allow plugins/external libraries
        {
            try
            {
                foreach (Type lib in GetStandardLibraryTypes(GetAllAssemblies()))
                {
                    if (!RegisterStdLibrary(vm, lib))
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception err)
            {
                vm.AddToErrorMessage("Failed to load assemblies: {0}", err.Message);
                return false;
            }
        }

        public static bool RegisterConstantsFromDict(Dictionary<string, ExObject> consts, ExVM toVM)
        {
            PushConstsTable(toVM);

            foreach (KeyValuePair<string, ExObject> pair in consts)
            {
                switch (pair.Value.Type)
                {
                    case ExObjType.INTEGER: CreateConstantInt(toVM, pair.Key, pair.Value.GetInt()); break;
                    case ExObjType.FLOAT: CreateConstantFloat(toVM, pair.Key, pair.Value.GetFloat()); break;
                    case ExObjType.STRING: CreateConstantString(toVM, pair.Key, pair.Value.GetString()); break;
                    case ExObjType.ARRAY: CreateConstantList(toVM, pair.Key, pair.Value.GetList()); break;
                    case ExObjType.DICT: CreateConstantDict(toVM, pair.Key, pair.Value.GetDict()); break;
                    case ExObjType.SPACE: CreateConstantSpace(toVM, pair.Key, pair.Value.GetSpace()); break;
                    default: break;
                }
            }

            toVM.Pop();
            return true;
        }

        /// <summary>
        /// Get all types defined in the executing assembly
        /// </summary>
        /// <returns>List of types defined in assembly</returns>
        public static List<Type> GetTypesFromAssemblies()
        {
            return new(Assembly.GetExecutingAssembly().GetTypes());
        }

        public static List<Type> GetStandardLibraryTypes()
        {
            return GetStandardLibraryTypes(GetAllAssemblies());
        }

        public static Type GetStandardLibraryFromName(string name)
        {
            return GetStandardLibraryTypes(GetAllAssemblies()).FirstOrDefault(l => GetLibraryNameFromStdClass(l) == name);
        }

        /// <summary>
        /// Get standard libraries defined in current assembly
        /// </summary>
        /// <param name="asm"></param>
        /// <returns>List of std lib types</returns>
        public static List<Type> GetStandardLibraryTypes(params Assembly[] asm)
        {
            List<Type> types = GetTypesFromAssemblies();
            if (asm != null)
            {
                foreach (Assembly a in asm)
                {
                    types.AddRange(a.GetTypes());
                }
            }

            return types.Where(t => t.IsClass && string.Equals(t.Namespace, ExMat.StandardLibraryNameSpace, StringComparison.Ordinal) && IsStdLib(t)).ToList();
        }

        /// <summary>
        /// Get the <see cref="ExStdLibBase"/> representation of a standard library
        /// </summary>
        /// <param name="stdClass">Standard library type object</param>
        /// <returns>Given stdlib type's <see cref="ExStdLibBase"/> representation</returns>
        public static ExStdLibType GetLibraryTypeFromStdClass(Type stdClass)
        {
            return IsStdLib(stdClass)
                ? ((ExStdLibBase)Attribute.GetCustomAttributes(stdClass).FirstOrDefault(a => a is ExStdLibBase)).Type
                : 0;
        }

        public static ExStdLibType GetLibraryTypeFromName(string name)
        {
            Type t = GetStandardLibraryFromName(name);
            return t == null ? ExStdLibType.UNKNOWN : GetLibraryTypeFromStdClass(t);
        }

        public static bool ReloadLibrary(ExVM vm, string libname)
        {
            return RegisterStdLibrary(vm, GetStandardLibraryTypes(GetAllAssemblies()).FirstOrDefault(l => GetLibraryNameFromStdClass(l) == libname));
        }

        /// <summary>
        /// Get the <see cref="ExStdLibName"/> name of a standard library
        /// </summary>
        /// <param name="stdClass">Standard library type object</param>
        /// <returns>Given stdlib type's <see cref="ExStdLibName"/> name or <see cref="string.Empty"/> if attribute doesn't exist</returns>
        public static string GetLibraryNameFromStdClass(Type stdClass)
        {
            return IsStdLib(stdClass)
                ? ((ExStdLibName)Attribute.GetCustomAttributes(stdClass).FirstOrDefault(a => a is ExStdLibName)).Name
                : string.Empty;
        }

        /// <summary>
        /// Get the <see cref="ExStdLibRegister"/> named registery method of a standard library which is stored in a <see cref="ExMat.StdLibRegistery"/> delegate property
        /// </summary>
        /// <param name="stdClass">Standard library type object</param>
        /// <returns>Given stdlib type's <see cref="ExMat.StdLibRegistery"/> property with <see cref="ExStdLibRegister"/> name or <see langword="null"/> if it doesn't exist</returns>
        public static ExMat.StdLibRegistery GetStdLibraryRegisteryMethod(Type stdClass)
        {
            string regname = GetStdLibraryRegisteryName(stdClass);

            if (string.IsNullOrWhiteSpace(regname))
            {
                return null;
            }
            else
            {
                PropertyInfo pi = stdClass.GetProperty(regname);
                if (pi == null)
                {
                    return null;
                }

                try
                {
                    return (ExMat.StdLibRegistery)pi.GetValue(pi);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Get the <see cref="ExStdLibConstDict"/> named constants dictionary of a standard library which is stored in a <see cref="Dictionary{TKey=String, TVal=ExObject}"/> property
        /// </summary>
        /// <param name="stdClass">Standard library type object</param>
        /// <returns>Given stdlib type's <see cref="Dictionary{TKey=String, TVal=ExObject}"/> property with <see cref="ExStdLibConstDict"/> name or <see langword="null"/> if it doesn't exist</returns>
        public static Dictionary<string, ExObject> GetStdLibraryConstsDict(Type stdClass)
        {
            string cname = GetStdLibraryConstsName(stdClass);

            if (string.IsNullOrWhiteSpace(cname))
            {
                return null;
            }
            else
            {
                PropertyInfo pi = stdClass.GetProperty(cname);
                if (pi == null)
                {
                    return null;
                }

                try
                {
                    return (Dictionary<string, ExObject>)pi.GetValue(pi);
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Get the <see cref="ExStdLibRegister.RegisterMethodName"/> of a standard library 
        /// </summary>
        /// <param name="stdClass">Standard library type object</param>
        /// <returns>Given stdlib type's <see cref="ExStdLibRegister"/> register name or <see cref="string.Empty"/> if attribute doesn't exist/returns>
        public static string GetStdLibraryRegisteryName(Type stdClass)
        {
            return IsStdLib(stdClass) && Attribute.IsDefined(stdClass, typeof(ExStdLibRegister))
                ? ((ExStdLibRegister)Attribute.GetCustomAttributes(stdClass).FirstOrDefault(a => a is ExStdLibRegister)).RegisterMethodName
                : string.Empty;
        }

        /// <summary>
        /// Get the <see cref="ExStdLibConstDict.Name"/> of a standard library 
        /// </summary>
        /// <param name="stdClass">Standard library type object</param>
        /// <returns>Given stdlib type's <see cref="ExStdLibConstDict"/> constants dictionary name or <see cref="string.Empty"/> if attribute doesn't exist/returns>
        public static string GetStdLibraryConstsName(Type stdClass)
        {
            return IsStdLib(stdClass) && Attribute.IsDefined(stdClass, typeof(ExStdLibConstDict))
                ? ((ExStdLibConstDict)Attribute.GetCustomAttributes(stdClass).FirstOrDefault(a => a is ExStdLibConstDict)).Name
                : string.Empty;
        }

        /// <summary>
        /// Check if given class has <see cref="ExStdLibName"/> attribute defined
        /// </summary>
        /// <param name="stdClass">Class to check</param>
        /// <returns><see langword="true"/> if <paramref name="stdClass"/> is a std lib, otherwise <see langword="false"/></returns>
        public static bool IsStdLib(Type stdClass)
        {
            return Attribute.IsDefined(stdClass, typeof(ExStdLibName));
        }

        /// <summary>
        /// Register native functions to a virtual machines roottable
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="lib">Standard library type object to get the native functions from</param>
        public static void RegisterNativeFunctions(ExVM vm, Type lib)
        {
            List<ExNativeFunc> funcs = GetNonDelegateNativeFunctions(lib);

            ExStdLibType libtype = GetLibraryTypeFromStdClass(lib);

            PushRootTable(vm);
            foreach (ExNativeFunc func in funcs)
            {
                RegisterNativeFunction(vm, func, libtype);    // Yerli fonksiyonları oluştur ve kaydet
            }
            vm.Pop();
        }

        public static void CreateNewSlotConts(ExVM vm, string name)
        {
            CreateNewSlot(vm, -3, true, name);
        }

        /// <summary>
        /// Create a constant 64bit integer value
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="name">Name of the constant</param>
        /// <param name="val">Constant's value</param>
        public static void CreateConstantInt(ExVM vm, string name, long val)
        {
            PushString(vm, name, -1);
            vm.Push(val);
            CreateNewSlotConts(vm, name);
        }

        /// <summary>
        /// Create a constant 64bit float value
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="name">Name of the constant</param>
        /// <param name="val">Constant's value</param>
        public static void CreateConstantFloat(ExVM vm, string name, double val)
        {
            PushString(vm, name, -1);
            vm.Push(val);
            CreateNewSlotConts(vm, name);
        }

        /// <summary>
        /// Create a constant string
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="name">Name of the constant</param>
        /// <param name="val">Constant's value</param>
        public static void CreateConstantString(ExVM vm, string name, string val)
        {
            PushString(vm, name, -1);
            PushString(vm, val, -1);
            CreateNewSlotConts(vm, name);
        }

        /// <summary>
        /// Create a constant space
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="name">Name of the constant</param>
        /// <param name="val">Constant's value</param>
        public static void CreateConstantSpace(ExVM vm, string name, ExSpace val)
        {
            PushString(vm, name, -1);
            vm.Push(new ExObject(val));
            CreateNewSlotConts(vm, name);
        }

        /// <summary>
        /// Create a constant dictionary
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="name">Name of the constant</param>
        /// <param name="dict">Dictionary</param>
        public static void CreateConstantDict(ExVM vm, string name, Dictionary<string, ExObject> dict)
        {
            PushString(vm, name, -1);
            vm.Push(new ExObject(dict));
            CreateNewSlotConts(vm, name);
        }

        /// <summary>
        /// Create a constant list
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="name">Name of the constant</param>
        /// <param name="lis">List</param>
        public static void CreateConstantList(ExVM vm, string name, List<ExObject> lis)
        {
            PushString(vm, name, -1);
            vm.Push(new ExList(lis));
            CreateNewSlotConts(vm, name);
        }

        /// <summary>
        /// Push a string to a virtual machine's stack
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="str">String to push to stack</param>
        /// <param name="len">If above zero, pushes <paramref name="str"/><c>[<see cref="Range"/>(0, len)]</c></param>
        public static void PushString(ExVM vm, string str, int len)
        {
            if (string.IsNullOrEmpty(str))
            {
                vm.PushNull();
            }
            else
            {
                //TO-DO
                if (len > 0)
                {
                    str = str[new Range(0, len)];
                }
                if (!vm.SharedState.Strings.ContainsKey(str))
                {
                    vm.SharedState.Strings.Add(str, new(str));
                }
                else
                {
                    vm.SharedState.Strings[str] = new(str);
                }
                vm.Push(str);
            }
        }

        /// <summary>
        /// Create a native closure from a native function and push to a virtual machine's stack
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="f">Native function to create a closure of</param>
        public static void CreateClosure(ExVM vm, ExMat.StdLibFunction f)
        {
            vm.Push(ExNativeClosure.Create(vm.SharedState, f, 0));
        }

        /// <summary>
        /// Set a name of a native function
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="id">Native function index at stack <see cref="ExVM.StackBase"/><c> + <paramref name="id"/> - 1</c></param>
        /// <param name="name">New name for the native function</param>
        public static void SetNativeClosureName(ExVM vm, int id, string name)
        {
            ExObject o = GetFromStack(vm, id);
            if (o.Type == ExObjType.NATIVECLOSURE)
            {
                o.GetNClosure().Name = new(name);
            }
        }

        /// <summary>
        /// Get the type names from a mask
        /// </summary>
        /// <param name="mask">Mask to get type names from</param>
        /// <returns>Type names found in <paramref name="mask"/> wrapped in <c>(</c> and <c>)</c> seperated with <c>,</c></returns>
        public static string GetExpectedTypes(int mask)
        {
            List<string> names = new();
            for (int i = 0; i < 19; i++)
            {
                if (((mask >> i) % 2) == 1)
                {
                    names.Add(((ExBaseType)(1 << i)).ToString());
                }
            }

            return "(" + string.Join(", ", names) + ")";
        }

        /// <summary>
        /// Get the integer <see cref="ExBaseType"/> representation of a given character mask. Refer to <see cref="ExMat.TypeMasks"/>
        /// <param name="c">Character mask</param>
        /// <returns>Integer parsed representation of given mask <paramref name="c"/>
        /// <para> If mask is unknown, returns <see cref="int.MaxValue"/></para></returns>
        private static int CompileTypeChar(char c)
        {
            if (ExMat.TypeMasks.ContainsValue(c))
            {
                return ExMat.TypeMasks.FirstOrDefault(p => p.Value == c).Key;
            }
            return int.MaxValue;  // bilinmeyen maske
        }

        private static string DecompileTypeMaskChar(int c)
        {
            if (ExMat.TypeMasks.ContainsKey(c))
            {
                return ExMat.TypeMasks[c].ToString(CultureInfo.CurrentCulture);
            }
            else
            {
                int n = Enum.GetNames(typeof(ExBaseType)).Length;
                List<char> comb = new();

                for (int i = 0; i < n; i++)
                {
                    if (((1 << i) & c) > 0 && ExMat.TypeMasks.ContainsKey(i))
                    {
                        comb.Add(ExMat.TypeMasks[i]);
                    }
                }
                return string.Join("|", comb);
            }
        }

        /// <summary>
        /// Compile a string mask into list of parameter masks for expected types
        /// </summary>
        /// <param name="mask">String mask for expected types for parameters, refer to <see cref="ExMat.TypeMasks)"/> method</param>
        /// <param name="results">Compilation results from <paramref name="mask"/> for each parameter</param>
        /// <returns><see langword="true"/> if mask was compiled successfully, otherwise <see langword="false"/></returns>
        public static bool CompileTypeMask(string mask, List<int> results)
        {
            int i = 0, m = 0, l = mask.Length;

            while (i < l)
            {
                int _mask = CompileTypeChar(mask[i]);
                if (_mask == int.MaxValue)
                {
                    return false;   // bilinmeyen maske
                }
                else if (_mask == 0)
                {
                    results.Add(-1);
                    i++;
                    m = 0;
                    continue;
                }
                else
                {
                    m |= _mask;
                }

                i++;
                if (i < l && mask[i] == '|')    // "|" var, okumaya devam et
                {
                    i++;
                    if (i == l) // "|" sonrası maske yok
                    {
                        return false;
                    }
                    continue;
                }
                results.Add(m);   // Maskeyi listeye ekle
                m = 0;      // Maskeyi sıfırla
            }
            return true;
        }

        public static string GetNClosureDefaultParamInfo(int i, int paramsleft, List<string> infos, Dictionary<string, ExObject> defs)
        {
            StringBuilder s = new();
            for (; i < paramsleft; i++)
            {
                s.AppendFormat(CultureInfo.CurrentCulture, "[{0}", infos[i]);

                if (defs.ContainsKey((i + 1).ToString(CultureInfo.CurrentCulture)))
                {
                    s.AppendFormat(CultureInfo.CurrentCulture, " = {0}", GetSimpleString(defs[(i + 1).ToString(CultureInfo.CurrentCulture)]));
                }

                s.Append(']');

                if (i != paramsleft - 1)
                {
                    s.Append(',');
                }
            }
            return s.ToString();
        }
        public static string GetSimpleString(ExObject obj)
        {
            switch (obj.Type)
            {

                case ExObjType.COMPLEX:
                    {
                        return obj.GetComplexString();
                    }
                case ExObjType.INTEGER:
                    {
                        return obj.GetInt().ToString(CultureInfo.CurrentCulture);
                    }
                case ExObjType.FLOAT:
                    {
                        return GetFloatString(obj);
                    }
                case ExObjType.STRING:
                    {
                        return obj.GetString();
                    }
                case ExObjType.BOOL:
                    {
                        return obj.Value.b_Bool ? "true" : "false";
                    }
                case ExObjType.NULL:
                    {
                        return obj.Value.s_String ?? "null";
                    }
                case ExObjType.ARRAY:
                    {
                        return "ARRAY";
                    }
                case ExObjType.DICT:
                    {
                        return "DICTIONARY";
                    }
                case ExObjType.NATIVECLOSURE:
                case ExObjType.CLOSURE:
                    {
                        return "FUNCTION";
                    }
                case ExObjType.SPACE:
                    {
                        return obj.GetSpace().GetSpaceString();
                    }
                default:
                    {
                        return string.Empty;
                    }
            }
        }

        public static Dictionary<ExObject, int> GetRepeatCounts(List<ExObject> lis)
        {
            Dictionary<ExObject, int> counts = new();

            if (lis == null)
            {
                return counts;
            }

            for (int i = 0; i < lis.Count; i++)
            {
                ExObject o = lis[i];

                int c = 0;
                bool cfound = false;

                ExObject[] carr = new ExObject[counts.Count];
                counts.Keys.CopyTo(carr, 0);
                KeyValuePair<ExObject, int> pair = counts.FirstOrDefault(p => ++c > 0 && CheckEqual(p.Key, o, ref cfound) && cfound);
                if (pair.Key != null)
                {
                    counts[carr[c - 1]] += 1;
                }
                if (!cfound)
                {
                    counts.Add(o, 1);
                }
            }
            return counts;
        }

        public static string GetTypeMaskInfo(int mask)
        {
            switch (mask)
            {
                case -1:
                case 0:
                    {
                        return "ANY";
                    }
                default:
                    {
                        int n = Enum.GetNames(typeof(ExBaseType)).Length;
                        List<string> comb = new();

                        for (int i = 0; i < n; i++)
                        {
                            if (((1 << i) & mask) > 0)
                            {
                                comb.Add(((ExBaseType)(1 << i)).ToString());
                            }
                        }
                        return string.Join("|", comb);
                    }
            }
        }

        public static bool GetTypeMaskInfos(List<int> masks, List<string> info)
        {
            if (masks == null || info == null)
            {
                return false;
            }

            foreach (int mask in masks)
            {
                info.Add(GetTypeMaskInfo(mask));
            }
            return true;
        }

        public static ExNativeFuncBase GetNativeFunc(MethodBase method)
        {
            return (ExNativeFuncBase)method.GetCustomAttributes(typeof(ExNativeFuncBase), true).FirstOrDefault();
        }

        public static ExNativeParamBase GetNativeParam(MethodBase method, int index)
        {
            return (ExNativeParamBase)method.GetCustomAttributes(typeof(ExNativeParamBase), true).FirstOrDefault(a => a is ExNativeParamBase p && p.Index == index);
        }

        public static ExObject GetNativeParamDefault(MethodBase method, int index)
        {
            return ((ExNativeParamBase)method.GetCustomAttributes(typeof(ExNativeParamBase), true).FirstOrDefault(a => a is ExNativeParamBase p && p.Index == index)).DefaultValue;
        }

        /// <summary>
        /// Set parameter checks for the native function on top of the stack of <paramref name="vm"/>
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="n">Used to decide required amount of arguments, shouldn't be equal to <c>0</c>
        /// <para> <paramref name="n"/> less than <c>0</c> : Has -<paramref name="n"/> parameters, -<paramref name="n"/> + <c>1</c> required</para>
        /// <para><paramref name="n"/> higher than <c>0</c> : Has <paramref name="n"/> - <c>1</c> parameters, <paramref name="n"/> - <c>1</c> required</para></param>
        /// <param name="mask">Parameter type masks to compile</param>
        public static void SetParamCheck(ExVM vm, int n, string mask)
        {
            ExObject o = GetFromStack(vm, -1);

            if (o.Type == ExObjType.NATIVECLOSURE)
            {
                ExNativeClosure nc = o.GetNClosure();
                nc.nParameterChecks = n;

                List<int> r = new();
                if (!string.IsNullOrEmpty(mask)
                    && !CompileTypeMask(mask, r))
                {
                    Throw("failed to compile type mask", vm);
                }
                nc.TypeMasks = r;
            }
        }

        /// <summary>
        /// Create a dictionary of parameter indices and default values from a given parameter list
        /// </summary>
        /// <param name="ps">List of parameters</param>
        /// <returns>Parameter index and default value filled dictionary</returns>
        public static Dictionary<int, ExObject> GetDefaultValuesFromParameters(List<ExNativeParam> ps)
        {
            Dictionary<int, ExObject> d = new();    // TO-DO change how default values are handled
            if (ps == null)
            {
                return d;
            }

            for (int i = 0; i < ps.Count; i++)
            {
                if (ps[i].HasDefaultValue)
                {
                    d.Add(i + 1, new(ps[i].DefaultValue));
                }
            }
            return d;
        }

        /// <summary>
        /// Set default values for parameters
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="d">Dictionary of integer keys for parameter numbers and <see cref="ExObject"/> values for default values</param>
        public static void SetDefaultValues(ExVM vm, List<ExNativeParam> ps)
        {
            ExObject o = GetFromStack(vm, -1);
            if (o.Type == ExObjType.NATIVECLOSURE)
            {
                o.GetNClosure().DefaultValues = GetDefaultValuesFromParameters(ps);
            }
        }

        public static void SetDocumentation(ExVM vm, ExNativeFunc reg)
        {
            ExObject o = GetFromStack(vm, -1);
            if (o.Type == ExObjType.NATIVECLOSURE)
            {
                o.GetNClosure().Documentation = CreateDocStringFromRegFunc(reg, o.GetNClosure().TypeMasks.Count == 0);
                o.GetNClosure().Summary = reg.Description;
                o.GetNClosure().Returns = reg.ReturnsType;
                o.GetNClosure().Base = GetSimpleLibNameFromStdLibType(reg.Base);
            }
        }

        public static ExStdLibType GetStdLibTypeFromSimpleLibName(string name)
        {
            string n = Enum.GetNames(typeof(ExStdLibType)).FirstOrDefault(t => GetSimpleLibNameFromStdLibType(Enum.Parse<ExStdLibType>(t)) == name);
            return string.IsNullOrWhiteSpace(n) ? ExStdLibType.UNKNOWN : Enum.Parse<ExStdLibType>(n);
        }

        public static string GetSimpleLibNameFromStdLibType(ExStdLibType type)
        {
            return type.ToString().ToLower(CultureInfo.CurrentCulture);
        }

        public static string GetParameterInfoString(ExNativeParam param, int pno)
        {
            return param.HasDefaultValue
                ? string.Format(CultureInfo.CurrentCulture, "\n\t{0}. [<{1}> {2} = {3}] : {4}",
                                pno,
                                GetTypeMaskInfo(param.Type),
                                param.Name,
                                param.DefaultValue.Type == ExObjType.STRING
                                    ? GetEscapedFormattedString(param.DefaultValue.GetString())
                                    : GetSimpleString(param.DefaultValue),
                                param.Description)
                : string.Format(CultureInfo.CurrentCulture, "\n\t{0}. <{1}> {2} : {3}", pno, GetTypeMaskInfo(param.Type), param.Name, param.Description);
        }

        public static string CreateDocStringFromRegFunc(ExNativeFunc reg, bool hasVargs)
        {
            StringBuilder s = new();

            if (reg.IsDelegateFunction)
            {
                s.AppendFormat(CultureInfo.CurrentCulture, "Base: {0}", ((ExBaseType)CompileTypeChar(reg.BaseTypeMask)).ToString());
            }
            else
            {
                s.AppendFormat(CultureInfo.CurrentCulture, "Library: {0}", reg.Base.ToString().ToLower(CultureInfo.CurrentCulture));
            }

            s.AppendFormat(CultureInfo.CurrentCulture, "\nFunction: <{0}> {1}", reg.ReturnsType, reg.Name);

            int i = 0, j = 0;

            s.Append("\nParameters:");

            if (hasVargs && reg.NumberOfParameters != -1)
            {
                while (j < -reg.NumberOfParameters - 1)
                {
                    s.AppendFormat(CultureInfo.CurrentCulture, "\n\t{0}. <ANY> unknown", ++j);
                }
            }

            while (reg.Parameters != null
                && i < reg.Parameters.Count)
            {
                s.Append(GetParameterInfoString(reg.Parameters[i], j + ++i));
            }

            if (hasVargs)
            {
                s.AppendFormat(CultureInfo.CurrentCulture, "\n\t{0}. <...> vargs", j + i + 1);
            }
            else if (i + j == 0)
            {
                s.Append(" No parameters");
            }

            s.AppendFormat(CultureInfo.CurrentCulture, "\nSummary: {0}", reg.Description);

            return s.ToString();
        }

        public static void Throw(string msg, ExVM vm, ExExceptionType type = ExExceptionType.BASE)
        {
            switch (type)
            {
                case ExExceptionType.BASE:
                    throw new ExException(vm, msg);
                case ExExceptionType.COMPILER:
                    throw new ExCompilerException(vm, msg);
                case ExExceptionType.RUNTIME:
                    throw new ExRuntimeException(vm, msg);
                default:
                    throw new Exception(msg);
            }
        }

        public static void HandleException(Exception exp, ExVM vm)
        {
            vm.AddToErrorMessage(exp.Message);
            vm.AddToErrorMessage(exp.StackTrace);
            WriteErrorMessages(vm, ExErrorType.INTERNAL);
        }

        public static void HandleException(ExException exp, ExVM vm, ExErrorType typeOverride = ExErrorType.DEFAULT)
        {
            vm.AddToErrorMessage(exp.Message);
            WriteErrorMessages(vm, typeOverride);
        }

        /// <summary>
        /// Creates a new key-value pair in a dictionary or a member in a class
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="idx">Stack index of target object to create a new slot on:
        /// <para><see cref="ExVM.StackBase"/><c> + <paramref name="idx"/> - 1</c></para></param>
        public static void CreateNewSlot(ExVM vm, int idx, bool isConst = false, string name = "")
        {
            if (GetTopOfStack(vm) < 3)
            {
                Throw("not enough parameters in stack", vm);
            }
            ExObject self = GetFromStack(vm, idx);
            if (self.Type is ExObjType.DICT or ExObjType.CLASS)
            {
                ExObject k = new();
                k.Assign(vm.GetAbove(-2));
                if (ExTypeCheck.IsNull(k))
                {
                    Throw("'null' is not a valid key", vm);
                }
                vm.NewSlot(self, k, vm.GetAbove(-1), false);

                if (isConst)
                {
                    if (!vm.SharedState.Consts.ContainsKey(name))
                    {
                        vm.SharedState.Consts.Add(name, new(vm.GetAbove(-1)));
                    }
                    else
                    {
                        vm.SharedState.Consts[name] = new(vm.GetAbove(-1));
                    }
                }
                vm.Pop(2);
            }
        }

        /// <summary>
        /// Count how many times an object appears in a list
        /// </summary>
        /// <param name="lis">List of objects to iterate through</param>
        /// <param name="obj">Object to count appearences of</param>
        /// <returns>Count of <paramref name="obj"/> in <paramref name="lis"/></returns>
        public static int CountValueEqualsInArray(List<ExObject> lis, ExObject obj)
        {
            int i = 0;
            bool f = false;
            int count = 0;
            for (; i < lis.Count; i++)
            {
                CheckEqual(lis[i], obj, ref f);
                if (f)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Find the index of an object in a list
        /// </summary>
        /// <param name="lis">List of objects to iterate through</param>
        /// <param name="obj">Objects to search for the index</param>
        /// <returns>First index <paramref name="obj"/> appears in <paramref name="lis"/> or <c>-1</c> if object wasn't found in the list</returns>
        public static int GetValueIndexFromArray(List<ExObject> lis, ExObject obj)
        {
            for (int i = 0; i < lis.Count; i++)
            {
                bool f = false;
                CheckEqual(lis[i], obj, ref f);
                if (f)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Gets an attribute of a given function and assigns it to given destination
        /// </summary>
        /// <param name="func">Function to get the attribute from</param>
        /// <param name="attr">Attribute name</param>
        /// <param name="dest">Attribute's value if found</param>
        /// <returns>If attribute exists <see cref="ExGetterStatus.FOUND"/>, otherwise <see cref="ExGetterStatus.ERROR"/></returns>
        public static ExGetterStatus GetFunctionAttribute(IExClosure func, string attr, ref ExObject dest)
        {
            switch (attr)
            {
                case ExMat.DocsName:
                case ExMat.ReturnsName:
                case ExMat.HelpName:
                case ExMat.FuncName:
                    {
                        dest = new((string)func.GetAttribute(attr));
                        return ExGetterStatus.FOUND;
                    }
                case ExMat.DelegName:
                case ExMat.VargsName:
                    {
                        dest = new((bool)func.GetAttribute(attr));
                        return ExGetterStatus.FOUND;
                    }
                case ExMat.nParams:
                case ExMat.nDefParams:
                case ExMat.nMinArgs:
                    {
                        dest = new((int)func.GetAttribute(attr));
                        return ExGetterStatus.FOUND;
                    }
                case ExMat.DefParams:
                    {
                        dest = new((Dictionary<string, ExObject>)func.GetAttribute(attr));
                        return ExGetterStatus.FOUND;
                    }
                default:
                    {
                        dynamic res = func.GetAttribute(attr);
                        if (res is null)
                        {
                            return ExGetterStatus.ERROR;
                        }
                        dest = new((ExObject)res);
                        return ExGetterStatus.FOUND;
                    }
            }
        }

        /// <summary>
        /// Parse string to integer, allows hex and binary
        /// </summary>
        /// <param name="s">String to parse</param>
        /// <param name="res">Resulting value</param>
        /// <returns><see langword="true"/> if parsing was successful, otherwise <see langword="false"/></returns>
        public static bool ParseStringToInteger(string s, ref ExObject res)
        {
            if (long.TryParse(s, out long r))
            {
                res = new(r);
            }
            else if (s.StartsWith("0x", StringComparison.Ordinal))
            {
                if (s.Length <= 18
                    && long.TryParse(s[2..], NumberStyles.HexNumber, null, out long hr))
                {
                    res = new(hr);
                }
            }
            else if (s.StartsWith("0b", StringComparison.Ordinal)
                    && s.Length <= 34)
            {
                try
                {
                    res = new(Convert.ToInt32(s[2..], 2));
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else if (s.StartsWith("0B", StringComparison.Ordinal)
                    && s.Length <= 66)
            {
                try
                {
                    res = new(Convert.ToInt64(s[2..], 2));
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parse string to float, allows hex and binary
        /// </summary>
        /// <param name="s">String to parse</param>
        /// <param name="res">Resulting value</param>
        /// <returns><see langword="true"/> if parsing was successful, otherwise <see langword="false"/></returns>
        public static bool ParseStringToFloat(string s, ref ExObject res)
        {
            if (double.TryParse(s, out double r))
            {
                res = new(r);
            }
            else if (s.StartsWith("0x", StringComparison.Ordinal))
            {
                if (s.Length <= 18
                    && long.TryParse(s[2..], NumberStyles.HexNumber, null, out long hr))
                {
                    res = new(new DoubleLong() { i = hr }.f);
                }
            }
            else if (s.StartsWith("0b", StringComparison.Ordinal)
                    && s.Length <= 66)
            {
                try
                {
                    res = new(new DoubleLong() { i = Convert.ToInt64(s[2..], 2) }.f);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Compile a code string and push resulting <see langword="main"/> function to virtual machine stack
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="source">Code string to compile</param>
        /// <returns><see langword="true"/> if <paramref name="source"/> was compiled successfully
        /// <para><see langword="false"/> if there was a compilation error</para></returns>
        public static bool CompileSource(ExVM vm, string source)
        {
            ExCompiler c = new(true);   // derleyici
            ExObject main = new();      // main fonksiyonu

            if (c.InitializeCompiler(vm, source, ref main))
            {
                // main'i belleğe yükle
                vm.Push(ExClosure.Create(vm.SharedState, main.Value._FuncPro));
                return true;
            }
            vm.ErrorString = c.ErrorString;
            return false;
        }

        /// <summary>
        /// Push the roottable to virtual machine's stack
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <returns>Always returns <see langword="true"/></returns>
        public static bool PushRootTable(ExVM vm)
        {
            vm.Push(vm.RootDictionary);
            return true;
        }

        /// <summary>
        /// Push the constants table to given VM's stack
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <returns>Always returns <see langword="true"/></returns>
        public static bool PushConstsTable(ExVM vm)
        {
            vm.Push(vm.SharedState.Consts);
            return true;
        }

        /// <summary>
        /// Get an object from a virtual machine's stack
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="idx">Stack index of the object:
        /// <para>if <paramref name="idx"/> less than 0: <see cref="ExVM.StackTop"/><c> - <paramref name="idx"/></c></para>
        /// <para>if <paramref name="idx"/> higher than 0: <see cref="ExVM.StackBase"/><c> + <paramref name="idx"/> - 1</c></para></param>
        /// <returns>Object from the <see cref="ExVM.Stack"/></returns>
        public static ExObject GetFromStack(ExVM vm, int idx)
        {
            return idx >= 0 ? vm.GetAt(idx + vm.StackBase - 1) : vm.GetAbove(idx);
        }
        /// <summary>
        /// Calculate how far top is from base
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <returns><see cref="ExVM.StackTop"/> - <see cref="ExVM.StackBase"/></returns>
        public static int GetTopOfStack(ExVM vm)
        {
            return vm.StackTop - vm.StackBase;
        }

        /// <summary>
        /// Call a closure with given amount of arguments from stack. Is also used to execute <c>main</c> from stack.
        /// </summary>
        /// <param name="vm">Virtual machine to use</param>
        /// <param name="nArguments">Amount of arguments to pick from top of the stack</param>
        /// <param name="returnVal">Wheter to push resulting value to stack</param>
        /// <param name="forceReturn">Wheter to return last statement's return value in case there is no return statement.
        /// <para>This helps interactive console display values without return statements</para></param>
        /// <returns><see langword="true"/> if call was executed successfully
        /// <para><see langword="false"/> if there was a runtime error</para></returns>
        public static bool Call(ExVM vm, int nArguments, bool returnVal, bool forceReturn = false)
        {
            ExObject result = new();
            ExObject main = vm.GetAbove(-(nArguments + 1));
            if (vm.Call(ref main, nArguments, vm.StackTop - nArguments, ref result, forceReturn))
            {
                vm.Pop(nArguments);
                if (returnVal)
                {
                    vm.Push(result);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Transpose a matrix of <see cref="ExObject"/>s
        /// </summary>
        /// <param name="rows">Amount of rows</param>
        /// <param name="cols">Amount of columns</param>
        /// <param name="vals">List of <see cref="ExObjType.ARRAY"/> objects</param>
        /// <returns>Transposed list of <see cref="ExObjType.ARRAY"/> objects with <c><paramref name="cols"/> x <paramref name="rows"/></c> dimensions</returns>
        public static List<ExObject> TransposeMatrix(int rows, int cols, List<ExObject> vals)
        {
            List<ExObject> lis = new(cols);
            for (int i = 0; i < cols; i++)
            {
                lis.Add(new ExObject(new List<ExObject>(rows)));
                for (int j = 0; j < rows; j++)
                {
                    lis[i].GetList().Add(vals[j].Value.l_List[i]);
                }
            }
            return lis;
        }

        /// <summary>
        /// Assert transpose operation rules on given matrix and get column count
        /// </summary>
        /// <param name="vm">Virtual machine to use in case of errors</param>
        /// <param name="vals">List of <see cref="ExObjType.ARRAY"/> objects</param>
        /// <param name="cols">Integer variable to store column count if there were no errors</param>
        /// <returns><see langword="true"/> if <paramref name="vals"/> was a valid matrix
        /// <para><see langword="false"/> if <paramref name="vals"/> wasn't a valid matrix</para></returns>
        public static bool DoMatrixTransposeChecks(ExVM vm, List<ExObject> vals, ref int cols)
        {
            foreach (ExObject row in vals)
            {
                if (row.Type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("given list have to contain lists");
                    return false;
                }
                else
                {
                    if (!ExUtils.AssertNumericArray(row))
                    {
                        vm.AddToErrorMessage("given list have to contain lists of numeric values");
                        return false;
                    }

                    if (cols != 0 && row.GetList().Count != cols)
                    {
                        vm.AddToErrorMessage("given list have varying length of lists");
                        return false;
                    }
                    else
                    {
                        cols = row.GetList().Count;
                    }
                }
            }

            if (cols == 0)
            {
                vm.AddToErrorMessage("empty list can't be used for transposing");
                return false;
            }
            return true;
        }



        public static bool ConvertIntegerStringArrayToString(List<ExObject> lis, StringBuilder str)
        {
            foreach (ExObject o in lis)
            {
                if (o.Type == ExObjType.STRING) // && o.GetString().Length == 1)
                {
                    str.Append(o.GetString());
                }
                else if (o.Type == ExObjType.INTEGER && o.GetInt() >= 0 && o.GetInt() <= char.MaxValue)
                {
                    str.Append((char)o.GetInt());
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public static string Escape(string str)
        {
            return SymbolDisplay.FormatLiteral(str, false);
        }

        public static string EscapeCmdEchoString(string str)
        {
            return str.Replace("%", "%%")
                    .Replace("^", "^^")
                    .Replace("&", "^&")
                    .Replace("<", "^<")
                    .Replace(">", "^>")
                    .Replace("|", "^|")
                    .Replace("'", "^'")
                    .Replace(",", "^,")
                    .Replace(";", "^;")
                    .Replace("=", "^=")
                    .Replace("(", "^(")
                    .Replace(")", "^)")
                    .Replace("!", "^!")
                    .Replace("\"", "^\"");
        }

        private static bool ShouldPrintTop(ExVM vm)
        {
            return !vm.PrintedToConsole
                    && ExTypeCheck.IsNotNull(vm.GetAbove(-1));
        }

        private static string FitConsoleString(int width, string source)
        {
            int slen = source.Length;
            return "/" + new string(' ', (width - slen) / 2) + source + new string(' ', ((width - slen) / 2) + (slen % 2 == 1 ? 1 : 0)) + "/\n";
        }

        /// <summary>
        /// Writes version and program info in different colors
        /// </summary>
        /// <param name="vm">Virtual machine to get information from</param>
        public static void WriteInfoString(ExVM vm)
        {
            string version = vm.SharedState.Consts.ContainsKey("_version_")
                ? vm.SharedState.Consts["_version_"].GetString()
                : "<UNKNOWN_VERSION>";

            string date = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();

            int width = 120;

            Console.BackgroundColor = ConsoleColor.DarkBlue;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(new string('/', width + 2));

            Console.Write(FitConsoleString(width, version));
            Console.Write(FitConsoleString(width, date));
            Console.Write(FitConsoleString(width, ExMat.HelpInfoString));

            Console.WriteLine(new string('/', width + 2));

            Console.ResetColor();
        }

        /// <summary>
        /// Writes <c>OUT[<paramref name="line"/>]</c> for <c><paramref name="line"/></c>th output line's beginning
        /// </summary>
        /// <param name="line">Output line number</param>
        public static void WriteOut(int line)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("OUT[");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(line);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("]: ");
            Console.ResetColor();
        }

        /// <summary>
        /// Writes <c>IN [<paramref name="line"/>]</c> for <c><paramref name="line"/></c>th input line's beginning
        /// </summary>
        /// <param name="line">Input line number</param>
        public static void WriteIn(int line)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if (line > 0)
            {
                Console.Write("\n");
            }
            Console.Write("\nIN [");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(line);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("]: ");
            Console.ResetColor();
        }

        public static ExErrorType GetErrorTypeFromException(ExException exp)
        {
            switch (exp.Type)
            {
                case ExExceptionType.BASE:
                    {
                        return ExErrorType.DEFAULT;
                    }
                case ExExceptionType.RUNTIME:
                    {
                        return ExErrorType.RUNTIME;
                    }
                case ExExceptionType.COMPILER:
                    {
                        return ExErrorType.COMPILE;
                    }
                default:
                    {
                        return ExErrorType.INTERNAL;
                    }
            }
        }


        public static void SleepVM(ExVM vm, int time)
        {
            vm.IsSleeping = true;
            Thread.Sleep(time);
            vm.IsSleeping = false;
        }

        public static string GetEscapedFormattedString(string raw)
        {
            return string.Format(CultureInfo.CurrentCulture, "\'{0}\'", Escape(raw));
        }

        public static int CallTop(ExVM vm)
        {
            PushRootTable(vm);            // Global tabloyu belleğe yükle
            if (Call(vm, 1, true, true))
            {
                bool isString = vm.GetAbove(-1).Type == ExObjType.STRING;

                if (ShouldPrintTop(vm)
                    && ToString(vm, -1, 2))
                {
                    WriteOut(vm.InputCount++);
                    if (isString)
                    {
                        vm.Print(GetEscapedFormattedString(vm.GetAbove(-1).GetString()));
                    }
                    else
                    {
                        vm.Print(vm.GetAbove(-1).GetString());
                    }
                }
            }
            else
            {
                return vm.ExitCalled ? vm.ExitCode : WriteErrorMessages(vm, ExErrorType.RUNTIME);
            }

            return 0;
        }

        private static Thread GarbageCollector;
        /// <summary>
        /// Call garbage collector
        /// </summary>
        public static void CollectGarbage()
        {
            if (GarbageCollector != null && GarbageCollector.IsAlive)
            {
                return;
            }

            GarbageCollector = new(() =>
            {
                for (int i = 0; i < ExMat.GCCOLLECTCOUNT; i++)
                {
                    GC.Collect(); //lgtm [cs/call-to-gc]
                    GC.WaitForPendingFinalizers();
                    GC.Collect(); //lgtm [cs/call-to-gc]
                    if (i != ExMat.GCCOLLECTCOUNT - 1)
                    {
                        GC.WaitForPendingFinalizers();
                        Thread.Sleep(200);
                    }
                }
            });

            GarbageCollector.Start();



        }

        /// <summary>
        /// Initialize a virtual machine with given stack size
        /// </summary>
        /// <param name="stacksize">Stack size</param>
        /// <param name="interacive">Wheter to make the virtual machine interactive</param>
        /// <returns>A new virtual machine instance with stack size <paramref name="stacksize"/></returns>
        public static ExVM Start(int stacksize, bool interacive = false)
        {
            ExSState exS = new();
            exS.Initialize();
            ExVM vm = new() { SharedState = exS };

            exS.Root = vm;

            vm.Initialize(stacksize);
            vm.IsInteractive = interacive;
            vm.InputCount = 0;
            return vm;
        }

        /// <summary>
        /// Writes error traces stored in a virtual machine to console
        /// </summary>
        /// <param name="vm">Virtual machine to use</param>
        public static void WriteErrorTraces(ExVM vm)
        {
            foreach (List<int> pair in vm.ErrorTrace)
            {
                vm.PrintLine("[TRACE] [LINE: " + pair[0] + ", INSTRUCTION ID: " + pair[1] + "] ");
            }
        }

        /// <summary>
        /// Write error messages depending on the error type
        /// </summary>
        /// <param name="vm">Virtual machine to use</param>
        /// <param name="typ">Type of error</param>
        /// <returns>Always returns <c>-1</c></returns>
        public static int WriteErrorMessages(ExVM vm, ExErrorType typ)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            vm.PrintLine("\n\n*******************************");
            if (vm.ErrorOverride != ExErrorType.DEFAULT)
            {
                typ = vm.ErrorOverride;
                vm.ErrorOverride = ExErrorType.DEFAULT;
            }

            switch (typ)
            {
                case ExErrorType.COMPILE:
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        vm.PrintLine("COMPILER ERROR");
                        Console.ForegroundColor = ConsoleColor.White;
                        vm.PrintLine(vm.ErrorString);
                        break;
                    }
                case ExErrorType.RUNTIME:
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        vm.PrintLine("RUNTIME ERROR");
                        Console.ForegroundColor = ConsoleColor.White;
                        WriteErrorTraces(vm);
                        vm.PrintLine(vm.ErrorString);
                        break;
                    }
                case ExErrorType.INTERRUPT:
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        vm.PrintLine("INTERRUPTED");
                        Console.ForegroundColor = ConsoleColor.White;
                        vm.PrintLine(vm.ErrorString);
                        break;
                    }
                case ExErrorType.INTERRUPTINPUT:
                    {
                        Console.ForegroundColor = ConsoleColor.Magenta;
                        vm.PrintLine("INPUT STREAM RESET");
                        Console.ForegroundColor = ConsoleColor.White;
                        vm.PrintLine(vm.ErrorString);
                        break;
                    }
                case ExErrorType.INTERNAL:
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        vm.PrintLine("INTERNAL ERROR");
                        Console.ForegroundColor = ConsoleColor.White;
                        vm.PrintLine(vm.ErrorString);
                        vm.PrintLine("\nPress any key to close the window...");
                        Console.ReadKey(true);
                        break;
                    }
            }

            Console.ForegroundColor = ConsoleColor.DarkRed;
            vm.PrintLine("*******************************");
            Console.ResetColor();

            vm.ErrorString = string.Empty;

            return -1;
        }
    }
}
