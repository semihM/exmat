using System;
using System.Collections.Generic;
using System.Linq;
using ExMat.FuncPrototype;
using ExMat.InfoVar;
using ExMat.Lexer;
using ExMat.Objects;
using ExMat.OPs;

namespace ExMat.States
{
    public class ExFState
    {
        public ExObjectPtr _source;

        public ExObjectPtr _name;

        public Dictionary<string, dynamic> _names = new();

        public int _nliterals;
        public Dictionary<string, dynamic> _literals = new();

        public List<ExLocalInfo> _localvs = new();
        public List<ExLocalInfo> _localinfos = new();

        public int _nouters;
        public List<ExOuterInfo> _outerinfos = new();

        public List<ExLineInfo> _lineinfos = new();

        public List<ExObjectPtr> _params = new();
        public List<int> _defparams = new();

        public List<ExFuncPro> _funcs = new();

        public int _stacksize;
        public ExStack _tStack = new();

        public List<ExInstr> _instructions = new();

        public ExFState _parent;
        public List<ExFState> _children = new();

        public ExSState _Sstate;
        public ExSState _SstateB;

        public List<int> _breaks = new();
        public List<int> _continues = new();
        public List<int> _breaktargs = new();
        public List<int> _continuetargs = new();

        public bool _not_snoozed;

        public int _returnE;

        public int _lastline;

        public bool _pvars;

        private const int MAX_STACK_SIZE = 255;
        private const int MAX_LITERALS = int.MaxValue;

        public int n_statement = 0;

        public ExFState() { }

        public ExFState(ExSState sState, ExFState parent)
        {
            _nliterals = 0;

            _Sstate = sState;
            _SstateB = sState;

            _not_snoozed = true;
            _parent = parent;

            _stacksize = 0;
            _returnE = 0;
            _nouters = 0;

            _pvars = false;
        }

        public void SetInstrParams(int pos, int p1, int p2, int p3, dynamic p4)
        {
            _instructions[pos].arg0._val.i_Int = p1;
            _instructions[pos].arg1 = p2;
            _instructions[pos].arg2._val.i_Int = p3;
            _instructions[pos].arg3._val.i_Int = p4;
        }
        public void SetInstrParam(int pos, int pno, int val)
        {
            switch (pno)
            {
                case 0:
                    {
                        _instructions[pos].arg0._val.i_Int = val;
                        break;
                    }
                case 1:
                case 4:
                    {
                        _instructions[pos].arg1 = val;
                        break;
                    }
                case 2:
                    {
                        _instructions[pos].arg2._val.i_Int = val;
                        break;
                    }
                case 3:
                    {
                        _instructions[pos].arg3._val.i_Int = val;
                        break;
                    }
            }
        }

        public bool IsBlockMacro(string name)
        {
            return _Sstate._blockmacros.ContainsKey(name);
        }

        public bool AddBlockMacro(string name, ExMacro mac)
        {
            _Sstate._blockmacros.Add(name, mac);
            return true;
        }

        public bool IsMacro(ExObject o)
        {
            return _Sstate._macros.ContainsKey(o.GetString());
        }

        public bool IsFuncMacro(ExObject o)
        {
            return _Sstate._macros[o.GetString()].GetBool();
        }

        public bool AddMacro(ExObjectPtr o, bool isfunc, bool forced = false)
        {
            if (_Sstate._macros.ContainsKey(o.GetString()))
            {
                if (forced)
                {
                    _Sstate._macros[o.GetString()].Assign(isfunc);
                    return true;
                }
                return false;
            }
            else
            {
                _Sstate._macros.Add(o.GetString(), new(isfunc));
                return true;
            }
        }

        public int GetConst(ExObjectPtr o)
        {
            string name;
            if (o._type == ExObjType.SPACE)
            {
                name = o._val.c_Space.GetString();
            }
            else
            {
                name = o.GetString();
            }

            ExObjectPtr v = new();
            if (!_literals.ContainsKey(name))
            {
                v._val.i_Int = _nliterals;
                _literals.Add(name, v);
                _nliterals++;
                if (_nliterals > MAX_LITERALS)
                {
                    v.Nullify();
                    throw new Exception("too many literals");
                }
            }
            else
            {
                dynamic val = _literals[name];
                if (val._type == ExObjType.WEAKREF)
                {
                    v = val._WeakRef.obj;
                }
                else
                {
                    v = val;
                }
            }

            return v._val.i_Int;
        }

        public int GetCurrPos()
        {
            return _instructions.Count - 1;
        }

        public int GetLocalStackSize()
        {
            return _localvs.Count;
        }

        public void SetLocalStackSize(int s)
        {
            int c_s = _localvs.Count;

            while (c_s > s)
            {
                c_s--;
                ExLocalInfo li = _localvs.Last();
                if (li.name._type != ExObjType.NULL)
                {
                    if (li._eopc == int.MaxValue)
                    {
                        _nouters--;
                    }
                    li._eopc = GetCurrPos();
                    _localinfos.Add(li);
                }
                _localvs.RemoveAt(_localvs.Count - 1);
            }
        }

        public int GetOuterSize(int s_size)
        {
            int c = 0;
            int ls = _localvs.Count - 1;
            while (ls >= s_size)
            {
                if (_localvs[ls--]._eopc == int.MaxValue)
                {
                    c++;
                }
            }
            return c;
        }

        public void AddLineInfo(int line, bool l_op, bool forced)
        {
            if (_lastline != line || forced)
            {
                ExLineInfo li = new();
                li.line = line;
                li.op = (OPC)GetCurrPos() + 1;
                if (l_op)
                {
                    AddInstr(OPC.LINE, 0, line, 0, 0);
                }
                if (_lastline != line)
                {
                    _lineinfos.Add(li);
                }
                _lastline = line;
            }
        }
        public void DiscardTopTarget()
        {
            int dissed = PopTarget();
            int s = _instructions.Count;
            if (s > 0 && _not_snoozed)
            {
                ExInstr instr = _instructions[s - 1];
                switch (instr.op)
                {
                    case OPC.SET:
                    case OPC.NEWSLOT:
                    case OPC.SETOUTER:
                    case OPC.CALL:
                        {
                            if (instr.arg0._val.i_Int == dissed)
                            {
                                instr.arg0._val.i_Int = 985;
                            }
                            break;
                        }
                }
            }
        }

        public int TopTarget()
        {
            return _tStack.Back().GetInt();
        }

        public int PopTarget()
        {
            int n = _tStack.Back().GetInt();

            if (n >= _localvs.Count)
            {
                throw new Exception("unknown variable"); // TO-DO which var name
            }
            ExLocalInfo l = _localvs[n];
            if (l.name._type == ExObjType.NULL)
            {
                _localvs.RemoveAt(_localvs.Count - 1);
            }
            _tStack.Pop();

            return n;
        }

        public int PushTarget(int n = -1)
        {
            if (n != -1)
            {
                _tStack.Push(new(n));
                return n;
            }

            n = FindAStackPos();
            _tStack.Push(new(n));

            return n;
        }

        public int FindAStackPos()
        {
            int size = _localvs.Count;
            _localvs.Add(new ExLocalInfo());
            if (_localvs.Count > _stacksize)
            {
                if (_stacksize > MAX_STACK_SIZE)
                {
                    throw new Exception("Too many locals!");
                }
                _stacksize = _localvs.Count;
            }
            return size;
        }

        public bool IsConstArg(string name, ref ExObject e)
        {
            if (_Sstate._consts.ContainsKey(name))
            {
                ExObjectPtr val = _Sstate._consts[name];
                if (val._type == ExObjType.WEAKREF)
                {
                    e = val._val._WeakRef.obj;
                }
                else
                {
                    e = val;
                }
                return true;
            }

            return false;
        }

        public bool IsLocalArg(int pos)
        {
            return pos < _localvs.Count && _localvs[pos].name._type != ExObjType.NULL;
        }

        public int PushVar(ExObject v)
        {
            int n = _localvs.Count;
            ExLocalInfo l = new();
            l.name = v;
            l._sopc = GetCurrPos() + 1;
            l._pos = _localvs.Count;
            _localvs.Add(l);
            if (_localvs.Count > _stacksize)
            {
                _stacksize = _localvs.Count;
            }
            return n;
        }

        public int GetLocal(ExObject local)
        {
            int c = _localvs.Count;
            while (c > 0)
            {
                if (_localvs[--c].name._val.s_String == local._val.s_String)
                {
                    return c;
                }
            }
            return -1;
        }
        public int GetOuter(ExObject obj)
        {
            int c = _outerinfos.Count;
            for (int i = 0; i < c; i++)
            {
                if (_outerinfos[i].name._val.s_String == obj._val.s_String)
                {
                    return i;
                }
            }

            int p;
            if (_parent != null)
            {
                p = _parent.GetLocal(obj);
                if (p == -1)
                {
                    p = _parent.GetOuter(obj);
                    if (p != -1)
                    {
                        _outerinfos.Add(new ExOuterInfo(obj, new ExInt(p), ExOuterType.OUTER));
                        return _outerinfos.Count - 1;
                    }
                }
                else
                {
                    _parent.SetLocalToOuter(p);
                    _outerinfos.Add(new ExOuterInfo(obj, new ExInt(p), ExOuterType.LOCAL));
                    return _outerinfos.Count - 1;
                }
            }
            return -1;
        }

        public void SetLocalToOuter(int p)
        {
            _localvs[p]._eopc = int.MaxValue;
            _nouters++;
        }

        public ExObjectPtr CreateString(string s, int len = -1)
        {
            if (!_Sstate._strings.ContainsKey(s))
            {
                ExObjectPtr str = new() { _type = ExObjType.STRING };
                str._val.s_String = s;
                _Sstate._strings.Add(s, str);
                return str;
            }
            return _Sstate._strings[s];
        }
        public void AddInstr(ExInstr curr)
        {
            int size = _instructions.Count;

            if (size > 0 && _not_snoozed)
            {
                ExInstr prev = _instructions[size - 1];

                switch (curr.op)
                {
                    case OPC.JZ:
                        {
                            if (prev.op == OPC.CMP && prev.arg1 < 985)
                            {
                                prev.op = OPC.JCMP;
                                prev.arg0 = new(prev.arg1);
                                prev.arg1 = curr.arg1;
                                _instructions[size - 1] = prev;
                                return;
                            }
                            goto case OPC.SET;
                        }
                    case OPC.SET:
                        {
                            if (curr.arg0._val.i_Int == curr.arg3._val.i_Int)
                            {
                                curr.arg0._val.i_Int = 985;
                            }
                            break;
                        }
                    case OPC.SETOUTER:
                        {
                            if (curr.arg0._val.i_Int == curr.arg2._val.i_Int)
                            {
                                curr.arg0._val.i_Int = 985;
                            }
                            break;
                        }
                    case OPC.RETURN:
                        {
                            if (_parent != null && curr.arg0._val.i_Int != 985 && prev.op == OPC.CALL && _returnE < size - 1)
                            {
                                prev.op = OPC.CALL_TAIL;
                                _instructions[size - 1] = prev;
                            }
                            else if (prev.op == OPC.CLOSE)
                            {
                                _instructions[size - 1] = curr;
                                return;
                            }
                            break;
                        }
                    case OPC.GET:
                        {
                            if (prev.op == OPC.LOAD && prev.arg0._val.i_Int == curr.arg2._val.i_Int && (!IsLocalArg(prev.arg0._val.i_Int)))
                            {
                                prev.arg2 = new(curr.arg1);
                                prev.op = OPC.GETK;
                                prev.arg0._val.i_Int = curr.arg0._val.i_Int;
                                _instructions[size - 1] = prev;
                                return;
                            }
                            break;
                        }
                    case OPC.PREPCALL:
                        {
                            if (prev.op == OPC.LOAD && prev.arg0._val.i_Int == curr.arg1 && (!IsLocalArg(prev.arg0._val.i_Int)))
                            {
                                prev.op = OPC.PREPCALLK;
                                prev.arg0._val.i_Int = curr.arg0._val.i_Int;
                                prev.arg2._val.i_Int = curr.arg2._val.i_Int;
                                prev.arg3._val.i_Int = curr.arg3._val.i_Int;
                                _instructions[size - 1] = prev;
                                return;
                            }
                            break;
                        }
                    case OPC.ARRAY_APPEND:
                        {
                            ArrayAType idx = ArrayAType.INVALID;
                            switch (prev.op)
                            {
                                case OPC.LOAD:
                                    {
                                        idx = ArrayAType.LITERAL;
                                        break;
                                    }
                                case OPC.LOAD_INT:
                                    {
                                        idx = ArrayAType.INTEGER;
                                        break;
                                    }
                                case OPC.LOAD_FLOAT:
                                    {
                                        idx = ArrayAType.FLOAT;
                                        break;
                                    }
                                case OPC.LOAD_BOOL:
                                    {
                                        idx = ArrayAType.BOOL;
                                        break;
                                    }
                                default:
                                    break;
                            }

                            if (idx != ArrayAType.INVALID && prev.arg0._val.i_Int == curr.arg1 && (!IsLocalArg(prev.arg0._val.i_Int)))
                            {
                                prev.op = OPC.ARRAY_APPEND;
                                prev.arg0._val.i_Int = curr.arg0._val.i_Int;
                                prev.arg2._val.i_Int = (int)idx;
                                prev.arg3._val.i_Int = 985;
                                _instructions[size - 1] = prev;
                                return;
                            }
                            break;
                        }
                    case OPC.MOVE:
                        {
                            switch (prev.op)
                            {
                                case OPC.GET:
                                case OPC.ADD:
                                case OPC.SUB:
                                case OPC.MLT:
                                case OPC.EXP:
                                case OPC.DIV:
                                case OPC.MOD:
                                case OPC.BITWISE:
                                case OPC.LOAD:
                                case OPC.LOAD_INT:
                                case OPC.LOAD_FLOAT:
                                case OPC.LOAD_BOOL:
                                    {
                                        if (prev.arg0._val.i_Int == curr.arg1)
                                        {
                                            prev.arg0._val.i_Int = curr.arg0._val.i_Int;
                                            _not_snoozed = false;
                                            _instructions[size - 1] = prev;
                                            return;
                                        }
                                        break;
                                    }
                            }

                            if (prev.op == OPC.MOVE)
                            {
                                prev.op = OPC.DMOVE;
                                prev.arg2._val.i_Int = curr.arg0._val.i_Int;
                                prev.arg3._val.i_Int = curr.arg1;
                                _instructions[size - 1] = prev;
                                return;
                            }

                            break;
                        }
                    case OPC.LOAD:
                        {
                            if (prev.op == OPC.LOAD && curr.arg1 <= 985)
                            {
                                prev.op = OPC.DLOAD;
                                prev.arg2._val.i_Int = curr.arg0._val.i_Int;
                                prev.arg3._val.i_Int = curr.arg1;
                                _instructions[size - 1] = prev;
                                return;
                            }
                            break;
                        }
                    case OPC.EQ:
                    case OPC.NEQ:
                        {
                            if (prev.op == OPC.LOAD && prev.arg0._val.i_Int == curr.arg1 && (!IsLocalArg(prev.arg0._val.i_Int)))
                            {
                                prev.op = curr.op;
                                prev.arg0._val.i_Int = curr.arg0._val.i_Int;
                                prev.arg2._val.i_Int = curr.arg2._val.i_Int;
                                prev.arg3._val.i_Int = 985;
                                _instructions[size - 1] = prev;
                                return;
                            }
                            break;
                        }
                    case OPC.LOAD_NULL:
                        {
                            if (prev.op == OPC.LOAD_NULL && (prev.arg0._val.i_Int + prev.arg1 == curr.arg0._val.i_Int))
                            {
                                prev.arg1++;
                                prev.op = OPC.LOAD_NULL;
                                _instructions[size - 1] = prev;
                                return;
                            }
                            break;
                        }
                    case OPC.LINE:
                        {
                            if (prev.op == OPC.LINE)
                            {
                                _instructions.RemoveAt(size - 1);
                                _lineinfos.RemoveAt(_lineinfos.Count - 1);
                            }
                            break;
                        }
                }
            }

            _not_snoozed = true;
            _instructions.Add(curr);
        }

        public void AddInstr(OPC op, int arg0, int arg1, int arg2, int arg3)
        {
            ExInstr instr = new() { op = op, arg0 = new(arg0), arg1 = arg1, arg2 = new(arg2), arg3 = new(arg3) };
            AddInstr(instr);
        }

        public void AddInstr(OPC op, int arg0, int arg1, dynamic arg2, dynamic arg3)
        {
            ExInstr instr = new() { op = op, arg0 = new(arg0), arg1 = arg1, arg2 = new(arg2), arg3 = new(arg3) };
            AddInstr(instr);
        }

        public ExFState PushChildState(ExSState es)
        {
            ExFState ch = new() { _Sstate = es, _parent = this };
            _children.Add(ch);
            return ch;
        }

        public void PopChildState()
        {
            ExFState ch = _children.Last();
            while (ch._children.Count > 0)
            {
                ch.PopChildState();
            }
            _children.RemoveAt(_children.Count - 1);
        }

        public void AddParam(ExObject p)
        {
            PushVar(p);
            _params.Add((ExObjectPtr)p);
        }

        public void AddDefParam(int p)
        {
            _defparams.Add(p);
        }

        public int GetDefParamCount()
        {
            return _defparams.Count;
        }

        public ExFuncPro CreatePrototype()
        {
            ExFuncPro funcPro = ExFuncPro.Create(_Sstate,
                                                 _instructions.Count,
                                                 _nliterals,
                                                 _params.Count,
                                                 _funcs.Count,
                                                 _outerinfos.Count,
                                                 _lineinfos.Count,
                                                 _localinfos.Count,
                                                 _defparams.Count);

            funcPro._stacksize = _stacksize;
            funcPro._source = _source;
            funcPro._name = _name;

            foreach (KeyValuePair<string, dynamic> pair in _literals)
            {
                if (pair.Value._type == ExObjType.WEAKREF)
                {
                    int ind = pair.Value._WeakRef.obj._val.i_Int;
                    while (funcPro._lits.Count <= ind)
                    {
                        funcPro._lits.Add(new(string.Empty));
                    }
                    funcPro._lits[ind]._val.s_String = pair.Key;
                }
                else
                {
                    int ind = pair.Value._val.i_Int;
                    while (funcPro._lits.Count <= ind)
                    {
                        funcPro._lits.Add(new(string.Empty));
                    }
                    funcPro._lits[ind]._val.s_String = pair.Key;
                }
            }

            int i;
            for (i = 0; i < _funcs.Count; i++)
            {
                funcPro._funcs.Add(_funcs[i]);
            }
            for (i = 0; i < _params.Count; i++)
            {
                funcPro._params.Add(_params[i]);
            }
            for (i = 0; i < _outerinfos.Count; i++)
            {
                funcPro._outers.Add(_outerinfos[i]);
            }
            for (i = 0; i < _localinfos.Count; i++)
            {
                funcPro._localinfos.Add(_localinfos[i]);
            }
            for (i = 0; i < _lineinfos.Count; i++)
            {
                funcPro._lineinfos.Add(_lineinfos[i]);
            }
            for (i = 0; i < _defparams.Count; i++)
            {
                funcPro._defparams.Add(_defparams[i]);
            }

            foreach (ExInstr it in _instructions)
            {
                funcPro._instr.Add(new ExInstr() { op = it.op, arg0 = it.arg0, arg1 = it.arg1, arg2 = it.arg2, arg3 = it.arg3 });
            }

            funcPro._pvars = _pvars;

            return funcPro;
        }

        public ExFState Copy()
        {
            return new()
            {
                _children = _children,
                _defparams = _defparams,
                _funcs = _funcs,
                _instructions = _instructions,
                _lastline = _lastline,
                _lineinfos = _lineinfos,
                _literals = _literals,
                _localinfos = _localinfos,
                _localvs = _localvs,
                _name = _name,
                _names = _names,
                _nliterals = _nliterals,
                _not_snoozed = _not_snoozed,
                _nouters = _nouters,
                _outerinfos = _outerinfos,
                _params = _params,
                _parent = _parent,
                _pvars = _pvars,
                _returnE = _returnE,
                _source = _source,
                _Sstate = _Sstate,
                _SstateB = _SstateB,
                _stacksize = _stacksize,
                _tStack = _tStack
            };
        }
    }
}
