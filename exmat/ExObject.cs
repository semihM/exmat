using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using ExMat.Class;
using ExMat.Closure;
using ExMat.FuncPrototype;

namespace ExMat.Objects
{
    public static class Disposer
    {
        public static void DisposeList<T>(ref List<T> lis) where T : IDisposable, new()
        {
            if (lis == null)
            {
                return;
            }
            foreach (T o in lis)
            {
                o.Dispose();
            }
            lis.RemoveRange(0, lis.Count);
            lis = null;
        }
        public static void DisposeDict<R, T>(ref Dictionary<R, T> dict)
            where T : IDisposable, new()
        {
            if (dict == null)
            {
                return;
            }
            foreach (KeyValuePair<R, T> pair in dict)
            {
                pair.Value.Dispose();
            }
            dict = null;
        }

        public static void DisposeObjects<T>(params T[] ps) where T : IDisposable, new()
        {
            foreach (T o in ps)
            {
                if (o != null)
                {
                    o.Dispose();
                }
            }
        }
    }

    public enum ExMemberFlag
    {
        METHOD = 0x01000000,
        FIELD = 0x02000000
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExObject : IDisposable
    {
        public ExObjType Type = ExObjType.NULL;

        public ExObjVal Value;

        private bool disposedValue;

        public bool IsDelegable()
        {
            return ((int)Type & (int)ExObjFlag.DELEGABLE) != 0;
        }

        public bool IsRealNumber()
        {
            return Type == ExObjType.INTEGER || Type == ExObjType.FLOAT;
        }

        public bool IsNumeric()
        {
            return ((int)Type & (int)ExObjFlag.NUMERIC) != 0;
        }

        public bool IsCountingRefs()
        {
            return ((int)Type & (int)ExObjFlag.COUNTREFERENCES) != 0;
        }

        public long GetInt()
        {
            return Type == ExObjType.INTEGER ? Value.i_Int : (long)Value.f_Float;
        }

        public double GetFloat()
        {
            return Type == ExObjType.FLOAT ? Value.f_Float : Value.i_Int;
        }

        public Complex GetComplex()
        {
            return new(Value.f_Float, Value.c_Float);
        }
        public Complex GetComplexConj()
        {
            return new(Value.f_Float, -Value.c_Float);
        }

        public string GetComplexString()
        {
            if (Value.f_Float == 0.0)
            {
                return Value.c_Float + "i";
            }
            else if (Value.c_Float > 0)
            {
                return Value.f_Float + "+" + Value.c_Float + "i";
            }
            else if (Value.c_Float == 0.0)
            {
                return Value.f_Float.ToString();
            }
            else
            {
                return Value.f_Float + Value.c_Float.ToString() + "i";
            }
        }

        public string GetString()
        {
            return Type == ExObjType.STRING ? Value.s_String : (Type == ExObjType.NULL && Value.s_String != null ? Value.s_String : string.Empty);
        }

        public void SetString(string s)
        {
            Value.s_String = s;
        }

        public bool IsFalseable()
        {
            return ((int)Type & (int)ExObjFlag.CANBEFALSE) != 0;
        }
        public bool GetBool()
        {
            if (IsFalseable())
            {
                switch (Type)
                {
                    case ExObjType.BOOL:
                        return Value.b_Bool;
                    case ExObjType.NULL:
                        return false;
                    case ExObjType.INTEGER:
                        return Value.i_Int != 0;
                    case ExObjType.FLOAT:
                        return Value.f_Float != 0;
                    case ExObjType.COMPLEX:
                        return Value.f_Float != 0 && Value.c_Float != 0;
                    default:
                        return true;
                }
            }
            return true;
        }

        public Dictionary<string, ExObject> GetDict()
        {
            return Type == ExObjType.DICT ? Value.d_Dict : null;
        }

        public List<ExObject> GetList()
        {
            return Value.l_List;
        }

        public ExClosure GetClosure()
        {
            return Value._Closure;
        }

        public ExNativeClosure GetNClosure()
        {
            return Value._NativeClosure;
        }

        public ExInstance GetInstance()
        {
            return Value._Instance;
        }

        public virtual string GetDebuggerDisplay()
        {
            string s = Type.ToString();
            switch (Type)
            {
                case ExObjType.ARRAY: s += Value.l_List == null ? " null" : "(" + Value.l_List.Count.ToString() + ")"; break;
                case ExObjType.INTEGER: s += " " + GetInt(); break;
                case ExObjType.FLOAT: s += " " + GetFloat(); break;
                case ExObjType.COMPLEX: s += " " + GetComplex().ToString(); break;
                //case ExObjType.SYMBOL: s += " (" + GetSymCoef().GetDebuggerDisplay() + ") * (" + (GetSym().type == ExSymType.VARIABLE ? GetSymName() : GetSym().GetNumeric().ToString()) + ") ** (" + GetSymExpo().GetDebuggerDisplay() + ")"; break;
                case ExObjType.BOOL: s += GetBool() ? " true" : " false"; break;
                case ExObjType.STRING: s += " " + GetString(); break;
                case ExObjType.CLOSURE: s = (GetClosure() == null ? s + GetString() : Value._Closure.GetDebuggerDisplay()); break;
                case ExObjType.NATIVECLOSURE: s = (GetNClosure() == null ? s + GetString() : Value._NativeClosure.GetDebuggerDisplay()); break;
                case ExObjType.NULL: break;
            }
            return s;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Value.i_Int = 0;
                    Value.f_Float = 0;
                    Value.c_Float = 0;
                    Value.s_String = null;

                    Value.c_Space = null;
                    Disposer.DisposeList(ref Value.l_List);
                    Disposer.DisposeDict(ref Value.d_Dict);

                    Value._RefC = null;
                    Value._WeakRef = null;

                    Value._Closure = null;
                    Value._NativeClosure = null;

                    Value._Class = null;
                    Value._Instance = null;

                    Value._Outer = null;
                    Value._FuncPro = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public bool IsField()
        {
            return (GetInt() & (int)ExMemberFlag.FIELD) > 0;
        }
        public bool IsMethod()
        {
            return (GetInt() & (int)ExMemberFlag.METHOD) > 0;
        }
        public int GetMemberID()
        {
            return (int)GetInt() & 0x00FFFFFF;
        }

        public static bool IsRefC(ExObjType t)
        {
            return ((int)t & (int)ExObjFlag.COUNTREFERENCES) > 0;
        }
        public virtual void AddReference(ExObjType t, ExObjVal v, bool forced = false)
        {
            if (!IsRefC(t) && !forced)
            {
                return;
            }

            if (v._RefC == null)
            {
                v._RefC = new();
            }

            v._RefC.ReferenceCount++;
        }
        public virtual void Release()
        {
            if (IsRefC(Type) && ((--Value._RefC.ReferenceCount) == 0))
            {
                Nullify();
            }
        }
        public static void Release(ExObjType t, ExObjVal v)
        {
            if (IsRefC(t) && v._RefC != null && (--v._RefC.ReferenceCount) == 0)
            {
                v.i_Int = 0;
            }
        }

        ///////////////////////////////
        /// BASIC
        ///////////////////////////////
        public ExObject()
        {
            Type = ExObjType.NULL;
            Value = new();
        }
        public ExObject(ExObject objp)
        {
            Type = objp.Type;
            Value = objp.Value;
            AddReference(Type, Value);
        }
        public void Assign(ExObject o)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value = o.Value;
            Type = o.Type;

            AddReference(Type, Value);
            Release(t, v);
        }

        ///////////////////////////////
        /// SCALAR
        ///////////////////////////////
        public ExObject(long i)
        {
            Type = ExObjType.INTEGER;
            Value.i_Int = i;
        }
        public void Assign(long i)
        {
            Release(Type, Value);
            Value.i_Int = i;
            Type = ExObjType.INTEGER;
        }
        public ExObject(double f)
        {
            Type = ExObjType.FLOAT;
            Value.f_Float = f;
        }
        public void Assign(double f)
        {
            Release(Type, Value);
            Value.f_Float = f;
            Type = ExObjType.FLOAT;
        }
        public ExObject(bool b)
        {
            Type = ExObjType.BOOL;
            Value.b_Bool = b;
        }
        public void Assign(bool b)
        {
            Release(Type, Value);
            Value.b_Bool = b;
            Type = ExObjType.BOOL;
        }
        public ExObject(string s)
        {
            Type = ExObjType.STRING;
            Value.s_String = s;
        }
        public void Assign(string s)
        {
            Release(Type, Value);
            Value.s_String = s;
            Type = ExObjType.STRING;
        }
        public ExObject(Complex f)
        {
            Type = ExObjType.COMPLEX;
            Value.f_Float = f.Real;
            Value.c_Float = f.Imaginary;
        }
        public void Assign(Complex f)
        {
            Release(Type, Value);
            Value.f_Float = f.Real;
            Value.c_Float = f.Imaginary;
            Type = ExObjType.COMPLEX;
        }
        ///////////////////////////////
        /// EXREF
        ///////////////////////////////
        public ExObject(Dictionary<string, ExObject> dict)
        {
            Type = ExObjType.DICT;
            Value._RefC = new();
            Value.d_Dict = dict;
        }
        public void Assign(Dictionary<string, ExObject> dict)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value.d_Dict = dict;
            Value._RefC = new();
            Type = ExObjType.DICT;

            AddReference(Type, Value, true);
            Release(t, v);
        }
        public ExObject(List<ExObject> o)
        {
            Type = ExObjType.ARRAY;
            Value.l_List = o;
            Value._RefC = new();
            AddReference(Type, Value, true);
        }
        public void Assign(List<ExObject> o)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value.l_List = o;
            Value._RefC = new();
            Type = ExObjType.ARRAY;

            AddReference(Type, Value, true);
            Release(t, v);
        }

        public ExObject(ExInstance o)
        {
            Type = ExObjType.INSTANCE;
            Value._Instance = o;
            Value._RefC = new();
            AddReference(Type, Value, true);
        }
        public void Assign(ExInstance o)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value._Instance = o;
            Value._RefC = new() { ReferenceCount = Value._Instance.ReferenceCount++ };
            Type = ExObjType.INSTANCE;

            AddReference(Type, Value, true);
            Release(t, v);
        }
        public ExObject(ExList o)
        {
            Type = ExObjType.ARRAY;
            Value.l_List = o.Value.l_List;
            Value._RefC = new();
            AddReference(Type, Value, true);
        }
        public void Assign(ExList o)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value.l_List = o.Value.l_List;
            Value._RefC = new() { ReferenceCount = o.Value._RefC.ReferenceCount++ };
            Type = ExObjType.ARRAY;

            AddReference(Type, Value, true);
            Release(t, v);
        }
        public ExObject(ExClass o)
        {
            Type = ExObjType.CLASS;
            Value._Class = o;
            Value._RefC = new();
            AddReference(Type, Value, true);
        }
        public void Assign(ExClass o)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value._Class = o;
            Value._RefC = new() { ReferenceCount = Value._Class.ReferenceCount++ };
            Type = ExObjType.CLASS;

            AddReference(Type, Value, true);
            Release(t, v);
        }

        public ExObject(ExClosure o)
        {
            Type = ExObjType.CLOSURE;
            Value._Closure = o;
            Value._RefC = new();
            AddReference(Type, Value, true);
        }
        public void Assign(ExClosure o)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value._Closure = o;
            Value._RefC = new() { ReferenceCount = Value._Closure.ReferenceCount++ };
            Type = ExObjType.CLOSURE;

            AddReference(Type, Value, true);
            Release(t, v);
        }

        public ExObject(ExNativeClosure o)
        {
            Type = ExObjType.NATIVECLOSURE;
            Value._NativeClosure = o;
            Value._RefC = new();
            AddReference(Type, Value, true);
        }
        public void Assign(ExNativeClosure o)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value._NativeClosure = o;
            Value._RefC = new() { ReferenceCount = Value._NativeClosure.ReferenceCount++ };
            Type = ExObjType.NATIVECLOSURE;

            AddReference(Type, Value, true);
            Release(t, v);
        }

        public ExObject(ExOuter o)
        {
            Type = ExObjType.OUTER;
            Value._Outer = o;
            AddReference(Type, Value, true);
        }
        public void Assign(ExOuter o)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value._Outer = o;
            Value._RefC = new() { ReferenceCount = Value._Outer.ReferenceCount++ };
            Type = ExObjType.OUTER;

            AddReference(Type, Value, true);
            Release(t, v);
        }

        public ExObject(ExWeakRef o)
        {
            Type = ExObjType.WEAKREF;
            Value._WeakRef = o;
            AddReference(Type, Value, true);
        }
        public void Assign(ExWeakRef o)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value._WeakRef = o;
            Value._RefC = new() { ReferenceCount = Value._WeakRef.ReferenceCount++ };
            Type = ExObjType.WEAKREF;

            AddReference(Type, Value, true);
            Release(t, v);
        }

        public ExObject(ExPrototype o)
        {
            Type = ExObjType.FUNCPRO;
            Value._FuncPro = o;
            AddReference(Type, Value, true);
        }
        public void Assign(ExPrototype o)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value._FuncPro = o;
            Value._RefC = new() { ReferenceCount = Value._FuncPro.ReferenceCount++ };
            Type = ExObjType.FUNCPRO;

            AddReference(Type, Value, true);
            Release(t, v);
        }

        public ExObject(ExSpace space)
        {
            Type = ExObjType.SPACE;
            Value.c_Space = space;
            Value._RefC = new();

            AddReference(Type, Value, true);
        }
        public void Assign(ExSpace space)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value.c_Space = space;
            Type = ExObjType.SPACE;
            Value._RefC = new();

            AddReference(Type, Value, true);
            Release(t, v);
        }
        //
        public void Nullify()
        {
            ExObjType oldt = Type;
            ExObjVal oldv = Value;
            Type = ExObjType.NULL;
            Value = new();
            Release(oldt, oldv);
        }

    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExRefC : IDisposable
    {
        public int ReferenceCount;
        public ExWeakRef WeakReference;
        private bool disposedValue;

        public ExRefC() { }

        public ExWeakRef GetWeakRef(ExObjType t, ExObjVal v)
        {
            if (WeakReference == null)
            {
                ExWeakRef e = new();
                e.ReferencedObject = new();
                e.ReferencedObject.Type = t;
                e.ReferencedObject.Value = v;
                WeakReference = e;
            }
            return WeakReference;
        }

        public string GetDebuggerDisplay()
        {
            return "REFC: " + ReferenceCount;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    WeakReference = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExRefC()
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

    public class ExWeakRef : ExRefC
    {
        public ExObject ReferencedObject;
        private bool disposedValue;

        public virtual void Release()
        {
            if (((int)ReferencedObject.Type & (int)ExObjFlag.COUNTREFERENCES) != 0)
            {
                ReferencedObject.Value._WeakRef = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disposer.DisposeObjects(ReferencedObject);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

    }

    public class ExList : ExObject
    {
        public ExList()
        {
            Type = ExObjType.ARRAY;
            Value.l_List = new();
            Value._RefC = new();
        }

        public ExList(bool n)
        {
            Type = ExObjType.ARRAY;
            Value.l_List = n ? new() : null;
            Value._RefC = new();
        }
        public ExList(char c)
        {
            Type = ExObjType.ARRAY;
            Value.l_List = c != '0' ? new() : null;
            Value._RefC = new();
        }
        public ExList(ExList e)
        {
            Type = ExObjType.ARRAY;
            Value.l_List = e.Value.l_List;
            Value._RefC = new();
        }
        public ExList(List<ExObject> e)
        {
            Type = ExObjType.ARRAY;
            Value.l_List = e;
            Value._RefC = new();
        }

        public ExList(List<string> e)
        {
            Type = ExObjType.ARRAY;

            Value.l_List = new(e.Count);

            Value._RefC = new();

            foreach (string s in e)
            {
                Value.l_List.Add(new(s));
            }
        }
    }

    public class ExRegFunc : IDisposable
    {
        public string Name;             // Fonksiyon ismi
        public ExFunc Function;         // Fonksiyon referansı
        public bool IsDelegateFunction; // Temsili fonksiyon ?

        public string ParameterMask;    // Parameter tipleri maskesi
        public int nParameterChecks;    // Argüman sayısı kontrolü

        public Dictionary<int, ExObject> DefaultValues = new(); // Varsayılan değerler

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ParameterMask = null;
                    Name = null;
                    Function = null;
                    nParameterChecks = 0;
                    Disposer.DisposeDict(ref DefaultValues);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExRegFunc()
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
    public class ExStack : IDisposable //<T> where T : class, new()
    {
        public List<ExObject> Values;

        public int Size;
        public int Allocated;
        private bool disposedValue;

        public string GetDebuggerDisplay()
        {
            return "STACK(" + Allocated + "): " + Size + (Values == null ? " null" : " " + Values.Count);
        }

        public ExStack() { Size = 0; Allocated = 0; Values = new(); }

        public ExStack(int size)
        {
            Values = new(size);
            Size = 0;
            Allocated = size;
        }
        public ExStack(ExStack stc)
        {
            CopyFrom(stc);
        }

        public void Release()
        {
            if (Allocated > 0)
            {
                for (int i = 0; i < Size; i++)
                {
                    Values[i].Release();
                    Values[i] = null;
                }
                Values = null;
            }
        }

        public void Resize(int n, ExObject filler = null)
        {
            if (n > Allocated)
            {
                ReAlloc(n);
            }

            if (n > Size)
            {
                while (Size < n)
                {
                    Values[Size] = new(filler);
                    Size++;
                }
            }
            else
            {
                for (int i = n; i < Size; i++)
                {
                    Values[i].Release();
                    Values[i] = null;
                }
                Size = n;
            }
        }

        public void ReAlloc(int n)
        {
            n = n > 0 ? n : 4;
            if (Values == null)
            {
                Values = new(n);
                for (int i = 0; i < n; i++)
                {
                    Values.Add(new());
                }
            }
            else
            {
                int dif = n - Allocated;
                if (dif < 0)
                {
                    for (int i = 0; i < -dif; i++)
                    {
                        Values[Allocated - i - 1].Release();
                        Values.RemoveAt(Allocated - i - 1);
                    }
                }
                else
                {
                    for (int i = 0; i < dif; i++)
                    {
                        Values.Add(new());
                    }
                }
            }
            Allocated = n;
        }

        public void CopyFrom(ExStack stc)
        {
            if (Size > 0)
            {
                Resize(0);
            }
            if (stc.Size > Allocated)
            {
                ReAlloc(stc.Size);
            }

            for (int i = 0; i < stc.Size; i++)
            {
                Values[i] = new(stc.Values[i]);
            }
            Size = stc.Size;
        }

        public ExObject Back()
        {
            return Values[Size - 1];
        }

        public ExObject Push(ExObject o)
        {
            if (Allocated <= Size)
            {
                ReAlloc(Size * 2);
            }
            return Values[Size++] = new(o);
        }

        public void Pop()
        {
            Size--;
            Values[Size].Release();
            Values[Size] = new(); // = null
        }

        public void Insert(int i, ExObject o)
        {
            //_values.Insert(i, o);
            Resize(Size + 1);
            for (int j = Size; j > i; j--)
            {
                Values[j] = Values[j - 1];
            }
            Values[i].Assign(o);
        }

        public void Remove(int i)
        {
            Values[i].Release();
            Values.RemoveAt(i);
            Size--;
        }


        public ExObject this[int n]
        {
            get => Values[n];
            set => Values[n].Assign(value);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disposer.DisposeList(ref Values);
                    Allocated = 0;
                    Size = 0;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExStack()
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
    public class ExSpace : IDisposable
    {
        public int Dimension = 1;
        public string Domain = string.Empty;
        public char Sign = '\\';

        public ExSpace Child;
        private bool disposedValue;

        public string GetString()
        {
            return "@" + Domain + "@" + Sign + "@" + Dimension + (Child == null ? "" : "$$" + Child.GetString());
        }

        public static ExSpace GetSpaceFromString(string s)
        {
            ExSpace spc = new();
            ExSpace curr = spc;
            foreach (string ch in s.Split("$$", StringSplitOptions.RemoveEmptyEntries))
            {
                ExSpace c = new();
                string[] arr = ch.Split("@", StringSplitOptions.RemoveEmptyEntries);
                c.Domain = arr[0];
                c.Sign = arr[1][0];
                c.Dimension = int.Parse(arr[2]);
                curr.Child = c;
                curr = c;
            }
            return spc.Child;
        }
        public ExSpace() { }
        public ExSpace(int dim, string dom, char sign, ExSpace ch)
        {
            Dimension = dim;
            Sign = sign;
            Domain = dom;
            Child = ch;
        }

        public void AddDimension(ExSpace ch)
        {
            Child = new(ch.Dimension, ch.Domain, ch.Sign, null);
        }

        public ExSpace(string spc, int d, char s = '\\')
        {
            Domain = spc;
            Dimension = d;
            Sign = s;
        }

        public static ExObject Create(string spc, char s, params int[] dims)
        {
            return new(CreateSpace(spc, s, dims));
        }

        public static ExSpace CreateSpace(string spc, char s, params int[] dims)
        {
            if (dims.Length == 0)
            {
                return null;
            }
            if (dims.Length == 1)
            {
                return new(spc, dims[0], s);
            }

            ExSpace p = new(spc, dims[0], s);
            ExSpace ch = new(spc, dims[1], s);
            p.Child = ch;

            ExSpace curr = ch;
            for (int i = 2; i < dims.Length; i++)
            {
                ch = new(spc, dims[i], s);
                curr.Child = ch;
                curr = ch;
            }

            return p;
        }

        public string GetSpaceString()
        {
            string s = "SPACE(" + Domain + ", " + (Dimension == -1 ? "var" : Dimension) + (Sign == '\\' ? ")" : ", " + Sign + ")");
            if (Child != null)
            {
                s += " x " + Child.GetSpaceString();
            }
            return s;
        }
        public virtual string GetDebuggerDisplay()
        {
            return GetSpaceString();
        }

        public static void Copy(ExSpace p, ExSpace ch)
        {
            p.Dimension = ch.Dimension;
            p.Sign = ch.Sign;
            p.Domain = ch.Domain;
            p.Child = ch.Child;
        }

        public ExSpace DeepCopy()
        {
            ExSpace s = new();
            s.Sign = Sign;
            s.Dimension = Dimension;
            s.Domain = Domain;
            ExSpace ch = Child;

            if (ch != null)
            {
                s.Child = ch.DeepCopy();
            }

            return s;
        }

        public ExSpace this[int i]
        {
            get
            {
                ExSpace ch = Child;
                while (i > 0 && ch != null)
                {
                    i--;
                    ch = ch.Child;
                }
                return ch;
            }
        }

        public int VarCount()
        {
            int d = Dimension == -1 ? 1 : 0;
            ExSpace ch = Child;
            while (ch != null)
            {
                d += ch.Dimension == -1 ? 1 : 0;
                ch = ch.Child;
            }
            return d;
        }

        public int Depth()
        {
            int d = 1;
            ExSpace ch = Child;
            while (ch != null)
            {
                d++;
                ch = ch.Child;
            }
            return d;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Domain = null;
                    Dimension = 0;
                    if (Child != null)
                    {
                        Child.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExSpace()
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
}
