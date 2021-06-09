using System;
using System.Collections.Generic;
using System.Diagnostics;
using ExMat.Objects;
using ExMat.OPs;

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

    public enum ExOuterType
    {
        LOCAL,
        OUTER
    }
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

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExOuterInfo()
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

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExLineInfo
    {
        public int Position;
        public int Line;

        public ExLineInfo() { }

        private string GetDebuggerDisplay()
        {
            return ">>> LINE " + Line + "(" + Position + ")";
        }
    }

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

    public class Node<T> : IDisposable
        where T : class, IDisposable, new()
    {
        public Node<T> Prev;
        public Node<T> Next;
        public T Value;
        private bool disposedValue;

        public static Node<T> BuildNodesFromList(List<T> l, int start = 0)
        {
            Node<T> first = new() { Value = l[0] };
            int c = l.Count;
            for (int i = 1; i < c; i++)
            {
                first.Next = new() { Value = l[i], Prev = first };
                first = first.Next;
            }

            while (c > start + 1 && first.Prev != null)
            {
                c--;
                first = first.Prev;
            }

            return first;
        }

        public T this[int i]
        {
            get
            {
                Node<T> curr = this;
                while (i > 0 && curr.Next != null)
                {
                    curr = curr.Next;
                    i--;
                }
                return curr.Value;
            }
            set
            {
                Node<T> curr = this;
                while (i > 0 && curr.Next != null)
                {
                    curr = curr.Next;
                    i--;
                }
                curr.Value = value;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Node<T> curr = this;
                    while (curr.Prev != null)
                    {
                        curr = curr.Prev;
                    }

                    if (curr.Value != null)
                    {
                        curr.Value.Dispose();
                        curr.Value = null;
                    }

                    while (curr.Next != null)
                    {
                        if (curr.Prev != null)
                        {
                            curr.Prev.Next = null;
                            curr.Prev = null;
                        }

                        if (curr.Value != null)
                        {
                            curr.Value.Dispose();
                            curr.Value = null;
                        }
                        curr = curr.Next;
                    }

                    if (curr.Prev != null)
                    {
                        curr.Prev.Next = null;
                        curr.Prev = null;
                    }

                    if (curr.Value != null)
                    {
                        curr.Value.Dispose();
                        curr.Value = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
