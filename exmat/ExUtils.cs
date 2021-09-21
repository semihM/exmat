using System.Collections.Generic;
using System.Linq;
using ExMat.Objects;

namespace ExMat.Utils
{
    /// <summary>
    /// Utility method provider
    /// </summary>
    public static class ExUtils
    {
        /// <summary>
        /// Return ordered list of real number found in given list
        /// </summary>
        /// <param name="lis">List to iterate through</param>
        /// <returns>A new list with real numbers found in <paramref name="lis"/> in ascending order</returns>
        public static List<ExObject> GetOrderedNumericList(List<ExObject> lis)
        {
            return lis.Where(o => ExTypeCheck.IsRealNumber(o))
                    .OrderBy(x => x.GetFloat())
                    .ToList();
        }

        /// <summary>
        /// Return wheter lis has only numeric values
        /// </summary>
        /// <param name="lis">List to check</param>
        /// <returns></returns>
        public static bool AssertNumericArray(ExObject lis)
        {
            foreach (ExObject num in lis.GetList())
            {
                if (!ExTypeCheck.IsNumeric(num))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Get a value between [0,<see cref="int.MaxValue"/>] from given 64bit integer
        /// </summary>
        /// <param name="i">Value to put within limits</param>
        /// <returns>if <c><paramref name="i"/> &gt; 0</c> then if <c><paramref name="i"/> &gt; <see cref="int.MaxValue"/> -> <see cref="int.MaxValue"/></c> else <c><paramref name="i"/></c>
        /// <para>if <c><paramref name="i"/> &lt; 0</c> then if <c><paramref name="i"/> &gt; <see cref="int.MinValue"/> -> -<paramref name="i"/></c> else <c>0</c></para></returns>
        public static int LongTo32NonNegativeIntegerRange(long i)
        {
            if (i < 0)
            {
                i = i > int.MinValue ? System.Math.Abs(i) : 0;
            }

            return i > int.MaxValue ? int.MaxValue : (int)i;
        }

        /// <summary>
        /// Get a value between [<see cref="int.MinValue"/>,<see cref="int.MaxValue"/>] from given 64bit integer
        /// </summary>
        /// <param name="i">Value to put within limits</param>
        /// <returns></returns>
        public static int LongTo32SignedIntegerRange(long i)
        {
            return i < 0 ? i >= int.MinValue ? (int)i : int.MinValue : i > int.MaxValue ? int.MaxValue : (int)i;
        }

        /// <summary>
        /// Append given filler object to given list given times
        /// </summary>
        /// <param name="list">List to append to</param>
        /// <param name="filler">Filler object, <see langword="null"/> for <see cref="ExObjType.NULL"/> fillers</param>
        /// <param name="n">How many times to append</param>
        /// <returns><paramref name="list"/> list</returns>
        public static List<ExObject> AppendFillerNTimes(List<ExObject> list, ExObject filler, int n)
        {
            if (filler == null)
            {
                for (int i = 0; i < n; i++)
                {
                    list.Add(new());
                }
            }
            else
            {
                for (int i = 0; i < n; i++)
                {
                    list.Add(new(filler));
                }
            }
            return list;
        }

        /// <summary>
        /// Create a new shuffled list from given list
        /// </summary>
        /// <param name="list">List to get elements from</param>
        /// <returns>A new list</returns>
        public static List<ExObject> ShuffleList(List<ExObject> list)
        {
            System.Random rand = new();
            List<ExObject> res = GetACopyOf(list).OrderBy(a => rand.Next()).ToList();
            return res;
        }

        /// <summary>
        /// Get a copy of the given list
        /// </summary>
        /// <param name="list">List to copy</param>
        /// <returns>A new list, copy of <paramref name="list"/></returns>
        public static List<ExObject> GetACopyOf(List<ExObject> list)
        {
            List<ExObject> res = new(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                res.Add(new(list[i]));
            }
            return res;
        }

        /// <summary>
        /// Get given amount of random distinct(same index won't be picked again) values from given list.
        /// </summary>
        /// <param name="list">List to pick values from</param>
        /// <param name="n">Amount of items to pick</param>
        /// <returns>A new list with <paramref name="n"/> distinct items picked from <paramref name="list"/></returns>
        public static List<ExObject> GetNRandomObjectsFrom(List<ExObject> list, int n)
        {
            System.Random rand = new();
            List<ExObject> res = new(n);
            List<ExObject> copy = GetACopyOf(list);
            while (n > 0)
            {
                int idx = rand.Next(n--);
                res.Add(new(copy[idx]));
                copy.RemoveAt(idx);
            }
            return res;
        }

        internal static void ShallowAppend<T>(List<T> from, List<T> to, int start = 0)
        {
            for (; start < from.Count; start++)
            {
                to.Add(from[start]);
            }
        }

        /// <summary>
        /// Initialize given list object with null objects
        /// </summary>
        /// <typeparam name="T">Class with empty constructor</typeparam>
        /// <param name="lis">List to initialize</param>
        /// <param name="n">Length of the list</param>
        public static void InitList<T>(ref List<T> lis, int n) where T : class, new()
        {
            lis = new(n);
            for (int i = 0; i < n; i++)
            {
                lis.Add(new());
            }
        }

        /// <summary>
        /// Initialize given list object filled with given filler objects
        /// </summary>
        /// <param name="lis">List to initialize</param>
        /// <param name="n">Length of the list</param>
        /// <param name="filler">Filler object</param>
        public static void InitList(ref List<ExObject> lis, int n, ExObject filler)
        {
            lis = new(n);
            for (int i = 0; i < n; i++)
            {
                lis.Add(new(filler));
            }
        }

        /// <summary>
        /// Expand list with <typeparamref name="T"/> values to have the new given length
        /// </summary>
        /// <typeparam name="T">Class with empty constructor</typeparam>
        /// <param name="lis"></param>
        /// <param name="n"></param>
        public static void ExpandListTo<T>(List<T> lis, int n) where T : class, new()
        {
            n -= lis.Count;
            for (int i = 0; i < n; i++)
            {
                lis.Add(new());
            }
        }
    }
}
