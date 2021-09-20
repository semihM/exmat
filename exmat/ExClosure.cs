using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.Globalization;
using ExMat.FuncPrototype;
using ExMat.Interfaces;
using ExMat.Objects;
using ExMat.States;
using ExMat.Utils;

namespace ExMat.Closure
{
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExClosure : ExRefC, IExClosure
    {
        public ExClass.ExClass Base;                // Ait olunan sınıf(varsa)
        public ExPrototype Function;        // Fonksiyon prototipi
        public List<ExObject> OutersList;   // Dış değişken bilgisi
        public List<ExObject> DefaultParams;// Varsayılan parametre değerleri
        public ExSState SharedState;        // Ortak değerler

        protected override void Dispose(bool disposing)
        {
            if (ReferenceCount > 0)
            {
                return;
            }
            base.Dispose(disposing);
            Base = null;

            WeakReference = null;
            ExDisposer.DisposeObjects(Function);
            ExDisposer.DisposeList(ref OutersList);
            ExDisposer.DisposeList(ref DefaultParams);
            SharedState = null;
        }

        public static ExClosure Create(ExSState exS, ExPrototype fpro)
        {
            ExClosure cls = new() { SharedState = exS, Function = fpro };
            ExUtils.InitList(ref cls.OutersList, fpro.nOuters);
            ExUtils.InitList(ref cls.DefaultParams, fpro.nDefaultParameters);
            return cls;
        }

        public ExClosure() { }

        public ExClosure Copy()
        {
            ExPrototype fp = Function;
            ExClosure res = Create(SharedState, fp);

            res.WeakReference = WeakReference;
            if (res.WeakReference != null)
            {
                res.WeakReference.ReferenceCount++;
            }

            for (int i = 0; i < fp.nOuters; i++)
            {
                res.OutersList[i].Assign(OutersList[i]);
            }
            for (int i = 0; i < fp.nDefaultParameters; i++)
            {
                res.DefaultParams[i].Assign(DefaultParams[i]);
            }
            return res;
        }

        public dynamic GetAttribute(string attr)
        {
            switch (attr)
            {
                case ExMat.FuncName:
                    {
                        return Function.Name.GetString();
                    }
                case ExMat.VargsName:
                    {
                        return Function.HasVargs;
                    }
                case ExMat.DelegName:
                    {
                        return Base != null;
                    }
                case ExMat.nParams:
                    {
                        return Function.nParams - 1 - (Function.HasVargs ? 1 : 0);
                    }
                case ExMat.nDefParams:
                    {
                        return Function.nDefaultParameters;
                    }
                case ExMat.nMinArgs:
                    {
                        return Function.nParams - 1 - Function.nDefaultParameters - (Function.HasVargs ? 1 : 0);
                    }
                case ExMat.DefParams:
                    {
                        int ndef = Function.nDefaultParameters;
                        int npar = Function.nParams - 1;
                        int start = npar - ndef;
                        Dictionary<string, ExObject> dict = new();
                        foreach (ExObject d in DefaultParams)
                        {
                            dict.Add((++start).ToString(CultureInfo.CurrentCulture), d);
                        }
                        return dict;
                    }
                default:
                    {
                        if (Base != null)
                        {
                            string mem = Function.Name.GetString();
                            int memid = Base.Members[mem].GetMemberID();

                            if (ExTypeCheck.IsNotNull(Base.Methods[memid].Attributes)
                                && Base.Methods[memid].Attributes.GetDict().ContainsKey(attr))
                            {
                                return Base.Methods[memid].Attributes.GetDict()[attr];
                            }
                        }

                        return null;
                    }
            }
        }
        public virtual void Release()
        {
            foreach (ExObject o in OutersList)
            {
                o.Nullify();
            }
            OutersList = null;

            foreach (ExObject o in DefaultParams)
            {
                o.Nullify();
            }
            DefaultParams = null;
        }
        public static new ExObjType GetType()
        {
            return ExObjType.CLOSURE;
        }

        public string GetInfoString()
        {
            string s = string.Empty;
            switch (Function.ClosureType)
            {
                case ExClosureType.DEFAULT:
                    {
                        string name = Function.Name.GetString();
                        s = string.IsNullOrWhiteSpace(name) ? "LAMBDA(" : "FUNCTION(" + name + ", ";

                        if (Function.nDefaultParameters > 0)
                        {
                            s += Function.nParams - 1 + " params (min:" + (Function.nParams - Function.nDefaultParameters - 1) + "))";
                        }
                        else if (Function.HasVargs)
                        {
                            s += "vargs, min:" + (Function.nParams - 2) + " params)";
                        }
                        else
                        {
                            s += Function.nParams - 1 + " params)";
                        }
                        break;
                    }
                case ExClosureType.RULE:
                    {
                        s = "RULE(" + Function.Name.GetString() + ", ";
                        s += Function.nParams - 1 + " params)";
                        break;
                    }
                case ExClosureType.CLUSTER:
                    {
                        s = "CLUSTER(" + Function.Name.GetString() + ", ";
                        s += Function.nParams - 1 + " params)";
                        break;
                    }
                case ExClosureType.SEQUENCE:
                    {
                        s = "SEQUENCE(" + Function.Name.GetString() + ", 1 params)";
                        break;
                    }
            }
            return s;
        }

#if DEBUG
        public new string GetDebuggerDisplay()
        {
            return Function.Name == null || ExTypeCheck.IsNull(Function.Name)
                ? "CLOSURE"
                : Base != null ? "[HAS BASE] CLOSURE(" + Function.Name.GetString() + ")" : "CLOSURE(" + Function.Name.GetString() + ")";
        }
#endif

    }

}
