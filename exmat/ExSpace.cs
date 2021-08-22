using System;
using System.Diagnostics;

namespace ExMat.Objects
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExSpace : IDisposable
    {
        public int Dimension = 1;
        public string Domain = string.Empty;
        public char Sign = '\\';

        public ExSpace Child;
        private bool disposedValue;

        public string GetString()
        {
            return "@" + Domain + "@" + Sign + "@" + Dimension + (Child == null ? "" : "$$" + Child.GetString());
        }

        public static ExSpace GetSpaceFromString(string s)
        {
            ExSpace spc = new();
            ExSpace curr = spc;
            foreach (string ch in s.Split("$$", StringSplitOptions.RemoveEmptyEntries))
            {
                ExSpace c = new();
                string[] arr = ch.Split("@", StringSplitOptions.RemoveEmptyEntries);
                c.Domain = arr[0];
                c.Sign = arr[1][0];
                c.Dimension = int.Parse(arr[2]);
                curr.Child = c;
                curr = c;
            }
            return spc.Child;
        }
        public ExSpace() { }
        public ExSpace(int dim, string dom, char sign, ExSpace ch)
        {
            Dimension = dim;
            Sign = sign;
            Domain = dom;
            Child = ch;
        }

        public void AddDimension(ExSpace ch)
        {
            Child = new(ch.Dimension, ch.Domain, ch.Sign, null);
        }

        public ExSpace(string spc, int d, char s = '\\')
        {
            Domain = spc;
            Dimension = d;
            Sign = s;
        }

        public static ExObject Create(string spc, char s, params int[] dims)
        {
            return new(CreateSpace(spc, s, dims));
        }

        public static ExSpace CreateSpace(string spc, char s, params int[] dims)
        {
            if (dims.Length == 0)
            {
                return null;
            }
            if (dims.Length == 1)
            {
                return new(spc, dims[0], s);
            }

            ExSpace p = new(spc, dims[0], s);
            ExSpace ch = new(spc, dims[1], s);
            p.Child = ch;

            ExSpace curr = ch;
            for (int i = 2; i < dims.Length; i++)
            {
                ch = new(spc, dims[i], s);
                curr.Child = ch;
                curr = ch;
            }

            return p;
        }

        public string GetSpaceString()
        {
            string s = "SPACE(" + Domain + ", " + (Dimension == -1 ? "var" : Dimension) + (Sign == '\\' ? ")" : ", " + Sign + ")");
            if (Child != null)
            {
                s += " x " + Child.GetSpaceString();
            }
            return s;
        }
        public virtual string GetDebuggerDisplay()
        {
            return GetSpaceString();
        }

        public static void Copy(ExSpace p, ExSpace ch)
        {
            p.Dimension = ch.Dimension;
            p.Sign = ch.Sign;
            p.Domain = ch.Domain;
            p.Child = ch.Child;
        }

        public ExSpace DeepCopy()
        {
            ExSpace s = new();
            s.Sign = Sign;
            s.Dimension = Dimension;
            s.Domain = Domain;
            ExSpace ch = Child;

            if (ch != null)
            {
                s.Child = ch.DeepCopy();
            }

            return s;
        }

        public ExSpace this[int i]
        {
            get
            {
                ExSpace ch = Child;
                while (i > 0 && ch != null)
                {
                    i--;
                    ch = ch.Child;
                }
                return ch;
            }
        }

        public int VarCount()
        {
            int d = Dimension == -1 ? 1 : 0;
            ExSpace ch = Child;
            while (ch != null)
            {
                d += ch.Dimension == -1 ? 1 : 0;
                ch = ch.Child;
            }
            return d;
        }

        public int Depth()
        {
            int d = 1;
            ExSpace ch = Child;
            while (ch != null)
            {
                d++;
                ch = ch.Child;
            }
            return d;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Domain = null;
                    Dimension = 0;
                    if (Child != null)
                    {
                        Child.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExSpace()
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
