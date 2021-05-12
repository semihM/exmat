using System;
using System.Collections.Generic;
using ExMat.InfoVar;
using ExMat.Objects;
using ExMat.States;
using ExMat.BaseLib;
using ExMat.Utils;
using ExMat.Closure;
using ExMat.Class;
using ExMat.FuncPrototype;
using ExMat.API;
using ExMat.OPs;

namespace ExMat.VM
{
    public class ExVM
    {
        public ExSState _sState = new();

        public List<ExObjectPtr> _stack;
        public int _stackbase;
        public int _top;

        public List<ExCallInfo> _callsstack;
        public Node<ExCallInfo> ci;
        public int _alloccallsize;
        public int _callstacksize;

        public ExObjectPtr _rootdict;

        public ExOuter _openouters;

        public int _nnativecalls;
        public int _nmetacalls;

        public ExObjectPtr tmpreg = new();

        public List<ExTrap> _traps = new();

        public void Initialize(int s_size)
        {
            ExUtils.InitList(ref _stack, s_size);
            _alloccallsize = 4;
            ExUtils.InitList(ref _callsstack, _alloccallsize);
            _callstacksize = 0;

            _stackbase = 0;
            _top = 0;
            _rootdict = new() { _type = ExObjType.DICT };
            _rootdict._val.d_Dict = new();
            ExBaseLib.RegisterBase(this);

        }

        public void ToString(ExObjectPtr obj, ref ExObjectPtr res, bool inside = false)
        {
            switch (obj._type)
            {
                case ExObjType.INTEGER:
                    {
                        res = new(obj.GetInt().ToString());
                        break;
                    }
                case ExObjType.FLOAT:
                    {
                        res = new(obj.GetFloat().ToString());
                        break;
                    }
                case ExObjType.STRING:
                    {
                        res = inside ? new("\"" + obj.GetString() + "\"") : new(obj.GetString());
                        break;
                    }
                case ExObjType.BOOL:
                    {
                        res = new(obj._val.b_Bool ? "true" : "false");
                        break;
                    }
                case ExObjType.NULL:
                    {
                        res = new(obj._val.s_String ?? "null");
                        break;
                    }
                case ExObjType.ARRAY:
                    {
                        if (inside)
                        {
                            res = new("ARRAY(" + (obj._val.l_List == null ? "empty" : obj._val.l_List.Count) + ")");
                            break;
                        }
                        ExObjectPtr temp = new(string.Empty);
                        string s = "[";
                        int n = 0;
                        int c = obj._val.l_List.Count;

                        foreach (ExObjectPtr o in obj._val.l_List)
                        {
                            ToString(o, ref temp, true);

                            s += temp.GetString();

                            n++;
                            if (n != c)
                            {
                                s += ", ";
                            }
                        }
                        s += "]";

                        res = new(s);
                        break;
                    }
                case ExObjType.DICT:
                    {
                        if (inside)
                        {
                            res = new("DICT(" + (obj._val.l_List == null ? "empty" : obj._val.l_List.Count) + ")");
                            break;
                        }
                        ExObjectPtr temp = new(string.Empty);
                        string s = "{";
                        int n = 0;
                        int c = obj._val.d_Dict.Count;
                        if (c > 0)
                        {
                            s += "\n\t";
                        }

                        foreach (KeyValuePair<string, ExObjectPtr> pair in obj._val.d_Dict)
                        {
                            ToString(pair.Value, ref temp, true);

                            s += pair.Key + " = " + temp.GetString();

                            n++;
                            if (n != c)
                            {
                                s += "\n\t";
                            }
                            else
                            {
                                s += "\n";
                            }
                        }
                        s += "}";

                        res = new(s);
                        break;
                    }
                default:
                    {
                        res = new(obj._type.ToString());
                        break;
                    }
            }
        }

        public bool NewSlotA(ExObjectPtr self, ExObjectPtr key, ExObjectPtr val, ExObjectPtr attrs, bool bstat, bool braw)
        {
            if (self._type != ExObjType.CLASS)
            {
                throw new Exception("object has to be a class");
            }

            ExClass cls = self._val._Class;

            if (!braw)
            {
                ExObjectPtr meta = cls._metas[(int)ExMetaM.NEWM];
                if (meta._type != ExObjType.NULL)
                {
                    Push(self);
                    Push(key);
                    Push(val);
                    Push(attrs);
                    Push(bstat);
                    return CallMetaMethod(ref meta, ExMetaM.NEWM, 5, ref tmpreg);
                }
            }

            if (!NewSlot(self, key, val, bstat))
            {
                return false;
            }

            if (attrs._type != ExObjType.NULL)
            {
                cls.SetAttrs(key, attrs);
            }
            return true;
        }

        public bool NewSlot(ExObjectPtr self, ExObjectPtr key, ExObjectPtr val, bool bstat)
        {
            if (key._type == ExObjType.NULL)
            {
                throw new Exception("'null' can't be used as index");
            }

            switch (self._type)
            {
                case ExObjType.DICT:
                    {
                        bool raw = true;
                        bool deleg = true; // TO-DO
                        if (deleg)
                        {
                        }

                        if (raw)
                        {
                            ExObjectPtr v = new();
                            v.Assign(val);
                            self._val.d_Dict.Add(key.GetString(), v);
                        }
                        break;
                    }
                case ExObjType.INSTANCE:
                    {
                        throw new Exception("instances don't support new slots");
                    }
                case ExObjType.CLASS:
                    {
                        if (!self._val._Class.NewSlot(_sState, key, val, bstat))
                        {
                            if (self._val._Class._islocked)
                            {
                                throw new Exception("can't modify a class that has already been instantianted");
                            }
                            else
                            {
                                throw new Exception(key.GetString() + " already exists");
                            }
                        }
                        break;
                    }
                default:
                    {
                        throw new Exception("indexing " + self._type.ToString() + " with " + key._type.ToString());
                    }
            }
            return true;
        }

        public void Pop(int n)
        {
            for (int i = 0; i < n; i++)
            {
                _stack[--_top].Nullify();
            }
        }

        public void Remove(int n)
        {
            n = n >= 0 ? n + _stackbase - 1 : _top + n;
            for (int i = n; i < _top; i++)
            {
                _stack[i].Assign(_stack[i + 1]);
            }
            _stack[_top].Nullify();
            _top--;
        }

        public void Pop()
        {
            _stack[--_top].Nullify();
        }
        public ExObjectPtr PopGet()
        {
            return _stack[--_top];
        }

        public void Push(bool o) => _stack[_top++].Assign(o);
        public void Push(ExObject o) => _stack[_top++].Assign(o);
        public void Push(ExObjectPtr o) => _stack[_top++].Assign(o);
        public void Push(ExUserP o) => _stack[_top++].Assign(o);
        public void Push(ExUserData o) => _stack[_top++].Assign(o);
        public void Push(Dictionary<string, ExObjectPtr> o) => _stack[_top++].Assign(o);
        public void Push(List<ExObjectPtr> o) => _stack[_top++].Assign(o);
        public void Push(ExInstance o) => _stack[_top++].Assign(o);
        public void Push(ExClass o) => _stack[_top++].Assign(o);
        public void Push(ExClosure o) => _stack[_top++].Assign(o);
        public void Push(ExNativeClosure o) => _stack[_top++].Assign(o);
        public void Push(ExOuter o) => _stack[_top++].Assign(o);
        public void Push(ExWeakRef o) => _stack[_top++].Assign(o);
        public void Push(ExFuncPro o) => _stack[_top++].Assign(o);

        public void PushNull()
        {
            _stack[_top++].Nullify();
        }

        public ExObjectPtr Top()
        {
            return _stack[_top - 1];
        }

        public ExObjectPtr GetAbove(int n)
        {
            return _stack[_top + n];
        }
        public ExObjectPtr GetAt(int n)
        {
            return _stack[n];
        }

        public ExObjectPtr CreateString(string s, int len = -1)
        {
            if (!_sState._strings.ContainsKey(s))
            {
                ExObjectPtr str = new() { _type = ExObjType.STRING };
                str.SetString(s);

                _sState._strings.Add(s, str);
                return str;
            }
            return _sState._strings[s];
        }

        public static bool CreateClassInst(ExClass cls, ref ExObjectPtr o, ExObjectPtr cns)
        {
            o.Assign(cls.CreateInstance());
            if (!cls.GetConstructor(ref cns))
            {
                cns.Nullify();
            }
            return true;
        }

        public bool StartCall(ExClosure cls, int trg, int args, int sbase, bool tail)
        {
            ExFuncPro pro = cls._func;

            int p = pro.n_params;
            int newt = sbase + pro._stacksize;
            int nargs = args;

            if (pro._pvars)
            {
                p--;
                if (nargs < p)
                {
                    throw new Exception("wrong number of parameters");
                }

                int nvargs = nargs - p;
                List<ExObjectPtr> varglis = new();
                int pb = sbase + p;
                for (int n = 0; n < nvargs; n++)
                {
                    ExObjectPtr varg = new();
                    varg.Assign(_stack[pb]);
                    varglis.Add(varg);
                    _stack[pb].Nullify();
                    pb++;
                }

                _stack[sbase + p].Assign(new ExList(varglis));
            }
            else if (p != nargs)
            {
                int n_def = pro.n_defparams;
                int diff;
                if (n_def > 0 && nargs < p && (diff = p - nargs) <= n_def)
                {
                    for (int n = n_def - diff; n < n_def; n++)
                    {
                        _stack[sbase + nargs++].Assign(cls._defparams[n]);
                    }
                }
                else
                {
                    throw new Exception("wrong number of parameters");
                }
            }

            if (cls._envweakref != null)
            {
                _stack[sbase].Assign(cls._envweakref.obj);
            }

            if (!EnterFrame(sbase, newt, tail))
            {
                return false;
            }

            ci._val._closure = new(); ci._val._closure.Assign(cls);
            ci._val._lits = pro._lits;
            ci._val._instrs = pro._instr;
            ci._val._idx_instrs = 0;
            ci._val._target = trg;

            return true;
        }

        public bool CallMetaMethod(ref ExObjectPtr cls, ExMetaM m, int nparams, ref ExObjectPtr res)
        {
            _nmetacalls++;
            if (Call(ref cls, nparams, _top - nparams, ref res))
            {
                _nmetacalls--;
                Pop(nparams);
                return true;
            }
            _nmetacalls--;
            Pop(nparams);
            return false;
        }

        public bool Exec(ExObjectPtr cls, int narg, int stackbase, ref ExObjectPtr o)
        {
            if (_nnativecalls + 1 > 100)
            {
                throw new Exception("Native stack overflow");
            }

            _nnativecalls++;
            // TO-DO AutoDec nativacalls
            Node<ExCallInfo> prevci = ci;
            int traps = 0;

            // TO-DO Exec types
            tmpreg.Assign(cls);
            if (!StartCall(tmpreg._val._Closure, _top - narg, narg, stackbase, false))
            {
                throw new Exception("no calls found");
            }

            if (ci == prevci)
            {
                o.Assign(_stack[_stackbase + _top - narg]);
                return true;
            }
            ci._val._root = true;

            //Exception restore
            for (; ; )
            {
                if (ci._val == null)
                {
                    return true;
                }

                if (ci._val._idx_instrs >= ci._val._instrs.Count)
                {
                    throw new Exception("instruction index error");
                }

                ExInstr i = ci._val._instrs[ci._val._idx_instrs++];
                switch (i.op)
                {
                    case OPC.LINE:
                        continue;
                    case OPC.LOAD:
                        {
                            GetTargetInStack(i).Assign(ci._val._lits[i.arg1]);
                            continue;
                        }
                    case OPC.LOAD_INT:
                        {
                            GetTargetInStack(i).Assign(i.arg1);
                            continue;
                        }
                    case OPC.LOAD_FLOAT:
                        {
                            GetTargetInStack(i).Assign(new ExFloat(new FloatInt() { i = i.arg1 }.f));
                            continue;
                        }
                    case OPC.LOAD_BOOL:
                        {
                            GetTargetInStack(i).Assign(i.arg1 == 1);
                            continue;
                        }
                    case OPC.DLOAD:
                        {
                            GetTargetInStack(i).Assign(ci._val._lits[i.arg1]);
                            GetTargetInStack(i.arg2.GetInt()).Assign(ci._val._lits[i.arg3.GetInt()]);
                            continue;
                        }
                    case OPC.CALL_TAIL:
                        {
                            ExObjectPtr tmp = new(GetTargetInStack(i.arg1));
                            if (tmp._type == ExObjType.CLOSURE)
                            {
                                ExObjectPtr c = new(); c.Assign(tmp);
                                if (_openouters != null)
                                {
                                    CloseOuters(_stackbase);
                                }
                                for (int j = 0; j < i.arg3.GetInt(); j++)
                                {
                                    GetTargetInStack(i).Assign(GetTargetInStack(i.arg2.GetInt() + j));
                                }
                                if (!StartCall(c._val._Closure, ci._val._target, i.arg3.GetInt(), _stackbase, true))
                                {
                                    throw new Exception("guarded failed call");
                                }
                                continue;
                            }
                            goto case OPC.CALL;
                        }
                    case OPC.CALL:
                        {
                            ExObjectPtr tmp2 = new(GetTargetInStack(i.arg1));
                            switch (tmp2._type)
                            {
                                case ExObjType.CLOSURE:
                                    {
                                        if (!StartCall(tmp2._val._Closure, i.arg0.GetInt(), i.arg3.GetInt(), _stackbase + i.arg2.GetInt(), false))
                                        {
                                            throw new Exception("guarded failed call");
                                        }
                                        continue;
                                    }
                                case ExObjType.NATIVECLOSURE:
                                    {
                                        if (!CallNative(tmp2._val._NativeClosure, i.arg3.GetInt(), _stackbase + i.arg2.GetInt(), ref tmp2))
                                        {
                                            throw new Exception("guarded failed call");
                                        }

                                        if (i.arg0.GetInt() != 985)
                                        {
                                            GetTargetInStack(i.arg0).Assign(tmp2);
                                        }
                                        continue;
                                    }
                                case ExObjType.CLASS:
                                    {
                                        ExObjectPtr instance = new();
                                        if (!CreateClassInst(tmp2._val._Class, ref instance, tmp2))
                                        {
                                            throw new Exception("guarded failed call");
                                        }
                                        if (i.arg0.GetInt() != -1)
                                        {
                                            GetTargetInStack(i.arg0).Assign(instance);
                                        }

                                        int sbase;
                                        switch (tmp2._type)
                                        {
                                            case ExObjType.CLOSURE:
                                                {
                                                    sbase = _stackbase + i.arg2.GetInt();
                                                    _stack[sbase].Assign(instance);
                                                    if (!StartCall(tmp2._val._Closure, -1, i.arg3.GetInt(), sbase, false))
                                                    {
                                                        throw new Exception("guarded failed call");
                                                    }
                                                    break;
                                                }
                                            case ExObjType.NATIVECLOSURE:
                                                {
                                                    sbase = _stackbase + i.arg2.GetInt();
                                                    _stack[sbase].Assign(instance);
                                                    if (!CallNative(tmp2._val._NativeClosure, i.arg3.GetInt(), sbase, ref tmp2))
                                                    {
                                                        throw new Exception("guarded failed call");
                                                    }
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                case ExObjType.DICT:
                                case ExObjType.USERDATA:
                                case ExObjType.INSTANCE:
                                    {
                                        ExObjectPtr cls2 = null;
                                        if (tmp2._val._Deleg != null
                                            && tmp2._val._Deleg.GetMetaM(this, ExMetaM.CALL, ref cls2))
                                        {
                                            Push(tmp2);
                                            for (int j = 0; j < i.arg3.GetInt(); j++)
                                            {
                                                Push(GetTargetInStack(j + i.arg2.GetInt()));
                                            }

                                            if (!CallMetaMethod(ref cls2, ExMetaM.CALL, i.arg3.GetInt() + 1, ref tmp2))
                                            {
                                                throw new Exception("meta method failed call");
                                            }

                                            if (i.arg0.GetInt() != -1)
                                            {
                                                GetTargetInStack(i.arg0).Assign(tmp2);
                                            }
                                            break;
                                        }
                                        goto default;
                                    }
                                default:
                                    throw new Exception("attemt to call " + tmp2._type.ToString());
                            }
                            continue;
                        }
                    case OPC.PREPCALL:
                    case OPC.PREPCALLK:
                        {
                            ExObjectPtr k = i.op == OPC.PREPCALLK ? ci._val._lits[i.arg1] : new(GetTargetInStack(i.arg1));
                            ExObjectPtr obj = new(GetTargetInStack(i.arg2));

                            if (!Getter(ref obj, ref k, ref tmpreg, false, (ExFallback)i.arg2.GetInt()))
                            {
                                throw new Exception("error getting call");
                            }

                            GetTargetInStack(i.arg3).Assign(obj);
                            SwapObjects(GetTargetInStack(i), ref tmpreg);
                            continue;
                        }
                    case OPC.GETK:
                        {
                            ExObjectPtr tmp = new(GetTargetInStack(i.arg2));
                            ExObjectPtr lit = ci._val._lits[i.arg1];

                            if (!Getter(ref tmp, ref lit, ref tmpreg, false, (ExFallback)i.arg2.GetInt()))
                            {
                                throw new Exception("error getting literal"); // access to local var decl before
                            }
                            //GetTargetInStack(i).Assign(tmpreg); // TO-DO
                            SwapObjects(GetTargetInStack(i), ref tmpreg);
                            continue;
                        }
                    case OPC.MOVE:
                        {
                            GetTargetInStack(i).Assign(GetTargetInStack(i.arg1));
                            continue;
                        }
                    case OPC.NEWSLOT:
                        {
                            if (!NewSlot(GetTargetInStack(i.arg1), GetTargetInStack(i.arg2), GetTargetInStack(i.arg3), false))
                            {
                                throw new Exception("guarded failed newslot");
                            }
                            if (i.arg0.GetInt() != 985)
                            {
                                GetTargetInStack(i).Assign(GetTargetInStack(i.arg3));
                            }
                            continue;
                        }
                    case OPC.DELETE:
                        {
                            // TO-DO
                            continue;
                        }
                    case OPC.SET:
                        {
                            ExObjectPtr t = new(GetTargetInStack(i.arg3));
                            if (!Setter(GetTargetInStack(i.arg1), GetTargetInStack(i.arg2), ref t, ExFallback.OK))
                            {
                                throw new Exception("failed setter for '" + GetTargetInStack(i.arg2).GetString() + "' key");
                            }
                            if (i.arg0.GetInt() != 985)
                            {
                                GetTargetInStack(i).Assign(GetTargetInStack(i.arg3));
                            }
                            continue;
                        }
                    case OPC.GET:
                        {
                            ExObjectPtr s1 = new(GetTargetInStack(i.arg1));
                            ExObjectPtr s2 = new(GetTargetInStack(i.arg2));
                            if (!Getter(ref s1, ref s2, ref tmpreg, false, (ExFallback)i.arg1))
                            {
                                throw new Exception("failed getter for '" + s2.GetString() + "' key");
                            }
                            SwapObjects(GetTargetInStack(i), ref tmpreg);
                            //GetTargetInStack(i).Assign(tmpreg);
                            continue;
                        }
                    case OPC.EQ:
                    case OPC.NEQ:
                        {
                            bool res = false;
                            if (!CheckEqual(GetTargetInStack(i.arg2), GetConditionFromInstr(i), ref res))
                            {
                                throw new Exception("equal op failed");
                            }
                            GetTargetInStack(i).Assign(i.op == OPC.EQ ? res : !res);
                            continue;
                        }
                    case OPC.ADD:
                    case OPC.SUB:
                    case OPC.MLT:
                    case OPC.DIV:
                    case OPC.MOD:
                        {
                            GetTargetInStack(i).Assign(DoArithmeticOP(i.op, GetTargetInStack(i.arg2), GetTargetInStack(i.arg1)));
                            continue;
                        }
                    case OPC.BITWISE:
                        {
                            GetTargetInStack(i).Assign(DoBitwiseOP(i.arg3.GetInt(), GetTargetInStack(i.arg2), GetTargetInStack(i.arg1)));
                            continue;
                        }
                    case OPC.RETURN:
                        {
                            if (ReturnValue(i.arg0.GetInt(), i.arg1, ref tmpreg))
                            {
                                //o.Assign(tmpreg); // TO-DO
                                SwapObjects(o, ref tmpreg);
                                return true;
                            }
                            continue;
                        }
                    case OPC.LOAD_NULL:
                        {
                            for (int n = 0; n < i.arg1; n++)
                            {
                                GetTargetInStack(i.arg0.GetInt() + n).Nullify();
                            }
                            continue;
                        }
                    case OPC.LOAD_ROOT:
                        {
                            GetTargetInStack(i).Assign(_rootdict);
                            continue;
                        }
                    case OPC.DMOVE:
                        {
                            GetTargetInStack(i.arg0).Assign(GetTargetInStack(i.arg1));
                            GetTargetInStack(i.arg2).Assign(GetTargetInStack(i.arg3));
                            continue;
                        }
                    case OPC.JMP:
                        {
                            ci._val._idx_instrs += i.arg1;
                            continue;
                        }
                    case OPC.JCMP:
                        {
                            if (!DoCompareOP((CmpOP)i.arg3.GetInt(), GetTargetInStack(i.arg2), GetTargetInStack(i.arg0), ref tmpreg))
                            {
                                throw new Exception("failed compare op");
                            }
                            if (!tmpreg.GetBool())
                            {
                                ci._val._idx_instrs += i.arg1;
                            }
                            continue;
                        }
                    case OPC.JZ:
                        {
                            if (!GetTargetInStack(i.arg0).GetBool())
                            {
                                ci._val._idx_instrs += i.arg1;
                            }
                            continue;
                        }
                    case OPC.GETOUTER:
                        {
                            ExClosure currcls = ci._val._closure._val._Closure;
                            ExOuter outr = currcls._outervals[i.arg1]._val._Outer;
                            GetTargetInStack(i).Assign(outr._valptr);
                            continue;
                        }
                    case OPC.SETOUTER:
                        {
                            ExClosure currcls = ci._val._closure._val._Closure;
                            ExOuter outr = currcls._outervals[i.arg1]._val._Outer;
                            outr._valptr.Assign(GetTargetInStack(i.arg2));
                            if (i.arg0.GetInt() != 985)
                            {
                                GetTargetInStack(i).Assign(GetTargetInStack(i.arg2));
                            }
                            continue;
                        }
                    case OPC.NEW_OBJECT:
                        {
                            switch (i.arg3.GetInt())
                            {
                                case (int)ExNOT.DICT:
                                    {
                                        GetTargetInStack(i).Assign(new Dictionary<string, ExObjectPtr>());
                                        continue;
                                    }
                                case (int)ExNOT.ARRAY:
                                    {
                                        GetTargetInStack(i).Assign(new List<ExObjectPtr>(i.arg1));
                                        continue;
                                    }
                                case (int)ExNOT.CLASS:
                                    {
                                        if (!DoClassOP(GetTargetInStack(i), i.arg1, i.arg2.GetInt()))
                                        {
                                            throw new Exception("failed to create class");
                                        }
                                        continue;
                                    }
                                default:
                                    throw new Exception("unknown object type " + i.arg3.GetInt());
                            }
                        }
                    case OPC.ARRAY_APPEND:
                        {
                            ExObjectPtr val = new();
                            switch (i.arg2.GetInt())
                            {
                                case (int)ArrayAType.STACK:
                                    val.Assign(GetTargetInStack(i.arg1)); break;
                                case (int)ArrayAType.LITERAL:
                                    val.Assign(ci._val._lits[i.arg1]); break;
                                case (int)ArrayAType.INTEGER:
                                    val.Assign(i.arg1); break;
                                case (int)ArrayAType.FLOAT:
                                    val.Assign(new FloatInt() { i = i.arg1 }.f); break;
                                case (int)ArrayAType.BOOL:
                                    val.Assign(i.arg1 == 1); break;
                                default:
                                    {
                                        throw new Exception("unknown array append method");
                                    }
                            }
                            GetTargetInStack(i.arg0)._val.l_List.Add(val);
                            continue;
                        }
                    case OPC.INC:
                    case OPC.PINC:
                        {
                            ExObjectPtr ob = new(i.arg3);
                            ExObjectPtr trg = new(GetTargetInStack(i));
                            ExObjectPtr s1 = new(GetTargetInStack(i.arg1));
                            ExObjectPtr s2 = new(GetTargetInStack(i.arg2));
                            if (!DoDerefInc(OPC.ADD, ref trg, ref s1, ref s2, ref ob, i.op == OPC.PINC, (ExFallback)i.arg1))
                            {
                                throw new Exception(i.op + " failed");
                            }
                            continue;
                        }
                    case OPC.INCL:
                    case OPC.PINCL:
                        {
                            ExObjectPtr ob = GetTargetInStack(i.arg1);
                            if (ob._type == ExObjType.INTEGER)
                            {
                                GetTargetInStack(i).Assign(ob);
                                ob._val.i_Int += i.arg3.GetInt();
                            }
                            else
                            {
                                ob = new(i.arg3);
                                if (i.op == OPC.INCL)
                                {
                                    ob.Assign(DoArithmeticOP(OPC.ADD, ob, o));
                                }
                                else
                                {
                                    ExObjectPtr targ = new(GetTargetInStack(i));
                                    ExObjectPtr val = new(GetTargetInStack(i.arg1));
                                    if (!DoVarInc(OPC.ADD, ref targ, ref val, ref ob))
                                    {
                                        throw new Exception("PINCL failed");
                                    }
                                }
                            }
                            continue;
                        }
                    case OPC.EXISTS:
                        {
                            ExObjectPtr s1 = new(GetTargetInStack(i.arg1));
                            ExObjectPtr s2 = new(GetTargetInStack(i.arg2));
                            GetTargetInStack(i).Assign(Getter(ref s1, ref s2, ref tmpreg, true, ExFallback.DONT));
                            continue;
                        }
                    case OPC.CMP:
                        {
                            ExObjectPtr target = new(GetTargetInStack(i));
                            if (!DoCompareOP((CmpOP)i.arg3.GetInt(), GetTargetInStack(i.arg2), GetTargetInStack(i.arg1), ref target))
                            {
                                throw new Exception("comparison failed");
                            }
                            continue;
                        }
                    case OPC.TRAPPOP:
                        {
                            for (int j = 0; j < i.arg0.GetInt(); j++)
                            {
                                _traps.RemoveAt(_traps.Count - 1);
                                traps--;
                                ci._val._traps--;
                            }
                            continue;
                        }
                    case OPC.CLOSE:
                        {
                            if (_openouters != null)
                            {
                                CloseOuters(GetTargetInStack(i.arg1).GetInt());
                            }
                            continue;
                        }
                    case OPC.AND:
                        {
                            if (!GetTargetInStack(i.arg2).GetBool())
                            {
                                GetTargetInStack(i).Assign(i.arg2);
                                ci._val._idx_instrs += i.arg1;
                            }
                            continue;
                        }
                    case OPC.OR:
                        {
                            if (GetTargetInStack(i.arg2).GetBool())
                            {
                                GetTargetInStack(i).Assign(i.arg2);
                                ci._val._idx_instrs += i.arg1;
                            }
                            continue;
                        }
                    case OPC.NOT:
                        {
                            GetTargetInStack(i).Assign(!GetTargetInStack(i.arg1).GetBool());
                            continue;
                        }
                    case OPC.NEGATE:
                        {
                            ExObjectPtr t = new(GetTargetInStack(i));
                            if (!DoNegateOP(ref t, GetTargetInStack(i.arg2)))
                            {
                                throw new Exception("attempt to negate unknown");
                            }
                            continue;
                        }
                    case OPC.CLOSURE:
                        {
                            ExClosure cl = ci._val._closure._val._Closure;
                            ExFuncPro fp = cl._func;
                            if (!DoClosureOP(GetTargetInStack(i), fp._funcs[i.arg1]))
                            {
                                throw new Exception("failed to create closure");
                            }
                            continue;
                        }
                    case OPC.NEWSLOTA:
                        {
                            if (!NewSlotA(GetTargetInStack(i.arg1),
                                         GetTargetInStack(i.arg2),
                                         GetTargetInStack(i.arg3),
                                         (i.arg0.GetInt() & (int)ExNewSlotFlag.ATTR) > 0 ? GetTargetInStack(i.arg2.GetInt() - 1) : new(),
                                         (i.arg0.GetInt() & (int)ExNewSlotFlag.STATIC) > 0,
                                         false))
                            {
                                throw new Exception("class slot failed");
                            }
                            continue;
                        }
                    default:
                        {
                            throw new Exception("unknown operator " + i.op);
                        }
                }
            }
        }

        public void FindOuterVal(ExObjectPtr target, ExObjectPtr sidx)
        {
            ExOuter opo = _openouters;
            ExOuter tmp;
            while (opo != null && opo._valptr.GetInt() >= sidx.GetInt())
            {
                if (opo._valptr.GetInt() == sidx.GetInt())
                {
                    target.Assign(new ExObjectPtr(opo));
                    return;
                }
                opo = opo._next;
            }

            tmp = ExOuter.Create(_sState, sidx);
            tmp._next = opo;
            tmp.idx = sidx.GetInt() - FindFirstNullInStack();
            //tmp._refc++;
            opo.Assign(tmp);
            target.Assign(new ExObjectPtr(tmp));
        }

        public int FindFirstNullInStack()
        {
            for (int i = 0; i < _stack.Count; i++)
            {
                if (_stack[i]._type == ExObjType.NULL)
                {
                    return i;
                }
            }
            return -1;
        }

        public bool DoClosureOP(ExObjectPtr t, ExFuncPro fp)
        {
            int nout;
            ExClosure cl = ExClosure.Create(_sState, fp);

            if ((nout = fp.n_outers) > 0)
            {
                for (int i = 0; i < nout; i++)
                {
                    ExOuterInfo ov = fp._outers[i];
                    switch (ov._type)
                    {
                        case ExOuterType.LOCAL:
                            {
                                FindOuterVal(cl._outervals[i], GetTargetInStack(ov._src.GetInt()));
                                break;
                            }
                        case ExOuterType.OUTER:
                            {
                                cl._outervals[i].Assign(ci._val._closure._val._Closure._outervals[ov._src.GetInt()]);
                                break;
                            }
                    }
                }
            }
            int ndefpars;
            if ((ndefpars = fp.n_defparams) > 0)
            {
                for (int i = 0; i < ndefpars; i++)
                {
                    int pos = fp._defparams[i];
                    cl._defparams[i].Assign(_stack[_stackbase + pos]);
                }
            }

            t.Assign(cl);
            return true;
        }

        public static bool DoNegateOP(ref ExObjectPtr target, ExObjectPtr val)
        {
            switch (val._type)
            {
                case ExObjType.INTEGER:
                    {
                        target.Assign(-val.GetInt());
                        return true;
                    }
                case ExObjType.FLOAT:
                    {
                        target.Assign(-val.GetFloat());
                        return true;
                    }
                case ExObjType.DICT:
                case ExObjType.USERDATA:
                case ExObjType.INSTANCE:
                    {
                        //TO-DO
                        return false;
                    }
            }
            // Attempt to negate val._type
            return false;
        }

        public bool DoDerefInc(OPC op, ref ExObjectPtr t, ref ExObjectPtr self, ref ExObjectPtr k, ref ExObjectPtr inc, bool post, ExFallback idx)
        {
            ExObjectPtr tmp = new();
            ExObjectPtr tmpk = k;
            ExObjectPtr tmps = self;
            if (!Getter(ref self, ref tmpk, ref tmp, false, idx))
            {
                return false;
            }
            t = DoArithmeticOP(op, tmp, inc);
            if (!Setter(tmps, tmpk, ref t, idx))
            {
                return false;
            }
            if (post)
            {
                t.Assign(tmp);
            }
            return true;
        }

        public static bool DoVarInc(OPC op, ref ExObjectPtr t, ref ExObjectPtr o, ref ExObjectPtr diff)
        {
            ExObjectPtr res = DoArithmeticOP(op, o, diff);
            t.Assign(o);
            o.Assign(res);
            return true;
        }

        public bool DoClassOP(ExObjectPtr target, int bcls, int attr)
        {
            ExClass cb = null;
            ExObjectPtr atrs = new();
            if (bcls != -1)
            {
                // TO-DO extern ??
            }

            if (attr != 985)
            {
                atrs.Assign(_stack[_stackbase + attr]);
            }

            target.Assign(ExClass.Create(_sState, cb));

            // TO-DO meta methods!
            if (target._val._Class._metas[(int)ExMetaM.INH]._type != ExObjType.NULL)
            {
                int np = 2;
                ExObjectPtr r = new();
                Push(target);
                Push(atrs);
                ExObjectPtr mm = target._val._Class._metas[(int)ExMetaM.INH];
                Call(ref mm, np, _top - np, ref r);
                Pop(np);
            }
            target._val._Class._attrs.Assign(atrs);
            return true;
        }

        private static bool InnerDoCompareOP(ExObjectPtr a, ExObjectPtr b, ref int t)
        {
            ExObjType at = a._type;
            ExObjType bt = b._type;
            if (at == bt)
            {
                if (a._val.i_Int == b._val.i_Int)
                {
                    t = 0;
                    return true;
                }
                switch (at)
                {
                    case ExObjType.STRING:
                        {
                            t = a.GetString() == b.GetString() ? 0 : -1;
                            return true;
                        }
                    case ExObjType.INTEGER:
                        {
                            t = a.GetInt() < b.GetInt() ? -1 : 1;
                            return true;
                        }
                    case ExObjType.FLOAT:
                        {
                            t = a.GetFloat() < b.GetFloat() ? -1 : 1;
                            return true;
                        }
                    default:
                        {
                            //TO-DO
                            throw new Exception("failed compare operator");
                        }
                }

            }
            else
            {
                if (a.IsNumeric() && b.IsNumeric())
                {
                    if (at == ExObjType.INTEGER && bt == ExObjType.FLOAT)
                    {
                        if (a.GetInt() == b.GetFloat())
                        {
                            t = 0;
                        }
                        else if (a.GetInt() < b.GetFloat())
                        {
                            t = -1;
                        }
                        else
                        {
                            t = 1;
                        }
                    }
                    else
                    {
                        if (a.GetFloat() == b.GetInt())
                        {
                            t = 0;
                        }
                        else if (a.GetFloat() < b.GetInt())
                        {
                            t = -1;
                        }
                        else
                        {
                            t = 1;
                        }
                    }
                    return true;
                }
                else if (at == ExObjType.NULL)
                {
                    t = -1;
                    return true;
                }
                else if (bt == ExObjType.NULL)
                {
                    t = 1;
                    return true;
                }
                else
                {
                    throw new Exception("failed to compare " + at.ToString() + " and " + bt.ToString());
                }
            }
        }
        public static bool DoCompareOP(CmpOP cop, ExObjectPtr a, ExObjectPtr b, ref ExObjectPtr res)
        {
            int t = 0;
            if (InnerDoCompareOP(a, b, ref t))
            {
                switch (cop)
                {
                    case CmpOP.GRT:
                        res = new(t > 0); return true;
                    case CmpOP.GET:
                        res = new(t >= 0); return true;
                    case CmpOP.LST:
                        res = new(t < 0); return true;
                    case CmpOP.LET:
                        res = new(t <= 0); return true;
                }
            }
            return false;
        }

        public bool ReturnValue(int a0, int a1, ref ExObjectPtr res)
        {
            bool r = ci._val._root;
            int cbase = _stackbase - ci._val._prevbase;

            if (r)
            {
                if (a0 != 985)
                {
                    res.Assign(_stack[_stackbase + a1]);
                }
                else
                {
                    res.Nullify();
                }
            }
            else if (ci._val._target != -1)
            {   // TO-DO cbase may be too low -> index < 0
                if (cbase + ci._val._target >= 0) // TEMP HACK
                {
                    if (a0 != 985)
                    {
                        _stack[cbase + ci._val._target].Assign(_stack[_stackbase + a1]);
                    }
                    else
                    {
                        _stack[cbase + ci._val._target].Nullify();
                    }
                }
            }

            LeaveFrame();
            return r;
        }

        public static ExObjectPtr DoBitwiseOP(int iop, ExObjectPtr a, ExObjectPtr b)
        {
            int a_mask = (int)a._type | (int)b._type;
            ExObjectPtr res;
            if (a_mask == (int)ExObjType.INTEGER)
            {
                BitOP op = (BitOP)iop;
                switch (op)
                {
                    case BitOP.AND:
                        res = new(a.GetInt() & b.GetInt()); break;
                    case BitOP.OR:
                        res = new(a.GetInt() | b.GetInt()); break;
                    case BitOP.XOR:
                        res = new(a.GetInt() ^ b.GetInt()); break;
                    case BitOP.SHIFTL:
                        res = new(a.GetInt() << b.GetInt()); break;
                    case BitOP.SHIFTR:
                        res = new(a.GetInt() >> b.GetInt()); break;
                    default:
                        {
                            throw new Exception("unknown bitwise operation");
                        }
                }
            }
            else
            {
                throw new Exception("bitwise op between " + a._type.ToString() + " and " + b._type.ToString());
            }
            return res;
        }
        private static void InnerDoArithmeticOPInt(OPC op, int a, int b, ref ExObjectPtr res)
        {
            switch (op)
            {
                case OPC.ADD:
                    res = new(a + b); break;
                case OPC.SUB:
                    res = new(a - b); break;
                case OPC.MLT:
                    res = new(a * b); break;
                case OPC.DIV:
                    {
                        if (b == 0)
                        {
                            throw new Exception("division by zero");
                        }

                        res = new(a / b); break;
                    }
                case OPC.MOD:
                    {
                        if (b == 0)
                        {
                            throw new Exception("modulo by zero");
                        }

                        res = new(a % b); break;
                    }
                default:
                    {
                        throw new Exception("unknown arithmetic operation");
                    }
            }
        }
        private static void InnerDoArithmeticOPFloat(OPC op, float a, float b, ref ExObjectPtr res)
        {
            switch (op)
            {
                case OPC.ADD:
                    res = new(a + b); break;
                case OPC.SUB:
                    res = new(a - b); break;
                case OPC.MLT:
                    res = new(a * b); break;
                case OPC.DIV:
                    {
                        if (b == 0)
                        {
                            throw new Exception("division by zero");
                        }

                        res = new(a / b); break;
                    }
                case OPC.MOD:
                    {
                        if (b == 0)
                        {
                            throw new Exception("modulo by zero");
                        }

                        res = new(a % b); break;
                    }
                default:
                    {
                        throw new Exception("unknown arithmetic operation");
                    }
            }
        }
        public static ExObjectPtr DoArithmeticOP(OPC op, ExObjectPtr a, ExObjectPtr b)
        {
            ExObjectPtr res = null;
            // TO-DO find out why string are nulled
            if (a._type == ExObjType.NULL && a._val.s_String != null)
            {
                a._type = ExObjType.STRING;
            }
            if (b._type == ExObjType.NULL && b._val.s_String != null)
            {
                b._type = ExObjType.STRING;
            }
            int a_mask = (int)a._type | (int)b._type;
            switch (a_mask)
            {
                case (int)ArithmeticMask.INT:
                    {
                        InnerDoArithmeticOPInt(op, a.GetInt(), b.GetInt(), ref res);
                        break;
                    }
                case (int)ArithmeticMask.FLOATINT:
                case (int)ArithmeticMask.FLOAT:
                    {
                        InnerDoArithmeticOPFloat(op, a.GetFloat(), b.GetFloat(), ref res);
                        break;
                    }
                case (int)ArithmeticMask.STRING:
                    {
                        res = new(a.GetString() + b.GetString());
                        break;
                    }
                case (int)ArithmeticMask.STRINGNULL:
                    {
                        res = new(a._type == ExObjType.NULL ? ("null" + b.GetString()) : (a.GetString() + "null"));
                        break;
                    }
                case (int)ArithmeticMask.STRINGINT:
                case (int)ArithmeticMask.STRINGFLOAT:
                    {
                        if (a._type == ExObjType.STRING)
                        {
                            res = new(a.GetString() + (b._type == ExObjType.INTEGER ? b.GetInt() : b.GetFloat()));
                        }
                        else
                        {
                            res = new((a._type == ExObjType.INTEGER ? a.GetInt() : a.GetFloat()) + b.GetString());
                        }
                        break;
                    }
                default:
                    {
                        throw new Exception("can't do " + op.ToString() + " operation between " + a._type.ToString() + " and " + b._type.ToString());
                    }
            }
            return res;
        }

        public enum ArithmeticMask
        {
            INT = ExObjType.INTEGER,
            FLOATINT = ExObjType.INTEGER | ExObjType.FLOAT,
            FLOAT = ExObjType.FLOAT,
            STRING = ExObjType.STRING,
            STRINGINT = ExObjType.STRING | ExObjType.INTEGER,
            STRINGFLOAT = ExObjType.STRING | ExObjType.FLOAT,
            STRINGNULL = ExObjType.STRING | ExObjType.NULL
        }


        public static bool CheckEqual(ExObjectPtr x, ExObjectPtr y, ref bool res)
        {
            if (x._type == y._type)
            {
                if (x._type == ExObjType.BOOL)
                {
                    res = x.GetBool() == y.GetBool();
                }
                else
                {
                    res = x.GetInt() == y.GetInt();
                }
            }
            else
            {
                if (x.IsNumeric() && y.IsNumeric())
                {
                    res = x.GetFloat() == y.GetFloat();
                }
                else
                {
                    res = false;
                }
            }
            return true;
        }

        public ExObjectPtr GetConditionFromInstr(ExInstr i)
        {
            return i.arg3.GetInt() != 0 ? ci._val._lits[i.arg1] : GetTargetInStack(i.arg1);
        }

        public enum ExFallback
        {
            OK,
            NOMATCH,
            ERROR,
            DONT = 999
        }

        public bool Setter(ExObjectPtr self, ExObjectPtr k, ref ExObjectPtr v, ExFallback f)
        {
            switch (self._type)
            {
                case ExObjType.DICT:
                    {
                        if (self._val.d_Dict == null)
                        {
                            throw new Exception("attempted to access null dictionary");
                        }

                        if (!self._val.d_Dict.ContainsKey(k.GetString()))
                        {
                            self._val.d_Dict.Add(k.GetString(), new());
                        }
                        self._val.d_Dict[k.GetString()].Assign(v);
                        return true;
                    }
                case ExObjType.ARRAY:
                    {
                        if (k.IsNumeric())
                        {
                            if (self._val.l_List == null)
                            {
                                throw new Exception("attempted to access null array");
                            }

                            int n = k.GetInt();
                            int l = self._val.l_List.Count;
                            if (Math.Abs(n) < l)
                            {
                                if (n < 0)
                                {
                                    n = l + n;
                                }
                                self._val.l_List[n].Assign(v);
                                return true;
                            }
                            else
                            {
                                throw new Exception("array index error: count " + self._val.l_List.Count + " idx: " + k.GetInt());
                            }
                        }
                        throw new Exception("can't index array with " + k._type.ToString());
                    }
                case ExObjType.INSTANCE:
                    {
                        if (self._val._Instance == null)
                        {
                            throw new Exception("attempted to access null instance");
                        }

                        ExObjectPtr res = new();
                        if (self._val._Instance._class._members.TryGetValue(k.GetString(), out res) && res.IsField())
                        {
                            self._val._Instance._values[res.GetMemberID()].Assign(new ExObjectPtr(v));
                            return true;
                        }
                        return false;
                    }
                case ExObjType.STRING:
                    {
                        if (k.IsNumeric())
                        {
                            int n = k.GetInt();
                            int l = self.GetString().Length;
                            if (Math.Abs(n) < l)
                            {
                                if (n < 0)
                                {
                                    n = l + n;
                                }
                                self.SetString(self.GetString().Substring(0, n) + k.GetString() + self.GetString()[n..l]);
                                return true;
                            }
                            throw new Exception("string index error. count " + self.GetString().Length + " idx " + k.GetInt());
                        }
                        break;
                    }
                default:
                    break;
            }

            switch (SetterFallback(self, k, ref v))
            {
                case ExFallback.OK:
                    return true;
                case ExFallback.NOMATCH:
                    break;
                case ExFallback.ERROR:
                    return false;
            }

            if (f == ExFallback.OK)
            {
                if (_rootdict._val.d_Dict.ContainsKey(k.GetString()))
                {
                    _rootdict._val.d_Dict[k.GetString()].Assign(v);
                    return true;
                }
            }

            throw new Exception("key error: " + k.GetString());
        }
        public ExFallback SetterFallback(ExObjectPtr self, ExObjectPtr k, ref ExObjectPtr v)
        {
            switch (self._type)
            {
                case ExObjType.DICT:
                    {
                        if (self._val._Deleg != null && self._val._Deleg._delegate != null)
                        {
                            if (Setter(self._val._Deleg._delegate, k, ref v, ExFallback.DONT))
                            {
                                return ExFallback.OK;
                            }
                        }
                        else
                        {
                            return ExFallback.NOMATCH;
                        }
                        goto case ExObjType.INSTANCE;
                    }
                case ExObjType.INSTANCE:
                case ExObjType.USERDATA:
                    {
                        ExObjectPtr cls = null;
                        ExObjectPtr t = new();
                        if (self._val._Deleg != null && self._val._Deleg.GetMetaM(this, ExMetaM.SET, ref cls))
                        {
                            Push(self);
                            Push(k);
                            Push(v);
                            _nmetacalls++;
                            //TO-DO Auto dec metacalls
                            if (Call(ref cls, 3, _top - 3, ref t))
                            {
                                Pop(3);
                                return ExFallback.OK;
                            }
                            else
                            {
                                Pop(3);
                            }
                        }
                        break;
                    }
            }
            return ExFallback.NOMATCH;
        }

        public bool InvokeDefaultDeleg(ExObjectPtr self, ExObjectPtr k, ref ExObjectPtr dest)
        {
            Dictionary<string, ExObjectPtr> del = new();
            switch (self._type)
            {
                case ExObjType.CLASS:
                    {
                        del = _sState._class_del._val.d_Dict;
                        break;
                    }
                case ExObjType.INSTANCE:
                    {
                        del = _sState._inst_del._val.d_Dict;
                        break;
                    }
                case ExObjType.DICT:
                    {
                        del = _sState._dict_del._val.d_Dict;
                        break;
                    }
                case ExObjType.ARRAY:
                    {
                        del = _sState._list_del._val.d_Dict;
                        break;
                    }
                case ExObjType.STRING:
                    {
                        del = _sState._str_del._val.d_Dict;
                        break;
                    }
                case ExObjType.INTEGER:
                case ExObjType.FLOAT:
                case ExObjType.BOOL:
                    {
                        del = _sState._num_del._val.d_Dict;
                        break;
                    }
                case ExObjType.CLOSURE:
                case ExObjType.NATIVECLOSURE:
                    {
                        del = _sState._closure_del._val.d_Dict;
                        break;
                    }
                case ExObjType.WEAKREF:
                    {
                        del = _sState._wref_del._val.d_Dict;
                        break;
                    }
            }
            return del.TryGetValue(k.GetString(), out dest);
        }

        public ExFallback GetterFallback(ExObjectPtr self, ExObjectPtr k, ref ExObjectPtr dest)
        {
            switch (self._type)
            {
                case ExObjType.USERDATA:
                case ExObjType.DICT:
                    {
                        if (self._val._Deleg != null && self._val._Deleg._delegate != null)
                        {
                            if (Getter(ref self._val._Deleg._delegate, ref k, ref dest, false, ExFallback.DONT))
                            {
                                return ExFallback.OK;
                            }
                        }
                        else
                        {
                            return ExFallback.NOMATCH;
                        }
                        goto case ExObjType.INSTANCE;
                    }
                case ExObjType.INSTANCE:
                    {
                        ExObjectPtr cls = null;
                        if (self._val._Deleg != null && self._val._Deleg.GetMetaM(this, ExMetaM.GET, ref cls))
                        {
                            Push(self);
                            Push(k);
                            _nmetacalls++;
                            //TO-DO Auto dec metacalls
                            if (Call(ref cls, 2, _top - 2, ref dest))
                            {
                                Pop(2);
                                return ExFallback.OK;
                            }
                            else
                            {
                                Pop(2);
                            }
                        }
                        break;
                    }
            }
            return ExFallback.NOMATCH;
        }
        public bool Getter(ref ExObjectPtr self, ref ExObjectPtr k, ref ExObjectPtr dest, bool raw, ExFallback f)
        {
            switch (self._type)
            {
                case ExObjType.DICT:
                    {
                        if (self._val.d_Dict == null)
                        {
                            throw new Exception("attempted to access null dictionary");
                        }

                        if (self._val.d_Dict.ContainsKey(k.GetString()))
                        {
                            dest.Assign(new ExObjectPtr(self._val.d_Dict[k.GetString()]));
                            return true;
                        }

                        break;
                    }
                case ExObjType.ARRAY:
                    {
                        if (k.IsNumeric())
                        {
                            if (self._val.l_List == null)
                            {
                                throw new Exception("attempted to access null array");
                            }

                            if (self._val.l_List.Count != 0 && self._val.l_List.Count > k.GetInt())
                            {
                                dest.Assign(new ExObjectPtr(self._val.l_List[k.GetInt()]));
                                return true;
                            }
                            else
                            {
                                return false;
                                //throw new Exception("array index error: count " + self._val.l_List.Count + " idx: " + k.GetInt());
                            }
                        }
                        break;
                    }
                case ExObjType.INSTANCE:
                    {
                        if (self._val._Instance == null)
                        {
                            throw new Exception("attempted to access null instance");
                        }

                        if (self._val._Instance._class._members.ContainsKey(k.GetString()))
                        {
                            dest.Assign(new ExObjectPtr(self._val._Instance._class._members[k.GetString()]));
                            if (dest.IsField())
                            {
                                ExObjectPtr o = new(self._val._Instance._values[dest.GetMemberID()]);
                                dest.Assign(o._type == ExObjType.WEAKREF ? o._val._WeakRef.obj : o);
                            }
                            else
                            {
                                dest.Assign(new ExObjectPtr(self._val._Instance._class._methods[dest.GetMemberID()].val));
                            }
                            return true;
                        }
                        break;
                    }
                case ExObjType.CLASS:
                    {
                        if (self._val._Class == null)
                        {
                            throw new Exception("attempted to access null class");
                        }
                        if (self._val._Class._members.ContainsKey(k.GetString()))
                        {
                            dest.Assign(new ExObjectPtr(self._val._Class._members[k.GetString()]));
                            if (dest.IsField())
                            {
                                ExObjectPtr o = new(self._val._Class._defvals[dest.GetMemberID()].val);
                                dest.Assign(o._type == ExObjType.WEAKREF ? o._val._WeakRef.obj : o);
                            }
                            else
                            {
                                dest.Assign(new ExObjectPtr(self._val._Class._methods[dest.GetMemberID()].val));
                            }
                            return true;
                        }
                        break;
                    }
                case ExObjType.STRING:
                    {
                        if (k.IsNumeric())   // TO-DO stack index is wrong
                        {
                            int n = k.GetInt();
                            if (Math.Abs(n) < self.GetString().Length)
                            {
                                if (n < 0)
                                {
                                    n = self.GetString().Length + n;
                                }
                                dest = new ExObjectPtr(self.GetString()[n].ToString());
                                return true;
                            }
                            throw new Exception("string index error. count " + self.GetString().Length + " idx " + k.GetInt());
                        }
                        break;
                    }
                default:
                    break;
            }

            if (!raw)
            {
                switch (GetterFallback(self, k, ref dest))
                {
                    case ExFallback.OK:
                        return true;
                    case ExFallback.NOMATCH:
                        break;
                    case ExFallback.ERROR:
                        return false;
                }
                if (InvokeDefaultDeleg(self, k, ref dest))
                {
                    return true;
                }
            }
            if (f == ExFallback.OK)
            {
                if (_rootdict._val.d_Dict.TryGetValue(k.GetString(), out dest))
                {
                    return true;
                }
            }

            return false;
        }

        public ExObjectPtr GetTargetInStack(ExInstr i)
        {
            return _stack[_stackbase + i.arg0.GetInt()];
        }
        public ExObjectPtr GetTargetInStack(int i)
        {
            return _stack[_stackbase + i];
        }

        public ExObjectPtr GetTargetInStack(ExInt i)
        {
            return _stack[_stackbase + i.GetInt()];
        }
        public static void SwapObjects(ExObjectPtr x, ref ExObjectPtr y)
        {
            ExObjType t = x._type;
            ExObjVal v = x._val;
            x._type = y._type;
            x._val = y._val;
            y._type = t;
            y._val = v;
        }

        private static bool IncludesType(int t1, int t2)
        {
            return (t1 & t2) != 0;
        }

        public void CloseOuters(int idx)
        {
            ExOuter o;
            while ((o = _openouters) != null && idx > 0)
            {
                o._v = o._valptr;
                o._valptr = o._v;
                _openouters = o._next;
                idx--;
                o.Release();
            }
        }

        public bool EnterFrame(int newb, int newt, bool tail)
        {
            if (!tail)
            {
                if (_callstacksize == _alloccallsize)
                {
                    _alloccallsize *= 2;
                    ExUtils.ExpandListTo(_callsstack, _alloccallsize);
                }

                ci = Node<ExCallInfo>.BuildNodesFromList(_callsstack, _callstacksize++);

                ci._val._prevbase = newb - _stackbase;
                ci._val._prevtop = _top - _stackbase;
                ci._val.n_calls = 1;
                ci._val._traps = 0;
                ci._val._root = false;
            }
            else
            {
                ci._val.n_calls++;
            }

            _stackbase = newb;
            _top = newt;

            if (newt + 15 > _stack.Count)
            {
                if (_nmetacalls > 0)
                {
                    throw new Exception("stack overflow, cant resize while in metamethod");
                }
                ExUtils.ExpandListTo(_stack, 15 << 2);
                // TO-DO Check if reloacteouters is needed
            }
            return true;
        }

        public void LeaveFrame()
        {
            int last_t = _top;
            int last_b = _stackbase;
            int css = --_callstacksize;

            ci._val._closure.Nullify();
            _stackbase -= ci._val._prevbase;
            _top = _stackbase + ci._val._prevtop;

            if (css > 0)
            {
                ci._val = _callsstack[css - 1];
            }
            else
            {
                ci._val = null;
            }

            if (_openouters != null)
            {
                CloseOuters(last_b);
            }

            while (last_t >= _top)
            {
                _stack[last_t--].Nullify();
            }
        }

        public bool CallNative(ExNativeClosure cls, int narg, int newb, ref ExObjectPtr o)
        {
            int nparamscheck = cls.n_paramscheck;
            int new_top = newb + narg + cls.n_outervals;

            if (_nnativecalls + 1 > 100)
            {
                throw new Exception("Native stack overflow");
            }

            if ((nparamscheck > 0 && nparamscheck != narg) || (nparamscheck < 0 && narg < -nparamscheck))
            {
                throw new Exception("wrong number of parameters");
            }

            List<int> ts = cls._typecheck;
            int t_n = ts.Count;
            if (t_n > 0)
            {
                for (int i = 0; i < narg && i < t_n; i++)
                {
                    if (ts[i] != -1 && !IncludesType((int)_stack[newb + i]._type, ts[i]))
                    {
                        throw new Exception("invalid parameter type, expected: type("+ ts[i].ToString()+") got: "+ _stack[newb + i]._type.ToString());
                    }
                }
            }
            if (!EnterFrame(newb, new_top, false))
            {
                return false;
            }

            ci._val._closure = cls;

            int outers = cls.n_outervals;
            for (int i = 0; i < outers; i++)
            {
                _stack[newb + narg + i].Assign(cls._outervals[i]);
            }

            if (cls._envweakref != null)
            {
                _stack[newb].Assign(cls._envweakref.obj);
            }

            _nnativecalls++;
            int ret = cls._func.Invoke(this);
            _nnativecalls--;

            if (ret < 0)
            {
                LeaveFrame();
                throw new Exception("returned < 0 from native func");
            }

            if (ret == 0)
            {
                // o.Nullify(); // TODO: Stops rest of the instructions
            }
            else
            {
                o.Assign(_stack[_top - 1]);
            }
            LeaveFrame();
            return true;
        }

        public bool Call(ref ExObjectPtr cls, int nparams, int stackbase, ref ExObjectPtr o)
        {
            switch (cls._type)
            {
                case ExObjType.CLOSURE:
                    {
                        return Exec(cls, nparams, stackbase, ref o);
                    }
                case ExObjType.NATIVECLOSURE:
                    {
                        return CallNative(cls._val._NativeClosure, nparams, stackbase, ref o);
                    }
                case ExObjType.CLASS:
                    {
                        ExObjectPtr cn = new();
                        ExObjectPtr tmp = new();

                        CreateClassInst(cls._val._Class, ref o, cn);
                        if (cn._type != ExObjType.NULL)
                        {
                            _stack[stackbase].Assign(o);
                            return Call(ref cn, nparams, stackbase, ref tmp);
                        }
                        return true;
                    }
                default:
                    return false;
            }

        }

    }
}
