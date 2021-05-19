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
        public static bool GetSafeObject(ExVM vm, int idx, ExObjType typ, ref ExObjectPtr res)
        {
            res = GetFromStack(vm, idx);
            if (res._type != typ)
            {
                vm.AddToErrorMessage("wrong argument type, expected " + typ.ToString() + " got " + res._type.ToString());
                return false;
            }
            return true;
        }

        public static bool ToString(ExVM vm, int i)
        {
            ExObjectPtr o = GetFromStack(vm, i);
            ExObjectPtr res = new(string.Empty);
            if (!vm.ToString(o, ref res))
            {
                return false;
            }
            vm.Push(res);
            return true;
        }

        public static bool ToFloat(ExVM vm, int i)
        {
            ExObjectPtr o = GetFromStack(vm, i);
            ExObjectPtr res = null;
            if (!vm.ToFloat(o, ref res))
            {
                return false;
            }
            vm.Push(res);
            return true;
        }

        public static bool ToInteger(ExVM vm, int i)
        {
            ExObjectPtr o = GetFromStack(vm, i);
            ExObjectPtr res = null;
            if (!vm.ToInteger(o, ref res))
            {
                return false;
            }
            vm.Push(res);
            return true;
        }

        public static bool GetString(ExVM vm, int idx, ref string s)
        {
            ExObjectPtr o = new();
            if (!GetSafeObject(vm, idx, ExObjType.STRING, ref o))
            {
                return false;
            }
            s = o.GetString();
            return true;
        }

        public static void RegisterNativeFunctions(ExVM vm, List<ExRegFunc> funcs)
        {
            int i = 0;

            while (funcs[i].name != string.Empty)
            {
                PushString(vm, funcs[i].name, -1);
                CreateClosure(vm, funcs[i].func, 0);
                SetNativeClosureName(vm, -1, funcs[i].name);
                SetParamCheck(vm, funcs[i].n_pchecks, funcs[i].mask);
                CreateNewSlot(vm, -3, false);
                i++;
            }
        }

        public static void CreateConstantInt(ExVM vm, string name, int val)
        {
            PushString(vm, name, -1);
            PushInt(vm, val);
            CreateNewSlot(vm, -3, false);
        }

        public static void CreateConstantFloat(ExVM vm, string name, float val)
        {
            PushString(vm, name, -1);
            PushFloat(vm, val);
            CreateNewSlot(vm, -3, false);
        }

        public static void CreateConstantString(ExVM vm, string name, string val)
        {
            PushString(vm, name, -1);
            PushString(vm, val, -1);
            CreateNewSlot(vm, -3, false);
        }

        public static void PushBool(ExVM vm, bool b)
        {
            vm.Push(new ExBool(b));
        }
        public static void PushFloat(ExVM vm, float f)
        {
            vm.Push(new ExFloat(f));
        }
        public static void PushInt(ExVM vm, int n)
        {
            vm.Push(new ExInt(n));
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
                vm._sState._strings.Add(str, new(str));
                vm.Push(new ExString(str));
            }
        }
        public static void CreateClosure(ExVM vm, ExFunc f, int fvars)
        {
            ExNativeClosure nc = ExNativeClosure.Create(vm._sState, f, fvars);
            nc.n_paramscheck = 0;
            for (int i = 0; i < fvars; i++)
            {
                nc._outervals.Add(vm.Top());
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
        public enum ExTypeMask
        {
        }

        public static string GetExpectedTypes(int mask)
        {
            List<string> names = new();
            for (int i = 0; i < 19; i++)
            {
                if (((mask >> i) % 2) == 1)
                {
                    names.Add(((ExRawType)(1 << i)).ToString());
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
                    case 'e': m |= (int)ExRawType.NULL; break;
                    case 'i': m |= (int)ExRawType.INTEGER; break;
                    case 'f': m |= (int)ExRawType.FLOAT; break;
                    case 'b': m |= (int)ExRawType.BOOL; break;
                    case 'n': m |= (int)ExRawType.INTEGER | (int)ExRawType.FLOAT; break;
                    case 's': m |= (int)ExRawType.STRING; break;
                    case 'd': m |= (int)ExRawType.DICT; break;
                    case 'a': m |= (int)ExRawType.ARRAY; break;
                    case 'c': m |= (int)ExRawType.CLOSURE | (int)ExRawType.NATIVECLOSURE; break;
                    case 'u': m |= (int)ExRawType.USERDATA; break;
                    case 'p': m |= (int)ExRawType.USERPTR; break;
                    case 'x': m |= (int)ExRawType.INSTANCE; break;
                    case 'y': m |= (int)ExRawType.CLASS; break;
                    case 'w': m |= (int)ExRawType.WEAKREF; break;
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
            ExObjectPtr self = GetFromStack(vm, n);
            if (self._type == ExObjType.DICT || self._type == ExObjType.CLASS)
            {
                ExObjectPtr k = new();
                k.Assign(vm.GetAbove(-2));
                if (k._type == ExObjType.NULL)
                {
                    throw new Exception("'null' is not a valid key");
                }
                vm.NewSlot(self, k, vm.GetAbove(-1), false);
                vm.Pop(2);
            }
        }

        public static int GetValueIndexFromArray(List<ExObjectPtr> lis, ExObjectPtr obj)
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
            ExObjectPtr o = new();

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

        public static ExObjectPtr GetFromStack(ExVM vm, int i)
        {
            return i >= 0 ? vm.GetAt(i + vm._stackbase - 1) : vm.GetAbove(i);
        }
        public static int GetTopOfStack(ExVM vm)
        {
            return vm._top - vm._stackbase;
        }

        public static bool Call(ExVM vm, int pcount, bool ret)
        {
            ExObjectPtr res = new();
            ExObjectPtr tmp = vm.GetAbove(-(pcount + 1));
            if (vm.Call(ref tmp, pcount, vm._top - pcount, ref res))
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
            ExObjectPtr res = new();
            ExObjectPtr tmp = vm.GetAbove(-(nargs) + (cls ? -1 : 0));
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

        public static bool Call(ExVM vm, int pcount, bool ret, bool parsing = false, bool cls = false)
        {
            ExObjectPtr res = new();
            ExObjectPtr tmp = vm.GetAbove(-(pcount + 1 - (parsing && cls ? 1 : 0)));
            if (vm.Call(ref tmp, pcount - (parsing ? (cls ? 1 : (tmp.GetNClosure().n_paramscheck == 1 ? 2 : 1)) : 0), vm._top - pcount + (parsing ? 1 : 0), ref res, cls))
            {
                vm.Pop(pcount - (parsing ? 1 : 0));
                if (ret)
                {
                    vm.Push(res);
                }
                return true;
            }
            return false;
        }

        // VM START
        public static ExVM Start(int stacksize)
        {
            ExSState exS = new();
            exS.Initialize();
            ExVM vm = new() { _sState = exS };

            exS._rootVM = vm;

            vm.Initialize(stacksize);
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
