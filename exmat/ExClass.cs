using System.Collections.Generic;
using System.Diagnostics;
using ExMat.Objects;
using ExMat.States;
using ExMat.Utils;

namespace ExMat.Class
{

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
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

        public readonly int LengthReprestation;

        public ExClass()
        {
            ExUtils.InitList(ref MetaFuncs, (int)ExMetaMethod.LAST);
        }
        public ExClass(ExSState exS, ExClass b)
        {
            SharedState = exS;
            Base = b;

            GotInstanced = false;
            ConstructorID = -1;
            LengthReprestation = 0;
            ExUtils.InitList(ref MetaFuncs, (int)ExMetaMethod.LAST);

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

            string k = key.GetString();
            ExObject tmp;
            
            if (Members.ContainsKey(k))
            {
                if(Members[k].IsField())
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
                        Members.Add(k, new((int)ExMemberFlag.METHOD | Methods.Count));
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
            Members.Add(k, new((int)ExMemberFlag.FIELD | DefaultValues.Count));
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
}
