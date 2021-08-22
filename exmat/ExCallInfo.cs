using System;
using System.Collections.Generic;
using System.Diagnostics;
using ExMat.Objects;
using ExMat.OPs;

namespace ExMat.InfoVar
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExCallInfo : IDisposable
    {
        public List<ExObject> Literals; // Değişken isimleri, yazı dizileri vs.
        public ExObject Closure;        // İçinde bulunulan fonksiyon/kod bloğu

        public int PrevBase;            // Çağrı öncesi taban bellek indeksi
        public int PrevTop;             // Çağrı öncesi tavan bellek indeksi
        public int Target;              // Çağrının döneceği değerin hedef indeksi

        public int nCalls;              // Çağrı sayısı

        public bool IsRootCall;             // Kök çağrı(main) mı ?
        public List<ExInstr> Instructions;  // Komutlar listesi
        public int InstructionsIndex;       // Komut indeksi

        private bool disposedValue;

        public ExCallInfo() { }
        public ExCallInfo ShallowCopy()
        {
            return new() { nCalls = nCalls, Closure = Closure, Literals = Literals, PrevBase = PrevBase, PrevTop = PrevTop, IsRootCall = IsRootCall, Target = Target };
        }
        private string GetDebuggerDisplay()
        {
            string i = Instructions == null ? " null" : " " + Instructions.Count;
            return ">>CALLINFO (n_instr:" + i + ", n_idx" + InstructionsIndex + ")";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disposer.DisposeList(ref Literals);
                    Disposer.DisposeObjects(Closure);

                    Instructions.RemoveAll((ExInstr i) => true);
                    Instructions = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExCallInfo()
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
