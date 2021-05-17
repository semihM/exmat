using System;
using System.Collections.Generic;
using System.Diagnostics;
using ExMat.Objects;
using ExMat.OPs;

namespace ExMat.InfoVar
{
    public enum ExOuterType
    {
        LOCAL = 0,
        OUTER
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExLocalInfo : IDisposable
    {
        public ExObject name = new();
        public int _sopc = 0;
        public int _eopc = 0;
        public int _pos = 0;
        private bool disposedValue;

        public ExLocalInfo() { }
        public ExLocalInfo(ExLocalInfo lcl)
        {
            _sopc = lcl._sopc;
            _eopc = lcl._eopc;
            _pos = lcl._pos;
        }
        private string GetDebuggerDisplay()
        {
            return "(" + _sopc + ", " + _eopc + ", " + _pos + ") " + name.GetDebuggerDisplay();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    name.Dispose();
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

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExOuterInfo : IDisposable
    {
        public ExObjectPtr name = new();
        public ExObjectPtr _src = new();
        public ExOuterType _type;
        private bool disposedValue;

        public ExOuterInfo() { }
        public ExOuterInfo(ExObject _name, ExObject src, ExOuterType typ)
        {
            name.Assign(_name);
            _src.Assign(src);
            _type = typ;
        }
        public ExOuterInfo(ExOuterInfo o)
        {
            name.Assign(o.name);
            _src.Assign(o._src);
            _type = o._type;
        }

        private string GetDebuggerDisplay()
        {
            return "::(" + _type.ToString() + ")" + name.GetDebuggerDisplay();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    name.Dispose();
                    _src.Dispose();
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
        public OPC op;
        public int line = 0;

        public ExLineInfo() { }

        private string GetDebuggerDisplay()
        {
            return ">>> LINE " + line + "(" + op.ToString() + ")";
        }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExCallInfo : IDisposable
    {
        public ExInstr _instr;
        public List<ExObjectPtr> _lits;
        public Dictionary<string, ExObjectPtr> _spaces;
        public ExObjectPtr _closure;
        public int _prevbase;
        public int _prevtop;
        public int _target;
        public int n_calls;
        public int _traps;
        public bool _root;
        public List<ExInstr> _instrs;
        public int _idx_instrs;
        private bool disposedValue;

        public ExCallInfo() { }
        public ExCallInfo ShallowCopy()
        {
            return new() { _instr = _instr, n_calls = n_calls, _closure = _closure, _lits = _lits, _prevbase = _prevbase, _prevtop = _prevtop, _root = _root, _target = _target };
        }
        private string GetDebuggerDisplay()
        {
            string i = _instrs == null ? " null" : " " + _instrs.Count;
            return ">>CALLINFO (n_instr:" + i + ", n_idx" + _idx_instrs + ")";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (ExObjectPtr o in _lits)
                    {
                        o.Dispose();
                    }
                    _lits.RemoveAll((ExObjectPtr o) => true);
                    _lits = null;

                    _closure.Dispose();
                    _instr.Dispose();

                    foreach (ExObjectPtr o in _spaces.Values)
                    {
                        o.Dispose();
                    }
                    _spaces = null;

                    foreach (ExInstr o in _instrs)
                    {
                        o.Dispose();
                    }
                    _instrs.RemoveAll((ExInstr o) => true);
                    _instrs = null;
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
        public Node<T> _prev;
        public Node<T> _next;
        public T _val;
        private bool disposedValue;

        public static Node<T> BuildNodesFromList(List<T> l, int start = 0)
        {
            Node<T> first = new() { _val = l[0] };
            int c = l.Count;
            for (int i = 1; i < c; i++)
            {
                first._next = new() { _val = l[i], _prev = first };
                first = first._next;
            }

            while (c > start + 1 && first._prev != null)
            {
                c--;
                first = first._prev;
            }

            return first;
        }

        public T this[int i]
        {
            get
            {
                Node<T> curr = this;
                while (i > 0 && curr._next != null)
                {
                    curr = curr._next;
                    i--;
                }
                return curr._val;
            }
            set
            {
                Node<T> curr = this;
                while (i > 0 && curr._next != null)
                {
                    curr = curr._next;
                    i--;
                }
                curr._val = value;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Node<T> curr = this;
                    while (curr._prev != null)
                    {
                        curr = curr._prev;
                    }

                    if (curr._val != null)
                    {
                        curr._val.Dispose();
                        curr._val = null;
                    }

                    while (curr._next != null)
                    {
                        if (curr._prev != null)
                        {
                            curr._prev._next = null;
                            curr._prev = null;
                        }

                        if (curr._val != null)
                        {
                            curr._val.Dispose();
                            curr._val = null;
                        }
                        curr = curr._next;
                    }

                    if (curr._prev != null)
                    {
                        curr._prev._next = null;
                        curr._prev = null;
                    }

                    if (curr._val != null)
                    {
                        curr._val.Dispose();
                        curr._val = null;
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
