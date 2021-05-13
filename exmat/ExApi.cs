using System;
using ExMat.VM;
using ExMat.Objects;
using ExMat.States;
using ExMat.Closure;
using ExMat.FuncPrototype;
using ExMat.Compiler;
using System.Collections.Generic;

namespace ExMat.API
{
    public static class ExAPI
    {
        public static void GetSafeObject(ExVM vm, int idx, ExObjType typ, ref ExObjectPtr res)
        {
            res = GetFromStack(vm, idx);
            if (res._type != typ)
            {
                throw new Exception("wrong argument type, expected " + typ.ToString() + " got " + res._type.ToString());
            }
        }

        public static void ToString(ExVM vm, int i)
        {
            ExObjectPtr o = GetFromStack(vm, i);
            ExObjectPtr res = new(string.Empty);
            vm.ToString(o, ref res);
            vm.Push(res);
        }

        public static void GetString(ExVM vm, int idx, ref string s)
        {
            ExObjectPtr o = new();
            GetSafeObject(vm, idx, ExObjType.STRING, ref o);
            s = o._val.s_String;
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
                o._val._NativeClosure._name = new(name);
            }
            else
            {
                throw new Exception("native closure expected");
            }
        }
        public enum ExTypeMask
        {
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
            ExNativeClosure nc = o._val._NativeClosure;
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

            if (n == int.MaxValue)
            {
                nc.n_paramscheck = nc._typecheck.Count;
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

    }
}
