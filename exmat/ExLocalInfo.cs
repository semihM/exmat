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
    internal sealed class ExLocalInfo : IDisposable
    {
        public ExObject Name = new();   // Değişken ismi
        public int StartOPC;            // Değişkenin tanımlandığı komut indeksi
        public int EndOPC;              // Değişkenin silineceği komut indeksi
        public int Position;            // Değişken listesi içindeki sıra indeksi

        private bool disposedValue;

        public ExLocalInfo() { }
        public ExLocalInfo(ExLocalInfo lcl)
        {
            StartOPC = lcl.StartOPC;
            EndOPC = lcl.EndOPC;
            Position = lcl.Position;
        }
#if DEBUG
        private string GetDebuggerDisplay()
        {
            return "(" + StartOPC + ", " + EndOPC + ", " + Position + ") " + Name.GetDebuggerDisplay();
        }
#endif

        internal void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Name.Dispose();
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
