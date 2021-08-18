﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ExMat.Closure;
using ExMat.Compiler;
using ExMat.Objects;
using ExMat.States;
using ExMat.VM;

namespace ExMat.API
{
    /// <summary>
    /// A middle-ground class for accessing mostly <see cref="ExVM"/> related methods easier
    /// </summary>
    public static class ExAPI
    {
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

        /// <summary>
        /// Parse an object as 64bit floating point value
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="idx">Object index to stringify: <see cref="ExVM.StackBase"/><c> + <paramref name="idx"/> - 1</c></param>
        /// <param name="pop">Amount of objects to pop from stack, used for returning values in interactive console</param>
        /// <returns><see langword="true"/> if object was successfully parsed as float
        /// <para><see langword="false"/> if there was an error parsing the object</para></returns>
        public static bool ToFloat(ExVM vm, int idx, int pop = 0)
        {
            ExObject o = GetFromStack(vm, idx);
            ExObject res = null;
            if (!vm.ToFloat(o, ref res))
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
        public static bool ToInteger(ExVM vm, int idx, int pop = 0)
        {
            ExObject o = GetFromStack(vm, idx);
            ExObject res = null;
            if (!vm.ToInteger(o, ref res))
            {
                return false;
            }
            vm.CleanReturn(pop, res);
            return true;
        }

        public static ConsoleColor GetColorFromName(string name, ConsoleColor def = ConsoleColor.Black)
        {
            switch (name.Trim().ToLower())
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
                switch (enc.ToLower())
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
            return Convert.ToString(i, 2).PadLeft(bits, '0').Select(c => int.Parse(c.ToString())).ToArray();
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
        /// <returns>A <see cref="ExRegFunc"/> if <paramref name="name"/> was found in <paramref name="fs"/> list
        /// <para><see langword="null"/> if there was no functions named <paramref name="name"/> in <paramref name="fs"/></para></returns>
        public static ExRegFunc FindNativeFunction(ExVM vm, List<ExRegFunc> fs, string name)
        {
            foreach (ExRegFunc f in fs)
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
        /// <returns><see langword="true"/> if <paramref name="name"/> was found in <paramref name="fs"/> and reloaded
        /// <para><see langword="false"/> if there was no function named <paramref name="name"/> in <paramref name="fs"/></para></returns>
        public static bool ReloadNativeFunction(ExVM vm, List<ExRegFunc> fs, string name)
        {
            ExRegFunc r;
            if ((r = FindNativeFunction(vm, fs, name)) != null)
            {
                RegisterNativeFunction(vm, r);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Register a native function to a virtual machines roottable
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="func">Native function</param>
        /// <param name="force">Used to determine if call was a reload call or first call from program's execution</param>
        public static void RegisterNativeFunction(ExVM vm, ExRegFunc func)
        {
            PushString(vm, func.Name, -1);              // Fonksiyon ismi
            CreateClosure(vm, func.Function);           // Fonksiyonun temeli
            SetNativeClosureName(vm, -1, func.Name);    // İsmi fonksiyon temeline ekle
            SetParamCheck(vm, func.nParameterChecks, func.ParameterMask);   // Parametre kontrolü
            SetDefaultValues(vm, func.DefaultValues);   // Varsayılan parametre değerleri
            CreateNewSlot(vm, -3);                      // Tabloya kaydet
        }

        /// <summary>
        /// Register native functions to a virtual machines roottable
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="funcs">List of native functions</param>
        public static void RegisterNativeFunctions(ExVM vm, List<ExRegFunc> funcs)
        {
            foreach (ExRegFunc func in funcs)
            {
                RegisterNativeFunction(vm, func);    // Yerli fonksiyonları oluştur ve kaydet
            }
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
            CreateNewSlot(vm, -3);
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
            CreateNewSlot(vm, -3);
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
            CreateNewSlot(vm, -3);
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
            CreateNewSlot(vm, -3);
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
            CreateNewSlot(vm, -3);
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
        public static void CreateClosure(ExVM vm, ExRegFunc.FunctionRef f)
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
        /// Compile a string mask into list of parameter masks for expected types
        /// </summary>
        /// <param name="mask">String mask for expected types for parameters
        /// <para>Use <c>|</c> for multiple types for parameters, each character represents a parameter unless combined with <c>|</c> character</para>
        /// <para>Spaces are ignored </para>
        /// <para>    Mask    |   Expected Type</para>
        /// <para>-------------------------</para>
        /// <para>    .       |   Any type</para>
        /// <para>    e       |   NULL</para>
        /// <para>    i       |   INTEGER</para>
        /// <para>    f       |   FLOAT</para>
        /// <para>    C       |   COMPLEX     (Capital c)</para>
        /// <para>    b       |   BOOL</para>
        /// <para>    n       |   INTEGER|FLOAT|COMPLEX</para>
        /// <para>    s       |   STRING</para>
        /// <para>    d       |   DICT</para>
        /// <para>    a       |   ARRAY</para>
        /// <para>    c       |   CLOSURE|NATIVECLOSURE</para>
        /// <para>    x       |   INSTANCE</para>
        /// <para>    y       |   CLASS</para>
        /// <para>    w       |   WEAKREF</para>
        /// </param>
        /// <param name="results">Compilation results from <paramref name="mask"/> for each parameter</param>
        /// <returns></returns>
        public static bool CompileTypeMask(string mask, List<int> results)
        {
            int i = 0, m = 0, l = mask.Length;

            while (i < l)
            {
                switch (mask[i])    // Her bir karakteri incele
                {
                    case '.': m = -1; results.Add(m); i++; m = 0; continue;
                    case 'e': m |= (int)ExBaseType.NULL; break;
                    case 'i': m |= (int)ExBaseType.INTEGER; break;
                    case 'f': m |= (int)ExBaseType.FLOAT; break;
                    case 'C': m |= (int)ExBaseType.COMPLEX; break;
                    case 'b': m |= (int)ExBaseType.BOOL; break;
                    case 'n': m |= (int)ExBaseType.INTEGER | (int)ExBaseType.FLOAT | (int)ExBaseType.COMPLEX; break;
                    case 's': m |= (int)ExBaseType.STRING; break;
                    case 'd': m |= (int)ExBaseType.DICT; break;
                    case 'a': m |= (int)ExBaseType.ARRAY; break;
                    case 'c': m |= (int)ExBaseType.CLOSURE | (int)ExBaseType.NATIVECLOSURE; break;
                    case 'x': m |= (int)ExBaseType.INSTANCE; break;
                    case 'y': m |= (int)ExBaseType.CLASS; break;
                    case 'w': m |= (int)ExBaseType.WEAKREF; break;
                    default: return false;  // bilinmeyen maske
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

                if (!string.IsNullOrEmpty(mask))
                {
                    List<int> r = new();
                    if (!CompileTypeMask(mask, r))
                    {
                        throw new Exception("failed to compile type mask");
                    }
                    nc.TypeMasks = r;
                }
                else
                {
                    nc.TypeMasks = new();
                }
            }
        }

        /// <summary>
        /// Set default values for parameters
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="d">Dictionary of integer keys for parameter numbers and <see cref="ExObject"/> values for default values</param>
        public static void SetDefaultValues(ExVM vm, Dictionary<int, ExObject> d)
        {
            ExObject o = GetFromStack(vm, -1);

            if (o.Type == ExObjType.NATIVECLOSURE)
            {
                o.GetNClosure().DefaultValues = d;
            }
        }

        /// <summary>
        /// Creates a new key-value pair in a dictionary or a member in a class
        /// </summary>
        /// <param name="vm">Virtual machine to use the stack of</param>
        /// <param name="idx">Stack index of target object to create a new slot on:
        /// <para><see cref="ExVM.StackBase"/><c> + <paramref name="idx"/> - 1</c></para></param>
        public static void CreateNewSlot(ExVM vm, int idx)
        {
            if (GetTopOfStack(vm) < 3)
            {
                throw new Exception("not enough parameters in stack");
            }
            ExObject self = GetFromStack(vm, idx);
            if (self.Type == ExObjType.DICT || self.Type == ExObjType.CLASS)
            {
                ExObject k = new();
                k.Assign(vm.GetAbove(-2));
                if (k.Type == ExObjType.NULL)
                {
                    throw new Exception("'null' is not a valid key");
                }
                vm.NewSlot(self, k, vm.GetAbove(-1), false);
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
                ExVM.CheckEqual(lis[i], obj, ref f);
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
                ExVM.CheckEqual(lis[i], obj, ref f);
                if (f)
                {
                    return i;
                }
            }
            return -1;
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
        public static void PushRootTable(ExVM vm)
        {
            vm.Push(vm.RootDictionary);
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
        // TO-DO: FIX THIS MESS ....
        public static bool Call(ExVM vm, int pcount, bool ret, bool bP = false, bool cls = false, int nargs = 2)
        {
            ExObject res = new();
            ExObject tmp = vm.GetAbove(-(nargs) + (cls ? -1 : 0));
            if (cls)
            {
                int top = vm.StackTop;
                vm.StackTop = vm.StackBase + tmp.GetClosure().Function.StackSize + 2;
                if (vm.Call(ref tmp,
                        nargs - (bP
                                    ? (cls
                                            ? 0
                                            : (tmp.GetNClosure().nParameterChecks == 1
                                                ? 1
                                                : 0))
                                    : 0),
                        vm.StackBase + 2,
                        ref res,
                        cls))
                {
                    while (vm.StackTop >= top)
                    {
                        vm.Pop();
                    }
                    if (ret)
                    {
                        vm.Push(res);
                    }
                    vm.StackTop = top;
                    return true;
                }
            }
            else
            {
                if (vm.Call(ref tmp,
                        nargs - (bP
                                    ? ((tmp.GetNClosure().nParameterChecks == 1
                                                ? 1
                                                : 0))
                                    : 0),
                        vm.StackBase + 1, // vm._top - nargs + (cls ? (parsing ? -2 : 0)  : 0 ), //+ ,
                        ref res,
                        cls))
                {
                    vm.Pop(nargs - 1);
                    if (ret)
                    {
                        vm.Push(res);
                    }
                    return true;
                }
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
                    foreach (ExObject num in row.GetList())
                    {
                        if (!num.IsNumeric())
                        {
                            vm.AddToErrorMessage("given list have to contain lists of numeric values");
                            return false;
                        }
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

        /// <summary>
        /// Call garbage collector
        /// </summary>
        public static void CollectGarbage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
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
                Console.WriteLine("[TRACE] [LINE: " + pair[0] + ", INSTRUCTION ID: " + pair[1] + "] ");
            }
        }

        /// <summary>
        /// Write error messages depending on the error type
        /// </summary>
        /// <param name="vm">Virtual machine to use</param>
        /// <param name="typ">Type of error</param>
        public static void WriteErrorMessages(ExVM vm, ExErrorType typ)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("\n\n*******************************");
            switch (typ)
            {
                case ExErrorType.COMPILE:
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("COMPILER ERROR");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(vm.ErrorString);
                        break;
                    }
                case ExErrorType.RUNTIME:
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("RUNTIME ERROR");
                        Console.ForegroundColor = ConsoleColor.White;
                        WriteErrorTraces(vm);
                        Console.WriteLine(vm.ErrorString);
                        break;
                    }
            }

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("*******************************");
            Console.ResetColor();

            vm.ErrorString = string.Empty;
        }
    }
}
