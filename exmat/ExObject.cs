using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.Globalization;
using System.Numerics;
using ExMat.Closure;
using ExMat.ExClass;
using ExMat.FuncPrototype;
using ExMat.Outer;

namespace ExMat.Objects
{
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExObject : IDisposable
    {
        public ExObjType Type = ExObjType.NULL; // Veri tipi
        public ExObjVal Value;                  // Veri değeri
        public ExObjValCustom ValueCustom;

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
                        return ValueCustom.s_String ?? string.Empty;
                    }
                default:
                    {
                        return string.Empty;
                    }
            }
        }

        public ExWeakRef GetWeakRef()
        {
            return ValueCustom._WeakRef;
        }

        public void SetString(string s)
        {
            ValueCustom.s_String = s;
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
            return ValueCustom.d_Dict;
        }

        public List<ExObject> GetList()
        {
            return ValueCustom.l_List;
        }

        public ExClosure GetClosure()
        {
            return ValueCustom._Closure;
        }

        public ExNativeClosure GetNClosure()
        {
            return ValueCustom._NativeClosure;
        }

        public ExInstance GetInstance()
        {
            return ValueCustom._Instance;
        }

        public ExClass.ExClass GetClass()
        {
            return ValueCustom._Class;
        }

        public ExSpace GetSpace()
        {
            return ValueCustom.c_Space;
        }

#if DEBUG
        public virtual string GetDebuggerDisplay()
        {
            string s = Type.ToString();
            switch (Type)
            {
                case ExObjType.ARRAY: s += GetList() == null ? " null" : "(" + GetList().Count.ToString(CultureInfo.CurrentCulture) + ")"; break;
                case ExObjType.INTEGER: s += " " + GetInt(); break;
                case ExObjType.FLOAT: s += " " + GetFloat(); break;
                case ExObjType.COMPLEX: s += " " + GetComplex().ToString(CultureInfo.CurrentCulture); break;
                case ExObjType.BOOL: s += GetBool() ? " true" : " false"; break;
                case ExObjType.STRING: s += " " + GetString(); break;
                case ExObjType.CLOSURE: s = GetClosure() == null ? s + GetString() : GetClosure().GetDebuggerDisplay(); break;
                case ExObjType.NATIVECLOSURE: s = GetNClosure() == null ? s + GetString() : GetNClosure().GetDebuggerDisplay(); break;
                case ExObjType.NULL: break;
            }
            return s;
        }
#endif

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Release(Type, ValueCustom);
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

        public static void AddReference(ExObjType t, ExObjValCustom v, bool forced = false)
        {
            if (forced || ExTypeCheck.DoesTypeCountRef(t))
            {
                v._RefC.ReferenceCount++;
            }
        }

        public virtual void Release()
        {
            if (ExTypeCheck.DoesTypeCountRef(Type) && ((--ValueCustom._RefC.ReferenceCount) == 0))
            {
                Nullify();
            }
        }
        public static void Release(ExObjType t, ExObjValCustom v)
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
            ValueCustom = new();
        }

        public ExObject(ExObject other)
        {
            Type = other.Type;
            Value = other.Value;
            ValueCustom = other.ValueCustom;
            AddReference(Type, ValueCustom);
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
            ValueCustom.s_String = s;
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
            ValueCustom.d_Dict = dict;
            ValueCustom._RefC = new();
            AddReference(Type, ValueCustom, true);
        }

        public ExObject(List<ExObject> lis)
        {
            Type = ExObjType.ARRAY;
            ValueCustom.l_List = lis;
            ValueCustom._RefC = new();
            AddReference(Type, ValueCustom, true);
        }

        public ExObject(ExInstance inst)
        {
            Type = ExObjType.INSTANCE;
            ValueCustom._Instance = inst;
            ValueCustom._RefC = ValueCustom._Instance;
            AddReference(Type, ValueCustom, true);
        }

        public ExObject(ExClass.ExClass @class)
        {
            Type = ExObjType.CLASS;
            ValueCustom._Class = @class;
            ValueCustom._RefC = ValueCustom._Class;
            AddReference(Type, ValueCustom, true);
        }

        public ExObject(ExClosure cls)
        {
            Type = ExObjType.CLOSURE;
            ValueCustom._Closure = cls;
            ValueCustom._RefC = ValueCustom._Closure;
            AddReference(Type, ValueCustom, true);
        }

        public ExObject(ExNativeClosure ncls)
        {
            Type = ExObjType.NATIVECLOSURE;
            ValueCustom._NativeClosure = ncls;
            ValueCustom._RefC = ValueCustom._NativeClosure;
            AddReference(Type, ValueCustom, true);
        }

        public ExObject(ExOuter outer)
        {
            Type = ExObjType.OUTER;
            ValueCustom._Outer = outer;
            ValueCustom._RefC = ValueCustom._Outer;
            AddReference(Type, ValueCustom, true);
        }

        public ExObject(ExWeakRef wref)
        {
            Type = ExObjType.WEAKREF;
            ValueCustom._WeakRef = wref;
            ValueCustom._RefC = ValueCustom._WeakRef;
            AddReference(Type, ValueCustom, true);
        }

        public ExObject(ExPrototype pro)
        {
            Type = ExObjType.FUNCPRO;
            ValueCustom._FuncPro = pro;
            ValueCustom._RefC = ValueCustom._FuncPro;
            AddReference(Type, ValueCustom, true);
        }

        public ExObject(ExSpace space)
        {
            Type = ExObjType.SPACE;
            ValueCustom.c_Space = space;
            ValueCustom._RefC = ValueCustom.c_Space;
            AddReference(Type, ValueCustom, true);
        }

        // Assignement
        public void Assign(ExObject other)
        {
            ExObjType t = Type;
            ExObjValCustom v = ValueCustom;

            Value = other.Value;
            ValueCustom = other.ValueCustom;
            Type = other.Type;

            AddReference(Type, ValueCustom);
            Release(t, v);
        }
        public void Assign(long i)
        {
            Release(Type, ValueCustom);
            Value.i_Int = i;
            Type = ExObjType.INTEGER;
        }
        public void Assign(double f)
        {
            Release(Type, ValueCustom);
            Value.f_Float = f;
            Type = ExObjType.FLOAT;
        }
        public void Assign(bool b)
        {
            Release(Type, ValueCustom);
            Value.b_Bool = b;
            Type = ExObjType.BOOL;
        }
        public void Assign(string s)
        {
            Release(Type, ValueCustom);
            ValueCustom.s_String = s;
            Type = ExObjType.STRING;
        }
        public void Assign(Complex cmplx)
        {
            Release(Type, ValueCustom);
            Value.f_Float = cmplx.Real;
            Value.c_Float = cmplx.Imaginary;
            Type = ExObjType.COMPLEX;
        }
        public void Assign(ExSpace space)
        {
            ExObjType t = Type;
            ExObjValCustom v = ValueCustom;

            ValueCustom.c_Space = space;
            Type = ExObjType.SPACE;
            ValueCustom._RefC = ValueCustom.c_Space;

            AddReference(Type, ValueCustom, true);
            Release(t, v);
        }
        public void Assign(Dictionary<string, ExObject> dict)
        {
            ExObjType t = Type;
            ExObjValCustom v = ValueCustom;

            ValueCustom.d_Dict = dict;
            ValueCustom._RefC = new();
            Type = ExObjType.DICT;

            AddReference(Type, ValueCustom, true);
            Release(t, v);
        }
        public void Assign(List<ExObject> lis)
        {
            ExObjType t = Type;
            ExObjValCustom v = ValueCustom;

            ValueCustom.l_List = lis;
            ValueCustom._RefC = new();
            Type = ExObjType.ARRAY;

            AddReference(Type, ValueCustom, true);
            Release(t, v);
        }
        public void Assign(ExInstance inst)
        {
            ExObjType t = Type;
            ExObjValCustom v = ValueCustom;

            ValueCustom._Instance = inst;
            ValueCustom._RefC = ValueCustom._Instance;
            Type = ExObjType.INSTANCE;

            AddReference(Type, ValueCustom, true);
            Release(t, v);
        }
        public void Assign(ExClass.ExClass @class)
        {
            ExObjType t = Type;
            ExObjValCustom v = ValueCustom;

            ValueCustom._Class = @class;
            ValueCustom._RefC = ValueCustom._Class;
            Type = ExObjType.CLASS;

            AddReference(Type, ValueCustom, true);
            Release(t, v);
        }
        public void Assign(ExClosure cls)
        {
            ExObjType t = Type;
            ExObjValCustom v = ValueCustom;

            ValueCustom._Closure = cls;
            ValueCustom._RefC = ValueCustom._Closure;
            Type = ExObjType.CLOSURE;

            AddReference(Type, ValueCustom, true);
            Release(t, v);
        }
        public void Assign(ExNativeClosure ncls)
        {
            ExObjType t = Type;
            ExObjValCustom v = ValueCustom;

            ValueCustom._NativeClosure = ncls;
            ValueCustom._RefC = ValueCustom._NativeClosure;
            Type = ExObjType.NATIVECLOSURE;

            AddReference(Type, ValueCustom, true);
            Release(t, v);
        }
        public void Assign(ExOuter outer)
        {
            ExObjType t = Type;
            ExObjValCustom v = ValueCustom;

            ValueCustom._Outer = outer;
            ValueCustom._RefC = ValueCustom._Outer;
            Type = ExObjType.OUTER;

            AddReference(Type, ValueCustom, true);
            Release(t, v);
        }
        public void Assign(ExPrototype pro)
        {
            ExObjType t = Type;
            ExObjValCustom v = ValueCustom;

            ValueCustom._FuncPro = pro;
            ValueCustom._RefC = ValueCustom._FuncPro;
            Type = ExObjType.FUNCPRO;

            AddReference(Type, ValueCustom, true);
            Release(t, v);
        }
        public void Assign(ExWeakRef wref)
        {
            ExObjType t = Type;
            ExObjValCustom v = ValueCustom;

            ValueCustom._WeakRef = wref;
            ValueCustom._RefC = ValueCustom._WeakRef;
            Type = ExObjType.WEAKREF;

            AddReference(Type, ValueCustom, true);
            Release(t, v);
        }

        //
        public void Nullify()
        {
            ExObjType oldt = Type;
            ExObjValCustom oldv = ValueCustom;
            Type = ExObjType.NULL;
            ValueCustom = new();
            Release(oldt, oldv);
        }
    }
}
