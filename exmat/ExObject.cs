using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using ExMat.Class;
using ExMat.Closure;
using ExMat.FuncPrototype;
using ExMat.Outer;

namespace ExMat.Objects
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExObject : IDisposable
    {
        public ExObjType Type = ExObjType.NULL; // Veri tipi
        public ExObjVal Value;                  // Veri değeri

        private bool disposedValue;

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
            switch (Type)
            {
                case ExObjType.STRING:
                case ExObjType.NULL:
                    {
                        return Value.s_String ?? string.Empty;
                    }
                default:
                    {
                        return string.Empty;
                    }
            }
        }

        public ExWeakRef GetWeakRef()
        {
            return Value._WeakRef;
        }

        public void SetString(string s)
        {
            Value.s_String = s;
        }

        public bool GetBool()
        {
            if (ExTypeCheck.IsFalseable(this))
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
            return Value.d_Dict;
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

        public ExClass GetClass()
        {
            return Value._Class;
        }

        public ExSpace GetSpace()
        {
            return Value.c_Space;
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
                case ExObjType.BOOL: s += GetBool() ? " true" : " false"; break;
                case ExObjType.STRING: s += " " + GetString(); break;
                case ExObjType.CLOSURE: s = GetClosure() == null ? s + GetString() : Value._Closure.GetDebuggerDisplay(); break;
                case ExObjType.NATIVECLOSURE: s = GetNClosure() == null ? s + GetString() : Value._NativeClosure.GetDebuggerDisplay(); break;
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
                    ExDisposer.DisposeList(ref Value.l_List);
                    ExDisposer.DisposeDict(ref Value.d_Dict);

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
        public static void AddReference(ExObjType t, ExObjVal v, bool forced = false)
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
            if (dict != null)
            {
                Type = ExObjType.DICT;
                Value._RefC = new();
                Value.d_Dict = dict;
            }
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
}
