using System;
using System.Collections.Generic;
using System.Diagnostics;
using ExMat.Class;
using ExMat.Closure;
using ExMat.FuncPrototype;
using ExMat.States;
using ExMat.VM;

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

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExObject : IDisposable
    {
        public ExObjType _type = ExObjType.NULL;

        public ExObjVal _val;
        private bool disposedValue;

        public ExObject() { }
        public ExObject(int i)
        {
            _type = ExObjType.INTEGER;
            _val.i_Int = i;
        }
        public ExObject(double f)
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
            return ((int)_type & (int)ExObjFlag.BOOLFALSEABLE) != 0;
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

        public Dictionary<string, ExObjectPtr> GetDict()
        {
            return _type == ExObjType.DICT ? _val.d_Dict : null;
        }

        public List<ExObjectPtr> GetList()
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
                    _val._RefC = null;
                    switch (_type)
                    {
                        case ExObjType.INTEGER:
                        case ExObjType.FLOAT:
                        case ExObjType.BOOL:
                            {
                                _val.i_Int = 0;
                                break;
                            }
                        case ExObjType.STRING:
                            {
                                _val.s_String = null;
                                break;
                            }
                        case ExObjType.ARRAY:
                            {
                                Disposer.DisposeList(ref _val.l_List);
                                break;
                            }
                        case ExObjType.DICT:
                            {
                                Disposer.DisposeDict(ref _val.d_Dict);
                                break;
                            }
                        default:
                            {
                                _val._Method = null;
                                _val._Closure = null;
                                _val._Outer = null;
                                _val._NativeClosure = null;
                                _val._UserData = null;
                                _val._UserPointer = null;
                                _val._FuncPro = null;
                                _val._Deleg = null;
                                _val._Thread = null;
                                _val._Class = null;
                                _val._Instance = null;
                                _val._WeakRef = null;
                                break;
                            }
                    }
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
            return (int)GetInt() & 0x00FFFFFF;
        }

        public static bool IsRefC(ExObjType t)
        {
            return ((int)t & (int)ExObjFlag.REF_COUNTED) > 0;
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
        public ExObjectPtr(long i)
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
        public ExObjectPtr(double f)
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
            _val._RefC = new() { _refc = _val._Instance._refc++ };
            _type = ExObjType.INSTANCE;

            AddReference(_type, _val, true);
            Release(t, v);
        }
        public ExObjectPtr(ExList o)
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
            _val._RefC = new() { _refc = _val._Class._refc++ };
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
            _val._RefC = new() { _refc = _val._Closure._refc++ };
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
            _val._RefC = new() { _refc = _val._NativeClosure._refc++ };
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
            _val._RefC = new() { _refc = _val._Outer._refc++ };
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
            _val._RefC = new() { _refc = _val._WeakRef._refc++ };
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
            _val._RefC = new() { _refc = _val._FuncPro._refc++ };
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
            _val._RefC = new() { _refc = _val._UserData._refc++ };
            _type = ExObjType.USERDATA;

            AddReference(_type, _val, true);
            Release(t, v);
        }

        public ExObjectPtr(ExSpace o)
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
    public class ExRefC : ExObjectPtr
    {
        public int _refc = 0;
        public ExWeakRef _weakref;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _weakref.Dispose();
        }

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
        public ExObjectPtr obj;

        public ExWeakRef()
        {
            _type = ExObjType.WEAKREF;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            obj.Dispose();
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
        public ExObjectPtr _delegate;

        public bool GetMetaM(ExVM vm, ExMetaM m, ref ExObjectPtr res)
        {
            if (_delegate != null)
            {
                if (_delegate._type == ExObjType.DICT)
                {
                    string k = vm._sState._metaMethods[(int)m].GetString();
                    if (_delegate.GetDict().ContainsKey(k))
                    {
                        res.Assign(_delegate.GetDict()[k]);
                        return true;
                    }
                }
                else if (_delegate._type == ExObjType.ARRAY)
                {
                    int k = vm._sState.GetMetaIdx(vm._sState._metaMethods[(int)m].GetString());
                    if (_delegate.GetList()[k]._type != ExObjType.NULL)
                    {
                        res.Assign(_delegate.GetList()[k]);
                        return true;
                    }
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

        public static void RemoveFromChain(ref ExCollectable ch, ExCollectable c)
        {
            if (c._prev != null)
            {
                c._prev._next = c._next;
            }
            else
            {
                ch = c._next;
            }

            if (c._next != null)
            {
                c._next._prev = c._prev;
            }

            c._prev = null;
            c._next = null;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (_prev != null)
            {
                _prev._next = _next;
                _prev = null;
            }

            if (_next != null)
            {
                _next._prev = _prev;
                _next = null;
            }

            _sState = null;
        }
    }

    public class ExList : ExObjectPtr
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
        public ExList(List<ExObjectPtr> e)
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

        public ExInt(double n)
        {
            _type = ExObjType.INTEGER;
            _val.i_Int = n > long.MaxValue ? long.MaxValue : ( n < long.MinValue ? long.MinValue : (long)n);
        }
        public ExInt(long n)
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
    }

    public class ExFloat : ExObjectPtr
    {
        public ExFloat()
        {
            _type = ExObjType.FLOAT;
            _val.f_Float = 0;
        }

        public ExFloat(long n)
        {
            _type = ExObjType.FLOAT;
            _val.f_Float = n;
        }
        public ExFloat(double n)
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

    public class ExRegFunc : IDisposable
    {
        public string name;
        public ExFunc func;
        public int n_pchecks;
        public Dictionary<int, ExObjectPtr> d_defaults = new();
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
        public List<ExObjectPtr> _values;

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
