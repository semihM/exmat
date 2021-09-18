using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using ExMat.Closure;
using ExMat.ExClass;
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
            return Value.f_Float == 0.0
                ? Value.c_Float + "i"
                : Value.c_Float > 0
                    ? Value.f_Float + "+" + Value.c_Float + "i"
                    : Value.c_Float == 0.0
                                    ? Value.f_Float.ToString(CultureInfo.CurrentCulture)
                                    : Value.f_Float + Value.c_Float.ToString(CultureInfo.CurrentCulture) + "i";
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

        public ExClass.ExClass GetClass()
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
                case ExObjType.ARRAY: s += Value.l_List == null ? " null" : "(" + Value.l_List.Count.ToString(CultureInfo.CurrentCulture) + ")"; break;
                case ExObjType.INTEGER: s += " " + GetInt(); break;
                case ExObjType.FLOAT: s += " " + GetFloat(); break;
                case ExObjType.COMPLEX: s += " " + GetComplex().ToString(CultureInfo.CurrentCulture); break;
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
                    Release(Type, Value);
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

        public static void AddReference(ExObjType t, ExObjVal v, bool forced = false)
        {
            if (forced || ExTypeCheck.DoesTypeCountRef(t))
            {
                v._RefC.ReferenceCount++;
            }
        }

        public virtual void Release()
        {
            if (ExTypeCheck.DoesTypeCountRef(Type) && ((--Value._RefC.ReferenceCount) == 0))
            {
                Nullify();
            }
        }
        public static void Release(ExObjType t, ExObjVal v)
        {
            if (ExTypeCheck.DoesTypeCountRef(t) && (--v._RefC.ReferenceCount) == 0)
            {
                v.s_String = null;

                ExDisposer.DisposeDict(ref v.d_Dict);
                ExDisposer.DisposeList(ref v.l_List);

                ExDisposer.DisposeObject(ref v.c_Space);
                ExDisposer.DisposeObject(ref v._Class);
                ExDisposer.DisposeObject(ref v._Instance);
                ExDisposer.DisposeObject(ref v._Closure);
                ExDisposer.DisposeObject(ref v._NativeClosure);
                ExDisposer.DisposeObject(ref v._WeakRef);
                ExDisposer.DisposeObject(ref v._RefC);
                ExDisposer.DisposeObject(ref v._Outer);
                ExDisposer.DisposeObject(ref v._FuncPro);
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

        public ExObject(ExObject other)
        {
            Type = other.Type;
            Value = other.Value;
            AddReference(Type, Value);
        }

        ///////////////////////////////
        /// SCALAR
        ///////////////////////////////
        public ExObject(long i)
        {
            Type = ExObjType.INTEGER;
            Value.i_Int = i;
        }

        public ExObject(double f)
        {
            Type = ExObjType.FLOAT;
            Value.f_Float = f;
        }

        public ExObject(bool b)
        {
            Type = ExObjType.BOOL;
            Value.b_Bool = b;
        }

        public ExObject(string s)
        {
            Type = ExObjType.STRING;
            Value.s_String = s;
        }

        public ExObject(Complex cmplx)
        {
            Type = ExObjType.COMPLEX;
            Value.f_Float = cmplx.Real;
            Value.c_Float = cmplx.Imaginary;
        }

        ///////////////////////////////
        /// Ref counted
        ///////////////////////////////
        public ExObject(Dictionary<string, ExObject> dict)
        {
            Type = ExObjType.DICT;
            Value.d_Dict = dict;
            Value._RefC = new();
            AddReference(Type, Value, true);
        }

        public ExObject(List<ExObject> lis)
        {
            Type = ExObjType.ARRAY;
            Value.l_List = lis;
            Value._RefC = new();
            AddReference(Type, Value, true);
        }

        public ExObject(ExInstance inst)
        {
            Type = ExObjType.INSTANCE;
            Value._Instance = inst;
            Value._RefC = Value._Instance;
            AddReference(Type, Value, true);
        }

        public ExObject(ExClass.ExClass @class)
        {
            Type = ExObjType.CLASS;
            Value._Class = @class;
            Value._RefC = Value._Class;
            AddReference(Type, Value, true);
        }

        public ExObject(ExClosure cls)
        {
            Type = ExObjType.CLOSURE;
            Value._Closure = cls;
            Value._RefC = Value._Closure;
            AddReference(Type, Value, true);
        }

        public ExObject(ExNativeClosure ncls)
        {
            Type = ExObjType.NATIVECLOSURE;
            Value._NativeClosure = ncls;
            Value._RefC = Value._NativeClosure;
            AddReference(Type, Value, true);
        }

        public ExObject(ExOuter outer)
        {
            Type = ExObjType.OUTER;
            Value._Outer = outer;
            Value._RefC = Value._Outer;
            AddReference(Type, Value, true);
        }

        public ExObject(ExWeakRef wref)
        {
            Type = ExObjType.WEAKREF;
            Value._WeakRef = wref;
            Value._RefC = Value._WeakRef;
            AddReference(Type, Value, true);
        }

        public ExObject(ExPrototype pro)
        {
            Type = ExObjType.FUNCPRO;
            Value._FuncPro = pro;
            Value._RefC = Value._FuncPro;
            AddReference(Type, Value, true);
        }

        public ExObject(ExSpace space)
        {
            Type = ExObjType.SPACE;
            Value.c_Space = space;
            Value._RefC = Value.c_Space;
            AddReference(Type, Value, true);
        }

        // Assignement
        public void Assign(ExObject other)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value = other.Value;
            Type = other.Type;

            AddReference(Type, Value);
            Release(t, v);
        }
        public void Assign(long i)
        {
            Release(Type, Value);
            Value.i_Int = i;
            Type = ExObjType.INTEGER;
        }
        public void Assign(double f)
        {
            Release(Type, Value);
            Value.f_Float = f;
            Type = ExObjType.FLOAT;
        }
        public void Assign(bool b)
        {
            Release(Type, Value);
            Value.b_Bool = b;
            Type = ExObjType.BOOL;
        }
        public void Assign(string s)
        {
            Release(Type, Value);
            Value.s_String = s;
            Type = ExObjType.STRING;
        }
        public void Assign(Complex cmplx)
        {
            Release(Type, Value);
            Value.f_Float = cmplx.Real;
            Value.c_Float = cmplx.Imaginary;
            Type = ExObjType.COMPLEX;
        }
        public void Assign(ExSpace space)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value.c_Space = space;
            Type = ExObjType.SPACE;
            Value._RefC = Value.c_Space;

            AddReference(Type, Value, true);
            Release(t, v);
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
        public void Assign(List<ExObject> lis)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value.l_List = lis;
            Value._RefC = new();
            Type = ExObjType.ARRAY;

            AddReference(Type, Value, true);
            Release(t, v);
        }
        public void Assign(ExInstance inst)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value._Instance = inst;
            Value._RefC = Value._Instance;
            Type = ExObjType.INSTANCE;

            AddReference(Type, Value, true);
            Release(t, v);
        }
        public void Assign(ExClass.ExClass @class)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value._Class = @class;
            Value._RefC = Value._Class;
            Type = ExObjType.CLASS;

            AddReference(Type, Value, true);
            Release(t, v);
        }
        public void Assign(ExClosure cls)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value._Closure = cls;
            Value._RefC = Value._Closure;
            Type = ExObjType.CLOSURE;

            AddReference(Type, Value, true);
            Release(t, v);
        }
        public void Assign(ExNativeClosure ncls)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value._NativeClosure = ncls;
            Value._RefC = Value._NativeClosure;
            Type = ExObjType.NATIVECLOSURE;

            AddReference(Type, Value, true);
            Release(t, v);
        }
        public void Assign(ExOuter outer)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value._Outer = outer;
            Value._RefC = Value._Outer;
            Type = ExObjType.OUTER;

            AddReference(Type, Value, true);
            Release(t, v);
        }
        public void Assign(ExPrototype pro)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value._FuncPro = pro;
            Value._RefC = Value._FuncPro;
            Type = ExObjType.FUNCPRO;

            AddReference(Type, Value, true);
            Release(t, v);
        }
        public void Assign(ExWeakRef wref)
        {
            ExObjType t = Type;
            ExObjVal v = Value;

            Value._WeakRef = wref;
            Value._RefC = Value._WeakRef;
            Type = ExObjType.WEAKREF;

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
