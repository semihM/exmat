using System;
#if DEBUG
using System.Diagnostics;
#endif
using ExMat.Objects;

namespace ExMat.ExClass
{
    /// <summary>
    /// Class member class
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public sealed class ExClassMem : IDisposable
    {
        /// <summary>
        /// Value stored
        /// </summary>
        public ExObject Value = new();      // Özellik değeri
        /// <summary>
        /// Custom attributes of this member
        /// </summary>
        public ExObject Attributes = new(); // Alt özellikler tablosu
        private bool disposedValue;

        /// <summary>
        /// Empty constructor
        /// </summary>
        public ExClassMem() { }
        /// <summary>
        /// Shallow copying other member
        /// </summary>
        /// <param name="mem">Other to copy from</param>
        public ExClassMem(ExClassMem mem)
        {
            Value = new(mem.Value);
            Attributes = new(mem.Attributes);
        }

#if DEBUG
        private string GetDebuggerDisplay()
        {
            return "CMEM(" + Value.GetDebuggerDisplay() + ")";
        }
#endif

        internal void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ExDisposer.DisposeObjects(Value, Attributes);
                    Value.Release();
                    Attributes.Release();
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
