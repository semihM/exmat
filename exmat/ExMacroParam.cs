using System;
using System.Collections.Generic;

namespace ExMat.Lexer
{
    public class ExMacroParam : IDisposable
    {
        public List<int> Lines = new();
        public List<int> Columns = new();
        public string Name;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Name = null;
                    Columns = null;
                    Lines = null;
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
