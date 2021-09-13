using System;
using System.Diagnostics;
using ExMat.Objects;

namespace ExMat.ExClass
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExClassMem : IDisposable
    {
        public ExObject Value = new();      // Özellik değeri
        public ExObject Attributes = new(); // Alt özellikler tablosu
        private bool disposedValue;

        public ExClassMem() { }
        public ExClassMem(ExClassMem mem)
        {
            Value = new(mem.Value);
            Attributes = new(mem.Attributes);
        }

        public void Nullify()
        {
            Value.Nullify();
            Attributes.Nullify();
        }

        public string GetDebuggerDisplay()
        {
            return "CMEM(" + Value.GetDebuggerDisplay() + ")";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ExDisposer.DisposeObjects(Value, Attributes);
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
