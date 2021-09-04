using System.Collections.Generic;
using System.Linq;
using ExMat.Objects;

namespace ExMat.Utils
{
    public static class ExUtils
    {
        public static bool AssertNumericArray(ExObject lis)
        {
            foreach (ExObject num in lis.GetList())
            {
                if (!num.IsNumeric())
                {
                    return false;
                }
            }
            return true;
        }

        public static int LongTo32NonNegativeIntegerRange(long i)
        {
            if (i < 0)
            {
                i = i > int.MinValue ? System.Math.Abs(i) : 0;
            }

            return i > int.MaxValue ? int.MaxValue : (int)i;
        }

        public static int LongTo32SignedIntegerRange(long i)
        {
            if (i < 0)
            {
                return i >= int.MinValue ? (int)i : int.MinValue;
            }
            else
            {
                return i > int.MaxValue ? int.MaxValue : (int)i;
            }
        }

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

        public static List<ExObject> ShuffleList(List<ExObject> list)
        {
            System.Random rand = new();
            List<ExObject> res = GetACopyOf(list).OrderBy(a => rand.Next()).ToList();
            return res;
        }

        public static List<ExObject> GetACopyOf(List<ExObject> list)
        {
            List<ExObject> res = new(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                res.Add(new(list[i]));
            }
            return res;
        }

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

        public static void InitList<T>(ref List<T> lis, int n) where T : class, new()
        {
            lis = new(n);
            for (int i = 0; i < n; i++)
            {
                lis.Add(new());
            }
        }
        public static void InitList(ref List<int> lis, int n)
        {
            lis = new(n);
            for (int i = 0; i < n; i++)
            {
                lis.Add(0);
            }
        }
        public static void InitList(ref List<dynamic> lis, int n)
        {
            lis = new(n);
            for (int i = 0; i < n; i++)
            {
                lis.Add(null);
            }
        }
        public static void InitList(ref List<ExObject> lis, int n, ExObject filler)
        {
            lis = new(n);
            for (int i = 0; i < n; i++)
            {
                lis.Add(new(filler));
            }
        }

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
