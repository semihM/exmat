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
        SEQUENCE
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExPrototype : ExRefC
    {
        public int nInstr;
        public int nLits;
        public int nParams;
        public int nFuncs;
        public int nOuters;
        public int nLineInfos;
        public int nLocalInfos;
        public int nDefaultParameters;
        public int StackSize;
        public bool HasVargs;

        public ExClosureType ClosureType = ExClosureType.FUNCTION;

        public ExObject Name;
        public ExObject Source;
        public ExSState SharedState;
        public List<ExInstr> Instructions;
        public List<ExObject> Literals;
        public List<ExObject> Parameters;
        public List<int> DefaultParameters;
        public List<ExPrototype> Functions;
        public List<ExLocalInfo> LocalInfos;
        public List<ExLineInfo> LineInfos;
        public List<ExOuterInfo> Outers;

        public static ExPrototype Create(ExSState sState,
                                       int nInstr,
                                       int nLits,
                                       int nParams,
                                       int nFuncs,
                                       int nOuters,
                                       int nLineinfos,
                                       int nLocalinfos,
                                       int nDefparams)
        {
            ExPrototype funcPro = new(sState);

            funcPro.nInstr = nInstr;
            funcPro.Instructions = new();

            funcPro.nLits = nLits;
            funcPro.Literals = new();

            funcPro.nFuncs = nFuncs;
            funcPro.Functions = new();

            funcPro.nParams = nParams;
            funcPro.Parameters = new();

            funcPro.nOuters = nOuters;
            funcPro.Outers = new();

            funcPro.nLineInfos = nLineinfos;
            funcPro.LineInfos = new();

            funcPro.nLocalInfos = nLocalinfos;
            funcPro.LocalInfos = new();

            funcPro.nDefaultParameters = nDefparams;
            funcPro.DefaultParameters = new();
            //ExUtils.InitList(ref funcPro._defparams, n_defparams);

            return funcPro;
        }

        public ExPrototype()
        {
        }

        public ExPrototype(ExSState ss)
        {
            StackSize = 0;
            SharedState = ss;
        }

        public new string GetDebuggerDisplay()
        {
            return "FPRO(" + Name.GetString() + ", n_func: " + nFuncs + ", n_lits: " + nLits + ", n_instr: " + nInstr + ")";
        }

        public ExLineInfo FindLineInfo(int idx)
        {
            int line = LineInfos[0].Line;
            int low = 0, mid = 0, high = nLineInfos - 1;

            while (low <= high)
            {
                mid = low + ((high - low) >> 1);
                int cop = LineInfos[mid].Position;
                if (cop > idx)
                {
                    high = mid - 1;
                }
                else if (cop < idx)
                {
                    if (mid < (nLineInfos - 1) && LineInfos[mid + 1].Position >= idx)
                    {
                        break;
                    }
                    low = mid + 1;
                }
                else
                {
                    break;
                }
            }

            return LineInfos[mid];
        }

        public bool IsFunction()
        {
            return ClosureType == ExClosureType.FUNCTION;
        }
        public bool IsCluster()
        {
            return ClosureType == ExClosureType.CLUSTER;
        }
        public bool IsRule()
        {
            return ClosureType == ExClosureType.RULE;
        }
        /*public bool IsMacro()
        {
            return ClosureType == ExClosureType.MACRO;
        }*/
        public bool IsSequence()
        {
            return ClosureType == ExClosureType.SEQUENCE;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Disposer.DisposeObjects(Source, Name);

            LineInfos = null;
            DefaultParameters = null;

            Disposer.DisposeList(ref Outers);
            Disposer.DisposeList(ref LocalInfos);
            Disposer.DisposeList(ref Functions);
            Disposer.DisposeList(ref Parameters);
            Disposer.DisposeList(ref Literals);

            Instructions.RemoveAll((ExInstr i) => true);
            Instructions = null;
        }
    }

    public class ExFunc
    {
        public MethodInfo Method;
        public ExFunc()
        {
        }
        public ExFunc(MethodInfo m)
        {
            Method = m;
        }
        public int Invoke(ExVM vm, int nargs)
        {
            return (int)Method.Invoke(null, new object[] { vm, nargs });
        }
    }
}
