using System;
#if DEBUG
using System.Diagnostics;
#endif
using ExMat.Objects;

namespace ExMat.InfoVar
{
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    internal class ExOuterInfo : IDisposable
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

#if DEBUG
        private string GetDebuggerDisplay()
        {
            return "::(" + Type.ToString() + ")" + Name.GetDebuggerDisplay();
        }
#endif

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ExDisposer.DisposeObjects(Name, Index);
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
