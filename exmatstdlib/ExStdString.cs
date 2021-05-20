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
        public static int STRING_strip(ExVM vm, int nargs)
        {
            ExObjectPtr s = ExAPI.GetFromStack(vm, 2);
            vm.Push(s.GetString().Trim());
            return 1;
        }
        public static int STRING_lstrip(ExVM vm, int nargs)
        {
            ExObjectPtr s = ExAPI.GetFromStack(vm, 2);
            vm.Push(s.GetString().TrimStart());
            return 1;
        }

        public static int STRING_rstrip(ExVM vm, int nargs)
        {
            ExObjectPtr s = ExAPI.GetFromStack(vm, 2);
            vm.Push(s.GetString().TrimEnd());
            return 1;
        }

        public static int STRING_split(ExVM vm, int nargs)
        {
            ExObjectPtr s = ExAPI.GetFromStack(vm, 2);
            ExObjectPtr c = ExAPI.GetFromStack(vm, 3);
            StringSplitOptions remove_empty = nargs == 3
                ? (ExAPI.GetFromStack(vm, 4).GetBool()
                    ? StringSplitOptions.RemoveEmptyEntries
                    : StringSplitOptions.None)
                : StringSplitOptions.None;

            string[] arr = s.GetString().Split(c.GetString(), remove_empty);

            List<ExObjectPtr> lis = new(arr.Length);
            for (int i = 0; i < arr.Length; i++)
            {
                lis.Add(new(arr[i]));
            }

            vm.Push(new ExObjectPtr(lis));
            return 1;
        }

        public static void ReplaceMacroParams(ExMacro m, List<ExObjectPtr> args)
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
                return 1;
            }

            return -1;
        }
        private static readonly List<ExRegFunc> _stdstrfuncs = new()
        {
            new() { name = "compile", func = new(Type.GetType("ExMat.BaseLib.ExStdString").GetMethod("STRING_compile")), n_pchecks = 2, mask = ".s" },

            new() { name = "strip", func = new(Type.GetType("ExMat.BaseLib.ExStdString").GetMethod("STRING_strip")), n_pchecks = 2, mask = ".s" },
            new() { name = "lstrip", func = new(Type.GetType("ExMat.BaseLib.ExStdString").GetMethod("STRING_lstrip")), n_pchecks = 2, mask = ".s" },
            new() { name = "rstrip", func = new(Type.GetType("ExMat.BaseLib.ExStdString").GetMethod("STRING_rstrip")), n_pchecks = 2, mask = ".s" },
            new() { name = "split", func = new(Type.GetType("ExMat.BaseLib.ExStdString").GetMethod("STRING_split")), n_pchecks = -3, mask = ".ssb" },

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
