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
    /// <summary>
    /// Closure model, can return values and can be called
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public sealed class ExClosure : ExRefC, IExClosure
    {
        /// <summary>
        /// Base object this closure belongs to or null
        /// </summary>
        public ExClass.ExClass Base;                // Ait olunan sınıf(varsa)
        /// <summary>
        /// Function prototype
        /// </summary>
        public ExPrototype Function;        // Fonksiyon prototipi
        internal List<ExObject> OutersList;   // Dış değişken bilgisi
        internal List<ExObject> DefaultParams;// Varsayılan parametre değerleri
        /// <summary>
        /// Shared state
        /// </summary>
        public ExSState SharedState;        // Ortak değerler

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
            Base = null;

            WeakReference = null;
            ExDisposer.DisposeObjects(Function);
            ExDisposer.DisposeList(ref OutersList);
            ExDisposer.DisposeList(ref DefaultParams);
            SharedState = null;
        }

        /// <summary>
        /// Initialize a closure using given shared state and the prototype
        /// </summary>
        /// <param name="exS">Shared state</param>
        /// <param name="fpro">Prototype</param>
        /// <returns>A new closure</returns>
        public static ExClosure Create(ExSState exS, ExPrototype fpro)
        {
            ExClosure cls = new() { SharedState = exS, Function = fpro };
            ExUtils.InitList(ref cls.OutersList, fpro.Info.nOuters);
            ExUtils.InitList(ref cls.DefaultParams, fpro.Info.nDefaultParameters);
            return cls;
        }

        /// <summary>
        /// Empty constructor
        /// </summary>
        public ExClosure() { }

        /// <summary>
        /// Get an attribute of the closure
        /// <para>Built in names:</para>
        /// <para><see cref="ExMat.FuncName"/></para>
        /// <para><see cref="ExMat.VargsName"/></para>
        /// <para><see cref="ExMat.DelegName"/></para>
        /// <para><see cref="ExMat.nParams"/></para>
        /// <para><see cref="ExMat.nDefParams"/></para>
        /// <para><see cref="ExMat.nMinArgs"/></para>
        /// <para><see cref="ExMat.DefParams"/></para>
        /// <para>Or checks <see cref="Base"/> class's attribute if nothing has matches</para>
        /// </summary>
        /// <param name="attr">Attribute name</param>
        /// <returns>Depends on the attribute requested</returns>
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
                        return Function.Info.nParams - 1 - (Function.HasVargs ? 1 : 0);
                    }
                case ExMat.nDefParams:
                    {
                        return Function.Info.nDefaultParameters;
                    }
                case ExMat.nMinArgs:
                    {
                        return Function.Info.nParams - 1 - Function.Info.nDefaultParameters - (Function.HasVargs ? 1 : 0);
                    }
                case ExMat.DefParams:
                    {
                        int ndef = Function.Info.nDefaultParameters;
                        int npar = Function.Info.nParams - 1;
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

        internal string GetInfoString()
        {
            string s = string.Empty;
            switch (Function.ClosureType)
            {
                case ExClosureType.DEFAULT:
                    {
                        string name = Function.Name.GetString();
                        s = string.IsNullOrWhiteSpace(name) ? "LAMBDA(" : "FUNCTION(" + name + ", ";

                        if (Function.Info.nDefaultParameters > 0)
                        {
                            s += Function.Info.nParams - 1 + " params (min:" + (Function.Info.nParams - Function.Info.nDefaultParameters - 1) + "))";
                        }
                        else if (Function.HasVargs)
                        {
                            s += "vargs, min:" + (Function.Info.nParams - 2) + " params)";
                        }
                        else
                        {
                            s += Function.Info.nParams - 1 + " params)";
                        }
                        break;
                    }
                case ExClosureType.RULE:
                    {
                        s = "RULE(" + Function.Name.GetString() + ", ";
                        s += Function.Info.nParams - 1 + " params)";
                        break;
                    }
                case ExClosureType.CLUSTER:
                    {
                        s = "CLUSTER(" + Function.Name.GetString() + ", ";
                        s += Function.Info.nParams - 1 + " params)";
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
        private new string GetDebuggerDisplay()
        {
            return Function.Name == null || ExTypeCheck.IsNull(Function.Name)
                ? "CLOSURE"
                : Base != null ? "[HAS BASE] CLOSURE(" + Function.Name.GetString() + ")" : "CLOSURE(" + Function.Name.GetString() + ")";
        }
#endif

    }

}
