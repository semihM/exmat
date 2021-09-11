using System.Collections.Generic;
using System.Diagnostics;
using ExMat.InfoVar;
using ExMat.Objects;
using ExMat.OPs;
using ExMat.States;

namespace ExMat.FuncPrototype
{
    public enum ExClosureType
    {
        FUNCTION,   // Varsayılan fonksiyon türü
        RULE,       // Kural, her zaman boolean dönen tür
        CLUSTER,    // Küme, tanım kümesindeki bir değerin görüntü kümesi karşılığını dönen tür 
        SEQUENCE    // Dizi, optimize edilmiş tekrarlı fonksiyon türü
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
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
            ClosureType = ExClosureType.FUNCTION;

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
            ClosureType = ExClosureType.FUNCTION;
            StackSize = 0;
            SharedState = ss;
        }

        public new string GetDebuggerDisplay()
        {
            return "FPRO(" + Name.GetString() + ", n_func: " + nFuncs + ", n_lits: " + nLits + ", n_instr: " + nInstr + ")";
        }

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
        public bool IsSequence()
        {
            return ClosureType == ExClosureType.SEQUENCE;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            ExDisposer.DisposeObjects(Source, Name);

            LineInfos = null;
            DefaultParameters = null;

            ExDisposer.DisposeList(ref Outers);
            ExDisposer.DisposeList(ref LocalInfos);
            ExDisposer.DisposeList(ref Functions);
            ExDisposer.DisposeList(ref Parameters);
            ExDisposer.DisposeList(ref Literals);

            Instructions.RemoveAll((ExInstr i) => true);
            Instructions = null;
        }
    }
}
