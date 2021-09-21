using System;
using System.Collections.Generic;

namespace ExMat.Objects
{
    /// <summary>
    /// Object disposer method provider
    /// </summary>
    public static class ExDisposer
    {
        /// <summary>
        /// Dispose given list and assign <see langword="null"/> to it
        /// </summary>
        /// <typeparam name="T">List element type</typeparam>
        /// <param name="lis">List of disposable objects</param>
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
        /// <summary>
        /// Dispose given dictionary and assign <see langword="null"/> to it
        /// </summary>
        /// <typeparam name="TKey">Type doesn't matter</typeparam>
        /// <typeparam name="TValue">Disposable dictionary value type</typeparam>
        /// <param name="dict">Dictionary to dispose</param>
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

        /// <summary>
        /// Dispose given objects
        /// </summary>
        /// <typeparam name="T">Disposable type</typeparam>
        /// <param name="ps">Objects to dispose</param>
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

        /// <summary>
        /// Dispose given object and assign <see langword="null"/> to it
        /// </summary>
        /// <typeparam name="T">Disposable type</typeparam>
        /// <param name="o">Object to dispose</param>
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
