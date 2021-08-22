using System;
using System.Diagnostics;
using ExMat.Objects;

namespace ExMat.InfoVar
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExLocalInfo : IDisposable
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
        private string GetDebuggerDisplay()
        {
            return "(" + StartOPC + ", " + EndOPC + ", " + Position + ") " + Name.GetDebuggerDisplay();
        }

        protected virtual void Dispose(bool disposing)
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

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExLocalInfo()
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
