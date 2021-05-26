using System.Collections.Generic;
using ExMat.Objects;

namespace ExMat.Utils
{
    public static class ExUtils
    {
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
