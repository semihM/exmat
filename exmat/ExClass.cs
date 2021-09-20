﻿using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using ExMat.Objects;
using ExMat.States;
using ExMat.Utils;

namespace ExMat.ExClass
{

#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExClass : ExRefC
    {
        public ExClass Base;    // TO-DO Sınıf hiyerarşisi
        public Dictionary<string, ExObject> Members = new();// Özellikler
        public List<ExObject> MetaFuncs = new();            // Meta metotlar
        public List<ExClassMem> DefaultValues = new();      // Özelliklerin varsayılan değerleri
        public List<ExClassMem> Methods = new();            // Metotlar
        public ExObject Attributes = new();                 // Özelliklerin alt-özellikleri

        public bool GotInstanced;           // Örneklendi ?
        public int ConstructorID;           // İnşa metotunun metotlar listesindeki indeksi 
        public ExSState SharedState;        // Ortak değerler

        public readonly ulong Hash;

        public ExClass()
        {
            System.DateTime dt = System.DateTime.Now;
            Hash = (((ulong)(int)dt.Kind) << 62) | ((ulong)dt.Ticks);

            GotInstanced = false;
            ConstructorID = -1;

            ExUtils.InitList(ref MetaFuncs, (int)ExMetaMethod.LAST);
        }

        public ExClass(ExSState exS, ExClass b)
        {
            System.DateTime dt = System.DateTime.Now;
            Hash = (((ulong)(int)dt.Kind) << 62) | ((ulong)dt.Ticks);

            SharedState = exS;
            Base = b;

            GotInstanced = false;
            ConstructorID = -1;
            ExUtils.InitList(ref MetaFuncs, (int)ExMetaMethod.LAST);

            if (b != null)
            {
                ConstructorID = b.ConstructorID;
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
                o = new(Methods[ConstructorID].Value);
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

        private bool AddMethod(ExSState exs, string key, ExObject val, ExObject existing)
        {
            int metaid;
            if ((val.Type is ExObjType.CLOSURE or ExObjType.NATIVECLOSURE)
                && (metaid = exs.GetMetaIdx(key)) != -1)
            {
                MetaFuncs[metaid].Assign(val);
            }
            else
            {
                ExObject tmpv = val;
                if (Base != null && val.Type == ExObjType.CLOSURE)
                {
                    tmpv.Assign(val.ValueCustom._Closure);
                    tmpv.GetClosure().Base = Base;
                    Base.ReferenceCount++;
                }
                else
                {
                    tmpv.GetClosure().Base = this;
                    ReferenceCount++;
                }

                if (ExTypeCheck.IsNull(existing))
                {
                    bool bconstr = exs.ConstructorID.GetString() == key;

                    if (bconstr)
                    {
                        ConstructorID = Methods.Count;
                    }

                    ExClassMem cm = new();
                    cm.Value.Assign(tmpv);
                    Members.Add(key, new((int)ExMemberFlag.METHOD | Methods.Count));
                    Methods.Add(cm);
                }
                else
                {
                    Methods[existing.GetMemberID()].Value.Assign(tmpv);
                }
            }
            return true;
        }

        private bool AddMember(string k, ExObject val)
        {
            ExClassMem cmem = new();
            cmem.Value.Assign(val);
            Members.Add(k, new((int)ExMemberFlag.FIELD | DefaultValues.Count));
            DefaultValues.Add(cmem);
            return true;
        }

        public bool NewSlot(ExSState exs, ExObject key, ExObject val, bool bstat)
        {
            bool bdict = (val.Type is ExObjType.CLOSURE or ExObjType.NATIVECLOSURE) || bstat;
            if (GotInstanced && !bdict)
            {
                return false;
            }

            string k = key.GetString();
            ExObject tmp;

            if (Members.ContainsKey(k))
            {
                if (Members[k].IsField())
                {
                    DefaultValues[Members[k].GetMemberID()].Value.Assign(val);
                    return true;
                }
                else
                {
                    tmp = Members[k];
                }
            }
            else
            {
                tmp = new();
            }

            return bdict ? AddMethod(exs, k, val, tmp) : AddMember(k, val);
        }

#if DEBUG
        public new string GetDebuggerDisplay()
        {
            return Base != null
                ? "[" + Base.GetDebuggerDisplay() + "]" + "CLASS(c_idx: " + ConstructorID + ", n_mem: " + Members.Count + ")"
                : "CLASS(c_idx: " + ConstructorID + ", n_mem: " + Members.Count + ")";
        }
#endif

        protected override void Dispose(bool disposing)
        {
            if (ReferenceCount > 0)
            {
                return;
            }

            base.Dispose(disposing);

            ExDisposer.DisposeObjects(Base);
            ExDisposer.DisposeObjects(Attributes);

            ExDisposer.DisposeList(ref Methods);
            ExDisposer.DisposeList(ref DefaultValues);
            ExDisposer.DisposeList(ref MetaFuncs);
        }
    }
}
