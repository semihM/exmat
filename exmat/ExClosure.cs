using System.Collections.Generic;
using System.Diagnostics;
using ExMat.Class;
using ExMat.FuncPrototype;
using ExMat.Objects;
using ExMat.States;
using ExMat.Utils;

namespace ExMat.Closure
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExOuter : ExObject
    {
        public ExObject _valptr;
        public int idx;
        public ExObject _v;
        public ExOuter f_next;
        public ExOuter _prev;
        public ExOuter _next;
        public ExSState _sState;

        public ExOuter()
        {
            _type = ExObjType.OUTER;
            _val._RefC = new();
        }

        public static ExOuter Create(ExSState exS, ExObject o)
        {
            ExOuter exo = new() { _sState = exS, _valptr = new(o), f_next = null };
            return exo;
        }

        public static new ExObjType GetType()
        {
            return ExObjType.OUTER;
        }

        public override void Release()
        {
            if (--_val._RefC._refc == 0)
            {
                _sState = null;
                if (_prev != null)
                {
                    _prev._next = _next;
                    _prev = null;
                }
                if (_next != null)
                {
                    _next._prev = _prev;
                    _next = null;
                }
            }
        }

        public new string GetDebuggerDisplay()
        {
            return "OUTER(" + idx + ", *(" + (_valptr == null ? "null" : _valptr.GetInt()) + "), " + (_v == null ? "null" : _v._type.ToString()) + ")";
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Disposer.DisposeObjects(_valptr, _v);

            if (_prev != null)
            {
                _prev._next = _next;
                _prev = null;
            }
            if (_next != null)
            {
                _next._prev = _prev;
                _next = null;
            }

            f_next = null;
        }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExClosure : ExObject
    {
        public ExWeakRef _envweakref;
        public ExClass _base;
        public ExPrototype _func;
        public List<ExObject> _outervals;
        public List<ExObject> _defparams;
        public ExSState _sState;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _base = null;

            _envweakref = null;
            Disposer.DisposeObjects(_func);
            Disposer.DisposeList(ref _outervals);
            Disposer.DisposeList(ref _defparams);
        }

        public static ExClosure Create(ExSState exS, ExPrototype fpro)
        {
            ExClosure cls = new() { _sState = exS, _func = fpro };
            ExUtils.InitList(ref cls._outervals, fpro.n_outers);
            ExUtils.InitList(ref cls._defparams, fpro.n_defparams);
            return cls;
        }

        public ExClosure()
        {
            _type = ExObjType.CLOSURE;
            _val._RefC = new() { _refc = 1 };
        }

        public ExClosure Copy()
        {
            ExPrototype fp = _func;
            ExClosure res = Create(_sState, fp);
            res._envweakref = _envweakref;
            if (res._envweakref != null)
            {
                res._envweakref._val._RefC._refc++;
            }
            for (int i = 0; i < fp.n_outers; i++)
            {
                res._outervals[i].Assign(_outervals[i]);
            }
            for (int i = 0; i < fp.n_defparams; i++)
            {
                res._defparams[i].Assign(_defparams[i]);
            }
            return res;
        }

        public override void Release()
        {
            base.Release();
            foreach (ExObject o in _outervals)
            {
                o.Nullify();
            }
            _outervals = null;

            foreach (ExObject o in _defparams)
            {
                o.Nullify();
            }
            _defparams = null;
        }
        public static new ExObjType GetType()
        {
            return ExObjType.CLOSURE;
        }

        public new string GetDebuggerDisplay()
        {
            if (_func._name == null || _func._name._type == ExObjType.NULL)
            {
                return "CLOSURE";
            }
            if (_base != null)
            {
                return "[HAS BASE] CLOSURE(" + _func._name.GetString() + ")";
            }
            return "CLOSURE(" + _func._name.GetString() + ")";
        }


    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExNativeClosure : ExObject
    {
        public ExSState _sState;
        public ExWeakRef _envweakref;
        public ExObject _name;
        public ExFunc _func;
        public List<ExObject> _outervals;
        public List<int> _typecheck = new();
        public Dictionary<int, ExObject> d_defaults = new();
        public int n_outervals;
        public int n_paramscheck;
        public bool b_deleg = false;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _envweakref = null;
            _typecheck = null;
            n_outervals = 0;
            n_paramscheck = 0;

            Disposer.DisposeObjects(_name, _func);
            Disposer.DisposeList(ref _outervals);
            Disposer.DisposeDict(ref d_defaults);
        }

        public ExNativeClosure()
        {
            _type = ExObjType.NATIVECLOSURE;
            _val._RefC = new() { _refc = 1 };
        }

        public static ExNativeClosure Create(ExSState exS, ExFunc f, int nout)
        {
            ExNativeClosure cls = new() { _sState = exS, _func = f };
            ExUtils.InitList(ref cls._outervals, nout);
            cls.n_outervals = nout;
            return cls;
        }

        public override void Release()
        {
            base.Release();
            foreach (ExObject o in _outervals)
            {
                o.Nullify();
            }
            _outervals = null;
        }
        public static new ExObjType GetType()
        {
            return ExObjType.NATIVECLOSURE;
        }

        public new string GetDebuggerDisplay()
        {
            return "NATIVECLOSURE(" + _name.GetString() + ")";
        }
    }
}
