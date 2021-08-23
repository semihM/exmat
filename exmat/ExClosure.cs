using System.Collections.Generic;
using System.Diagnostics;
using ExMat.Class;
using ExMat.FuncPrototype;
using ExMat.Interfaces;
using ExMat.Objects;
using ExMat.States;
using ExMat.Utils;

namespace ExMat.Closure
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExClosure : ExRefC, IExClosureAttr
    {
        public ExClass Base;                // Ait olunan sınıf(varsa)
        public ExPrototype Function;        // Fonksiyon prototipi
        public List<ExObject> OutersList;   // Dış değişken bilgisi
        public List<ExObject> DefaultParams;// Varsayılan parametre değerleri
        public ExSState SharedState;        // Ortak değerler

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Base = null;

            WeakReference = null;
            Disposer.DisposeObjects(Function);
            Disposer.DisposeList(ref OutersList);
            Disposer.DisposeList(ref DefaultParams);
        }

        public static ExClosure Create(ExSState exS, ExPrototype fpro)
        {
            ExClosure cls = new() { SharedState = exS, Function = fpro };
            ExUtils.InitList(ref cls.OutersList, fpro.nOuters);
            ExUtils.InitList(ref cls.DefaultParams, fpro.nDefaultParameters);
            return cls;
        }

        public ExClosure()
        {
            ReferenceCount = 1;
        }

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
                            dict.Add((++start).ToString(), d);
                        }
                        return dict;
                    }
                default:
                    {
                        if (Base != null)
                        {
                            string mem = Function.Name.GetString();
                            int memid = Base.Members[mem].GetMemberID();

                            if (Base.Methods[memid].Attributes.GetDict().ContainsKey(attr))
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

        public new string GetDebuggerDisplay()
        {
            if (Function.Name == null || Function.Name.Type == ExObjType.NULL)
            {
                return "CLOSURE";
            }
            if (Base != null)
            {
                return "[HAS BASE] CLOSURE(" + Function.Name.GetString() + ")";
            }
            return "CLOSURE(" + Function.Name.GetString() + ")";
        }


    }

}
