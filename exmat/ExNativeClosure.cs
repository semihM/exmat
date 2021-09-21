using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.Globalization;
using System.Text;
using ExMat.Interfaces;
using ExMat.Objects;
using ExMat.States;
using ExMat.Utils;

namespace ExMat.Closure
{
    /// <summary>
    /// Native closure model
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExNativeClosure : ExRefC, IExClosure
    {
        /// <summary>
        /// Shared state
        /// </summary>
        public ExSState SharedState;            // Ortak değerler
        /// <summary>
        /// Name of the closure
        /// </summary>
        public ExObject Name;                   // Fonksiyon ismi
        /// <summary>
        /// Function to use for calls
        /// </summary>
        public ExMat.StdLibFunction Function;      // C# metotu referansı
        /// <summary>
        /// Wheter this is a delegate function closure
        /// </summary>
        public bool IsDelegateFunction;         // Temsili(delegate) metot?

        /// <summary>
        /// Outer value count
        /// </summary>
        public int nOuters;                     // Dışardaki değişkenlere referansı sayısı

        /// <summary>
        /// Outers
        /// </summary>
        public List<ExObject> OutersList;       // Dış değişkenlerin bellek indeks bilgisi

        /// <summary>
        /// Type masks for each parameter. If <see langword="null"/>, vargs is enabled
        /// </summary>
        public List<int> TypeMasks = new();     // Parametre maskeleri
        /// <summary>
        /// Parameter check value. 
        /// <para>Works together with <see cref="TypeMasks"/></para>
        /// <para>Positive 'n': n - 1 parameters == n - 1 arguments</para>
        /// <para>Negative 'n': -n parameters == -n - 1 arguments minimum</para>
        /// </summary>
        public int nParameterChecks;            // Parametre sayısı

        /// <summary>
        /// Default parameter values with their parameter numbers
        /// </summary>
        public Dictionary<int, ExObject> DefaultValues = new(); // Varsayılan değerler

        /// <summary>
        /// Full documentation
        /// </summary>
        public string Documentation = string.Empty;

        /// <summary>
        /// Short description
        /// </summary>
        public string Summary = string.Empty;

        /// <summary>
        /// Return type
        /// </summary>
        public string Returns = string.Empty;

        /// <summary>
        /// Base object type
        /// </summary>
        public string Base = string.Empty;

        internal override void Dispose(bool disposing)
        {
            if (ReferenceCount > 0)
            {
                return;
            }

            base.Dispose(disposing);

            Summary = null;
            Returns = null;
            Documentation = null;
            WeakReference = null;
            TypeMasks = null;
            Function = null;
            SharedState = null;

            ExDisposer.DisposeObjects(Name);
            ExDisposer.DisposeList(ref OutersList);
            ExDisposer.DisposeDict(ref DefaultValues);
        }

        /// <summary>
        /// Get an attribute of the closure
        /// <para>Built in names:</para>
        /// <para><see cref="ExMat.FuncName"/></para>
        /// <para><see cref="ExMat.HelpName"/></para>
        /// <para><see cref="ExMat.DocsName"/></para>
        /// <para><see cref="ExMat.VargsName"/></para>
        /// <para><see cref="ExMat.DelegName"/></para>
        /// <para><see cref="ExMat.nParams"/></para>
        /// <para><see cref="ExMat.nDefParams"/></para>
        /// <para><see cref="ExMat.nMinArgs"/></para>
        /// <para><see cref="ExMat.DefParams"/></para>
        /// <para>Or returns null</para>
        /// </summary>
        /// <param name="attr">Attribute name</param>
        /// <returns>Depends on the attribute requested</returns>
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
                        return nParameterChecks < 0
                            ? (dynamic)(TypeMasks.Count == 0 ? (-nParameterChecks - 1) : TypeMasks.Count - 1)
                            : nParameterChecks > 0 ? (dynamic)(nParameterChecks - 1) : (dynamic)(TypeMasks.Count - 1);
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
                            dict.Add(pair.Key.ToString(CultureInfo.CurrentCulture), new(pair.Value));
                        }
                        return dict;
                    }
                default:
                    {
                        return null;
                    }
            }
        }
        /// <summary>
        /// Empty constructor
        /// </summary>
        public ExNativeClosure() { }

        /// <summary>
        /// Create a new native closure with given std lib function
        /// </summary>
        /// <param name="exS">Shared state</param>
        /// <param name="f">Std lib method</param>
        /// <param name="nout">Number of outer vals</param>
        /// <returns>Created native closure</returns>
        public static ExNativeClosure Create(ExSState exS, ExMat.StdLibFunction f, int nout)
        {
            ExNativeClosure cls = new() { SharedState = exS, Function = f };
            ExUtils.InitList(ref cls.OutersList, nout);
            cls.nOuters = nout;
            return cls;
        }

        private string GetParameterInfoString()
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

            return s.Length == 0 ? $"{(IsDelegateFunction ? "delegate, " : string.Empty)}params: 0, minargs: 0" : s.ToString();
        }

        internal string GetInfoString()
        {
            string s = string.Empty;

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

#if DEBUG
        internal new string GetDebuggerDisplay()
        {
            return "NATIVECLOSURE(" + Name.GetString() + ")";
        }
#endif
    }
}
