using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExMat.Objects;
using ExMat.Class;
using ExMat.InfoVar;
using ExMat.FuncPrototype;
using ExMat.States;
using ExMat.Utils;

namespace ExMat.Closure
{
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

        public new ExObjType GetType()
        {
            return ExObjType.OUTER;
        }

        public override void Release()
        {
            _refc--;
            if (_refc == 0)
            {
                RemoveFromChain(_sState._GC_CHAIN, this);
            }
        }
    }

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
        public new ExObjType GetType()
        {
            return ExObjType.CLOSURE;
        }

        public ExWeakRef _envweakref;
        public ExClass _base;
        public ExFuncPro _func;
        public List<ExObjectPtr> _outervals;
        public List<ExObjectPtr> _defparams;

    }

    public class ExNativeClosure : ExCollectable
    {
        public ExWeakRef _envweakref;
        public ExObjectPtr _name;
        public ExFunc _func;
        public List<ExObjectPtr> _outervals;
        public List<int> _typecheck;
        public int n_outervals;
        public int n_paramscheck;

        public ExNativeClosure()
        {
            _type = ExObjType.NATIVECLOSURE;
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
        public new ExObjType GetType()
        {
            return ExObjType.NATIVECLOSURE;
        }
    }
}
