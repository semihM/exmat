using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ExMat.API;
using ExMat.Lexer;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.BaseLib
{
    public static class ExStdString
    {
        private static string RandomString(int length)
        {
            Random random = new();
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static ExFunctionStatus StdStringRands(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, RandomString(nargs == 1 ? (int)vm.GetPositiveIntegerArgument(1, 10) : 10));
        }

        private static ExObject GetMatch(Match match)
        {
            if (match.Success)
            {
                return new(
                    new Dictionary<string, ExObject>()
                    {
                        { "start", new(match.Index)},
                        { "end", new(match.Index + match.Length)},
                        { "value", new(match.Value)}
                    }
                );
            }
            else
            {
                return new();
            }
        }
        private static List<ExObject> GetMatchList(MatchCollection matches)
        {
            List<ExObject> list = new();

            foreach (Match match in matches)
            {
                list.Add(GetMatch(match));
            }

            return list;
        }

        public static ExFunctionStatus StdStringRegexMatches(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, GetMatchList(Regex.Matches(vm.GetArgument(1).GetString(), vm.GetArgument(2).GetString())));
        }

        public static ExFunctionStatus StdStringRegexMatch(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, GetMatch(Regex.Match(vm.GetArgument(1).GetString(), vm.GetArgument(2).GetString())));
        }

        public static ExFunctionStatus StdStringRegexReplace(ExVM vm, int nargs)
        {
            int count = nargs == 4 ? (int)vm.GetPositiveIntegerArgument(4, int.MaxValue) : int.MaxValue;
            return vm.CleanReturn(nargs + 2, new Regex(vm.GetArgument(2).GetString()).Replace(vm.GetArgument(1).GetString(), vm.GetArgument(3).GetString(), count));
        }

        public static ExFunctionStatus StdStringRegexSplit(ExVM vm, int nargs)
        {
            int count = nargs == 3 ? (int)vm.GetPositiveIntegerArgument(3, int.MaxValue) : int.MaxValue;
            return vm.CleanReturn(nargs + 2, new ExList(new Regex(vm.GetArgument(1).GetString()).Split(vm.GetArgument(2).GetString(), count)));
        }
        public static ExFunctionStatus StdStringRegexEscape(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, Regex.Escape(vm.GetArgument(1).GetString()));
        }

        public static ExFunctionStatus StdStringStrip(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetArgument(1).GetString().Trim());
        }
        public static ExFunctionStatus StdStringLstrip(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetArgument(1).GetString().TrimStart());
        }

        public static ExFunctionStatus StdStringRstrip(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetArgument(1).GetString().TrimEnd());
        }

        public static ExFunctionStatus StdStringSplit(ExVM vm, int nargs)
        {
            string s = vm.GetArgument(1).GetString();
            string c = vm.GetArgument(2).GetString();
            StringSplitOptions remove_empty = StringSplitOptions.None;
            if (nargs == 3 && vm.GetArgument(3).GetBool())
            {
                remove_empty = StringSplitOptions.RemoveEmptyEntries;
            }

            string[] arr = s.Split(c, remove_empty);

            List<ExObject> lis = new(arr.Length);
            for (int i = 0; i < arr.Length; i++)
            {
                lis.Add(new(arr[i]));
            }

            return vm.CleanReturn(nargs + 2, new ExObject(lis));
        }

        public static ExFunctionStatus StdStringJoin(ExVM vm, int nargs)
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
                if (vm.ToString(lis[i], ref str, depth)) //lgtm [cs/dereferenced-value-may-be-null]
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

        public static ExFunctionStatus StdStringCompile(ExVM vm, int nargs)
        {
            string code = vm.GetArgument(1).GetString();

            if (ExApi.CompileSource(vm, code))
            {
                return vm.CleanReturn(nargs + 3, new ExObject(vm.GetAbove(-1)));
            }

            return ExFunctionStatus.ERROR;
        }

        public static ExFunctionStatus StdStringFormat(ExVM vm, int nargs)
        {
            string format = vm.GetArgument(1).GetString();
            object[] ps = new object[nargs - 1];
            ExObject[] args = ExApi.GetNObjects(vm, nargs - 1, 3);
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
                Name = "reg_escape",
                Function = StdStringRegexEscape,
                nParameterChecks = 2,
                ParameterMask = ".s"
            },

            new()
            {
                Name = "reg_split",
                Function = StdStringRegexSplit,
                nParameterChecks = -3,
                ParameterMask = ".ssi|f",
                DefaultValues = new()
                {
                    { 3, new(int.MaxValue) }
                }
            },

            new()
            {
                Name = "reg_replace",
                Function = StdStringRegexReplace,
                nParameterChecks = -4,
                ParameterMask = ".sssi|f",
                DefaultValues = new()
                {
                    { 4, new(int.MaxValue) }
                }
            },

            new()
            {
                Name = "reg_match",
                Function = StdStringRegexMatch,
                nParameterChecks = 3,
                ParameterMask = ".ss"
            },

            new()
            {
                Name = "reg_matches",
                Function = StdStringRegexMatches,
                nParameterChecks = 3,
                ParameterMask = ".ss"
            },

            new()
            {
                Name = "compile",
                Function = StdStringCompile,
                nParameterChecks = 2,
                ParameterMask = ".s"
            },

            new()
            {
                Name = "strip",
                Function = StdStringStrip,
                nParameterChecks = 2,
                ParameterMask = ".s"
            },
            new()
            {
                Name = "lstrip",
                Function = StdStringLstrip,
                nParameterChecks = 2,
                ParameterMask = ".s"
            },
            new()
            {
                Name = "rstrip",
                Function = StdStringRstrip,
                nParameterChecks = 2,
                ParameterMask = ".s"
            },
            new()
            {
                Name = "split",
                Function = StdStringSplit,
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
                Function = StdStringJoin,
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
                Function = StdStringFormat,
                nParameterChecks = -2,
                ParameterMask = null
            },
            new()
            {
                Name = "rands",
                Function = StdStringRands,
                nParameterChecks = -1,
                ParameterMask = ".i",
                DefaultValues = new()
                {
                    { 1, new(10) }
                }
            }
        };
        public static List<ExRegFunc> StringFuncs => _stdstrfuncs;

        public static bool RegisterStdString(ExVM vm)
        {
            ExApi.RegisterNativeFunctions(vm, StringFuncs);
            return true;
        }
    }
}
