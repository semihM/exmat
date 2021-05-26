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
        public ExObjType _type = ExObjType.NULL;

        public ExObjVal _val;

        private bool disposedValue;

        public bool IsDelegable()
        {
            return ((int)_type & (int)ExObjFlag.DELEGABLE) != 0;
        }

        public bool IsNumeric()
        {
            return ((int)_type & (int)ExObjFlag.NUMERIC) != 0;
        }

        public long GetInt()
        {
            return _type == ExObjType.INTEGER ? _val.i_Int : (long)_val.f_Float;
        }

        public double GetFloat()
        {
            return _type == ExObjType.FLOAT ? _val.f_Float : _val.i_Int;
        }

        public Complex GetComplex()
        {
            return new(_val.f_Float, _val.c_Float);
        }
        public Complex GetComplexConj()
        {
            return new(_val.f_Float, -_val.c_Float);
        }

        public string GetComplexString()
        {
            if (_val.f_Float == 0.0)
            {
                return _val.c_Float + "i";
            }
            else if (_val.c_Float > 0)
            {
                return _val.f_Float + "+" + _val.c_Float + "i";
            }
            else if (_val.c_Float == 0.0)
            {
                return _val.f_Float.ToString();
            }
            else
            {
                return _val.f_Float + _val.c_Float.ToString() + "i";
            }
        }

        public string GetString()
        {
            return _type == ExObjType.STRING ? _val.s_String : (_type == ExObjType.NULL && _val.s_String != null ? _val.s_String : string.Empty);
        }

        public void SetString(string s)
        {
            _val.s_String = s;
        }

        public bool IsFalseable()
        {
            return ((int)_type & (int)ExObjFlag.CANBEFALSE) != 0;
        }
        public bool GetBool()
        {
            if (IsFalseable())
            {
                switch (_type)
                {
                    case ExObjType.BOOL:
                        return _val.b_Bool;
                    case ExObjType.NULL:
                        return false;
                    case ExObjType.INTEGER:
                        return _val.i_Int != 0;
                    case ExObjType.FLOAT:
                        return _val.f_Float != 0;
                    default:
                        return true;
                }
            }
            return true;
        }

        public Dictionary<string, ExObject> GetDict()
        {
            return _type == ExObjType.DICT ? _val.d_Dict : null;
        }

        public List<ExObject> GetList()
        {
            return _val.l_List;
        }

        public ExClosure GetClosure()
        {
            return _val._Closure;
        }

        public ExNativeClosure GetNClosure()
        {
            return _val._NativeClosure;
        }

        public ExInstance GetInstance()
        {
            return _val._Instance;
        }

        public virtual string GetDebuggerDisplay()
        {
            string s = _type.ToString();
            switch (_type)
            {
                case ExObjType.ARRAY: s += _val.l_List == null ? " null" : "(" + _val.l_List.Count.ToString() + ")"; break;
                case ExObjType.INTEGER: s += " " + GetInt(); break;
                case ExObjType.FLOAT: s += " " + GetFloat(); break;
                case ExObjType.BOOL: s += GetBool() ? " true" : " false"; break;
                case ExObjType.STRING: s += " " + GetString(); break;
                case ExObjType.CLOSURE: s = (GetClosure() == null ? s + GetString() : _val._Closure.GetDebuggerDisplay()); break;
                case ExObjType.NATIVECLOSURE: s = (GetNClosure() == null ? s + GetString() : _val._NativeClosure.GetDebuggerDisplay()); break;
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
                    _val.i_Int = 0;
                    _val.f_Float = 0;
                    _val.c_Float = 0;
                    _val.s_String = null;

                    _val._RefC = null;
                    _val.c_Space = null;
                    Disposer.DisposeList(ref _val.l_List);
                    Disposer.DisposeDict(ref _val.d_Dict);
                    _val._Method = null;
                    _val._Closure = null;
                    _val._Outer = null;
                    _val._NativeClosure = null;
                    _val._FuncPro = null;
                    _val._Class = null;
                    _val._Instance = null;
                    _val._WeakRef = null;
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

            v._RefC._refc++;
        }
        public virtual void Release()
        {
            if (IsRefC(_type) && ((--_val._RefC._refc) == 0))
            {
                Nullify();
            }
        }
        public static void Release(ExObjType t, ExObjVal v)
        {
            if (IsRefC(t) && v._RefC != null && (--v._RefC._refc) == 0)
            {
                v.i_Int = 0;
            }
        }

        ///////////////////////////////
        /// BASIC
        ///////////////////////////////
        public ExObject()
        {
            _type = ExObjType.NULL;
            _val = new();
        }
        public ExObject(ExObject objp)
        {
            _type = objp._type;
            _val = objp._val;
            AddReference(_type, _val);
        }
        public void Assign(ExObject o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val = o._val;
            _type = o._type;

            AddReference(_type, _val);
            Release(t, v);
        }

        ///////////////////////////////
        /// SCALAR
        ///////////////////////////////
        public ExObject(long i)
        {
            _type = ExObjType.INTEGER;
            _val.i_Int = i;
        }
        public void Assign(long i)
        {
            Release(_type, _val);
            _val.i_Int = i;
            _type = ExObjType.INTEGER;
        }
        public ExObject(double f)
        {
            _type = ExObjType.FLOAT;
            _val.f_Float = f;
        }
        public void Assign(double f)
        {
            Release(_type, _val);
            _val.f_Float = f;
            _type = ExObjType.FLOAT;
        }
        public ExObject(bool b)
        {
            _type = ExObjType.BOOL;
            _val.b_Bool = b;
        }
        public void Assign(bool b)
        {
            Release(_type, _val);
            _val.b_Bool = b;
            _type = ExObjType.BOOL;
        }
        public ExObject(string s)
        {
            _type = ExObjType.STRING;
            _val.s_String = s;
        }
        public void Assign(string s)
        {
            Release(_type, _val);
            _val.s_String = s;
            _type = ExObjType.STRING;
        }
        public ExObject(Complex f)
        {
            _type = ExObjType.COMPLEX;
            _val.f_Float = f.Real;
            _val.c_Float = f.Imaginary;
        }
        public void Assign(Complex f)
        {
            Release(_type, _val);
            _val.f_Float = f.Real;
            _val.c_Float = f.Imaginary;
            _type = ExObjType.COMPLEX;
        }
        ///////////////////////////////
        /// EXREF
        ///////////////////////////////
        public ExObject(Dictionary<string, ExObject> dict)
        {
            _type = ExObjType.DICT;
            _val._RefC = new();
            _val.d_Dict = dict;
        }
        public void Assign(Dictionary<string, ExObject> dict)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val.d_Dict = dict;
            _val._RefC = new();
            _type = ExObjType.DICT;

            AddReference(_type, _val, true);
            Release(t, v);
        }
        public ExObject(List<ExObject> o)
        {
            _type = ExObjType.ARRAY;
            _val.l_List = o;
            _val._RefC = new();
            AddReference(_type, _val, true);
        }
        public void Assign(List<ExObject> o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val.l_List = o;
            _val._RefC = new();
            _type = ExObjType.ARRAY;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObject(ExInstance o)
        {
            _type = ExObjType.INSTANCE;
            _val._Instance = o;
            _val._RefC = new();
            AddReference(_type, _val, true);
        }
        public void Assign(ExInstance o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val._Instance = o;
            _val._RefC = new() { _refc = _val._Instance._val._RefC._refc++ };
            _type = ExObjType.INSTANCE;

            AddReference(_type, _val, true);
            Release(t, v);
        }
        public ExObject(ExList o)
        {
            _type = ExObjType.ARRAY;
            _val.l_List = o._val.l_List;
            _val._RefC = new();
            AddReference(_type, _val, true);
        }
        public void Assign(ExList o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val.l_List = o._val.l_List;
            _val._RefC = new() { _refc = o._val._RefC._refc++ };
            _type = ExObjType.ARRAY;

            AddReference(_type, _val, true);
            Release(t, v);
        }
        public ExObject(ExClass o)
        {
            _type = ExObjType.CLASS;
            _val._Class = o;
            _val._RefC = new();
            AddReference(_type, _val, true);
        }
        public void Assign(ExClass o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val._Class = o;
            _val._RefC = new() { _refc = _val._Class._val._RefC._refc++ };
            _type = ExObjType.CLASS;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObject(ExClosure o)
        {
            _type = ExObjType.CLOSURE;
            _val._Closure = o;
            _val._RefC = new();
            AddReference(_type, _val, true);
        }
        public void Assign(ExClosure o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val._Closure = o;
            _val._RefC = new() { _refc = _val._Closure._val._RefC._refc++ };
            _type = ExObjType.CLOSURE;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObject(ExNativeClosure o)
        {
            _type = ExObjType.NATIVECLOSURE;
            _val._NativeClosure = o;
            _val._RefC = new();
            AddReference(_type, _val, true);
        }
        public void Assign(ExNativeClosure o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val._NativeClosure = o;
            _val._RefC = new() { _refc = _val._NativeClosure._val._RefC._refc++ };
            _type = ExObjType.NATIVECLOSURE;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObject(ExOuter o)
        {
            _type = ExObjType.OUTER;
            _val._Outer = o;
            AddReference(_type, _val, true);
        }
        public void Assign(ExOuter o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val._Outer = o;
            _val._RefC = new() { _refc = _val._Outer._val._RefC._refc++ };
            _type = ExObjType.OUTER;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObject(ExWeakRef o)
        {
            _type = ExObjType.WEAKREF;
            _val._WeakRef = o;
            AddReference(_type, _val, true);
        }
        public void Assign(ExWeakRef o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val._WeakRef = o;
            _val._RefC = new() { _refc = _val._WeakRef._val._RefC._refc++ };
            _type = ExObjType.WEAKREF;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObject(ExFuncPro o)
        {
            _type = ExObjType.FUNCPRO;
            _val._FuncPro = o;
            AddReference(_type, _val, true);
        }
        public void Assign(ExFuncPro o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val._FuncPro = o;
            _val._RefC = new() { _refc = _val._FuncPro._val._RefC._refc++ };
            _type = ExObjType.FUNCPRO;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObject(ExSpace o)
        {
            _type = ExObjType.SPACE;
            _val.c_Space = o;
            _val._RefC = new();

            AddReference(_type, _val, true);
        }
        public void Assign(ExSpace o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val.c_Space = o;
            _type = ExObjType.SPACE;
            _val._RefC = new();

            AddReference(_type, _val, true);
            Release(t, v);
        }
        //
        public void Nullify()
        {
            ExObjType oldt = _type;
            ExObjVal oldv = _val;
            _type = ExObjType.NULL;
            _val = new();
            Release(oldt, oldv);
        }

    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExRefC : IDisposable
    {
        public int _refc = 0;
        public ExWeakRef _weakref;
        private bool disposedValue;

        public ExRefC()
        {
            _refc = 0;
            _weakref = null;
        }

        public ExWeakRef GetWeakRef(ExObjType t)
        {
            if (_weakref != null)
            {
                ExWeakRef e = new();
                e.obj._type = t;
                e.obj._val._RefC = this;
                _weakref = e;
            }
            return _weakref;
        }

        public string GetDebuggerDisplay()
        {
            return "REFC: " + _refc;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _weakref = null;
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

    public class ExWeakRef : ExObject
    {
        public ExObject obj;

        public ExWeakRef()
        {
            _type = ExObjType.WEAKREF;
            _val._RefC = new();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            obj.Dispose();
        }

        public override void Release()
        {
            if (((int)obj._type & (int)ExObjFlag.COUNTREFERENCES) == (int)obj._type)
            {
                obj._val._WeakRef = null;
            }
        }
    }

    public class ExList : ExObject
    {
        public ExList()
        {
            _type = ExObjType.ARRAY;
            _val.l_List = new();
            _val._RefC = new();
        }

        public ExList(bool n)
        {
            _type = ExObjType.ARRAY;
            _val.l_List = n ? new() : null;
            _val._RefC = new();
        }
        public ExList(char c)
        {
            _type = ExObjType.ARRAY;
            _val.l_List = c != '0' ? new() : null;
            _val._RefC = new();
        }
        public ExList(ExList e)
        {
            _type = ExObjType.ARRAY;
            _val.l_List = e._val.l_List;
            _val._RefC = new();
        }
        public ExList(List<ExObject> e)
        {
            _type = ExObjType.ARRAY;
            _val.l_List = e;
            _val._RefC = new();
        }

        public ExList(List<string> e)
        {
            _type = ExObjType.ARRAY;

            _val.l_List = new(e.Count);

            _val._RefC = new();

            foreach (string s in e)
            {
                _val.l_List.Add(new(s));
            }
        }
    }

    public class ExRegFunc : IDisposable
    {
        public string name;
        public ExFunc func;
        public int n_pchecks;
        public Dictionary<int, ExObject> d_defaults = new();
        public bool b_isdeleg = false;
        public string mask;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    mask = null;
                    name = null;
                    n_pchecks = 0;
                    func.Dispose();
                    Disposer.DisposeDict(ref d_defaults);
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
        public List<ExObject> _values;

        public int _size;
        public int _alloc;
        private bool disposedValue;

        public string GetDebuggerDisplay()
        {
            return "STACK(" + _alloc + "): " + _size + (_values == null ? " null" : " " + _values.Count);
        }

        public ExStack() { _size = 0; _alloc = 0; _values = new(); }

        public ExStack(int size)
        {
            _values = new(size);
            _size = 0;
            _alloc = size;
        }
        public ExStack(ExStack stc)
        {
            CopyFrom(stc);
        }

        public void Release()
        {
            if (_alloc > 0)
            {
                for (int i = 0; i < _size; i++)
                {
                    _values[i].Release();
                    _values[i] = null;
                }
                _values = null;
            }
        }

        public void Resize(int n, ExObject filler = null)
        {
            if (n > _alloc)
            {
                ReAlloc(n);
            }

            if (n > _size)
            {
                while (_size < n)
                {
                    _values[_size] = new(filler);
                    _size++;
                }
            }
            else
            {
                for (int i = n; i < _size; i++)
                {
                    _values[i].Release();
                    _values[i] = null;
                }
                _size = n;
            }
        }

        public void ReAlloc(int n)
        {
            n = n > 0 ? n : 4;
            if (_values == null)
            {
                _values = new(n);
                for (int i = 0; i < n; i++)
                {
                    _values.Add(new());
                }
            }
            else
            {
                int dif = n - _alloc;
                if (dif < 0)
                {
                    for (int i = 0; i < -dif; i++)
                    {
                        _values[_alloc - i - 1].Release();
                        _values.RemoveAt(_alloc - i - 1);
                    }
                }
                else
                {
                    for (int i = 0; i < dif; i++)
                    {
                        _values.Add(new());
                    }
                }
            }
            _alloc = n;
        }

        public void CopyFrom(ExStack stc)
        {
            if (_size > 0)
            {
                Resize(0);
            }
            if (stc._size > _alloc)
            {
                ReAlloc(stc._size);
            }

            for (int i = 0; i < stc._size; i++)
            {
                _values[i] = new(stc._values[i]);
            }
            _size = stc._size;
        }

        public ExObject Back()
        {
            return _values[_size - 1];
        }

        public ExObject Push(ExObject o)
        {
            if (_alloc <= _size)
            {
                ReAlloc(_size * 2);
            }
            return _values[_size++] = new(o);
        }

        public void Pop()
        {
            _size--;
            _values[_size].Release();
            _values[_size] = new(); // = null
        }

        public void Insert(int i, ExObject o)
        {
            //_values.Insert(i, o);
            Resize(_size + 1);
            for (int j = _size; j > i; j--)
            {
                _values[j] = _values[j - 1];
            }
            _values[i].Assign(o);
        }

        public void Remove(int i)
        {
            _values[i].Release();
            _values.RemoveAt(i);
            _size--;
        }


        public ExObject this[int n]
        {
            get => _values[n];
            set => _values[n].Assign(value);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disposer.DisposeList(ref _values);
                    _alloc = 0;
                    _size = 0;
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
        public int dim = 1;
        public string space = string.Empty;
        public char sign = '\\';
        public ExSpace _parent;
        private bool disposedValue;

        public string GetString()
        {
            return "@" + space + "@" + sign + "@" + dim;
        }

        public static ExSpace GetSpaceFromString(string s)
        {
            ExSpace spc = new();
            string[] arr = s.Split("@", StringSplitOptions.RemoveEmptyEntries);
            spc.space = arr[0];
            spc.sign = arr[1][0];
            spc.dim = int.Parse(arr[2]);
            return spc;
        }
        public ExSpace() { }

        public virtual string GetDebuggerDisplay()
        {
            return "SPACE(" + dim + ", " + space + ", " + sign + ")";
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    space = null;
                    dim = 0;
                    _parent.Dispose();
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
