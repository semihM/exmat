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
    /// <summary>
    /// Closure prototype
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExPrototype : ExRefC
    {
        /// <summary>
        /// Type of closure
        /// </summary>
        public ExClosureType ClosureType;   // Fonksiyon türü 

        /// <summary>
        /// Name of the prototype
        /// </summary>
        public ExObject Name;               // Fonksiyon ismi
        /// <summary>
        /// Source code
        /// </summary>
        public ExObject Source;             // Kaynak kod dizisi
        /// <summary>
        /// Shared state
        /// </summary>
        public ExSState SharedState;        // Ortak değerler
        /// <summary>
        /// Instructions
        /// </summary>
        internal List<ExInstr> Instructions;  // Komut listesi
        /// <summary>
        /// Literals
        /// </summary>
        public List<ExObject> Literals;     // Yazı dizileri ve isimler listesi
        /// <summary>
        /// Parameters
        /// </summary>
        public List<ExObject> Parameters;   // Parametreler
        /// <summary>
        /// Default values' indices of parameters
        /// </summary>
        public List<int> DefaultParameters; // Varsayılan değerler
        /// <summary>
        /// List of prototypes in a chain
        /// </summary>
        public List<ExPrototype> Functions; // Fonksiyon(lar)
        /// <summary>
        /// Local variable information
        /// </summary>
        internal List<ExLocalInfo> LocalInfos;// Fonksiyon içindeki değişken bilgileri
        internal List<ExOuterInfo> Outers;    // Dışarıdaki değişkenlere referanslar
        /// <summary>
        /// For tracking
        /// </summary>
        internal List<ExLineInfo> LineInfos;  // Komutların satır ve indeks bilgisi

        internal int StackSize;               // Fonksiyon komutlarının ihtiyacı olan yığın boyutu

        /// <summary>
        /// Does the closure have vargs enabled ?
        /// </summary>
        public bool HasVargs;               // Belirsiz sayıda parametreye sahip ?

        #region Fields for storing sizes of List<T> fields 
        /// <summary>
        /// Number of instructions
        /// </summary>
        public int nInstr;
        /// <summary>
        /// Number of literals
        /// </summary>
        public int nLits;
        /// <summary>
        /// Number of parameters
        /// </summary>
        public int nParams;
        /// <summary>
        /// Number of functions
        /// </summary>
        public int nFuncs;
        /// <summary>
        /// Number of outers
        /// </summary>
        public int nOuters;
        /// <summary>
        /// Number of line infos
        /// </summary>
        public int nLineInfos;
        /// <summary>
        /// Number of local var infos
        /// </summary>
        public int nLocalInfos;
        /// <summary>
        /// Number of default values
        /// </summary>
        public int nDefaultParameters;
        #endregion

        internal static ExPrototype Create(ExSState sState,
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

        /// <summary>
        /// Initializer
        /// </summary>
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

        private ExPrototype(ExSState ss)
        {
            ClosureType = ExClosureType.DEFAULT;
            StackSize = 0;
            SharedState = ss;
        }

#if DEBUG
        internal new string GetDebuggerDisplay()
        {
            return "FPRO(" + Name.GetString() + ", n_func: " + nFuncs + ", n_lits: " + nLits + ", n_instr: " + nInstr + ")";
        }
#endif

        internal ExLineInfo FindLineInfo(int idx)
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

        /// <summary>
        /// Returns <see cref="ClosureType"/> == <see cref="ExClosureType.DEFAULT"/>
        /// </summary>
        /// <returns></returns>
        public bool IsFunction()
        {
            return ClosureType == ExClosureType.DEFAULT;
        }
        /// <summary>
        /// Returns <see cref="ClosureType"/> == <see cref="ExClosureType.CLUSTER"/>
        /// </summary>
        /// <returns></returns>
        public bool IsCluster()
        {
            return ClosureType == ExClosureType.CLUSTER;
        }
        /// <summary>
        /// Returns <see cref="ClosureType"/> == <see cref="ExClosureType.RULE"/>
        /// </summary>
        /// <returns></returns>
        public bool IsRule()
        {
            return ClosureType == ExClosureType.RULE;
        }
        /// <summary>
        /// Returns <see cref="ClosureType"/> == <see cref="ExClosureType.SEQUENCE"/>
        /// </summary>
        /// <returns></returns>
        public bool IsSequence()
        {
            return ClosureType == ExClosureType.SEQUENCE;
        }

        internal override void Dispose(bool disposing)
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
