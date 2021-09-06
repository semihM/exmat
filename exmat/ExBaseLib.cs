﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ExMat.API;
using ExMat.Class;
using ExMat.Exceptions;
using ExMat.Objects;
using ExMat.Utils;
using ExMat.VM;

namespace ExMat.BaseLib
{
    public static class ExBaseLib
    {
        // BASIC FUNCTIONS
        public static ExFunctionStatus StdHelp(ExVM vm, int nargs) //TO-DO Refactor
        {
            ExObject o = vm.GetArgument(1);
            bool print = nargs != 2 || vm.GetArgument(2).GetBool();
            switch (o.Type)
            {
                case ExObjType.STRING:
                    {
                        string name = o.GetString();
                        if (vm.RootDictionary.GetDict().ContainsKey(name))
                        {
                            ExObject found = vm.RootDictionary.GetDict()[name];
                            if (found.Type == ExObjType.NATIVECLOSURE)
                            {
                                if (print)
                                {
                                    vm.PrintLine(found.GetNClosure().Documentation);
                                }
                                return vm.CleanReturn(nargs + 2, found.GetNClosure().Documentation);
                            }
                            return vm.AddToErrorMessage(string.Format("Expected a native function. '{0}' is not a native function, it is type '{1}'!", name, found.Type.ToString()));
                        }
                        return vm.AddToErrorMessage(string.Format("Unknown native function: {0}. Perhaps it is a delegate? Try passing the function directly.", name));
                    }
                default:
                    {
                        if (print)
                        {
                            vm.PrintLine(o.GetNClosure().Documentation);
                        }
                        return vm.CleanReturn(nargs + 2, o.GetNClosure().Documentation);
                    }
            }
        }
        public static ExFunctionStatus StdRoot(ExVM vm, int nargs)
        {
            vm.Pop(nargs + 2);
            ExApi.PushRootTable(vm);
            return ExFunctionStatus.SUCCESS;
        }

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

        public static ExFunctionStatus StdToBase64(ExVM vm, int nargs)
        {
            string str = vm.GetArgument(1).GetString();
            Encoding enc = ExApi.DecideEncodingFromString(nargs > 1 ? vm.GetArgument(2).GetString() : "utf-8");
            return vm.CleanReturn(nargs + 2, Convert.ToBase64String(enc.GetBytes(str)));
        }

        public static ExFunctionStatus StdFromBase64(ExVM vm, int nargs)
        {
            string str = vm.GetArgument(1).GetString();
            Encoding enc = ExApi.DecideEncodingFromString(nargs > 1 ? vm.GetArgument(2).GetString() : "utf-8");
            return vm.CleanReturn(nargs + 2, enc.GetString(Convert.FromBase64String(str)));
        }

        public static ExFunctionStatus StdPrint(ExVM vm, int nargs)
        {
            if (!ExApi.ConvertAndGetString(vm, 1, nargs == 2 ? (int)vm.GetPositiveIntegerArgument(2, 1) : 2, out string output))
            {
                return ExFunctionStatus.ERROR;
            }

            vm.Print(output);   // Konsola çıktıyı yazdır
            return ExFunctionStatus.VOID;           // Dönülen değer yok ( boş değer )
        }
        public static ExFunctionStatus StdPrintl(ExVM vm, int nargs)
        {
            if (!ExApi.ConvertAndGetString(vm, 1, nargs == 2 ? (int)vm.GetPositiveIntegerArgument(2, 1) : 2, out string output))
            {
                return ExFunctionStatus.ERROR;
            }

            vm.PrintLine(output);
            return ExFunctionStatus.VOID;
        }

        public static ExFunctionStatus StdType(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, new ExObject(vm.GetArgument(1).Type.ToString()));
        }

        public static ExFunctionStatus StdTime(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, new ExObject((DateTime.Now - vm.StartingTime).TotalMilliseconds));
        }

        private static void GetDateFromStringArgument(bool shrt, string[] splt, List<ExObject> res)
        {
            DateTime now = DateTime.Now;
            DateTime today = DateTime.Today;
            DateTime utcnow = DateTime.UtcNow;

            foreach (string arg in splt)
            {
                switch (arg.ToLower())
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
                            res.Add(new ExObject(now.Year.ToString()));
                            break;
                        }
                    case "month":
                        {
                            res.Add(new ExObject(now.Month.ToString()));
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
                            res.Add(new ExObject(now.Day.ToString()));
                            break;
                        }
                    case "yday":
                        {
                            res.Add(new ExObject(now.DayOfYear.ToString()));
                            break;
                        }
                    case "hours":
                    case "hour":
                    case "hh":
                    case "h":
                        {
                            res.Add(new ExObject(now.Hour.ToString()));
                            break;
                        }
                    case "minutes":
                    case "minute":
                    case "min":
                    case "mm":
                    case "m":
                        {
                            res.Add(new ExObject(now.Minute.ToString()));
                            break;
                        }
                    case "seconds":
                    case "second":
                    case "sec":
                    case "ss":
                    case "s":
                        {
                            res.Add(new ExObject(now.Second.ToString()));
                            break;
                        }
                    case "miliseconds":
                    case "milisecond":
                    case "ms":
                        {
                            res.Add(new ExObject(now.Millisecond.ToString()));
                            break;
                        }
                    case "utc":
                        {
                            res.Add(new ExObject(shrt ? utcnow.ToShortDateString() : utcnow.ToLongDateString()));
                            res.Add(new ExObject(shrt ? utcnow.ToShortTimeString() : utcnow.ToLongTimeString()));
                            res.Add(new ExObject(utcnow.Millisecond.ToString()));
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
                            res.Add(new ExObject(utcnow.Month.ToString()));
                            break;
                        }
                    case "utc-month":
                        {
                            res.Add(new ExObject(utcnow.Month.ToString()));
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
                            res.Add(new ExObject(utcnow.Day.ToString()));
                            break;
                        }
                    case "utc-yday":
                        {
                            res.Add(new ExObject(utcnow.DayOfYear.ToString()));
                            break;
                        }
                    case "utc-h":
                    case "utc-hh":
                    case "utc-hour":
                    case "utc-hours":
                        {
                            res.Add(new ExObject(utcnow.Hour.ToString()));
                            break;
                        }
                }
            }
        }

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

                        if (res.Count == 1)
                        {
                            return vm.CleanReturn(nargs + 2, new ExObject(res[0]));
                        }
                        else
                        {
                            return vm.CleanReturn(nargs + 2, new ExObject(res));
                        }
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2,
                            new ExObject(new List<ExObject>()
                                {
                                    new(DateTime.Today.ToLongDateString()),
                                    new(DateTime.Now.ToLongTimeString()),
                                    new(DateTime.Now.Millisecond.ToString())
                                })
                            );
                    }
            }
        }

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

        // BASIC CLASS-LIKE FUNCTIONS
        public static ExFunctionStatus StdComplex(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 2:
                    {
                        ExObject o = vm.GetArgument(1);
                        if (o.Type == ExObjType.COMPLEX)
                        {
                            return vm.CleanReturn(4, new Complex(o.GetComplex().Real, vm.GetArgument(2).GetFloat()));
                        }
                        return vm.CleanReturn(4, new Complex(o.GetFloat(), vm.GetArgument(2).GetFloat()));
                    }
                case 1:
                    {
                        ExObject obj = vm.GetArgument(1);
                        if (obj.Type == ExObjType.COMPLEX)
                        {
                            return vm.CleanReturn(3, new Complex(obj.GetComplex().Real, obj.GetComplex().Imaginary));
                        }
                        else
                        {
                            return vm.CleanReturn(3, new Complex(obj.GetFloat(), 0));
                        }
                    }
                default:
                    {
                        return vm.CleanReturn(2, new Complex());
                    }
            }

        }

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

        public static ExFunctionStatus HandleCharArrayToString(ExVM vm, int nargs, ExObject obj)
        {
            if (obj.Type == ExObjType.ARRAY)
            {
                StringBuilder str = new(obj.GetList().Count);

                if (!ExApi.ConvertIntegerStringArrayToString(obj.GetList(), str))
                {
                    return vm.AddToErrorMessage("failed to create string, list must contain all positive integers within 'char' range or strings");
                }
                else
                {
                    return vm.CleanReturn(nargs + 2, str.ToString());
                }
            }
            else if (obj.Type == ExObjType.INTEGER)
            {
                long val = obj.GetInt();

                if (val < char.MinValue || val > char.MaxValue)
                {
                    return vm.AddToErrorMessage("integer out of range for char conversion");
                }

                return vm.CleanReturn(nargs + 2, ((char)val).ToString());
            }
            else
            {
                return vm.AddToErrorMessage("expected INTEGER or ARRAY when 2nd parameter of 'string' is true");
            }
        }

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
        public static ExFunctionStatus StdFloat(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 1:
                    {
                        if (!ExApi.ToFloatFromStack(vm, 2, 3))
                        {
                            return ExFunctionStatus.ERROR;
                        }

                        return ExFunctionStatus.SUCCESS;
                    }
                default:
                    {
                        return vm.CleanReturn(2, 0.0);
                    }
            }
        }
        public static ExFunctionStatus StdInteger(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 1:
                    {
                        if (!ExApi.ToIntegerFromStack(vm, 2, 3))
                        {
                            return ExFunctionStatus.ERROR;
                        }

                        return ExFunctionStatus.SUCCESS;
                    }
                default:
                    {
                        return vm.CleanReturn(2, 0);
                    }
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
                                        lis.Add(new(bit.ToString()));
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
                                    if (b > int.MaxValue || b < int.MinValue)
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
                                        lis.Add(new(bit.ToString()));
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
                                    char[] chars = b.ToString("X16").ToCharArray();
                                    List<ExObject> lis = new(chars.Length + (prefix ? 2 : 0));
                                    if (prefix)
                                    {
                                        lis.Add(new("0"));
                                        lis.Add(new("x"));
                                    }
                                    foreach (char i in chars)
                                    {
                                        lis.Add(new(i.ToString()));
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

            List<ExObject> l = new(obj.GetList().Count);

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
                                    else if (defs.IndexOf(o.GetInt().ToString()) != -1)  // TO-DO fix this mess
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
                                    else if (defs.IndexOf(o.GetInt().ToString()) != -1)  // TO-DO fix this mess
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

            if (obj2 != null)
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

        public static ExFunctionStatus StdParse(ExVM vm, int nargs)
        {
            ExObject cls = vm.GetArgument(1);
            List<ExObject> args = new ExObject(vm.GetArgument(2)).GetList();
            if (args.Count > vm.Stack.Allocated - vm.StackTop - 3)
            {
                vm.AddToErrorMessage("stack size is too small for parsing " + args.Count + " arguments! Current size: " + vm.Stack.Allocated);
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

        ///
        public static ExFunctionStatus StdList(ExVM vm, int nargs)
        {
            ExObject o = vm.GetArgument(1);
            if (o.Type == ExObjType.STRING)
            {
                char[] s = o.GetString().ToCharArray();

                List<ExObject> lis = new(s.Length);

                foreach (char c in s)
                {
                    lis.Add(new(c.ToString()));
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
                    l.Value.l_List = new(s);
                    ExUtils.InitList(ref l.Value.l_List, s, vm.GetArgument(2));
                }
                else
                {
                    l.Value.l_List = new(s);
                    ExUtils.InitList(ref l.Value.l_List, s);
                }

                return vm.CleanReturn(nargs + 2, l);
            }
        }

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
                                                                    l.Add(new(start + i * step));
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
                                                                    l.Add(new(start + i * step));
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
                                                                    l.Add(new(start + i * step));
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
                                                                    l.Add(new(start + i * step));
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
                                                                    l.Add(new(start + i * step));
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
                                                                    l.Add(new(start + i * step));
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
                                                                l.Add(new(start + i * step));
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.FLOAT:
                                                        {
                                                            double step = d.GetFloat();
                                                            for (int i = 0; i <= count; i++)
                                                            {
                                                                l.Add(new(start + i * step));
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.COMPLEX:
                                                        {
                                                            Complex step = d.GetComplex();
                                                            for (int i = 0; i <= count; i++)
                                                            {
                                                                l.Add(new(start + i * step));
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
                                                    l.Add(new(start + i * start));
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
                                                                    l.Add(new(start + i * step));
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
                                                                    l.Add(new(start + i * step));
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
                                                                    l.Add(new(start + i * step));
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
                                                                    l.Add(new(start + i * step));
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
                                                                    l.Add(new(start + i * step));
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
                                                                    l.Add(new(start + i * step));
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
                                                                l.Add(new(start + i * step));
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.FLOAT:
                                                        {
                                                            double step = d.GetFloat();
                                                            for (int i = 0; i < count; i++)
                                                            {
                                                                l.Add(new(start + i * step));
                                                            }
                                                            break;
                                                        }
                                                    case ExObjType.COMPLEX:
                                                        {
                                                            Complex step = d.GetComplex();
                                                            for (int i = 0; i < count; i++)
                                                            {
                                                                l.Add(new(start + i * step));
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
                                                    l.Add(new(start + i * start));
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
                        l.Value.l_List = new(m);

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
        // REAL NUMBERS
        public static ExFunctionStatus StdNumericReal(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetRootArgument().Type == ExObjType.INTEGER
                                                ? new ExObject(vm.GetRootArgument().GetInt())
                                                : new(vm.GetRootArgument().GetFloat()));
        }
        public static ExFunctionStatus StdNumericImage(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetRootArgument().Type == ExObjType.INTEGER
                                                ? new ExObject(0)
                                                : new ExObject(0.0));
        }

        // COMPLEX
        public static ExFunctionStatus StdComplexPhase(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, new ExObject(vm.GetRootArgument().GetComplex().Phase));
        }
        public static ExFunctionStatus StdComplexMagnitude(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, new ExObject(vm.GetRootArgument().GetComplex().Magnitude));
        }
        public static ExFunctionStatus StdComplexImg(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, new ExObject(vm.GetRootArgument().Value.c_Float));
        }
        public static ExFunctionStatus StdComplexReal(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, new ExObject(vm.GetRootArgument().Value.f_Float));
        }
        public static ExFunctionStatus StdComplexConjugate(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetRootArgument().GetComplexConj());
        }

        // COMMON
        public static ExFunctionStatus StdDefaultLength(ExVM vm, int nargs)
        {
            int size = -1;
            ExObject obj = vm.GetRootArgument();   // Objeyi al
            switch (obj.Type)
            {
                case ExObjType.ARRAY:
                    {
                        size = obj.GetList().Count;
                        break;
                    }
                case ExObjType.DICT:
                    {
                        size = obj.Value.d_Dict.Count;
                        break;
                    }
                case ExObjType.STRING:
                    {
                        size = obj.Value.s_String.Length;
                        break;
                    }
            }
            return vm.CleanReturn(nargs + 2, new ExObject(size));
        }

        // STRING
        public static ExFunctionStatus StdStringIndexOf(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (!ExApi.GetSafeObject(vm, -2, ExObjType.STRING, ref res))
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
            return vm.CleanReturn(1, res.GetString().IndexOf(vm.GetArgument(1).GetString()));
        }

        public static ExFunctionStatus StdStringToUpper(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetRootArgument().GetString().ToUpper());
        }
        public static ExFunctionStatus StdStringToLower(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetRootArgument().GetString().ToLower());
        }
        public static ExFunctionStatus StdStringReverse(ExVM vm, int nargs)
        {
            char[] ch = vm.GetRootArgument().GetString().ToCharArray();
            Array.Reverse(ch);
            return vm.CleanReturn(nargs + 2, new string(ch));
        }
        public static ExFunctionStatus StdStringReplace(ExVM vm, int nargs)
        {
            string obj = vm.GetRootArgument().GetString();
            return vm.CleanReturn(nargs + 2, obj.Replace(vm.GetArgument(1).GetString(), vm.GetArgument(2).GetString()));
        }

        public static ExFunctionStatus StdStringRepeat(ExVM vm, int nargs)
        {
            string obj = vm.GetRootArgument().GetString();
            int rep = (int)vm.GetPositiveIntegerArgument(1, 0);
            StringBuilder res = new();
            while (rep-- > 0)
            {
                res.Append(obj);
            }
            return vm.CleanReturn(nargs + 2, res.ToString());
        }

        private static ExFunctionStatus StringIndexCheck(ExVM vm, int n, ref string s)
        {
            if (n < 0 || n >= s.Length)
            {
                return vm.AddToErrorMessage("string can't be indexed with integer higher than it's length or negative");
            }
            s = s[n].ToString();
            return ExFunctionStatus.SUCCESS;
        }
        public static ExFunctionStatus StdStringAlphabetic(ExVM vm, int nargs)
        {
            string s = vm.GetRootArgument().GetString();
            if (nargs == 1
                && StringIndexCheck(vm, (int)vm.GetPositiveIntegerArgument(1, 0), ref s) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }
            return vm.CleanReturn(nargs + 2, Regex.IsMatch(s, "^[A-Za-z]+$"));
        }
        public static ExFunctionStatus StdStringNumeric(ExVM vm, int nargs)
        {
            string s = vm.GetRootArgument().GetString();
            if (nargs == 1
                && StringIndexCheck(vm, (int)vm.GetPositiveIntegerArgument(1, 0), ref s) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }
            return vm.CleanReturn(nargs + 2, Regex.IsMatch(s, @"^\d+(\.\d+)?((E|e)(\+|\-)\d+)?$"));
        }
        public static ExFunctionStatus StdStringAlphaNumeric(ExVM vm, int nargs)
        {
            string s = vm.GetRootArgument().GetString();
            if (nargs == 1
                && StringIndexCheck(vm, (int)vm.GetPositiveIntegerArgument(1, 0), ref s) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }
            return vm.CleanReturn(nargs + 2, Regex.IsMatch(s, "^[A-Za-z0-9]+$"));
        }
        public static ExFunctionStatus StdStringLower(ExVM vm, int nargs)
        {
            string s = vm.GetRootArgument().GetString();
            if (nargs == 1
                && StringIndexCheck(vm, (int)vm.GetPositiveIntegerArgument(1, 0), ref s) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }
            foreach (char c in s)
            {
                if (!char.IsLower(c))
                {
                    return vm.CleanReturn(nargs + 2, false);
                }
            }
            return vm.CleanReturn(nargs + 2, !string.IsNullOrEmpty(s));
        }
        public static ExFunctionStatus StdStringUpper(ExVM vm, int nargs)
        {
            string s = vm.GetRootArgument().GetString();
            if (nargs == 1
                && StringIndexCheck(vm, (int)vm.GetPositiveIntegerArgument(1, 0), ref s) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }
            foreach (char c in s)
            {
                if (!char.IsUpper(c))
                {
                    return vm.CleanReturn(nargs + 2, false);
                }
            }
            return vm.CleanReturn(nargs + 2, !string.IsNullOrEmpty(s));
        }
        public static ExFunctionStatus StdStringWhitespace(ExVM vm, int nargs)
        {
            string s = vm.GetRootArgument().GetString();
            if (nargs == 1
                && StringIndexCheck(vm, (int)vm.GetPositiveIntegerArgument(1, 0), ref s) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }
            foreach (char c in s)
            {
                if (!char.IsWhiteSpace(c))
                {
                    return vm.CleanReturn(nargs + 2, false);
                }
            }
            return vm.CleanReturn(nargs + 2, s.Length > 0);
        }
        public static ExFunctionStatus StdStringSymbol(ExVM vm, int nargs)
        {
            string s = vm.GetRootArgument().GetString();
            if (nargs == 1
                && StringIndexCheck(vm, (int)vm.GetPositiveIntegerArgument(1, 0), ref s) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }
            foreach (char c in s)
            {
                if (!char.IsSymbol(c))
                {
                    return vm.CleanReturn(nargs + 2, false);
                }
            }
            return vm.CleanReturn(nargs + 2, !string.IsNullOrEmpty(s));
        }
        public static ExFunctionStatus StdStringSlice(ExVM vm, int nargs)
        {
            ExObject o = new();
            if (!ExApi.GetSafeObject(vm, -1 - nargs, ExObjType.STRING, ref o))
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }

            int start = (int)vm.GetArgument(1).GetInt();

            char[] arr = o.GetString().ToCharArray();
            char[] res = null;

            int n = arr.Length;
            bool filled = false;

            switch (nargs)
            {
                case 1:
                    {
                        if (start < 0)
                        {
                            start += n;
                        }
                        if (start > n || start < 0)
                        {
                            vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                            return ExFunctionStatus.ERROR;
                        }

                        filled = true;
                        res = new char[start];

                        for (int i = 0; i < start; i++)
                        {
                            res[i] = arr[i];
                        }
                        break;
                    }
                case 2:
                    {
                        int end = (int)vm.GetArgument(2).GetInt();
                        if (start < 0)
                        {
                            start += n;
                        }
                        if (start >= n || start < 0)
                        {
                            vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                            return ExFunctionStatus.ERROR;
                        }

                        if (end < 0)
                        {
                            end += n;
                        }
                        if (end > n || end < 0)
                        {
                            vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                            return ExFunctionStatus.ERROR;
                        }

                        if (start >= end)
                        {
                            break;
                        }

                        filled = true;
                        res = new char[end - start];

                        for (int i = start, j = 0; i < end; i++, j++)
                        {
                            res[j] = arr[i];
                        }
                        break;
                    }
            }

            return vm.CleanReturn(nargs + 2, filled ? new string(res) : string.Empty);
        }

        // ARRAY
        public static ExFunctionStatus StdArrayAppend(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (ExApi.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res))
            {
                res = new(ExUtils.GetACopyOf(res.GetList()));
                res.GetList().Add(new(vm.GetArgument(1)));
                return vm.CleanReturn(nargs + 2, res);
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }
        public static ExFunctionStatus StdArrayRemoveAt(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (ExApi.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res))
            {
                int liscount = res.GetList().Count;
                if (liscount == 0)
                {
                    return vm.AddToErrorMessage("can't remove from an empty list");
                }

                int remove_idx = (int)vm.GetPositiveRangedIntegerArgument(1, 0, liscount - 1);

                res = new(ExUtils.GetACopyOf(res.GetList()));
                res.GetList().RemoveAt(remove_idx);

                return vm.CleanReturn(nargs + 2, res);
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }
        public static ExFunctionStatus StdArrayExtend(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (ExApi.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res))
            {
                res.GetList().AddRange(vm.GetArgument(1).GetList());
                return vm.CleanReturn(nargs + 2, res);
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }
        public static ExFunctionStatus StdArrayExpand(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (ExApi.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res))
            {
                res = new(ExUtils.GetACopyOf(res.GetList()));
                res.GetList().AddRange(vm.GetArgument(1).GetList());
                return vm.CleanReturn(nargs + 2, res);
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }
        public static ExFunctionStatus StdArrayPush(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (ExApi.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res))
            {
                res.GetList().Add(new(vm.GetArgument(1)));
                return vm.CleanReturn(nargs + 2, res);
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }
        public static ExFunctionStatus StdArrayPop(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (ExApi.GetSafeObject(vm, -1, ExObjType.ARRAY, ref res))
            {
                int liscount = res.GetList().Count;
                if (liscount == 0)
                {
                    return vm.AddToErrorMessage("can't pop from empty list");
                }

                int popcount = nargs == 1 ? (int)vm.GetPositiveRangedIntegerArgument(1, 0, liscount) : 1;
                for (int i = 0; i < popcount; i++)
                {
                    res.GetList().RemoveAt(liscount - 1 - i);
                }
                return vm.CleanReturn(nargs + 2, res);
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }

        public static ExFunctionStatus StdArrayResize(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (!ExApi.GetSafeObject(vm, -1, ExObjType.ARRAY, ref res))
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }

            int newsize = (int)vm.GetPositiveIntegerArgument(1, 0);

            int curr = res.GetList().Count;
            ExObject filler = nargs == 2 ? vm.GetArgument(2) : null;

            if (curr > 0 && newsize > 0)
            {
                if (newsize >= curr)
                {
                    ExUtils.AppendFillerNTimes(res.GetList(), filler, newsize - curr);
                }
                else
                {
                    while (curr != newsize)
                    {
                        res.GetList()[curr - 1].Nullify();
                        res.GetList().RemoveAt(curr - 1);
                        curr--;
                    }
                }
            }
            else if (newsize > 0)
            {
                res.Value.l_List = new(newsize);
                ExUtils.AppendFillerNTimes(res.GetList(), filler, newsize);
            }
            else
            {
                res.Value.l_List = new();
            }

            return vm.CleanReturn(nargs + 2, res);
        }

        public static ExFunctionStatus StdArrayIndexOf(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (ExApi.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res))
            {
                return vm.CleanReturn(nargs + 2, ExApi.GetValueIndexFromArray(res.GetList(), vm.GetArgument(1)));
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }

        public static ExFunctionStatus StdArrayCount(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (ExApi.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res))
            {
                using ExObject obj = new(vm.GetArgument(1));

                int i = ExApi.CountValueEqualsInArray(res.GetList(), obj);
                return vm.CleanReturn(nargs + 2, i);
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }

        public static ExFunctionStatus StdArraySlice(ExVM vm, int nargs)
        {
            ExObject o = new();
            if (ExApi.GetSafeObject(vm, -1 - nargs, ExObjType.ARRAY, ref o))
            {
                int start = (int)vm.GetArgument(1).GetInt();

                List<ExObject> arr = o.GetList();
                List<ExObject> res = new(0);

                int n = arr.Count;

                switch (nargs)
                {
                    case 1:
                        {
                            if (start < 0)
                            {
                                start += n;
                            }
                            if (start > n || start < 0)
                            {
                                vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                                return ExFunctionStatus.ERROR;
                            }

                            res = new(start);

                            for (int i = 0; i < start; i++)
                            {
                                res.Add(new(arr[i]));
                            }
                            break;
                        }
                    case 2:
                        {
                            int end = (int)vm.GetArgument(2).GetInt();
                            if (start < 0)
                            {
                                start += n;
                            }
                            if (start > n || start < 0)
                            {
                                vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                                return ExFunctionStatus.ERROR;
                            }

                            if (end < 0)
                            {
                                end += n;
                            }
                            if (end > n || end < 0)
                            {
                                vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                                return ExFunctionStatus.ERROR;
                            }

                            if (start >= end)
                            {
                                break;
                            }

                            res = new(end - start);

                            for (int i = start; i < end; i++)
                            {
                                res.Add(new(arr[i]));
                            }

                            break;
                        }
                }
                return vm.CleanReturn(nargs + 2, res);
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }

        public static ExFunctionStatus StdArrayShuffle(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, ExUtils.ShuffleList(vm.GetRootArgument().GetList()));
        }

        public static ExFunctionStatus StdArrayRandom(ExVM vm, int nargs)
        {
            List<ExObject> lis = vm.GetRootArgument().GetList();
            int count = nargs == 1 ? (int)vm.GetPositiveIntegerArgument(1, 1) : 1;

            if (count > lis.Count)
            {
                return vm.AddToErrorMessage(string.Format("can't pick {0} values from list with length {1}", count, lis.Count));
            }
            else if (count == lis.Count)
            {
                return vm.CleanReturn(nargs + 2, ExUtils.ShuffleList(lis));
            }
            else if (count == 1)
            {
                return vm.CleanReturn(nargs + 2, new ExObject(lis[new Random().Next(lis.Count)]));
            }
            else
            {
                return vm.CleanReturn(nargs + 2, ExUtils.GetNRandomObjectsFrom(lis, count));
            }
        }

        public static ExFunctionStatus StdArrayReverse(ExVM vm, int nargs)
        {
            ExObject obj = vm.GetRootArgument();
            List<ExObject> lis = obj.GetList();
            List<ExObject> res = new(lis.Count);
            for (int i = lis.Count - 1; i >= 0; i--)
            {
                res.Add(new(lis[i]));
            }
            return vm.CleanReturn(nargs + 2, res);
        }

        public static ExFunctionStatus StdArrayCopy(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, ExUtils.GetACopyOf(vm.GetRootArgument().GetList()));
        }

        public static ExFunctionStatus StdArrayTranspose(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (ExApi.GetSafeObject(vm, -1, ExObjType.ARRAY, ref res))
            {
                List<ExObject> vals = res.GetList();
                int rows = vals.Count;
                int cols = 0;

                if (!ExApi.DoMatrixTransposeChecks(vm, vals, ref cols))
                {
                    return ExFunctionStatus.ERROR;
                }

                List<ExObject> lis = ExApi.TransposeMatrix(rows, cols, vals);

                return vm.CleanReturn(nargs + 2, lis);
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }

        // DICT
        public static ExFunctionStatus StdDictHasKey(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (ExApi.GetSafeObject(vm, -2, ExObjType.DICT, ref res))
            {
                return vm.CleanReturn(nargs + 2, res.GetDict().ContainsKey(vm.GetArgument(1).GetString()));
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }
        public static ExFunctionStatus StdDictKeys(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (ExApi.GetSafeObject(vm, -1, ExObjType.DICT, ref res))
            {
                List<ExObject> keys = new(res.GetDict().Count);
                foreach (string key in res.GetDict().Keys)
                {
                    keys.Add(new(key));
                }
                return vm.CleanReturn(nargs + 2, keys);
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }

        public static ExFunctionStatus StdDictValues(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (ExApi.GetSafeObject(vm, -1, ExObjType.DICT, ref res))
            {
                List<ExObject> vals = new(res.GetDict().Count);
                foreach (ExObject val in res.GetDict().Values)
                {
                    vals.Add(new(val));
                }
                return vm.CleanReturn(nargs + 2, vals);
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }

        public static ExFunctionStatus StdDictRandomKey(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (ExApi.GetSafeObject(vm, -1, ExObjType.DICT, ref res))
            {
                return vm.CleanReturn(nargs + 2, new List<string>(res.GetDict().Keys)[new Random().Next(0, res.GetDict().Count)]);
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }

        public static ExFunctionStatus StdDictRandomVal(ExVM vm, int nargs)
        {
            ExObject res = new();
            if (ExApi.GetSafeObject(vm, -1, ExObjType.DICT, ref res))
            {
                return vm.CleanReturn(nargs + 2, new List<ExObject>(res.GetDict().Values)[new Random().Next(0, res.GetDict().Count)]);
            }
            else
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
        }
        // CLASS
        public static ExFunctionStatus StdClassHasAttr(ExVM vm, int nargs)
        {
            ExObject res = new();

            if (!ExApi.GetSafeObject(vm, -3, ExObjType.CLASS, ref res))
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
            string mem = vm.GetArgument(1).GetString();
            string attr = vm.GetArgument(2).GetString();

            ExClass cls = res.Value._Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }
                return vm.CleanReturn(nargs + 2, false);
            }

            return vm.AddToErrorMessage("unknown member or method '" + mem + "'");
        }
        public static ExFunctionStatus StdClassGetAttr(ExVM vm, int nargs)
        {
            ExObject res = new();

            if (!ExApi.GetSafeObject(vm, -3, ExObjType.CLASS, ref res))
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
            string mem = vm.GetArgument(1).GetString();
            string attr = vm.GetArgument(2).GetString();

            ExClass cls = res.Value._Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls.DefaultValues[v.GetMemberID()].Attributes.GetDict()[attr]);
                        return vm.CleanReturn(nargs + 2, val);
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls.Methods[v.GetMemberID()].Attributes.GetDict()[attr]);
                        return vm.CleanReturn(nargs + 2, val);
                    }
                }
                return vm.AddToErrorMessage("unknown attribute '" + attr + "'");
            }

            return vm.AddToErrorMessage("unknown member or method '" + mem + "'");
        }

        public static ExFunctionStatus StdClassSetAttr(ExVM vm, int nargs)
        {
            ExObject res = new();

            if (!ExApi.GetSafeObject(vm, -4, ExObjType.CLASS, ref res))
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
            string mem = vm.GetArgument(1).GetString();
            string attr = vm.GetArgument(2).GetString();
            ExObject val = vm.GetArgument(3);

            ExClass cls = res.Value._Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        cls.DefaultValues[v.GetMemberID()].Attributes.GetDict()[attr].Assign(val);
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        cls.Methods[v.GetMemberID()].Attributes.GetDict()[attr].Assign(val);
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }
                return vm.AddToErrorMessage("unknown attribute '" + attr + "'");
            }

            return vm.AddToErrorMessage("unknown member or method '" + mem + "'");
        }

        // INSTANCE
        public static ExFunctionStatus StdInstanceHasAttr(ExVM vm, int nargs)
        {
            ExObject res = new();

            if (!ExApi.GetSafeObject(vm, -3, ExObjType.INSTANCE, ref res))
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
            string mem = vm.GetArgument(1).GetString();
            string attr = vm.GetArgument(2).GetString();

            ExClass cls = res.GetInstance().Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }
                return vm.CleanReturn(nargs + 2, false);
            }

            return vm.AddToErrorMessage("unknown member or method '" + mem + "'");
        }
        public static ExFunctionStatus StdInstanceGetAttr(ExVM vm, int nargs)
        {
            ExObject res = new();

            if (!ExApi.GetSafeObject(vm, -3, ExObjType.INSTANCE, ref res))
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
            string mem = vm.GetArgument(1).GetString();
            string attr = vm.GetArgument(2).GetString();

            ExClass cls = res.GetInstance().Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls.DefaultValues[v.GetMemberID()].Attributes.GetDict()[attr]);
                        return vm.CleanReturn(nargs + 2, val);
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls.Methods[v.GetMemberID()].Attributes.GetDict()[attr]);
                        return vm.CleanReturn(nargs + 2, val);
                    }
                }
                return vm.AddToErrorMessage("unknown attribute '" + attr + "'");
            }

            return vm.AddToErrorMessage("unknown member or method '" + mem + "'");
        }

        public static ExFunctionStatus StdInstanceSetAttr(ExVM vm, int nargs)
        {
            ExObject res = new();

            if (!ExApi.GetSafeObject(vm, -4, ExObjType.INSTANCE, ref res))
            {
                return vm.AddToErrorMessage("stack is corrupted!");
            }
            string mem = vm.GetArgument(1).GetString();
            string attr = vm.GetArgument(2).GetString();
            ExObject val = vm.GetArgument(3);
            ExClass cls = res.GetInstance().Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        cls.DefaultValues[v.GetMemberID()].Attributes.GetDict()[attr].Assign(val);
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        cls.Methods[v.GetMemberID()].Attributes.GetDict()[attr].Assign(val);
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }

                return vm.AddToErrorMessage("unknown attribute '" + attr + "'");
            }

            return vm.AddToErrorMessage("unknown member or method '" + mem + "'");
        }
        //
        public static ExFunctionStatus StdReloadBase(ExVM vm, int nargs)
        {
            if (nargs == 1)
            {
                string name = vm.GetArgument(1).GetString();
                if (name == ExMat.ReloadBaseLib)
                {
                    return vm.CleanReturn(nargs + 2, vm.RootDictionary);
                }

                ExApi.PushRootTable(vm);
                if (!ExApi.ReloadNativeFunction(vm, BaseFuncs, name, ExStdLibType.BASE, true))
                {
                    return vm.AddToErrorMessage(string.Format("unknown Std function {0}", name));
                }

            }
            else
            {
                if (!RegisterStdBase(vm))
                {
                    return vm.AddToErrorMessage("something went wrong...");
                }
            }
            return vm.CleanReturn(nargs + 2, vm.RootDictionary);
        }

        public static ExFunctionStatus StdExit(ExVM vm, int nargs)
        {
            vm.CleanReturn(nargs + 2, vm.GetArgument(1).GetInt());
            return ExFunctionStatus.EXIT;
        }

        public static ExFunctionStatus StdInteractive(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.IsInteractive);
        }

        public static ExFunctionStatus StdGCCollect(ExVM vm, int nargs)
        {
            vm.Pop(2);
            ExApi.CollectGarbage();
            return ExFunctionStatus.VOID;
        }

        public static ExFunctionStatus StdWeakRef(ExVM vm, int nargs)
        {
            ExObject ret = vm.GetRootArgument();
            if (ret.IsCountingRefs())
            {
                vm.Push(ret.Value._RefC.GetWeakRef(ret.Type, ret.Value));
                return ExFunctionStatus.SUCCESS;
            }
            vm.Push(ret);
            return ExFunctionStatus.SUCCESS;
        }

        public static ExFunctionStatus StdWeakRefValue(ExVM vm, int nargs)
        {
            ExObject ret = vm.GetRootArgument();
            if (ret.Type != ExObjType.WEAKREF)
            {
                return vm.AddToErrorMessage("can't get reference value of non-weakref object");
            }

            vm.Push(ret.Value._WeakRef.ReferencedObject);
            return ExFunctionStatus.SUCCESS;
        }

        public static MethodInfo GetBaseLibMethod(string name)
        {
            return Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod(name);
        }
        //
        private static readonly List<ExRegFunc> _exStdFuncs = new()
        {
            new()
            {
                Name = "help",
                Function = StdHelp,
                Parameters = new()
                {
                    new("func_or_name", "s|Y", "Native function itself or the name of the native function", new("help")),
                    new("print", ".", "Wheter to print the information", new(true))
                },
                Returns = ExBaseType.STRING,
                Description = "Get and print the built-in help information of a native function. Printing can be disabled with the 2nd parameter."
            },
            new()
            {
                Name = "root",
                Function = StdRoot,
                Returns = ExBaseType.DICT,
                Parameters = new(),
                Safe = true,
                Description = "Get the root table."
            },
            new()
            {
                Name = "sleep",
                Function = StdSleep,
                Parameters = new()
                {
                    new("miliseconds", "r")
                },
                Returns = ExBaseType.BOOL,
                Safe = true,
                Description = "Sleeps main thread given amount of time. Returns 'true' when thread wakes up."
            },
            new()
            {
                Name = "to_base64",
                Function = StdToBase64,
                Parameters = new()
                {
                    new("source", "s", "Source string to convert"),
                    new("encoding", "s", "Encoding of the 'source'", new("utf-8"))
                },
                Returns = ExBaseType.STRING,
                Safe = true,
                Description = "Convert given string to it's base64 representation."
            },
            new()
            {
                Name = "from_base64",
                Function = StdFromBase64,
                Parameters = new()
                {
                    new("source", "s", "Source string to convert"),
                    new("encoding", "s", "Encoding of the 'source'", new("utf-8"))
                },
                Returns = ExBaseType.STRING,
                Safe = true,
                Description = "Convert given string from it's base64 representation to original."
            },
            new()
            {
                Name = "print",
                Function = StdPrint,
                Parameters = new()
                {
                    new("message", ".", "Message or object"),
                    new("depth", "n", "Printing depth for objects, 1 = Object's string representation, 2 = Stringify first level objects inside lists/dictionaries, ...", new(2))
                },
                Description = "Print messages or objects to immediate console."
            },

            new()
            {
                Name = "printl",
                Function = StdPrintl,
                Parameters = new()
                {
                    new("message", ".", "Message or object to print to immediate console"),
                    new("depth", "n", "Printing depth for objects, 1 = Object's string representation, 2 = Stringify first level objects inside lists/dictionaries, ...", new(2))
                },
                Description = "Print messages or objects to immediate console with a new line '\\n' at the end"
            },

            new()
            {
                Name = "time",
                Function = StdTime,
                Returns = ExBaseType.FLOAT,
                Parameters = new(),
                Safe = true,
                Description = "Get how long the VM has been alive in miliseconds."
            },
            new()
            {
                Name = "date",
                Function = StdDate,
                Parameters = new()
                {
                    new("type", "s", "Type(s) of date. Use '|' to combine and get a list. Available types:" + string.Join("\n\t\t", ExMat.DateTypeInfoString.Split('\n')), new("today")),
                    new("short_format", ".", "Get the shorter format of date values", new(false))
                },
                Returns = ExBaseType.STRING | ExBaseType.ARRAY,
                Safe = true,
                Description = "Get current date information."
            },

            new()
            {
                Name = "type",
                Function = StdType,
                Parameters = new()
                {
                    new("object", ".", "Object to get the type of")
                },
                Returns = ExBaseType.STRING,
                Safe = true,
                Description = "Get the type of an object as a string."
            },

            new()
            {
                Name = "assert",
                Function = StdAssert,
                Parameters = new()
                {
                    new("condition", ".", "Condition to check"),
                    new("message", "s", "Assertion error message", new(string.Empty))
                },
                Description = "Assert given 'condition' is true as bool. Throws assertion error with given message if 'condition' is false as bool"
            },

            new()
            {
                Name = "string",
                Function = StdString,
                Parameters = new()
                {
                    new("object", ".", "Condition to check", new(string.Empty)),
                    new("is_char_array", ".", "Assertion error message", new(false)),
                    new("depth", "n", "Depth for objects to stringify", new(2))
                },
                Returns = ExBaseType.STRING,
                Description = "Convert given object to a string. Allows character/integer lists to string conversion via 'is_char_array'."
            },
            new()
            {
                Name = "complex",
                Function = StdComplex,
                Parameters = new()
                {
                    new("real_part", "n", "Real part of the number", new(0.0)),
                    new("img_part", "n", "Imaginary part of the number", new(0.0))
                },
                Returns = ExBaseType.COMPLEX,
                Safe = true,
                Description = "Create a complex number with given real and imaginary parts"
            },
            new()
            {
                Name = "complex2",
                Function = StdComplex2,
                Parameters = new()
                {
                    new("magnitute", "r", "Magnitute of the number", new(0.0)),
                    new("phase", "r", "Phase of the number", new(0.0))
                },
                Returns = ExBaseType.COMPLEX,
                Safe = true,
                Description = "Create a complex number with given magnitute and phase"
            },
            new()
            {
                Name = "float",
                Function = StdFloat,
                Parameters = new()
                {
                    new("object", ".", "Object to parse as float", new(0.0))
                },
                Returns = ExBaseType.FLOAT,
                Description = "Parse given object as a float"
            },
            new()
            {
                Name = "integer",
                Function = StdInteger,
                Parameters = new()
                {
                    new("object", ".", "Object to parse as integer", new(0))
                },
                Returns = ExBaseType.INTEGER,
                Description = "Parse given object as an integer"
            },
            new()
            {
                Name = "bool",
                Function = StdBool,
                Parameters = new()
                {
                    new("object", ".", "Object to bool value of", new(true))
                },
                Returns = ExBaseType.BOOL,
                Safe = true,
                Description = "Get bool value of given object"
            },
            new()
            {
                Name = "bits",
                Function = StdBits,
                Parameters = new()
                {
                    new("number", "r", "64bit number", new(0)),
                    new("reverse", ".", "Reverse bit order", new(false))
                },
                Returns = ExBaseType.ARRAY,
                Safe = true,
                Description = "Get the 64bit representation of a real number"
            },
            new()
            {
                Name = "bits32",
                Function = StdBits32,
                Parameters = new()
                {
                    new("number", "r", "32bit number", new(0)),
                    new("reverse", ".", "Reverse bit order", new(false))
                },
                Returns = ExBaseType.ARRAY,
                Description = "Get the 32bit representation of a real number"
            },
            new()
            {
                Name = "bytes",
                Function = StdBytes,
                Parameters = new()
                {
                    new("object", "r|s", "64bit number or string to get bytes of", new(0)),
                    new("reverse", ".", "Reverse byte order", new(false))
                },
                Returns = ExBaseType.ARRAY,
                Description = "Get the 8 byte representation of a real number or all bytes of a string in a list"
            },
            new()
            {
                Name = "hex",
                Function = StdHex,
                Parameters = new()
                {
                    new("number", "r", "64bit number", new(0)),
                    new("add_0x_prefix", ".", "Wheter to add '0' and 'x' as first 2 elements of the list", new(true))
                },
                Returns = ExBaseType.ARRAY,
                Safe = true,
                Description = "Get a list of characters which represent given 64bit number in hexadecimal format"
            },
            new()
            {
                Name = "binary",
                Function = StdBinary,
                Parameters = new()
                {
                    new("number", "r", "64bit number", new(0)),
                    new("add_0B_prefix", ".", "Wheter to add '0' and 'B' as first 2 elements of the list", new(true))
                },
                Returns = ExBaseType.ARRAY,
                Safe = true,
                Description = "Get a list of zeros and ones which represent given 64bit number in binary format"
            },
            new()
            {
                Name = "binary32",
                Function = StdBinary32,
                Parameters = new()
                {
                    new("number", "r", "32bit number", new(0)),
                    new("add_0b_prefix", ".", "Wheter to add '0' and 'b' as first 2 elements of the list", new(true))
                },
                Returns = ExBaseType.ARRAY,
                Description = "Get a list of zeros and ones which represent given 32bit number in binary format"
            },

            new()
            {
                Name = "list",
                Function = StdList,
                Parameters = new()
                {
                    new("length", "n|s", "Length of the list or string to get the character list of", new(0)),
                    new("filler", ".", "Filler object to use while initializing the list", new())
                },
                Returns = ExBaseType.ARRAY,
                Safe = true,
                Description = "Initialize an empty list or convert a string into a list of characters"
            },
            new()
            {
                Name = "range",
                Function = StdRange,
                Parameters = new()
                {
                    new("start", "n", "Inclusive start value"),
                    new("end", "n", "Exclusive end value", new(0)),
                    new("step", "n", "Step size between values", new(1))
                },
                Returns = ExBaseType.ARRAY,
                Description = "Initialize a number range series with given inclusive start and exclusive end values and step information."
            },
            new()
            {
                Name = "rangei",
                Function = StdRangei,
                Parameters = new()
                {
                    new("start", "n", "Inclusive start value"),
                    new("end", "n", "Inclusive end value", new(0)),
                    new("step", "n", "Step size between values", new(1))
                },
                Returns = ExBaseType.ARRAY,
                Description = "Initialize a number range series with given inclusive start and end values and step information."
            },
            new()
            {
                Name = "matrix",
                Function = StdMatrix,
                Parameters = new()
                {
                    new("rows", "i", "Row dimension"),
                    new("cols", "i", "Column dimension", new(1)),
                    new("filler", ".", "Filler object", new())
                },
                Returns = ExBaseType.ARRAY,
                Description = "Initialize a matrix with 'rows'x'cols' dimensions."
            },

            new()
            {
                Name = "map",
                Function = StdMap,
                Parameters = new()
                {
                    new("func", "c|y", "Mapping function, single parameter (2 parameters if 'alt_list' is used)"),
                    new("list", "a", "List to iterate over"),
                    new("alt_list", "a", "A secondary optional list to iterate with 'list'.", new ExList())
                },
                Returns = ExBaseType.ARRAY,
                Description = "Map a list to a new list with a function. A secondary list can be given to iterate over simultaneously to pass a 2nd parameter to mapping function"
            },
            new()
            {
                Name = "filter",
                Function = StdFilter,
                Parameters = new()
                {
                    new("func", "c", "Filter function, single parameter"),
                    new("list", "a", "List to iterate over")
                },
                Returns = ExBaseType.ARRAY,
                Description = "Filter a list with a filtering function and return a new list"
            },
            new()
            {
                Name = "call",
                Function = StdCall,
                NumberOfParameters = -2,
                Returns = -1,
                Description = "Call the function in the first argument with other arguments passed to it"
            },
            new()
            {
                Name = "parse",
                Function = StdParse,
                Parameters = new()
                {
                    new("func_or_class", "c|y", "Function or class for parsing array of arguments to"),
                    new("list", "a", "Array of arguments for the function or class")
                },
                Returns = -1,
                Description = "Parse a list of arguments to a function or a class"
            },
            new()
            {
                Name = "iter",
                Function = StdIter,
                Parameters = new()
                {
                    new("func_or_class", "c|y", "Function or class with 3 parameters: (<int> iteration, <var> previous_value,  <var> current_value). Returned value becomes previous_value for the next iteration"),
                    new("list", "a", "List to iterate through"),
                    new("start_value", ".", "This value is used as the 'previous_value' for the first iteration")
                },
                Returns = -1,
                Description = "Iterate through a list and reduce with given function. Each iteration gets the returned value from the function on previous iteration."
            },
            new()
            {
                Name = "first",
                Function = StdFirst,
                Parameters = new()
                {
                    new("func", "c", "Condition to be met. Single parameter function, gets passed list elements to it"),
                    new("list", "a", "List to iterate through")
                },
                Returns = -1,
                Description = "Find the first value meeting the given condition in a list"
            },
            new()
            {
                Name = "any",
                Function = StdAny,
                Parameters = new()
                {
                    new("func", "c", "Condition to be met. Single parameter function, gets passed list elements to it"),
                    new("list", "a", "List to iterate through")
                },
                Returns = ExBaseType.BOOL,
                Description = "Check if any element in a list meets the given condition"
            },
            new()
            {
                Name = "all",
                Function = StdAll,
                Parameters = new()
                {
                    new("func", "c", "Condition to be met. Single parameter function, gets passed list elements to it"),
                    new("list", "a", "List to iterate through")
                },
                Returns = ExBaseType.BOOL,
                Description = "Check if all elements of a list meet the given condition"
            },

            new()
            {
                Name = "exit",
                Function = StdExit,
                Parameters = new()
                {
                    new("func", "r", "Condition to be met. Single parameter function, gets passed list elements to it", new(0))
                },
                Safe = true,
                Description = "Exits the virtual machine at the next possible oppurtunity"
            },
            new()
            {
                Name = "is_interactive",
                Function = StdInteractive,
                Returns = ExBaseType.BOOL,
                Parameters = new(),
                Safe = true,
                Description = "Check if current script is running inside an interactive console"
            },
            new()
            {
                Name = "collect_garbage",
                Function = StdGCCollect,
                Safe = true,
                Description = "Force run the garbage collector"
            },

            new()
            {
                Name = ExMat.ReloadBaseLib,
                Function = StdReloadBase,
                Parameters = new()
                {
                    new("func_name", "s", "Name of the base library function to reload", new(ExMat.ReloadBaseLib))
                },
                Returns = ExBaseType.DICT
            }
        };

        public static List<ExRegFunc> BaseFuncs => _exStdFuncs;

        public static void RegisterStdBaseConstants(ExVM vm)
        {
            // Global tabloyu sanal belleğe ata
            ExApi.PushRootTable(vm);

            // Sabit değerleri tabloya ekle
            ExApi.CreateConstantInt(vm, "_versionnumber_", ExMat.VersionNumber);
            ExApi.CreateConstantString(vm, "_version_", ExMat.Version);

            ExApi.CreateConstantString(vm, "_config_",
#if DEBUG
                "DEBUG"
#else     
                "RELEASE"
#endif
            );

            ExApi.CreateConstantDict(vm, "COLORS", new()
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
                { "DARKCYAN", new("darkcyan") },
            });
            // Kayıtları yaptıktan sonra global tabloyu bellekten kaldır
            vm.Pop(1);
        }

        public static bool RegisterStdBase(ExVM vm)
        {
            // Yerli fonksiyonları global tabloya kaydet
            ExApi.RegisterNativeFunctions(vm, BaseFuncs, ExStdLibType.BASE);

            RegisterStdBaseConstants(vm);

            return true;
        }
    }
}
