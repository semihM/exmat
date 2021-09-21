using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using ExMat.Objects;
using ExMat.States;

namespace ExMat.ExClass
{
    /// <summary>
    /// Instance object model
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public sealed class ExInstance : ExRefC
    {
        /// <summary>
        /// Meta methods and other delegates dictionary
        /// </summary>
        public ExObject Delegate;
        /// <summary>
        /// Shared state
        /// </summary>
        public ExSState SharedState;
        /// <summary>
        /// Class instanced from
        /// </summary>
        public ExClass Class;
        /// <summary>
        /// Members list
        /// </summary>
        public List<ExObject> MemberValues;
        /// <summary>
        /// Unique hash value
        /// </summary>
        public readonly ulong Hash;

        /// <summary>
        /// Empty constructor
        /// </summary>
        public ExInstance()
        {
            MemberValues = new();
            System.DateTime dt = System.DateTime.Now;
            Hash = (((ulong)(int)dt.Kind) << 62) | ((ulong)dt.Ticks);
        }

        /// <summary>
        /// Get a meta method from <see cref="Delegate"/> dictionary
        /// </summary>
        /// <param name="m">Neta method</param>
        /// <param name="res">Result closure object</param>
        /// <returns>Wheter meta method was defined and found</returns>
        public bool GetMetaM(ExMetaMethod m, ref ExObject res)
        {
            if (Delegate != null)
            {
                if (Delegate.Type == ExObjType.DICT)
                {
                    string k = SharedState.MetaMethods[(int)m].GetString();
                    if (Delegate.GetDict().ContainsKey(k))
                    {
                        res.Assign(Delegate.GetDict()[k]);
                        return true;
                    }
                }
                else if (Delegate.Type == ExObjType.ARRAY)
                {
                    int k = SharedState.GetMetaIdx(SharedState.MetaMethods[(int)m].GetString());
                    if (ExTypeCheck.IsNotNull(Delegate.GetList()[k]))
                    {
                        res.Assign(Delegate.GetList()[k]);
                        return true;
                    }
                }
                return false;
            }
            return false;
        }

        /// <summary>
        /// Initialize <see cref="Delegate"/> and increment <see cref="Class"/> reference count
        /// </summary>
        /// <param name="exs">Shared state</param>
        public void Init(ExSState exs)
        {
            SharedState = exs;
            Delegate = new ExObject(Class.MetaFuncs);
            Class.ReferenceCount++;
        }

        /// <summary>
        /// Create and initialize instance from given class
        /// </summary>
        /// <param name="exS">Shared state</param>
        /// <param name="cls">Class to instance from</param>
        /// <returns>Created instance</returns>
        public static ExInstance Create(ExSState exS, ExClass cls)
        {
            ExInstance ins = new() { SharedState = exS, Class = cls };
            for (int i = 0; i < cls.DefaultValues.Count; i++)
            {
                ins.MemberValues.Add(new ExObject(cls.DefaultValues[i].Value));
            }
            ins.Init(exS);
            return ins;
        }

        /// <summary>
        /// Check if the instance is an instance of given class
        /// </summary>
        /// <param name="cls">Class to check</param>
        /// <returns>Wheter the instance was instanced from <paramref name="cls"/> class</returns>
        public bool IsInstanceOf(ExClass cls)
        {
            ExClass p = Class;
            while (p != null)
            {
                if (p == cls)
                {
                    return true;
                }
                p = p.Base;
            }
            return false;
        }

#if DEBUG
        private new string GetDebuggerDisplay()
        {
            return "INSTANCE(n_vals: " + MemberValues.Count + ")";
        }
#endif
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

            Class.Release();

            ExDisposer.DisposeList(ref MemberValues);
        }
    }
}
