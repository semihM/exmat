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
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExFuncPro : ExCollectable
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

        public bool is_rule;

        public bool is_cluster;
        public int i_optstart;

        public bool is_macro;

        public ExObjectPtr _name;
        public ExObjectPtr _source;

        public List<ExInstr> _instr;
        public List<ExObjectPtr> _lits;
        public List<ExObjectPtr> _params;
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
            _next = null;
            _prev = null;
            _sState = ss;
            AddToChain(_sState._GC_CHAIN, this);
        }

        public new string GetDebuggerDisplay()
        {
            return "FPRO(" + _name.GetString() + ", n_func: " + n_funcs + ", n_lits: " + n_lits + ", n_instr: " + n_instr + ")";
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _source.Dispose();
            _name.Dispose();

            foreach (ExOuterInfo o in _outers)
            {
                o.Dispose();
            }
            _outers.RemoveAll((ExOuterInfo o) => true);
            _outers = null;

            _lineinfos.RemoveAll((ExLineInfo o) => true);
            _lineinfos = null;

            foreach (ExLocalInfo o in _localinfos)
            {
                o.Dispose();
            }
            _localinfos.RemoveAll((ExLocalInfo o) => true);
            _localinfos = null;

            foreach (ExFuncPro o in _funcs)
            {
                o.Dispose();
            }
            _funcs.RemoveAll((ExFuncPro o) => true);
            _funcs = null;

            _defparams = null;

            foreach (ExObjectPtr o in _params)
            {
                o.Dispose();
            }
            _params.RemoveAll((ExObjectPtr o) => true);
            _params = null;

            foreach (ExObjectPtr o in _lits)
            {
                o.Dispose();
            }
            _lits.RemoveAll((ExObjectPtr o) => true);
            _lits = null;

            foreach (ExInstr o in _instr)
            {
                o.Dispose();
            }
            _instr.RemoveAll((ExInstr o) => true);
            _instr = null;
        }
    }

    public class ExFunc : ExObjectPtr
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
