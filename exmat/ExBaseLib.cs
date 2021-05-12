using System;
using System.Collections.Generic;
using ExMat.VM;
using ExMat.API;
using ExMat.Objects;
using System.Reflection;
using ExMat.Utils;

namespace ExMat.BaseLib
{
    public static class ExBaseLib
    {
        public static int BASE_print(ExVM vm)
        {
            string s = string.Empty;
            ExAPI.ToString(vm, 2);
            ExAPI.GetString(vm, -1, ref s);

            Console.Write(s);
            return 0;
        }
        public static int BASE_printl(ExVM vm)
        {
            string s = string.Empty;
            ExAPI.ToString(vm, 2);
            ExAPI.GetString(vm, -1, ref s);

            Console.WriteLine(s);
            return 0;
        }

        public static int BASE_type(ExVM vm)
        {
            ExObjectPtr o = ExAPI.GetFromStack(vm, 2);
            string t = o._type.ToString();
            vm.Push(new ExObjectPtr(t));
            return 1;
        }
        public static int BASE_string(ExVM vm)
        {
            ExAPI.ToString(vm, 2);

            return 1;
        }
        public static int BASE_list(ExVM vm)
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
        public static int BASE_range(ExVM vm)
        {
            ExList l = new();
            ExObjectPtr s = ExAPI.GetFromStack(vm, 2);
            if (ExAPI.GetTopOfStack(vm) > 3)    // range(x,y,z)
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

            }
            else if (ExAPI.GetTopOfStack(vm) > 2) // range(x,y)
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
            }
            else // range(x)
            {
                float end = s.GetFloat();
                l._val.l_List = new();

                int count = (int)end;
                for (int i = 0; i < count; i++)
                {
                    l._val.l_List.Add(new(i));
                }
            }
            vm.Push(l);
            return 1;
        }

        private static readonly List<ExRegFunc> _exRegFuncs = new()
        {
            new() { name = "print", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_print")), n_pchecks = 2, mask = null },
            new() { name = "printl", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_printl")), n_pchecks = 2, mask = null },
            new() { name = "type", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_type")), n_pchecks = 2, mask = null },
            new() { name = "string", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_string")), n_pchecks = 2, mask = null },
            new() { name = "list", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_list")), n_pchecks = 2, mask = ".n" },
            new() { name = "range", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_range")), n_pchecks = 2, mask = ".n" },

            new() { name = string.Empty }
        };
        public static List<ExRegFunc> BaseFuncs { get => _exRegFuncs; }

        public static void RegisterBase(ExVM vm)
        {
            int i = 0;
            ExAPI.PushRootTable(vm);

            while (BaseFuncs[i].name != string.Empty)
            {
                ExAPI.PushString(vm, BaseFuncs[i].name, -1);
                ExAPI.CreateClosure(vm, BaseFuncs[i].func, 0);
                ExAPI.SetNativeClosureName(vm, -1, BaseFuncs[i].name);
                ExAPI.SetParamCheck(vm, BaseFuncs[i].n_pchecks, BaseFuncs[i].mask);
                ExAPI.CreateNewSlot(vm, -3, false);
                i++;
            }

            ExAPI.PushString(vm, "_versionnumber_", -1);
            ExAPI.PushInt(vm, 1);
            ExAPI.CreateNewSlot(vm, -3, false);
            ExAPI.PushString(vm, "_version_", -1);
            ExAPI.PushString(vm, "ExMat v0.0.1", -1);
            ExAPI.CreateNewSlot(vm, -3, false);
            vm.Pop(1);
        }
    }
}
