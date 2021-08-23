using System;
using System.Collections.Generic;

namespace ExMat.InfoVar
{
    public class ExNode<T> : IDisposable
        where T : class, IDisposable, new()
    {
        public ExNode<T> Prev;
        public ExNode<T> Next;
        public T Value;
        private bool disposedValue;

        public static ExNode<T> BuildNodesFromList(List<T> l, int start = 0)
        {
            ExNode<T> first = new() { Value = l[0] };
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
                ExNode<T> curr = this;
                while (i > 0 && curr.Next != null)
                {
                    curr = curr.Next;
                    i--;
                }
                return curr.Value;
            }
            set
            {
                ExNode<T> curr = this;
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
                    if (Value != null)
                    {
                        Value = null;
                    }

                    if (Next != null)
                    {
                        Next.Prev = Prev;
                    }

                    if (Prev != null)
                    {
                        Prev.Next = Next;
                    }

                    Prev = null;
                    Next = null;
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
