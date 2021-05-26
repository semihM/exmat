using System;
using System.Collections.Generic;
using System.Diagnostics;
using ExMat.Objects;
using ExMat.States;
using ExMat.Utils;

namespace ExMat.Class
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExClassMem : IDisposable
    {
        public ExObject val = new();
        public ExObject attrs = new();
        private bool disposedValue;

        public ExClassMem() { }
        public ExClassMem(ExClassMem mem)
        {
            val = new(mem.val);
            attrs = new(mem.attrs);
        }

        public void Nullify()
        {
            val.Nullify();
            attrs.Nullify();
        }

        public string GetDebuggerDisplay()
        {
            return "CMEM(" + val.GetDebuggerDisplay() + ")";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disposer.DisposeObjects(val, attrs);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExClassMem()
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
    public class ExClass : ExObject
    {
        public ExClass _base;
        public Dictionary<string, ExObject> _members = new();
        public List<ExObject> _metas = new();
        public List<ExClassMem> _defvals = new();
        public List<ExClassMem> _methods = new();
        public ExObject _attrs = new();
        public ExObject _hook = new(0);
        public bool _islocked;
        public int _constridx;
        public int _udsize;
        public ExSState _sState;

        public ExClass()
        {
            _type = ExObjType.CLASS;
            _val._RefC = new();
            ExUtils.InitList(ref _metas, (int)ExMetaM._LAST);
        }
        public ExClass(ExSState exS, ExClass b)
        {
            _sState = exS;
            _type = ExObjType.CLASS;
            _val._RefC = new();
            _base = b;
            _hook = new();
            _islocked = false;
            _constridx = -1;
            _udsize = 0;
            ExUtils.InitList(ref _metas, (int)ExMetaM._LAST);

            if (b != null)
            {
                _constridx = b._constridx;
                _udsize = b._udsize;
                _defvals = new(b._defvals.Count);
                for (int i = 0; i < b._defvals.Count; i++)
                {
                    _defvals.Add(new(b._defvals[i]));
                }
                _methods = new(b._methods.Count);
                for (int i = 0; i < b._methods.Count; i++)
                {
                    _methods.Add(new(b._methods[i]));
                }
            }
        }

        public static ExClass Create(ExSState exs, ExClass b)
        {
            return new(exs, b);
        }

        public static new ExObjType GetType()
        {
            return ExObjType.CLASS;
        }
        public override void Release()
        {
            if ((--_val._RefC._refc) == 0)
            {
                _attrs.Release();
                _defvals.RemoveAll((ExClassMem e) => true); _defvals = null;
                _methods.RemoveAll((ExClassMem e) => true); _methods = null;
                _metas.RemoveAll((ExObject e) => true); _metas = null;
                _members = null;
                if (_base != null)
                {
                    _base.Release();
                }
            }
        }

        public void LockCls()
        {
            _islocked = true;
            if (_base != null && _base._type != ExObjType.NULL)
            {
                _base.LockCls();
            }
        }
        public ExInstance CreateInstance()
        {
            if (!_islocked)
            {
                LockCls();
            }
            return ExInstance.Create(_sState, this);
        }

        public bool GetConstructor(ref ExObject o)
        {
            if (_constridx != -1)
            {
                o.Assign(_methods[_constridx].val);
                return true;
            }
            return false;
        }

        public bool SetAttrs(ExObject key, ExObject val)
        {
            if (_members.ContainsKey(key.GetString()))
            {
                ExObject v = _members[key.GetString()];
                if (v.IsField())
                {
                    _defvals[v.GetMemberID()].attrs.Assign(val);
                }
                else
                {
                    _methods[v.GetMemberID()].attrs.Assign(val);
                }
                return true;
            }
            return false;
        }

        public bool NewSlot(ExSState exs, ExObject key, ExObject val, bool bstat)
        {
            bool bdict = val._type == ExObjType.CLOSURE || val._type == ExObjType.NATIVECLOSURE || bstat;
            if (_islocked && !bdict)
            {
                return false;
            }

            ExObject tmp = new();
            if (_members.ContainsKey(key.GetString()) && (tmp = _members[key.GetString()]).IsField())
            {
                _defvals[_members[key.GetString()].GetMemberID()].val.Assign(val);
                return true;
            }

            if (bdict)
            {
                int metaid;
                if ((val._type == ExObjType.CLOSURE || val._type == ExObjType.NATIVECLOSURE)
                    && (metaid = exs.GetMetaIdx(key.GetString())) != -1)
                {
                    _metas[metaid].Assign(val);
                }
                else
                {
                    ExObject tmpv = val;
                    if (_base != null && val._type == ExObjType.CLOSURE)
                    {
                        tmpv.Assign(val._val._Closure);
                        tmpv.GetClosure()._base = _base;
                        _base._val._RefC._refc++;
                    }
                    else
                    {
                        tmpv.GetClosure()._base = this;
                        _val._RefC._refc++;
                    }

                    if (tmp._type == ExObjType.NULL)
                    {
                        bool bconstr = exs._constructid.GetString() == key.GetString();

                        if (bconstr)
                        {
                            _constridx = _methods.Count;
                        }

                        ExClassMem cm = new();
                        cm.val.Assign(tmpv);
                        _members.Add(key.GetString(), new((int)ExMemberFlag.METHOD | _methods.Count));
                        _methods.Add(cm);
                    }
                    else
                    {
                        _methods[tmp.GetMemberID()].val.Assign(tmpv);
                    }
                }
                return true;
            }

            ExClassMem cmem = new();
            cmem.val.Assign(val);
            _members.Add(key.GetString(), new((int)ExMemberFlag.FIELD | _defvals.Count));
            _defvals.Add(cmem);

            return true;
        }

        public new string GetDebuggerDisplay()
        {
            if (_base != null)
            {
                return "[" + _base.GetDebuggerDisplay() + "]" + "CLASS(c_idx: " + _constridx + ", n_mem: " + _members.Count + ")";
            }
            return "CLASS(c_idx: " + _constridx + ", n_mem: " + _members.Count + ")";
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Disposer.DisposeObjects(_base, _attrs, _hook);

            Disposer.DisposeList(ref _methods);
            Disposer.DisposeList(ref _defvals);
            Disposer.DisposeList(ref _metas);
        }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExInstance : ExDeleg
    {
        public ExSState _sState;
        public ExClass _class;
        public ExObject _hook;
        public List<ExObject> _values;
        public ExInstance()
        {
            _type = ExObjType.INSTANCE;
            _val._RefC = new();
            _values = new();
        }

        public static new ExObjType GetType()
        {
            return ExObjType.INSTANCE;
        }

        public void Init(ExSState exs)
        {
            _hook = new();
            _delegate = new ExObject(_class._metas); // TO-DO keep both metas and members
            _class._val._RefC._refc++;
        }

        public static ExInstance Create(ExSState exS, ExInstance inst)
        {
            ExInstance ins = new() { _sState = exS, _class = inst._class };
            for (int i = 0; i < inst._class._defvals.Count; i++)
            {
                ins._values.Add(new ExObject(inst._class._defvals[i].val));
            }
            ins.Init(exS);
            return ins;
        }

        public static ExInstance Create(ExSState exS, ExClass cls)
        {
            ExInstance ins = new() { _sState = exS, _class = cls };
            for (int i = 0; i < cls._defvals.Count; i++)
            {
                ins._values.Add(new ExObject(cls._defvals[i].val));
            }
            ins.Init(exS);
            return ins;
        }

        public bool GetMeta(int midx, ref ExObject res)
        {
            if (_class._metas[midx]._type != ExObjType.NULL)
            {
                res = _class._metas[midx];
                return true;
            }
            return false;
        }

        public bool IsInstanceOf(ExClass cls)
        {
            ExClass p = _class;
            while (p != null)
            {
                if (p == cls)
                {
                    return true;
                }
                p = p._base;
            }
            return false;
        }

        public override void Release()
        {
            if ((--_val._RefC._refc) == 0)
            {
                if (_class != null)
                {
                    _class.Release();
                }
                _values.RemoveAll((ExObject e) => true); _values = null;
            }
        }

        public new string GetDebuggerDisplay()
        {
            return "INSTANCE(n_vals: " + _values.Count + ")";
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Disposer.DisposeObjects(_class, _hook);
            Disposer.DisposeList(ref _values);
        }
    }
}
