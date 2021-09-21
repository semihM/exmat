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
    public sealed class ExPrototype : ExRefC
    {
        /// <summary>
        /// Type of closure
        /// </summary>
        public ExClosureType ClosureType;   // Fonksiyon türü 

        /// <summary>
        /// Name of the prototype
        /// </summary>
        public ExObject Name = new();               // Fonksiyon ismi
        /// <summary>
        /// Source code
        /// </summary>
        public ExObject Source = new();             // Kaynak kod dizisi
        /// <summary>
        /// Shared state
        /// </summary>
        public ExSState SharedState = new();        // Ortak değerler
        /// <summary>
        /// Instructions
        /// </summary>
        internal List<ExInstr> Instructions = new();  // Komut listesi
        /// <summary>
        /// Literals
        /// </summary>
        public List<ExObject> Literals = new();     // Yazı dizileri ve isimler listesi
        /// <summary>
        /// Parameters
        /// </summary>
        public List<ExObject> Parameters = new();   // Parametreler
        /// <summary>
        /// Default values' indices of parameters
        /// </summary>
        public List<int> DefaultParameters = new(); // Varsayılan değerler
        /// <summary>
        /// List of prototypes in a chain
        /// </summary>
        public List<ExPrototype> Functions = new(); // Fonksiyon(lar)
        /// <summary>
        /// Local variable information
        /// </summary>
        internal List<ExLocalInfo> LocalInfos = new();// Fonksiyon içindeki değişken bilgileri
        internal List<ExOuterInfo> Outers = new();    // Dışarıdaki değişkenlere referanslar
        /// <summary>
        /// For tracking
        /// </summary>
        internal List<ExLineInfo> LineInfos = new();  // Komutların satır ve indeks bilgisi

        /// <summary>
        /// Count information for fields
        /// </summary>
        public ExPrototypeInfo Info;

        /// <summary>
        /// Stack size for the function callstack
        /// </summary>
        internal int StackSize;               // Fonksiyon komutlarının ihtiyacı olan yığın boyutu

        /// <summary>
        /// Does the closure have vargs enabled ?
        /// </summary>
        public bool HasVargs;               // Belirsiz sayıda parametreye sahip ?


        internal static ExPrototype Create(ExSState sState, ExPrototypeInfo info)
        {
            ExPrototype funcPro = new(sState);
            funcPro.Info = info;
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
            return "FPRO(" + Name.GetString() + ", n_func: " + Info.nFuncs + ", n_lits: " + Info.nLits + ", n_instr: " + Info.nInstr + ")";
        }
#endif

        internal ExLineInfo FindLineInfo(int idx)
        {
            if (LineInfos.Count < 1)
            {
                return new() { Line = 1, Position = 0 };
            }

            int low = 0, mid = 0, high = Info.nLineInfos - 1;

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
                    if (mid < (Info.nLineInfos - 1) && LineInfos[mid + 1].Position >= idx)
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

        /// <summary>
        /// Disposer
        /// </summary>
        /// <param name="disposing"></param>
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
