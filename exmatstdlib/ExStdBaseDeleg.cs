using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using ExMat.API;
using ExMat.Class;
using ExMat.Objects;
using ExMat.Utils;
using ExMat.VM;

namespace ExMat.StdLib
{
    public static partial class ExStdBase
    {
        #region UTILITY
        private static ExFunctionStatus StringIndexCheck(ExVM vm, int n, ref string s)
        {
            if (n < 0 || n >= s.Length)
            {
                return vm.AddToErrorMessage("string can't be indexed with integer higher than it's length or negative");
            }
            s = s[n].ToString();
            return ExFunctionStatus.SUCCESS;
        }
        #endregion

        #region DELEGATES

        #region INTEGER | FLOAT
        [ExNativeFuncDelegate("real", ExBaseType.INTEGER | ExBaseType.FLOAT, "Return the real part of the value. This delegate always returns the value itself", 'r')]
        public static ExFunctionStatus StdNumericReal(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetRootArgument().Type == ExObjType.INTEGER
                                                ? new ExObject(vm.GetRootArgument().GetInt())
                                                : new(vm.GetRootArgument().GetFloat()));
        }

        [ExNativeFuncDelegate("img", ExBaseType.INTEGER | ExBaseType.FLOAT, "Return the imaginary part of the value. This delegate always returns 0", 'r')]
        public static ExFunctionStatus StdNumericImage(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetRootArgument().Type == ExObjType.INTEGER
                                                ? new ExObject(0)
                                                : new ExObject(0.0));
        }
        #endregion

        #region COMPLEX
        [ExNativeFuncDelegate("phase", ExBaseType.FLOAT, "Return the phase of the value", 'C')]
        public static ExFunctionStatus StdComplexPhase(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, new ExObject(vm.GetRootArgument().GetComplex().Phase));
        }

        [ExNativeFuncDelegate("abs", ExBaseType.FLOAT, "Return the magnitute of the value", 'C')]
        public static ExFunctionStatus StdComplexMagnitude(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, new ExObject(vm.GetRootArgument().GetComplex().Magnitude));
        }

        [ExNativeFuncDelegate("img", ExBaseType.FLOAT, "Return the imaginary part of the value", 'C')]
        public static ExFunctionStatus StdComplexImg(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, new ExObject(vm.GetRootArgument().Value.c_Float));
        }

        [ExNativeFuncDelegate("real", ExBaseType.FLOAT, "Return the real part of the value", 'C')]
        public static ExFunctionStatus StdComplexReal(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, new ExObject(vm.GetRootArgument().Value.f_Float));
        }

        [ExNativeFuncDelegate("conj", ExBaseType.COMPLEX, "Return the complex conjugate of the value", 'C')]
        public static ExFunctionStatus StdComplexConjugate(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetRootArgument().GetComplexConj());
        }
        #endregion

        #region STRING
        [ExNativeFuncDelegate("index_of", ExBaseType.INTEGER, "Return the index of given substring or -1", 's')]
        [ExNativeParamBase(1, "substring", "s", "Substring to search for")]
        public static ExFunctionStatus StdStringIndexOf(ExVM vm, int nargs)
        {
            return vm.CleanReturn(1, vm.GetRootArgument().GetString().IndexOf(vm.GetArgument(1).GetString()));
        }

        [ExNativeFuncDelegate("to_upper", ExBaseType.STRING, "Return a new string with characters capitalized", 's')]
        public static ExFunctionStatus StdStringToUpper(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetRootArgument().GetString().ToUpper());
        }

        [ExNativeFuncDelegate("to_lower", ExBaseType.STRING, "Return a new string with characters uncapitalized", 's')]
        public static ExFunctionStatus StdStringToLower(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetRootArgument().GetString().ToLower());
        }

        [ExNativeFuncDelegate("reverse", ExBaseType.STRING, "Return a new string with character order reversed", 's')]
        public static ExFunctionStatus StdStringReverse(ExVM vm, int nargs)
        {
            char[] ch = vm.GetRootArgument().GetString().ToCharArray();
            Array.Reverse(ch);
            return vm.CleanReturn(nargs + 2, new string(ch));
        }

        [ExNativeFuncDelegate("replace", ExBaseType.STRING, "Return a new string with given substrings replaced with given new string", 's')]
        [ExNativeParamBase(1, "old", "s", "Value to be replaced")]
        [ExNativeParamBase(2, "new", "s", "Value to use for replacing")]
        public static ExFunctionStatus StdStringReplace(ExVM vm, int nargs)
        {
            string obj = vm.GetRootArgument().GetString();
            return vm.CleanReturn(nargs + 2, obj.Replace(vm.GetArgument(1).GetString(), vm.GetArgument(2).GetString()));
        }

        [ExNativeFuncDelegate("repeat", ExBaseType.STRING, "Return a new string with the original string repeat given times", 's')]
        [ExNativeParamBase(1, "repeat", "r", "Times to repeat the string")]
        public static ExFunctionStatus StdStringRepeat(ExVM vm, int nargs)
        {
            string obj = vm.GetRootArgument().GetString();
            int rep = (int)vm.GetPositiveIntegerArgument(1, 0);
            StringBuilder res = new();
            while (rep-- > 0)
            {
                res.Append(obj);
            }
            return vm.CleanReturn(nargs + 2, res.ToString());
        }

        [ExNativeFuncDelegate("isAlphabetic", ExBaseType.BOOL, "Check if the string or a character at given index is alphabetic", 's')]
        [ExNativeParamBase(1, "index", "r", "Character index to check instead of the whole string", 0)]
        public static ExFunctionStatus StdStringAlphabetic(ExVM vm, int nargs)
        {
            string s = vm.GetRootArgument().GetString();
            if (nargs == 1
                && StringIndexCheck(vm, (int)vm.GetPositiveIntegerArgument(1, 0), ref s) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }
            return vm.CleanReturn(nargs + 2, Regex.IsMatch(s, "^[A-Za-z]+$"));
        }

        [ExNativeFuncDelegate("isNumeric", ExBaseType.BOOL, "Check if the string or a character at given index is numeric", 's')]
        [ExNativeParamBase(1, "index", "r", "Character index to check instead of the whole string", 0)]
        public static ExFunctionStatus StdStringNumeric(ExVM vm, int nargs)
        {
            string s = vm.GetRootArgument().GetString();
            if (nargs == 1
                && StringIndexCheck(vm, (int)vm.GetPositiveIntegerArgument(1, 0), ref s) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }
            return vm.CleanReturn(nargs + 2, Regex.IsMatch(s, @"^\d+(\.\d+)?((E|e)(\+|\-)\d+)?$"));
        }

        [ExNativeFuncDelegate("isAlphaNumeric", ExBaseType.BOOL, "Check if the string or a character at given index is alphabetic or numeric", 's')]
        [ExNativeParamBase(1, "index", "r", "Character index to check instead of the whole string", 0)]
        public static ExFunctionStatus StdStringAlphaNumeric(ExVM vm, int nargs)
        {
            string s = vm.GetRootArgument().GetString();
            if (nargs == 1
                && StringIndexCheck(vm, (int)vm.GetPositiveIntegerArgument(1, 0), ref s) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }
            return vm.CleanReturn(nargs + 2, Regex.IsMatch(s, "^[A-Za-z0-9]+$"));
        }

        [ExNativeFuncDelegate("isLower", ExBaseType.BOOL, "Check if the string or a character at given index is lower case", 's')]
        [ExNativeParamBase(1, "index", "r", "Character index to check instead of the whole string", 0)]
        public static ExFunctionStatus StdStringLower(ExVM vm, int nargs)
        {
            string s = vm.GetRootArgument().GetString();
            if (nargs == 1
                && StringIndexCheck(vm, (int)vm.GetPositiveIntegerArgument(1, 0), ref s) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }
            foreach (char c in s)
            {
                if (!char.IsLower(c))
                {
                    return vm.CleanReturn(nargs + 2, false);
                }
            }
            return vm.CleanReturn(nargs + 2, !string.IsNullOrEmpty(s));
        }

        [ExNativeFuncDelegate("isUpper", ExBaseType.BOOL, "Check if the string or a character at given index is upper case", 's')]
        [ExNativeParamBase(1, "index", "r", "Character index to check instead of the whole string", 0)]
        public static ExFunctionStatus StdStringUpper(ExVM vm, int nargs)
        {
            string s = vm.GetRootArgument().GetString();
            if (nargs == 1
                && StringIndexCheck(vm, (int)vm.GetPositiveIntegerArgument(1, 0), ref s) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }
            foreach (char c in s)
            {
                if (!char.IsUpper(c))
                {
                    return vm.CleanReturn(nargs + 2, false);
                }
            }
            return vm.CleanReturn(nargs + 2, !string.IsNullOrEmpty(s));
        }

        [ExNativeFuncDelegate("isWhitespace", ExBaseType.BOOL, "Check if the string or a character at given index is whitespace", 's')]
        [ExNativeParamBase(1, "index", "r", "Character index to check instead of the whole string", 0)]
        public static ExFunctionStatus StdStringWhitespace(ExVM vm, int nargs)
        {
            string s = vm.GetRootArgument().GetString();
            if (nargs == 1
                && StringIndexCheck(vm, (int)vm.GetPositiveIntegerArgument(1, 0), ref s) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }
            foreach (char c in s)
            {
                if (!char.IsWhiteSpace(c))
                {
                    return vm.CleanReturn(nargs + 2, false);
                }
            }
            return vm.CleanReturn(nargs + 2, s.Length > 0);
        }

        [ExNativeFuncDelegate("isSymbol", ExBaseType.BOOL, "Check if the string or a character at given index is symbolic", 's')]
        [ExNativeParamBase(1, "index", "r", "Character index to check instead of the whole string", 0)]
        public static ExFunctionStatus StdStringSymbol(ExVM vm, int nargs)
        {
            string s = vm.GetRootArgument().GetString();
            if (nargs == 1
                && StringIndexCheck(vm, (int)vm.GetPositiveIntegerArgument(1, 0), ref s) == ExFunctionStatus.ERROR)
            {
                return ExFunctionStatus.ERROR;
            }
            foreach (char c in s)
            {
                if (!char.IsSymbol(c))
                {
                    return vm.CleanReturn(nargs + 2, false);
                }
            }
            return vm.CleanReturn(nargs + 2, !string.IsNullOrEmpty(s));
        }

        [ExNativeFuncDelegate("slice", ExBaseType.STRING, "Return a new string with characters picked from given range. Negative indices gets incremented by string length", 's')]
        [ExNativeParamBase(1, "index1", "r", "If used alone: [0,index1), otherwise: [index1,index2)")]
        [ExNativeParamBase(2, "index2", "r", "Ending index, returned list length == index2 - index1", (-1))]
        public static ExFunctionStatus StdStringSlice(ExVM vm, int nargs)
        {
            ExObject o = vm.GetRootArgument();

            int start = (int)vm.GetArgument(1).GetInt();

            char[] arr = o.GetString().ToCharArray();
            char[] res = null;

            int n = arr.Length;
            bool filled = false;

            switch (nargs)
            {
                case 1:
                    {
                        if (start < 0)
                        {
                            start += n;
                        }
                        if (start > n || start < 0)
                        {
                            vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                            return ExFunctionStatus.ERROR;
                        }

                        filled = true;
                        res = new char[start];

                        for (int i = 0; i < start; i++)
                        {
                            res[i] = arr[i];
                        }
                        break;
                    }
                case 2:
                    {
                        int end = (int)vm.GetArgument(2).GetInt();
                        if (start < 0)
                        {
                            start += n;
                        }
                        if (start >= n || start < 0)
                        {
                            vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                            return ExFunctionStatus.ERROR;
                        }

                        if (end < 0)
                        {
                            end += n;
                        }
                        if (end > n || end < 0)
                        {
                            vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                            return ExFunctionStatus.ERROR;
                        }

                        if (start >= end)
                        {
                            break;
                        }

                        filled = true;
                        res = new char[end - start];

                        for (int i = start, j = 0; i < end; i++, j++)
                        {
                            res[j] = arr[i];
                        }
                        break;
                    }
            }

            return vm.CleanReturn(nargs + 2, filled ? new string(res) : string.Empty);
        }
        #endregion

        #region ARRAY
        [ExNativeFuncDelegate("append", ExBaseType.ARRAY, "Return a new list with given item appended", 'a')]
        [ExNativeParamBase(1, "object", ".", "Object to append")]
        public static ExFunctionStatus StdArrayAppend(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            res = new(ExUtils.GetACopyOf(res.GetList()));
            res.GetList().Add(new(vm.GetArgument(1)));
            return vm.CleanReturn(nargs + 2, res);
        }

        [ExNativeFuncDelegate("remove_at", ExBaseType.ARRAY, "Return a new list with the item at given index removed", 'a')]
        [ExNativeParamBase(1, "index", "r", "Index of the item to remove")]
        public static ExFunctionStatus StdArrayRemoveAt(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            int liscount = res.GetList().Count;
            if (liscount == 0)
            {
                return vm.AddToErrorMessage("can't remove from an empty list");
            }

            int remove_idx = (int)vm.GetPositiveRangedIntegerArgument(1, 0, liscount - 1);

            res = new(ExUtils.GetACopyOf(res.GetList()));
            res.GetList().RemoveAt(remove_idx);

            return vm.CleanReturn(nargs + 2, res);
        }

        [ExNativeFuncDelegate("extend", ExBaseType.ARRAY, "Return the original list with given list of objects appended", 'a')]
        [ExNativeParamBase(1, "list", "a", "List of items to append")]
        public static ExFunctionStatus StdArrayExtend(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            res.GetList().AddRange(vm.GetArgument(1).GetList());
            return vm.CleanReturn(nargs + 2, res);
        }

        [ExNativeFuncDelegate("expand", ExBaseType.ARRAY, "Return a new list with given list of objects appended", 'a')]
        [ExNativeParamBase(1, "list", "a", "List of items to append")]
        public static ExFunctionStatus StdArrayExpand(ExVM vm, int nargs)
        {
            ExObject res = new(ExUtils.GetACopyOf(vm.GetRootArgument().GetList()));
            res.GetList().AddRange(vm.GetArgument(1).GetList());
            return vm.CleanReturn(nargs + 2, res);
        }

        [ExNativeFuncDelegate("push", ExBaseType.ARRAY, "Return the original list with given item appended", 'a')]
        [ExNativeParamBase(1, "object", ".", "Object to push to end")]
        public static ExFunctionStatus StdArrayPush(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            res.GetList().Add(new(vm.GetArgument(1)));
            return vm.CleanReturn(nargs + 2, res);
        }

        [ExNativeFuncDelegate("pop", ExBaseType.ARRAY, "Return the original list with given amount of items popped", 'a')]
        [ExNativeParamBase(1, "count", "r", "Amount of items to pop", 1)]
        public static ExFunctionStatus StdArrayPop(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();

            int liscount = res.GetList().Count;
            if (liscount == 0)
            {
                return vm.AddToErrorMessage("can't pop from empty list");
            }

            int popcount = nargs == 1 ? (int)vm.GetPositiveRangedIntegerArgument(1, 0, liscount) : 1;
            for (int i = 0; i < popcount; i++)
            {
                res.GetList().RemoveAt(liscount - 1 - i);
            }
            return vm.CleanReturn(nargs + 2, res);
        }

        [ExNativeFuncDelegate("resize", ExBaseType.ARRAY, "Return the original list resized", 'a')]
        [ExNativeParamBase(1, "new_size", "r", "New size for the list")]
        [ExNativeParamBase(2, "filler", ".", "Filler object if new size is bigger than current size", def: null)]
        public static ExFunctionStatus StdArrayResize(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();

            int newsize = (int)vm.GetPositiveIntegerArgument(1, 0);

            int curr = res.GetList().Count;
            ExObject filler = nargs == 2 ? vm.GetArgument(2) : null;

            if (curr > 0 && newsize > 0)
            {
                if (newsize >= curr)
                {
                    ExUtils.AppendFillerNTimes(res.GetList(), filler, newsize - curr);
                }
                else
                {
                    while (curr != newsize)
                    {
                        res.GetList()[curr - 1].Nullify();
                        res.GetList().RemoveAt(curr - 1);
                        curr--;
                    }
                }
            }
            else if (newsize > 0)
            {
                res.Value.l_List = new(newsize);
                ExUtils.AppendFillerNTimes(res.GetList(), filler, newsize);
            }
            else
            {
                res.Value.l_List = new();
            }

            return vm.CleanReturn(nargs + 2, res);
        }

        [ExNativeFuncDelegate("index_of", ExBaseType.INTEGER, "Return the index of an object or -1 if nothing found", 'a')]
        [ExNativeParamBase(1, "object", ".", "Object to search for")]
        public static ExFunctionStatus StdArrayIndexOf(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, ExApi.GetValueIndexFromArray(vm.GetRootArgument().GetList(), vm.GetArgument(1)));
        }

        [ExNativeFuncDelegate("count", ExBaseType.INTEGER, "Count how many times given object appears in the list", 'a')]
        [ExNativeParamBase(1, "object", ".", "Object to search for")]
        public static ExFunctionStatus StdArrayCount(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            using ExObject obj = new(vm.GetArgument(1));

            int i = ExApi.CountValueEqualsInArray(res.GetList(), obj);
            return vm.CleanReturn(nargs + 2, i);
        }

        [ExNativeFuncDelegate("slice", ExBaseType.ARRAY, "Return a new list with items picked from given range. Negative indices gets incremented by list length", 'a')]
        [ExNativeParamBase(1, "index1", "r", "If used alone: [0,index1), otherwise: [index1,index2)")]
        [ExNativeParamBase(2, "index2", "r", "Ending index, returned list length == index2 - index1", (-1))]
        public static ExFunctionStatus StdArraySlice(ExVM vm, int nargs)
        {
            ExObject o = vm.GetRootArgument();
            int start = (int)vm.GetArgument(1).GetInt();

            List<ExObject> arr = o.GetList();
            List<ExObject> res = new(0);

            int n = arr.Count;

            switch (nargs)
            {
                case 1:
                    {
                        if (start < 0)
                        {
                            start += n;
                        }
                        if (start > n || start < 0)
                        {
                            vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                            return ExFunctionStatus.ERROR;
                        }

                        res = new(start);

                        for (int i = 0; i < start; i++)
                        {
                            res.Add(new(arr[i]));
                        }
                        break;
                    }
                case 2:
                    {
                        int end = (int)vm.GetArgument(2).GetInt();
                        if (start < 0)
                        {
                            start += n;
                        }
                        if (start > n || start < 0)
                        {
                            vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                            return ExFunctionStatus.ERROR;
                        }

                        if (end < 0)
                        {
                            end += n;
                        }
                        if (end > n || end < 0)
                        {
                            vm.AddToErrorMessage("index out of range, must be in range: [" + (-n) + ", " + n + "]");
                            return ExFunctionStatus.ERROR;
                        }

                        if (start >= end)
                        {
                            break;
                        }

                        res = new(end - start);

                        for (int i = start; i < end; i++)
                        {
                            res.Add(new(arr[i]));
                        }

                        break;
                    }
            }
            return vm.CleanReturn(nargs + 2, res);
        }

        [ExNativeFuncDelegate("shuffle", ExBaseType.ARRAY, "Return a new shuffled list", 'a')]
        public static ExFunctionStatus StdArrayShuffle(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, ExUtils.ShuffleList(vm.GetRootArgument().GetList()));
        }

        [ExNativeFuncDelegate("random", 0, "Return a random item or a list of given amount of random items. If 'count' > 1, a list of unique item picks is returned.", 'a')]
        [ExNativeParamBase(1, "count", "n", "Amount of random values to return", 1)]
        public static ExFunctionStatus StdArrayRandom(ExVM vm, int nargs)
        {
            List<ExObject> lis = vm.GetRootArgument().GetList();
            int count = nargs == 1 ? (int)vm.GetPositiveIntegerArgument(1, 1) : 1;

            if (count > lis.Count)
            {
                return vm.AddToErrorMessage("can't pick {0} values from list with length {1}", count, lis.Count);
            }
            else if (count == lis.Count)
            {
                return vm.CleanReturn(nargs + 2, ExUtils.ShuffleList(lis));
            }
            else if (count == 1)
            {
                return vm.CleanReturn(nargs + 2, new ExObject(lis[new Random().Next(lis.Count)]));
            }
            else
            {
                return vm.CleanReturn(nargs + 2, ExUtils.GetNRandomObjectsFrom(lis, count));
            }
        }

        [ExNativeFuncDelegate("reverse", ExBaseType.ARRAY, "Return a new list with the order of items reversed", 'a')]
        public static ExFunctionStatus StdArrayReverse(ExVM vm, int nargs)
        {
            ExObject obj = vm.GetRootArgument();
            List<ExObject> lis = obj.GetList();
            List<ExObject> res = new(lis.Count);
            for (int i = lis.Count - 1; i >= 0; i--)
            {
                res.Add(new(lis[i]));
            }
            return vm.CleanReturn(nargs + 2, res);
        }

        [ExNativeFuncDelegate("copy", ExBaseType.ARRAY, "Return a copy of the list.", 'a')]
        public static ExFunctionStatus StdArrayCopy(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, ExUtils.GetACopyOf(vm.GetRootArgument().GetList()));
        }

        [ExNativeFuncDelegate("transpose", ExBaseType.ARRAY, "Return the transposed form of given matrix. Not usable for non-matrix formats.", 'a')]
        public static ExFunctionStatus StdArrayTranspose(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            List<ExObject> vals = res.GetList();
            int rows = vals.Count;
            int cols = 0;

            if (!ExApi.DoMatrixTransposeChecks(vm, vals, ref cols))
            {
                return ExFunctionStatus.ERROR;
            }

            List<ExObject> lis = ExApi.TransposeMatrix(rows, cols, vals);

            return vm.CleanReturn(nargs + 2, lis);
        }

        [ExNativeFuncDelegate("order_asc", ExBaseType.ARRAY, "Return a new list in ascending order. Only uses real numbers in the list.", 'a')]
        public static ExFunctionStatus StdArrayOrderAsc(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, ExUtils.GetOrderedNumericList(vm.GetRootArgument().GetList()));
        }
        [ExNativeFuncDelegate("order_des", ExBaseType.ARRAY, "Return a new list in descending order. Only uses real numbers in the list.", 'a')]
        public static ExFunctionStatus StdArrayOrderDes(ExVM vm, int nargs)
        {
            List<ExObject> lis = ExUtils.GetOrderedNumericList(vm.GetRootArgument().GetList());
            lis.Reverse();
            return vm.CleanReturn(nargs + 2, lis);
        }
        [ExNativeFuncDelegate("unique", ExBaseType.ARRAY, "Return a new list of distinct values found in the list.", 'a')]
        public static ExFunctionStatus StdArrayUnique(ExVM vm, int nargs)
        {
            List<ExObject> lis = vm.GetRootArgument().GetList();

            List<ExObject> uniques = new();

            foreach (ExObject o in lis)
            {
                if (!uniques.Exists(x => ExApi.CheckEqualReturnRes(x, o)))
                {
                    uniques.Add(new(o));
                }
            }

            return vm.CleanReturn(nargs + 2, uniques);
        }
        #endregion

        #region DICT
        [ExNativeFuncDelegate("has_key", ExBaseType.BOOL, "Check if given key exists", 'd')]
        [ExNativeParamBase(1, "key", "s", "Key to check")]
        public static ExFunctionStatus StdDictHasKey(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, vm.GetRootArgument().GetDict().ContainsKey(vm.GetArgument(1).GetString()));
        }

        [ExNativeFuncDelegate("get_keys", ExBaseType.ARRAY, "Get a list of the keys", 'd')]
        public static ExFunctionStatus StdDictKeys(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            List<ExObject> keys = new(res.GetDict().Count);
            foreach (string key in res.GetDict().Keys)
            {
                keys.Add(new(key));
            }
            return vm.CleanReturn(nargs + 2, keys);
        }

        [ExNativeFuncDelegate("get_values", ExBaseType.ARRAY, "Get a list of the values", 'd')]
        public static ExFunctionStatus StdDictValues(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            List<ExObject> vals = new(res.GetDict().Count);
            foreach (ExObject val in res.GetDict().Values)
            {
                vals.Add(new(val));
            }
            return vm.CleanReturn(nargs + 2, vals);
        }

        [ExNativeFuncDelegate("random_key", ExBaseType.STRING, "Get a random key", 'd')]
        public static ExFunctionStatus StdDictRandomKey(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            return vm.CleanReturn(nargs + 2, new List<string>(res.GetDict().Keys)[new Random().Next(0, res.GetDict().Count)]);
        }

        [ExNativeFuncDelegate("random_val", 0, "Get a random value", 'd')]
        public static ExFunctionStatus StdDictRandomVal(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            return vm.CleanReturn(nargs + 2, new List<ExObject>(res.GetDict().Values)[new Random().Next(0, res.GetDict().Count)]);
        }
        #endregion

        #region CLASS
        [ExNativeFuncDelegate("has_attr", ExBaseType.BOOL, "Check if an attribute exists for a member or a method", 'y')]
        [ExNativeParamBase(1, "member_or_method", "s", "Member or method name")]
        [ExNativeParamBase(2, "attribute", "s", "Attribute name to check")]
        public static ExFunctionStatus StdClassHasAttr(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();

            string mem = vm.GetArgument(1).GetString();
            string attr = vm.GetArgument(2).GetString();

            ExClass cls = res.GetClass();
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }
                return vm.CleanReturn(nargs + 2, false);
            }

            return vm.AddToErrorMessage("unknown member or method '" + mem + "'");
        }

        [ExNativeFuncDelegate("get_attr", ExBaseType.BOOL, "Get an attribute of a member or a method", 'y')]
        [ExNativeParamBase(1, "member_or_method", "s", "Member or method name")]
        [ExNativeParamBase(2, "attribute", "s", "Attribute name to get")]
        public static ExFunctionStatus StdClassGetAttr(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            string mem = vm.GetArgument(1).GetString();
            string attr = vm.GetArgument(2).GetString();

            ExClass cls = res.GetClass();
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls.DefaultValues[v.GetMemberID()].Attributes.GetDict()[attr]);
                        return vm.CleanReturn(nargs + 2, val);
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls.Methods[v.GetMemberID()].Attributes.GetDict()[attr]);
                        return vm.CleanReturn(nargs + 2, val);
                    }
                }
                return vm.AddToErrorMessage("unknown attribute '" + attr + "'");
            }

            return vm.AddToErrorMessage("unknown member or method '" + mem + "'");
        }

        [ExNativeFuncDelegate("set_attr", ExBaseType.BOOL, "Set an attribute of a member or a method", 'y')]
        [ExNativeParamBase(1, "member_or_method", "s", "Member or method name")]
        [ExNativeParamBase(2, "attribute", "s", "Attribute name to get")]
        [ExNativeParamBase(3, "new_value", ".", "New attribute value")]
        public static ExFunctionStatus StdClassSetAttr(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            string mem = vm.GetArgument(1).GetString();
            string attr = vm.GetArgument(2).GetString();
            ExObject val = vm.GetArgument(3);

            ExClass cls = res.GetClass();
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        cls.DefaultValues[v.GetMemberID()].Attributes.GetDict()[attr].Assign(val);
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        cls.Methods[v.GetMemberID()].Attributes.GetDict()[attr].Assign(val);
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }
                return vm.AddToErrorMessage("unknown attribute '" + attr + "'");
            }

            return vm.AddToErrorMessage("unknown member or method '" + mem + "'");
        }
        #endregion

        #region INSTANCE
        [ExNativeFuncDelegate("has_attr", ExBaseType.BOOL, "Check if an attribute exists for a member or a method", 'x')]
        [ExNativeParamBase(1, "member_or_method", "s", "Member or method name")]
        [ExNativeParamBase(2, "attribute", "s", "Attribute name to check")]
        public static ExFunctionStatus StdInstanceHasAttr(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            string mem = vm.GetArgument(1).GetString();
            string attr = vm.GetArgument(2).GetString();

            ExClass cls = res.GetInstance().Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }
                return vm.CleanReturn(nargs + 2, false);
            }

            return vm.AddToErrorMessage("unknown member or method '" + mem + "'");
        }

        [ExNativeFuncDelegate("get_attr", ExBaseType.BOOL, "Get an attribute of a member or a method", 'x')]
        [ExNativeParamBase(1, "member_or_method", "s", "Member or method name")]
        [ExNativeParamBase(2, "attribute", "s", "Attribute name to get")]
        public static ExFunctionStatus StdInstanceGetAttr(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            string mem = vm.GetArgument(1).GetString();
            string attr = vm.GetArgument(2).GetString();

            ExClass cls = res.GetInstance().Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls.DefaultValues[v.GetMemberID()].Attributes.GetDict()[attr]);
                        return vm.CleanReturn(nargs + 2, val);
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        ExObject val = new(cls.Methods[v.GetMemberID()].Attributes.GetDict()[attr]);
                        return vm.CleanReturn(nargs + 2, val);
                    }
                }
                return vm.AddToErrorMessage("unknown attribute '" + attr + "'");
            }

            return vm.AddToErrorMessage("unknown member or method '" + mem + "'");
        }

        [ExNativeFuncDelegate("set_attr", ExBaseType.BOOL, "Set an attribute of a member or a method", 'x')]
        [ExNativeParamBase(1, "member_or_method", "s", "Member or method name")]
        [ExNativeParamBase(2, "attribute", "s", "Attribute name to get")]
        [ExNativeParamBase(3, "new_value", ".", "New attribute value")]
        public static ExFunctionStatus StdInstanceSetAttr(ExVM vm, int nargs)
        {
            ExObject res = vm.GetRootArgument();
            string mem = vm.GetArgument(1).GetString();
            string attr = vm.GetArgument(2).GetString();
            ExObject val = vm.GetArgument(3);
            ExClass cls = res.GetInstance().Class;
            if (cls.Members.ContainsKey(mem))
            {
                ExObject v = cls.Members[mem];
                if (v.IsField())
                {
                    if (cls.DefaultValues[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        cls.DefaultValues[v.GetMemberID()].Attributes.GetDict()[attr].Assign(val);
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }
                else
                {
                    if (cls.Methods[v.GetMemberID()].Attributes.GetDict().ContainsKey(attr))
                    {
                        cls.Methods[v.GetMemberID()].Attributes.GetDict()[attr].Assign(val);
                        return vm.CleanReturn(nargs + 2, true);
                    }
                }

                return vm.AddToErrorMessage("unknown attribute '" + attr + "'");
            }

            return vm.AddToErrorMessage("unknown member or method '" + mem + "'");
        }
        #endregion

        #region WEAKREF
        [ExNativeFuncDelegate("ref", 0, "Return the referenced object", 'w')]
        public static ExFunctionStatus StdWeakRefValue(ExVM vm, int nargs)
        {
            ExObject ret = vm.GetRootArgument();
            if (ret.Type != ExObjType.WEAKREF)
            {
                return vm.AddToErrorMessage("can't get reference value of non-weakref object");
            }

            vm.Push(ret.Value._WeakRef.ReferencedObject);
            return ExFunctionStatus.SUCCESS;
        }
        #endregion

        #region COMMON DELEGATES
        [ExNativeFuncDelegate(ExCommonDelegateType.WEAKREF, 'w')]
        [ExNativeFuncDelegate(ExCommonDelegateType.WEAKREF, 'y')]
        [ExNativeFuncDelegate(ExCommonDelegateType.WEAKREF, 'd')]
        [ExNativeFuncDelegate(ExCommonDelegateType.WEAKREF, 'a')]
        [ExNativeFuncDelegate(ExCommonDelegateType.WEAKREF, 'C')]
        [ExNativeFuncDelegate(ExCommonDelegateType.WEAKREF, 'f')]
        [ExNativeFuncDelegate(ExCommonDelegateType.WEAKREF, 'r')]
        [ExNativeFuncDelegate(ExCommonDelegateType.WEAKREF, 's')]
        [ExNativeFuncDelegate(ExCommonDelegateType.WEAKREF, 'S')]
        [ExNativeFuncDelegate(ExCommonDelegateType.WEAKREF, 'x')]
        public static ExFunctionStatus StdWeakRef(ExVM vm, int nargs)
        {
            ExObject ret = vm.GetRootArgument();
            if (ExTypeCheck.IsCountingRefs(ret))
            {
                vm.Push(ret.Value._RefC.GetWeakRef(ret.Type, ret.Value));
                return ExFunctionStatus.SUCCESS;
            }
            vm.Push(ret);
            return ExFunctionStatus.SUCCESS;
        }

        [ExNativeFuncDelegate(ExCommonDelegateType.LENGTH, 'd')]
        [ExNativeFuncDelegate(ExCommonDelegateType.LENGTH, 'a')]
        [ExNativeFuncDelegate(ExCommonDelegateType.LENGTH, 's')]
        public static ExFunctionStatus StdDefaultLength(ExVM vm, int nargs)
        {
            int size = -1;
            ExObject obj = vm.GetRootArgument();   // Objeyi al
            switch (obj.Type)
            {
                case ExObjType.ARRAY:
                    {
                        size = obj.GetList().Count;
                        break;
                    }
                case ExObjType.DICT:
                    {
                        size = obj.Value.d_Dict.Count;
                        break;
                    }
                case ExObjType.STRING:
                    {
                        size = obj.Value.s_String.Length;
                        break;
                    }
            }
            return vm.CleanReturn(nargs + 2, new ExObject(size));
        }

        #endregion

        #endregion
    }
}
