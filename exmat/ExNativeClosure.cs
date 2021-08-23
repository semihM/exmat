using System.Collections.Generic;
using System.Diagnostics;
using ExMat.Interfaces;
using ExMat.Objects;
using ExMat.States;
using ExMat.Utils;

namespace ExMat.Closure
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExNativeClosure : ExRefC, IExClosureAttr
    {
        public ExSState SharedState;            // Ortak değerler
        public ExObject Name;                   // Fonksiyon ismi
        public ExRegFunc.FunctionRef Function;  // C# metotu referansı
        public bool IsDelegateFunction;         // Temsili(delegate) metot?

        public int nOuters;                     // Dışardaki değişkenlere referansı sayısı
        public List<ExObject> OutersList;       // Dış değişkenlerin bellek indeks bilgisi

        public List<int> TypeMasks = new();     // Parametre maskeleri
        public int nParameterChecks;            // Parametre sayısı

        public Dictionary<int, ExObject> DefaultValues = new(); // Varsayılan değerler

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            WeakReference = null;
            TypeMasks = null;
            nOuters = 0;
            nParameterChecks = 0;
            Function = null;

            Disposer.DisposeObjects(Name);
            Disposer.DisposeList(ref OutersList);
            Disposer.DisposeDict(ref DefaultValues);
        }

        public dynamic GetAttribute(string attr)
        {
            switch (attr)
            {
                case ExMat.FuncName:
                    {
                        return Name.GetString();
                    }
                case ExMat.VargsName:
                    {
                        return TypeMasks.Count == 0;
                    }
                case ExMat.nParams:
                    {
                        if (nParameterChecks < 0)
                        {
                            return TypeMasks.Count == 0 ? (-nParameterChecks - 1) : TypeMasks.Count - 1;
                        }
                        else if (nParameterChecks > 0)
                        {
                            return nParameterChecks - 1;
                        }
                        else
                        {
                            return TypeMasks.Count - 1;
                        }
                    }
                case ExMat.nDefParams:
                    {
                        return DefaultValues.Count;
                    }
                case ExMat.nMinArgs:
                    {
                        return nParameterChecks < 0 ? (-nParameterChecks - 1) : (nParameterChecks - 1);
                    }
                case ExMat.DefParams:
                    {
                        Dictionary<string, ExObject> dict = new();
                        foreach (KeyValuePair<int, ExObject> pair in DefaultValues)
                        {
                            dict.Add(pair.Key.ToString(), new(pair.Value));
                        }
                        return dict;
                    }
                default:
                    {
                        return null;
                    }
            }
        }

        public ExNativeClosure()
        {
            ReferenceCount = 1;
        }

        public static ExNativeClosure Create(ExSState exS, ExRegFunc.FunctionRef f, int nout)
        {
            ExNativeClosure cls = new() { SharedState = exS, Function = f };
            ExUtils.InitList(ref cls.OutersList, nout);
            cls.nOuters = nout;
            return cls;
        }

        public virtual void Release()
        {
            foreach (ExObject o in OutersList)
            {
                o.Nullify();
            }
            OutersList = null;
        }
        public static new ExObjType GetType()
        {
            return ExObjType.NATIVECLOSURE;
        }

        public new string GetDebuggerDisplay()
        {
            return "NATIVECLOSURE(" + Name.GetString() + ")";
        }
    }
}
