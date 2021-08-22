using System;
using System.Diagnostics;

namespace ExMat.Objects
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExRefC : IDisposable
    {
        public int ReferenceCount;      // Referans sayısı
        public ExWeakRef WeakReference; // Zayıf referans
        private bool disposedValue;

        public ExRefC() { }

        public ExWeakRef GetWeakRef(ExObjType t, ExObjVal v)
        {
            if (WeakReference == null)
            {
                ExWeakRef e = new();
                e.ReferencedObject = new();
                e.ReferencedObject.Type = t;
                e.ReferencedObject.Value = v;
                WeakReference = e;
            }
            return WeakReference;
        }

        public string GetDebuggerDisplay()
        {
            return "REFC: " + ReferenceCount;
        }

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

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExRefC()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
