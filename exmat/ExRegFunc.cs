using System;
using System.Collections.Generic;
using ExMat.VM;

namespace ExMat.Objects
{
    public class ExRegFunc : IDisposable
    {
        public string Name;             // Fonksiyon ismi

        public delegate ExFunctionStatus FunctionRef(ExVM vm, int nargs);
        public FunctionRef Function;    // Fonksiyon referansı

        public string ParameterMask;    // Parameter tipleri maskesi
        public int nParameterChecks;    // Argüman sayısı kontrolü

        public bool IsDelegateFunction; // Temsili(delegate) fonksiyon ?

        public Dictionary<int, ExObject> DefaultValues = new(); // Varsayılan değerler

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ParameterMask = null;
                    Name = null;
                    Function = null;
                    nParameterChecks = 0;
                    Disposer.DisposeDict(ref DefaultValues);
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
