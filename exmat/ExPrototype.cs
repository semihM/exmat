using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using ExMat.InfoVar;
using ExMat.Objects;
using ExMat.OPs;
using ExMat.States;

namespace ExMat.FuncPrototype
{
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExPrototype : ExRefC
    {
        public ExClosureType ClosureType;   // Fonksiyon türü 

        public ExObject Name;               // Fonksiyon ismi
        public ExObject Source;             // Kaynak kod dizisi
        public ExSState SharedState;        // Ortak değerler
        public List<ExInstr> Instructions;  // Komut listesi
        public List<ExObject> Literals;     // Yazı dizileri ve isimler listesi
        public List<ExObject> Parameters;   // Parametreler
        public List<int> DefaultParameters; // Varsayılan değerler
        public List<ExPrototype> Functions; // Fonksiyon(lar)
        public List<ExLocalInfo> LocalInfos;// Fonksiyon içindeki değişken bilgileri
        public List<ExOuterInfo> Outers;    // Dışarıdaki değişkenlere referanslar
        public List<ExLineInfo> LineInfos;  // Komutların satır ve indeks bilgisi

        public int StackSize;               // Fonksiyon komutlarının ihtiyacı olan yığın boyutu
        public bool HasVargs;               // Belirsiz sayıda parametreye sahip ?

        #region Fields for storing sizes of List<T> fields 
        public int nInstr;
        public int nLits;
        public int nParams;
        public int nFuncs;
        public int nOuters;
        public int nLineInfos;
        public int nLocalInfos;
        public int nDefaultParameters;
        #endregion

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

            return funcPro;
        }

        public ExPrototype()
        {
            ClosureType = ExClosureType.DEFAULT;

            StackSize = 0;
            Functions = new();
            Instructions = new();
            Literals = new();
            Parameters = new();
            Outers = new();
            LocalInfos = new();
            LineInfos = new();
            DefaultParameters = new();
        }

        public ExPrototype(ExSState ss)
        {
            ClosureType = ExClosureType.DEFAULT;
            StackSize = 0;
            SharedState = ss;
        }

#if DEBUG
        public new string GetDebuggerDisplay()
        {
            return "FPRO(" + Name.GetString() + ", n_func: " + nFuncs + ", n_lits: " + nLits + ", n_instr: " + nInstr + ")";
        }
#endif

        public ExLineInfo FindLineInfo(int idx)
        {
            if (LineInfos.Count < 1)
            {
                return new() { Line = 1, Position = 0 };
            }

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
            return ClosureType == ExClosureType.DEFAULT;
        }
        public bool IsCluster()
        {
            return ClosureType == ExClosureType.CLUSTER;
        }
        public bool IsRule()
        {
            return ClosureType == ExClosureType.RULE;
        }
        public bool IsSequence()
        {
            return ClosureType == ExClosureType.SEQUENCE;
        }

        protected override void Dispose(bool disposing)
        {
            if (ReferenceCount > 0)
            {
                return;
            }
            base.Dispose(disposing);

            ExDisposer.DisposeObjects(Source, Name);

            LineInfos = null;
            DefaultParameters = null;

            ExDisposer.DisposeList(ref Outers);
            ExDisposer.DisposeList(ref LocalInfos);
            ExDisposer.DisposeList(ref Functions);
            ExDisposer.DisposeList(ref Parameters);
            ExDisposer.DisposeList(ref Literals);

            if (Instructions != null)
            {
                Instructions.Clear();
                Instructions = null;
            }
        }
    }
}
