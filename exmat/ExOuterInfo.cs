using System;
using System.Diagnostics;
using ExMat.Objects;

namespace ExMat.InfoVar
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExOuterInfo : IDisposable
    {
        public ExObject Name = new();   // Değişken ismi
        public ExObject Index = new();  // Sanal bellekteki indeks
        public ExOuterType Type;        // 
        private bool disposedValue;

        public ExOuterInfo() { }
        public ExOuterInfo(ExObject n, ExObject src, ExOuterType typ)
        {
            Name.Assign(n);
            Index.Assign(src);
            Type = typ;
        }
        public ExOuterInfo(ExOuterInfo o)
        {
            Name.Assign(o.Name);
            Index.Assign(o.Index);
            Type = o.Type;
        }

        private string GetDebuggerDisplay()
        {
            return "::(" + Type.ToString() + ")" + Name.GetDebuggerDisplay();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disposer.DisposeObjects(Name, Index);
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
