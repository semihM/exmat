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
    /// <summary>
    /// Base object model
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExObject : IDisposable
    {
        /// <summary>
        /// Object type
        /// </summary>
        public ExObjType Type = ExObjType.NULL; // Veri tipi
        /// <summary>
        /// Basic values: integer, float, boolean, complex(imaginary factor)
        /// </summary>
        public ExObjVal Value;                  // Veri değeri
        /// <summary>
        /// Custom values
        /// </summary>
        public ExObjValCustom ValueCustom;

        private bool disposedValue;

        /// <summary>
        /// Get the integer value stored
        /// </summary>
        /// <returns>If <see cref="Type"/> is <see cref="ExObjType.INTEGER"/> -> returns <see cref="ExObjVal.i_Int"/> from <see cref="Value"/>
        /// <para>Otherwise -> returns <see cref="ExObjVal.f_Float"/> from <see cref="Value"/> as integer (unsafe limits)</para></returns>
        public long GetInt()
        {
            return Type == ExObjType.INTEGER ? Value.i_Int : (long)Value.f_Float;
        }

        /// <summary>
        /// Get the float value stored
        /// </summary>
        /// <returns>If <see cref="Type"/> is <see cref="ExObjType.FLOAT"/> -> returns <see cref="ExObjVal.f_Float"/> from <see cref="Value"/>
        /// <para>Otherwise -> returns <see cref="ExObjVal.i_Int"/> from <see cref="Value"/> as float</para></returns>
        public double GetFloat()
        {
            return Type == ExObjType.FLOAT ? Value.f_Float : Value.i_Int;
        }

        /// <summary>
        /// Get the complex value stored
        /// </summary>
        /// <returns>New <see cref="Complex"/> struct using <see cref="ExObjVal.f_Float"/> and <see cref="ExObjVal.c_Float"/> for real and imaginary parts respectively</returns>
        public Complex GetComplex()
        {
            return new(Value.f_Float, Value.c_Float);
        }
        /// <summary>
        /// Get the complex value stored conjugated
        /// </summary>
        /// <returns>New <see cref="Complex"/> struct using <see cref="ExObjVal.f_Float"/> and <c>-</c><see cref="ExObjVal.c_Float"/> for real and imaginary parts respectively</returns>
        public Complex GetComplexConj()
        {
            return new(Value.f_Float, -Value.c_Float);
        }

        /// <summary>
        /// Get the string represtation of the complex value stored, zero values not used unless both parts are zero => 0i
        /// </summary>
        /// <returns>String in format <c>{<see cref="ExObjVal.f_Float"/>} +/- {<see cref="ExObjVal.c_Float"/>}i</c></returns>
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

        /// <summary>
        /// Get string value stored, never returns <see langword="null"/>, uses <see cref="string.Empty"/> instead.
        /// </summary>
        /// <returns>Returns <see cref="ExObjValCustom.s_String"/></returns>
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

        /// <summary>
        /// Gets the weak reference object stored
        /// </summary>
        /// <returns>Returns <see cref="ExObjValCustom._WeakRef"/></returns>
        public ExWeakRef GetWeakRef()
        {
            return ValueCustom._WeakRef;
        }

        internal void SetString(string s)
        {
            ValueCustom.s_String = s;
        }

        /// <summary>
        /// Gets the boolean value of for any type
        /// </summary>
        /// <returns>Returns bool represtation of the current value stored for the <see cref="Type"/></returns>
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


        /// <summary>
        /// Gets the <see cref="Dictionary{TKey, TVal}"/> stored (&lt;<see cref="string"/>,<see cref="ExObject"/>&gt;)
        /// </summary>
        /// <returns>Returns <see cref="ExObjValCustom.d_Dict"/></returns>
        public Dictionary<string, ExObject> GetDict()
        {
            return ValueCustom.d_Dict;
        }

        /// <summary>
        /// Gets the <see cref="List{T}"/> stored (&lt;<see cref="ExObject"/>&gt;)
        /// </summary>
        /// <returns>Returns <see cref="ExObjValCustom.l_List"/></returns>
        public List<ExObject> GetList()
        {
            return ValueCustom.l_List;
        }

        /// <summary>
        /// Gets the closure stored
        /// </summary>
        /// <returns>Returns <see cref="ExObjValCustom._Closure"/></returns>
        public ExClosure GetClosure()
        {
            return ValueCustom._Closure;
        }

        /// <summary>
        /// Gets the native closure stored
        /// </summary>
        /// <returns>Returns <see cref="ExObjValCustom._NativeClosure"/></returns>
        public ExNativeClosure GetNClosure()
        {
            return ValueCustom._NativeClosure;
        }

        /// <summary>
        /// Gets the instance stored
        /// </summary>
        /// <returns>Returns <see cref="ExObjValCustom._Instance"/></returns>
        public ExInstance GetInstance()
        {
            return ValueCustom._Instance;
        }

        /// <summary>
        /// Gets the class stored
        /// </summary>
        /// <returns>Returns <see cref="ExObjValCustom._Class"/></returns>
        public ExClass.ExClass GetClass()
        {
            return ValueCustom._Class;
        }

        /// <summary>
        /// Gets the space object stored
        /// </summary>
        /// <returns>Returns <see cref="ExObjValCustom.c_Space"/></returns>
        public ExSpace GetSpace()
        {
            return ValueCustom.c_Space;
        }

#if DEBUG
        internal virtual string GetDebuggerDisplay()
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

        internal virtual void Dispose(bool disposing)
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

        /// <summary>
        /// Disposer
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Wheter this object is a field in an <see cref="ExClass.ExClass"/> object
        /// </summary>
        /// <returns></returns>
        public bool IsField()
        {
            return (GetInt() & (int)ExMemberFlag.FIELD) > 0;
        }
        /// <summary>
        /// Wheter this object is a method in an <see cref="ExClass.ExClass"/> object
        /// </summary>
        /// <returns></returns>
        public bool IsMethod()
        {
            return (GetInt() & (int)ExMemberFlag.METHOD) > 0;
        }

        /// <summary>
        /// Get the member index of this object in an <see cref="ExClass.ExClass"/> object
        /// </summary>
        /// <returns></returns>
        public int GetMemberID()
        {
            return (int)GetInt() & 0x00FFFFFF;
        }

        /// <summary>
        /// Increment reference count to this object, can be forced with <paramref name="forced"/> to skip type check, requires <paramref name="t"/> to be reference counting type
        /// </summary>
        /// <param name="t">Object type</param>
        /// <param name="v">Object value storing reference counter</param>
        /// <param name="forced">Wheter to skip type check</param>
        public static void AddReference(ExObjType t, ExObjValCustom v, bool forced = false)
        {
            if (forced || ExTypeCheck.DoesTypeCountRef(t))
            {
                v._RefC.ReferenceCount++;
            }
        }

        /// <summary>
        /// Decrement reference count to this object, if reference count hits <c>0</c>, object will be disposed
        /// </summary>
        public virtual void Release()
        {
            if (ExTypeCheck.DoesTypeCountRef(Type) && ((--ValueCustom._RefC.ReferenceCount) == 0))
            {
                Nullify();
            }
        }

        internal static void Release(ExObjType t, ExObjValCustom v)
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
        // BASIC
        ///////////////////////////////
        /// <summary>
        /// Null constructor
        /// </summary>
        public ExObject()
        {
            Type = ExObjType.NULL;
            Value = new();
            ValueCustom = new();
        }

        /// <summary>
        /// Copy and reference to given object
        /// </summary>
        /// <param name="other">Object to increment reference of</param>
        public ExObject(ExObject other)
        {
            Type = other.Type;
            Value = other.Value;
            ValueCustom = other.ValueCustom;
            AddReference(Type, ValueCustom);
        }

        ///////////////////////////////
        // SCALAR
        ///////////////////////////////
        /// <summary>
        /// Integer constructor
        /// </summary>
        /// <param name="i">Value to store</param>
        public ExObject(long i)
        {
            Type = ExObjType.INTEGER;
            Value.i_Int = i;
        }

        /// <summary>
        /// Float constructor
        /// </summary>
        /// <param name="f">Value to store</param>
        public ExObject(double f)
        {
            Type = ExObjType.FLOAT;
            Value.f_Float = f;
        }

        /// <summary>
        /// Bool constructor
        /// </summary>
        /// <param name="b">Value to store</param>
        public ExObject(bool b)
        {
            Type = ExObjType.BOOL;
            Value.b_Bool = b;
        }

        /// <summary>
        /// String constructor
        /// </summary>
        /// <param name="s">Value to store</param>
        public ExObject(string s)
        {
            Type = ExObjType.STRING;
            ValueCustom.s_String = s;
        }

        /// <summary>
        /// Complex number constructor
        /// </summary>
        /// <param name="cmplx">Value to store</param>
        public ExObject(Complex cmplx)
        {
            Type = ExObjType.COMPLEX;
            Value.f_Float = cmplx.Real;
            Value.c_Float = cmplx.Imaginary;
        }

        ///////////////////////////////
        // Ref counted
        ///////////////////////////////
        /// <summary>
        /// Dictionary constructor
        /// </summary>
        /// <param name="dict">Value to store</param>
        public ExObject(Dictionary<string, ExObject> dict)
        {
            Type = ExObjType.DICT;
            ValueCustom.d_Dict = dict;
            ValueCustom._RefC = new();
            AddReference(Type, ValueCustom, true);
        }

        /// <summary>
        /// List constructor
        /// </summary>
        /// <param name="lis">Value to store</param>
        public ExObject(List<ExObject> lis)
        {
            Type = ExObjType.ARRAY;
            ValueCustom.l_List = lis;
            ValueCustom._RefC = new();
            AddReference(Type, ValueCustom, true);
        }

        /// <summary>
        /// Instance constructor
        /// </summary>
        /// <param name="inst">Value to store</param>
        public ExObject(ExInstance inst)
        {
            Type = ExObjType.INSTANCE;
            ValueCustom._Instance = inst;
            ValueCustom._RefC = ValueCustom._Instance;
            AddReference(Type, ValueCustom, true);
        }

        /// <summary>
        /// Class constructor
        /// </summary>
        /// <param name="class">Value to store</param>
        public ExObject(ExClass.ExClass @class)
        {
            Type = ExObjType.CLASS;
            ValueCustom._Class = @class;
            ValueCustom._RefC = ValueCustom._Class;
            AddReference(Type, ValueCustom, true);
        }

        /// <summary>
        /// Closure constructor
        /// </summary>
        /// <param name="cls">Value to store</param>
        public ExObject(ExClosure cls)
        {
            Type = ExObjType.CLOSURE;
            ValueCustom._Closure = cls;
            ValueCustom._RefC = ValueCustom._Closure;
            AddReference(Type, ValueCustom, true);
        }

        /// <summary>
        /// Native closure constructor
        /// </summary>
        /// <param name="ncls">Value to store</param>
        public ExObject(ExNativeClosure ncls)
        {
            Type = ExObjType.NATIVECLOSURE;
            ValueCustom._NativeClosure = ncls;
            ValueCustom._RefC = ValueCustom._NativeClosure;
            AddReference(Type, ValueCustom, true);
        }

        /// <summary>
        /// Outer constructor
        /// </summary>
        /// <param name="outer">Value to store</param>
        public ExObject(ExOuter outer)
        {
            Type = ExObjType.OUTER;
            ValueCustom._Outer = outer;
            ValueCustom._RefC = ValueCustom._Outer;
            AddReference(Type, ValueCustom, true);
        }

        /// <summary>
        /// Weakreference constructor
        /// </summary>
        /// <param name="wref">Value to store</param>
        public ExObject(ExWeakRef wref)
        {
            Type = ExObjType.WEAKREF;
            ValueCustom._WeakRef = wref;
            ValueCustom._RefC = ValueCustom._WeakRef;
            AddReference(Type, ValueCustom, true);
        }

        /// <summary>
        /// Prototype constructor
        /// </summary>
        /// <param name="pro">Value to store</param>
        public ExObject(ExPrototype pro)
        {
            Type = ExObjType.FUNCPRO;
            ValueCustom._FuncPro = pro;
            ValueCustom._RefC = ValueCustom._FuncPro;
            AddReference(Type, ValueCustom, true);
        }

        /// <summary>
        /// Space constructor
        /// </summary>
        /// <param name="space">Value to store</param>
        public ExObject(ExSpace space)
        {
            Type = ExObjType.SPACE;
            ValueCustom.c_Space = space;
            ValueCustom._RefC = ValueCustom.c_Space;
            AddReference(Type, ValueCustom, true);
        }

        /// <summary>
        /// Reference to another object, deref current values stored
        /// </summary>
        /// <param name="other">Other object</param>
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

        /// <summary>
        /// Assing integer, deref current values stored
        /// </summary>
        /// <param name="i">New value</param>
        public void Assign(long i)
        {
            Release(Type, ValueCustom);
            Value.i_Int = i;
            Type = ExObjType.INTEGER;
        }
        /// <summary>
        /// Assing float, deref current values stored
        /// </summary>
        /// <param name="f">New value</param>
        public void Assign(double f)
        {
            Release(Type, ValueCustom);
            Value.f_Float = f;
            Type = ExObjType.FLOAT;
        }
        /// <summary>
        /// Assing bool, deref current values stored
        /// </summary>
        /// <param name="b">New value</param>
        public void Assign(bool b)
        {
            Release(Type, ValueCustom);
            Value.b_Bool = b;
            Type = ExObjType.BOOL;
        }
        /// <summary>
        /// Assing string, deref current values stored
        /// </summary>
        /// <param name="s">New value</param>
        public void Assign(string s)
        {
            Release(Type, ValueCustom);
            ValueCustom.s_String = s;
            Type = ExObjType.STRING;
        }
        /// <summary>
        /// Assing complex number, deref current values stored
        /// </summary>
        /// <param name="cmplx">New value</param>
        public void Assign(Complex cmplx)
        {
            Release(Type, ValueCustom);
            Value.f_Float = cmplx.Real;
            Value.c_Float = cmplx.Imaginary;
            Type = ExObjType.COMPLEX;
        }
        /// <summary>
        /// Assing space, deref current values stored
        /// </summary>
        /// <param name="space">New value</param>
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
        /// <summary>
        /// Assing dictionary, deref current values stored
        /// </summary>
        /// <param name="dict">New value</param>
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
        /// <summary>
        /// Assing list, deref current values stored
        /// </summary>
        /// <param name="lis">New value</param>
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
        /// <summary>
        /// Assing instance, deref current values stored
        /// </summary>
        /// <param name="inst">New value</param>
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
        /// <summary>
        /// Assing class, deref current values stored
        /// </summary>
        /// <param name="class">New value</param>
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
        /// <summary>
        /// Assing closure, deref current values stored
        /// </summary>
        /// <param name="cls">New value</param>
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
        /// <summary>
        /// Assing native closure, deref current values stored
        /// </summary>
        /// <param name="ncls">New value</param>
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
        /// <summary>
        /// Assing outer variable, deref current values stored
        /// </summary>
        /// <param name="outer">New value</param>
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
        /// <summary>
        /// Assing prototype, deref current values stored
        /// </summary>
        /// <param name="pro">New value</param>
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
        /// <summary>
        /// Assing weak reference, deref current values stored
        /// </summary>
        /// <param name="wref">New value</param>
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

        /// <summary>
        /// Nullify the object, deref current value
        /// </summary>
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
