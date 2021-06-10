using System;
using System.Collections.Generic;
using ExMat.API;
using ExMat.Lexer;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.BaseLib
{
    public static class ExStdString
    {
        public static int Strip(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 2).GetString();
            vm.Pop(nargs + 2);
            vm.Push(s.Trim());
            return 1;
        }
        public static int Lstrip(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 2).GetString();
            vm.Pop(nargs + 2);
            vm.Push(s.TrimStart());
            return 1;
        }

        public static int Rstrip(ExVM vm, int nargs)
        {
            string s = ExAPI.GetFromStack(vm, 2).GetString();
            vm.Pop(nargs + 2);
            vm.Push(s.TrimEnd());
            return 1;
        }

        public static int Split(ExVM vm, int nargs)
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

        public static int Join(ExVM vm, int nargs)
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
            string[] lines = m.Source.Split('\n');
            for (int i = 0; i < m.Parameters.Count; i++)
            {
                ExMacroParam p = m.Parameters[i];
                string val = args[i].GetString();
                for (int j = 0; j < p.Lines.Count; j++)
                {
                    lines[p.Lines[j]] = lines[p.Lines[j]].Substring(0, p.Columns[j]) + val + lines[p.Lines[j]].Substring(p.Columns[j] + p.Name.Length + 4, lines[p.Lines[j]].Length);
                }
            }
        }

        public static int Compile(ExVM vm, int nargs)
        {
            string code = ExAPI.GetFromStack(vm, 2).GetString();

            if (ExAPI.CompileSource(vm, code))
            {
                ExObject m = new(vm.GetAbove(-1));
                vm.Pop(nargs + 3);
                vm.Push(m);
                return 1;
            }

            return -1;
        }

        public static int Format(ExVM vm, int nargs)
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

        private static readonly List<ExRegFunc> _stdstrfuncs = new()
        {
            new()
            {
                Name = "compile",
                Function = Compile,
                nParameterChecks = 2,
                ParameterMask = ".s"
            },

            new()
            {
                Name = "strip",
                Function = Strip,
                nParameterChecks = 2,
                ParameterMask = ".s"
            },
            new()
            {
                Name = "lstrip",
                Function = Lstrip,
                nParameterChecks = 2,
                ParameterMask = ".s"
            },
            new()
            {
                Name = "rstrip",
                Function = Rstrip,
                nParameterChecks = 2,
                ParameterMask = ".s"
            },
            new()
            {
                Name = "split",
                Function = Split,
                nParameterChecks = -3,
                ParameterMask = ".ssb",
                DefaultValues = new()
                {
                    { 3, new(false) }
                }
            },
            new()
            {
                Name = "join",
                Function = Join,
                nParameterChecks = -3,
                ParameterMask = ".sai",
                DefaultValues = new()
                {
                    { 3, new(2) }
                }
            },
            new()
            {
                Name = "format",
                Function = Format,
                nParameterChecks = -2,
                ParameterMask = null
            }
        };
        public static List<ExRegFunc> StringFuncs => _stdstrfuncs;

        public static bool RegisterStdString(ExVM vm, bool force = false)
        {
            ExAPI.RegisterNativeFunctions(vm, StringFuncs, force);
            return true;
        }
    }
}
