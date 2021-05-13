using System.Collections.Generic;
using System.Diagnostics;
using ExMat.Class;
using ExMat.Closure;
using ExMat.FuncPrototype;
using ExMat.States;
using ExMat.VM;

namespace ExMat.Objects
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExObject
    {
        public ExObjType _type = ExObjType.NULL;

        public ExObjVal _val;

        public ExObject() { }
        public ExObject(int i)
        {
            _type = ExObjType.INTEGER;
            _val.i_Int = i;
        }
        public ExObject(float f)
        {
            _type = ExObjType.FLOAT;
            _val.f_Float = f;
        }
        public ExObject(bool b)
        {
            _type = ExObjType.BOOL;
            _val.b_Bool = b;
        }
        public ExObject(string s)
        {
            _type = ExObjType.STRING;
            _val.s_String = s;
        }

        public bool IsNumeric()
        {
            return ((int)_type & (int)ExObjFlag.NUMERIC) != 0;
        }

        public int GetInt()
        {
            return _type == ExObjType.INTEGER ? _val.i_Int : (int)_val.f_Float;
        }

        public float GetFloat()
        {
            return _type == ExObjType.FLOAT ? _val.f_Float : _val.i_Int;
        }

        public string GetString()
        {
            return _type == ExObjType.STRING ? _val.s_String : (_type == ExObjType.NULL && _val.s_String != null ? _val.s_String : string.Empty);
        }

        public void SetString(string s)
        {
            _val.s_String = s;
        }

        public bool GetBool()
        {
            if (((int)_type & (int)ExObjFlag.BOOLFALSEABLE) != 0)
            {
                switch (_type)
                {
                    case ExObjType.BOOL:
                        return _val.b_Bool;
                    case ExObjType.NULL:
                        return false;
                    case ExObjType.INTEGER:
                        return _val.i_Int == 1;
                    case ExObjType.FLOAT:
                        return _val.f_Float == 1;
                    default:
                        return true;
                }
            }
            return true;
        }

        public Dictionary<string, ExObjectPtr> GetDict()
        {
            return _type == ExObjType.DICT ? _val.d_Dict : null;
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
                case ExObjType.NULL: break;
            }
            return s;
        }
    }

    public enum ExMemberFlag
    {
        METHOD = 0x01000000,
        FIELD = 0x02000000
    }

    public class ExObjectPtr : ExObject
    {
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
            return GetInt() & 0x00FFFFFF;
        }

        public static bool IsRefC(ExObjType t)
        {
            return ((int)t & (int)ExObjFlag.REF_COUNTED) > 0;
        }
        public virtual void AddReference(ExObjType t, ExObjVal v, bool forced = false)
        {
            if (!IsRefC(t))
            {
                return;
            }

            if (v._RefC == null)
            {
                v._RefC = new();
            }

            if (forced || IsRefC(t))
            {
                v._RefC._refc++;
            }
        }
        public virtual void Release()
        {
            if (IsRefC(_type) && ((--_val._RefC._refc) == 0))
            {
                Nullify();
            }
        }
        public void Release(ExObjType t, ExObjVal v)
        {
            if (IsRefC(t) && v._RefC != null && (--v._RefC._refc) == 0)
            {
                _val.i_Int = 0;
                _type = ExObjType.NULL;
            }
        }

        ///////////////////////////////
        /// BASIC
        ///////////////////////////////
        public ExObjectPtr()
        {
            _type = ExObjType.NULL;
            _val = new();
        }
        public ExObjectPtr(ExObjectPtr objp)
        {
            _type = objp._type;
            _val = objp._val;
            AddReference(_type, _val);
        }
        public void Assign(ExObjectPtr o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val = o._val;
            _type = o._type;

            AddReference(_type, _val);
            Release(t, v);
        }

        public ExObjectPtr(ExObject objp)
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
        public ExObjectPtr(int i)
        {
            _type = ExObjType.INTEGER;
            _val.i_Int = i;
        }
        public void Assign(int i)
        {
            Release(_type, _val);
            _val.i_Int = i;
            _type = ExObjType.INTEGER;
        }
        public ExObjectPtr(float f)
        {
            _type = ExObjType.FLOAT;
            _val.f_Float = f;
        }
        public void Assign(float f)
        {
            Release(_type, _val);
            _val.f_Float = f;
            _type = ExObjType.FLOAT;
        }
        public ExObjectPtr(bool b)
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
        public ExObjectPtr(string s)
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


        ///////////////////////////////
        /// EX
        ///////////////////////////////
        public ExObjectPtr(ExInt o)
        {
            _type = ExObjType.INTEGER;
            _val.i_Int = o._val.i_Int;
        }
        public void Assign(ExInt o)
        {
            Release(_type, _val);
            _val.i_Int = o._val.i_Int;
            _type = ExObjType.INTEGER;
        }
        public ExObjectPtr(ExFloat o)
        {
            Release(_type, _val);
            _type = ExObjType.FLOAT;
            _val.f_Float = o._val.f_Float;
        }
        public void Assign(ExFloat o)
        {
            Release(_type, _val);
            _val.f_Float = o._val.f_Float;
            _type = ExObjType.FLOAT;
        }
        public ExObjectPtr(ExString o)
        {
            Release(_type, _val);
            _type = ExObjType.STRING;
            _val.s_String = o._val.s_String;
        }
        public void Assign(ExString o)
        {
            Release(_type, _val);
            _val.s_String = o._val.s_String;
            _type = ExObjType.STRING;
        }
        public ExObjectPtr(ExBool o)
        {
            Release(_type, _val);
            _type = ExObjType.BOOL;
            _val.b_Bool = o._val.b_Bool;
        }
        public void Assign(ExBool o)
        {
            Release(_type, _val);
            _val.b_Bool = o._val.b_Bool;
            _type = ExObjType.BOOL;
        }
        public ExObjectPtr(ExUserP o)
        {
            Release(_type, _val);
            _type = ExObjType.USERPTR;
            _val._UserPointer = o._val._UserPointer;
        }
        public void Assign(ExUserP o)
        {
            Release(_type, _val);
            _val._UserPointer = o._val._UserPointer;
            _type = ExObjType.USERPTR;
        }

        ///////////////////////////////
        /// EXREF
        ///////////////////////////////
        public ExObjectPtr(Dictionary<string, ExObjectPtr> dict)
        {
            _type = ExObjType.DICT;
            _val._RefC = new();
            _val.d_Dict = dict;
        }
        public void Assign(Dictionary<string, ExObjectPtr> dict)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val.d_Dict = dict;
            _val._RefC = new();
            _type = ExObjType.DICT;

            AddReference(_type, _val, true);
            Release(t, v);
        }
        public ExObjectPtr(List<ExObjectPtr> o)
        {
            _type = ExObjType.ARRAY;
            _val.l_List = o;
            _val._RefC = new();
            AddReference(_type, _val, true);
        }
        public void Assign(List<ExObjectPtr> o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val.l_List = o;
            _val._RefC = new();
            _type = ExObjType.ARRAY;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObjectPtr(ExInstance o)
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
            _val._RefC = new() { _refc = _val._Instance._refc };
            _type = ExObjType.INSTANCE;

            AddReference(_type, _val, true);
            Release(t, v);
        }
        public ExObjectPtr(ExClass o)
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
            _val._RefC = new() { _refc = _val._Class._refc };
            _type = ExObjType.CLASS;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObjectPtr(ExClosure o)
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
            _val._RefC = new() { _refc = _val._Closure._refc };
            _type = ExObjType.CLOSURE;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObjectPtr(ExNativeClosure o)
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
            _val._RefC = new() { _refc = _val._NativeClosure._refc };
            _type = ExObjType.NATIVECLOSURE;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObjectPtr(ExOuter o)
        {
            _type = ExObjType.CLOSURE;
            _val._Outer = o;
            AddReference(_type, _val, true);
        }
        public void Assign(ExOuter o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val._Outer = o;
            _val._RefC = new() { _refc = _val._Outer._refc };
            _type = ExObjType.OUTER;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObjectPtr(ExWeakRef o)
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
            _val._RefC = new() { _refc = _val._WeakRef._refc };
            _type = ExObjType.WEAKREF;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObjectPtr(ExFuncPro o)
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
            _val._RefC = new() { _refc = _val._FuncPro._refc };
            _type = ExObjType.FUNCPRO;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObjectPtr(ExUserData o)
        {
            _type = ExObjType.USERDATA;
            _val._UserData = o;
            AddReference(_type, _val, true);
        }
        public void Assign(ExUserData o)
        {
            ExObjType t = _type;
            ExObjVal v = _val;

            _val._UserData = o;
            _val._RefC = new() { _refc = _val._UserData._refc };
            _type = ExObjType.USERDATA;

            AddReference(_type, _val, true);
            Release(t, v);
        }
        //
        public void Nullify()
        {
            ExObjType oldt = _type;
            dynamic oldv = _val;
            _type = ExObjType.NULL;
            _val = new();
            Release(oldt, oldv);
        }

    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExRefC : ExObjectPtr
    {
        public int _refc = 0;
        public ExWeakRef _weakref;

        public override void Release() { base.Release(); }

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

        public new string GetDebuggerDisplay()
        {
            return "REFC: " + _refc;
        }
    }

    public class ExWeakRef : ExRefC
    {
        public ExObject obj;

        public ExWeakRef()
        {
            _type = ExObjType.WEAKREF;
        }

        public override void Release()
        {
            if (((int)obj._type & (int)ExObjFlag.REF_COUNTED) == (int)obj._type)
            {
                obj._val._WeakRef = null;
            }
        }
    }

    public class ExDeleg : ExCollectable
    {
        public ExObjectPtr _delegate; // TO-DO dict

        public bool GetMetaM(ExVM vm, ExMetaM m, ref ExObjectPtr res)
        {
            if (_delegate != null)
            {
                string k = vm._sState._metaMethods[(int)m].ToString();
                if (_delegate._val.d_Dict.ContainsKey(k))
                {
                    res = _delegate._val.d_Dict[k];
                    return true;
                }
                return false;
            }
            return false;
        }
    }

    public class ExCollectable : ExRefC
    {
        public ExCollectable _prev;
        public ExCollectable _next;
        public ExSState _sState;

        public ExCollectable GetHead()
        {
            ExCollectable curr = this;
            while (curr._prev != null)
            {
                curr = curr._prev;
            }
            return curr;
        }

        public void AddToChain(ExCollectable ch, ExCollectable c)
        {
            c._prev = null;
            c._next = ch;
            GetHead()._prev = c;
        }

        public static void RemoveFromChain(ExCollectable ch, ExCollectable c)
        {
            if (c._prev != null)
            {
                c._prev._next = c._next;
            }

            if (c._next != null)
            {
                c._next._prev = c._prev;
            }

            c._prev = null;
            c._next = null;
        }
    }

    public class ExList : ExObjectPtr
    {
        public ExList()
        {
            _type = ExObjType.ARRAY;
            _val.l_List = new();
        }

        public ExList(bool n)
        {
            _type = ExObjType.ARRAY;
            _val.l_List = n ? new() : null;
        }
        public ExList(char c)
        {
            _type = ExObjType.ARRAY;
            _val.l_List = c != '0' ? new() : null;
        }
        public ExList(ExList e)
        {
            _type = ExObjType.ARRAY;
            _val.l_List = e._val.l_List;
        }
        public ExList(List<ExObjectPtr> e)
        {
            _type = ExObjType.ARRAY;
            _val.l_List = e;
        }
    }

    public class ExBool : ExObjectPtr
    {
        public ExBool()
        {
            _type = ExObjType.BOOL;
            _val.b_Bool = false;
        }

        public ExBool(bool n)
        {
            _type = ExObjType.BOOL;
            _val.b_Bool = n;
        }
        public ExBool(char c)
        {
            _type = ExObjType.BOOL;
            _val.b_Bool = c != '0';
        }
        public ExBool(ExInt e)
        {
            _type = ExObjType.BOOL;
            _val.b_Bool = e._val.b_Bool;
        }
        public ExBool(dynamic o)
        {
            _type = ExObjType.BOOL;
            _val.b_Bool = o != null && o != false && o != -1;
        }
    }

    public class ExInt : ExObjectPtr
    {
        public ExInt()
        {
            _type = ExObjType.INTEGER;
            _val.i_Int = 0;
        }

        public ExInt(int n)
        {
            _type = ExObjType.INTEGER;
            _val.i_Int = n;
        }
        public ExInt(char c)
        {
            _type = ExObjType.INTEGER;
            _val.i_Int = c;
        }
        public ExInt(ExInt e)
        {
            _type = ExObjType.INTEGER;
            _val.i_Int = e._val.i_Int;
        }
        public ExInt(dynamic o)
        {
            _type = ExObjType.INTEGER;
            _val.i_Int = (int)o;
        }
    }

    public class ExFloat : ExObjectPtr
    {
        public ExFloat()
        {
            _type = ExObjType.FLOAT;
            _val.f_Float = 0;
        }

        public ExFloat(float n)
        {
            _type = ExObjType.FLOAT;
            _val.f_Float = n;
        }
        public ExFloat(char c)
        {
            _type = ExObjType.FLOAT;
            _val.f_Float = c;
        }
        public ExFloat(ExFloat e)
        {
            _type = ExObjType.FLOAT;
            _val.f_Float = e._val.f_Float;
        }
        public ExFloat(dynamic o)
        {
            _type = ExObjType.FLOAT;
            _val.f_Float = (float)o;
        }
    }

    public class ExString : ExObjectPtr
    {
        public ExString()
        {
            _type = ExObjType.STRING;
            _val.s_String = string.Empty;
        }

        public ExString(string n)
        {
            _type = ExObjType.STRING;
            _val.s_String = n;
        }
        public ExString(char c)
        {
            _type = ExObjType.STRING;
            _val.s_String = c.ToString();
        }
        public ExString(ExString e)
        {
            _type = ExObjType.STRING;
            _val.s_String = e._val.s_String;
        }
        public ExString(dynamic o)
        {
            _type = ExObjType.STRING;
            if (string.IsNullOrEmpty(o))
            {
                _val.s_String = string.Empty;
            }
            else
            {
                _val.s_String = o.ToString();
            }
        }
    }

    public class ExUserP : ExObjectPtr
    {
        public ExUserP() { _type = ExObjType.USERPTR; }
    }
    public class ExUserData : ExDeleg
    {
        public ExInt _size;
        public ExInt _hook;
        public ExUserP _typetag;

        public ExUserData()
        {
            _type = ExObjType.USERDATA;
        }

        public static ExUserData Create(ExSState exS, ExInt size)
        {
            ExUserData u = new() { _delegate = new(), _hook = new() };
            u._next = null;
            u._prev = null;
            u._sState = exS;
            u.AddToChain(u._sState._GC_CHAIN, u);
            u._size = size;

            u._typetag = new();
            u._typetag._val._UserPointer._val.i_Int = 0;

            return u;
        }
    }

    public class ExRegFunc
    {
        public string name;
        public ExFunc func;
        public int n_pchecks;
        public string mask;
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExStack //<T> where T : class, new()
    {
        public List<ExObjectPtr> _values;

        public int _size;
        public int _alloc;

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

        public void Resize(int n, ExObjectPtr filler = null)
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

        public ExObjectPtr Back() => _values[_size - 1];

        public ExObjectPtr Push(ExObjectPtr o)
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

        public void Insert(int i, ExObjectPtr o)
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


        public ExObjectPtr this[int n]
        {
            get => _values[n];
            set => _values[n].Assign(value);
        }
    }
}
