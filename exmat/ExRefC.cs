using System;
#if DEBUG
using System.Diagnostics;
#endif

namespace ExMat.Objects
{
    /// <summary>
    /// Reference counter class
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExRefC : IDisposable
    {
        /// <summary>
        /// Reference count
        /// </summary>
        public int ReferenceCount;      // Referans sayısı
        /// <summary>
        /// Weakly referenced object if any
        /// </summary>
        public ExWeakRef WeakReference; // Zayıf referans
        private bool disposedValue;

        /// <summary>
        /// Empty constructor
        /// </summary>
        public ExRefC() { }

        /// <summary>
        /// Get or construct the weak reference object
        /// </summary>
        /// <param name="t">If there is no weakref, assign this type</param>
        /// <param name="v">If there is no weakref, assign this basic value</param>
        /// <param name="vc">If there is no weakref, assign this custom value</param>
        /// <returns>Weak reference found or constructed</returns>
        public ExWeakRef GetWeakRef(ExObjType t, ExObjVal v, ExObjValCustom vc)
        {
            if (WeakReference == null)
            {
                ExWeakRef e = new();
                e.ReferencedObject = new();
                e.ReferencedObject.Type = t;
                e.ReferencedObject.Value = v;
                e.ReferencedObject.ValueCustom = vc;
                e.ReferencedObject.ValueCustom._RefC = this;
                WeakReference = e;
            }
            return WeakReference;
        }

#if DEBUG
        internal virtual string GetDebuggerDisplay()
        {
            return "REFC: " + ReferenceCount;
        }
#endif

        internal virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    WeakReference = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }
        /// <summary>
        /// Disposer
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
