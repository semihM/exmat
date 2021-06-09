using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using ExMat.API;
using ExMat.Class;
using ExMat.Closure;
using ExMat.FuncPrototype;
using ExMat.Objects;
using ExMat.Utils;
using ExMat.VM;

namespace ExMat.BaseLib
{
    public static class ExBaseLib
    {
        // BASIC FUNCTIONS
        public static int StdPrint(ExVM vm, int nargs)
        {
            int maxdepth = 2;
            if (nargs == 2)
            {
                maxdepth = (int)ExAPI.GetFromStack(vm, 3).GetInt();
                maxdepth = maxdepth < 1 ? 1 : maxdepth;
            }
            string s = string.Empty;
            ExAPI.ToString(vm, 2, maxdepth);
            if (!ExAPI.GetString(vm, -1, ref s))
            {
                return -1;
            }

            vm.Print(s);
            return 0;
        }
        public static int StdPrintl(ExVM vm, int nargs)
        {
            int maxdepth = 2;
            if (nargs == 2)
            {
                maxdepth = (int)ExAPI.GetFromStack(vm, 3).GetInt();
                maxdepth = maxdepth < 1 ? 1 : maxdepth;
            }

            string s = string.Empty;
            ExAPI.ToString(vm, 2, maxdepth, 0, true);
            if (!ExAPI.GetString(vm, -1, ref s))
            {
                return -1;
            }

            vm.PrintLine(s);
            return 0;
        }

        public static int StdType(ExVM vm, int nargs)
        {
            ExObject o = ExAPI.GetFromStack(vm, 2);
            string t = o.Type.ToString();
            vm.Pop(nargs + 2);
            vm.Push(new ExObject(t));
            return 1;
        }

        public static int StdTime(ExVM vm, int nargs)
        {
            vm.Pop(nargs + 2);
            vm.Push(new ExObject((double)(DateTime.Now - vm.StartingTime).TotalMilliseconds));
            return 1;
        }

        public static int StdDate(ExVM vm, int nargs)
        {
            bool shrt = false;
            DateTime now = DateTime.Now;
            DateTime today = DateTime.Today;
            DateTime utcnow = DateTime.UtcNow;

            switch (nargs)
            {
                case 2:
                    {
                        shrt = ExAPI.GetFromStack(vm, 3).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        string[] splt = ExAPI.GetFromStack(vm, 2).GetString().Split("|", StringSplitOptions.RemoveEmptyEntries);
                        List<ExObject> res = new(splt.Length);
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
                        if (res.Count == 1)
                        {
                            vm.Pop(nargs + 2);
                            vm.Push(new ExObject(res[0]));
                        }
                        else
                        {
                            vm.Pop(nargs + 2);
                            vm.Push(new ExObject(res));
                        }
                        break;
                    }
                default:
                    {
                        vm.Pop(nargs + 2);
                        vm.Push(new ExObject(new List<ExObject>() { new(today.ToLongDateString()), new(now.ToLongTimeString()), new(now.Millisecond.ToString()) }));
                        break;
                    }
            }
            return 1;
        }

        public static int StdAssert(ExVM vm, int nargs)
        {
            if (nargs == 2)
            {
                if (ExAPI.GetFromStack(vm, 2).GetBool())
                {
                    vm.Pop(4);
                    return 0;
                }
                else
                {
                    string m = ExAPI.GetFromStack(vm, 3).GetString();
                    vm.Pop(4);
                    vm.AddToErrorMessage("ASSERT FAILED: " + m);
                    return -1;
                }
            }
            bool b = ExAPI.GetFromStack(vm, 2).GetBool();
            vm.Pop(3);
            vm.AddToErrorMessage("ASSERT FAILED!");
            return b ? 0 : -1;
        }

        // BASIC CLASS-LIKE FUNCTIONS
        public static int StdComplex(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 2:
                    {
                        ExObject o = ExAPI.GetFromStack(vm, 2);
                        if (o.Type == ExObjType.COMPLEX)
                        {
                            vm.AddToErrorMessage("can't use complex number as real part");
                            return -1;
                        }
                        double real = o.GetFloat();
                        double img = ExAPI.GetFromStack(vm, 3).GetFloat();
                        vm.Pop(4);
                        vm.Push(new Complex(real, img));
                        break;
                    }
                case 1:
                    {
                        ExObject obj = ExAPI.GetFromStack(vm, 2);
                        Complex c;
                        if (obj.Type == ExObjType.COMPLEX)
                        {
                            c = new(obj.GetComplex().Real, obj.GetComplex().Imaginary);
                        }
                        else
                        {
                            c = new(obj.GetFloat(), 0);
                        }
                        vm.Pop(3);
                        vm.Push(c);
                        break;
                    }
                case 0:
                    {
                        vm.Pop(2);
                        vm.Push(new Complex());
                        break;
                    }
            }

            return 1;
        }

        public static int StdComplex2(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 2:
                    {
                        double mag = ExAPI.GetFromStack(vm, 2).GetFloat();
                        double phase = ExAPI.GetFromStack(vm, 3).GetFloat();
                        vm.Pop(4);
                        vm.Push(Complex.FromPolarCoordinates(mag, phase));
                        break;
                    }
                case 1:
                    {
                        Complex c = Complex.FromPolarCoordinates(ExAPI.GetFromStack(vm, 2).GetFloat(), 0);
                        vm.Pop(3);
                        vm.Push(c);
                        break;
                    }
                case 0:
                    {
                        vm.Pop(2);
                        vm.Push(new Complex());
                        break;
                    }
            }

            return 1;
        }
        public static int StdBool(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 1:
                    {
                        bool b = ExAPI.GetFromStack(vm, 2).GetBool();
                        vm.Pop(3);
                        vm.Push(b);
                        break;
                    }
                case 0:
                    {
                        vm.Pop(2);
                        vm.Push(true);
                        break;
                    }
            }

            return 1;
        }

        public static int StdString(ExVM vm, int nargs)
        {
            bool carr = false;
            int depth = 2;
            switch (nargs)
            {
                case 3:
                    {
                        depth = (int)ExAPI.GetFromStack(vm, 4).GetInt();
                        goto case 2;
                    }
                case 2:
                    {
                        carr = ExAPI.GetFromStack(vm, 3).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        ExObject obj = ExAPI.GetFromStack(vm, 2);
                        if (carr)
                        {
                            if (obj.Type == ExObjType.ARRAY)
                            {
                                string str = string.Empty;
                                foreach (ExObject o in obj.GetList())
                                {
                                    if (o.Type == ExObjType.STRING) // && o.GetString().Length == 1)
                                    {
                                        str += o.GetString();
                                    }
                                    else if (o.Type == ExObjType.INTEGER && o.GetInt() >= 0)
                                    {
                                        str += (char)o.GetInt();
                                    }
                                    else
                                    {
                                        vm.AddToErrorMessage("failed to create string, list must contain all positive integers or strings");
                                        return -1;
                                    }
                                }

                                vm.Pop(nargs + 2);
                                vm.Push(str);
                                break;
                            }
                            else if (obj.Type == ExObjType.INTEGER)
                            {
                                long val = obj.GetInt();
                                if (val < char.MinValue || val > char.MaxValue)
                                {
                                    vm.AddToErrorMessage("integer out of range for char conversion");
                                    return -1;
                                }

                                vm.Pop(nargs + 2);
                                vm.Push(((char)val).ToString());
                            }
                        }
                        else if (!ExAPI.ToString(vm, 2, depth, nargs + 2))
                        {
                            return -1;
                        }
                        break;
                    }
                case 0:
                    {
                        vm.Pop(2);
                        vm.Push(string.Empty);
                        break;
                    }
            }

            return 1;
        }
        public static int StdFloat(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 1:
                    {
                        if (!ExAPI.ToFloat(vm, 2, 3))
                        {
                            return -1;
                        }
                        break;
                    }
                case 0:
                    {
                        vm.Pop(2);
                        vm.Push(0.0);
                        break;
                    }
            }

            return 1;
        }
        public static int StdInteger(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 1:
                    {
                        if (!ExAPI.ToInteger(vm, 2, 3))
                        {
                            return -1;
                        }
                        break;
                    }
                case 0:
                    {
                        vm.Pop(2);
                        vm.Push(0);
                        break;
                    }
            }

            return 1;
        }
        public static int StdBits32(ExVM vm, int nargs)
        {
            bool reverse = true;
            switch (nargs)
            {
                case 2:
                    {
                        reverse = !ExAPI.GetFromStack(vm, 3).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        ExObject v = ExAPI.GetFromStack(vm, 2);
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
                                    if (b >= int.MaxValue || b <= int.MinValue)
                                    {
                                        vm.AddToErrorMessage("64bit '" + v.Type.ToString() + "' out of range for 32bit use");
                                        return -1;
                                    }
                                    b = (int)b;

                                    List<ExObject> l = new(32);

                                    for (int i = 0; i < 32; i++)
                                    {
                                        l.Add(new((b >> i) % 2 == 0 ? 0 : 1));
                                    }

                                    if (reverse)
                                    {
                                        l.Reverse();
                                    }

                                    vm.Pop(nargs + 2);
                                    vm.Push(l);
                                    break;
                                }
                        }
                        break;
                    }
                case 0:
                    {
                        vm.Pop(2);
                        vm.Push(new ExList());
                        break;
                    }
            }

            return 1;
        }

        public static int StdBits(ExVM vm, int nargs)
        {
            bool reverse = true;
            switch (nargs)
            {
                case 2:
                    {
                        reverse = !ExAPI.GetFromStack(vm, 3).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        ExObject v = ExAPI.GetFromStack(vm, 2);
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
                                    List<ExObject> l = new(64);

                                    for (int i = 0; i < 64; i++)
                                    {
                                        l.Add(new((b >> i) % 2 == 0 ? 0 : 1));
                                    }

                                    if (reverse)
                                    {
                                        l.Reverse();
                                    }

                                    vm.Pop(nargs + 2);
                                    vm.Push(l);
                                    break;
                                }
                        }
                        break;
                    }
                case 0:
                    {
                        vm.Pop(2);
                        vm.Push(new ExList());
                        break;
                    }
            }

            return 1;
        }

        public static int StdBytes(ExVM vm, int nargs)
        {
            bool reverse = true;
            switch (nargs)
            {
                case 2:
                    {
                        reverse = !ExAPI.GetFromStack(vm, 3).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        ExObject v = ExAPI.GetFromStack(vm, 2);
                        byte[] bytes = null;
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
                                    vm.Pop(nargs + 2);
                                    vm.Push(b);
                                    break;
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
                                    vm.Pop(nargs + 2);
                                    vm.Push(b);
                                    break;
                                }
                        }
                        break;
                    }
                case 0:
                    {
                        vm.Pop(2);
                        vm.Push(new ExList());
                        break;
                    }
            }

            return 1;
        }

        public static int StdMap(ExVM vm, int nargs)
        {
            ExObject cls = ExAPI.GetFromStack(vm, 2);
            ExObject obj = new(ExAPI.GetFromStack(vm, 3));
            ExObject obj2 = nargs == 3 ? new(ExAPI.GetFromStack(vm, 4)) : null;

            vm.Pop(nargs - 1);

            ExObject res = new();
            ExObject tmp = new();

            int n = 2;
            int m = 0;

            int argcount = obj.Value.l_List.Count;

            List<ExObject> l = new(obj.Value.l_List.Count);

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
                                    vm.AddToErrorMessage("expected same length for both arrays while mapping");
                                    return -1;
                                }
                                n++;

                                for (int i = 0; i < argcount; i++)
                                {
                                    ExObject o = obj.Value.l_List[i];
                                    ExObject o2 = obj2.Value.l_List[i];
                                    vm.Push(cls);
                                    vm.Push(vm.RootDictionary);

                                    vm.Push(o);
                                    vm.Push(o2);
                                    if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                                    {
                                        vm.Pop();
                                        vm.IsMainCall = _bm;
                                        return -1;
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
                                    ExObject o = obj.Value.l_List[i];
                                    vm.Push(cls);
                                    vm.Push(vm.RootDictionary);

                                    vm.Push(o);
                                    if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                                    {
                                        vm.Pop();
                                        vm.IsMainCall = _bm;
                                        return -1;
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
                            vm.Pop(n + m + 1);
                            vm.Push(new ExObject(l));
                            return 1;
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
                    vm.AddToErrorMessage("expected same length for both arrays while mapping");
                    return -1;
                }
                n++;

                for (int i = 0; i < obj.Value.l_List.Count; i++)
                {
                    ExObject o = obj.Value.l_List[i];
                    ExObject o2 = obj2.Value.l_List[i];
                    vm.Push(cls);
                    vm.Push(vm.RootDictionary);

                    vm.Push(o);
                    vm.Push(o2);
                    if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                    {
                        vm.Pop();
                        vm.IsMainCall = bm;
                        return -1;
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
                foreach (ExObject o in obj.Value.l_List)
                {
                    vm.Push(cls);
                    vm.Push(vm.RootDictionary);

                    vm.Push(o);
                    if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                    {
                        vm.Pop();
                        vm.IsMainCall = bm;
                        return -1;
                    }
                    else
                    {
                        vm.Pop(n + 1 + m);
                        l.Add(new(tmp));
                    }
                }
            }

            vm.IsMainCall = bm;
            vm.Pop(n + m + 1);
            vm.Push(new ExObject(l));
            return 1;
        }

        public static int StdFilter(ExVM vm, int nargs)
        {
            ExObject cls = ExAPI.GetFromStack(vm, 2);
            ExObject obj = new(ExAPI.GetFromStack(vm, 3));
            List<ExObject> l = new(obj.Value.l_List.Count);

            vm.Pop();

            ExObject res = new();

            bool iscls = cls.Type == ExObjType.CLOSURE;

            if (!iscls && cls.Type != ExObjType.NATIVECLOSURE)
            {
                vm.AddToErrorMessage("can't call non-closure type");
                return -1;
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
            foreach (ExObject o in obj.Value.l_List)
            {
                vm.Push(cls);
                vm.Push(vm.RootDictionary);

                vm.Push(o);
                if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                {
                    vm.Pop();
                    vm.IsMainCall = bm;
                    return -1;
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
            vm.Pop(n + m + 1);
            vm.Push(new ExObject(l));
            return 1;
        }

        public static int StdCall(ExVM vm, int nargs)
        {
            ExObject cls = ExAPI.GetFromStack(vm, 2);

            ExObject res = new();

            bool need_b = false;
            bool iscls = cls.Type == ExObjType.CLOSURE;

            if (!iscls && cls.Type != ExObjType.NATIVECLOSURE)
            {
                vm.AddToErrorMessage("can't call non-closure type");
                return -1;
            }

            List<ExObject> args = new();


            if (iscls)
            {
                ExPrototype pro = cls.GetClosure().Function;

                if (pro.nParams == 1)
                {
                    if (nargs != 1)
                    {
                        vm.AddToErrorMessage("expected 0 arguments");
                        return -1;
                    }
                    need_b = true;
                }
                else
                {
                    int p = pro.nParams;
                    need_b = p > 1;

                    if (need_b)
                    {
                        int nn = nargs - 1;
                        while (nn > 0)
                        {
                            args.Add(new(vm.GetAbove(-1)));
                            vm.Pop();
                            nn--;
                        }
                    }

                    if (need_b
                        && pro.nDefaultParameters > 0)
                    {
                        int n_def = pro.nDefaultParameters;
                        int diff;
                        if (n_def > 0 && nargs < p && (diff = p - nargs) <= n_def)
                        {
                            for (int n = n_def - diff; n < n_def; n++)
                            {
                                args.Add(cls.GetClosure().DefaultParams[n]);
                            }
                        }
                    }
                    else if (pro.nParams != nargs)
                    {
                        vm.AddToErrorMessage("wrong number of arguments");
                        return -1;
                    }
                }
            }
            else
            {
                if (!DecideCallNeedNC(vm, cls.GetNClosure(), nargs, ref need_b))
                {
                    return -1;
                }
            }

            if (iscls)  //TO-DO fix this mess
            {
                vm.Push(vm.GetAbove(-2));
                args.Reverse();
                vm.PushParse(args);
                nargs = args.Count + 1;
            }

            bool bm = vm.IsMainCall;
            vm.IsMainCall = false;
            if (ExAPI.Call(vm, 3, true, need_b, iscls, nargs))
            {
                res.Assign(vm.GetAbove(-1)); // ExAPI.GetFromStack(vm, nargs - (iscls ? 1 : 0))
                vm.Pop();
            }
            else
            {
                vm.IsMainCall = bm;
                return -1;
            }

            vm.IsMainCall = bm;
            vm.Pop(3);
            vm.Push(res);
            return 1;
        }

        private static bool DecideCallNeedNC(ExVM v, ExNativeClosure c, int n, ref bool b)
        {
            if (c.IsDelegateFunction)
            {
                b = c.nParameterChecks == 1;
            }
            else if (c.nParameterChecks > 0)
            {
                if (c.nParameterChecks == 1)
                {
                    if (n != 1)
                    {
                        v.AddToErrorMessage("expected 0 arguments");
                        return false;
                    }
                    b = n == 2;
                }
                else
                {
                    b = c.nParameterChecks == 2;
                }
            }
            else
            {
                b = c.nParameterChecks == -1;
            }

            return true;
        }

        public static int StdParse(ExVM vm, int nargs)
        {
            ExObject cls = ExAPI.GetFromStack(vm, 2);
            List<ExObject> args = new ExObject(ExAPI.GetFromStack(vm, 3)).Value.l_List;
            if (args.Count > vm.Stack.Count - vm.StackTop - 3)
            {
                vm.AddToErrorMessage("stack size is too small for parsing " + args.Count + " arguments! Current size: " + vm.Stack.Count);
                return -1;
            }

            vm.Pop();

            ExObject res = new();
            int n = args.Count + 1;
            int extra = 0;
            switch (cls.Type)
            {
                case ExObjType.CLOSURE:
                    {
                        vm.Push(cls);
                        vm.Push(vm.RootDictionary);
                        if (cls.GetClosure().Function.IsCluster()
                            && cls.GetClosure().DefaultParams.Count == 1)
                        {
                            vm.Push(args); // Handle 1 parameter clusters => [args]
                            n = 2;
                        }
                        else
                        {
                            vm.PushParse(args);
                        }
                        extra++;
                        break;
                    }
                case ExObjType.NATIVECLOSURE:
                    {
                        vm.Push(cls);
                        if (args.Count == 0)
                        {
                            vm.Push(new ExObject());
                        }
                        vm.PushParse(args);
                        break;
                    }
                case ExObjType.CLASS:
                    {
                        vm.Push(cls);
                        vm.Push(vm.RootDictionary);
                        vm.PushParse(args);
                        extra++;
                        break;
                    }
                default:
                    {
                        vm.AddToErrorMessage("can't call '" + cls.Type + "' type");
                        return -1;
                    }
            }

            ExObject tmp = new();
            bool bm = vm.IsMainCall;
            vm.IsMainCall = false;
            if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
            {
                vm.Pop(n + extra);
                vm.IsMainCall = bm;
                return -1;
            }

            vm.IsMainCall = bm;
            vm.Pop(n + 3 + extra);
            vm.Push(tmp);
            return 1;
        }

        public static int StdIter(ExVM vm, int nargs)
        {
            ExObject cls = ExAPI.GetFromStack(vm, 2);
            ExObject obj = new(ExAPI.GetFromStack(vm, 3));
            ExObject prev = new(ExAPI.GetFromStack(vm, 4));

            vm.Pop();

            bool iscls = cls.Type == ExObjType.CLOSURE;

            if (!iscls && cls.Type != ExObjType.NATIVECLOSURE)
            {
                vm.AddToErrorMessage("can't call non-closure type");
                return -1;
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
            foreach (ExObject o in obj.Value.l_List) // TO-DO use for loop, remove need of 3rd arg in iter
            {
                vm.Push(cls);
                vm.Push(vm.RootDictionary);
                vm.Push(o); // curr
                vm.Push(prev);  // prev
                vm.Push(i); // idx
                if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                {
                    vm.Pop();
                    vm.IsMainCall = bm;
                    return -1;
                }
                else
                {
                    vm.Pop(n + 1 + m);
                    prev.Assign(tmp);
                    i++;
                }
            }

            vm.IsMainCall = bm;
            vm.Pop(n + m);
            vm.Push(prev);
            return 1;
        }

        public static int StdFirst(ExVM vm, int nargs)
        {
            ExObject cls = ExAPI.GetFromStack(vm, 2);
            ExObject obj = new(ExAPI.GetFromStack(vm, 3));
            ExObject res = new();

            vm.Pop();

            bool iscls = cls.Type == ExObjType.CLOSURE;

            if (!iscls && cls.Type != ExObjType.NATIVECLOSURE)
            {
                vm.AddToErrorMessage("can't call non-closure type");
                return -1;
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
            foreach (ExObject o in obj.Value.l_List)
            {
                vm.Push(cls);
                vm.Push(vm.RootDictionary);

                vm.Push(o);
                if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                {
                    vm.Pop();
                    vm.IsMainCall = bm;
                    return -1;
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
            vm.Pop(n + m + 1);
            vm.Push(res);
            return 1;
        }

        public static int StdAll(ExVM vm, int nargs)
        {
            ExObject cls = ExAPI.GetFromStack(vm, 2);
            ExObject obj = new(ExAPI.GetFromStack(vm, 3));

            vm.Pop();

            bool iscls = cls.Type == ExObjType.CLOSURE;

            if (!iscls && cls.Type != ExObjType.NATIVECLOSURE)
            {
                vm.AddToErrorMessage("can't call non-closure type");
                return -1;
            }

            ExObject tmp = new();

            int n = 2;
            int m = 0;
            if (!iscls && cls.GetNClosure().IsDelegateFunction)
            {
                n--;
                m++;
            }

            bool found = obj.Value.l_List.Count > 0;
            ExObject res = new(found);

            bool bm = vm.IsMainCall;
            vm.IsMainCall = false;
            foreach (ExObject o in obj.Value.l_List)
            {
                vm.Push(cls);
                vm.Push(vm.RootDictionary);

                vm.Push(o);
                if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                {
                    vm.Pop();
                    vm.IsMainCall = bm;
                    return -1;
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
            vm.Pop(n + m + 1);
            vm.Push(res);
            return 1;
        }

        public static int StdAny(ExVM vm, int nargs)
        {
            ExObject cls = ExAPI.GetFromStack(vm, 2);
            ExObject obj = new(ExAPI.GetFromStack(vm, 3));

            vm.Pop();

            bool iscls = cls.Type == ExObjType.CLOSURE;

            if (!iscls && cls.Type != ExObjType.NATIVECLOSURE)
            {
                vm.AddToErrorMessage("can't call non-closure type");
                return -1;
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
            foreach (ExObject o in obj.Value.l_List)
            {
                vm.Push(cls);
                vm.Push(vm.RootDictionary);

                vm.Push(o);
                if (!vm.Call(ref cls, n, vm.StackTop - n, ref tmp, true))
                {
                    vm.Pop();
                    vm.IsMainCall = bm;
                    return -1;
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
            vm.Pop(n + m + 1);
            vm.Push(res);
            return 1;
        }

        ///
        public static int StdList(ExVM vm, int nargs)
        {
            ExObject o = ExAPI.GetFromStack(vm, 2);
            if (o.Type == ExObjType.STRING)
            {
                char[] s = o.GetString().ToCharArray();

                List<ExObject> lis = new(s.Length);

                foreach (char c in s)
                {
                    lis.Add(new(c.ToString()));
                }

                vm.Pop(nargs + 2);
                vm.Push(lis);
                return 1;
            }
            else
            {
                ExList l = new();
                int s = (int)o.GetInt();
                if (s < 0)
                {
                    s = 0;
                }
                if (ExAPI.GetTopOfStack(vm) > 2)
                {
                    l.Value.l_List = new(s);
                    ExUtils.InitList(ref l.Value.l_List, s, ExAPI.GetFromStack(vm, 3));
                }
                else
                {
                    l.Value.l_List = new(s);
                    ExUtils.InitList(ref l.Value.l_List, s);
                }
                vm.Pop(nargs + 2);
                vm.Push(l);
                return 1;
            }
        }

        public static int StdRangei(ExVM vm, int nargs)
        {
            List<ExObject> l = new();
            ExObject s = ExAPI.GetFromStack(vm, 2);

            switch (s.Type)
            {
                case ExObjType.INTEGER:
                    {
                        long start = s.GetInt();
                        switch (nargs)
                        {
                            case 3:
                                {
                                    ExObject e = ExAPI.GetFromStack(vm, 3);
                                    ExObject d = ExAPI.GetFromStack(vm, 4);
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
                                                                    vm.AddToErrorMessage("can't use real number 'start' and 'end' range with 0 real valued complex number 'step'");
                                                                    return -1;
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
                                                vm.AddToErrorMessage("can't create range from real number to complex");
                                                return -1;
                                            }
                                    }
                                    break;
                                }

                            case 2:
                                {
                                    ExObject e = ExAPI.GetFromStack(vm, 3);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                double end = e.GetFloat();
                                                if (end > start)
                                                {
                                                    int count = (int)((end - start));

                                                    for (int i = 0; i <= count; i++)
                                                    {
                                                        l.Add(new(start + i));
                                                    }
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                vm.AddToErrorMessage("can't create range from real number to complex");
                                                return -1;
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
                                    ExObject e = ExAPI.GetFromStack(vm, 3);
                                    ExObject d = ExAPI.GetFromStack(vm, 4);
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
                                                                    vm.AddToErrorMessage("can't use real number 'start' and 'end' range with 0 real valued complex number 'step'");
                                                                    return -1;
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
                                                vm.AddToErrorMessage("can't create range from real number to complex");
                                                return -1;
                                            }
                                    }
                                    break;
                                }

                            case 2:
                                {
                                    ExObject e = ExAPI.GetFromStack(vm, 3);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                double end = e.GetFloat();
                                                if (end > start)
                                                {
                                                    int count = (int)((end - start));

                                                    for (int i = 0; i <= count; i++)
                                                    {
                                                        l.Add(new(start + i));
                                                    }
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                vm.AddToErrorMessage("can't create range from real number to complex");
                                                return -1;
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
                                    ExObject e = ExAPI.GetFromStack(vm, 3);
                                    ExObject d = ExAPI.GetFromStack(vm, 4);
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
                                                            long step = d.GetInt();
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
                                                vm.AddToErrorMessage("expected integer as 2nd argument for complex number range(start, count, step)");
                                                return -1;
                                            }
                                    }
                                    break;
                                }

                            case 2:
                                {
                                    ExObject e = ExAPI.GetFromStack(vm, 3);
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
                                                vm.AddToErrorMessage("expected integer as 2nd argument for complex number range(start, count)");
                                                return -1;
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
            vm.Pop(nargs + 2);
            vm.Push(l);
            return 1;
        }

        public static int StdRange(ExVM vm, int nargs)
        {
            List<ExObject> l = new();
            ExObject s = ExAPI.GetFromStack(vm, 2);
            switch (s.Type)
            {
                case ExObjType.INTEGER:
                    {
                        long start = s.GetInt();
                        switch (nargs)
                        {
                            case 3:
                                {
                                    ExObject e = ExAPI.GetFromStack(vm, 3);
                                    ExObject d = ExAPI.GetFromStack(vm, 4);
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
                                                                    vm.AddToErrorMessage("can't use real number 'start' and 'end' range with 0 real valued complex number 'step'");
                                                                    return -1;
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
                                                vm.AddToErrorMessage("can't create range from real number to complex");
                                                return -1;
                                            }
                                    }
                                    break;
                                }

                            case 2:
                                {
                                    ExObject e = ExAPI.GetFromStack(vm, 3);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                double end = e.GetFloat();
                                                if (end > start)
                                                {
                                                    int count = (int)((end - start));

                                                    for (int i = 0; i < count; i++)
                                                    {
                                                        l.Add(new(start + i));
                                                    }
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                vm.AddToErrorMessage("can't create range from real number to complex");
                                                return -1;
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
                                    ExObject e = ExAPI.GetFromStack(vm, 3);
                                    ExObject d = ExAPI.GetFromStack(vm, 4);
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
                                                                    vm.AddToErrorMessage("can't use real number 'start' and 'end' range with 0 real valued complex number 'step'");
                                                                    return -1;
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
                                                vm.AddToErrorMessage("can't create range from real number to complex");
                                                return -1;
                                            }
                                    }
                                    break;
                                }

                            case 2:
                                {
                                    ExObject e = ExAPI.GetFromStack(vm, 3);
                                    switch (e.Type)
                                    {
                                        case ExObjType.FLOAT:
                                        case ExObjType.INTEGER:
                                            {
                                                double end = e.GetFloat();
                                                if (end > start)
                                                {
                                                    int count = (int)((end - start));

                                                    for (int i = 0; i < count; i++)
                                                    {
                                                        l.Add(new(start + i));
                                                    }
                                                }
                                                break;
                                            }
                                        case ExObjType.COMPLEX:
                                            {
                                                vm.AddToErrorMessage("can't create range from real number to complex");
                                                return -1;
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
                                    ExObject e = ExAPI.GetFromStack(vm, 3);
                                    ExObject d = ExAPI.GetFromStack(vm, 4);
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
                                                            long step = d.GetInt();
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
                                                vm.AddToErrorMessage("expected integer as 2nd argument for complex number range(start, count, step)");
                                                return -1;
                                            }
                                    }
                                    break;
                                }

                            case 2:
                                {
                                    ExObject e = ExAPI.GetFromStack(vm, 3);
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
                                                vm.AddToErrorMessage("expected integer as 2nd argument for complex number range(start, count)");
                                                return -1;
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

            vm.Pop(nargs + 2);
            vm.Push(l);
            return 1;
        }

        public static int StdMatrix(ExVM vm, int nargs)
        {
            ExList l = new();
            ExObject s = ExAPI.GetFromStack(vm, 2);

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

                        int n = (int)ExAPI.GetFromStack(vm, 3).GetInt();
                        if (n < 0)
                        {
                            n = 0;
                        }

                        ExObject filler = nargs == 3 ? ExAPI.GetFromStack(vm, 4) : new();
                        l.Value.l_List = new(m);

                        switch (filler.Type)
                        {
                            case ExObjType.CLOSURE:
                                {
                                    if (filler.GetClosure().Function.nParams != 3
                                        && (filler.GetClosure().Function.nParams - filler.GetClosure().Function.nDefaultParameters) > 3)
                                    {
                                        vm.AddToErrorMessage("given function must allow 2-argument calls");
                                        return -1;
                                    }

                                    bool bm = vm.IsMainCall;
                                    vm.IsMainCall = false;
                                    for (int i = 0; i < m; i++)
                                    {
                                        List<ExObject> lis = new(n);
                                        for (int j = 0; j < n; j++)
                                        {
                                            ExObject res = new();
                                            vm.Push(vm.RootDictionary);
                                            vm.Push(i);
                                            vm.Push(j);
                                            if (!vm.Call(ref filler, 3, vm.StackTop - 3, ref res, true))
                                            {
                                                vm.IsMainCall = bm;
                                                return -1;
                                            }
                                            vm.Pop(3);

                                            lis.Add(new(res));
                                        }
                                        l.Value.l_List.Add(new ExObject(lis));
                                    }

                                    vm.IsMainCall = bm;
                                    break;
                                }
                            case ExObjType.NATIVECLOSURE:
                                {
                                    int nparamscheck = filler.GetNClosure().nParameterChecks;
                                    if (((nparamscheck > 0) && (nparamscheck != 3)) ||
                                        ((nparamscheck < 0) && (3 < (-nparamscheck))))
                                    {
                                        if (nparamscheck < 0)
                                        {
                                            vm.AddToErrorMessage("'" + filler.GetNClosure().Name.GetString() + "' takes minimum " + (-nparamscheck - 1) + " arguments");
                                            return -1;
                                        }
                                        vm.AddToErrorMessage("'" + filler.GetNClosure().Name.GetString() + "' takes exactly " + (nparamscheck - 1) + " arguments");
                                        return -1;
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
                                                return -1;
                                            }
                                            vm.Pop(2);

                                            lis.Add(new(res));
                                        }
                                        l.Value.l_List.Add(new ExObject(lis));
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
                                        l.Value.l_List.Add(new ExObject(lis));
                                    }
                                    break;
                                }
                        }
                        break;
                    }
            }
            vm.Pop(nargs + 2);
            vm.Push(l);
            return 1;
        }
        // COMPLEX
        public static int StdComplexPhase(ExVM vm, int nargs)
        {
            double size = ExAPI.GetFromStack(vm, 1).GetComplex().Phase;
            vm.Pop(nargs + 2);
            vm.Push(new ExObject(size));
            return 1;
        }
        public static int StdComplexMagnitude(ExVM vm, int nargs)
        {
            double size = ExAPI.GetFromStack(vm, 1).GetComplex().Magnitude;
            vm.Pop(nargs + 2);
            vm.Push(new ExObject(size));
            return 1;
        }
        public static int StdComplexImg(ExVM vm, int nargs)
        {
            double size = ExAPI.GetFromStack(vm, 1).Value.c_Float;
            vm.Pop(nargs + 2);
            vm.Push(new ExObject(size));
            return 1;
        }
        public static int StdComplexReal(ExVM vm, int nargs)
        {
            double size = ExAPI.GetFromStack(vm, 1).Value.f_Float;
            vm.Pop(nargs + 2);
            vm.Push(new ExObject(size));
            return 1;
        }
        public static int StdComplexConjugate(ExVM vm, int nargs)
        {
            Complex c = ExAPI.GetFromStack(vm, 1).GetComplexConj();
            vm.Pop(nargs + 2);
            vm.Push(c);
            return 1;
        }

        // COMMON
        public static int StdDefaultLength(ExVM vm, int nargs)
        {
            int size = -1;
            ExObject obj = ExAPI.GetFromStack(vm, 1);
            switch (obj.Type)
            {
                case ExObjType.ARRAY:
                    {
                        size = obj.Value.l_List.Count;
                        break;
                    }
                case ExObjType.DICT:
                    {
                        size = obj.Value.d_Dict.Count;
                        break;
                    }
                case ExObjType.STRING:
                    {
                        size = obj.GetString().Length;
                        break;
                    }
                case ExObjType.CLASS:
                    {
                        size = obj.Value._Class.LengthReprestation;
                        break;
                    }
                case ExObjType.INSTANCE:
                    {
                        size = obj.Value._Instance.Class.LengthReprestation;
                        break;
                    }
                default:
                    {
                        break; // TO-DO
                    }
            }
            vm.Pop(nargs + 2);
            vm.Push(new ExObject(size));
            return 1;
        }

        // STRING
        public static int StdStringIndexOf(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.STRING, ref res);
            string sub = vm.GetAbove(-1).GetString();
            string s = res.GetString();
            vm.Pop();
            vm.Push(res.GetString().IndexOf(sub));
            return 1;
        }

        public static int StdStringToUpper(ExVM vm, int nargs)
        {
            string obj = ExAPI.GetFromStack(vm, 1).GetString();
            vm.Pop(nargs + 2);
            vm.Push(obj.ToUpper());
            return 1;
        }
        public static int StdStringToLower(ExVM vm, int nargs)
        {
            string obj = ExAPI.GetFromStack(vm, 1).GetString();
            vm.Pop(nargs + 2);
            vm.Push(obj.ToLower());
            return 1;
        }
        public static int StdStringReverse(ExVM vm, int nargs)
        {
            string obj = ExAPI.GetFromStack(vm, 1).GetString();
            char[] ch = obj.ToCharArray();
            Array.Reverse(ch);
            vm.Pop(nargs + 2);
            vm.Push(new string(ch));
            return 1;
        }
        public static int StdStringReplace(ExVM vm, int nargs)
        {
            string obj = ExAPI.GetFromStack(vm, 1).GetString();
            string old = ExAPI.GetFromStack(vm, 2).GetString();
            string rep = ExAPI.GetFromStack(vm, 3).GetString();
            vm.Pop(nargs + 2);
            vm.Push(obj.Replace(old, rep));
            return 1;
        }

        public static int StdStringRepeat(ExVM vm, int nargs)
        {
            string obj = ExAPI.GetFromStack(vm, 1).GetString();
            int rep = (int)ExAPI.GetFromStack(vm, 2).GetInt();
            string res = string.Empty;
            while (rep > 0)
            {
                res += obj;
                rep--;
            }
            vm.Pop(nargs + 2);
            vm.Push(res);
            return 1;
        }

        public static int StdStringAlphabetic(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 1).GetString();
            if (nargs == 1)
            {
                int n = (int)ExAPI.GetFromStack(vm, 2).GetInt();
                if (n < 0 || n >= s.Length)
                {
                    vm.AddToErrorMessage("string can't be indexed with integer higher than it's length or negative");
                    return -1;
                }
                s = s[n].ToString();
            }
            vm.Pop(nargs + 2);
            vm.Push(Regex.IsMatch(s, "^[A-Za-z]+$"));
            return 1;
        }
        public static int StdStringNumeric(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 1).GetString();
            if (nargs == 1)
            {
                int n = (int)ExAPI.GetFromStack(vm, 2).GetInt();
                if (n < 0 || n >= s.Length)
                {
                    vm.AddToErrorMessage("string can't be indexed with integer higher than it's length or negative");
                    return -1;
                }
                s = s[n].ToString();
            }
            vm.Pop(nargs + 2);
            vm.Push(Regex.IsMatch(s, @"^\d+(\.\d+)?((E|e)(\+|\-)\d+)?$"));
            return 1;
        }
        public static int StdStringAlphaNumeric(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 1).GetString();
            if (nargs == 1)
            {
                int n = (int)ExAPI.GetFromStack(vm, 2).GetInt();
                if (n < 0 || n >= s.Length)
                {
                    vm.AddToErrorMessage("string can't be indexed with integer higher than it's length or negative");
                    return -1;
                }
                s = s[n].ToString();
            }
            vm.Pop(nargs + 2);
            vm.Push(Regex.IsMatch(s, "^[A-Za-z0-9]+$"));
            return 1;
        }
        public static int StdStringLower(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 1).GetString();
            if (nargs == 1)
            {
                int n = (int)ExAPI.GetFromStack(vm, 2).GetInt();
                if (n < 0 || n >= s.Length)
                {
                    vm.AddToErrorMessage("string can't be indexed with integer higher than it's length or negative");
                    return -1;
                }
                s = s[n].ToString();
            }
            foreach (char c in s)
            {
                if (!char.IsLower(c))
                {
                    vm.Pop(nargs + 2);
                    vm.Push(false);
                    return 1;
                }
            }
            vm.Pop(nargs + 2);
            vm.Push(true && !string.IsNullOrEmpty(s));
            return 1;
        }
        public static int StdStringUpper(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 1).GetString();
            if (nargs == 1)
            {
                int n = (int)ExAPI.GetFromStack(vm, 2).GetInt();
                if (n < 0 || n >= s.Length)
                {
                    vm.AddToErrorMessage("string can't be indexed with integer higher than it's length or negative");
                    return -1;
                }
                s = s[n].ToString();
            }
            foreach (char c in s)
            {
                if (!char.IsUpper(c))
                {
                    vm.Pop(nargs + 2);
                    vm.Push(false);
                    return 1;
                }
            }
            vm.Pop(nargs + 2);
            vm.Push(true && !string.IsNullOrEmpty(s));
            return 1;
        }
        public static int StdStringWhitespace(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 1).GetString();
            if (nargs == 1)
            {
                int n = (int)ExAPI.GetFromStack(vm, 2).GetInt();
                if (n < 0 || n >= s.Length)
                {
                    vm.AddToErrorMessage("string can't be indexed with integer higher than it's length or negative");
                    return -1;
                }
                s = s[n].ToString();
            }
            foreach (char c in s)
            {
                if (!char.IsWhiteSpace(c))
                {
                    vm.Pop(nargs + 2);
                    vm.Push(false);
                    return 1;
                }
            }
            vm.Pop(nargs + 2);
            vm.Push(true && s.Length > 0);
            return 1;
        }
        public static int StdStringSymbol(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 1).GetString();
            if (nargs == 1)
            {
                int n = (int)ExAPI.GetFromStack(vm, 2).GetInt();
                if (n < 0 || n >= s.Length)
                {
                    vm.AddToErrorMessage("string can't be indexed with integer higher than it's length or negative");
                    return -1;
                }
                s = s[n].ToString();
            }
            foreach (char c in s)
            {
                if (!char.IsSymbol(c))
                {
                    vm.Pop(nargs + 2);
                    vm.Push(false);
                    return 1;
                }
            }
            vm.Pop(nargs + 2);
            vm.Push(true && !string.IsNullOrEmpty(s));
            return 1;
        }
        public static int StdStringSlice(ExVM vm, int nargs)
        {
            ExObject o = new();
            ExAPI.GetSafeObject(vm, -1 - nargs, ExObjType.STRING, ref o);

            int start = (int)ExAPI.GetFromStack(vm, 2).GetInt();

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
                            return -1;
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
                        int end = (int)ExAPI.GetFromStack(vm, 3).GetInt();
                        if (start < 0)
                        {
                            start += n;
                        }
                        if (start >= n || start < 0)
                        {
                            vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                            return -1;
                        }

                        if (end < 0)
                        {
                            end += n;
                        }
                        if (end > n || end < 0)
                        {
                            vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                            return -1;
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

            vm.Pop(nargs + 2);
            if (filled)
            {
                vm.Push(new string(res));
            }
            else
            {
                vm.Push(string.Empty);
            }
            return 1;
        }

        // ARRAY
        public static int StdArrayAppend(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res);
            res.Value.l_List.Add(new(vm.GetAbove(-1)));
            vm.Pop(nargs + 2);
            vm.Push(res);
            return 1;
        }
        public static int StdArrayExtend(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res);
            res.Value.l_List.AddRange(vm.GetAbove(-1).Value.l_List);
            vm.Pop(nargs + 2);
            vm.Push(res);
            return 1;
        }
        public static int StdArrayPop(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, 1, ExObjType.ARRAY, ref res);
            if (res.Value.l_List.Count > 0)
            {
                ExObject p = new(res.Value.l_List[^1]);
                res.Value.l_List.RemoveAt(res.Value.l_List.Count - 1);
                vm.Pop(nargs + 2);
                vm.Push(p); // TO-DO make this optional
                return 1;
            }
            else
            {
                vm.AddToErrorMessage("can't pop from empty list");
                return -1;
            }
        }

        public static int StdArrayResize(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, 1, ExObjType.ARRAY, ref res);
            int newsize = (int)ExAPI.GetFromStack(vm, 2).GetInt();
            if (newsize < 0)
            {
                newsize = 0;
            }

            int curr = res.Value.l_List.Count;
            if (curr > 0 && newsize > 0)
            {
                if (newsize >= curr)
                {
                    for (int i = curr; i < newsize; i++)
                    {
                        res.Value.l_List.Add(new());
                    }
                }
                else
                {
                    while (curr != newsize)
                    {
                        res.Value.l_List[curr - 1].Nullify();
                        res.Value.l_List.RemoveAt(curr - 1);
                        curr--;
                    }
                }
            }
            else if (newsize > 0)
            {
                res.Value.l_List = new(newsize);
                for (int i = 0; i < newsize; i++)
                {
                    res.Value.l_List.Add(new());
                }
            }
            else
            {
                res.Value.l_List = new();
            }

            vm.Pop();
            return 1;
        }

        public static int StdArrayIndexOf(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res);
            using ExObject obj = new(ExAPI.GetFromStack(vm, 2));

            int i = ExAPI.GetValueIndexFromArray(res.Value.l_List, obj);
            vm.Pop(nargs + 2);
            vm.Push(i);
            return 1;
        }

        public static int StdArrayCount(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res);
            using ExObject obj = new(ExAPI.GetFromStack(vm, 2));

            int i = ExAPI.CountValueEqualsInArray(res.Value.l_List, obj);
            vm.Pop(nargs + 2);
            vm.Push(i);
            return 1;
        }

        public static int StdArraySlice(ExVM vm, int nargs)
        {
            ExObject o = new();
            ExAPI.GetSafeObject(vm, -1 - nargs, ExObjType.ARRAY, ref o);

            int start = (int)ExAPI.GetFromStack(vm, 2).GetInt();

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
                            return -1;
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
                        int end = (int)ExAPI.GetFromStack(vm, 3).GetInt();
                        if (start < 0)
                        {
                            start += n;
                        }
                        if (start > n || start < 0)
                        {
                            vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                            return -1;
                        }

                        if (end < 0)
                        {
                            end += n;
                        }
                        if (end > n || end < 0)
                        {
                            vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                            return -1;
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
            vm.Pop(nargs + 2);
            vm.Push(res);
            return 1;
        }

        public static int StdArrayReverse(ExVM vm, int nargs)
        {
            ExObject obj = ExAPI.GetFromStack(vm, 1);
            List<ExObject> lis = obj.GetList();
            List<ExObject> res = new(lis.Count);
            for (int i = lis.Count - 1; i >= 0; i--)
            {
                res.Add(new(lis[i]));
            }
            vm.Pop(nargs + 2);
            vm.Push(res);
            return 1;
        }

        public static int StdArrayCopy(ExVM vm, int nargs)
        {
            ExObject obj = ExAPI.GetFromStack(vm, 1);
            List<ExObject> lis = obj.GetList();
            List<ExObject> res = new(lis.Count);
            for (int i = 0; i < lis.Count; i++)
            {
                res.Add(new(lis[i]));
            }
            vm.Pop(nargs + 2);
            vm.Push(res);
            return 1;
        }

        public static int StdArrayTranspose(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, -1, ExObjType.ARRAY, ref res);
            List<ExObject> vals = res.GetList();
            int rows = vals.Count;
            int cols = 0;

            if (!ExAPI.DoMatrixTransposeChecks(vm, vals, ref cols))
            {
                return -1;
            }

            List<ExObject> lis = ExAPI.TransposeMatrix(rows, cols, vals);

            vm.Pop(nargs + 2);
            vm.Push(lis);
            return 1;
        }

        // DICT
        public static int StdDictHasKey(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.DICT, ref res);
            string key = vm.GetAbove(-1).GetString();
            bool b = res.Value.d_Dict.ContainsKey(key);

            vm.Pop(nargs + 2);
            vm.Push(b);
            return 1;
        }
        public static int StdDictKeys(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, -1, ExObjType.DICT, ref res);
            List<ExObject> keys = new(res.GetDict().Count);
            foreach (string key in res.GetDict().Keys)
            {
                keys.Add(new(key));
            }
            vm.Pop(nargs + 2);
            vm.Push(keys);
            return 1;
        }

        public static int StdDictValues(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, -1, ExObjType.DICT, ref res);
            List<ExObject> vals = new(res.GetDict().Count);
            foreach (ExObject val in res.GetDict().Values)
            {
                vals.Add(new(val));
            }
            vm.Pop(nargs + 2);
            vm.Push(vals);
            return 1;
        }

        // CLASS
        public static int StdClassHasAttr(ExVM vm, int nargs)
        {
            ExObject res = new();

            ExAPI.GetSafeObject(vm, -3, ExObjType.CLASS, ref res);
            string mem = vm.GetAbove(-2).GetString();
            string attr = vm.GetAbove(-1).GetString();

            ExClass cls = res.Value._Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        vm.Pop(nargs + 2);
                        vm.Push(true);
                        return 1;
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        vm.Pop(nargs + 2);
                        vm.Push(true);
                        return 1;
                    }
                }
                vm.Pop(nargs + 2);
                vm.Push(false);
                return 1;
            }

            vm.AddToErrorMessage("unknown member or method '" + mem + "'");
            return -1;
        }
        public static int StdClassGetAttr(ExVM vm, int nargs)
        {
            ExObject res = new();

            ExAPI.GetSafeObject(vm, -3, ExObjType.CLASS, ref res);
            string mem = vm.GetAbove(-2).GetString();
            string attr = vm.GetAbove(-1).GetString();

            ExClass cls = res.Value._Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls.DefaultValues[v.GetMemberID()].Attributes.GetDict()[attr]);
                        vm.Pop(nargs + 2);
                        vm.Push(val);
                        return 1;
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls.Methods[v.GetMemberID()].Attributes.GetDict()[attr]);
                        vm.Pop(nargs + 2);
                        vm.Push(val);
                        return 1;
                    }
                }
                vm.AddToErrorMessage("unknown attribute '" + attr + "'");
                return -1;
            }

            vm.AddToErrorMessage("unknown member or method '" + mem + "'");
            return -1;
        }

        public static int StdClassSetAttr(ExVM vm, int nargs)
        {
            ExObject res = new();

            ExAPI.GetSafeObject(vm, -4, ExObjType.CLASS, ref res);
            string mem = vm.GetAbove(-3).GetString();
            string attr = vm.GetAbove(-2).GetString();
            ExObject val = vm.GetAbove(-1);

            ExClass cls = res.Value._Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        cls.DefaultValues[v.GetMemberID()].Attributes.GetDict()[attr].Assign(val);
                        vm.Pop(nargs + 2);
                        return 1;
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        cls.Methods[v.GetMemberID()].Attributes.GetDict()[attr].Assign(val);
                        vm.Pop(nargs + 2);
                        return 1;
                    }
                }
                vm.AddToErrorMessage("unknown attribute '" + attr + "'");
                return -1;
            }

            vm.AddToErrorMessage("unknown member or method '" + mem + "'");
            return -1;
        }

        // INSTANCE
        public static int StdInstanceHasAttr(ExVM vm, int nargs)
        {
            ExObject res = new();

            ExAPI.GetSafeObject(vm, -3, ExObjType.INSTANCE, ref res);
            string mem = vm.GetAbove(-2).GetString();
            string attr = vm.GetAbove(-1).GetString();

            ExClass cls = res.Value._Instance.Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        vm.Pop(nargs + 2);
                        vm.Push(true);
                        return 1;
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        vm.Pop(nargs + 2);
                        vm.Push(true);
                        return 1;
                    }
                }
                vm.Pop(nargs + 2);
                vm.Push(false);
                return 1;
            }

            vm.AddToErrorMessage("unknown member or method '" + mem + "'");
            return -1;
        }
        public static int StdInstanceGetAttr(ExVM vm, int nargs)
        {
            ExObject res = new();

            ExAPI.GetSafeObject(vm, -3, ExObjType.INSTANCE, ref res);
            string mem = vm.GetAbove(-2).GetString();
            string attr = vm.GetAbove(-1).GetString();

            ExClass cls = res.Value._Instance.Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls.DefaultValues[v.GetMemberID()].Attributes.GetDict()[attr]);
                        vm.Pop(nargs + 2);
                        vm.Push(val);
                        return 1;
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls.Methods[v.GetMemberID()].Attributes.GetDict()[attr]);
                        vm.Pop(nargs + 2);
                        vm.Push(val);
                        return 1;
                    }
                }
                vm.AddToErrorMessage("unknown attribute '" + attr + "'");
                return -1;
            }

            vm.AddToErrorMessage("unknown member or method '" + mem + "'");
            return -1;
        }

        public static int StdInstanceSetAttr(ExVM vm, int nargs)
        {
            ExObject res = new();

            ExAPI.GetSafeObject(vm, -4, ExObjType.INSTANCE, ref res);
            string mem = vm.GetAbove(-3).GetString();
            string attr = vm.GetAbove(-2).GetString();
            ExObject val = vm.GetAbove(-1);
            ExClass cls = res.Value._Instance.Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        cls.DefaultValues[v.GetMemberID()].Attributes.GetDict()[attr].Assign(val);
                        vm.Pop(nargs + 2);
                        return 1;
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        cls.Methods[v.GetMemberID()].Attributes.GetDict()[attr].Assign(val);
                        vm.Pop(nargs + 2);
                        return 1;
                    }
                }
                vm.AddToErrorMessage("unknown attribute '" + attr + "'");
                return -1;
            }

            vm.AddToErrorMessage("unknown member or method '" + mem + "'");
            return -1;
        }
        //
        public static int StdReloadBase(ExVM vm, int nargs)
        {
            if (nargs == 1)
            {
                string name = ExAPI.GetFromStack(vm, 2).GetString();
                if (name == ReloadBaseFunc)
                {
                    vm.Pop(nargs + 2);
                    vm.Push(vm.RootDictionary);
                    return 1;
                }

                ExAPI.PushRootTable(vm);
                ExAPI.ReloadNativeFunction(vm, BaseFuncs, name, true);

            }
            else
            {
                if (!RegisterStdBase(vm, true))
                {
                    vm.AddToErrorMessage("something went wrong...");
                    return -1;
                }
            }
            vm.Pop(nargs + 2);
            vm.Push(vm.RootDictionary);
            return 1;
        }

        public static int StdExit(ExVM vm, int nargs)
        {
            long ret = ExAPI.GetFromStack(vm, 2).GetInt();
            vm.Pop(nargs + 2);
            vm.Push(ret);
            return 985;
        }

        public static int StdInteractive(ExVM vm, int nargs)
        {
            vm.Pop(2);
            vm.Push(vm.IsInteractive);
            return 1;
        }

        public static int StdGCCollect(ExVM vm, int nargs)
        {
            vm.Pop(2);
            ExAPI.CollectGarbage();
            return 0;
        }

        public static int StdWeakRef(ExVM vm, int nargs)
        {
            ExObject ret = ExAPI.GetFromStack(vm, 1);
            if (ret.IsCountingRefs())
            {
                vm.Push(ret.Value._RefC.GetWeakRef(ret.Type, ret.Value));
                return 1;
            }
            vm.Push(ret);
            return 1;
        }

        public static int StdWeakRefValue(ExVM vm, int nargs)
        {
            ExObject ret = ExAPI.GetFromStack(vm, 1);
            if (ret.Type != ExObjType.WEAKREF)
            {
                vm.AddToErrorMessage("can't get reference value of non-weakref object");
                return -1;
            }

            vm.Push(ret.Value._WeakRef.ReferencedObject);
            return 1;
        }

        public static MethodInfo GetBaseLibMethod(string name)
        {
            return Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod(name);
        }
        //
        private static readonly List<ExRegFunc> _exRegFuncs = new()
        {
            new()
            {
                Name = "print",
                Function = new(GetBaseLibMethod("StdPrint")),
                nParameterChecks = -2,
                ParameterMask = "..n",
                DefaultValues = new()
                {
                    { 2, new() }
                }
            },

            new()
            {
                Name = "printl",
                Function = new(GetBaseLibMethod("StdPrintl")),
                nParameterChecks = -2,
                ParameterMask = "..n",
                DefaultValues = new()
                {
                    { 2, new() }
                }
            },

            new()
            {
                Name = "time",
                Function = new(GetBaseLibMethod("StdTime")),
                nParameterChecks = -1,
                ParameterMask = null
            },
            new()
            {
                Name = "date",
                Function = new(GetBaseLibMethod("StdDate")),
                nParameterChecks = -1,
                ParameterMask = ".s.",
                DefaultValues = new()
                {
                    { 1, new("today") },
                    { 2, new(false) }
                }
            },

            new()
            {
                Name = "type",
                Function = new(GetBaseLibMethod("StdType")),
                nParameterChecks = 2,
                ParameterMask = null
            },
            new()
            {
                Name = "assert",
                Function = new(GetBaseLibMethod("StdAssert")),
                nParameterChecks = -2,
                ParameterMask = "..s",
                DefaultValues = new()
                {
                    { 1, new(true) },
                    { 2, new("") }
                }
            },

            new()
            {
                Name = "string",
                Function = new(GetBaseLibMethod("StdString")),
                nParameterChecks = -1,
                ParameterMask = "...i",
                DefaultValues = new()
                {
                    { 1, new("") },
                    { 2, new(false) },
                    { 3, new(2) }
                }
            },
            new()
            {
                Name = "complex",
                Function = new(GetBaseLibMethod("StdComplex")),
                nParameterChecks = -1,
                ParameterMask = ".n|Cn",
                DefaultValues = new()
                {
                    { 1, new(0.0) },
                    { 2, new(0.0) }
                }
            },
            new()
            {
                Name = "complex2",
                Function = new(GetBaseLibMethod("StdComplex2")),
                nParameterChecks = -1,
                ParameterMask = ".i|fi|f",
                DefaultValues = new()
                {
                    { 1, new(0.0) },
                    { 2, new(0.0) }
                }
            },
            new()
            {
                Name = "float",
                Function = new(GetBaseLibMethod("StdFloat")),
                nParameterChecks = -1,
                ParameterMask = "..",
                DefaultValues = new()
                {
                    { 1, new(0) }
                }
            },
            new()
            {
                Name = "integer",
                Function = new(GetBaseLibMethod("StdInteger")),
                nParameterChecks = -1,
                ParameterMask = "..",
                DefaultValues = new()
                {
                    { 1, new(0) }
                }
            },
            new()
            {
                Name = "bool",
                Function = new(GetBaseLibMethod("StdBool")),
                nParameterChecks = -1,
                ParameterMask = "..",
                DefaultValues = new()
                {
                    { 1, new(true) }
                }
            },
            new()
            {
                Name = "bits",
                Function = new(GetBaseLibMethod("StdBits")),
                nParameterChecks = -1,
                ParameterMask = ".i|f.",
                DefaultValues = new()
                {
                    { 1, new(0) },
                    { 2, new(false) }
                }
            },
            new()
            {
                Name = "bits32",
                Function = new(GetBaseLibMethod("StdBits32")),
                nParameterChecks = -1,
                ParameterMask = ".i|f.",
                DefaultValues = new()
                {
                    { 1, new(0) },
                    { 2, new(false) }
                }
            },
            new()
            {
                Name = "bytes",
                Function = new(GetBaseLibMethod("StdBytes")),
                nParameterChecks = -1,
                ParameterMask = ".i|f|s.",
                DefaultValues = new()
                {
                    { 1, new(0) },
                    { 2, new(false) }
                }
            },

            new()
            {
                Name = "list",
                Function = new(GetBaseLibMethod("StdList")),
                nParameterChecks = -1,
                ParameterMask = ".n|s.",
                DefaultValues = new()
                {
                    { 1, new(0) },
                    { 2, new() }
                }
            },
            new()
            {
                Name = "range",
                Function = new(GetBaseLibMethod("StdRange")),
                nParameterChecks = -2,
                ParameterMask = ".nnn",
                DefaultValues = new()
                {
                    { 1, new(0) },
                    { 2, new(0) },
                    { 3, new(1) }
                }
            },
            new()
            {
                Name = "rangei",
                Function = new(GetBaseLibMethod("StdRangei")),
                nParameterChecks = -2,
                ParameterMask = ".nnn",
                DefaultValues = new()
                {
                    { 1, new(0) },
                    { 2, new(0) },
                    { 3, new(1) }
                }
            },
            new()
            {
                Name = "matrix",
                Function = new(GetBaseLibMethod("StdMatrix")),
                nParameterChecks = -3,
                ParameterMask = ".ii.",
                DefaultValues = new()
                {
                    { 1, new(0) },
                    { 2, new(0) },
                    { 3, new() }
                }
            },

            new()
            {
                Name = "map",
                Function = new(GetBaseLibMethod("StdMap")),
                nParameterChecks = -3,
                ParameterMask = ".c|yaa"
            },
            new()
            {
                Name = "filter",
                Function = new(GetBaseLibMethod("StdFilter")),
                nParameterChecks = 3,
                ParameterMask = ".ca"
            },
            new()
            {
                Name = "call",
                Function = new(GetBaseLibMethod("StdCall")),
                nParameterChecks = -2,
                ParameterMask = null
            },
            new()
            {
                Name = "parse",
                Function = new(GetBaseLibMethod("StdParse")),
                nParameterChecks = 3,
                ParameterMask = ".c|ya"
            },
            new()
            {
                Name = "iter",
                Function = new(GetBaseLibMethod("StdIter")),
                nParameterChecks = 4,
                ParameterMask = ".c|ya."
            },
            new()
            {
                Name = "first",
                Function = new(GetBaseLibMethod("StdFirst")),
                nParameterChecks = 3,
                ParameterMask = ".ca"
            },
            new()
            {
                Name = "any",
                Function = new(GetBaseLibMethod("StdAny")),
                nParameterChecks = 3,
                ParameterMask = ".ca"
            },
            new()
            {
                Name = "all",
                Function = new(GetBaseLibMethod("StdAll")),
                nParameterChecks = 3,
                ParameterMask = ".ca"
            },

            new()
            {
                Name = "exit",
                Function = new(GetBaseLibMethod("StdExit")),
                nParameterChecks = -1,
                ParameterMask = ".i|f"
            },
            new()
            {
                Name = "is_interactive",
                Function = new(GetBaseLibMethod("StdInteractive")),
                nParameterChecks = 1,
                ParameterMask = "."
            },
            new()
            {
                Name = "collect_garbage",
                Function = new(GetBaseLibMethod("StdGCCollect")),
                nParameterChecks = 1,
                ParameterMask = "."
            },

            new()
            {
                Name = ReloadBaseFunc,
                Function = new(GetBaseLibMethod("StdReloadbase")),
                nParameterChecks = -1,
                ParameterMask = ".s"
            },
            new()
            {
                Name = string.Empty
            }
        };
        public static List<ExRegFunc> BaseFuncs => _exRegFuncs;

        private const string _reloadbase = "reload_base";
        public static string ReloadBaseFunc => _reloadbase;

        public static bool RegisterStdBase(ExVM vm, bool force = false)
        {
            ExAPI.PushRootTable(vm);
            ExAPI.RegisterNativeFunctions(vm, BaseFuncs, force);

            ExAPI.CreateConstantInt(vm, "_versionnumber_", 1);
            ExAPI.CreateConstantString(vm, "_version_", "ExMat v0.0.1");

            vm.Pop(1);

            return true;
        }
    }
}
