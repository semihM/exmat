using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ExMat.API;
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
                maxdepth = ExAPI.GetFromStack(vm, 3).GetInt();
                maxdepth = maxdepth < 1 ? 1 : maxdepth;
            }
            string s = string.Empty;
            ExAPI.ToString(vm, 2, maxdepth);
            if (!ExAPI.GetString(vm, -1, ref s))
            {
                return -1;
            }

            Console.Write(s);
            return 0;
        }
        public static int BASE_printl(ExVM vm, int nargs)
        {
            int maxdepth = 2;
            if (nargs == 2)
            {
                maxdepth = ExAPI.GetFromStack(vm, 3).GetInt();
                maxdepth = maxdepth < 1 ? 1 : maxdepth;
            }

            string s = string.Empty;
            ExAPI.ToString(vm, 2, maxdepth);
            if (!ExAPI.GetString(vm, -1, ref s))
            {
                return -1;
            }

            Console.WriteLine(s);
            return 0;
        }

        public static int BASE_type(ExVM vm, int nargs)
        {
            ExObjectPtr o = ExAPI.GetFromStack(vm, 2);
            string t = o._type.ToString();
            vm.Push(new ExObjectPtr(t));
            return 1;
        }

        public static int BASE_time(ExVM vm, int nargs)
        {
            vm.Push(new ExObjectPtr((float)(DateTime.Now - vm.StartingTime).TotalMilliseconds));
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
                        List<ExObjectPtr> res = new(splt.Length);
                        foreach (string arg in splt)
                        {
                            switch (arg.ToLower())
                            {
                                case "today":
                                    {
                                        res.Add(new ExObjectPtr(shrt ? today.ToShortDateString() : today.ToLongDateString()));
                                        break;
                                    }
                                case "now":
                                case "time":
                                    {
                                        res.Add(new ExObjectPtr(shrt ? now.ToShortTimeString() : now.ToLongTimeString()));
                                        break;
                                    }
                                case "year":
                                    {
                                        res.Add(new ExObjectPtr(now.Month.ToString()));
                                        break;
                                    }
                                case "month":
                                    {
                                        res.Add(new ExObjectPtr(now.Month.ToString()));
                                        break;
                                    }
                                case "day":
                                case "wday":
                                    {
                                        res.Add(new ExObjectPtr(now.DayOfWeek.ToString()));
                                        break;
                                    }
                                case "mday":
                                    {
                                        res.Add(new ExObjectPtr(now.Day.ToString()));
                                        break;
                                    }
                                case "yday":
                                    {
                                        res.Add(new ExObjectPtr(now.DayOfYear.ToString()));
                                        break;
                                    }
                                case "hours":
                                case "hour":
                                case "hh":
                                case "h":
                                    {
                                        res.Add(new ExObjectPtr(now.Hour.ToString()));
                                        break;
                                    }
                                case "minutes":
                                case "minute":
                                case "min":
                                case "mm":
                                case "m":
                                    {
                                        res.Add(new ExObjectPtr(now.Minute.ToString()));
                                        break;
                                    }
                                case "seconds":
                                case "second":
                                case "sec":
                                case "ss":
                                case "s":
                                    {
                                        res.Add(new ExObjectPtr(now.Second.ToString()));
                                        break;
                                    }
                                case "miliseconds":
                                case "milisecond":
                                case "ms":
                                    {
                                        res.Add(new ExObjectPtr(now.Millisecond.ToString()));
                                        break;
                                    }
                                case "utc-today":
                                    {
                                        res.Add(new ExObjectPtr(shrt ? utcnow.ToShortDateString() : utcnow.ToLongDateString()));
                                        break;
                                    }
                                case "utc-now":
                                case "utc-time":
                                    {
                                        res.Add(new ExObjectPtr(shrt ? utcnow.ToShortTimeString() : utcnow.ToLongTimeString()));
                                        break;
                                    }
                                case "utc-year":
                                    {
                                        res.Add(new ExObjectPtr(utcnow.Month.ToString()));
                                        break;
                                    }
                                case "utc-month":
                                    {
                                        res.Add(new ExObjectPtr(utcnow.Month.ToString()));
                                        break;
                                    }
                                case "utc-day":
                                case "utc-wday":
                                    {
                                        res.Add(new ExObjectPtr(utcnow.DayOfWeek.ToString()));
                                        break;
                                    }
                                case "utc-mday":
                                    {
                                        res.Add(new ExObjectPtr(utcnow.Day.ToString()));
                                        break;
                                    }
                                case "utc-yday":
                                    {
                                        res.Add(new ExObjectPtr(utcnow.DayOfYear.ToString()));
                                        break;
                                    }
                                case "utc-hh":
                                case "utc-hours":
                                case "utc-hour":
                                    {
                                        res.Add(new ExObjectPtr(utcnow.Hour.ToString()));
                                        break;
                                    }
                            }
                        }
                        if (res.Count == 1)
                        {
                            vm.Push(new ExObjectPtr(res[0]));
                        }
                        else
                        {
                            vm.Push(new ExObjectPtr(res));
                        }
                        break;
                    }
                default:
                    {
                        vm.Push(new ExObjectPtr(new List<ExObjectPtr>() { new(DateTime.Today.ToLongDateString()), new(now.ToLongTimeString()), new(now.Millisecond.ToString()) }));
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
                    return 0;
                }
                else
                {
                    vm.AddToErrorMessage("ASSERT FAILED: " + ExAPI.GetFromStack(vm, 3).GetString());
                    return -1;
                }
            }
            return ExAPI.GetFromStack(vm, 2).GetBool() ? 0 : -1;
        }

        // BASIC CLASS-LIKE FUNCTIONS
        public static int BASE_bool(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 1:
                    {
                        vm.Push(ExAPI.GetFromStack(vm, 2).GetBool());
                        break;
                    }
                case 0:
                    {
                        vm.Push(true);
                        break;
                    }
            }

            return 1;
        }

        public static int BASE_string(ExVM vm, int nargs)
        {
            bool carr = false;
            switch (nargs)
            {
                case 2:
                    {
                        carr = ExAPI.GetFromStack(vm, 3).GetBool();
                        goto case 1;
                    }
                case 1:
                    {
                        ExObjectPtr obj = ExAPI.GetFromStack(vm, 2);
                        if (obj._type == ExObjType.ARRAY && carr)
                        {
                            string str = string.Empty;
                            foreach (ExObjectPtr o in obj.GetList())
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

                            vm.Push(str);
                            break;
                        }
                        else if (!ExAPI.ToString(vm, 2))
                        {
                            return -1;
                        }
                        break;
                    }
                case 0:
                    {
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
                        if (!ExAPI.ToFloat(vm, 2))
                        {
                            return -1;
                        }
                        break;
                    }
                case 0:
                    {
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
                        if (!ExAPI.ToInteger(vm, 2))
                        {
                            return -1;
                        }
                        break;
                    }
                case 0:
                    {
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
                        ExObjectPtr v = ExAPI.GetFromStack(vm, 2);
                        int b = 0;
                        switch (v._type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    b = v.GetInt();
                                    goto default;
                                }
                            case ExObjType.FLOAT:
                                {
                                    b = new FloatInt() { f = v.GetFloat() }.i;
                                    goto default;
                                }
                            default:
                                {
                                    List<ExObjectPtr> l = new(32);

                                    for (int i = 0; i < 32; i++)
                                    {
                                        l.Add(new((b >> i) % 2 == 0 ? 0 : 1));
                                    }

                                    if (reverse)
                                    {
                                        l.Reverse();
                                    }

                                    vm.Push(l);
                                    break;
                                }
                        }
                        break;
                    }
                case 0:
                    {
                        vm.Push(new ExList());
                        break;
                    }
            }

            return 1;
        }

        public static int BASE_bytes(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 1:
                    {
                        ExObjectPtr v = ExAPI.GetFromStack(vm, 2);
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
                                    List<ExObjectPtr> b = new(chars.Length);
                                    foreach (char i in chars)
                                    {
                                        b.Add(new(i));
                                    }
                                    vm.Push(b);
                                    break;
                                }
                            default:
                                {
                                    List<ExObjectPtr> b = new(bytes.Length);
                                    foreach (byte i in bytes)
                                    {
                                        b.Add(new(i));
                                    }
                                    vm.Push(b);
                                    break;
                                }
                        }
                        break;
                    }
                case 0:
                    {
                        vm.Push(new ExList());
                        break;
                    }
            }

            return 1;
        }

        public static int BASE_map(ExVM vm, int nargs)
        {
            ExObjectPtr cls = ExAPI.GetFromStack(vm, 2);
            ExObjectPtr obj = new(ExAPI.GetFromStack(vm, 3));
            List<ExObjectPtr> l = new(obj._val.l_List.Count);

            int n = 0;
            bool need_b;
            bool iscls = cls._type == ExObjType.CLOSURE;

            if (iscls)
            {
                need_b = cls.GetClosure()._func.n_params > 1;
                vm.Pop();
            }
            else
            {
                if (cls.GetNClosure().b_deleg)
                {
                    need_b = cls.GetNClosure().n_paramscheck == 1;
                }
                else if (cls.GetNClosure().n_paramscheck > 0)
                {
                    need_b = cls.GetNClosure().n_paramscheck == 2;
                }
                else
                {
                    need_b = cls.GetNClosure().n_paramscheck == -1;
                }
            }

            foreach (ExObjectPtr o in obj._val.l_List)
            {
                if (iscls)  //TO-DO fix this mess
                {
                    vm.Push(vm.GetAbove(-2));
                    vm.Push(o);
                }
                else if (cls.GetNClosure().n_paramscheck == 1)
                {
                    vm.Push(o);
                    vm.Push(new ExObjectPtr());
                }
                else
                {
                    vm.Push(new ExObjectPtr());
                    vm.Push(o);
                }
                if (ExAPI.Call(vm, 3, true, need_b, iscls))
                {
                    l.Add(new(ExAPI.GetFromStack(vm, 4 - (iscls ? 1 : 0))));
                    vm.Pop();
                    n++;
                }
                else
                {
                    return -1;
                }
            }
            vm.Push(new ExObjectPtr(l));
            return 1;
        }

        public static int BASE_filter(ExVM vm, int nargs)
        {
            ExObjectPtr cls = ExAPI.GetFromStack(vm, 2);
            ExObjectPtr obj = new(ExAPI.GetFromStack(vm, 3));
            List<ExObjectPtr> l = new(obj._val.l_List.Count);

            int n = 0;
            bool need_b;
            bool iscls = cls._type == ExObjType.CLOSURE;

            if (iscls)
            {
                need_b = cls.GetClosure()._func.n_params > 1;
                vm.Pop();
            }
            else
            {
                if (cls.GetNClosure().b_deleg)
                {
                    need_b = cls.GetNClosure().n_paramscheck == 1;
                }
                else if (cls.GetNClosure().n_paramscheck > 0)
                {
                    need_b = cls.GetNClosure().n_paramscheck == 2;
                }
                else
                {
                    need_b = cls.GetNClosure().n_paramscheck == -1;
                }
            }

            foreach (ExObjectPtr o in obj._val.l_List)
            {
                if (iscls)  //TO-DO fix this mess
                {
                    vm.Push(vm.GetAbove(-2));
                    vm.Push(o);
                }
                else if (cls.GetNClosure().n_paramscheck == 1)
                {
                    vm.Push(o);
                    vm.Push(new ExObjectPtr());
                }
                else
                {
                    vm.Push(new ExObjectPtr());
                    vm.Push(o);
                }
                if (ExAPI.Call(vm, 3, true, need_b, iscls))
                {
                    if (ExAPI.GetFromStack(vm, 4 - (iscls ? 1 : 0)).GetBool())
                    {
                        l.Add(o);
                    }
                    vm.Pop();
                    n++;
                }
                else
                {
                    return -1;
                }
            }
            vm.Push(new ExObjectPtr(l));
            return 1;
        }

        public static int BASE_call(ExVM vm, int nargs)
        {
            ExObjectPtr cls = ExAPI.GetFromStack(vm, 2);

            ExObjectPtr res = new();

            bool need_b = false;
            bool iscls = cls._type == ExObjType.CLOSURE;

            if (!iscls && cls._type != ExObjType.NATIVECLOSURE)
            {
                vm.AddToErrorMessage("can't call non-closure type");
                return -1;
            }

            List<ExObjectPtr> args = new();


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
            if (ExAPI.Call(vm, 3, true, need_b, iscls, nargs))
            {
                res.Assign(vm.GetAbove(-1)); // ExAPI.GetFromStack(vm, nargs - (iscls ? 1 : 0))
                vm.Pop();
            }
            else
            {
                return -1;
            }

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
            ExObjectPtr cls = ExAPI.GetFromStack(vm, 2);
            List<ExObjectPtr> args = new ExObjectPtr(ExAPI.GetFromStack(vm, 3))._val.l_List;
            if(args.Count > vm._stack.Count - vm._top - 2)
            {
                vm.AddToErrorMessage("stack size is too small for parsing " + args.Count + " arguments! Current size: " + vm._stack.Count);
                return -1;
            }

            vm.Pop();

            nargs = args.Count + 1;

            ExObjectPtr res = new();

            bool need_b = false;
            bool iscls = cls._type == ExObjType.CLOSURE;

            if (!iscls && cls._type != ExObjType.NATIVECLOSURE)
            {
                vm.AddToErrorMessage("can't call non-closure type");
                return -1;
            }

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
            }

            vm.PushParse(args);

            nargs = args.Count + 1;

            if (ExAPI.Call(vm, 3, true, need_b, iscls, nargs))
            {
                res.Assign(vm.GetAbove(-1));
                vm.Pop();
            }
            else
            {
                return -1;
            }

            vm.Push(res);
            return 1;
        }

        public static int BASE_list(ExVM vm, int nargs)
        {
            ExList l = new();
            ExObjectPtr s = ExAPI.GetFromStack(vm, 2);
            if (ExAPI.GetTopOfStack(vm) > 2)
            {
                l._val.l_List = new(s.GetInt());
                ExUtils.InitList(ref l._val.l_List, s.GetInt(), ExAPI.GetFromStack(vm, 3));
            }
            else
            {
                l._val.l_List = new(s.GetInt());
                ExUtils.InitList(ref l._val.l_List, s.GetInt());
            }
            vm.Push(l);
            return 1;
        }
        public static int BASE_range(ExVM vm, int nargs)
        {
            ExList l = new();
            ExObjectPtr s = ExAPI.GetFromStack(vm, 2);

            switch (nargs)
            {
                case 3:
                    {
                        float start = s.GetFloat();
                        float end = ExAPI.GetFromStack(vm, 3).GetFloat();
                        float step = ExAPI.GetFromStack(vm, 4).GetFloat();
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
                        float start = s.GetFloat();
                        float end = ExAPI.GetFromStack(vm, 3).GetFloat();
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
                        float end = s.GetFloat();
                        l._val.l_List = new();

                        int count = (int)end;
                        for (int i = 0; i < count; i++)
                        {
                            l._val.l_List.Add(new(i));
                        }

                        break;
                    }
            }
            vm.Push(l);
            return 1;
        }
        public static int BASE_matrix(ExVM vm, int nargs)
        {
            ExList l = new();
            ExObjectPtr s = ExAPI.GetFromStack(vm, 2);

            switch (nargs)
            {
                case 2:
                case 3:
                    {
                        int m = s.GetInt();
                        if (m < 0)
                        {
                            m = 0;
                        }

                        int n = ExAPI.GetFromStack(vm, 3).GetInt();
                        if (n < 0)
                        {
                            n = 0;
                        }

                        ExObjectPtr filler = nargs == 3 ? ExAPI.GetFromStack(vm, 4) : new();

                        l._val.l_List = new(m);

                        for (int i = 0; i < m; i++)
                        {
                            List<ExObjectPtr> lis = null;
                            ExUtils.InitList(ref lis, n, filler);
                            l._val.l_List.Add(new ExObjectPtr(lis));
                        }

                        break;
                    }
            }
            vm.Push(l);
            return 1;
        }

        // COMMON
        public static int BASE_default_length(ExVM vm, int nargs)
        {
            int size = -1;
            ExObjectPtr obj = ExAPI.GetFromStack(vm, 1);
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
            vm.Push(new ExObjectPtr(size));
            return 1;
        }

        // STRING
        public static int BASE_string_index_of(ExVM vm, int nargs)
        {
            ExObjectPtr res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.STRING, ref res);
            string sub = vm.GetAbove(-1).GetString();
            vm.Pop();
            vm.Push(res.GetString().IndexOf(sub));
            return 1;
        }

        public static int BASE_string_toupper(ExVM vm, int nargs)
        {
            ExObjectPtr obj = ExAPI.GetFromStack(vm, 1);
            vm.Push(obj.GetString().ToUpper());
            return 1;
        }
        public static int BASE_string_tolower(ExVM vm, int nargs)
        {
            ExObjectPtr obj = ExAPI.GetFromStack(vm, 1);
            vm.Push(obj.GetString().ToLower());
            return 1;
        }
        public static int BASE_string_reverse(ExVM vm, int nargs)
        {
            ExObjectPtr obj = ExAPI.GetFromStack(vm, 1);
            char[] ch = obj.GetString().ToCharArray();
            Array.Reverse(ch);
            vm.Push(new string(ch));
            return 1;
        }
        public static int BASE_string_replace(ExVM vm, int nargs)
        {
            ExObjectPtr obj = ExAPI.GetFromStack(vm, 1);
            ExObjectPtr old = ExAPI.GetFromStack(vm, 2);
            ExObjectPtr rep = ExAPI.GetFromStack(vm, 3);
            vm.Push(obj.GetString().Replace(old.GetString(), rep.GetString()));
            return 1;
        }
        public static int BASE_string_isAlphabetic(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 1).GetString();
            if (nargs == 1)
            {
                int n = ExAPI.GetFromStack(vm, 2).GetInt();
                if (n < 0 || n >= s.Length)
                {
                    vm.AddToErrorMessage("string can't be indexed with integer higher than it's length or negative");
                    return -1;
                }
                s = s[n].ToString();
            }
            vm.Push(Regex.IsMatch(s,"^[A-Za-z]+$"));
            return 1;
        }
        public static int BASE_string_isNumeric(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 1).GetString();
            if (nargs == 1)
            {
                int n = ExAPI.GetFromStack(vm, 2).GetInt();
                if (n < 0 || n >= s.Length)
                {
                    vm.AddToErrorMessage("string can't be indexed with integer higher than it's length or negative");
                    return -1;
                }
                s = s[n].ToString();
            }
            vm.Push(Regex.IsMatch(s, @"^\d+(\.\d+)?((E|e)(\+|\-)\d+)?$"));
            return 1;
        }
        public static int BASE_string_isAlphaNumeric(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 1).GetString();
            if (nargs == 1)
            {
                int n = ExAPI.GetFromStack(vm, 2).GetInt();
                if (n < 0 || n >= s.Length)
                {
                    vm.AddToErrorMessage("string can't be indexed with integer higher than it's length or negative");
                    return -1;
                }
                s = s[n].ToString();
            }
            vm.Push(Regex.IsMatch(s, "^[A-Za-z0-9]+$"));
            return 1;
        }
        public static int BASE_string_isLower(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 1).GetString();
            if (nargs == 1)
            {
                int n = ExAPI.GetFromStack(vm, 2).GetInt();
                if (n < 0 || n >= s.Length)
                {
                    vm.AddToErrorMessage("string can't be indexed with integer higher than it's length or negative");
                    return -1;
                }
                s = s[n].ToString();
            }
            foreach (char c in s)
            {
                if(!char.IsLower(c))
                {
                    vm.Push(false);
                    return 1;
                }
            }
            vm.Push(true && !string.IsNullOrEmpty(s));
            return 1;
        }
        public static int BASE_string_isUpper(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 1).GetString();
            if (nargs == 1)
            {
                int n = ExAPI.GetFromStack(vm, 2).GetInt();
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
                    vm.Push(false);
                    return 1;
                }
            }
            vm.Push(true && !string.IsNullOrEmpty(s));
            return 1;
        }
        public static int BASE_string_isWhitespace(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 1).GetString();
            if (nargs == 1)
            {
                int n = ExAPI.GetFromStack(vm, 2).GetInt();
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
                    vm.Push(false);
                    return 1;
                }
            }
            vm.Push(true && s.Length > 0);
            return 1;
        }
        public static int BASE_string_isSymbol(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 1).GetString();
            if(nargs == 1)
            {
                int n = ExAPI.GetFromStack(vm, 2).GetInt();
                if(n < 0 || n >= s.Length)
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
                    vm.Push(false);
                    return 1;
                }
            }
            vm.Push(true && !string.IsNullOrEmpty(s));
            return 1;
        }
        // ARRAY
        public static int BASE_array_append(ExVM vm, int nargs)
        {
            ExObjectPtr res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res);
            res._val.l_List.Add(new(vm.GetAbove(-1)));
            vm.Pop();
            return 1;
        }
        public static int BASE_array_extend(ExVM vm, int nargs)
        {
            ExObjectPtr res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res);
            res._val.l_List.AddRange(vm.GetAbove(-1)._val.l_List);
            vm.Pop();
            return 1;
        }
        public static int BASE_array_pop(ExVM vm, int nargs)
        {
            ExObjectPtr res = new();
            ExAPI.GetSafeObject(vm, 1, ExObjType.ARRAY, ref res);
            if (res._val.l_List.Count > 0)
            {
                vm.Push(res._val.l_List[^1]); // TO-DO make this optional
                res._val.l_List.RemoveAt(res._val.l_List.Count - 1);
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
            ExObjectPtr res = new();
            ExAPI.GetSafeObject(vm, 1, ExObjType.ARRAY, ref res);
            int newsize = ExAPI.GetFromStack(vm, 2).GetInt();
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
            ExObjectPtr res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res);
            using ExObjectPtr obj = new(vm.GetAbove(-1));
            vm.Pop();
            vm.Push(ExAPI.GetValueIndexFromArray(res._val.l_List, obj));
            return 1;
        }

        public static int BASE_array_reverse(ExVM vm, int nargs)
        {
            ExObjectPtr obj = ExAPI.GetFromStack(vm, 1);
            List<ExObjectPtr> lis = obj.GetList();
            List<ExObjectPtr> res = new(lis.Count);
            for (int i = lis.Count - 1; i >= 0; i--)
            {
                res.Add(new(lis[i]));
            }
            vm.Push(res);
            return 1;
        }
        // DICT
        public static int BASE_dict_has_key(ExVM vm, int nargs)
        {
            ExObjectPtr res = new();
            ExAPI.GetSafeObject(vm, -2, ExObjType.DICT, ref res);
            string key = vm.GetAbove(-1).GetString();
            vm.Pop();
            vm.Push(res._val.d_Dict.ContainsKey(key));
            return 1;
        }
        public static int BASE_dict_keys(ExVM vm, int nargs)
        {
            ExObjectPtr res = new();
            ExAPI.GetSafeObject(vm, -1, ExObjType.DICT, ref res);
            List<ExObjectPtr> keys = new(res.GetDict().Count);
            foreach (string key in res.GetDict().Keys)
            {
                keys.Add(new(key));
            }
            vm.Push(keys);
            return 1;
        }

        public static int BASE_dict_values(ExVM vm, int nargs)
        {
            ExObjectPtr res = new();
            ExAPI.GetSafeObject(vm, -1, ExObjType.DICT, ref res);
            List<ExObjectPtr> vals = new(res.GetDict().Count);
            foreach (ExObjectPtr val in res.GetDict().Values)
            {
                vals.Add(new(val));
            }
            vm.Push(vals);
            return 1;
        }
        //
        public static int BASE_reloadbase(ExVM vm, int nargs)
        {
            if (nargs == 1)
            {
                string name = ExAPI.GetFromStack(vm, 2).GetString();
                if (name == ReloadBaseFunc)
                {
                    return 0;
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
            return 0;
        }

        //
        private static readonly List<ExRegFunc> _exRegFuncs = new()
        {
            new() { name = "print", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_print")), n_pchecks = -2, mask = "..n" },
            new() { name = "printl", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_printl")), n_pchecks = -2, mask = "..n" },

            new() { name = "time", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_time")), n_pchecks = 1, mask = null },
            new() { name = "date", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_date")), n_pchecks = -1, mask = ".s." },

            new() { name = "type", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_type")), n_pchecks = 2, mask = null },
            new() { name = "assert", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_assert")), n_pchecks = -2, mask = "..s" },

            new() { name = "string", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_string")), n_pchecks = -1, mask = "..." },
            new() { name = "float", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_float")), n_pchecks = -1, mask = ".." },
            new() { name = "integer", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_integer")), n_pchecks = -1, mask = ".." },
            new() { name = "bool", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_bool")), n_pchecks = -1, mask = ".." },
            new() { name = "bits", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_bits")), n_pchecks = -1, mask = ".n." },
            new() { name = "bytes", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_bytes")), n_pchecks = -1, mask = ".n|s" },

            new() { name = "list", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_list")), n_pchecks = -1, mask = ".n." },
            new() { name = "range", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_range")), n_pchecks = -2, mask = ".nnn" },
            new() { name = "matrix", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_matrix")), n_pchecks = -3, mask = ".ii." },

            new() { name = "map", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_map")), n_pchecks = 3, mask = ".ca" },
            new() { name = "filter", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_filter")), n_pchecks = 3, mask = ".ca" },
            new() { name = "call", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_call")), n_pchecks = -2, mask = null },
            new() { name = "parse", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_parse")), n_pchecks = 3, mask = ".ca" },

            new() { name = ReloadBaseFunc, func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_reloadbase")), n_pchecks = -1, mask = ".s" },

            new() { name = string.Empty }
        };
        public static List<ExRegFunc> BaseFuncs { get => _exRegFuncs; }

        private const string _reloadbase = "reload_base";
        public static string ReloadBaseFunc { get => _reloadbase; }

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
