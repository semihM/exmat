using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ExMat.Objects
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExStack : IDisposable
    {
        public List<ExObject> Values;   // Yığındaki objeler

        public int Size;        // Obje sayısı
        public int Allocated;   // Yığının sahip olabileceği maksimum obje sayısı

        private bool disposedValue;

        public string GetDebuggerDisplay()
        {
            return "STACK(" + Allocated + "): " + Size + (Values == null ? " null" : " " + Values.Count);
        }

        public ExStack() { Size = 0; Allocated = 0; Values = null; }

        public ExStack(ExStack stc)
        {
            CopyFrom(stc);
        }

        public ExObject this[int i]
        {
            get => i >= 0 ? Values[i] : Values[0];
            set => Values[i >= 0 ? i : 0] = value;
        }

        public void Release()
        {
            if (Allocated > 0)
            {
                for (int i = 0; i < Size; i++)
                {
                    Values[i].Release();
                    Values[i] = null;
                }
                Values = null;
            }
        }

        public void Resize(int n, ExObject filler = null)
        {
            if (n > Allocated)
            {
                ReAlloc(n);
            }

            if (n > Size)
            {
                if (filler == null)
                {
                    while (Size < n)
                    {
                        Values[Size++] = new();
                    }
                }
                else
                {
                    while (Size < n)
                    {
                        Values[Size++] = new(filler);
                    }
                }
            }
            else
            {
                for (int i = n; i < Size; i++)
                {
                    Values[i].Release();
                    Values[i] = null;
                }
                Size = n;
            }
        }

        private void ReAlloc(int n)
        {
            n = n > 0 ? n : 4;
            if (Values == null)
            {
                Values = new(n);
                for (int i = 0; i < n; i++)
                {
                    Values.Add(null);
                }
            }
            else
            {
                int dif = n - Allocated;
                if (dif < 0)
                {
                    for (int i = 0; i < -dif; i++)
                    {
                        Values[Allocated - i - 1].Release();
                        Values.RemoveAt(Allocated - i - 1);
                    }
                }
                else
                {
                    for (int i = 0; i < dif; i++)
                    {
                        Values.Add(null);
                    }
                }
            }
            Allocated = n;
        }

        public void CopyFrom(ExStack stc)
        {
            if (Size > 0)
            {
                Resize(0);
            }
            if (stc.Size > Allocated)
            {
                ReAlloc(stc.Size);
            }

            for (int i = 0; i < stc.Size; i++)
            {
                Values[i] = new(stc.Values[i]);
            }
            Size = stc.Size;
        }

        public ExObject Back()
        {
            // Son eklenen objeyi döner
            return Values[Size - 1];
        }

        public ExObject Push(ExObject o)
        {
            if (Allocated <= Size)
            {
                // Yeni boş değerli objeler ekler
                ReAlloc(Size * 2);
            }
            return Values[Size++] = new(o);
        }

        public void Pop()
        {
            // Objenin referansını azaltır ve yerine boş obje atar
            Values[--Size].Release();
            Values[Size] = new();
        }

        public void Insert(int i, ExObject o)
        {
            Resize(Size + 1);
            for (int j = Size; j > i; j--)
            {
                Values[j] = Values[j - 1];
            }
            Values[i].Assign(o);
        }

        public void Remove(int i)
        {
            Values[i].Release();
            Values.RemoveAt(i);
            Size--;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ExDisposer.DisposeList(ref Values);
                    Allocated = 0;
                    Size = 0;
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
