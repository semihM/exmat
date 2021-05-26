using System;
using System.Collections.Generic;
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
        public static int BASE_print(ExVM vm, int nargs)
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
        public static int BASE_printl(ExVM vm, int nargs)
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

        public static int BASE_type(ExVM vm, int nargs)
        {
            ExObject o = ExAPI.GetFromStack(vm, 2);
            string t = o._type.ToString();
            vm.Pop(nargs + 2);
            vm.Push(new ExObject(t));
            return 1;
        }

        public static int BASE_time(ExVM vm, int nargs)
        {
            vm.Pop(nargs + 2);
            vm.Push(new ExObject((double)(DateTime.Now - vm.StartingTime).TotalMilliseconds));
            return 1;
        }

        public static int BASE_date(ExVM vm, int nargs)
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
                                        res.Add(new ExObject(now.Month.ToString()));
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

        public static int BASE_assert(ExVM vm, int nargs)
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
        public static int BASE_bool(ExVM vm, int nargs)
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

        public static int BASE_string(ExVM vm, int nargs)
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
                        if (obj._type == ExObjType.ARRAY && carr)
                        {
                            string str = string.Empty;
                            foreach (ExObject o in obj.GetList())
                            {
                                if (o._type == ExObjType.STRING) // && o.GetString().Length == 1)
                                {
                                    str += o.GetString();
                                }
                                else if (o._type == ExObjType.INTEGER && o.GetInt() >= 0)
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
        public static int BASE_float(ExVM vm, int nargs)
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
        public static int BASE_integer(ExVM vm, int nargs)
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

        public static int BASE_bits(ExVM vm, int nargs)
        {
            bool reverse = true;
            switch (nargs)
            {
                case 2:
                    {
                        reverse = ExAPI.GetFromStack(vm, 2).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        ExObject v = ExAPI.GetFromStack(vm, 2);
                        long b = 0;
                        switch (v._type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    b = (int)v.GetInt();
                                    goto default;
                                }
                            case ExObjType.FLOAT:
                                {
                                    b = new FloatInt() { f = v.GetFloat() }.i;
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

        public static int BASE_bytes(ExVM vm, int nargs)
        {
            bool reverse = false;
            switch (nargs)
            {
                case 2:
                    {
                        reverse = ExAPI.GetFromStack(vm, 3).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        ExObject v = ExAPI.GetFromStack(vm, 2);
                        byte[] bytes = null;
                        switch (v._type)
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

        public static int BASE_map(ExVM vm, int nargs)
        {
            ExObject cls = ExAPI.GetFromStack(vm, 2);
            ExObject obj = new(ExAPI.GetFromStack(vm, 3));
            List<ExObject> l = new(obj._val.l_List.Count);

            vm.Pop();

            ExObject res = new();

            bool iscls = cls._type == ExObjType.CLOSURE;

            if (!iscls && cls._type != ExObjType.NATIVECLOSURE)
            {
                vm.AddToErrorMessage("can't call non-closure type");
                return -1;
            }

            ExObject tmp = new();

            int n = 2;
            int m = 0;
            if (!iscls && cls.GetNClosure().b_deleg)
            {
                n--;
                m++;
            }
            bool is_seq = cls.GetClosure()._func.IsSequence();
            bool bm = vm.b_main;
            vm.b_main = false;

            if (is_seq)
            {
                List<ExObject> _defs = cls.GetClosure()._func._params;
                List<string> defs = new(_defs.Count);
                for (int i = 0; i < _defs.Count; i++)
                {
                    defs.Add(_defs[i].GetString());
                }

                foreach (ExObject o in obj._val.l_List)
                {
                    vm.Push(cls);
                    vm.Push(vm._rootdict);

                    vm.Push(o);
                    if (!vm.Call(ref cls, n, vm._top - n, ref tmp, true))
                    {
                        vm.Pop();
                        vm.b_main = bm;
                        return -1;
                    }
                    else if (defs.IndexOf(o.GetInt().ToString()) != -1)  // TO-DO fix this mess
                    {
                        l.Add(new(vm.GetAt(vm._top - n - 1)));
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
                foreach (ExObject o in obj._val.l_List)
                {
                    vm.Push(cls);
                    vm.Push(vm._rootdict);

                    vm.Push(o);
                    if (!vm.Call(ref cls, n, vm._top - n, ref tmp, true))
                    {
                        vm.Pop();
                        vm.b_main = bm;
                        return -1;
                    }
                    else
                    {
                        vm.Pop(n + 1 + m);
                        l.Add(new(tmp));
                    }
                }
            }

            vm.b_main = bm;
            vm.Pop(n + m + 1);
            vm.Push(new ExObject(l));
            return 1;
        }

        public static int BASE_filter(ExVM vm, int nargs)
        {
            ExObject cls = ExAPI.GetFromStack(vm, 2);
            ExObject obj = new(ExAPI.GetFromStack(vm, 3));
            List<ExObject> l = new(obj._val.l_List.Count);

            vm.Pop();

            ExObject res = new();

            bool iscls = cls._type == ExObjType.CLOSURE;

            if (!iscls && cls._type != ExObjType.NATIVECLOSURE)
            {
                vm.AddToErrorMessage("can't call non-closure type");
                return -1;
            }

            ExObject tmp = new();

            int n = 2;
            int m = 0;
            if (!iscls && cls.GetNClosure().b_deleg)
            {
                n--;
                m++;
            }
            bool bm = vm.b_main;
            vm.b_main = false;
            foreach (ExObject o in obj._val.l_List)
            {
                vm.Push(cls);
                vm.Push(vm._rootdict);

                vm.Push(o);
                if (!vm.Call(ref cls, n, vm._top - n, ref tmp, true))
                {
                    vm.Pop();
                    vm.b_main = bm;
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

            vm.b_main = bm;
            vm.Pop(n + m + 1);
            vm.Push(new ExObject(l));
            return 1;
        }

        public static int BASE_call(ExVM vm, int nargs)
        {
            ExObject cls = ExAPI.GetFromStack(vm, 2);

            ExObject res = new();

            bool need_b = false;
            bool iscls = cls._type == ExObjType.CLOSURE;

            if (!iscls && cls._type != ExObjType.NATIVECLOSURE)
            {
                vm.AddToErrorMessage("can't call non-closure type");
                return -1;
            }

            List<ExObject> args = new();


            if (iscls)
            {
                ExFuncPro pro = cls.GetClosure()._func;

                if (pro.n_params == 1)
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
                    int p = pro.n_params;
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
                        && pro.n_defparams > 0)
                    {
                        int n_def = pro.n_defparams;
                        int diff;
                        if (n_def > 0 && nargs < p && (diff = p - nargs) <= n_def)
                        {
                            for (int n = n_def - diff; n < n_def; n++)
                            {
                                args.Add(cls.GetClosure()._defparams[n]);
                            }
                        }
                    }
                    else if (pro.n_params != nargs)
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

            bool bm = vm.b_main;
            vm.b_main = false;
            if (ExAPI.Call(vm, 3, true, need_b, iscls, nargs))
            {
                res.Assign(vm.GetAbove(-1)); // ExAPI.GetFromStack(vm, nargs - (iscls ? 1 : 0))
                vm.Pop();
            }
            else
            {
                vm.b_main = bm;
                return -1;
            }

            vm.b_main = bm;
            vm.Pop(3);
            vm.Push(res);
            return 1;
        }

        private static bool DecideCallNeedNC(ExVM v, ExNativeClosure c, int n, ref bool b)
        {
            if (c.b_deleg)
            {
                b = c.n_paramscheck == 1;
            }
            else if (c.n_paramscheck > 0)
            {
                if (c.n_paramscheck == 1)
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
                    b = c.n_paramscheck == 2;
                }
            }
            else
            {
                b = c.n_paramscheck == -1;
            }

            return true;
        }

        public static int BASE_parse(ExVM vm, int nargs)
        {
            ExObject cls = ExAPI.GetFromStack(vm, 2);
            List<ExObject> args = new ExObject(ExAPI.GetFromStack(vm, 3))._val.l_List;
            if (args.Count > vm._stack.Count - vm._top - 3)
            {
                vm.AddToErrorMessage("stack size is too small for parsing " + args.Count + " arguments! Current size: " + vm._stack.Count);
                return -1;
            }

            vm.Pop();

            ExObject res = new();

            bool iscls = cls._type == ExObjType.CLOSURE;

            if (!iscls && cls._type != ExObjType.NATIVECLOSURE)
            {
                vm.AddToErrorMessage("can't call non-closure type");
                return -1;
            }

            int n = args.Count + 1;

            vm.Push(cls);
            if (iscls)
            {
                vm.Push(vm._rootdict);
            }

            if (args.Count == 0 && !iscls)
            {
                vm.Push(new ExObject());
            }
            else
            {
                if (iscls
                    && cls.GetClosure()._func.IsCluster()
                    && cls.GetClosure()._defparams.Count == 1)
                {
                    vm.Push(args); // Handle 1 parameter clusters => [args]
                    n = 2;
                }
                else
                {
                    vm.PushParse(args);
                }
            }

            ExObject tmp = new();
            bool bm = vm.b_main;
            vm.b_main = false;
            if (!vm.Call(ref cls, n, vm._top - n, ref tmp, true))
            {
                vm.Pop(n + (iscls ? 1 : 0));
                vm.b_main = bm;
                return -1;
            }

            vm.b_main = bm;
            vm.Pop(n + (iscls ? 4 : 3));
            vm.Push(tmp);
            return 1;
        }

        public static int BASE_list(ExVM vm, int nargs)
        {
            ExObject o = ExAPI.GetFromStack(vm, 2);
            if (o._type == ExObjType.STRING)
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
                    l._val.l_List = new(s);
                    ExUtils.InitList(ref l._val.l_List, s, ExAPI.GetFromStack(vm, 3));
                }
                else
                {
                    l._val.l_List = new(s);
                    ExUtils.InitList(ref l._val.l_List, s);
                }
                vm.Pop(nargs + 2);
                vm.Push(l);
                return 1;
            }
        }
        public static int BASE_range(ExVM vm, int nargs)
        {
            ExList l = new();
            ExObject s = ExAPI.GetFromStack(vm, 2);

            switch (nargs)
            {
                case 3:
                    {
                        double start = s.GetFloat();
                        double end = ExAPI.GetFromStack(vm, 3).GetFloat();
                        double step = ExAPI.GetFromStack(vm, 4).GetFloat();
                        l._val.l_List = new();

                        if (end > start)
                        {
                            int count = (int)((end - start) / step);

                            for (int i = 0; i < count; i++)
                            {
                                l._val.l_List.Add(new(start + i * step));
                            }
                        }

                        break;
                    }

                case 2:
                    {
                        double start = s.GetFloat();
                        double end = ExAPI.GetFromStack(vm, 3).GetFloat();
                        l._val.l_List = new();

                        if (end > start)
                        {
                            int count = (int)(end - start);
                            for (int i = 0; i < count; i++)
                            {
                                l._val.l_List.Add(new(start + i));
                            }
                        }

                        break;
                    }

                case 1:
                    {
                        double end = s.GetFloat();
                        l._val.l_List = new();

                        int count = (int)end;
                        for (int i = 0; i < count; i++)
                        {
                            l._val.l_List.Add(new(i));
                        }

                        break;
                    }
            }
            vm.Pop(nargs + 2);
            vm.Push(l);
            return 1;
        }
        public static int BASE_matrix(ExVM vm, int nargs)
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
                        l._val.l_List = new(m);

                        switch (filler._type)
                        {
                            case ExObjType.CLOSURE:
                                {
                                    if (filler.GetClosure()._func.n_params != 3
                                        && (filler.GetClosure()._func.n_params - filler.GetClosure()._func.n_defparams) > 3)
                                    {
                                        vm.AddToErrorMessage("given function must allow 2-argument calls");
                                        return -1;
                                    }

                                    bool bm = vm.b_main;
                                    vm.b_main = false;
                                    for (int i = 0; i < m; i++)
                                    {
                                        List<ExObject> lis = new(n);
                                        for (int j = 0; j < n; j++)
                                        {
                                            ExObject res = new();
                                            vm.Push(vm._rootdict);
                                            vm.Push(i);
                                            vm.Push(j);
                                            if (!vm.Call(ref filler, 3, vm._top - 3, ref res, true))
                                            {
                                                vm.b_main = bm;
                                                return -1;
                                            }
                                            vm.Pop(3);

                                            lis.Add(new(res));
                                        }
                                        l._val.l_List.Add(new ExObject(lis));
                                    }

                                    vm.b_main = bm;
                                    break;
                                }
                            case ExObjType.NATIVECLOSURE:
                                {
                                    int nparamscheck = filler.GetNClosure().n_paramscheck;
                                    if (((nparamscheck > 0) && (nparamscheck != 3)) ||
                                        ((nparamscheck < 0) && (3 < (-nparamscheck))))
                                    {
                                        if (nparamscheck < 0)
                                        {
                                            vm.AddToErrorMessage("'" + filler.GetNClosure()._name.GetString() + "' takes minimum " + (-nparamscheck - 1) + " arguments");
                                            return -1;
                                        }
                                        vm.AddToErrorMessage("'" + filler.GetNClosure()._name.GetString() + "' takes exactly " + (nparamscheck - 1) + " arguments");
                                        return -1;
                                    }

                                    bool bm = vm.b_main;
                                    vm.b_main = false;
                                    for (int i = 0; i < m; i++)
                                    {
                                        List<ExObject> lis = new(n);
                                        for (int j = 0; j < n; j++)
                                        {
                                            ExObject res = new();
                                            vm.Push(i);
                                            vm.Push(j);
                                            if (!vm.Call(ref filler, 2, vm._top - 2, ref res, true))
                                            {
                                                return -1;
                                            }
                                            vm.Pop(2);

                                            lis.Add(new(res));
                                        }
                                        l._val.l_List.Add(new ExObject(lis));
                                    }

                                    vm.b_main = bm;
                                    break;
                                }
                            default:
                                {
                                    for (int i = 0; i < m; i++)
                                    {
                                        List<ExObject> lis = null;
                                        ExUtils.InitList(ref lis, n, filler);
                                        l._val.l_List.Add(new ExObject(lis));
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

        // COMMON
        public static int BASE_default_length(ExVM vm, int nargs)
        {
            int size = -1;
            ExObject obj = ExAPI.GetFromStack(vm, 1);
            switch (obj._type)
            {
                case ExObjType.ARRAY:
                    {
                        size = obj._val.l_List.Count;
                        break;
                    }
                case ExObjType.DICT:
                    {
                        size = obj._val.d_Dict.Count;
                        break;
                    }
                case ExObjType.STRING:
                    {
                        size = obj.GetString().Length;
                        break;
                    }
                case ExObjType.CLASS:
                    {
                        size = obj._val._Class._udsize;
                        break;
                    }
                case ExObjType.INSTANCE:
                    {
                        size = obj._val._Instance._class._udsize;
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
        public static int BASE_string_index_of(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.STRING, ref res);
            string sub = vm.GetAbove(-1).GetString();
            string s = res.GetString();
            vm.Pop();
            vm.Push(res.GetString().IndexOf(sub));
            return 1;
        }

        public static int BASE_string_toupper(ExVM vm, int nargs)
        {
            string obj = ExAPI.GetFromStack(vm, 1).GetString();
            vm.Pop(nargs + 2);
            vm.Push(obj.ToUpper());
            return 1;
        }
        public static int BASE_string_tolower(ExVM vm, int nargs)
        {
            string obj = ExAPI.GetFromStack(vm, 1).GetString();
            vm.Pop(nargs + 2);
            vm.Push(obj.ToLower());
            return 1;
        }
        public static int BASE_string_reverse(ExVM vm, int nargs)
        {
            string obj = ExAPI.GetFromStack(vm, 1).GetString();
            char[] ch = obj.ToCharArray();
            Array.Reverse(ch);
            vm.Pop(nargs + 2);
            vm.Push(new string(ch));
            return 1;
        }
        public static int BASE_string_replace(ExVM vm, int nargs)
        {
            string obj = ExAPI.GetFromStack(vm, 1).GetString();
            string old = ExAPI.GetFromStack(vm, 2).GetString();
            string rep = ExAPI.GetFromStack(vm, 3).GetString();
            vm.Pop(nargs + 2);
            vm.Push(obj.Replace(old, rep));
            return 1;
        }

        public static int BASE_string_repeat(ExVM vm, int nargs)
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

        public static int BASE_string_isAlphabetic(ExVM vm, int nargs)
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
        public static int BASE_string_isNumeric(ExVM vm, int nargs)
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
        public static int BASE_string_isAlphaNumeric(ExVM vm, int nargs)
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
        public static int BASE_string_isLower(ExVM vm, int nargs)
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
        public static int BASE_string_isUpper(ExVM vm, int nargs)
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
        public static int BASE_string_isWhitespace(ExVM vm, int nargs)
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
        public static int BASE_string_isSymbol(ExVM vm, int nargs)
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
        // ARRAY
        public static int BASE_array_append(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res);
            res._val.l_List.Add(new(vm.GetAbove(-1)));
            vm.Pop(nargs + 2);
            vm.Push(res);
            return 1;
        }
        public static int BASE_array_extend(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res);
            res._val.l_List.AddRange(vm.GetAbove(-1)._val.l_List);
            vm.Pop(nargs + 2);
            vm.Push(res);
            return 1;
        }
        public static int BASE_array_pop(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, 1, ExObjType.ARRAY, ref res);
            if (res._val.l_List.Count > 0)
            {
                ExObject p = new(res._val.l_List[^1]);
                res._val.l_List.RemoveAt(res._val.l_List.Count - 1);
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

        public static int BASE_array_resize(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, 1, ExObjType.ARRAY, ref res);
            int newsize = (int)ExAPI.GetFromStack(vm, 2).GetInt();
            if (newsize < 0)
            {
                newsize = 0;
            }

            int curr = res._val.l_List.Count;
            if (curr > 0 && newsize > 0)
            {
                if (newsize >= curr)
                {
                    for (int i = curr; i < newsize; i++)
                    {
                        res._val.l_List.Add(new());
                    }
                }
                else
                {
                    while (curr != newsize)
                    {
                        res._val.l_List[curr - 1].Nullify();
                        res._val.l_List.RemoveAt(curr - 1);
                        curr--;
                    }
                }
            }
            else if (newsize > 0)
            {
                res._val.l_List = new(newsize);
                for (int i = 0; i < newsize; i++)
                {
                    res._val.l_List.Add(new());
                }
            }
            else
            {
                res._val.l_List = new();
            }

            vm.Pop();
            return 1;
        }

        public static int BASE_array_index_of(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res);
            using ExObject obj = new(vm.GetAbove(-1));

            int i = ExAPI.GetValueIndexFromArray(res._val.l_List, obj);
            vm.Pop(nargs + 2);
            vm.Push(i);
            return 1;
        }

        public static int BASE_array_reverse(ExVM vm, int nargs)
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
        // DICT
        public static int BASE_dict_has_key(ExVM vm, int nargs)
        {
            ExObject res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.DICT, ref res);
            string key = vm.GetAbove(-1).GetString();
            bool b = res._val.d_Dict.ContainsKey(key);

            vm.Pop(nargs + 2);
            vm.Push(b);
            return 1;
        }
        public static int BASE_dict_keys(ExVM vm, int nargs)
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

        public static int BASE_dict_values(ExVM vm, int nargs)
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

        public static int BASE_array_transpose(ExVM vm, int nargs)
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

        // CLASS
        public static int BASE_class_hasattr(ExVM vm, int nargs)
        {
            ExObject res = new();

            ExAPI.GetSafeObject(vm, -3, ExObjType.CLASS, ref res);
            string mem = vm.GetAbove(-2).GetString();
            string attr = vm.GetAbove(-1).GetString();

            ExClass cls = res._val._Class;
            if (cls._members.ContainsKey(mem))
            {
                ExObject v = cls._members[mem];
                if (v.IsField())
                {
                    if (cls._defvals[v.GetMemberID()].attrs.GetDict().ContainsKey(attr))
                    {
                        vm.Pop(nargs + 2);
                        vm.Push(true);
                        return 1;
                    }
                }
                else
                {
                    if (cls._methods[v.GetMemberID()].attrs.GetDict().ContainsKey(attr))
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
        public static int BASE_class_getattr(ExVM vm, int nargs)
        {
            ExObject res = new();

            ExAPI.GetSafeObject(vm, -3, ExObjType.CLASS, ref res);
            string mem = vm.GetAbove(-2).GetString();
            string attr = vm.GetAbove(-1).GetString();

            ExClass cls = res._val._Class;
            if (cls._members.ContainsKey(mem))
            {
                ExObject v = cls._members[mem];
                if (v.IsField())
                {
                    if (cls._defvals[v.GetMemberID()].attrs.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls._defvals[v.GetMemberID()].attrs.GetDict()[attr]);
                        vm.Pop(nargs + 2);
                        vm.Push(val);
                        return 1;
                    }
                }
                else
                {
                    if (cls._methods[v.GetMemberID()].attrs.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls._methods[v.GetMemberID()].attrs.GetDict()[attr]);
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

        public static int BASE_class_setattr(ExVM vm, int nargs)
        {
            ExObject res = new();

            ExAPI.GetSafeObject(vm, -4, ExObjType.CLASS, ref res);
            string mem = vm.GetAbove(-3).GetString();
            string attr = vm.GetAbove(-2).GetString();
            ExObject val = vm.GetAbove(-1);

            ExClass cls = res._val._Class;
            if (cls._members.ContainsKey(mem))
            {
                ExObject v = cls._members[mem];
                if (v.IsField())
                {
                    if (cls._defvals[v.GetMemberID()].attrs.GetDict().ContainsKey(attr))
                    {
                        cls._defvals[v.GetMemberID()].attrs.GetDict()[attr].Assign(val);
                        vm.Pop(nargs + 2);
                        return 1;
                    }
                }
                else
                {
                    if (cls._methods[v.GetMemberID()].attrs.GetDict().ContainsKey(attr))
                    {
                        cls._methods[v.GetMemberID()].attrs.GetDict()[attr].Assign(val);
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
        public static int BASE_instance_hasattr(ExVM vm, int nargs)
        {
            ExObject res = new();

            ExAPI.GetSafeObject(vm, -3, ExObjType.INSTANCE, ref res);
            string mem = vm.GetAbove(-2).GetString();
            string attr = vm.GetAbove(-1).GetString();

            ExClass cls = res._val._Instance._class;
            if (cls._members.ContainsKey(mem))
            {
                ExObject v = cls._members[mem];
                if (v.IsField())
                {
                    if (cls._defvals[v.GetMemberID()].attrs.GetDict().ContainsKey(attr))
                    {
                        vm.Pop(nargs + 2);
                        vm.Push(true);
                        return 1;
                    }
                }
                else
                {
                    if (cls._methods[v.GetMemberID()].attrs.GetDict().ContainsKey(attr))
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
        public static int BASE_instance_getattr(ExVM vm, int nargs)
        {
            ExObject res = new();

            ExAPI.GetSafeObject(vm, -3, ExObjType.INSTANCE, ref res);
            string mem = vm.GetAbove(-2).GetString();
            string attr = vm.GetAbove(-1).GetString();

            ExClass cls = res._val._Instance._class;
            if (cls._members.ContainsKey(mem))
            {
                ExObject v = cls._members[mem];
                if (v.IsField())
                {
                    if (cls._defvals[v.GetMemberID()].attrs.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls._defvals[v.GetMemberID()].attrs.GetDict()[attr]);
                        vm.Pop(nargs + 2);
                        vm.Push(val);
                        return 1;
                    }
                }
                else
                {
                    if (cls._methods[v.GetMemberID()].attrs.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls._methods[v.GetMemberID()].attrs.GetDict()[attr]);
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

        public static int BASE_instance_setattr(ExVM vm, int nargs)
        {
            ExObject res = new();

            ExAPI.GetSafeObject(vm, -4, ExObjType.INSTANCE, ref res);
            string mem = vm.GetAbove(-3).GetString();
            string attr = vm.GetAbove(-2).GetString();
            ExObject val = vm.GetAbove(-1);
            ExClass cls = res._val._Instance._class;
            if (cls._members.ContainsKey(mem))
            {
                ExObject v = cls._members[mem];
                if (v.IsField())
                {
                    if (cls._defvals[v.GetMemberID()].attrs.GetDict().ContainsKey(attr))
                    {
                        cls._defvals[v.GetMemberID()].attrs.GetDict()[attr].Assign(val);
                        vm.Pop(nargs + 2);
                        return 1;
                    }
                }
                else
                {
                    if (cls._methods[v.GetMemberID()].attrs.GetDict().ContainsKey(attr))
                    {
                        cls._methods[v.GetMemberID()].attrs.GetDict()[attr].Assign(val);
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
        public static int BASE_reloadbase(ExVM vm, int nargs)
        {
            if (nargs == 1)
            {
                string name = ExAPI.GetFromStack(vm, 2).GetString();
                if (name == ReloadBaseFunc)
                {
                    vm.Pop(nargs + 2);
                    vm.Push(vm._rootdict);
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
            vm.Push(vm._rootdict);
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
                name = "print",
                func = new(GetBaseLibMethod("BASE_print")),
                n_pchecks = -2,
                mask = "..n",
                d_defaults = new()
                {
                    { 2, new() }
                }
            },

            new()
            {
                name = "printl",
                func = new(GetBaseLibMethod("BASE_printl")),
                n_pchecks = -2,
                mask = "..n",
                d_defaults = new()
                {
                    { 2, new() }
                }
            },

            new()
            {
                name = "time",
                func = new(GetBaseLibMethod("BASE_time")),
                n_pchecks = 1,
                mask = null
            },
            new()
            {
                name = "date",
                func = new(GetBaseLibMethod("BASE_date")),
                n_pchecks = -1,
                mask = ".s.",
                d_defaults = new()
                {
                    { 1, new("today") },
                    { 2, new(false) }
                }
            },

            new()
            {
                name = "type",
                func = new(GetBaseLibMethod("BASE_type")),
                n_pchecks = 2,
                mask = null
            },
            new()
            {
                name = "assert",
                func = new(GetBaseLibMethod("BASE_assert")),
                n_pchecks = -2,
                mask = "..s",
                d_defaults = new()
                {
                    { 1, new(true) },
                    { 2, new("") }
                }
            },

            new()
            {
                name = "string",
                func = new(GetBaseLibMethod("BASE_string")),
                n_pchecks = -1,
                mask = "...i",
                d_defaults = new()
                {
                    { 1, new("") },
                    { 2, new(false) },
                    { 3, new(2) }
                }
            },
            new()
            {
                name = "float",
                func = new(GetBaseLibMethod("BASE_float")),
                n_pchecks = -1,
                mask = "..",
                d_defaults = new()
                {
                    { 1, new(0) }
                }
            },
            new()
            {
                name = "integer",
                func = new(GetBaseLibMethod("BASE_integer")),
                n_pchecks = -1,
                mask = "..",
                d_defaults = new()
                {
                    { 1, new(0) }
                }
            },
            new()
            {
                name = "bool",
                func = new(GetBaseLibMethod("BASE_bool")),
                n_pchecks = -1,
                mask = "..",
                d_defaults = new()
                {
                    { 1, new(true) }
                }
            },
            new()
            {
                name = "bits",
                func = new(GetBaseLibMethod("BASE_bits")),
                n_pchecks = -1,
                mask = ".n.",
                d_defaults = new()
                {
                    { 1, new(0) },
                    { 2, new(false) }
                }
            },
            new()
            {
                name = "bytes",
                func = new(GetBaseLibMethod("BASE_bytes")),
                n_pchecks = -1,
                mask = ".n|s.",
                d_defaults = new()
                {
                    { 1, new(0) },
                    { 2, new(false) }
                }
            },

            new()
            {
                name = "list",
                func = new(GetBaseLibMethod("BASE_list")),
                n_pchecks = -1,
                mask = ".n|s.",
                d_defaults = new()
                {
                    { 1, new(0) },
                    { 2, new() }
                }
            },
            new()
            {
                name = "range",
                func = new(GetBaseLibMethod("BASE_range")),
                n_pchecks = -2,
                mask = ".nnn",
                d_defaults = new()
                {
                    { 1, new(0) },
                    { 2, new(0) },
                    { 3, new(1) }
                }
            },
            new()
            {
                name = "matrix",
                func = new(GetBaseLibMethod("BASE_matrix")),
                n_pchecks = -3,
                mask = ".ii.",
                d_defaults = new()
                {
                    { 1, new(0) },
                    { 2, new(0) },
                    { 3, new() }
                }
            },

            new()
            {
                name = "map",
                func = new(GetBaseLibMethod("BASE_map")),
                n_pchecks = 3,
                mask = ".ca"
            },
            new()
            {
                name = "filter",
                func = new(GetBaseLibMethod("BASE_filter")),
                n_pchecks = 3,
                mask = ".ca"
            },
            new()
            {
                name = "call",
                func = new(GetBaseLibMethod("BASE_call")),
                n_pchecks = -2,
                mask = null
            },
            new()
            {
                name = "parse",
                func = new(GetBaseLibMethod("BASE_parse")),
                n_pchecks = 3,
                mask = ".ca"
            },

            new()
            {
                name = ReloadBaseFunc,
                func = new(GetBaseLibMethod("BASE_reloadbase")),
                n_pchecks = -1,
                mask = ".s"
            },

            new()
            {
                name = string.Empty
            }
        };
        public static List<ExRegFunc> BaseFuncs => _exRegFuncs;

        private const string _reloadbase = "reload_base";
        public static string ReloadBaseFunc => _reloadbase;

        public static bool RegisterStdBase(ExVM vm, bool force = false)
        {
            ExAPI.PushRootTable(vm);
            ExAPI.RegisterNativeFunctions(vm, BaseFuncs, force);

            ExAPI.CreateConstantInt(vm, "_versionnumber_", 1, force);
            ExAPI.CreateConstantString(vm, "_version_", "ExMat v0.0.1", force);

            vm.Pop(1);

            return true;
        }
    }
}
