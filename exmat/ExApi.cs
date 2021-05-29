using System;
using System.Collections.Generic;
using ExMat.Closure;
using ExMat.Compiler;
using ExMat.FuncPrototype;
using ExMat.Objects;
using ExMat.States;
using ExMat.VM;

namespace ExMat.API
{
    public static class ExAPI
    {
        public static bool GetSafeObject(ExVM vm, int idx, ExObjType typ, ref ExObject res)
        {
            res = GetFromStack(vm, idx);
            if (res._type != typ)
            {
                vm.AddToErrorMessage("wrong argument type, expected " + typ.ToString() + " got " + res._type.ToString());
                return false;
            }
            return true;
        }

        public static bool ToString(ExVM vm, int i, int maxdepth = 1, int pop = 0, bool beauty = false)
        {
            ExObject o = GetFromStack(vm, i);
            ExObject res = new(string.Empty);
            if (!vm.ToString(o, ref res, maxdepth, beauty: beauty))
            {
                return false;
            }
            vm.Pop(pop);
            vm.Push(res);
            return true;
        }

        public static bool ToFloat(ExVM vm, int i, int pop = 0)
        {
            ExObject o = GetFromStack(vm, i);
            ExObject res = null;
            if (!vm.ToFloat(o, ref res))
            {
                return false;
            }
            vm.Pop(pop);
            vm.Push(res);
            return true;
        }

        public static bool ToInteger(ExVM vm, int i, int pop = 0)
        {
            ExObject o = GetFromStack(vm, i);
            ExObject res = null;
            if (!vm.ToInteger(o, ref res))
            {
                return false;
            }
            vm.Pop(pop);
            vm.Push(res);
            return true;
        }

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

        public static ExRegFunc FindNativeFunction(ExVM vm, List<ExRegFunc> fs, string name)
        {
            foreach (ExRegFunc f in fs)
            {
                if (f.name == name && f.name != string.Empty)
                {
                    return f;
                }
            }
            return null;
        }

        public static bool ReloadNativeFunction(ExVM vm, List<ExRegFunc> fs, string name, bool force = false)
        {
            ExRegFunc r;
            if ((r = FindNativeFunction(vm, fs, name)) != null)
            {
                RegisterNativeFunction(vm, r, true);
                return true;
            }
            return false;
        }

        public static void RegisterNativeFunction(ExVM vm, ExRegFunc func, bool force = false)
        {
            PushString(vm, func.name, -1);
            CreateClosure(vm, func.func, 0, force);
            SetNativeClosureName(vm, -1, func.name);
            SetParamCheck(vm, func.n_pchecks, func.mask);
            SetDefaultValues(vm, func.d_defaults);
            CreateNewSlot(vm, -3, false);
        }

        public static void RegisterNativeFunctions(ExVM vm, List<ExRegFunc> funcs, bool force = false)
        {
            int i = 0;

            while (funcs[i].name != string.Empty)
            {
                RegisterNativeFunction(vm, funcs[i], force);
                i++;
            }
        }

        public static void CreateConstantInt(ExVM vm, string name, long val)
        {
            PushString(vm, name, -1);
            vm.Push(val);
            CreateNewSlot(vm, -3, false);
        }

        public static void CreateConstantFloat(ExVM vm, string name, double val)
        {
            PushString(vm, name, -1);
            vm.Push(val);
            CreateNewSlot(vm, -3, false);
        }

        public static void CreateConstantString(ExVM vm, string name, string val)
        {
            PushString(vm, name, -1);
            PushString(vm, val, -1);
            CreateNewSlot(vm, -3, false);
        }

        public static void CreateConstantSpace(ExVM vm, string name, ExSpace val)
        {
            PushString(vm, name, -1);
            vm.Push(new ExObject(val));
            CreateNewSlot(vm, -3, false);
        }

        public static void CreateConstantDict(ExVM vm, string name, Dictionary<string, ExObject> dict)
        {
            PushString(vm, name, -1);
            vm.Push(new ExObject(dict));
            CreateNewSlot(vm, -3, false);
        }

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
                if (!vm._sState._strings.ContainsKey(str))
                {
                    vm._sState._strings.Add(str, new(str));
                }
                else
                {
                    vm._sState._strings[str] = new(str);
                }
                vm.Push(str);
            }
        }
        public static void CreateClosure(ExVM vm, ExFunc f, int fvars, bool force = false)
        {
            ExNativeClosure nc = ExNativeClosure.Create(vm._sState, f, fvars);
            nc.n_paramscheck = 0;
            for (int i = 0; i < fvars; i++)
            {
                if (force)
                {
                    int idx;
                    if ((idx = nc._outervals.FindIndex((ExObject o) => o.GetNClosure()._name.GetString() == vm.Top().GetNClosure()._name.GetString())) != -1)
                    {
                        nc._outervals[idx].Assign(vm.Top());
                    }
                    else
                    {
                        nc._outervals.Add(new(vm.Top()));
                    }
                }
                else
                {
                    nc._outervals.Add(new(vm.Top()));
                }
                vm.Pop();
            }
            vm.Push(nc);
        }
        public static void SetNativeClosureName(ExVM vm, int id, string name)
        {
            ExObject o = GetFromStack(vm, id);
            if (o._type == ExObjType.NATIVECLOSURE)
            {
                o.GetNClosure()._name = new(name);
            }
            else
            {
                throw new Exception("native closure expected");
            }
        }

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

        public static bool CompileTypeMask(List<int> r, string mask)
        {
            int i;
            int m = 0;
            int l = mask.Length;
            for (i = 0; i < l;)
            {
                switch (mask[i])
                {
                    case ' ': i++; continue;
                    case '.': m = -1; r.Add(m); i++; m = 0; continue;
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
                    case 'u': m |= (int)ExBaseType.USERDATA; break;
                    case 'p': m |= (int)ExBaseType.USERPTR; break;
                    case 'x': m |= (int)ExBaseType.INSTANCE; break;
                    case 'y': m |= (int)ExBaseType.CLASS; break;
                    case 'w': m |= (int)ExBaseType.WEAKREF; break;
                    default: return false;
                }

                i++;
                if (i < l && mask[i] == '|')
                {
                    i++;
                    if (i == l)
                    {
                        return false;
                    }
                    continue;
                }
                r.Add(m);
                m = 0;
            }
            return true;
        }

        public static void SetParamCheck(ExVM vm, int n, string mask)
        {
            ExObject o = GetFromStack(vm, -1);

            if (o._type != ExObjType.NATIVECLOSURE)
            {
                throw new Exception("native closure expected");
            }
            ExNativeClosure nc = o.GetNClosure();
            nc.n_paramscheck = n;

            if (!string.IsNullOrEmpty(mask))
            {
                List<int> r = new();
                if (!CompileTypeMask(r, mask))
                {
                    throw new Exception("failed to compile type mask");
                }
                nc._typecheck = r;
            }
            else
            {
                nc._typecheck = new();
            }
        }
        public static void SetDefaultValues(ExVM vm, Dictionary<int, ExObject> d)
        {
            ExObject o = GetFromStack(vm, -1);

            if (o._type != ExObjType.NATIVECLOSURE)
            {
                throw new Exception("native closure expected");
            }
            o.GetNClosure().d_defaults = d;
        }
        public static void DoParamChecks(ExVM vm, int n)
        {
            if (GetTopOfStack(vm) < n)
            {
                throw new Exception("not enough params in stack");
            }
        }
        public static void CreateNewSlot(ExVM vm, int n, bool s)
        {
            DoParamChecks(vm, 3);
            ExObject self = GetFromStack(vm, n);
            if (self._type == ExObjType.DICT || self._type == ExObjType.CLASS)
            {
                ExObject k = new();
                k.Assign(vm.GetAbove(-2));
                if (k._type == ExObjType.NULL)
                {
                    throw new Exception("'null' is not a valid key");
                }
                vm.NewSlot(self, k, vm.GetAbove(-1), false);
                vm.Pop(2);
            }
        }

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

        public static int GetValueIndexFromArray(List<ExObject> lis, ExObject obj)
        {
            int i = 0;
            bool f = false;
            for (; i < lis.Count; i++)
            {
                ExVM.CheckEqual(lis[i], obj, ref f);
                if (f)
                {
                    return i;
                }
            }
            return -1;
        }

        public static bool CompileFile(ExVM vm, string _source)
        {
            ExCompiler c = new();
            ExObject o = new();

            if (c.Compile(vm, _source, ref o))
            {
                ExClosure cls = ExClosure.Create(vm._sState, o._val._FuncPro);
                vm.Push(cls);
                return true;
            }
            vm._error = c._error;
            return false;
        }

        public static void PushRootTable(ExVM vm)
        {
            vm.Push(vm._rootdict);
        }
        public static void PushConstTable(ExVM vm)
        {
            vm.Push(vm._sState._constdict);
        }
        public static void PushRegTable(ExVM vm)
        {
            vm.Push(vm._sState._reg);
        }

        public static ExObject GetFromStack(ExVM vm, int i)
        {
            return i >= 0 ? vm.GetAt(i + vm._stackbase - 1) : vm.GetAbove(i);
        }
        public static int GetTopOfStack(ExVM vm)
        {
            return vm._top - vm._stackbase;
        }

        public static bool Call(ExVM vm, int pcount, bool ret, bool force = false)
        {
            ExObject res = new();
            ExObject tmp = vm.GetAbove(-(pcount + 1));
            if (vm.Call(ref tmp, pcount, vm._top - pcount, ref res, force))
            {
                vm.Pop(pcount);
                if (ret)
                {
                    vm.Push(res);
                }
                return true;
            }
            return false;
        }

        public static bool Call(ExVM vm, int pcount, bool ret, bool b_p = false, bool cls = false, int nargs = 2)
        {
            ExObject res = new();
            ExObject tmp = vm.GetAbove(-(nargs) + (cls ? -1 : 0));
            if (cls)
            {
                int top = vm._top;
                vm._top = vm._stackbase + tmp.GetClosure()._func._stacksize + 2;
                if (vm.Call(ref tmp,
                        nargs - (b_p
                                    ? (cls
                                            ? 0
                                            : (tmp.GetNClosure().n_paramscheck == 1
                                                ? 1
                                                : 0))
                                    : 0),
                        vm._stackbase + 2, // vm._top - nargs + (cls ? (parsing ? -2 : 0)  : 0 ), //+ ,
                        ref res,
                        cls))
                {
                    while (vm._top >= top)
                    {
                        vm.Pop();
                    }
                    if (ret)
                    {
                        vm.Push(res);
                    }
                    vm._top = top;
                    return true;
                }
            }
            else
            {
                if (vm.Call(ref tmp,
                        nargs - (b_p
                                    ? ((tmp.GetNClosure().n_paramscheck == 1
                                                ? 1
                                                : 0))
                                    : 0),
                        vm._stackbase + 1, // vm._top - nargs + (cls ? (parsing ? -2 : 0)  : 0 ), //+ ,
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

        public static List<ExObject> TransposeMatrix(int rows, int cols, List<ExObject> vals)
        {
            List<ExObject> lis = new(cols);
            for (int i = 0; i < cols; i++)
            {
                lis.Add(new ExObject(new List<ExObject>(rows)));
                for (int j = 0; j < rows; j++)
                {
                    lis[i]._val.l_List.Add(vals[j]._val.l_List[i]);
                }
            }
            return lis;
        }

        public static bool DoMatrixTransposeChecks(ExVM vm, List<ExObject> vals, ref int cols)
        {
            foreach (ExObject row in vals)
            {
                if (row._type != ExObjType.ARRAY)
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

        public static void CollectGarbage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        // VM START
        public static ExVM Start(int stacksize, bool interacive = false)
        {
            ExSState exS = new();
            exS.Initialize();
            ExVM vm = new() { _sState = exS };

            exS._rootVM = vm;

            vm.Initialize(stacksize);
            vm.isInteractive = interacive;
            return vm;
        }

        public static void WriteErrorMessages(ExVM vm, string typ)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("\n\n*******************************");
            switch (typ)
            {
                case "COMPILE":
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine("FAILED TO COMPILE");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(vm._error);
                        break;
                    }
                case "EXECUTE":
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.WriteLine("FAILED TO EXECUTE");
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine(vm._error);
                        break;
                    }
            }

            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("*******************************");
            Console.ResetColor();

            vm._error = string.Empty;
        }
    }
}
