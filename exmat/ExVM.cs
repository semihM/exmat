using System;
using System.Collections.Generic;
using System.Numerics;
using ExMat.API;
using ExMat.BaseLib;
using ExMat.Class;
using ExMat.Closure;
using ExMat.FuncPrototype;
using ExMat.InfoVar;
using ExMat.Objects;
using ExMat.OPs;
using ExMat.States;
using ExMat.Utils;

namespace ExMat.VM
{
    public class ExVM
    {
        public readonly DateTime StartingTime = DateTime.Now;

        public ExSState _sState = new();

        public List<ExObject> _stack;
        public int _stackbase;
        public int _top;

        public List<ExCallInfo> _callsstack;
        public Node<ExCallInfo> ci;
        public int _alloccallsize;
        public int _callstacksize;

        public ExObject _rootdict;

        public ExOuter _openouters;

        public int _nnativecalls;
        public int _nmetacalls;

        public ExObject tmpreg = new();

        public List<ExTrap> _traps = new();

        public string _error;

        public bool _got_input = false;

        public bool _printed = false;

        public ExObject _lastreturn = new();
        public int n_return = 0;
        public bool b_main = true;

        public bool _exited = false;
        public int _exitcode = 0;

        public bool isInteractive = false;

        public void Print(string str)
        {
            Console.Write(str);
            _printed = true;
        }

        public void PrintLine(string str)
        {
            Console.WriteLine(str);
            _printed = true;
        }

        public void AddToErrorMessage(string msg)
        {
            if (string.IsNullOrEmpty(_error))
            {
                _error = "[ERROR]" + msg;
            }
            else
            {
                _error += "\n[ERROR]" + msg;
            }
        }

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
            ExBaseLib.RegisterStdBase(this);

        }

        public bool ToString(ExObject obj,
                             ref ExObject res,
                             int maxdepth = 2,
                             bool dval = false,
                             bool beauty = false,
                             string prefix = "")
        {
            switch (obj._type)
            {
                case ExObjType.COMPLEX:
                    {
                        res = new(obj.GetComplexString());
                        break;
                    }
                case ExObjType.INTEGER:
                    {
                        res = new(obj.GetInt().ToString());
                        break;
                    }
                case ExObjType.FLOAT:
                    {
                        double r = obj.GetFloat();
                        if (r % 1 == 0.0)
                        {
                            if (r < 1e+14)
                            {
                                res = new(obj.GetFloat().ToString());
                            }
                            else
                            {
                                res = new(obj.GetFloat().ToString("E14"));
                            }
                        }
                        else if (r >= (double)1e-14)
                        {
                            res = new(obj.GetFloat().ToString("0.00000000000000"));
                        }
                        else if (r < 1e+14)
                        {
                            res = new(obj.GetFloat().ToString());
                        }
                        else
                        {
                            res = new(obj.GetFloat().ToString("E14"));
                        }
                        break;
                    }
                case ExObjType.STRING:
                    {
                        res = maxdepth <= 1 || dval ? new("\"" + obj.GetString() + "\"") : new(obj.GetString());
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
                        if (maxdepth == 0)
                        {
                            res = new("ARRAY(" + (obj._val.l_List == null ? "empty" : obj._val.l_List.Count) + ")");
                            break;
                        }
                        ExObject temp = new(string.Empty);
                        string s = "[";
                        int n = 0;
                        int c = obj._val.l_List.Count;
                        maxdepth--;

                        if (beauty && !dval && c > 0)
                        {
                            if (prefix != string.Empty)
                            {
                                s = "\n" + prefix + s;
                            }
                        }

                        foreach (ExObject o in obj._val.l_List)
                        {
                            ToString(o, ref temp, maxdepth, !beauty, beauty, prefix + " ");

                            string ts = temp.GetString();
                            if (beauty && !dval)
                            {
                                if (ts.Length < 4)
                                {
                                    ts = (new string(' ', 8 - ts.Length)) + ts;
                                }
                                s += prefix + ts;
                            }
                            else
                            {
                                s += ts;
                            }

                            n++;
                            if (n != c)
                            {
                                s += ", ";
                            }
                        }

                        if (beauty && !dval)
                        {
                            if (prefix == string.Empty)
                            {
                                s += "]";
                            }
                            else
                            {
                                s += prefix + "]";
                            }
                        }
                        else
                        {
                            s += "]";
                        }

                        res = new(s);
                        break;
                    }
                case ExObjType.DICT:
                    {
                        if (maxdepth == 0)
                        {
                            res = new("DICT(" + (obj._val.l_List == null ? "empty" : obj._val.l_List.Count) + ")");
                            break;
                        }
                        ExObject temp = new(string.Empty);
                        string s = "{";
                        int n = 0;
                        int c = obj._val.d_Dict.Count;
                        if (beauty && c > 0)
                        {
                            if (prefix != string.Empty)
                            {
                                s = "\n" + prefix + s;
                            }
                        }
                        if (c > 0)
                        {
                            s += "\n\t";
                        }

                        maxdepth--;
                        foreach (KeyValuePair<string, ExObject> pair in obj._val.d_Dict)
                        {
                            ToString(pair.Value, ref temp, maxdepth, true, beauty, prefix + "\t");

                            if (beauty)
                            {
                                s += prefix + pair.Key + " = " + temp.GetString();
                            }
                            else
                            {
                                s += pair.Key + " = " + temp.GetString();
                            }

                            n++;

                            if (n != c)
                            {
                                s += "\n\t";
                                if (beauty)
                                {
                                    s += prefix;
                                }
                            }
                            else
                            {
                                s += "\n";
                                if (beauty)
                                {
                                    s += prefix;
                                }
                            }
                        }
                        s += "}";

                        res = new(s);
                        break;
                    }
                case ExObjType.NATIVECLOSURE:
                    {
                        string s = obj._type.ToString() + "(" + obj._val._NativeClosure._name.GetString() + ", ";
                        int n = obj._val._NativeClosure.n_paramscheck;
                        if (n < 0)
                        {
                            int tnc = obj._val._NativeClosure._typecheck.Count;
                            if (tnc == 0)
                            {
                                s += "min:" + (-n - 1) + " params";
                            }
                            else
                            {
                                s += (tnc - 1) + " params (min:" + (-n - 1) + ")";
                            }
                        }
                        else if (n > 0)
                        {
                            s += (n - 1) + " params";
                        }
                        else
                        {
                            s += "<=" + (obj._val._NativeClosure._typecheck.Count - 1) + " params";
                        }

                        s += ")";

                        res = new(s);
                        break;
                    }
                case ExObjType.CLOSURE:
                    {
                        ExFuncPro tmp = obj._val._Closure._func;
                        string s = string.Empty;
                        switch (tmp.type)
                        {
                            case ExClosureType.FUNCTION:
                                {
                                    string name = tmp._name.GetString();
                                    s = string.IsNullOrWhiteSpace(name) ? "LAMBDA(" : "FUNCTION(" + name + ", ";

                                    if (tmp.n_defparams > 0)
                                    {
                                        s += (tmp.n_params - 1) + " params (min:" + (tmp.n_params - tmp.n_defparams - 1) + "))";
                                    }
                                    else if (tmp._pvars)
                                    {
                                        s += "vargs, min:" + (tmp.n_params - 2) + " params)";
                                    }
                                    else
                                    {
                                        s += (tmp.n_params - 1) + " params)";
                                    }
                                    break;
                                }
                            case ExClosureType.RULE:
                                {
                                    s = "RULE(" + tmp._name.GetString() + ", ";
                                    s += (tmp.n_params - 1) + " params)";
                                    break;
                                }
                            case ExClosureType.MACRO:
                                {
                                    s = "MACRO(" + tmp._name.GetString() + ", ";
                                    if (tmp.n_defparams > 0)
                                    {
                                        s += (tmp.n_params - 1) + " params (min:" + (tmp.n_params - tmp.n_defparams - 1) + ")";
                                    }
                                    else
                                    {
                                        s += ")";
                                    }
                                    break;
                                }
                            case ExClosureType.CLUSTER:
                                {
                                    s = "CLUSTER(" + tmp._name.GetString() + ", ";
                                    s += (tmp.n_params - 1) + " params)";
                                    break;
                                }
                            case ExClosureType.SEQUENCE:
                                {
                                    s = "SEQUENCE(" + tmp._name.GetString() + ", 1 params)";
                                    break;
                                }
                        }

                        res = new(s);
                        break;
                    }
                default:
                    {
                        if (obj.IsDelegable())
                        {
                            ExObject c = new();

                            if (obj.GetInstance().GetMetaM(this, ExMetaM.STRING, ref c))
                            {
                                Push(obj);
                                return CallMeta(ref c, ExMetaM.STRING, 1, ref res);
                            }
                        }
                        res = new(obj._type.ToString());
                        break;
                    }
            }
            return true;
        }
        public bool ToFloat(ExObject obj, ref ExObject res)
        {
            switch (obj._type)
            {
                case ExObjType.COMPLEX:
                    {
                        if (obj.GetComplex().Imaginary != 0.0)
                        {
                            AddToErrorMessage("can't parse non-zero imaginary part complex number as float");
                            return false;
                        }
                        res = new(obj.GetComplex().Real);
                        break;
                    }
                case ExObjType.INTEGER:
                    {
                        res = new(obj.GetInt());
                        break;
                    }
                case ExObjType.FLOAT:
                    {
                        res = new(obj.GetFloat());
                        break;
                    }
                case ExObjType.STRING:
                    {
                        if (double.TryParse(obj.GetString(), out double r))
                        {
                            res = new(r);
                        }
                        else
                        {
                            AddToErrorMessage("failed to parse string as double");
                            return false;
                        }
                        break;
                    }
                case ExObjType.BOOL:
                    {
                        res = new((double)(obj._val.b_Bool ? 1.0 : 0.0));
                        break;
                    }
                default:
                    {
                        AddToErrorMessage("failed to parse " + obj._type.ToString() + " as double");
                        return false;
                    }
            }
            return true;
        }
        public bool ToInteger(ExObject obj, ref ExObject res)
        {
            switch (obj._type)
            {
                case ExObjType.COMPLEX:
                    {
                        if (obj.GetComplex().Imaginary != 0.0)
                        {
                            AddToErrorMessage("can't parse non-zero imaginary part complex number as integer");
                            return false;
                        }
                        res = new((long)obj.GetComplex().Real);
                        break;
                    }
                case ExObjType.INTEGER:
                    {
                        res = new(obj.GetInt());
                        break;
                    }
                case ExObjType.FLOAT:
                    {
                        res = new((int)obj.GetFloat());
                        break;
                    }
                case ExObjType.STRING:
                    {
                        if (int.TryParse(obj.GetString(), out int r))
                        {
                            res = new(r);
                        }
                        else
                        {
                            if (obj.GetString().Length == 1)
                            {
                                res = new(obj.GetString()[0]);
                            }
                            else
                            {
                                AddToErrorMessage("failed to parse string as integer");
                                return false;
                            }
                        }
                        break;
                    }
                case ExObjType.BOOL:
                    {
                        res = new(obj._val.b_Bool ? 1 : 0);
                        break;
                    }
                default:
                    {
                        AddToErrorMessage("failed to parse " + obj._type.ToString() + " as integer");
                        return false;
                    }
            }
            return true;
        }

        public bool NewSlotA(ExObject self, ExObject key, ExObject val, ExObject attrs, bool bstat, bool braw)
        {
            if (self._type != ExObjType.CLASS)
            {
                AddToErrorMessage("object has to be a class");
                return false;
            }

            ExClass cls = self._val._Class;

            if (!braw)
            {
                ExObject meta = cls._metas[(int)ExMetaM.NEWMEMBER];
                if (meta._type != ExObjType.NULL)
                {
                    Push(self);
                    Push(key);
                    Push(val);
                    Push(attrs);
                    Push(bstat);
                    return CallMeta(ref meta, ExMetaM.NEWMEMBER, 5, ref tmpreg);
                }
            }

            if (!NewSlot(self, key, val, bstat))
            {
                AddToErrorMessage("failed to create a slot named '" + key + "'");
                return false;
            }

            if (attrs._type != ExObjType.NULL)
            {
                cls.SetAttrs(key, attrs);
            }
            return true;
        }

        public bool NewSlot(ExObject self, ExObject key, ExObject val, bool bstat)
        {
            if (key._type == ExObjType.NULL)
            {
                AddToErrorMessage("'null' can't be used as index");
                return false;
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
                            ExObject v = new();
                            v.Assign(val);
                            if (self._val.d_Dict.ContainsKey(key.GetString()))
                            {
                                self._val.d_Dict[key.GetString()].Assign(v);    // TO-DO should i really allow this ?
                            }
                            else
                            {
                                self._val.d_Dict.Add(key.GetString(), new(v));
                            }
                        }
                        break;
                    }
                case ExObjType.INSTANCE:
                    {
                        AddToErrorMessage("instances don't support new slots");
                        return false;
                    }
                case ExObjType.CLASS:
                    {
                        if (!self._val._Class.NewSlot(_sState, key, val, bstat))
                        {
                            if (self._val._Class._islocked)
                            {
                                AddToErrorMessage("can't modify a class that has already been instantianted");
                            }
                            else
                            {
                                AddToErrorMessage(key.GetString() + " already exists");
                            }
                            return false;
                        }
                        break;
                    }
                default:
                    {
                        AddToErrorMessage("indexing " + self._type.ToString() + " with " + key._type.ToString());
                        return false;
                    }
            }
            return true;
        }

        public void Pop(long n)
        {
            Pop((int)n);
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
        public ExObject PopGet()
        {
            return _stack[--_top];
        }

        public void PushParse(List<ExObject> o)
        {
            foreach (ExObject ob in o)
            {
                Push(ob);
            }
        }
        public void Push(string o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(Complex o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(int o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(long o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(double o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(bool o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(ExObject o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(Dictionary<string, ExObject> o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(List<ExObject> o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(ExInstance o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(ExClass o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(ExClosure o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(ExNativeClosure o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(ExOuter o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(ExWeakRef o)
        {
            _stack[_top++].Assign(o);
        }

        public void Push(ExFuncPro o)
        {
            _stack[_top++].Assign(o);
        }

        public void PushNull()
        {
            _stack[_top++].Nullify();
        }

        public ExObject Top()
        {
            return _stack[_top - 1];
        }

        public ExObject GetAbove(int n)
        {
            return _stack[_top + n];
        }
        public ExObject GetAt(int n)
        {
            return _stack[n];
        }

        public ExObject CreateString(string s, int len = -1)
        {
            if (!_sState._strings.ContainsKey(s))
            {
                ExObject str = new() { _type = ExObjType.STRING };
                str.SetString(s);

                _sState._strings.Add(s, str);
                return str;
            }
            return _sState._strings[s];
        }

        public static bool CreateClassInst(ExClass cls, ref ExObject o, ExObject cns)
        {
            o.Assign(cls.CreateInstance());
            if (!cls.GetConstructor(ref cns))
            {
                cns.Nullify();
            }
            return true;
        }

        private bool DoClusterParamChecks(ExClosure cls, List<ExObject> lis)
        {
            int t_n = cls._defparams.Count;

            ExFuncPro pro = cls._func;
            List<ExObject> ts = cls._defparams;
            int nargs = lis.Count;

            if (t_n > 0)
            {
                if (t_n == 1)
                {
                    if (!IsInSpace(new(lis), ts[0]._val.c_Space, 1, false))
                    {
                        return false;
                    }
                }
                else
                {
                    if (t_n != nargs)
                    {
                        AddToErrorMessage("'" + pro._name.GetString() + "' takes " + (t_n) + " arguments");
                        return false;
                    }

                    for (int i = 0; i < nargs; i++)
                    {
                        if (!IsInSpace(lis[i], ts[i]._val.c_Space, i + 1, false))
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        private bool DoClusterParamChecks(ExClosure cls, int nargs, int sbase)
        {
            int t_n = cls._defparams.Count;

            ExFuncPro pro = cls._func;
            List<ExObject> ts = cls._defparams;

            if (t_n > 0)
            {
                if (t_n != nargs - 1)
                {
                    AddToErrorMessage("'" + pro._name.GetString() + "' takes " + (t_n) + " arguments");
                    return false;
                }

                for (int i = 0; i < nargs && i < t_n; i++)
                {
                    if (!IsInSpace(_stack[sbase + i + 1], ts[i]._val.c_Space, i + 1))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool StartCall(ExClosure cls, long trg, long args, long sbase, bool tail)
        {
            return StartCall(cls, (int)trg, (int)args, (int)sbase, tail);
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
                    AddToErrorMessage("'" + pro._name.GetString() + "' takes at least " + (p - 1) + " arguments");
                    return false;
                }

                int nvargs = nargs - p;
                List<ExObject> varglis = new();
                int pb = sbase + p;
                for (int n = 0; n < nvargs; n++)
                {
                    varglis.Add(new(_stack[pb]));
                    _stack[pb].Nullify();
                    pb++;
                }

                _stack[sbase + p].Assign(varglis);
            }
            else if (!pro.IsCluster()
                     && !pro.IsRule()
                     && !pro.IsSequence())
            {
                if (p != nargs)
                {
                    int n_def = pro.n_defparams;
                    int diff;
                    int defstart = p - n_def;
                    if (n_def > 0 && nargs < p && (diff = p - nargs) <= n_def)
                    {
                        for (int n = 1; n < p; n++)
                        {
                            if (_stack[sbase + n]._type == ExObjType.DEFAULT)
                            {
                                if (n >= defstart)
                                {
                                    _stack[sbase + n].Assign(cls._defparams[n - defstart]);
                                }
                                else
                                {
                                    AddToErrorMessage("can't use non-existant default value reference for parameter " + n);
                                    return false;
                                }
                            }
                            else if (n >= defstart)
                            {
                                _stack[sbase + n].Assign(cls._defparams[n - defstart]);
                                nargs++;
                            }
                        }
                    }
                    else
                    {
                        if (n_def > 0 && !pro.IsCluster() && !pro.IsCluster())
                        {
                            AddToErrorMessage("'" + pro._name.GetString() + "' takes min: " + (p - n_def - 1) + ", max:" + (p - 1) + " arguments");
                        }
                        else
                        {
                            AddToErrorMessage("'" + pro._name.GetString() + "' takes exactly " + (p - 1) + " arguments");
                        }
                        return false;
                    }
                }
                else
                {
                    int n_def = pro.n_defparams;
                    int defstart = p - n_def;
                    for (int n = 1; n < p; n++)
                    {
                        if (_stack[sbase + n]._type == ExObjType.DEFAULT)
                        {
                            if (n >= defstart)
                            {
                                _stack[sbase + n].Assign(cls._defparams[n - defstart]);
                            }
                            else
                            {
                                AddToErrorMessage("can't use non-existant default value reference for parameter " + n);
                                return false;
                            }
                        }
                    }
                }
            }

            if (pro.IsRule())
            {
                int t_n = pro._localinfos.Count;
                if (t_n != nargs)
                {
                    AddToErrorMessage("'" + pro._name.GetString() + "' takes " + (t_n - 1) + " arguments");
                    return false;
                }
            }
            else if (pro.IsCluster())
            {
                if (!DoClusterParamChecks(cls, nargs, sbase))
                {
                    return false;
                }
            }
            else if (pro.IsSequence())
            {
                if (nargs != 2)
                {
                    AddToErrorMessage("sequences require an argument to be called");
                    return false;
                }
                else
                {
                    if (!_stack[sbase + 1].IsNumeric())
                    {
                        AddToErrorMessage("expected integer or float as sequence argument");
                        return false;
                    }
                    else
                    {
                        long ind = _stack[sbase + 1].GetInt();
                        string idx = ind.ToString();
                        for (int i = 2; i < cls._func._params.Count; i++)
                        {
                            ExObject c = cls._func._params[i];
                            if (c.GetString() == idx)
                            {
                                // TO-DO doesnt return to main, also refactor this
                                // TO-DO optimize
                                _stack[sbase - 1].Assign(cls._defparams[i - 2]);
                                return true;
                            }
                        }
                        if (ind < 0)
                        {
                            AddToErrorMessage("index can't be negative, unless its a default value");
                            return false;
                        }
                    }
                }
            }

            if (cls._envweakref != null)
            {
                _stack[sbase].Assign(cls._envweakref.obj);
            }

            if (!EnterFrame(sbase, newt, tail))
            {
                AddToErrorMessage("failed to create a scope");
                return false;
            }

            ci._val._closure = new(); ci._val._closure.Assign(cls);
            ci._val._lits = pro._lits;
            ci._val._instrs = pro._instr;
            ci._val._idx_instrs = 0;
            ci._val._target = trg;

            return true;
        }

        public bool IsInSpace(ExObject argument, ExSpace space, int i, bool raise = true)
        {
            switch (argument._type)
            {
                case ExObjType.SPACE:   // TO-DO maybe allow spaces as arguments here ?
                    {
                        if (raise)
                        {
                            AddToErrorMessage("can't use 'CLUSTER' or 'SPACE' as an argument for parameter " + i);
                        }
                        return false;
                    }
                case ExObjType.ARRAY:
                    {
                        if (argument._val.l_List.Count != space.dim)
                        {
                            if (raise)
                            {
                                AddToErrorMessage("expected " + space.dim + " dimensions for parameter " + i);
                            }
                            return false;
                        }

                        switch (space.space)
                        {
                            case "A":
                                {
                                    return true;
                                }
                            case "r":
                                {
                                    switch (space.sign)
                                    {
                                        case '+':
                                            {
                                                foreach (ExObject val in argument._val.l_List)
                                                {
                                                    if (!val.IsNumeric() || val.GetFloat() <= 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected numeric positive non-zero values for parameter " + i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '-':
                                            {
                                                foreach (ExObject val in argument._val.l_List)
                                                {
                                                    if (!val.IsNumeric() || val.GetFloat() >= 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected numeric negative non-zero values for parameter " + i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '\\':
                                            {
                                                foreach (ExObject val in argument._val.l_List)
                                                {
                                                    if (!val.IsNumeric() || val.GetFloat() == 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected numeric non-zero values for parameter " + i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        default:
                                            {

                                                if (raise)
                                                {
                                                    AddToErrorMessage("expected + or - symbols");
                                                }
                                                return false;
                                            }
                                    }
                                    break;
                                }
                            case "R":
                                {
                                    switch (space.sign)
                                    {
                                        case '+':
                                            {
                                                foreach (ExObject val in argument._val.l_List)
                                                {
                                                    if (!val.IsNumeric() || val.GetFloat() < 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected numeric positive or zero values for parameter " + i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '-':
                                            {
                                                foreach (ExObject val in argument._val.l_List)
                                                {
                                                    if (!val.IsNumeric() || val.GetFloat() > 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected numeric negative or zero values for parameter " + i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '\\':
                                            {
                                                foreach (ExObject val in argument._val.l_List)
                                                {
                                                    if (!val.IsNumeric())
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected numeric values for parameter " + i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        default:
                                            {
                                                if (raise)
                                                {
                                                    AddToErrorMessage("expected + or - symbols");
                                                }
                                                return false;
                                            }
                                    }
                                    break;
                                }
                            case "Z":
                                {
                                    switch (space.sign)
                                    {
                                        case '+':
                                            {
                                                foreach (ExObject val in argument._val.l_List)
                                                {
                                                    if (val._type != ExObjType.INTEGER || val.GetFloat() < 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected integer positive or zero values for parameter " + i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '-':
                                            {
                                                foreach (ExObject val in argument._val.l_List)
                                                {
                                                    if (val._type != ExObjType.INTEGER || val.GetFloat() > 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected integer negative or zero values for parameter " + i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '\\':
                                            {
                                                foreach (ExObject val in argument._val.l_List)
                                                {
                                                    if (val._type != ExObjType.INTEGER)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected integer values for parameter " + i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        default:
                                            {
                                                if (raise)
                                                {
                                                    AddToErrorMessage("expected + or - symbols");
                                                }
                                                return false;
                                            }
                                    }
                                    break;
                                }
                            case "z":
                                {
                                    switch (space.sign)
                                    {
                                        case '+':
                                            {
                                                foreach (ExObject val in argument._val.l_List)
                                                {
                                                    if (val._type != ExObjType.INTEGER || val.GetInt() <= 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected integer positive non-zero values for parameter " + i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '-':
                                            {
                                                foreach (ExObject val in argument._val.l_List)
                                                {
                                                    if (val._type != ExObjType.INTEGER || val.GetInt() >= 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected integer negative non-zero values for parameter " + i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '\\':
                                            {
                                                foreach (ExObject val in argument._val.l_List)
                                                {
                                                    if (val._type != ExObjType.INTEGER || val.GetInt() == 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected integer non-zero values for parameter " + i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        default:
                                            {
                                                if (raise)
                                                {
                                                    AddToErrorMessage("expected + or - symbols");
                                                }
                                                return false;
                                            }
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                case ExObjType.INTEGER:
                case ExObjType.FLOAT:
                    {
                        if (space.dim != 1)
                        {
                            if (raise)
                            {
                                AddToErrorMessage("expected " + space.dim + " dimensions for parameter " + i);
                            }
                            return false;
                        }
                        switch (space.space)
                        {
                            case "A":
                                {
                                    return true;
                                }
                            case "r":
                                {
                                    switch (space.sign)
                                    {
                                        case '+':
                                            {
                                                if (!argument.IsNumeric() || argument.GetFloat() <= 0)
                                                {
                                                    if (raise)
                                                    {
                                                        AddToErrorMessage("expected numeric positive non-zero value for parameter " + i);
                                                    }
                                                    return false;
                                                }
                                                break;
                                            }
                                        case '-':
                                            {
                                                if (!argument.IsNumeric() || argument.GetFloat() >= 0)
                                                {
                                                    if (raise)
                                                    {
                                                        AddToErrorMessage("expected numeric negative non-zero value for parameter " + i);
                                                    }
                                                    return false;
                                                }
                                                break;
                                            }
                                        case '\\':
                                            {
                                                if (!argument.IsNumeric() || argument.GetFloat() == 0)
                                                {

                                                    if (raise)
                                                    {
                                                        AddToErrorMessage("expected numeric non-zero value for parameter " + i);
                                                    }
                                                    return false;
                                                }
                                                break;
                                            }
                                        default:
                                            {
                                                if (raise)
                                                {
                                                    AddToErrorMessage("expected + or - symbols");
                                                }
                                                return false;
                                            }
                                    }
                                    break;
                                }
                            case "R":
                                {
                                    switch (space.sign)
                                    {
                                        case '+':
                                            {
                                                if (!argument.IsNumeric() || argument.GetFloat() < 0)
                                                {
                                                    if (raise)
                                                    {
                                                        AddToErrorMessage("expected numeric positive or zero value for parameter " + i);
                                                    }
                                                    return false;
                                                }
                                                break;
                                            }
                                        case '-':
                                            {
                                                if (!argument.IsNumeric() || argument.GetFloat() > 0)
                                                {
                                                    if (raise)
                                                    {
                                                        AddToErrorMessage("expected numeric negative or zero value for parameter " + i);
                                                    }
                                                    return false;
                                                }
                                                break;
                                            }
                                        case '\\':
                                            {
                                                if (!argument.IsNumeric())
                                                {
                                                    if (raise)
                                                    {
                                                        AddToErrorMessage("expected numeric value for parameter " + i);
                                                    }
                                                    return false;
                                                }
                                                break;
                                            }
                                        default:
                                            {
                                                if (raise)
                                                {
                                                    AddToErrorMessage("expected + or - symbols");
                                                }
                                                return false;
                                            }
                                    }
                                    break;
                                }
                            case "Z":
                                {
                                    switch (space.sign)
                                    {
                                        case '+':
                                            {
                                                if (argument._type != ExObjType.INTEGER || argument.GetInt() < 0)
                                                {
                                                    if (raise)
                                                    {
                                                        AddToErrorMessage("expected integer positive or zero value for parameter " + i);
                                                    }
                                                    return false;
                                                }
                                                break;
                                            }
                                        case '-':
                                            {
                                                if (argument._type != ExObjType.INTEGER || argument.GetInt() > 0)
                                                {
                                                    if (raise)
                                                    {
                                                        AddToErrorMessage("expected integer negative or zero value for parameter " + i);
                                                    }
                                                    return false;
                                                }
                                                break;
                                            }
                                        case '\\':
                                            {
                                                if (argument._type != ExObjType.INTEGER)
                                                {
                                                    if (raise)
                                                    {
                                                        AddToErrorMessage("expected integer value for parameter " + i);
                                                    }
                                                    return false;
                                                }
                                                break;
                                            }
                                        default:
                                            {
                                                if (raise)
                                                {
                                                    AddToErrorMessage("expected + or - symbols");
                                                }
                                                return false;
                                            }
                                    }
                                    break;
                                }
                            case "z":
                                {
                                    switch (space.sign)
                                    {
                                        case '+':
                                            {
                                                if (argument._type != ExObjType.INTEGER || argument.GetInt() <= 0)
                                                {
                                                    if (raise)
                                                    {
                                                        AddToErrorMessage("expected integer positive non-zero value for parameter " + i);
                                                    }
                                                    return false;
                                                }
                                                break;
                                            }
                                        case '-':
                                            {
                                                if (argument._type != ExObjType.INTEGER || argument.GetInt() >= 0)
                                                {
                                                    if (raise)
                                                    {
                                                        AddToErrorMessage("expected integer negative non-zero value for parameter " + i);
                                                    }
                                                    return false;
                                                }
                                                break;
                                            }
                                        case '\\':
                                            {
                                                if (argument._type != ExObjType.INTEGER || argument.GetInt() == 0)
                                                {
                                                    if (raise)
                                                    {
                                                        AddToErrorMessage("expected integer non-zero value for parameter " + i);
                                                    }
                                                    return false;
                                                }
                                                break;
                                            }
                                        default:
                                            {
                                                if (raise)
                                                {
                                                    AddToErrorMessage("expected + or - symbols");
                                                }
                                                return false;
                                            }
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                case ExObjType.NULL:
                    {
                        return false;
                    }
                default:
                    {
                        return false;
                    }
            }
            return true;
        }

        private ExObject FindSpaceObject(int i)
        {
            string name = ci._val._lits[i].GetString();
            return new(ExSpace.GetSpaceFromString(name));
        }

        public bool FixStackAfterError()
        {
            while (ci._val != null)
            {
                if (ci._val._traps > 0)
                {
                    // TO-DO traps
                }

                bool end = ci._val != null && ci._val._root;
                if (!LeaveFrame())
                {
                    throw new Exception("something went wrong with the stack!");
                }
                if (end)
                {
                    break;
                }
            }

            return false;
        }

        public bool Exec(ExObject cls, int narg, int stackbase, ref ExObject o)
        {
            if (_nnativecalls + 1 > 100)
            {
                throw new Exception("Native stack overflow");
            }

            _nnativecalls++;

            Node<ExCallInfo> prevci = ci;
            int traps = 0;

            // TO-DO Exec types
            tmpreg = new(cls);
            if (!StartCall(tmpreg._val._Closure, _top - narg, narg, stackbase, false))
            {
                //AddToErrorMessage("no calls found");
                return false;
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
                if (ci._val == null || ci._val._instrs == null)
                {
                    return true;
                }

                if (ci._val._idx_instrs >= ci._val._instrs.Count || ci._val._idx_instrs < 0)
                {
                    return false;
                    //throw new Exception("instruction index error");
                }


                ExInstr i = ci._val._instrs[ci._val._idx_instrs++];
                switch (i.op)
                {
                    case OPC.LINE:
                        continue;
                    case OPC.LOAD:
                        {
                            GetTargetInStack(i).Assign(ci._val._lits[(int)i.arg1]);
                            continue;
                        }
                    case OPC.LOAD_INT:
                        {
                            GetTargetInStack(i).Assign(i.arg1);
                            continue;
                        }
                    case OPC.LOAD_FLOAT:
                        {
                            GetTargetInStack(i).Assign(new FloatInt() { i = i.arg1 }.f);
                            continue;
                        }
                    case OPC.LOAD_COMPLEX:
                        {
                            if (i.arg2.GetInt() == 1)
                            {
                                GetTargetInStack(i).Assign(new Complex(0.0, new FloatInt() { i = i.arg1 }.f));
                            }
                            else
                            {
                                GetTargetInStack(i).Assign(new Complex(0.0, i.arg1));
                            }
                            continue;
                        }
                    case OPC.LOAD_BOOL:
                        {
                            GetTargetInStack(i).Assign(i.arg1 == 1);
                            continue;
                        }
                    case OPC.LOAD_SPACE:
                        {
                            GetTargetInStack(i).Assign(FindSpaceObject((int)i.arg1));
                            continue;
                        }
                    case OPC.DLOAD:
                        {
                            GetTargetInStack(i).Assign(ci._val._lits[(int)i.arg1]);
                            GetTargetInStack(i.arg2.GetInt()).Assign(ci._val._lits[(int)i.arg3.GetInt()]);
                            continue;
                        }
                    case OPC.CALL_TAIL:
                        {
                            ExObject tmp = GetTargetInStack(i.arg1);
                            if (tmp._type == ExObjType.CLOSURE)
                            {
                                ExObject c = new(tmp);
                                if (_openouters != null)
                                {
                                    CloseOuters(_stackbase);
                                }
                                for (int j = 0; j < i.arg3.GetInt(); j++)
                                {
                                    GetTargetInStack(j).Assign(GetTargetInStack(i.arg2.GetInt() + j));
                                }
                                if (!StartCall(c._val._Closure, ci._val._target, i.arg3.GetInt(), _stackbase, true))
                                {
                                    //AddToErrorMessage("guarded failed call");
                                    return FixStackAfterError();
                                }
                                continue;
                            }
                            goto case OPC.CALL;
                        }
                    case OPC.CALL:
                        {
                            ExObject tmp2 = new(GetTargetInStack(i.arg1));
                            switch (tmp2._type)
                            {
                                case ExObjType.CLOSURE:
                                    {
                                        if (!StartCall(tmp2._val._Closure, i.arg0.GetInt(), i.arg3.GetInt(), _stackbase + i.arg2.GetInt(), false))
                                        {
                                            //AddToErrorMessage("guarded failed call");
                                            return FixStackAfterError();
                                        }
                                        continue;
                                    }
                                case ExObjType.NATIVECLOSURE:
                                    {
                                        if (!CallNative(tmp2._val._NativeClosure, i.arg3.GetInt(), _stackbase + i.arg2.GetInt(), ref tmp2))
                                        {
                                            //AddToErrorMessage("guarded failed call");
                                            return FixStackAfterError();
                                        }

                                        if (i.arg0.GetInt() != 985)
                                        {
                                            GetTargetInStack(i.arg0).Assign(tmp2);
                                        }
                                        continue;
                                    }
                                case ExObjType.CLASS:
                                    {
                                        ExObject instance = new();
                                        if (!CreateClassInst(tmp2._val._Class, ref instance, tmp2))
                                        {
                                            //AddToErrorMessage("guarded failed call");
                                            return FixStackAfterError();
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
                                                    sbase = _stackbase + (int)i.arg2.GetInt();
                                                    _stack[sbase].Assign(instance);
                                                    if (!StartCall(tmp2._val._Closure, -1, i.arg3.GetInt(), sbase, false))
                                                    {
                                                        //AddToErrorMessage("guarded failed call");
                                                        return FixStackAfterError();
                                                    }
                                                    break;
                                                }
                                            case ExObjType.NATIVECLOSURE:
                                                {
                                                    sbase = _stackbase + (int)i.arg2.GetInt();
                                                    _stack[sbase].Assign(instance);
                                                    if (!CallNative(tmp2._val._NativeClosure, i.arg3.GetInt(), sbase, ref tmp2))
                                                    {
                                                        //AddToErrorMessage("guarded failed call");
                                                        return FixStackAfterError();
                                                    }
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                case ExObjType.DICT:
                                    {
                                        goto default;
                                    }
                                case ExObjType.INSTANCE:
                                    {
                                        ExObject cls2 = null;
                                        if (tmp2.GetInstance().GetMetaM(this, ExMetaM.CALL, ref cls2))
                                        {
                                            Push(tmp2);
                                            for (int j = 0; j < i.arg3.GetInt(); j++)
                                            {
                                                Push(GetTargetInStack(j + i.arg2.GetInt()));
                                            }

                                            if (!CallMeta(ref cls2, ExMetaM.CALL, i.arg3.GetInt() + 1, ref tmp2))
                                            {
                                                AddToErrorMessage("meta method failed call");
                                                return FixStackAfterError();
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
                                    {
                                        AddToErrorMessage("attempt to call " + tmp2._type.ToString());
                                        return FixStackAfterError();
                                    }
                            }
                            continue;
                        }
                    case OPC.PREPCALL:
                    case OPC.PREPCALLK:
                        {
                            ExObject k = i.op == OPC.PREPCALLK ? ci._val._lits[(int)i.arg1] : GetTargetInStack(i.arg1);
                            ExObject obj = GetTargetInStack(i.arg2);

                            if (!Getter(ref obj, ref k, ref tmpreg, false, (ExFallback)i.arg2.GetInt()))
                            {
                                AddToErrorMessage("unknown method or field '" + k.GetString() + "'");
                                return FixStackAfterError();
                            }

                            GetTargetInStack(i.arg3).Assign(obj);
                            SwapObjects(GetTargetInStack(i), ref tmpreg);
                            continue;
                        }
                    case OPC.GETK:
                        {
                            ExObject tmp = GetTargetInStack(i.arg2);
                            ExObject lit = ci._val._lits[(int)i.arg1];

                            if (!Getter(ref tmp, ref lit, ref tmpreg, false, (ExFallback)i.arg2.GetInt()))
                            {
                                AddToErrorMessage("unknown variable '" + lit.GetString() + "'"); // access to local var decl before
                                return FixStackAfterError();
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
                                //AddToErrorMessage("guarded failed newslot");
                                return FixStackAfterError();
                            }
                            if (i.arg0.GetInt() != 985)
                            {
                                GetTargetInStack(i).Assign(GetTargetInStack(i.arg3));
                            }
                            continue;
                        }
                    case OPC.DELETE:
                        {
                            ExObject r = new(GetTargetInStack(i));
                            if (!RemoveObjectSlot(GetTargetInStack(i.arg1), GetTargetInStack(i.arg2), ref r))
                            {
                                AddToErrorMessage("failed to delete a slot");
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case OPC.SET:
                        {
                            ExObject t = new(GetTargetInStack(i.arg3));
                            if (!Setter(GetTargetInStack(i.arg1), GetTargetInStack(i.arg2), ref t, ExFallback.OK))
                            {
                                AddToErrorMessage("failed setter for '" + GetTargetInStack(i.arg2).GetString() + "' key");
                                return FixStackAfterError();
                            }
                            if (i.arg0.GetInt() != 985)
                            {
                                GetTargetInStack(i).Assign(GetTargetInStack(i.arg3));
                            }
                            continue;
                        }
                    case OPC.GET:
                        {
                            ExObject s1 = new(GetTargetInStack(i.arg1));
                            ExObject s2 = new(GetTargetInStack(i.arg2));
                            if (!Getter(ref s1, ref s2, ref tmpreg, false, (ExFallback)i.arg1))
                            {
                                //AddToErrorMessage("failed getter for '" + s2.GetString() + "' key");
                                return FixStackAfterError();
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
                                AddToErrorMessage("equal op failed");
                                return FixStackAfterError();
                            }
                            GetTargetInStack(i).Assign(i.op == OPC.EQ ? res : !res);
                            continue;
                        }
                    case OPC.ADD:
                    case OPC.SUB:
                    case OPC.MLT:
                    case OPC.EXP:
                    case OPC.DIV:
                    case OPC.MOD:
                        {
                            ExObject res = new();
                            if (!DoArithmeticOP(i.op, GetTargetInStack(i.arg2), GetTargetInStack(i.arg1), ref res))
                            {
                                return FixStackAfterError();
                            }
                            GetTargetInStack(i).Assign(res);
                            continue;
                        }
                    case OPC.MMLT:
                        {
                            ExObject res = new();
                            if (!DoMatrixMltOP(OPC.MMLT, GetTargetInStack(i.arg2), GetTargetInStack(i.arg1), ref res))
                            {
                                return FixStackAfterError();
                            }
                            GetTargetInStack(i).Assign(res);
                            continue;
                        }
                    case OPC.CARTESIAN:
                        {
                            ExObject res = new();
                            if (!DoCartesianProductOP(GetTargetInStack(i.arg2), GetTargetInStack(i.arg1), ref res))
                            {
                                return FixStackAfterError();
                            }
                            GetTargetInStack(i).Assign(res);
                            continue;
                        }
                    case OPC.BITWISE:
                        {
                            if (!DoBitwiseOP(i.arg3.GetInt(), GetTargetInStack(i.arg2), GetTargetInStack(i.arg1), GetTargetInStack(i)))
                            {
                                return FixStackAfterError();
                            }

                            continue;
                        }
                    case OPC.RETURNBOOL:
                    case OPC.RETURN:
                        {
                            if (ReturnValue((int)i.arg0.GetInt(), (int)i.arg1, ref tmpreg, i.op == OPC.RETURNBOOL))
                            {
                                SwapObjects(o, ref tmpreg);
                                return true;
                            }
                            continue;
                        }
                    case OPC.LOAD_NULL:
                        {
                            if (i.arg2.GetInt() == 1)
                            {
                                for (int n = 0; n < i.arg1; n++)
                                {
                                    GetTargetInStack(i.arg0.GetInt() + n).Nullify();
                                    GetTargetInStack(i.arg0.GetInt() + n).Assign(new ExObject() { _type = ExObjType.DEFAULT });
                                }
                            }
                            else
                            {
                                for (int n = 0; n < i.arg1; n++)
                                {
                                    GetTargetInStack(i.arg0.GetInt() + n).Nullify();
                                }
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
                    case OPC.JZS:
                        {
                            if (!GetTargetInStack(i.arg0).GetBool())
                            {
                                ci._val._idx_instrs += (int)i.arg1;
                            }
                            continue;
                        }
                    case OPC.JMP:
                        {
                            ci._val._idx_instrs += (int)i.arg1;
                            continue;
                        }
                    case OPC.JCMP:
                        {
                            if (!DoCompareOP((CmpOP)i.arg3.GetInt(), GetTargetInStack(i.arg2), GetTargetInStack(i.arg0), tmpreg))
                            {
                                return FixStackAfterError();
                            }
                            if (!tmpreg.GetBool())
                            {
                                ci._val._idx_instrs += (int)i.arg1;
                            }
                            continue;
                        }
                    case OPC.JZ:
                        {
                            if (!GetTargetInStack(i.arg0).GetBool())
                            {
                                ci._val._idx_instrs += (int)i.arg1;
                            }
                            continue;
                        }
                    case OPC.GETOUTER:
                        {
                            ExClosure currcls = ci._val._closure.GetClosure();
                            ExOuter outr = currcls._outervals[(int)i.arg1]._val._Outer;
                            GetTargetInStack(i).Assign(outr._valptr);
                            continue;
                        }
                    case OPC.SETOUTER:
                        {
                            ExClosure currcls = ci._val._closure.GetClosure();
                            ExOuter outr = currcls._outervals[(int)i.arg1]._val._Outer;
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
                                        GetTargetInStack(i).Assign(new Dictionary<string, ExObject>());
                                        continue;
                                    }
                                case (int)ExNOT.ARRAY:
                                    {
                                        GetTargetInStack(i).Assign(new List<ExObject>((int)i.arg1));
                                        continue;
                                    }
                                case (int)ExNOT.CLASS:
                                    {
                                        if (!DoClassOP(GetTargetInStack(i), (int)i.arg1, (int)i.arg2.GetInt()))
                                        {
                                            AddToErrorMessage("failed to create class");
                                            return FixStackAfterError();
                                        }
                                        continue;
                                    }
                                default:
                                    {
                                        AddToErrorMessage("unknown object type " + i.arg3.GetInt());
                                        return FixStackAfterError();
                                    }
                            }
                        }
                    case OPC.ARRAY_APPEND:
                        {
                            ExObject val = new();
                            switch (i.arg2.GetInt())
                            {
                                case (int)ArrayAType.STACK:
                                    val.Assign(GetTargetInStack(i.arg1)); break;
                                case (int)ArrayAType.LITERAL:
                                    val.Assign(ci._val._lits[(int)i.arg1]); break;
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
                    case OPC.TRANSPOSE:
                        {
                            ExObject s1 = new(GetTargetInStack(i.arg1));
                            if (!DoMatrixTranspose(GetTargetInStack(i), ref s1, (ExFallback)i.arg1))
                            {
                                return false;
                            }
                            continue;
                        }
                    case OPC.INC:
                    case OPC.PINC:
                        {
                            ExObject ob = new(i.arg3);

                            ExObject s1 = new(GetTargetInStack(i.arg1));
                            ExObject s2 = new(GetTargetInStack(i.arg2));
                            if (!DoDerefInc(OPC.ADD, GetTargetInStack(i), ref s1, ref s2, ref ob, i.op == OPC.PINC, (ExFallback)i.arg1))
                            {
                                throw new Exception(i.op + " failed");
                            }
                            continue;
                        }
                    case OPC.INCL:
                    case OPC.PINCL:
                        {
                            ExObject ob = GetTargetInStack(i.arg1);
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
                                    ExObject res = new();
                                    if (!DoArithmeticOP(OPC.ADD, ob, o, ref res))
                                    {
                                        return FixStackAfterError();
                                    }
                                    ob.Assign(res);
                                }
                                else
                                {
                                    ExObject targ = new(GetTargetInStack(i));
                                    ExObject val = new(GetTargetInStack(i.arg1));
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
                            ExObject s1 = new(GetTargetInStack(i.arg1));
                            ExObject s2 = new(GetTargetInStack(i.arg2));

                            GetTargetInStack(i).Assign(Getter(ref s1, ref s2, ref tmpreg, true, ExFallback.DONT, true));

                            continue;
                        }
                    case OPC.CMP:
                        {
                            if (!DoCompareOP((CmpOP)i.arg3.GetInt(), GetTargetInStack(i.arg2), GetTargetInStack(i.arg1), GetTargetInStack(i)))
                            {
                                return FixStackAfterError();
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
                                CloseOuters((int)GetTargetInStack(i.arg1).GetInt());
                            }
                            continue;
                        }
                    case OPC.AND:
                        {
                            if (!GetTargetInStack(i.arg2).GetBool())
                            {
                                GetTargetInStack(i).Assign(GetTargetInStack(i.arg2));
                                ci._val._idx_instrs += (int)i.arg1;
                            }
                            continue;
                        }
                    case OPC.OR:
                        {
                            if (GetTargetInStack(i.arg2).GetBool())
                            {
                                GetTargetInStack(i).Assign(GetTargetInStack(i.arg2));
                                ci._val._idx_instrs += (int)i.arg1;
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
                            if (!DoNegateOP(GetTargetInStack(i), GetTargetInStack(i.arg1)))
                            {
                                AddToErrorMessage("attempted to negate '" + GetTargetInStack(i.arg1)._type.ToString() + "'");
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case OPC.CLOSURE:
                        {
                            ExClosure cl = ci._val._closure.GetClosure();
                            ExFuncPro fp = cl._func;
                            if (!DoClosureOP(GetTargetInStack(i), fp._funcs[(int)i.arg1]))
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
                    case OPC.CMP_ARTH:
                        {
                            // TO-DO somethings wrong here
                            int idx = (int)((i.arg1 & 0xFFFF0000) >> 16);

                            ExObject si = GetTargetInStack(idx);
                            ExObject s2 = GetTargetInStack(i.arg2);
                            ExObject s1v = GetTargetInStack(i.arg1 & 0x0000FFFF);

                            if (!DoDerefInc((OPC)i.arg3.GetInt(), GetTargetInStack(i), ref si, ref s2, ref s1v, false, (ExFallback)idx))
                            {
                                throw new Exception("compound arithmetic failed");
                            }
                            continue;
                        }
                    case OPC.TYPEOF:
                        {
                            GetTargetInStack(i).Assign(GetTargetInStack(i.arg1)._type.ToString());
                            continue;
                        }
                    case OPC.INSTANCEOF:
                        {
                            if (GetTargetInStack(i.arg1)._type != ExObjType.CLASS)
                            {
                                AddToErrorMessage("instanceof operation can only be done with a 'class' type");
                                return FixStackAfterError();
                            }
                            GetTargetInStack(i).Assign(
                                GetTargetInStack(i.arg2)._type == ExObjType.INSTANCE
                                && GetTargetInStack(i.arg2)._val._Instance.IsInstanceOf(GetTargetInStack(i.arg1)._val._Class));
                            continue;
                        }
                    case OPC.RETURNMACRO:   // TO-DO
                        {
                            if (ReturnValue((int)i.arg0.GetInt(), (int)i.arg1, ref tmpreg, false, true))
                            {
                                SwapObjects(o, ref tmpreg);
                                return true;
                            }
                            continue;
                        }
                    case OPC.GETBASE:
                        {
                            ExClosure c = ci._val._closure._val._Closure;
                            if (c._base != null)
                            {
                                GetTargetInStack(i).Assign(c._base);
                            }
                            else
                            {
                                GetTargetInStack(i).Nullify();
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

        public bool RemoveObjectSlot(ExObject self, ExObject k, ref ExObject r)
        {
            switch (self._type)
            {
                case ExObjType.DICT:
                case ExObjType.INSTANCE:
                    {
                        ExObject cls = new();
                        ExObject tmp;

                        // TO-DO allow dict deleg ?
                        if (self._type == ExObjType.INSTANCE && self.GetInstance().GetMetaM(this, ExMetaM.DELSLOT, ref cls))
                        {
                            Push(self);
                            Push(k);
                            return CallMeta(ref cls, ExMetaM.DELSLOT, 2, ref r);
                        }
                        else
                        {
                            if (self._type == ExObjType.DICT)
                            {
                                if (self._val.d_Dict.ContainsKey(k.GetString()))
                                {
                                    tmp = new(self._val.d_Dict[k.GetString()]);

                                    self._val.d_Dict.Remove(k.GetString());
                                }
                                else
                                {
                                    AddToErrorMessage(k.GetString() + " doesn't exist");
                                    return false;
                                }
                            }
                            else
                            {
                                AddToErrorMessage("can't delete a slot from " + self._type.ToString());
                                return false;
                            }
                        }

                        r = tmp;
                        break;
                    }
                case ExObjType.ARRAY:
                    {
                        if (!k.IsNumeric())
                        {
                            AddToErrorMessage("can't use non-numeric index for removing");
                            return false;
                        }
                        else if (self.GetList() == null)
                        {
                            AddToErrorMessage("can't remove from null list");
                            return false;
                        }
                        else if (self.GetList().Count <= k.GetInt() || k.GetInt() < 0)
                        {
                            AddToErrorMessage("array index error: count " + self.GetList().Count + ", index " + k.GetInt());
                            return false;
                        }
                        else
                        {
                            self.GetList()[(int)k.GetFloat()].Release();
                            self.GetList().RemoveAt((int)k.GetInt());
                            return true;
                        }
                    }
                default:
                    {
                        AddToErrorMessage("can't delete a slot from " + self._type.ToString());
                        return false;
                    }
            }
            return true;
        }

        public void FindOuterVal(ExObject target, ExObject sidx)
        {
            if (_openouters == null)
            {
                _openouters = new();
            }

            ExOuter tmp;
            while (_openouters._valptr != null && _openouters._valptr.GetInt() >= sidx.GetInt())
            {
                if (_openouters._valptr.GetInt() == sidx.GetInt())
                {
                    target.Assign(new ExObject(_openouters));
                    return;
                }
                _openouters = _openouters._next;
            }

            tmp = ExOuter.Create(_sState, sidx);
            tmp._next = _openouters;
            tmp.idx = (int)sidx.GetInt() - FindFirstNullInStack();
            tmp._val._RefC._refc++;
            _openouters.Assign(tmp);
            target.Assign(new ExObject(tmp));
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

        public bool DoClosureOP(ExObject t, ExFuncPro fp)
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
                                cl._outervals[i].Assign(ci._val._closure._val._Closure._outervals[(int)ov._src.GetInt()]);
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

        public static bool DoNegateOP(ExObject target, ExObject val)
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
                case ExObjType.COMPLEX:
                    {
                        target.Assign(-val.GetComplex());
                        return true;
                    }
                case ExObjType.DICT:
                case ExObjType.INSTANCE:
                    {
                        //TO-DO
                        return false;
                    }
            }
            // Attempt to negate val._type
            return false;
        }

        public bool DoMatrixTranspose(ExObject t, ref ExObject mat, ExFallback idx)
        {
            if (mat._type != ExObjType.ARRAY)
            {
                AddToErrorMessage("expected matrix for transpose op");
                return false;
            }

            List<ExObject> vals = mat.GetList();
            int rows = vals.Count;
            int cols = 0;

            if (!ExAPI.DoMatrixTransposeChecks(this, vals, ref cols))
            {
                return false;
            }

            t.Assign(ExAPI.TransposeMatrix(rows, cols, vals));

            return true;
        }

        public bool DoDerefInc(OPC op, ExObject t, ref ExObject self, ref ExObject k, ref ExObject inc, bool post, ExFallback idx)
        {
            ExObject tmp = new();
            ExObject tmpk = k;
            ExObject tmps = self;
            if (!Getter(ref self, ref tmpk, ref tmp, false, idx))
            {
                return false;
            }

            if (!DoArithmeticOP(op, tmp, inc, ref t))
            {
                return false;
            }

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

        public bool DoVarInc(OPC op, ref ExObject t, ref ExObject o, ref ExObject diff)
        {
            ExObject res = new();
            if (!DoArithmeticOP(op, o, diff, ref res))
            {
                return false;
            }
            t.Assign(o);
            o.Assign(res);
            return true;
        }

        public bool DoClassOP(ExObject target, int bcls, int attr)
        {
            ExClass cb = null;
            ExObject atrs = new();
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
            if (target._val._Class._metas[(int)ExMetaM.INHERIT]._type != ExObjType.NULL)
            {
                int np = 2;
                ExObject r = new();
                Push(target);
                Push(atrs);
                ExObject mm = target._val._Class._metas[(int)ExMetaM.INHERIT];
                Call(ref mm, np, _top - np, ref r);
                Pop(np);
            }
            target._val._Class._attrs.Assign(atrs);
            return true;
        }

        private bool InnerDoCompareOP(ExObject a, ExObject b, ref int t)
        {
            ExObjType at = a._type;
            ExObjType bt = b._type;
            if (at == ExObjType.COMPLEX || bt == ExObjType.COMPLEX)
            {
                AddToErrorMessage("can't compare complex numbers");
                return false;
            }
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
                    AddToErrorMessage("failed to compare " + at.ToString() + " and " + bt.ToString());
                    return false;
                }
            }
        }
        public bool DoCompareOP(CmpOP cop, ExObject a, ExObject b, ExObject res)
        {
            int t = 0;
            if (InnerDoCompareOP(a, b, ref t))
            {
                switch (cop)
                {
                    case CmpOP.GRT:
                        res.Assign(t > 0); return true;
                    case CmpOP.GET:
                        res.Assign(t >= 0); return true;
                    case CmpOP.LST:
                        res.Assign(t < 0); return true;
                    case CmpOP.LET:
                        res.Assign(t <= 0); return true;
                }
            }
            return false;
        }

        public bool ReturnValue(int a0, int a1, ref ExObject res, bool make_bool = false, bool mac = false)
        {
            bool r = ci._val._root;
            int cbase = _stackbase - ci._val._prevbase;

            ExObject p;
            if (r)
            {
                p = res;
            }
            else if (ci._val._target == -1)
            {
                p = new();
            }
            else
            {
                p = _stack[cbase + ci._val._target];
            }

            if (p._type != ExObjType.NULL || _forcereturn)
            {
                if (a0 != 985)
                {
                    if (mac)
                    {
                        p.Assign(_stack[_stackbase - a0]);
                    }
                    else
                    {
                        p.Assign(make_bool ? new(_stack[_stackbase + a1].GetBool()) : _stack[_stackbase + a1]);
                    }

                    bool seq = ci._val._closure._val._Closure._func.IsSequence();
                    if (seq)
                    {
                        ci._val._closure._val._Closure._defparams.Add(new(p));
                        ci._val._closure._val._Closure._func._params.Add(new(_stack[_stackbase+1].GetInt().ToString()));
                    }

                    if (!LeaveFrame(seq))
                    {
                        throw new Exception("something went wrong with the stack!");
                    }
                    bool rets = (_lastreturn._type == ExObjType.NULL || n_return > 0);

                    // TO-DO instances and vars are 1 idx off
                    if (b_main && ((ci._val == null && rets) || (ci._val != null && ci._val._root)) && rets) // return for main
                    {
                        _lastreturn.Assign(p);
                        n_return--;
                    }
                    return r;
                }
                else
                {
                    p.Nullify();
                }
            }

            if (!LeaveFrame())
            {
                throw new Exception("something went wrong with the stack!");
            }
            return r;
        }

        public bool DoBitwiseOP(long iop, ExObject a, ExObject b, ExObject res)
        {
            int a_mask = (int)a._type | (int)b._type;
            if (a_mask == (int)ExObjType.INTEGER)
            {
                switch ((BitOP)iop)
                {
                    case BitOP.AND:
                        res.Assign(a.GetInt() & b.GetInt()); break;
                    case BitOP.OR:
                        res.Assign(a.GetInt() | b.GetInt()); break;
                    case BitOP.XOR:
                        res.Assign(a.GetInt() ^ b.GetInt()); break;
                    case BitOP.SHIFTL:
                        res.Assign(a.GetInt() << (int)b.GetInt()); break;
                    case BitOP.SHIFTR:
                        res.Assign(a.GetInt() >> (int)b.GetInt()); break;
                    default:
                        {
                            throw new Exception("unknown bitwise operation");
                        }
                }
            }
            else
            {
                AddToErrorMessage("bitwise op between '" + a._type.ToString() + "' and '" + b._type.ToString() + "'");
                return false;
            }
            return true;
        }
        private static bool InnerDoArithmeticOPInt(OPC op, long a, long b, ref ExObject res)
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
                            res = new(a > 0 ? double.PositiveInfinity : (a == 0 ? double.NaN : double.NegativeInfinity));
                            //AddToErrorMessage("division by zero");
                            break;
                        }

                        res = new(a / b); break;
                    }
                case OPC.MOD:
                    {
                        if (b == 0)
                        {
                            res = new(a > 0 ? double.PositiveInfinity : (a == 0 ? double.NaN : double.NegativeInfinity));
                            //AddToErrorMessage("modulo by zero");
                            break;
                        }

                        res = new(a % b); break;
                    }
                case OPC.EXP:
                    {
                        res = new(Math.Pow(a, b)); break;
                    }
                default:
                    {
                        throw new Exception("unknown arithmetic operation");
                    }
            }
            return true;
        }
        
        private static bool InnerDoArithmeticOPComplex(OPC op, Complex a, Complex b, ref ExObject res)
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
                        Complex c = Complex.Divide(a, b);
                        c = new Complex(Math.Round(c.Real, 15), Math.Round(c.Imaginary, 15));
                        res = new(c);
                        break;
                    }
                case OPC.EXP:
                    {
                        Complex c = Complex.Pow(a, b);
                        c = new Complex(Math.Round(c.Real, 15), Math.Round(c.Imaginary, 15));
                        res = new(c);
                        break;
                    }
                case OPC.MOD:
                    {
                        return false;
                    }
                default:
                    {
                        throw new Exception("unknown arithmetic operation");
                    }
            }
            return true;
        }

        private static bool InnerDoArithmeticOPComplex(OPC op, Complex a, double b, ref ExObject res)
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
                        Complex c = Complex.Divide(a, b);
                        c = new Complex(Math.Round(c.Real, 15), Math.Round(c.Imaginary, 15));
                        res = new(c);
                        break;
                    }
                case OPC.EXP:
                    {
                        Complex c = Complex.Pow(a, b);
                        c = new Complex(Math.Round(c.Real, 15), Math.Round(c.Imaginary, 15));
                        res = new(c);
                        break;
                    }
                case OPC.MOD:
                    {

                        return false;
                    }
                default:
                    {
                        throw new Exception("unknown arithmetic operation");
                    }
            }
            return true;
        }

        private static bool InnerDoArithmeticOPComplex(OPC op, double a, Complex b, ref ExObject res)
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
                        Complex c = Complex.Divide(a, b);
                        c = new Complex(Math.Round(c.Real, 15), Math.Round(c.Imaginary, 15));
                        res = new(c);
                        break;
                    }
                case OPC.EXP:
                    {
                        Complex c = Complex.Pow(a, b);
                        c = new Complex(Math.Round(c.Real, 15), Math.Round(c.Imaginary, 15));
                        res = new(c);
                        break;
                    }
                case OPC.MOD:
                    {
                        
                        return false;
                    }
                default:
                    {
                        throw new Exception("unknown arithmetic operation");
                    }
            }
            return true;
        }

        private static bool InnerDoArithmeticOPFloat(OPC op, double a, double b, ref ExObject res)
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
                            res = new(a > 0 ? double.PositiveInfinity : (a == 0 ? double.NaN : double.NegativeInfinity));
                            //AddToErrorMessage("division by zero");
                            break;
                        }

                        res = new(a / b); break;
                    }
                case OPC.MOD:
                    {
                        if (b == 0)
                        {
                            res = new(a > 0 ? double.PositiveInfinity : (a == 0 ? double.NaN : double.NegativeInfinity));
                            //AddToErrorMessage("modulo by zero");
                            break;
                        }

                        res = new(a % b); break;
                    }
                case OPC.EXP:
                    {
                        res = new((double)Math.Pow(a, b)); break;
                    }
                default:
                    {
                        throw new Exception("unknown arithmetic operation");
                    }
            }
            return true;
        }

        public bool DoMatrixMltChecks(List<ExObject> M, ref int cols)
        {
            cols = 0;
            foreach (ExObject row in M)
            {
                if (row._type != ExObjType.ARRAY)
                {
                    AddToErrorMessage("given list have to contain lists");
                    return false;
                }
                else
                {
                    foreach (ExObject num in row.GetList())
                    {
                        if (!num.IsNumeric())
                        {
                            AddToErrorMessage("given list have to contain lists of numeric values");
                            return false;
                        }
                    }

                    if (cols != 0 && row.GetList().Count != cols)
                    {
                        AddToErrorMessage("given list have varying length of lists");
                        return false;
                    }
                    else
                    {
                        cols = row.GetList().Count;
                    }
                }
            }

            if (cols == 0)
            {
                AddToErrorMessage("empty list can't be used for matrix multiplication");
                return false;
            }

            return true;
        }

        public bool MatrixMultiplication(List<ExObject> A, List<ExObject> B, ref ExObject res)
        {
            int rA = A.Count;
            int rB = B.Count;
            int cA = -1;
            int cB = -1;

            if (!DoMatrixMltChecks(A, ref cA) || !DoMatrixMltChecks(B, ref cB))
            {
                return false;
            }

            if (cA != rB)
            {
                AddToErrorMessage("dimensions don't match for matrix multiplication");
                return false;
            }

            List<ExObject> r = new(rA);

            for (int i = 0; i < rA; i++)
            {
                r.Add(new(new List<ExObject>(cB)));
                List<ExObject> row = A[i].GetList();

                for (int j = 0; j < cB; j++)
                {
                    double total = 0;
                    for (int k = 0; k < cA; k++)
                    {
                        total += row[k].GetFloat() * B[k]._val.l_List[j].GetFloat();
                    }

                    r[i]._val.l_List.Add(new(total));
                }

            }

            res = new(r);
            return true;
        }

        public bool DoMatrixMltOP(OPC op, ExObject a, ExObject b, ref ExObject res)
        {
            if (a._type != ExObjType.ARRAY || b._type != ExObjType.ARRAY)
            {
                AddToErrorMessage("can't do matrix multiplication with non-list types");
                return false;
            }

            return MatrixMultiplication(a.GetList(), b.GetList(), ref res);
        }

        public bool DoCartesianProductOP(ExObject a, ExObject b, ref ExObject res)
        {
            if (a._type != ExObjType.ARRAY || b._type != ExObjType.ARRAY)
            {
                AddToErrorMessage("can't get cartesian product of non-list types");
                return false;
            }

            int ac = a.GetList().Count;
            int bc = b.GetList().Count;
            List<ExObject> r = new(ac * bc);

            for (int i = 0; i < ac; i++)
            {
                ExObject ar = a._val.l_List[i];
                for (int j = 0; j < bc; j++)
                {
                    r.Add(new(new List<ExObject>(2) { new(ar), new(b._val.l_List[j]) }));
                }
            }
            res = new(r);

            return true;
        }

        public bool DoArithmeticOP(OPC op, ExObject a, ExObject b, ref ExObject res)
        {
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
                        if (!InnerDoArithmeticOPInt(op, a.GetInt(), b.GetInt(), ref res))
                        {
                            return false;
                        }
                        break;
                    }
                case (int)ArithmeticMask.INTCOMPLEX:
                    {
                        if (a._type == ExObjType.INTEGER)
                        {
                            if (!InnerDoArithmeticOPComplex(op, a.GetInt(), b.GetComplex(), ref res))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!InnerDoArithmeticOPComplex(op, a.GetComplex(), b.GetInt(), ref res))
                            {
                                return false;
                            }
                        }
                        break;
                    }
                case (int)ArithmeticMask.FLOATINT:
                case (int)ArithmeticMask.FLOAT:
                    {
                        if (!InnerDoArithmeticOPFloat(op, a.GetFloat(), b.GetFloat(), ref res))
                        {
                            return false;
                        };
                        break;
                    }
                case (int)ArithmeticMask.FLOATCOMPLEX:
                    {
                        if (a._type == ExObjType.FLOAT)
                        {
                            if (!InnerDoArithmeticOPComplex(op, a.GetFloat(), b.GetComplex(), ref res))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            if (!InnerDoArithmeticOPComplex(op, a.GetComplex(), b.GetFloat(), ref res))
                            {
                                return false;
                            }
                        }
                        break;
                    }
                case (int)ArithmeticMask.COMPLEX:
                    {
                        if (!InnerDoArithmeticOPComplex(op, a.GetComplex(), b.GetComplex(), ref res))
                        {
                            return false;
                        }
                        break;
                    }
                case (int)ArithmeticMask.STRING:
                    {
                        if (op != OPC.ADD)
                        {
                            goto default;
                        }
                        res = new(a.GetString() + b.GetString());
                        break;
                    }
                case (int)ArithmeticMask.STRINGNULL:
                    {
                        if (op != OPC.ADD)
                        {
                            goto default;
                        }
                        res = new(a._type == ExObjType.NULL ? ("null" + b.GetString()) : (a.GetString() + "null"));
                        break;
                    }
                case (int)ArithmeticMask.STRINGBOOL:
                    {
                        if (op != OPC.ADD)
                        {
                            goto default;
                        }
                        res = new(a._type == ExObjType.BOOL ? (a.GetBool().ToString().ToLower() + b.GetString()) : (a.GetString() + b.GetBool().ToString().ToLower()));
                        break;
                    }
                case (int)ArithmeticMask.STRINGCOMPLEX:
                    {
                        if (op != OPC.ADD)
                        {
                            goto default;
                        }
                        if (a._type == ExObjType.STRING)
                        {
                            res = new(a.GetString() + b.GetComplexString());
                        }
                        else
                        {
                            res = new(a.GetComplexString() + b.GetString());
                        }
                        break;
                    }
                case (int)ArithmeticMask.STRINGINT:
                case (int)ArithmeticMask.STRINGFLOAT:
                    {
                        if (op != OPC.ADD)
                        {
                            goto default;
                        }
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
                        if (DoArithmeticMetaOP(op, a, b, ref res))
                        {
                            return true;
                        }
                        AddToErrorMessage("can't do " + op.ToString() + " operation between " + a._type.ToString() + " and " + b._type.ToString());
                        return false;
                    }
            }
            return true;
        }
        public bool DoArithmeticMetaOP(OPC op, ExObject a, ExObject b, ref ExObject res)
        {
            ExMetaM meta;
            switch (op)
            {
                case OPC.ADD:
                    {
                        meta = ExMetaM.ADD;
                        break;
                    }
                case OPC.SUB:
                    {
                        meta = ExMetaM.SUB;
                        break;
                    }
                case OPC.DIV:
                    {
                        meta = ExMetaM.DIV;
                        break;
                    }
                case OPC.MLT:
                    {
                        meta = ExMetaM.MLT;
                        break;
                    }
                case OPC.MOD:
                    {
                        meta = ExMetaM.MOD;
                        break;
                    }
                case OPC.EXP:
                    {
                        meta = ExMetaM.EXP;
                        break;
                    }
                default:
                    {
                        meta = ExMetaM.ADD;
                        break;
                    }
            }
            if (a.IsDelegable())
            {
                ExObject c = new();

                if (a.GetInstance().GetMetaM(this, meta, ref c))
                {
                    Push(a);
                    Push(b);
                    return CallMeta(ref c, meta, 2, ref res);
                }
            }
            return false;
        }

        public bool CallMeta(ref ExObject cls, ExMetaM meta, long nargs, ref ExObject res)
        {
            _nmetacalls++;
            bool b = Call(ref cls, nargs, _top - nargs, ref res, true);
            _nmetacalls--;
            Pop(nargs);
            return b;
        }

        public enum ArithmeticMask
        {
            INT = ExObjType.INTEGER,
            INTCOMPLEX = ExObjType.COMPLEX | ExObjType.INTEGER,
            FLOATINT = ExObjType.INTEGER | ExObjType.FLOAT,
            FLOATCOMPLEX = ExObjType.COMPLEX | ExObjType.FLOAT,
            FLOAT = ExObjType.FLOAT,
            COMPLEX = ExObjType.COMPLEX,
            STRING = ExObjType.STRING,
            STRINGINT = ExObjType.STRING | ExObjType.INTEGER,
            STRINGFLOAT = ExObjType.STRING | ExObjType.FLOAT,
            STRINGCOMPLEX = ExObjType.STRING | ExObjType.COMPLEX,
            STRINGBOOL = ExObjType.STRING | ExObjType.BOOL,
            STRINGNULL = ExObjType.STRING | ExObjType.NULL
        }


        public static bool CheckEqual(ExObject x, ExObject y, ref bool res)
        {
            if (x._type == y._type)
            {
                switch (x._type)
                {
                    case ExObjType.BOOL:
                        res = x.GetBool() == y.GetBool();
                        break;
                    case ExObjType.STRING:
                        res = x.GetString() == y.GetString();
                        break;
                    case ExObjType.COMPLEX:
                        res = x.GetComplex() == y.GetComplex();
                        break;
                    case ExObjType.INTEGER:
                        res = x.GetInt() == y.GetInt();
                        break;
                    case ExObjType.FLOAT:
                        {
                            double xv = x.GetFloat();
                            double yv = y.GetFloat();
                            if (double.IsNaN(xv))
                            {
                                res = double.IsNaN(yv);
                            }
                            else if (double.IsNaN(yv))
                            {
                                res = double.IsNaN(xv);
                            }
                            else
                            {
                                res = x.GetFloat() == y.GetFloat();
                            }
                        }
                        break;
                    case ExObjType.NULL:
                        res = true;
                        break;
                    case ExObjType.NATIVECLOSURE:
                        CheckEqual(x._val._NativeClosure._name, y._val._NativeClosure._name, ref res);
                        break;
                    case ExObjType.CLOSURE:
                        CheckEqual(x._val._Closure._func._name, y._val._Closure._func._name, ref res);
                        break;
                    default:
                        res = x == y;   // TO-DO
                        break;
                }
            }
            else
            {
                bool bx = x.IsNumeric();
                bool by = y.IsNumeric();
                if (by && x._type == ExObjType.COMPLEX)
                {
                    res = x.GetComplex() == y.GetFloat();
                }
                else if (bx && y._type == ExObjType.COMPLEX)
                {
                    res = x.GetFloat() == y.GetComplex();
                }
                else if (bx && by)
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

        public ExObject GetConditionFromInstr(ExInstr i)
        {
            return i.arg3.GetInt() != 0 ? ci._val._lits[(int)i.arg1] : GetTargetInStack(i.arg1);
        }

        public enum ExFallback
        {
            OK,
            NOMATCH,
            ERROR,
            DONT = 999
        }

        public bool Setter(ExObject self, ExObject k, ref ExObject v, ExFallback f)
        {
            switch (self._type)
            {
                case ExObjType.DICT:
                    {
                        if (self._val.d_Dict == null)
                        {
                            AddToErrorMessage("attempted to access null dictionary");
                            return false;
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
                                AddToErrorMessage("attempted to access null array");
                                return false;
                            }

                            int n = (int)k.GetInt();
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
                                AddToErrorMessage("array index error: count " + self._val.l_List.Count + " idx: " + k.GetInt());
                                return false;
                            }
                        }
                        AddToErrorMessage("can't index array with " + k._type.ToString());
                        return false;
                    }
                case ExObjType.INSTANCE:
                    {
                        if (self._val._Instance == null)
                        {
                            AddToErrorMessage("attempted to access null instance");
                            return false;
                        }

                        if (self._val._Instance._class._members.ContainsKey(k.GetString())
                            && self._val._Instance._class._members[k.GetString()].IsField())
                        {
                            self._val._Instance._values[self._val._Instance._class._members[k.GetString()].GetMemberID()].Assign(new ExObject(v));
                            return true;
                        }
                        break;
                    }
                case ExObjType.STRING:
                    {
                        if (k.IsNumeric())
                        {
                            int n = (int)k.GetInt();
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
                            AddToErrorMessage("string index error. count " + self.GetString().Length + " idx " + k.GetInt());
                            return false;
                        }
                        break;
                    }
                case ExObjType.CLOSURE:
                    {
                        if (k._type == ExObjType.STRING)
                        {
                            foreach (ExClassMem c in self.GetClosure()._base._methods)
                            {
                                if (c.val.GetClosure()._func._name.GetString() == self.GetClosure()._func._name.GetString())
                                {
                                    if (c.attrs._type == ExObjType.DICT && c.attrs.GetDict().ContainsKey(k.GetString()))
                                    {
                                        c.attrs.GetDict()[k.GetString()].Assign(v);
                                        return true;
                                    }
                                    AddToErrorMessage("unknown attribute '" + k.GetString() + "'");
                                    return false;
                                }
                            }
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

            AddToErrorMessage("key error: " + k.GetString());
            return false;
        }
        public ExFallback SetterFallback(ExObject self, ExObject k, ref ExObject v)
        {
            switch (self._type)
            {
                case ExObjType.DICT:
                    {
                        if (self.GetInstance()._delegate != null)
                        {
                            if (Setter(self.GetInstance()._delegate, k, ref v, ExFallback.DONT))
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
                        ExObject cls = null;
                        ExObject t = new();
                        if (self.GetInstance().GetMetaM(this, ExMetaM.SET, ref cls))
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

        public bool InvokeDefaultDeleg(ExObject self, ExObject k, ref ExObject dest)
        {
            Dictionary<string, ExObject> del = new();
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
                case ExObjType.COMPLEX:
                    {
                        del = _sState._complex_del._val.d_Dict;
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
            if (del.ContainsKey(k.GetString()))
            {
                dest = new ExNativeClosure();
                dest._val._NativeClosure = (ExNativeClosure)del[k.GetString()];
                return true;
            }
            return false;
        }

        public ExFallback GetterFallback(ExObject self, ExObject k, ref ExObject dest)
        {
            switch (self._type)
            {
                case ExObjType.DICT:
                    {
                        //if (self.GetInstance()._delegate != null)
                        //{
                        //    if (Getter(ref self.GetInstance()._delegate, ref k, ref dest, false, ExFallback.DONT))
                        //    {
                        //        return ExFallback.OK;
                        //    }
                        //}
                        //else
                        //{
                        return ExFallback.NOMATCH;
                        //}
                        //goto case ExObjType.INSTANCE;
                    }
                case ExObjType.INSTANCE:
                    {
                        ExObject cls = null;
                        if (self.GetInstance().GetMetaM(this, ExMetaM.GET, ref cls))
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
        public bool Getter(ref ExObject self, ref ExObject k, ref ExObject dest, bool raw, ExFallback f, bool b_exist = false)
        {
            switch (self._type)
            {
                case ExObjType.DICT:
                    {
                        if (self._val.d_Dict == null)
                        {
                            AddToErrorMessage("attempted to access null dictionary");
                            return false;
                        }

                        if (self._val.d_Dict.ContainsKey(k.GetString()))
                        {
                            dest.Assign(new ExObject(self._val.d_Dict[k.GetString()]));
                            return true;
                        }

                        break;
                    }
                case ExObjType.ARRAY:
                    {
                        if (self._val.l_List == null)
                        {
                            AddToErrorMessage("attempted to access null array");
                            return false;
                        }

                        if (k.IsNumeric() && !b_exist)
                        {
                            if (!b_exist && self._val.l_List.Count != 0 && self._val.l_List.Count > k.GetInt())
                            {
                                dest.Assign(new ExObject(self._val.l_List[(int)k.GetInt()]));
                                return true;
                            }
                            else
                            {
                                if (!b_exist)
                                {
                                    AddToErrorMessage("array index error: count " + self._val.l_List.Count + ", idx: " + k.GetInt());
                                }
                                else
                                {
                                    bool found = false;
                                    foreach (ExObject o in self._val.l_List)
                                    {
                                        CheckEqual(o, k, ref found);
                                        if (found)
                                        {
                                            return true;
                                        }
                                    }
                                }
                                return false;
                            }
                        }
                        else if (b_exist)
                        {
                            bool found = false;
                            foreach (ExObject o in self._val.l_List)
                            {
                                CheckEqual(o, k, ref found);
                                if (found)
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                        break;
                    }
                case ExObjType.INSTANCE:
                    {
                        if (self._val._Instance == null)
                        {
                            AddToErrorMessage("attempted to access null instance");
                            return false;
                        }

                        if (self._val._Instance._class._members.ContainsKey(k.GetString()))
                        {
                            dest.Assign(new ExObject(self._val._Instance._class._members[k.GetString()]));
                            if (dest.IsField())
                            {
                                ExObject o = new(self._val._Instance._values[dest.GetMemberID()]);
                                dest.Assign(o._type == ExObjType.WEAKREF ? o._val._WeakRef.obj : o);
                            }
                            else
                            {
                                dest.Assign(new ExObject(self._val._Instance._class._methods[dest.GetMemberID()].val));
                            }
                            return true;
                        }
                        break;
                    }
                case ExObjType.CLASS:
                    {
                        if (self._val._Class == null)
                        {
                            AddToErrorMessage("attempted to access null class");
                            return false;
                        }
                        if (self._val._Class._members.ContainsKey(k.GetString()))
                        {
                            dest.Assign(new ExObject(self._val._Class._members[k.GetString()]));
                            if (dest.IsField())
                            {
                                ExObject o = new(self._val._Class._defvals[dest.GetMemberID()].val);
                                dest.Assign(o._type == ExObjType.WEAKREF ? o._val._WeakRef.obj : o);
                            }
                            else
                            {
                                dest.Assign(new ExObject(self._val._Class._methods[dest.GetMemberID()].val));
                            }
                            return true;
                        }
                        break;
                    }
                case ExObjType.STRING:
                    {
                        if (k.IsNumeric())   // TO-DO stack index is wrong
                        {
                            int n = (int)k.GetInt();
                            if (Math.Abs(n) < self.GetString().Length)
                            {
                                if (n < 0)
                                {
                                    n = self.GetString().Length + n;
                                }
                                dest = new ExObject(self.GetString()[n].ToString());
                                return true;
                            }
                            if (!b_exist)
                            {
                                AddToErrorMessage("string index error. count " + self.GetString().Length + " idx " + k.GetInt());
                            }
                            return false;
                        }
                        else if (b_exist)
                        {
                            return self.GetString().IndexOf(k.GetString()) != -1;
                        }
                        break;
                    }
                case ExObjType.SPACE:
                    {
                        if (b_exist)
                        {
                            return IsInSpace(k, self._val.c_Space, 1, false);
                        }

                        goto default;

                    }
                case ExObjType.CLOSURE:
                    {
                        if (b_exist)
                        {
                            if (!self.GetClosure()._func.IsCluster())
                            {
                                goto default;
                            }

                            List<ExObject> lis = k._type != ExObjType.ARRAY
                                    ? new() { k }
                                    : k._val.l_List;

                            if (!DoClusterParamChecks(self._val._Closure, lis))
                            {
                                return false;
                            }

                            ExObject tmp = new();
                            Push(self);
                            Push(_rootdict);

                            int nargs = 2;
                            if (self._val._Closure._defparams.Count == 1)
                            {
                                Push(lis);
                            }
                            else
                            {
                                nargs += lis.Count - 1;
                                PushParse(lis);
                            }

                            if (!Call(ref self, nargs, _top - nargs, ref tmp, true))
                            {
                                Pop(nargs + 1);
                                return false;
                            }
                            Pop(nargs + 1);
                            return tmp.GetBool();
                        }

                        if (k._type == ExObjType.STRING)
                        {
                            switch (k.GetString())
                            {
                                case "vargs":
                                    {
                                        dest = new(self.GetClosure()._func._pvars);
                                        return true;
                                    }
                                case "n_params":
                                    {
                                        dest = new(self.GetClosure()._func.n_params - 1);
                                        return true;
                                    }
                                case "n_defparams":
                                    {
                                        dest = new(self.GetClosure()._func.n_defparams);
                                        return true;
                                    }
                                case "n_minargs":
                                    {
                                        dest = new(self.GetClosure()._func.n_params - 1 - self.GetClosure()._func.n_defparams);
                                        return true;
                                    }
                                case "defparams":
                                    {
                                        int ndef = self.GetClosure()._func.n_defparams;
                                        int npar = self.GetClosure()._func.n_params - 1;
                                        int start = npar - ndef;
                                        Dictionary<string, ExObject> dict = new();
                                        foreach (ExObject d in self.GetClosure()._defparams)
                                        {
                                            dict.Add((++start).ToString(), d);
                                        }
                                        dest = new(dict);
                                        return true;
                                    }
                                default:
                                    {
                                        ExClass c = self.GetClosure()._base;

                                        string mem = self.GetClosure()._func._name.GetString();
                                        string attr = k.GetString();
                                        int memid = c._members[mem].GetMemberID();

                                        if (c._methods[memid].attrs.GetDict().ContainsKey(attr))
                                        {
                                            dest = new ExObject(c._methods[memid].attrs.GetDict()[attr]);
                                            return true;
                                        }

                                        AddToErrorMessage("unknown attribute '" + attr + "'");
                                        return false;
                                    }
                            }
                        }
                        goto default;

                    }
                case ExObjType.COMPLEX:
                    {
                        break;
                    }
                default:
                    {
                        if (!b_exist)
                        {
                            AddToErrorMessage("can't index '" + self._type.ToString() + "' with '" + k._type.ToString() + "'");
                        }
                        return false;
                    }
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
                if (_rootdict.GetDict().ContainsKey(k.GetString()))
                {
                    dest.Assign(_rootdict.GetDict()[k.GetString()]);
                    return true;
                }
            }

            return false;
        }

        public ExObject GetTargetInStack(ExInstr i)
        {
            return _stack[_stackbase + (int)i.arg0.GetInt()];
        }
        public ExObject GetTargetInStack(int i)
        {
            return _stack[_stackbase + i];
        }
        public ExObject GetTargetInStack(long i)
        {
            return _stack[_stackbase + (int)i];
        }

        public ExObject GetTargetInStack(ExObject i)
        {
            return _stack[_stackbase + (int)i.GetInt()];
        }

        public static void SwapObjects(ExObject x, ref ExObject y)
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

        public bool LeaveFrame(bool reset = false)
        {
            int last_t = _top;
            int last_b = _stackbase;
            int css = --_callstacksize;

            if (reset)
            {
                bool rets = (_lastreturn._type == ExObjType.NULL || n_return > 0);
                if (b_main && ((css <= 0 && rets) || (css > 0 && _callsstack[css - 1]._root)) && rets)
                {   // TO-DO refactor
                    List<ExObject> dp = new();
                    List<ExObject> ps = new();

                    for (int i = 0; i < ci._val._closure._val._Closure._func.n_params; i++)
                    {
                        ps.Add(new(ci._val._closure._val._Closure._func._params[i]));
                    }
                    for(int i = 0; i < ci._val._closure._val._Closure._func.n_defparams; i++)
                    {
                        dp.Add(new(ci._val._closure._val._Closure._defparams[i]));
                    }
                    ci._val._closure._val._Closure._defparams = new(dp);
                    ci._val._closure._val._Closure._func._params = new(ps);
                }
            }

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

            if (last_t >= _stack.Count)
            {
                AddToErrorMessage("stack overflow! Allocate more stack room for these operations");
                return false;
            }
            while (last_t >= _top)
            {
                if (_stack[last_t]._type == ExObjType.CLOSURE)
                {
                    if (_stack[last_t].GetClosure()._func._name._type == ExObjType.NULL) // TO-DO fix this, lambda funcs get removed
                    {
                        last_t--;
                    }
                    else
                    {
                        _stack[last_t--].Nullify();
                    }
                }
                else
                {
                    _stack[last_t--].Nullify();
                }
            }
            return true;
        }

        public bool CallNative(ExNativeClosure cls, long narg, long newb, ref ExObject o)
        {
            return CallNative(cls, (int)narg, (int)newb, ref o);
        }

        public bool CallNative(ExNativeClosure cls, int narg, int newb, ref ExObject o)
        {
            if (cls.GetNClosure() != null)  // shouldnt really happend
            {
                cls = cls.GetNClosure();
            }

            int nparamscheck = cls.n_paramscheck;
            int new_top = newb + narg + cls.n_outervals;

            if (_nnativecalls + 1 > 100)
            {
                throw new Exception("Native stack overflow");
            }

            if (((nparamscheck > 0) && (nparamscheck != narg)) ||
            ((nparamscheck < 0) && (narg < (-nparamscheck))))
            {
                if (nparamscheck < 0)
                {
                    AddToErrorMessage("'" + cls._name.GetString() + "' takes minimum " + (-nparamscheck - 1) + " arguments");
                    return false;
                }
                AddToErrorMessage("'" + cls._name.GetString() + "' takes exactly " + (nparamscheck - 1) + " arguments");
                return false;
            }

            List<int> ts = cls._typecheck;
            int t_n = ts.Count;

            if (t_n > 0)
            {
                if (nparamscheck < 0 && t_n < narg)
                {
                    AddToErrorMessage("'" + cls._name.GetString() + "' takes maximum " + (t_n - 1) + " arguments");
                    return false;
                }

                for (int i = 0; i < narg && i < t_n; i++)
                {
                    ExObjType typ = _stack[newb + i]._type;
                    if (typ == ExObjType.DEFAULT)
                    {
                        if (cls.d_defaults.ContainsKey(i))
                        {
                            _stack[newb + i].Assign(cls.d_defaults[i]);
                        }
                        else
                        {
                            AddToErrorMessage("can't use non-existant default value for parameter " + (i));
                            return false;
                        }
                    }
                    else if (ts[i] != -1 && !IncludesType((int)typ, ts[i]))
                    {
                        AddToErrorMessage("invalid parameter type, expected one of " + ExAPI.GetExpectedTypes(ts[i]) + ", got: " + _stack[newb + i]._type.ToString());
                        return false;
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
            int ret = cls._func.Invoke(this, narg - 1);
            _nnativecalls--;

            if (ret < 0)
            {
                if (!LeaveFrame())
                {
                    throw new Exception("something went wrong with the stack!");
                }
                return false;
            }
            else if (ret == 0)
            {
                o.Nullify();
            }
            else if (ret == 985)
            {
                _exitcode = (int)_stack[_top - 1].GetInt();
                _exited = true;
                return false;
            }
            else
            {
                o.Assign(_stack[_top - 1]);
            }

            if (!LeaveFrame())
            {
                throw new Exception("something went wrong with the stack!");
            }
            return true;
        }

        public bool _forcereturn = false;

        public bool Call(ref ExObject cls, long nparams, long stackbase, ref ExObject o, bool forcereturn = false)
        {
            return Call(ref cls, (int)nparams, (int)stackbase, ref o, forcereturn);
        }

        public bool Call(ref ExObject cls, int nparams, int stackbase, ref ExObject o, bool forcereturn = false)
        {
            bool f = _forcereturn;
            _forcereturn = forcereturn;
            switch (cls._type)
            {
                case ExObjType.CLOSURE:
                    {
                        bool state = Exec(cls, nparams, stackbase, ref o);
                        if (state)
                        {
                            _nnativecalls--;
                        }
                        _forcereturn = f;
                        return state;
                    }
                case ExObjType.NATIVECLOSURE:
                    {
                        bool s = CallNative(cls._val._NativeClosure, nparams, stackbase, ref o);
                        _forcereturn = f;
                        return s;
                    }
                case ExObjType.CLASS:
                    {
                        ExObject cn = new();
                        ExObject tmp = new();

                        CreateClassInst(cls._val._Class, ref o, cn);
                        if (cn._type != ExObjType.NULL)
                        {
                            _stack[stackbase].Assign(o);
                            bool s = Call(ref cn, nparams, stackbase, ref tmp);
                            _forcereturn = f;
                            return s;
                        }
                        _forcereturn = f;
                        return true;
                    }
                default:
                    return _forcereturn = f;
            }

        }

    }
}
