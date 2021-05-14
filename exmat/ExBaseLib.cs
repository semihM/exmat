using System;
using System.Collections.Generic;
using ExMat.API;
using ExMat.Objects;
using ExMat.Utils;
using ExMat.VM;

namespace ExMat.BaseLib
{
    public static class ExBaseLib
    {
        public static int BASE_print(ExVM vm, int nargs)
        {
            string s = string.Empty;
            ExAPI.ToString(vm, 2);
            if (!ExAPI.GetString(vm, -1, ref s))
            {
                return -1;
            }

            Console.Write(s);
            return 0;
        }
        public static int BASE_printl(ExVM vm, int nargs)
        {
            string s = string.Empty;
            ExAPI.ToString(vm, 2);
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
            vm.Push(new ExObjectPtr(DateTime.Now.Millisecond));
            return 1;
        }

        public static int BASE_string(ExVM vm, int nargs)
        {
            if (!ExAPI.ToString(vm, 2))
            {
                return -1;
            }

            return 1;
        }
        public static int BASE_float(ExVM vm, int nargs)
        {
            if (!ExAPI.ToFloat(vm, 2))
            {
                return -1;
            }

            return 1;
        }
        public static int BASE_integer(ExVM vm, int nargs)
        {
            if (!ExAPI.ToInteger(vm, 2))
            {
                return -1;
            }

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
            if (nargs == 3)    // range(x,y,z)
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
            else if (nargs == 2) // range(x,y)
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
            else if (nargs == 1)
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

        public static int BASE_array_append(ExVM vm, int nargs)
        {
            if (ExAPI.GetTopOfStack(vm) >= 2)
            {
                ExObjectPtr res = new();
                ExAPI.GetSafeObject(vm, -2, ExObjType.ARRAY, ref res);
                res._val.l_List.Add(new(vm.GetAbove(-1)));
                vm.Pop();
                return 0;
            }
            return -1;
        }
        public static int BASE_array_pop(ExVM vm, int nargs)
        {
            if (ExAPI.GetTopOfStack(vm) >= 1)
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
            return -1;
        }

        private static readonly List<ExRegFunc> _exRegFuncs = new()
        {
            new() { name = "print", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_print")), n_pchecks = 2, mask = null },
            new() { name = "printl", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_printl")), n_pchecks = 2, mask = null },
            new() { name = "type", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_type")), n_pchecks = 2, mask = null },
            new() { name = "time", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_time")), n_pchecks = 1, mask = null },
            new() { name = "string", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_string")), n_pchecks = -2, mask = null },
            new() { name = "float", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_float")), n_pchecks = -2, mask = null },
            new() { name = "integer", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_integer")), n_pchecks = -2, mask = null },
            new() { name = "list", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_list")), n_pchecks = -1, mask = ".n." },
            new() { name = "range", func = new(Type.GetType("ExMat.BaseLib.ExBaseLib").GetMethod("BASE_range")), n_pchecks = -2, mask = ".nnn" },

            new() { name = string.Empty }
        };
        public static List<ExRegFunc> BaseFuncs { get => _exRegFuncs; }

        public static void RegisterStdBase(ExVM vm)
        {
            ExAPI.PushRootTable(vm);
            ExAPI.RegisterNativeFunctions(vm, BaseFuncs);

            ExAPI.CreateConstantInt(vm, "_versionnumber_", 1);
            ExAPI.CreateConstantString(vm, "_version_", "ExMat v0.0.1");

            vm.Pop(1);
        }
    }
}
