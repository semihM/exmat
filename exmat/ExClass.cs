using System;
using System.Collections.Generic;
using System.Diagnostics;
using ExMat.Objects;
using ExMat.States;
using ExMat.Utils;
using ExMat.VM;

namespace ExMat.Class
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExClassMem : IDisposable
    {
        public ExObject Value = new();
        public ExObject Attributes = new();
        private bool disposedValue;

        public ExClassMem() { }
        public ExClassMem(ExClassMem mem)
        {
            Value = new(mem.Value);
            Attributes = new(mem.Attributes);
        }

        public void Nullify()
        {
            Value.Nullify();
            Attributes.Nullify();
        }

        public string GetDebuggerDisplay()
        {
            return "CMEM(" + Value.GetDebuggerDisplay() + ")";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disposer.DisposeObjects(Value, Attributes);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExClassMem()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExClass : ExRefC
    {
        public ExClass Base;    // TO-DO
        public Dictionary<string, ExObject> Members = new();
        public List<ExObject> MetaFuncs = new();
        public List<ExClassMem> DefaultValues = new();
        public List<ExClassMem> Methods = new();
        public ExObject Attributes = new();

        public bool GotInstanced;
        public int ConstructorID;
        public ExSState SharedState;

        public readonly int LengthReprestation;

        public ExClass()
        {
            ExUtils.InitList(ref MetaFuncs, (int)ExMetaM.LAST);
        }
        public ExClass(ExSState exS, ExClass b)
        {
            SharedState = exS;
            Base = b;

            GotInstanced = false;
            ConstructorID = -1;
            LengthReprestation = 0;
            ExUtils.InitList(ref MetaFuncs, (int)ExMetaM.LAST);

            if (b != null)
            {
                ConstructorID = b.ConstructorID;
                LengthReprestation = b.LengthReprestation;
                DefaultValues = new(b.DefaultValues.Count);
                for (int i = 0; i < b.DefaultValues.Count; i++)
                {
                    DefaultValues.Add(new(b.DefaultValues[i]));
                }
                Methods = new(b.Methods.Count);
                for (int i = 0; i < b.Methods.Count; i++)
                {
                    Methods.Add(new(b.Methods[i]));
                }
            }
        }

        public static ExClass Create(ExSState exs, ExClass b)
        {
            return new(exs, b);
        }

        public static new ExObjType GetType()
        {
            return ExObjType.CLASS;
        }
        public virtual void Release()
        {
            if ((--ReferenceCount) == 0)
            {
                Attributes.Release();
                DefaultValues.RemoveAll((ExClassMem e) => true); DefaultValues = null;
                Methods.RemoveAll((ExClassMem e) => true); Methods = null;
                MetaFuncs.RemoveAll((ExObject e) => true); MetaFuncs = null;
                Members = null;
                if (Base != null)
                {
                    Base.Release();
                }
            }
        }

        public void LockCls()
        {
            GotInstanced = true;
            if (Base != null)
            {
                Base.LockCls();
            }
        }
        public ExInstance CreateInstance()
        {
            if (!GotInstanced)
            {
                LockCls();
            }
            return ExInstance.Create(SharedState, this);
        }

        public bool GetConstructor(ref ExObject o)
        {
            if (ConstructorID != -1)
            {
                o.Assign(Methods[ConstructorID].Value);
                return true;
            }
            return false;
        }

        public bool SetAttrs(ExObject key, ExObject val)
        {
            if (Members.ContainsKey(key.GetString()))
            {
                ExObject v = Members[key.GetString()];
                if (v.IsField())
                {
                    DefaultValues[v.GetMemberID()].Attributes.Assign(val);
                }
                else
                {
                    Methods[v.GetMemberID()].Attributes.Assign(val);
                }
                return true;
            }
            return false;
        }

        public bool NewSlot(ExSState exs, ExObject key, ExObject val, bool bstat)
        {
            bool bdict = val.Type == ExObjType.CLOSURE || val.Type == ExObjType.NATIVECLOSURE || bstat;
            if (GotInstanced && !bdict)
            {
                return false;
            }

            ExObject tmp = new();
            if (Members.ContainsKey(key.GetString()) && (tmp = Members[key.GetString()]).IsField())
            {
                DefaultValues[Members[key.GetString()].GetMemberID()].Value.Assign(val);
                return true;
            }

            if (bdict)
            {
                int metaid;
                if ((val.Type == ExObjType.CLOSURE || val.Type == ExObjType.NATIVECLOSURE)
                    && (metaid = exs.GetMetaIdx(key.GetString())) != -1)
                {
                    MetaFuncs[metaid].Assign(val);
                }
                else
                {
                    ExObject tmpv = val;
                    if (Base != null && val.Type == ExObjType.CLOSURE)
                    {
                        tmpv.Assign(val.Value._Closure);
                        tmpv.GetClosure().Base = Base;
                        Base.ReferenceCount++;
                    }
                    else
                    {
                        tmpv.GetClosure().Base = this;
                        ReferenceCount++;
                    }

                    if (tmp.Type == ExObjType.NULL)
                    {
                        bool bconstr = exs.ConstructorID.GetString() == key.GetString();

                        if (bconstr)
                        {
                            ConstructorID = Methods.Count;
                        }

                        ExClassMem cm = new();
                        cm.Value.Assign(tmpv);
                        Members.Add(key.GetString(), new((int)ExMemberFlag.METHOD | Methods.Count));
                        Methods.Add(cm);
                    }
                    else
                    {
                        Methods[tmp.GetMemberID()].Value.Assign(tmpv);
                    }
                }
                return true;
            }

            ExClassMem cmem = new();
            cmem.Value.Assign(val);
            Members.Add(key.GetString(), new((int)ExMemberFlag.FIELD | DefaultValues.Count));
            DefaultValues.Add(cmem);

            return true;
        }

        public new string GetDebuggerDisplay()
        {
            if (Base != null)
            {
                return "[" + Base.GetDebuggerDisplay() + "]" + "CLASS(c_idx: " + ConstructorID + ", n_mem: " + Members.Count + ")";
            }
            return "CLASS(c_idx: " + ConstructorID + ", n_mem: " + Members.Count + ")";
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Disposer.DisposeObjects(Base);
            Disposer.DisposeObjects(Attributes);

            Disposer.DisposeList(ref Methods);
            Disposer.DisposeList(ref DefaultValues);
            Disposer.DisposeList(ref MetaFuncs);
        }
    }

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

        public bool GetMetaM(ExVM vm, ExMetaM m, ref ExObject res)
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
