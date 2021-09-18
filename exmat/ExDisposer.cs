using System;
using System.Collections.Generic;

namespace ExMat.Objects
{
    public static class ExDisposer
    {
        public static void DisposeList<T>(ref List<T> lis) where T : IDisposable, new()
        {
            if (lis == null)
            {
                return;
            }
            foreach (T o in lis)
            {
                o.Dispose();
            }
            lis.RemoveRange(0, lis.Count);
            lis = null;
        }
        public static void DisposeDict<TKey, TValue>(ref Dictionary<TKey, TValue> dict)
            where TValue : IDisposable, new()
        {
            if (dict == null)
            {
                return;
            }
            foreach (KeyValuePair<TKey, TValue> pair in dict)
            {
                pair.Value.Dispose();
            }
            dict = null;
        }

        public static void DisposeObjects<T>(params T[] ps) where T : IDisposable, new()
        {
            foreach (T o in ps)
            {
                if (o != null)
                {
                    o.Dispose();
                }
            }
        }

        public static void DisposeObject<T>(ref T o) where T : IDisposable, new()
        {
            if (o != null)
            {
                o.Dispose();
                o = default;
            }
        }
    }
}
