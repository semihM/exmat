using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using ExMat.InfoVar;
using ExMat.Objects;
using ExMat.OPs;
using ExMat.States;
using ExMat.VM;

namespace ExMat.FuncPrototype
{
    public enum ExClosureType
    {
        FUNCTION,
        RULE,
        CLUSTER,
        MACRO,
        SEQUENCE
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExFuncPro : ExObject
    {
        public int n_instr;
        public int n_lits;
        public int n_params;
        public int n_funcs;
        public int n_outers;
        public int n_lineinfos;
        public int n_localinfos;
        public int n_defparams;
        public int _stacksize;

        public bool _pvars;

        public int i_optstart;

        public ExClosureType type = ExClosureType.FUNCTION;

        public ExObject _name;
        public ExObject _source;
        public ExSState _sState;

        public List<ExInstr> _instr;
        public List<ExObject> _lits;
        public List<ExObject> _params;
        public List<int> _defparams;
        public List<ExFuncPro> _funcs;
        public List<ExLocalInfo> _localinfos;
        public List<ExLineInfo> _lineinfos;
        public List<ExOuterInfo> _outers;

        public static ExFuncPro Create(ExSState sState,
                                       int n_instr,
                                       int n_lits,
                                       int n_params,
                                       int n_funcs,
                                       int n_outers,
                                       int n_lineinfos,
                                       int n_localinfos,
                                       int n_defparams)
        {
            ExFuncPro funcPro = new(sState);

            funcPro.n_instr = n_instr;
            funcPro._instr = new();

            funcPro.n_lits = n_lits;
            funcPro._lits = new();

            funcPro.n_funcs = n_funcs;
            funcPro._funcs = new();

            funcPro.n_params = n_params;
            funcPro._params = new();

            funcPro.n_outers = n_outers;
            funcPro._outers = new();

            funcPro.n_lineinfos = n_lineinfos;
            funcPro._lineinfos = new();

            funcPro.n_localinfos = n_localinfos;
            funcPro._localinfos = new();

            funcPro.n_defparams = n_defparams;
            funcPro._defparams = new();
            //ExUtils.InitList(ref funcPro._defparams, n_defparams);

            return funcPro;
        }

        public ExFuncPro()
        {
            _type = ExObjType.FUNCPRO;
        }

        public ExFuncPro(ExSState ss)
        {
            _type = ExObjType.FUNCPRO;
            _stacksize = 0;
            _sState = ss;
        }

        public new string GetDebuggerDisplay()
        {
            return "FPRO(" + _name.GetString() + ", n_func: " + n_funcs + ", n_lits: " + n_lits + ", n_instr: " + n_instr + ")";
        }

        public bool IsFunction()
        {
            return type == ExClosureType.FUNCTION;
        }
        public bool IsCluster()
        {
            return type == ExClosureType.CLUSTER;
        }
        public bool IsRule()
        {
            return type == ExClosureType.RULE;
        }
        public bool IsMacro()
        {
            return type == ExClosureType.MACRO;
        }
        public bool IsSequence()
        {
            return type == ExClosureType.SEQUENCE;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Disposer.DisposeObjects(_source, _name);

            _lineinfos = null;
            _defparams = null;

            Disposer.DisposeList(ref _outers);
            Disposer.DisposeList(ref _localinfos);
            Disposer.DisposeList(ref _funcs);
            Disposer.DisposeList(ref _params);
            Disposer.DisposeList(ref _lits);
            Disposer.DisposeList(ref _instr);
        }
    }

    public class ExFunc : ExObject
    {
        public ExFunc()
        {
            _type = ExObjType.CLOSURE;
        }
        public ExFunc(MethodInfo m)
        {
            _type = ExObjType.CLOSURE;
            _val._Method = m;
        }
        public int Invoke(ExVM vm, int nargs)
        {
            return (int)_val._Method.Invoke(this, new object[] { vm, nargs });
        }
    }
}
