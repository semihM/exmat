using System;
#if DEBUG
using System.Diagnostics;
#endif
using System.Globalization;

namespace ExMat.Objects
{
    /// <summary>
    /// Space object model
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExSpace : ExRefC
    {
        /// <summary>
        /// Dimension
        /// </summary>
        public int Dimension = 1;
        /// <summary>
        /// Domain: R, Z, C, E ; Lower case to exclude zero (except for E)
        /// </summary>
        public string Domain = string.Empty;
        /// <summary>
        /// Sign: +, -, \\ (Any)
        /// </summary>
        public char Sign = '\\';

        /// <summary>
        /// Inner space for extra dimensions
        /// </summary>
        public ExSpace Child;

        /// <summary>
        /// Get string representation
        /// </summary>
        /// <returns></returns>
        public string GetString()
        {
            return "@" + Domain + "@" + Sign + "@" + Dimension + (Child == null ? "" : "$$" + Child.GetString());
        }

        internal static ExSpace GetSpaceFromString(string s)
        {
            ExSpace spc = new();
            ExSpace curr = spc;
            foreach (string ch in s.Split("$$", StringSplitOptions.RemoveEmptyEntries))
            {
                ExSpace c = new();
                string[] arr = ch.Split("@", StringSplitOptions.RemoveEmptyEntries);
                c.Domain = arr[0];
                c.Sign = arr[1][0];
                c.Dimension = int.Parse(arr[2], CultureInfo.CurrentCulture);
                curr.Child = c;
                curr = c;
            }
            return spc.Child;
        }
        /// <summary>
        /// Empty constructor
        /// </summary>
        public ExSpace() { }
        /// <summary>
        /// Space with given dimension, domain, sign and inner space
        /// </summary>
        /// <param name="dim">Dimension</param>
        /// <param name="dom">Domain</param>
        /// <param name="sign">Sign</param>
        /// <param name="ch">Extras</param>
        public ExSpace(int dim, string dom, char sign, ExSpace ch)
        {
            Dimension = dim;
            Sign = sign;
            Domain = dom;
            Child = ch;
        }

        /// <summary>
        /// Space with given domain, dimension, sign
        /// </summary>
        /// <param name="spc">Domain</param>
        /// <param name="d">Dimension</param>
        /// <param name="s">Sign</param>
        public ExSpace(string spc, int d, char s = '\\')
        {
            Domain = spc;
            Dimension = d;
            Sign = s;
        }

        /// <summary>
        /// Add given space as dimension
        /// </summary>
        /// <param name="ch"></param>
        public void AddDimension(ExSpace ch)
        {
            Child = new(ch.Dimension, ch.Domain, ch.Sign, ch.Child);
        }

        /// <summary>
        /// Return a new space instance with given values
        /// </summary>
        /// <param name="spc"></param>
        /// <param name="s"></param>
        /// <param name="dims"></param>
        /// <returns></returns>
        public static ExObject Create(string spc, char s, params int[] dims)
        {
            return new(CreateSpace(spc, s, dims));
        }

        private static ExSpace CreateSpace(string spc, char s, params int[] dims)
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

        /// <summary>
        /// Get informational string represtation
        /// </summary>
        /// <returns></returns>
        public string GetSpaceString()
        {
            string s = "SPACE(" + Domain + ", " + (Dimension == -1 ? "var" : Dimension) + (Sign == '\\' ? ")" : ", " + Sign + ")");
            if (Child != null)
            {
                s += " x " + Child.GetSpaceString();
            }
            return s;
        }

#if DEBUG
        internal override string GetDebuggerDisplay()
        {
            return GetSpaceString();
        }
#endif
        /// <summary>
        /// Get a copy of the given space
        /// </summary>
        /// <param name="to">Destination</param>
        /// <param name="from">Source</param>
        public static void Copy(ExSpace to, ExSpace from)
        {
            to.Dimension = from.Dimension;
            to.Sign = from.Sign;
            to.Domain = from.Domain;
            to.Child = from.Child;
        }

        /// <summary>
        /// Get a new and exact copy of the space
        /// </summary>
        /// <returns></returns>
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

        internal int VarCount()
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

        internal override void Dispose(bool disposing)
        {
            if (ReferenceCount > 0)
            {
                return;
            }
            base.Dispose(disposing);

            Domain = null;
            Dimension = 0;

            if (Child != null)
            {
                Child.Dispose();
            }
        }
    }
}
