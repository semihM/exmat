using System;
#if DEBUG
using System.Diagnostics;
#endif

namespace ExMat.Objects
{
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExRefC : IDisposable
    {
        public int ReferenceCount;      // Referans sayısı
        public ExWeakRef WeakReference; // Zayıf referans
        private bool disposedValue;

        public ExRefC() { }

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
        protected virtual string GetDebuggerDisplay()
        {
            return "REFC: " + ReferenceCount;
        }
#endif

        protected virtual void Dispose(bool disposing)
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

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
