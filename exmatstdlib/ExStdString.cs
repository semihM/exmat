using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ExMat.API;
using ExMat.Attributes;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.StdLib
{
    [ExStdLibBase(ExStdLibType.STRING)]
    [ExStdLibName("string")]
    [ExStdLibRegister(nameof(Registery))]
    public static class ExStdString
    {
        #region UTILITY
        private static ExObject GetMatch(Match match)
        {
            return match.Success
                ? (new(
                    new Dictionary<string, ExObject>()
                    {
                        { "start", new(match.Index)},
                        { "end", new(match.Index + match.Length)},
                        { "value", new(match.Value)}
                    }
                ))
                : (new());
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

        #endregion

        #region STRING FUNCTIONS
        [ExNativeFuncBase("rands", ExBaseType.STRING, "Create a cryptographically safe random string using characters from [a-zA-Z0-9] with given length.")]
        [ExNativeParamBase(1, "length", "r", "Length of the string", 10)]
        public static ExFunctionStatus StdStringRands(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, ExApi.RandomString(nargs == 1 ? (int)vm.GetPositiveIntegerArgument(1, 10) : 10));
        }

        [ExNativeFuncBase("reg_matches", ExBaseType.ARRAY, "Find all matches of given pattern in given string. Returns match informations as dictionaries.")]
        [ExNativeParamBase(1, "string", "s", "String to search through")]
        [ExNativeParamBase(2, "pattern", "s", "Pattern to match")]
        public static ExFunctionStatus StdStringRegexMatches(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, GetMatchList(Regex.Matches(vm.GetArgument(1).GetString(), vm.GetArgument(2).GetString())));
        }

        [ExNativeFuncBase("reg_match", ExBaseType.DICT | ExBaseType.NULL, "Find a match of given pattern in given string. Returns first match information as dictionary or null if nothing matches.")]
        [ExNativeParamBase(1, "string", "s", "String to search through")]
        [ExNativeParamBase(2, "pattern", "s", "Pattern to match")]
        public static ExFunctionStatus StdStringRegexMatch(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, GetMatch(Regex.Match(vm.GetArgument(1).GetString(), vm.GetArgument(2).GetString())));
        }

        [ExNativeFuncBase("reg_replace", ExBaseType.STRING, "Replace parts which matches given pattern in a string with a given value maximum given times")]
        [ExNativeParamBase(1, "string", "s", "String to search through")]
        [ExNativeParamBase(2, "old", "s", "Old value pattern")]
        [ExNativeParamBase(3, "new", "s", "Replacement value")]
        [ExNativeParamBase(4, "max_count", "r", "Maximum count of replacements", int.MaxValue)]
        public static ExFunctionStatus StdStringRegexReplace(ExVM vm, int nargs)
        {
            int count = nargs == 4 ? (int)vm.GetPositiveIntegerArgument(4, int.MaxValue) : int.MaxValue;
            return vm.CleanReturn(nargs + 2, new Regex(vm.GetArgument(2).GetString()).Replace(vm.GetArgument(1).GetString(), vm.GetArgument(3).GetString(), count));
        }

        [ExNativeFuncBase("reg_split", ExBaseType.ARRAY, "Split given string with a pattern given amount of maximum splits")]
        [ExNativeParamBase(1, "string", "s", "String to split")]
        [ExNativeParamBase(2, "pattern", "s", "Splitting pattern")]
        [ExNativeParamBase(3, "max_count", "r", "Maximum count of splitting", int.MaxValue)]
        public static ExFunctionStatus StdStringRegexSplit(ExVM vm, int nargs)
        {
            int count = nargs == 3 ? (int)vm.GetPositiveIntegerArgument(3, int.MaxValue) : int.MaxValue;
            return vm.CleanReturn(nargs + 2, new ExList(new Regex(vm.GetArgument(1).GetString()).Split(vm.GetArgument(2).GetString(), count)));
        }

        [ExNativeFuncBase("reg_escape", ExBaseType.STRING, "Escape regex characters in given string")]
        [ExNativeParamBase(1, "string", "s", "String to escape regex characters")]
        public static ExFunctionStatus StdStringRegexEscape(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, Regex.Escape(vm.GetArgument(1).GetString()));
        }

        [ExNativeFuncBase("strip", ExBaseType.STRING, "Return a new string of given string stripped from both at the begining and the end.")]
        [ExNativeParamBase(1, "string", "s", "String to strip")]
        public static ExFunctionStatus StdStringStrip(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetArgument(1).GetString().Trim());
        }

        [ExNativeFuncBase("lstrip", ExBaseType.STRING, "Return a new string of given string stripped from the begining.")]
        [ExNativeParamBase(1, "string", "s", "String to strip")]
        public static ExFunctionStatus StdStringLstrip(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetArgument(1).GetString().TrimStart());
        }

        [ExNativeFuncBase("rstrip", ExBaseType.STRING, "Return a new string of given string stripped from the end.")]
        [ExNativeParamBase(1, "string", "s", "String to strip")]
        public static ExFunctionStatus StdStringRstrip(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetArgument(1).GetString().TrimEnd());
        }

        [ExNativeFuncBase("split", ExBaseType.ARRAY, "Split given string with given splitter.")]
        [ExNativeParamBase(1, "string", "s", "String to split")]
        [ExNativeParamBase(2, "splitter", "s", "Splitting string")]
        [ExNativeParamBase(3, "remove_empty", ".", "Wheter to remove empty strings", false)]
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

        [ExNativeFuncBase("join", ExBaseType.STRING, "Join a list of objects with given seperators into a string, using given depth of stringification for the objects.")]
        [ExNativeParamBase(1, "seperator", "s", "String to use between strings")]
        [ExNativeParamBase(2, "list", "a", "List of objects")]
        [ExNativeParamBase(3, "depth", "r", "Depth to stringify objects to", 2)]
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

        [ExNativeFuncBase("compile", ExBaseType.CLOSURE, "Compile given code into a callable function")]
        [ExNativeParamBase(1, "code", "s", "Code to compile")]
        public static ExFunctionStatus StdStringCompile(ExVM vm, int nargs)
        {
            string code = vm.GetArgument(1).GetString();

            return ExApi.CompileSource(vm, code) ? vm.CleanReturn(nargs + 3, new ExObject(vm.GetAbove(-1))) : ExFunctionStatus.ERROR;
        }

        [ExNativeFuncBase("format", ExBaseType.STRING, "Replace given {x} patterns in the first string with the (x+2)th argument passed.\n\tExample: format(\"{0}, {1}\", \"first\", \"second\") == \"first, second\"", -2)]
        public static ExFunctionStatus StdStringFormat(ExVM vm, int nargs)
        {
            if (ExApi.GetFormatStringAndObjects(vm, nargs, out string format, out object[] ps) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }

            try
            {
                return vm.CleanReturn(nargs + 2, string.Format(CultureInfo.CurrentCulture, format, ps));
            }
            catch
            {
                return vm.AddToErrorMessage("not enough arguments given for format string");
            }
        }

        #endregion

        // MAIN

        public static ExMat.StdLibRegistery Registery => (ExVM vm) =>
        {
            return true;
        };
    }
}
