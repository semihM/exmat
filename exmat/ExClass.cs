using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using ExMat.Objects;
using ExMat.States;
using ExMat.Utils;

namespace ExMat.ExClass
{

    /// <summary>
    /// Class object
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExClass : ExRefC
    {
        /// <summary>
        /// WIP, base class
        /// </summary>
        public ExClass Base;    // TO-DO Sınıf hiyerarşisi
        /// <summary>
        /// Member names and values
        /// </summary>
        public Dictionary<string, ExObject> Members = new();// Özellikler
        /// <summary>
        /// Meta methods list
        /// </summary>
        public List<ExObject> MetaFuncs = new();            // Meta metotlar
        /// <summary>
        /// Default values for members used for instancing
        /// </summary>
        public List<ExClassMem> DefaultValues = new();      // Özelliklerin varsayılan değerleri
        /// <summary>
        /// Methods defined
        /// </summary>
        public List<ExClassMem> Methods = new();            // Metotlar
        /// <summary>
        /// Attributes dictionary
        /// </summary>
        public ExObject Attributes = new();                 // Özelliklerin alt-özellikleri

        /// <summary>
        /// Has this class ever been instanced ?
        /// </summary>
        public bool HasInstances => GotInstanced;

        internal bool GotInstanced;           // Örneklendi ?

        /// <summary>
        /// Method list index of the constructor
        /// </summary>
        public int ConstructorIndex => ConstructorID;

        internal int ConstructorID;           // İnşa metotunun metotlar listesindeki indeksi

        /// <summary>
        /// Shared state of the VM
        /// </summary>
        public ExSState SharedState;        // Ortak değerler
        /// <summary>
        /// Unique hash value for the class for comparison
        /// </summary>
        public readonly ulong Hash;

        /// <summary>
        /// Empty constructor
        /// </summary>
        public ExClass()
        {
            DateTime dt = DateTime.Now;
            Hash = (((ulong)(int)dt.Kind) << 62) | ((ulong)dt.Ticks);

            GotInstanced = false;
            ConstructorID = -1;

            ExUtils.InitList(ref MetaFuncs, Enum.GetNames(typeof(ExMetaMethod)).Length);
        }

        /// <summary>
        /// Class within given shared state and base class
        /// </summary>
        /// <param name="exS">Shared state</param>
        /// <param name="b">Base class or <see langword="null"/></param>
        public ExClass(ExSState exS, ExClass b)
        {
            DateTime dt = DateTime.Now;
            Hash = (((ulong)(int)dt.Kind) << 62) | ((ulong)dt.Ticks);

            SharedState = exS;
            Base = b;

            GotInstanced = false;
            ConstructorID = -1;
            ExUtils.InitList(ref MetaFuncs, Enum.GetNames(typeof(ExMetaMethod)).Length);

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

        /// <summary>
        /// Static method to call the constructor
        /// </summary>
        /// <param name="exs">Shared state</param>
        /// <param name="b">Base class</param>
        /// <returns>A new class</returns>
        public static ExClass Create(ExSState exs, ExClass b)
        {
            return new(exs, b);
        }

        internal virtual void Release()
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

        private void LockCls()
        {
            GotInstanced = true;
            if (Base != null)
            {
                Base.LockCls();
            }
        }

        /// <summary>
        /// Create an instance of this class
        /// </summary>
        /// <returns>A new instance</returns>
        public ExInstance CreateInstance()
        {
            if (!GotInstanced)
            {
                LockCls();
            }
            return ExInstance.Create(SharedState, this);
        }

        /// <summary>
        /// Get the constructor method of the class if any. <paramref name="o"/> will be <see langword="null"/> if there is no constructor
        /// </summary>
        /// <param name="o">Result method</param>
        /// <returns>Wheter there were a constructor method</returns>
        public bool GetConstructor(ref ExObject o)
        {
            if (ConstructorID != -1)
            {
                o = new(Methods[ConstructorID].Value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Set an attribute to given value
        /// </summary>
        /// <param name="key">Attribute name</param>
        /// <param name="val">New value</param>
        /// <returns>Wheter an attribute of given name <paramref name="key"/> existed and updated</returns>
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

        internal bool NewSlot(ExSState exs, ExObject key, ExObject val, bool bstat)
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
        private new string GetDebuggerDisplay()
        {
            return Base != null
                ? "[" + Base.GetDebuggerDisplay() + "]" + "CLASS(c_idx: " + ConstructorID + ", n_mem: " + Members.Count + ")"
                : "CLASS(c_idx: " + ConstructorID + ", n_mem: " + Members.Count + ")";
        }
#endif

        internal override void Dispose(bool disposing)
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
