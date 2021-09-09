using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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
        public ExNativeFunc.FunctionRef Function;  // C# metotu referansı
        public bool IsDelegateFunction;         // Temsili(delegate) metot?

        public int nOuters;                     // Dışardaki değişkenlere referansı sayısı
        public List<ExObject> OutersList;       // Dış değişkenlerin bellek indeks bilgisi

        public List<int> TypeMasks = new();     // Parametre maskeleri
        public int nParameterChecks;            // Parametre sayısı

        public Dictionary<int, ExObject> DefaultValues = new(); // Varsayılan değerler

        public string Documentation = string.Empty;

        public string Summary = string.Empty;

        public string Returns = string.Empty;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Summary = null;
            Returns = null;
            Documentation = null;
            WeakReference = null;
            TypeMasks = null;
            Function = null;
            SharedState = null;

            nOuters = 0;
            nParameterChecks = 0;

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
                case ExMat.HelpName:
                    {
                        return Documentation;
                    }
                case ExMat.DocsName:
                    {
                        return Summary;
                    }
                case ExMat.ReturnsName:
                    {
                        return Returns;
                    }
                case ExMat.VargsName:
                    {
                        return TypeMasks.Count == 0;
                    }
                case ExMat.DelegName:
                    {
                        return IsDelegateFunction;
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

        public static ExNativeClosure Create(ExSState exS, ExNativeFunc.FunctionRef f, int nout)
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

        public string GetParameterInfoString()
        {
            StringBuilder s = new();
            List<string> infos = new();

            if (!(bool)GetAttribute(ExMat.VargsName)
                && API.ExApi.GetTypeMaskInfos(TypeMasks, infos))
            {
                int min = (int)GetAttribute(ExMat.nMinArgs);
                int paramsleft = (int)GetAttribute(ExMat.nDefParams);
                int i = 0;

                if (!(bool)GetAttribute(ExMat.DelegName)
                    || min != paramsleft)
                {
                    infos.RemoveAt(0);
                }

                s.Append(string.Join(", ", infos.GetRange(0, min)));

                if (paramsleft > 0)
                {
                    if (s.Length > 0)
                    {
                        s.Append(", ");
                    }

                    s.Append(API.ExApi.GetNClosureDefaultParamInfo(i + min,
                                                                   paramsleft + min,
                                                                   infos,
                                                                   (Dictionary<string, ExObject>)GetAttribute(ExMat.DefParams)));
                }
            }

            if (s.Length == 0)
            {
                return $"{(IsDelegateFunction ? "delegate, " : string.Empty)}params: 0, minargs: 0";
            }

            return s.ToString();
        }

        public string GetInfoString()
        {
            string s = (string)GetAttribute(ExMat.FuncName) + ", ";

            if ((bool)GetAttribute(ExMat.VargsName))
            {
                s += "vargs";
                s += ", params: " + (int)GetAttribute(ExMat.nParams);
                s += ", minargs: " + (int)GetAttribute(ExMat.nMinArgs);
            }
            else
            {
                s += GetParameterInfoString();
            }

            return s;
        }

        public new string GetDebuggerDisplay()
        {
            return "NATIVECLOSURE(" + Name.GetString() + ")";
        }
    }
}
