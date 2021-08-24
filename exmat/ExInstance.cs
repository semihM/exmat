using System.Collections.Generic;
using System.Diagnostics;
using ExMat.Objects;
using ExMat.States;
using ExMat.VM;

namespace ExMat.Class
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExInstance : ExRefC
    {
        public ExObject Delegate;
        public ExSState SharedState;
        public ExClass Class;
        public List<ExObject> MemberValues;

        public ExInstance()
        {
            MemberValues = new();
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
                    if (Delegate.GetList()[k].Type != ExObjType.NULL)
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
            Delegate = new ExObject(Class.MetaFuncs); // TO-DO keep both metas and members
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
            if (Class.MetaFuncs[midx].Type != ExObjType.NULL)
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

        public new string GetDebuggerDisplay()
        {
            return "INSTANCE(n_vals: " + MemberValues.Count + ")";
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Disposer.DisposeObjects(Class);
            Disposer.DisposeList(ref MemberValues);
        }
    }
}
