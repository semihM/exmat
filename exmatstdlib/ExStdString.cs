using System;
using System.Collections.Generic;
using System.Reflection;
using ExMat.API;
using ExMat.Lexer;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.BaseLib
{
    public static class ExStdString
    {
        public static int STRING_strip(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 2).GetString();
            vm.Pop(nargs + 2);
            vm.Push(s.Trim());
            return 1;
        }
        public static int STRING_lstrip(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 2).GetString();
            vm.Pop(nargs + 2);
            vm.Push(s.TrimStart());
            return 1;
        }

        public static int STRING_rstrip(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 2).GetString();
            vm.Pop(nargs + 2);
            vm.Push(s.TrimEnd());
            return 1;
        }

        public static int STRING_split(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 2).GetString();
            string c = ExAPI.GetFromStack(vm, 3).GetString();
            StringSplitOptions remove_empty = nargs == 3
                ? (ExAPI.GetFromStack(vm, 4).GetBool()
                    ? StringSplitOptions.RemoveEmptyEntries
                    : StringSplitOptions.None)
                : StringSplitOptions.None;

            string[] arr = s.Split(c, remove_empty);

            List<ExObject> lis = new(arr.Length);
            for (int i = 0; i < arr.Length; i++)
            {
                lis.Add(new(arr[i]));
            }

            vm.Pop(nargs + 2);
            vm.Push(new ExObject(lis));
            return 1;
        }

        public static int STRING_join(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 2).GetString();
            List<ExObject> lis = ExAPI.GetFromStack(vm, 3).GetList();

            int depth = 2;
            if (nargs == 3)
            {
                depth = (int)ExAPI.GetFromStack(vm, 4).GetInt();
                if (depth <= 0)
                {
                    depth = 1;
                }
            }

            int n = lis != null ? lis.Count : 0;
            string res = string.Empty;

            for (int i = 0; i < n; i++)
            {
                ExObject str = new();
                if (vm.ToString(lis[i], ref str, depth))
                {
                    res += str.GetString();
                }
                else
                {
                    return -1;
                }
                if (i != n - 1)
                {
                    res += s;
                }
            }

            vm.Pop(nargs + 2);
            vm.Push(res);
            return 1;
        }

        public static void ReplaceMacroParams(ExMacro m, List<ExObject> args)
        {
            string[] lines = m.source.Split('\n');
            for (int i = 0; i < m._params.Count; i++)
            {
                ExMacroParam p = m._params[i];
                string val = args[i].GetString();
                for (int j = 0; j < p.lines.Count; j++)
                {
                    lines[p.lines[j]] = lines[p.lines[j]].Substring(0, p.cols[j]) + val + lines[p.lines[j]].Substring(p.cols[j] + p.name.Length + 4, lines[p.lines[j]].Length);
                }
            }
        }

        public static int STRING_compile(ExVM vm, int nargs)
        {
            string code = ExAPI.GetFromStack(vm, 2).GetString();

            if (ExAPI.CompileFile(vm, code))
            {
                ExObject m = new(vm.GetAbove(-1));
                vm.Pop(nargs + 3);
                vm.Push(m);
                return 1;
            }

            return -1;
        }

        public static int STRING_format(ExVM vm, int nargs)
        {
            string format = ExAPI.GetFromStack(vm, 2).GetString();
            object[] ps = new object[nargs - 1];
            ExObject[] args = ExAPI.GetNObjects(vm, nargs - 1, 3);
            for (int i = 0; i < nargs - 1; i++)
            {
                ExObject st = new();
                if (vm.ToString(args[i], ref st))
                {
                    ps[i] = st.GetString();
                }
                else
                {
                    return -1;
                }
            }

            try
            {
                vm.Pop(nargs + 2);
                vm.Push(string.Format(format, ps));
                return 1;
            }
            catch
            {
                vm.AddToErrorMessage("not enough arguments given for format string");
                return -1;
            }
        }

        public static MethodInfo GetStdStringMethod(string name)
        {
            return Type.GetType("ExMat.BaseLib.ExStdString").GetMethod(name);
        }
        private static readonly List<ExRegFunc> _stdstrfuncs = new()
        {
            new()
            {
                name = "compile",
                func = new(GetStdStringMethod("STRING_compile")),
                n_pchecks = 2,
                mask = ".s"
            },

            new()
            {
                name = "strip",
                func = new(GetStdStringMethod("STRING_strip")),
                n_pchecks = 2,
                mask = ".s"
            },
            new()
            {
                name = "lstrip",
                func = new(GetStdStringMethod("STRING_lstrip")),
                n_pchecks = 2,
                mask = ".s"
            },
            new()
            {
                name = "rstrip",
                func = new(GetStdStringMethod("STRING_rstrip")),
                n_pchecks = 2,
                mask = ".s"
            },
            new()
            {
                name = "split",
                func = new(GetStdStringMethod("STRING_split")),
                n_pchecks = -3,
                mask = ".ssb",
                d_defaults = new()
                {
                    { 3, new(false) }
                }
            },
            new()
            {
                name = "join",
                func = new(GetStdStringMethod("STRING_join")),
                n_pchecks = -3,
                mask = ".sai",
                d_defaults = new()
                {
                    { 3, new(2) }
                }
            },
            new()
            {
                name = "format",
                func = new(GetStdStringMethod("STRING_format")),
                n_pchecks = -2,
                mask = null
            },

            new() { name = string.Empty }
        };
        public static List<ExRegFunc> StringFuncs { get => _stdstrfuncs; }

        public static bool RegisterStdString(ExVM vm, bool force = false)
        {
            ExAPI.RegisterNativeFunctions(vm, StringFuncs, force);
            return true;
        }
    }
}
