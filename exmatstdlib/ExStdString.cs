using System;
using System.Collections.Generic;
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
        public static ExFunctionStatus StdStringRands(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, ExApi.RandomString(nargs == 1 ? (int)vm.GetPositiveIntegerArgument(1, 10) : 10));
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
                Parameters = new()
                {
                    new("string", "s", "String to escape regex characters")
                },
                Returns = ExBaseType.STRING,
                Description = "Escape regex characters in given string"
            },

            new()
            {
                Name = "reg_split",
                Function = StdStringRegexSplit,
                Parameters = new()
                {
                    new("string", "s", "String to split"),
                    new("pattern", "s", "Splitting pattern"),
                    new("max_count", "r", "Maximum count of splitting", new(int.MaxValue))
                },
                Returns = ExBaseType.ARRAY,
                Description = "Split given string with a pattern given amount of maximum splits"
            },

            new()
            {
                Name = "reg_replace",
                Function = StdStringRegexReplace,
                Parameters = new()
                {
                    new("string", "s", "String to search through"),
                    new("old", "s", "Old value pattern"),
                    new("new", "s", "Replacement value"),
                    new("max_count", "r", "Maximum count of replacements", new(int.MaxValue))
                },
                Returns = ExBaseType.STRING,
                Description = "Replace parts which matches given pattern in a string with a given value maximum given times"
            },

            new()
            {
                Name = "reg_match",
                Function = StdStringRegexMatch,
                Parameters = new()
                {
                    new("string", "s", "String to search through"),
                    new("pattern", "s", "Pattern to match")
                },
                Returns = ExBaseType.DICT | ExBaseType.NULL,
                Description = "Find a match of given pattern in given string. Returns first match information as dictionary or null if nothing matches."
            },

            new()
            {
                Name = "reg_matches",
                Function = StdStringRegexMatches,
                Parameters = new()
                {
                    new("string", "s", "String to search through"),
                    new("pattern", "s", "Pattern to match")
                },
                Returns = ExBaseType.ARRAY,
                Description = "Find all matches of given pattern in given string. Returns match informations as dictionaries."
            },

            new()
            {
                Name = "compile",
                Function = StdStringCompile,
                Parameters = new()
                {
                    new("code", "s", "Code to compile")
                },
                Returns = ExBaseType.CLOSURE,
                Description = "Compile given code into a callable function"
            },

            new()
            {
                Name = "strip",
                Function = StdStringStrip,
                Parameters = new()
                {
                    new("string", "s", "String to strip")
                },
                Returns = ExBaseType.STRING,
                Description = "Return a new string of given string stripped from both at the begining and the end."
            },
            new()
            {
                Name = "lstrip",
                Function = StdStringLstrip,
                Parameters = new()
                {
                    new("string", "s", "String to strip")
                },
                Returns = ExBaseType.STRING,
                Description = "Return a new string of given string stripped from the begining."
            },
            new()
            {
                Name = "rstrip",
                Function = StdStringRstrip,
                Parameters = new()
                {
                    new("string", "s", "String to strip")
                },
                Returns = ExBaseType.STRING,
                Description = "Return a new string of given string stripped from the end."
            },
            new()
            {
                Name = "split",
                Function = StdStringSplit,
                Parameters = new()
                {
                    new("string", "s", "String to split"),
                    new("splitter", "s", "Splitting string"),
                    new("remove_empty", ".", "Wheter to remove empty strings", new(false))
                },
                Returns = ExBaseType.ARRAY,
                Description = "Split given string with given splitter."
            },
            new()
            {
                Name = "join",
                Function = StdStringJoin,
                Parameters = new()
                {
                    new("seperator", "s", "String to use between strings"),
                    new("list", "a", "List of objects"),
                    new("depth", "r", "Depth to stringify objects to", new(2))
                },
                Returns = ExBaseType.STRING,
                Description = "Join a list of objects with given seperators into a string, using given depth of stringification for the objects."
            },
            new()
            {
                Name = "format",
                Function = StdStringFormat,
                NumberOfParameters = -2,
                Returns = ExBaseType.STRING,
                Description = "Replace given {x} patterns in the first string with the (x+2)th argument passed.\n\tExample: format(\"{0}, {1}\", \"first\", \"second\") == \"first, second\""
            },
            new()
            {
                Name = "rands",
                Function = StdStringRands,
                Parameters = new()
                {
                    new("length", "r", "Length of the string", new(10))
                },
                Returns = ExBaseType.STRING,
                Description = "Create a cryptographically safe random string using characters from [a-zA-Z0-9] with given length."
            }
        };
        public static List<ExRegFunc> StringFuncs => _stdstrfuncs;

        public static bool RegisterStdString(ExVM vm)
        {
            ExApi.RegisterNativeFunctions(vm, StringFuncs, ExStdLibType.STRING);

            return true;
        }
    }
}
