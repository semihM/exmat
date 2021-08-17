using System;
using System.Collections.Generic;
using System.Text;
using ExMat.API;
using ExMat.Lexer;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.BaseLib
{
    public static class ExStdString
    {
        public static ExFunctionStatus Strip(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetArgument(1).GetString().Trim());
        }
        public static ExFunctionStatus Lstrip(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetArgument(1).GetString().TrimStart());
        }

        public static ExFunctionStatus Rstrip(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetArgument(1).GetString().TrimEnd());
        }

        public static ExFunctionStatus Split(ExVM vm, int nargs)
        {
            string s = vm.GetArgument(1).GetString();
            string c = vm.GetArgument(2).GetString();
            StringSplitOptions remove_empty = nargs == 3
                ? (vm.GetArgument(3).GetBool()
                    ? StringSplitOptions.RemoveEmptyEntries
                    : StringSplitOptions.None)
                : StringSplitOptions.None;

            string[] arr = s.Split(c, remove_empty);

            List<ExObject> lis = new(arr.Length);
            for (int i = 0; i < arr.Length; i++)
            {
                lis.Add(new(arr[i]));
            }

            return vm.CleanReturn(nargs + 2, new ExObject(lis));
        }

        public static ExFunctionStatus Join(ExVM vm, int nargs)
        {
            string s = vm.GetArgument(1).GetString();
            List<ExObject> lis = vm.GetArgument(2).GetList();

            int depth = 2;
            if (nargs == 3)
            {
                depth = (int)vm.GetArgument(3).GetInt();
                if (depth <= 0)
                {
                    depth = 1;
                }
            }

            int n = lis != null ? lis.Count : 0;
            StringBuilder res = new();

            for (int i = 0; i < n; i++)
            {
                ExObject str = new();
                if (vm.ToString(lis[i], ref str, depth))
                {
                    res.Append(str.GetString());
                }
                else
                {
                    return ExFunctionStatus.ERROR;
                }
                if (i != n - 1)
                {
                    res.Append(s);
                }
            }

            return vm.CleanReturn(nargs + 2, res.ToString());
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

        public static ExFunctionStatus Compile(ExVM vm, int nargs)
        {
            string code = vm.GetArgument(1).GetString();

            if (ExAPI.CompileSource(vm, code))
            {
                return vm.CleanReturn(nargs + 3, new ExObject(vm.GetAbove(-1)));
            }

            return ExFunctionStatus.ERROR;
        }

        public static ExFunctionStatus Format(ExVM vm, int nargs)
        {
            string format = vm.GetArgument(1).GetString();
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
                    return ExFunctionStatus.ERROR;
                }
            }

            try
            {
                return vm.CleanReturn(nargs + 2, string.Format(format, ps));
            }
            catch
            {
                return vm.AddToErrorMessage("not enough arguments given for format string");
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

        public static bool RegisterStdString(ExVM vm)
        {
            ExAPI.RegisterNativeFunctions(vm, StringFuncs);
            return true;
        }
    }
}
