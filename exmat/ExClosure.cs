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
    public class ExOuter : ExCollectable
    {
        public ExObjectPtr _valptr;
        public int idx;
        public ExObjectPtr _v;
        public ExOuter f_next;
        public new ExOuter _prev;
        public new ExOuter _next;

        public ExOuter()
        {
            _type = ExObjType.OUTER;
        }

        public static ExOuter Create(ExSState exS, ExObjectPtr o)
        {
            ExOuter exo = new() { _sState = exS, _valptr = o, f_next = null };
            return exo;
        }

        public static new ExObjType GetType()
        {
            return ExObjType.OUTER;
        }

        public override void Release()
        {
            _refc--;
            if (_refc == 0)
            {
                RemoveFromChain(ref _sState._GC_CHAIN, this);
            }
        }

        public new string GetDebuggerDisplay()
        {
            return "OUTER(" + idx + ", *(" + _valptr.ToString() + "), " + _v.ToString() + ")";
        }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExClosure : ExCollectable
    {
        public static ExClosure Create(ExSState exS, ExFuncPro fpro)
        {
            ExClosure cls = new() { _sState = exS, _func = fpro };
            ExUtils.InitList(ref cls._outervals, fpro.n_outers);
            ExUtils.InitList(ref cls._defparams, fpro.n_defparams);
            return cls;
        }

        public ExClosure()
        {
            _type = ExObjType.CLOSURE;
            _refc = 1;
        }

        public ExClosure Copy()
        {
            ExFuncPro fp = _func;
            ExClosure res = Create(_sState, fp);
            res._envweakref = _envweakref;
            if (res._envweakref != null)
            {
                res._envweakref._refc++;
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
            foreach (ExObjectPtr o in _outervals)
            {
                o.Nullify();
            }
            _outervals = null;

            foreach (ExObjectPtr o in _defparams)
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

        public ExWeakRef _envweakref;
        public ExClass _base;
        public ExFuncPro _func;
        public List<ExObjectPtr> _outervals;
        public List<ExObjectPtr> _defparams;

    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExNativeClosure : ExCollectable
    {
        public ExWeakRef _envweakref;
        public ExObjectPtr _name;
        public ExFunc _func;
        public List<ExObjectPtr> _outervals;
        public List<int> _typecheck = new();
        public int n_outervals;
        public int n_paramscheck;
        public bool b_deleg = false;
        public ExNativeClosure()
        {
            _type = ExObjType.NATIVECLOSURE;
            _refc = 1;
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
            foreach (ExObjectPtr o in _outervals)
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
