using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using ExMat.Objects;
using ExMat.States;
using ExMat.VM;

namespace ExMat.ExClass
{
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExInstance : ExRefC
    {
        public ExObject Delegate;
        public ExSState SharedState;
        public ExClass Class;
        public List<ExObject> MemberValues;
        public readonly ulong Hash;

        public ExInstance()
        {
            MemberValues = new();
            System.DateTime dt = System.DateTime.Now;
            Hash = (((ulong)(int)dt.Kind) << 62) | ((ulong)dt.Ticks);
        }

        public static new ExObjType GetType()
        {
            return ExObjType.INSTANCE;
        }

        public bool GetMetaM(ExVM vm, ExMetaMethod m, ref ExObject res)
        {
            if (Delegate != null)
            {
                if (Delegate.Type == ExObjType.DICT)
                {
                    string k = vm.SharedState.MetaMethods[(int)m].GetString();
                    if (Delegate.GetDict().ContainsKey(k))
                    {
                        res.Assign(Delegate.GetDict()[k]);
                        return true;
                    }
                }
                else if (Delegate.Type == ExObjType.ARRAY)
                {
                    int k = vm.SharedState.GetMetaIdx(vm.SharedState.MetaMethods[(int)m].GetString());
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

        public void Init(ExSState exs)
        {
            Delegate = new ExObject(Class.MetaFuncs);
            Class.ReferenceCount++;
        }

        public static ExInstance Create(ExSState exS, ExInstance inst)
        {
            ExInstance ins = new() { SharedState = exS, Class = inst.Class };
            for (int i = 0; i < inst.Class.DefaultValues.Count; i++)
            {
                ins.MemberValues.Add(new ExObject(inst.Class.DefaultValues[i].Value));
            }
            ins.Init(exS);
            return ins;
        }

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

        public bool GetMeta(int midx, ref ExObject res)
        {
            if (ExTypeCheck.IsNotNull(Class.MetaFuncs[midx]))
            {
                res = Class.MetaFuncs[midx];
                return true;
            }
            return false;
        }

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

        public virtual void Release()
        {
            if ((--ReferenceCount) == 0)
            {
                if (Class != null)
                {
                    Class.Release();
                }
                MemberValues.RemoveAll((ExObject e) => true); MemberValues = null;
            }
        }

#if DEBUG
        public new string GetDebuggerDisplay()
        {
            return "INSTANCE(n_vals: " + MemberValues.Count + ")";
        }
#endif

        protected override void Dispose(bool disposing)
        {
            if (ReferenceCount > 0)
            {
                return;
            }
            base.Dispose(disposing);

            ExDisposer.DisposeObjects(Class);
            ExDisposer.DisposeList(ref MemberValues);
        }
    }
}
