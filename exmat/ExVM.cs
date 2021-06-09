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
        public ExSState SharedState = new();

        public List<ExObject> Stack;            // Sanal bellek
        public int StackBase;                   // Anlık bellek taban indeksi
        public int StackTop;                    // Anlık bellek tavan indeksi

        public List<ExCallInfo> CallStack;      // Çağrı yığını
        public Node<ExCallInfo> CallInfo;       // Çağrı bağlı listesi
        public int AllocatedCallSize;           // Yığının ilk boyutu
        public int CallStackSize;               // Yığının anlık boyutu

        public ExObject RootDictionary;         // Global tablo
        public ExObject TempRegistery = new();  // Geçici değer
        public ExOuter Outers;                  // Bilinmeyen değişken takibi

        public string ErrorString;                  // Hata mesajı
        public List<List<int>> ErrorTrace = new();  // Hata izi
        public int nNativeCalls;                // Yerel fonksiyon çağrı sayısı
        public int nMetaCalls;                  // Meta metot çağrı sayısı

        public bool GotUserInput;               // Girdi alma fonksiyonu kullanıldı ?
        public bool PrintedToConsole;           // Konsola çıktı yazıldı ?
        public bool IsMainCall = true;          // İçinde bulunulan çağrı kök çağrı mı ?
        public bool IsInteractive;              // İnteraktif konsol kullanımda mı ?

        public bool ExitCalled;                 // Çıkış fonksiyonu çağırıldı ?
        public int ExitCode;                    // Çıkışta dönülecek değer


        public void Print(string str)
        {
            Console.Write(str);
            PrintedToConsole = true;
        }

        public void PrintLine(string str)
        {
            Console.WriteLine(str);
            PrintedToConsole = true;
        }

        public void AddToErrorMessage(string msg)
        {
            if (string.IsNullOrEmpty(ErrorString))
            {
                ErrorString = "[ERROR]" + msg;
            }
            else
            {
                ErrorString += "\n[ERROR]" + msg;
            }
        }

        public void Initialize(int stacksize)
        {
            ExUtils.InitList(ref Stack, stacksize);
            AllocatedCallSize = 4;
            ExUtils.InitList(ref CallStack, AllocatedCallSize);
            CallStackSize = 0;

            StackBase = 0;
            StackTop = 0;
            RootDictionary = new() { Type = ExObjType.DICT };
            RootDictionary.Value.d_Dict = new();
            ExBaseLib.RegisterStdBase(this);

        }

        public bool ToString(ExObject obj,
                             ref ExObject res,
                             int maxdepth = 2,
                             bool dval = false,
                             bool beauty = false,
                             string prefix = "")
        {
            switch (obj.Type)
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
                        res = new(obj.Value.b_Bool ? "true" : "false");
                        break;
                    }
                case ExObjType.NULL:
                    {
                        res = new(obj.Value.s_String ?? "null");
                        break;
                    }
                case ExObjType.ARRAY:
                    {
                        if (maxdepth == 0)
                        {
                            res = new("ARRAY(" + (obj.Value.l_List == null ? "empty" : obj.Value.l_List.Count) + ")");
                            break;
                        }
                        ExObject temp = new(string.Empty);
                        string s = "[";
                        int n = 0;
                        int c = obj.Value.l_List.Count;
                        maxdepth--;

                        if (beauty && !dval && c > 0)
                        {
                            if (prefix != string.Empty)
                            {
                                s = "\n" + prefix + s;
                            }
                        }

                        foreach (ExObject o in obj.Value.l_List)
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
                            res = new("DICT(" + (obj.Value.l_List == null ? "empty" : obj.Value.l_List.Count) + ")");
                            break;
                        }
                        ExObject temp = new(string.Empty);
                        string s = "{";
                        int n = 0;
                        int c = obj.Value.d_Dict.Count;
                        if (beauty && c > 0)
                        {
                            if (prefix != string.Empty)
                            {
                                s = "\n" + prefix + s;
                            }
                        }
                        if (c > 0)
                        {
                            s += "\n" + prefix + "\t";
                        }

                        maxdepth--;
                        foreach (KeyValuePair<string, ExObject> pair in obj.Value.d_Dict)
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
                        string s = obj.Type.ToString() + "(" + obj.Value._NativeClosure.Name.GetString() + ", ";
                        int n = obj.Value._NativeClosure.nParameterChecks;
                        if (n < 0)
                        {
                            int tnc = obj.Value._NativeClosure.TypeMasks.Count;
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
                            s += "<=" + (obj.Value._NativeClosure.TypeMasks.Count - 1) + " params";
                        }

                        s += ")";

                        res = new(s);
                        break;
                    }
                case ExObjType.CLOSURE:
                    {
                        ExPrototype tmp = obj.Value._Closure.Function;
                        string s = string.Empty;
                        switch (tmp.ClosureType)
                        {
                            case ExClosureType.FUNCTION:
                                {
                                    string name = tmp.Name.GetString();
                                    s = string.IsNullOrWhiteSpace(name) ? "LAMBDA(" : "FUNCTION(" + name + ", ";

                                    if (tmp.nDefaultParameters > 0)
                                    {
                                        s += (tmp.nParams - 1) + " params (min:" + (tmp.nParams - tmp.nDefaultParameters - 1) + "))";
                                    }
                                    else if (tmp.HasVargs)
                                    {
                                        s += "vargs, min:" + (tmp.nParams - 2) + " params)";
                                    }
                                    else
                                    {
                                        s += (tmp.nParams - 1) + " params)";
                                    }
                                    break;
                                }
                            case ExClosureType.RULE:
                                {
                                    s = "RULE(" + tmp.Name.GetString() + ", ";
                                    s += (tmp.nParams - 1) + " params)";
                                    break;
                                }
                            #region _
                            /*
                        case ExClosureType.MACRO:
                            {
                                s = "MACRO(" + tmp.Name.GetString() + ", ";
                                if (tmp.nDefaultParameters > 0)
                                {
                                    s += (tmp.nParams - 1) + " params (min:" + (tmp.nParams - tmp.nDefaultParameters - 1) + ")";
                                }
                                else
                                {
                                    s += ")";
                                }
                                break;
                            }*/
                            #endregion
                            case ExClosureType.CLUSTER:
                                {
                                    s = "CLUSTER(" + tmp.Name.GetString() + ", ";
                                    s += (tmp.nParams - 1) + " params)";
                                    break;
                                }
                            case ExClosureType.SEQUENCE:
                                {
                                    s = "SEQUENCE(" + tmp.Name.GetString() + ", 1 params)";
                                    break;
                                }
                        }

                        res = new(s);
                        break;
                    }
                case ExObjType.SPACE:
                    {
                        res = new(obj.Value.c_Space.GetSpaceString());
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
                        res = new(obj.Type.ToString());
                        break;
                    }
            }
            return true;
        }
        public bool ToFloat(ExObject obj, ref ExObject res)
        {
            switch (obj.Type)
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
                        res = new((double)obj.GetInt());
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
                        res = new((double)(obj.Value.b_Bool ? 1.0 : 0.0));
                        break;
                    }
                default:
                    {
                        AddToErrorMessage("failed to parse " + obj.Type.ToString() + " as double");
                        return false;
                    }
            }
            return true;
        }

        public bool ToInteger(ExObject obj, ref ExObject res)
        {
            switch (obj.Type)
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
                        res = new((long)obj.GetFloat());
                        break;
                    }
                case ExObjType.STRING:
                    {
                        if (long.TryParse(obj.GetString(), out long r))
                        {
                            res = new(r);
                        }
                        else
                        {
                            AddToErrorMessage("failed to parse string as integer");
                            return false;
                        }
                        break;
                    }
                case ExObjType.BOOL:
                    {
                        res = new(obj.Value.b_Bool ? 1 : 0);
                        break;
                    }
                default:
                    {
                        AddToErrorMessage("failed to parse " + obj.Type.ToString() + " as integer");
                        return false;
                    }
            }
            return true;
        }

        public bool NewSlotA(ExObject self, ExObject key, ExObject val, ExObject attrs, bool bstat, bool braw)
        {
            if (self.Type != ExObjType.CLASS)
            {
                AddToErrorMessage("object has to be a class");
                return false;
            }

            ExClass cls = self.Value._Class;

            if (!braw)
            {
                ExObject meta = cls.MetaFuncs[(int)ExMetaM.NEWMEMBER];
                if (meta.Type != ExObjType.NULL)
                {
                    Push(self);
                    Push(key);
                    Push(val);
                    Push(attrs);
                    Push(bstat);
                    return CallMeta(ref meta, ExMetaM.NEWMEMBER, 5, ref TempRegistery);
                }
            }

            if (!NewSlot(self, key, val, bstat))
            {
                AddToErrorMessage("failed to create a slot named '" + key + "'");
                return false;
            }

            if (attrs.Type != ExObjType.NULL)
            {
                cls.SetAttrs(key, attrs);
            }
            return true;
        }

        public bool NewSlot(ExObject self, ExObject key, ExObject val, bool bstat)
        {
            if (key.Type == ExObjType.NULL)
            {
                AddToErrorMessage("'null' can't be used as index");
                return false;
            }

            switch (self.Type)
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
                            if (self.Value.d_Dict.ContainsKey(key.GetString()))
                            {
                                self.Value.d_Dict[key.GetString()].Assign(v);    // TO-DO should i really allow this ?
                            }
                            else
                            {
                                self.Value.d_Dict.Add(key.GetString(), new(v));
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
                        if (!self.Value._Class.NewSlot(SharedState, key, val, bstat))
                        {
                            if (self.Value._Class.GotInstanced)
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
                        AddToErrorMessage("indexing " + self.Type.ToString() + " with " + key.Type.ToString());
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
                Stack[--StackTop].Nullify();
            }
        }

        public void Remove(int n)
        {
            n = n >= 0 ? n + StackBase - 1 : StackTop + n;
            for (int i = n; i < StackTop; i++)
            {
                Stack[i].Assign(Stack[i + 1]);
            }
            Stack[StackTop].Nullify();
            StackTop--;
        }

        public void Pop()
        {
            Stack[--StackTop].Nullify();
        }
        public ExObject PopGet()
        {
            return Stack[--StackTop];
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
            Stack[StackTop++].Assign(o);
        }

        public void Push(Complex o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(int o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(long o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(double o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(bool o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(ExObject o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(Dictionary<string, ExObject> o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(List<ExObject> o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(ExInstance o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(ExClass o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(ExClosure o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(ExNativeClosure o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(ExOuter o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(ExWeakRef o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(ExPrototype o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void PushNull()
        {
            Stack[StackTop++].Nullify();
        }

        public ExObject Top()
        {
            return Stack[StackTop - 1];
        }

        public ExObject GetAbove(int n)
        {
            return Stack[StackTop + n];
        }
        public ExObject GetAt(int n)
        {
            return Stack[n];
        }

        public ExObject CreateString(string s, int len = -1)
        {
            if (!SharedState.Strings.ContainsKey(s))
            {
                ExObject str = new() { Type = ExObjType.STRING };
                str.SetString(s);

                SharedState.Strings.Add(s, str);
                return str;
            }
            return SharedState.Strings[s];
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
            int t_n = cls.DefaultParams.Count;

            ExPrototype pro = cls.Function;
            List<ExObject> ts = cls.DefaultParams;
            int nargs = lis.Count;

            if (t_n > 0)
            {
                if (t_n == 1)
                {
                    if (!IsInSpace(new(lis), ts[0].Value.c_Space, 1, false))
                    {
                        return false;
                    }
                }
                else
                {
                    if (t_n != nargs)
                    {
                        AddToErrorMessage("'" + pro.Name.GetString() + "' takes " + (t_n) + " arguments");
                        return false;
                    }

                    for (int i = 0; i < nargs; i++)
                    {
                        if (!IsInSpace(lis[i], ts[i].Value.c_Space, i + 1, false))
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
            int t_n = cls.DefaultParams.Count;

            ExPrototype pro = cls.Function;
            List<ExObject> ts = cls.DefaultParams;

            if (t_n > 0)
            {
                if (t_n != nargs - 1)
                {
                    AddToErrorMessage("'" + pro.Name.GetString() + "' takes " + (t_n) + " arguments");
                    return false;
                }

                for (int i = 0; i < nargs && i < t_n; i++)
                {
                    if (!IsInSpace(Stack[sbase + i + 1], ts[i].Value.c_Space, i + 1))
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
            ExPrototype pro = cls.Function;

            int p = pro.nParams;
            int newt = sbase + pro.StackSize;
            int nargs = args;

            if (pro.HasVargs)
            {
                p--;
                if (nargs < p)
                {
                    AddToErrorMessage("'" + pro.Name.GetString() + "' takes at least " + (p - 1) + " arguments");
                    return false;
                }

                int nvargs = nargs - p;
                List<ExObject> varglis = new();
                int pb = sbase + p;
                for (int n = 0; n < nvargs; n++)
                {
                    varglis.Add(new(Stack[pb]));
                    Stack[pb].Nullify();
                    pb++;
                }

                Stack[sbase + p].Assign(varglis);
            }
            else if (!pro.IsCluster()
                     && !pro.IsRule()
                     && !pro.IsSequence())
            {
                if (p != nargs)
                {
                    int n_def = pro.nDefaultParameters;
                    int diff;
                    int defstart = p - n_def;
                    if (n_def > 0 && nargs < p && (diff = p - nargs) <= n_def)
                    {
                        for (int n = 1; n < p; n++)
                        {
                            if (Stack[sbase + n].Type == ExObjType.DEFAULT)
                            {
                                if (n >= defstart)
                                {
                                    Stack[sbase + n].Assign(cls.DefaultParams[n - defstart]);
                                }
                                else
                                {
                                    AddToErrorMessage("can't use non-existant default value reference for parameter " + n);
                                    return false;
                                }
                            }
                            else if (n >= defstart)
                            {
                                Stack[sbase + n].Assign(cls.DefaultParams[n - defstart]);
                                nargs++;
                            }
                        }
                    }
                    else
                    {
                        if (n_def > 0 && !pro.IsCluster() && !pro.IsCluster())
                        {
                            AddToErrorMessage("'" + pro.Name.GetString() + "' takes min: " + (p - n_def - 1) + ", max:" + (p - 1) + " arguments");
                        }
                        else
                        {
                            AddToErrorMessage("'" + pro.Name.GetString() + "' takes exactly " + (p - 1) + " arguments");
                        }
                        return false;
                    }
                }
                else
                {
                    int n_def = pro.nDefaultParameters;
                    int defstart = p - n_def;
                    for (int n = 1; n < p; n++)
                    {
                        if (Stack[sbase + n].Type == ExObjType.DEFAULT)
                        {
                            if (n >= defstart)
                            {
                                Stack[sbase + n].Assign(cls.DefaultParams[n - defstart]);
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
                int t_n = pro.LocalInfos.Count;
                if (t_n != nargs)
                {
                    AddToErrorMessage("'" + pro.Name.GetString() + "' takes " + (t_n - 1) + " arguments");
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
                if (nargs < 2)
                {
                    AddToErrorMessage("sequences require at least 1 argument to be called");
                    return false;
                }
                else // CONTINUE HERE, ALLOW PARAMETERS FOR SEQUENCES
                {
                    if (!Stack[sbase + 1].IsNumeric())
                    {
                        AddToErrorMessage("expected integer or float as sequence argument");
                        return false;
                    }
                    else
                    {
                        if (Stack[sbase + 1].Type == ExObjType.INTEGER)
                        {
                            long ind = Stack[sbase + 1].GetInt();
                            string idx = ind.ToString();
                            for (int i = 2; i < cls.Function.Parameters.Count; i++)
                            {
                                ExObject c = cls.Function.Parameters[i];
                                if (c.GetString() == idx)
                                {
                                    // TO-DO doesnt return to main, also refactor this
                                    // TO-DO optimize
                                    Stack[sbase - 1].Assign(cls.DefaultParams[i - 2]);
                                    return true;
                                }
                            }
                            if (ind < 0)
                            {
                                AddToErrorMessage("index can't be negative, unless its a default value");
                                return false;
                            }
                        }
                        else if (Stack[sbase + 1].Type == ExObjType.FLOAT)
                        {
                            double ind = Stack[sbase + 1].GetFloat();
                            string idx = ind.ToString();
                            for (int i = 2; i < cls.Function.Parameters.Count; i++)
                            {
                                ExObject c = cls.Function.Parameters[i];
                                if (c.GetString() == idx)
                                {
                                    // TO-DO doesnt return to main, also refactor this
                                    // TO-DO optimize
                                    Stack[sbase - 1].Assign(cls.DefaultParams[i - 2]);
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
            }

            if (cls.WeakReference != null)
            {
                Stack[sbase].Assign(cls.WeakReference.ReferencedObject);
            }

            if (!EnterFrame(sbase, newt, tail))
            {
                AddToErrorMessage("failed to create a scope");
                return false;
            }

            CallInfo.Value.Closure = new(); CallInfo.Value.Closure.Assign(cls);
            CallInfo.Value.Literals = pro.Literals;
            CallInfo.Value.Instructions = pro.Instructions;
            CallInfo.Value.InstructionsIndex = 0;
            CallInfo.Value.Target = trg;

            return true;
        }

        public bool IsInSpace(ExObject argument, ExSpace space, int i, bool raise = true)
        {
            switch (argument.Type)
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
                        if (argument.Value.l_List.Count != space.Dimension && space.Dimension != -1)
                        {
                            if (raise)
                            {
                                AddToErrorMessage("expected " + space.Dimension + " dimensions for parameter " + i);
                            }
                            return false;
                        }

                        if (space.Child != null)
                        {
                            foreach (ExObject val in argument.Value.l_List)
                            {
                                if (!IsInSpace(val, space.Child, i, raise))
                                {
                                    return false;
                                }
                            }
                            break;
                        }

                        switch (space.Domain)
                        {
                            case "C":
                                {
                                    foreach (ExObject val in argument.Value.l_List)
                                    {
                                        if (!val.IsNumeric())
                                        {
                                            if (raise)
                                            {
                                                AddToErrorMessage("expected real or complex numbers for parameter " + i);
                                            }
                                            return false;
                                        }
                                    }
                                    break;
                                }
                            case "E":
                                {
                                    return true;
                                }
                            case "r":
                                {
                                    switch (space.Sign)
                                    {
                                        case '+':
                                            {
                                                foreach (ExObject val in argument.Value.l_List)
                                                {
                                                    if (!val.IsRealNumber() || val.GetFloat() <= 0)
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
                                                foreach (ExObject val in argument.Value.l_List)
                                                {
                                                    if (!val.IsRealNumber() || val.GetFloat() >= 0)
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
                                                foreach (ExObject val in argument.Value.l_List)
                                                {
                                                    if (!val.IsRealNumber() || val.GetFloat() == 0)
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
                                    switch (space.Sign)
                                    {
                                        case '+':
                                            {
                                                foreach (ExObject val in argument.Value.l_List)
                                                {
                                                    if (!val.IsRealNumber() || val.GetFloat() < 0)
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
                                                foreach (ExObject val in argument.Value.l_List)
                                                {
                                                    if (!val.IsRealNumber() || val.GetFloat() > 0)
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
                                                foreach (ExObject val in argument.Value.l_List)
                                                {
                                                    if (!val.IsRealNumber())
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
                                    switch (space.Sign)
                                    {
                                        case '+':
                                            {
                                                foreach (ExObject val in argument.Value.l_List)
                                                {
                                                    if (val.Type != ExObjType.INTEGER || val.GetFloat() < 0)
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
                                                foreach (ExObject val in argument.Value.l_List)
                                                {
                                                    if (val.Type != ExObjType.INTEGER || val.GetFloat() > 0)
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
                                                foreach (ExObject val in argument.Value.l_List)
                                                {
                                                    if (val.Type != ExObjType.INTEGER)
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
                                    switch (space.Sign)
                                    {
                                        case '+':
                                            {
                                                foreach (ExObject val in argument.Value.l_List)
                                                {
                                                    if (val.Type != ExObjType.INTEGER || val.GetInt() <= 0)
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
                                                foreach (ExObject val in argument.Value.l_List)
                                                {
                                                    if (val.Type != ExObjType.INTEGER || val.GetInt() >= 0)
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
                                                foreach (ExObject val in argument.Value.l_List)
                                                {
                                                    if (val.Type != ExObjType.INTEGER || val.GetInt() == 0)
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
                case ExObjType.COMPLEX:
                    {
                        if (space.Dimension > 1)
                        {
                            if (raise)
                            {
                                AddToErrorMessage("expected " + space.Dimension + " dimensions for parameter " + i);
                            }
                            return false;
                        }

                        if (space.Child != null)
                        {
                            if (!IsInSpace(new(new List<ExObject>(1) { argument }), space.Child, i, raise))
                            {
                                return false;
                            }
                            break;
                        }

                        if (space.Domain == "E" || space.Domain == "C")
                        {
                            return true;
                        }
                        else if (argument.Value.c_Float == 0.0)
                        {
                            argument = new(argument.Value.f_Float);
                            goto case ExObjType.FLOAT;
                        }
                        else
                        {
                            if (raise)
                            {
                                AddToErrorMessage("expected non-complex number for parameter " + i);
                            }
                            return false;
                        }
                    }
                case ExObjType.INTEGER:
                case ExObjType.FLOAT:
                    {
                        if (space.Dimension > 1)
                        {
                            if (raise)
                            {
                                AddToErrorMessage("expected " + space.Dimension + " dimensions for parameter " + i);
                            }
                            return false;
                        }

                        if (space.Child != null)
                        {
                            if (!IsInSpace(new(new List<ExObject>(1) { argument }), space.Child, i, raise))
                            {
                                return false;
                            }
                            break;
                        }

                        switch (space.Domain)
                        {
                            case "C":
                                {
                                    return true;
                                }
                            case "E":
                                {
                                    return true;
                                }
                            case "r":
                                {
                                    switch (space.Sign)
                                    {
                                        case '+':
                                            {
                                                if (argument.GetFloat() <= 0)
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
                                                if (argument.GetFloat() >= 0)
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
                                                if (argument.GetFloat() == 0)
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
                                    switch (space.Sign)
                                    {
                                        case '+':
                                            {
                                                if (argument.GetFloat() < 0)
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
                                                if (argument.GetFloat() > 0)
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
                                    switch (space.Sign)
                                    {
                                        case '+':
                                            {
                                                if (argument.Type != ExObjType.INTEGER || argument.GetInt() < 0)
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
                                                if (argument.Type != ExObjType.INTEGER || argument.GetInt() > 0)
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
                                                if (argument.Type != ExObjType.INTEGER)
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
                                    switch (space.Sign)
                                    {
                                        case '+':
                                            {
                                                if (argument.Type != ExObjType.INTEGER || argument.GetInt() <= 0)
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
                                                if (argument.Type != ExObjType.INTEGER || argument.GetInt() >= 0)
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
                                                if (argument.Type != ExObjType.INTEGER || argument.GetInt() == 0)
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
                default:
                    {
                        return false;
                    }
            }
            return true;
        }

        private ExObject FindSpaceObject(int i)
        {
            string name = CallInfo.Value.Literals[i].GetString();
            return new(ExSpace.GetSpaceFromString(name));
        }

        public bool FixStackAfterError()
        {
            ErrorTrace = new();
            while (CallInfo.Value != null)
            {
                bool end = CallInfo.Value != null && CallInfo.Value.IsRootCall;
                if (CallInfo.Value != null)
                {
                    ExLineInfo info = CallInfo.Value.Closure.GetClosure().Function.FindLineInfo(CallInfo.Value.InstructionsIndex);
                    ErrorTrace.Add(new(2) { info.Line, info.Position });
                }

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

        public bool Execute(ExObject cls, int narg, int stackbase, ref ExObject o)
        {
            if (nNativeCalls++ > 100)
            {
                throw new Exception("Native stack overflow");
            }

            Node<ExCallInfo> prevci = CallInfo;

            TempRegistery = new(cls);
            if (!StartCall(TempRegistery.Value._Closure, StackTop - narg, narg, stackbase, false))
            {
                return false;
            }

            if (CallInfo == prevci)
            {
                o.Assign(Stack[StackBase + StackTop - narg]);
                return true;
            }
            CallInfo.Value.IsRootCall = true;

            for (; ; )
            {
                if (CallInfo.Value == null
                    || CallInfo.Value.Instructions == null)
                {
                    return true;
                }

                if (CallInfo.Value.InstructionsIndex >= CallInfo.Value.Instructions.Count
                    || CallInfo.Value.InstructionsIndex < 0)
                {
                    return false;
                }


                ExInstr i = CallInfo.Value.Instructions[CallInfo.Value.InstructionsIndex++];
                switch (i.op)
                {
                    case OPC.LOAD:
                        {
                            GetTargetInStack(i).Assign(CallInfo.Value.Literals[(int)i.arg1]);
                            continue;
                        }
                    case OPC.DLOAD:
                        {
                            GetTargetInStack(i).Assign(CallInfo.Value.Literals[(int)i.arg1]);
                            GetTargetInStack(i.arg2).Assign(CallInfo.Value.Literals[(int)i.arg3]);
                            continue;
                        }
                    case OPC.LOADINTEGER:
                        {
                            GetTargetInStack(i).Assign(i.arg1);
                            continue;
                        }
                    case OPC.LOADFLOAT:
                        {
                            GetTargetInStack(i).Assign( new DoubleLong() { i = i.arg1 }.f );
                            continue;
                        }
                    case OPC.LOADCOMPLEX:
                        {
                            if (i.arg2 == 1)
                            {
                                GetTargetInStack(i).Assign(new Complex(0.0, new DoubleLong() { i = i.arg1 }.f));
                            }
                            else
                            {
                                GetTargetInStack(i).Assign(new Complex(0.0, i.arg1));
                            }
                            continue;
                        }
                    case OPC.LOADBOOLEAN:
                        {
                            GetTargetInStack(i).Assign(i.arg1 == 1);
                            continue;
                        }
                    case OPC.LOADSPACE:
                        {
                            GetTargetInStack(i).Assign(FindSpaceObject((int)i.arg1));
                            continue;
                        }
                    case OPC.CALLTAIL:
                        {
                            ExObject tmp = GetTargetInStack(i.arg1);
                            if (tmp.Type == ExObjType.CLOSURE)
                            {
                                ExObject c = new(tmp);
                                if (Outers != null)
                                {
                                    CloseOuters(StackBase);
                                }
                                for (int j = 0; j < i.arg3; j++)
                                {
                                    GetTargetInStack(j).Assign(GetTargetInStack(i.arg2 + j));
                                }
                                if (!StartCall(c.Value._Closure, CallInfo.Value.Target, i.arg3, StackBase, true))
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
                            switch (tmp2.Type)
                            {
                                case ExObjType.CLOSURE:
                                    {
                                        if (!StartCall(tmp2.Value._Closure, i.arg0, i.arg3, StackBase + i.arg2, false))
                                        {
                                            //AddToErrorMessage("guarded failed call");
                                            return FixStackAfterError();
                                        }
                                        continue;
                                    }
                                case ExObjType.NATIVECLOSURE:
                                    {
                                        if (!CallNative(tmp2.Value._NativeClosure, i.arg3, StackBase + i.arg2, ref tmp2))
                                        {
                                            //AddToErrorMessage("guarded failed call");
                                            return FixStackAfterError();
                                        }

                                        if (i.arg0 != 985)
                                        {
                                            GetTargetInStack(i.arg0).Assign(tmp2);
                                        }
                                        continue;
                                    }
                                case ExObjType.CLASS:
                                    {
                                        ExObject instance = new();
                                        if (!CreateClassInst(tmp2.Value._Class, ref instance, tmp2))
                                        {
                                            //AddToErrorMessage("guarded failed call");
                                            return FixStackAfterError();
                                        }
                                        if (i.arg0 != -1)
                                        {
                                            GetTargetInStack(i.arg0).Assign(instance);
                                        }

                                        int sbase;
                                        switch (tmp2.Type)
                                        {
                                            case ExObjType.CLOSURE:
                                                {
                                                    sbase = StackBase + (int)i.arg2;
                                                    Stack[sbase].Assign(instance);
                                                    if (!StartCall(tmp2.Value._Closure, -1, i.arg3, sbase, false))
                                                    {
                                                        //AddToErrorMessage("guarded failed call");
                                                        return FixStackAfterError();
                                                    }
                                                    break;
                                                }
                                            case ExObjType.NATIVECLOSURE:
                                                {
                                                    sbase = StackBase + (int)i.arg2;
                                                    Stack[sbase].Assign(instance);
                                                    if (!CallNative(tmp2.Value._NativeClosure, i.arg3, sbase, ref tmp2))
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
                                            for (int j = 0; j < i.arg3; j++)
                                            {
                                                Push(GetTargetInStack(j + i.arg2));
                                            }

                                            if (!CallMeta(ref cls2, ExMetaM.CALL, i.arg3 + 1, ref tmp2))
                                            {
                                                AddToErrorMessage("meta method failed call");
                                                return FixStackAfterError();
                                            }

                                            if (i.arg0 != -1)
                                            {
                                                GetTargetInStack(i.arg0).Assign(tmp2);
                                            }
                                            break;
                                        }
                                        goto default;
                                    }
                                case ExObjType.SPACE:
                                    {
                                        ExSpace sp_org = GetTargetInStack(i.arg1).Value.c_Space;
                                        ExSpace sp = sp_org.DeepCopy();
                                        int nparams = sp.Depth();
                                        int varcount = sp.VarCount();
                                        int argcount = (int)i.arg3 - 1;
                                        if (argcount > varcount)
                                        {
                                            AddToErrorMessage("expected maximum " + varcount + " arguments for space");
                                            return FixStackAfterError();
                                        }

                                        ExSpace child = sp;
                                        int sb = (int)i.arg2;
                                        int argid = 1;
                                        while (child != null && argcount > 0)
                                        {
                                            if (child.Dimension == -1)
                                            {
                                                ExObject dim = GetTargetInStack(sb + argid);
                                                if (dim.Type != ExObjType.INTEGER)
                                                {
                                                    AddToErrorMessage("spaces can't have non-integer dimensions");
                                                    return FixStackAfterError();
                                                }
                                                int d = (int)dim.GetInt();
                                                if (d < 0)
                                                {
                                                    d = -1;
                                                }
                                                child.Dimension = d;
                                                argcount--;
                                                argid++;
                                            }
                                            child = child.Child;
                                        }

                                        GetTargetInStack(i.arg0).Assign(sp);
                                        break;
                                    }
                                default:
                                    {
                                        AddToErrorMessage("attempt to call " + tmp2.Type.ToString());
                                        return FixStackAfterError();
                                    }
                            }
                            continue;
                        }
                    case OPC.PREPCALL:
                    case OPC.PREPCALLK:
                        {
                            ExObject k = i.op == OPC.PREPCALLK ? CallInfo.Value.Literals[(int)i.arg1] : GetTargetInStack(i.arg1);
                            ExObject obj = GetTargetInStack(i.arg2);

                            if (!Getter(ref obj, ref k, ref TempRegistery, false, (ExFallback)i.arg2))
                            {
                                AddToErrorMessage("unknown method or field '" + k.GetString() + "'");
                                return FixStackAfterError();
                            }

                            GetTargetInStack(i.arg3).Assign(obj);
                            SwapObjects(GetTargetInStack(i), ref TempRegistery);
                            continue;
                        }
                    case OPC.MOVE:
                        {
                            GetTargetInStack(i).Assign(GetTargetInStack(i.arg1));
                            continue;
                        }
                    case OPC.DMOVE:
                        {
                            GetTargetInStack(i.arg0).Assign(GetTargetInStack(i.arg1));
                            GetTargetInStack(i.arg2).Assign(GetTargetInStack(i.arg3));
                            continue;
                        }
                    case OPC.NEWSLOT:
                        {
                            if (!NewSlot(GetTargetInStack(i.arg1), GetTargetInStack(i.arg2), GetTargetInStack(i.arg3), false))
                            {
                                //AddToErrorMessage("guarded failed newslot");
                                return FixStackAfterError();
                            }
                            if (i.arg0 != 985)
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
                                //AddToErrorMessage("failed setter for '" + GetTargetInStack(i.arg2).GetString() + "' key");
                                return FixStackAfterError();
                            }
                            if (i.arg0 != 985)
                            {
                                GetTargetInStack(i).Assign(GetTargetInStack(i.arg3));
                            }
                            continue;
                        }
                    case OPC.GET:
                        {
                            ExObject s1 = new(GetTargetInStack(i.arg1));
                            ExObject s2 = new(GetTargetInStack(i.arg2));
                            if (!Getter(ref s1, ref s2, ref TempRegistery, false, (ExFallback)i.arg1))
                            {
                                //AddToErrorMessage("failed getter for '" + s2.GetString() + "' key");
                                return FixStackAfterError();
                            }
                            SwapObjects(GetTargetInStack(i), ref TempRegistery);
                            continue;
                        }
                    case OPC.GETK:
                        {
                            ExObject tmp = GetTargetInStack(i.arg2);
                            ExObject lit = CallInfo.Value.Literals[(int)i.arg1];

                            if (!Getter(ref tmp, ref lit, ref TempRegistery, false, (ExFallback)i.arg2))
                            {
                                AddToErrorMessage("unknown variable '" + lit.GetString() + "'"); // access to local var decl before
                                return FixStackAfterError();
                            }
                            SwapObjects(GetTargetInStack(i), ref TempRegistery);
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
                            if (!DoBitwiseOP(i.arg3, GetTargetInStack(i.arg2), GetTargetInStack(i.arg1), GetTargetInStack(i)))
                            {
                                return FixStackAfterError();
                            }

                            continue;
                        }
                    case OPC.RETURNBOOL:
                    case OPC.RETURN:
                        {
                            if (ReturnValue((int)i.arg0, (int)i.arg1, ref TempRegistery, i.op == OPC.RETURNBOOL, i.arg2 == 1))
                            {
                                SwapObjects(o, ref TempRegistery);
                                return true;
                            }
                            continue;
                        }
                    case OPC.LOADNULL:
                        {
                            if (i.arg2 == 1)
                            {
                                for (int n = 0; n < i.arg1; n++)
                                {
                                    GetTargetInStack(i.arg0 + n).Nullify();
                                    GetTargetInStack(i.arg0 + n).Assign(new ExObject() { Type = ExObjType.DEFAULT });
                                }
                            }
                            else
                            {
                                for (int n = 0; n < i.arg1; n++)
                                {
                                    GetTargetInStack(i.arg0 + n).Nullify();
                                }
                            }
                            continue;
                        }
                    case OPC.LOADROOT:
                        {
                            GetTargetInStack(i).Assign(RootDictionary);
                            continue;
                        }
                    case OPC.JZS:
                        {
                            if (!GetTargetInStack(i.arg0).GetBool())
                            {
                                CallInfo.Value.InstructionsIndex += (int)i.arg1;
                            }
                            continue;
                        }
                    case OPC.JMP:
                        {
                            CallInfo.Value.InstructionsIndex += (int)i.arg1;
                            continue;
                        }
                    case OPC.JCMP:
                        {
                            if (!DoCompareOP((CmpOP)i.arg3, GetTargetInStack(i.arg2), GetTargetInStack(i.arg0), TempRegistery))
                            {
                                return FixStackAfterError();
                            }
                            if (!TempRegistery.GetBool())
                            {
                                CallInfo.Value.InstructionsIndex += (int)i.arg1;
                            }
                            continue;
                        }
                    case OPC.JZ:
                        {
                            if (!GetTargetInStack(i.arg0).GetBool())
                            {
                                CallInfo.Value.InstructionsIndex += (int)i.arg1;
                            }
                            continue;
                        }
                    case OPC.GETOUTER:
                        {
                            ExClosure currcls = CallInfo.Value.Closure.GetClosure();
                            ExOuter outr = currcls.OutersList[(int)i.arg1].Value._Outer;
                            GetTargetInStack(i).Assign(outr.ValueRef);
                            continue;
                        }
                    case OPC.SETOUTER:
                        {
                            ExClosure currcls = CallInfo.Value.Closure.GetClosure();
                            ExOuter outr = currcls.OutersList[(int)i.arg1].Value._Outer;
                            outr.ValueRef.Assign(GetTargetInStack(i.arg2));
                            if (i.arg0 != 985)
                            {
                                GetTargetInStack(i).Assign(GetTargetInStack(i.arg2));
                            }
                            continue;
                        }
                    case OPC.NEWOBJECT:
                        {
                            switch (i.arg3)
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
                                        if (!DoClassOP(GetTargetInStack(i), (int)i.arg1, (int)i.arg2))
                                        {
                                            AddToErrorMessage("failed to create class");
                                            return FixStackAfterError();
                                        }
                                        continue;
                                    }
                                default:
                                    {
                                        AddToErrorMessage("unknown object type " + i.arg3);
                                        return FixStackAfterError();
                                    }
                            }
                        }
                    case OPC.APPENDTOARRAY:
                        {
                            ExObject val = new();
                            switch (i.arg2)
                            {
                                case (int)ArrayAType.STACK:
                                    val.Assign(GetTargetInStack(i.arg1)); break;
                                case (int)ArrayAType.LITERAL:
                                    val.Assign(CallInfo.Value.Literals[(int)i.arg1]); break;
                                case (int)ArrayAType.INTEGER:
                                    val.Assign(i.arg1); break;
                                case (int)ArrayAType.FLOAT:
                                    val.Assign(new DoubleLong() { i = i.arg1 }.f); break;
                                case (int)ArrayAType.BOOL:
                                    val.Assign(i.arg1 == 1); break;
                                default:
                                    {
                                        throw new Exception("unknown array append method");
                                    }
                            }
                            GetTargetInStack(i.arg0).Value.l_List.Add(val);
                            continue;
                        }
                    case OPC.TRANSPOSE:
                        {
                            ExObject s1 = new(GetTargetInStack(i.arg1));
                            if (!DoMatrixTranspose(GetTargetInStack(i), ref s1, (ExFallback)i.arg1))
                            {
                                return FixStackAfterError();
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
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case OPC.INCL:
                    case OPC.PINCL:
                        {
                            ExObject ob = GetTargetInStack(i.arg1);
                            if (ob.Type == ExObjType.INTEGER)
                            {
                                GetTargetInStack(i).Assign(ob);
                                ob.Value.i_Int += i.arg3;
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
                                        return FixStackAfterError();
                                    }
                                }
                            }
                            continue;
                        }
                    case OPC.EXISTS:
                        {
                            ExObject s1 = new(GetTargetInStack(i.arg1));
                            ExObject s2 = new(GetTargetInStack(i.arg2));
                            bool b = Getter(ref s1, ref s2, ref TempRegistery, true, ExFallback.DONT, true);

                            GetTargetInStack(i).Assign(i.arg3 == 0 ? b : !b);

                            continue;
                        }
                    case OPC.CMP:
                        {
                            if (!DoCompareOP((CmpOP)i.arg3, GetTargetInStack(i.arg2), GetTargetInStack(i.arg1), GetTargetInStack(i)))
                            {
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case OPC.CLOSE:
                        {
                            if (Outers != null)
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
                                CallInfo.Value.InstructionsIndex += (int)i.arg1;
                            }
                            continue;
                        }
                    case OPC.OR:
                        {
                            if (GetTargetInStack(i.arg2).GetBool())
                            {
                                GetTargetInStack(i).Assign(GetTargetInStack(i.arg2));
                                CallInfo.Value.InstructionsIndex += (int)i.arg1;
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
                                AddToErrorMessage("attempted to negate '" + GetTargetInStack(i.arg1).Type.ToString() + "'");
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case OPC.CLOSURE:
                        {
                            ExClosure cl = CallInfo.Value.Closure.GetClosure();
                            ExPrototype fp = cl.Function;
                            if (!DoClosureOP(GetTargetInStack(i), fp.Functions[(int)i.arg1]))
                            {
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case OPC.NEWSLOTA:
                        {
                            if (!NewSlotA(GetTargetInStack(i.arg1),
                                         GetTargetInStack(i.arg2),
                                         GetTargetInStack(i.arg3),
                                         (i.arg0 & (int)ExNewSlotFlag.ATTR) > 0 ? GetTargetInStack(i.arg2 - 1) : new(),
                                         (i.arg0 & (int)ExNewSlotFlag.STATIC) > 0,
                                         false))
                            {
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case OPC.COMPOUNDARITH:
                        {
                            // TO-DO somethings wrong here
                            int idx = (int)((i.arg1 & 0xFFFF0000) >> 16);

                            ExObject si = GetTargetInStack(idx);
                            ExObject s2 = GetTargetInStack(i.arg2);
                            ExObject s1v = GetTargetInStack(i.arg1 & 0x0000FFFF);

                            if (!DoDerefInc((OPC)i.arg3, GetTargetInStack(i), ref si, ref s2, ref s1v, false, (ExFallback)idx))
                            {
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case OPC.TYPEOF:
                        {
                            GetTargetInStack(i).Assign(GetTargetInStack(i.arg1).Type.ToString());
                            continue;
                        }
                    case OPC.INSTANCEOF:
                        {
                            if (GetTargetInStack(i.arg1).Type != ExObjType.CLASS)
                            {
                                AddToErrorMessage("instanceof operation can only be done with a 'class' type");
                                return FixStackAfterError();
                            }
                            GetTargetInStack(i).Assign(
                                GetTargetInStack(i.arg2).Type == ExObjType.INSTANCE
                                && GetTargetInStack(i.arg2).Value._Instance.IsInstanceOf(GetTargetInStack(i.arg1).Value._Class));
                            continue;
                        }
                    case OPC.RETURNMACRO:   // TO-DO
                        {
                            if (ReturnValue((int)i.arg0, (int)i.arg1, ref TempRegistery, false, true))
                            {
                                SwapObjects(o, ref TempRegistery);
                                return true;
                            }
                            continue;
                        }
                    case OPC.GETBASE:
                        {
                            ExClosure c = CallInfo.Value.Closure.Value._Closure;
                            if (c.Base != null)
                            {
                                GetTargetInStack(i).Assign(c.Base);
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
            switch (self.Type)
            {
                case ExObjType.DICT:
                case ExObjType.INSTANCE:
                    {
                        ExObject cls = new();
                        ExObject tmp;

                        // TO-DO allow dict deleg ?
                        if (self.Type == ExObjType.INSTANCE && self.GetInstance().GetMetaM(this, ExMetaM.DELSLOT, ref cls))
                        {
                            Push(self);
                            Push(k);
                            return CallMeta(ref cls, ExMetaM.DELSLOT, 2, ref r);
                        }
                        else
                        {
                            if (self.Type == ExObjType.DICT)
                            {
                                if (self.Value.d_Dict.ContainsKey(k.GetString()))
                                {
                                    tmp = new(self.Value.d_Dict[k.GetString()]);

                                    self.Value.d_Dict.Remove(k.GetString());
                                }
                                else
                                {
                                    AddToErrorMessage(k.GetString() + " doesn't exist");
                                    return false;
                                }
                            }
                            else
                            {
                                AddToErrorMessage("can't delete a slot from " + self.Type.ToString());
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
                        AddToErrorMessage("can't delete a slot from " + self.Type.ToString());
                        return false;
                    }
            }
            return true;
        }

        public void FindOuterVal(ExObject target, ExObject sidx)
        {
            if (Outers == null)
            {
                Outers = new();
            }

            ExOuter tmp;
            while (Outers.ValueRef != null && Outers.ValueRef.GetInt() >= sidx.GetInt())
            {
                if (Outers.ValueRef.GetInt() == sidx.GetInt())
                {
                    target.Assign(new ExObject(Outers));
                    return;
                }
                Outers = Outers._next;
            }

            tmp = ExOuter.Create(SharedState, sidx);
            tmp._next = Outers;
            tmp.Index = (int)sidx.GetInt() - FindFirstNullInStack();
            tmp.ReferenceCount++;
            Outers = tmp;
            target.Assign(new ExObject(tmp));
        }

        public int FindFirstNullInStack()
        {
            for (int i = 0; i < Stack.Count; i++)
            {
                if (Stack[i].Type == ExObjType.NULL)
                {
                    return i;
                }
            }
            return -1;
        }

        public bool DoClosureOP(ExObject t, ExPrototype fp)
        {
            int nout;
            ExClosure cl = ExClosure.Create(SharedState, fp);

            if ((nout = fp.nOuters) > 0)
            {
                for (int i = 0; i < nout; i++)
                {
                    ExOuterInfo ov = fp.Outers[i];
                    switch (ov.Type)
                    {
                        case ExOuterType.LOCAL:
                            {
                                FindOuterVal(cl.OutersList[i], GetTargetInStack(ov.Index.GetInt()));
                                break;
                            }
                        case ExOuterType.OUTER:
                            {
                                cl.OutersList[i].Assign(CallInfo.Value.Closure.Value._Closure.OutersList[(int)ov.Index.GetInt()]);
                                break;
                            }
                    }
                }
            }
            int ndefpars;
            if ((ndefpars = fp.nDefaultParameters) > 0)
            {
                for (int i = 0; i < ndefpars; i++)
                {
                    int pos = fp.DefaultParameters[i];
                    cl.DefaultParams[i].Assign(Stack[StackBase + pos]);
                }
            }

            t.Assign(cl);
            return true;
        }

        public static bool DoNegateOP(ExObject target, ExObject val)
        {
            switch (val.Type)
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
            if (mat.Type != ExObjType.ARRAY)
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
                atrs.Assign(Stack[StackBase + attr]);
            }

            target.Assign(ExClass.Create(SharedState, cb));

            // TO-DO meta methods!
            if (target.Value._Class.MetaFuncs[(int)ExMetaM.INHERIT].Type != ExObjType.NULL)
            {
                int np = 2;
                ExObject r = new();
                Push(target);
                Push(atrs);
                ExObject mm = target.Value._Class.MetaFuncs[(int)ExMetaM.INHERIT];
                Call(ref mm, np, StackTop - np, ref r);
                Pop(np);
            }
            target.Value._Class.Attributes.Assign(atrs);
            return true;
        }

        private bool InnerDoCompareOP(ExObject a, ExObject b, ref int t)
        {
            ExObjType at = a.Type;
            ExObjType bt = b.Type;
            if (at == ExObjType.COMPLEX || bt == ExObjType.COMPLEX)
            {
                AddToErrorMessage("can't compare complex numbers");
                return false;
            }
            if (at == bt)
            {
                if (a.Value.i_Int == b.Value.i_Int)
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

        public bool ReturnValue(int a0, int a1, ref ExObject res, bool mkbool = false, bool interactive = false)                            //  bool mac = false)
        {
            bool r = CallInfo.Value.IsRootCall;
            int cbase = StackBase - CallInfo.Value.PrevBase;

            ExObject p;
            if (r)
            {
                p = res;
            }
            else if (CallInfo.Value.Target == -1)
            {
                p = new();
            }
            else
            {
                p = Stack[cbase + CallInfo.Value.Target];
            }

            if (p.Type != ExObjType.NULL || _forcereturn)
            {
                if (a0 != 985 || interactive)
                {
                    #region _
                    /*
                    if (mac)
                    {
                        p.Assign(Stack[StackBase - a0]);
                    }else*/
                    #endregion
                    p.Assign(mkbool ? new(Stack[StackBase + a1].GetBool()) : Stack[StackBase + a1]);

                    bool seq = CallInfo.Value.Closure.Value._Closure.Function.IsSequence();
                    if (seq)
                    {
                        CallInfo.Value.Closure.GetClosure().DefaultParams.Add(new(p));
                        CallInfo.Value.Closure.GetClosure().Function.Parameters.Add(new(Stack[StackBase + 1].GetInt().ToString()));
                    }

                    if (!LeaveFrame(seq))
                    {
                        throw new Exception("something went wrong with the stack!");
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
            int a_mask = (int)a.Type | (int)b.Type;
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
                AddToErrorMessage("bitwise op between '" + a.Type.ToString() + "' and '" + b.Type.ToString() + "'");
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
                if (row.Type != ExObjType.ARRAY)
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
                        total += row[k].GetFloat() * B[k].Value.l_List[j].GetFloat();
                    }

                    r[i].Value.l_List.Add(new(total));
                }

            }

            res = new(r);
            return true;
        }

        public bool DoMatrixMltOP(OPC op, ExObject a, ExObject b, ref ExObject res)
        {
            if (a.Type != ExObjType.ARRAY || b.Type != ExObjType.ARRAY)
            {
                AddToErrorMessage("can't do matrix multiplication with non-list types");
                return false;
            }

            return MatrixMultiplication(a.GetList(), b.GetList(), ref res);
        }

        public bool DoCartesianProductOP(ExObject a, ExObject b, ref ExObject res)
        {
            if (a.Type != ExObjType.ARRAY || b.Type != ExObjType.ARRAY)
            {
                AddToErrorMessage("can't get cartesian product of non-list types");
                return false;
            }

            int ac = a.GetList().Count;
            int bc = b.GetList().Count;
            List<ExObject> r = new(ac * bc);

            for (int i = 0; i < ac; i++)
            {
                ExObject ar = a.Value.l_List[i];
                for (int j = 0; j < bc; j++)
                {
                    r.Add(new(new List<ExObject>(2) { new(ar), new(b.Value.l_List[j]) }));
                }
            }
            res = new(r);

            return true;
        }

        public bool DoArithmeticOP(OPC op, ExObject a, ExObject b, ref ExObject res)
        {
            if (a.Type == ExObjType.NULL && a.Value.s_String != null)
            {
                a.Type = ExObjType.STRING;
            }
            if (b.Type == ExObjType.NULL && b.Value.s_String != null)
            {
                b.Type = ExObjType.STRING;
            }
            int a_mask = (int)a.Type | (int)b.Type;
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
                        if (a.Type == ExObjType.INTEGER)
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
                        if (a.Type == ExObjType.FLOAT)
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
                        res = new(a.Type == ExObjType.NULL ? ("null" + b.GetString()) : (a.GetString() + "null"));
                        break;
                    }
                case (int)ArithmeticMask.STRINGBOOL:
                    {
                        if (op != OPC.ADD)
                        {
                            goto default;
                        }
                        res = new(a.Type == ExObjType.BOOL ? (a.GetBool().ToString().ToLower() + b.GetString()) : (a.GetString() + b.GetBool().ToString().ToLower()));
                        break;
                    }
                case (int)ArithmeticMask.STRINGCOMPLEX:
                    {
                        if (op != OPC.ADD)
                        {
                            goto default;
                        }
                        if (a.Type == ExObjType.STRING)
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
                        if (a.Type == ExObjType.STRING)
                        {
                            res = new(a.GetString() + (b.Type == ExObjType.INTEGER ? b.GetInt() : b.GetFloat()));
                        }
                        else
                        {
                            res = new((a.Type == ExObjType.INTEGER ? a.GetInt() : a.GetFloat()) + b.GetString());
                        }
                        break;
                    }
                default:
                    {
                        if (DoArithmeticMetaOP(op, a, b, ref res))
                        {
                            return true;
                        }
                        AddToErrorMessage("can't do " + op.ToString() + " operation between " + a.Type.ToString() + " and " + b.Type.ToString());
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
            nMetaCalls++;
            bool b = Call(ref cls, nargs, StackTop - nargs, ref res, true);
            nMetaCalls--;
            Pop(nargs);
            return b;
        }

        public enum ArithmeticMask
        {
            INT = ExObjType.INTEGER,
            INTCOMPLEX = ExObjType.COMPLEX | ExObjType.INTEGER,

            FLOAT = ExObjType.FLOAT,
            FLOATINT = ExObjType.INTEGER | ExObjType.FLOAT,
            FLOATCOMPLEX = ExObjType.COMPLEX | ExObjType.FLOAT,

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
            if (x.Type == y.Type)
            {
                switch (x.Type)
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
                        CheckEqual(x.Value._NativeClosure.Name, y.Value._NativeClosure.Name, ref res);
                        break;
                    case ExObjType.CLOSURE:
                        CheckEqual(x.Value._Closure.Function.Name, y.Value._Closure.Function.Name, ref res);
                        break;
                    case ExObjType.ARRAY:
                        {
                            if (x.Value.l_List.Count != y.Value.l_List.Count)
                            {
                                res = false;
                                break;
                            }
                            res = true;
                            for (int i = 0; i < x.Value.l_List.Count; i++)
                            {
                                ExObject r = x.Value.l_List[i];
                                if (!res)
                                {
                                    break;
                                }
                                if (!CheckEqual(r, y.Value.l_List[i], ref res))
                                {
                                    return false;
                                }
                            }
                            break;
                        }
                    default:
                        res = x == y;   // TO-DO
                        break;
                }
            }
            else
            {
                bool bx = x.IsNumeric();
                bool by = y.IsNumeric();
                if (by && x.Type == ExObjType.COMPLEX)
                {
                    res = x.GetComplex() == y.GetFloat();
                }
                else if (bx && y.Type == ExObjType.COMPLEX)
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
            return i.arg3 != 0 ? CallInfo.Value.Literals[(int)i.arg1] : GetTargetInStack(i.arg1);
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
            switch (self.Type)
            {
                case ExObjType.DICT:
                    {
                        if (self.Value.d_Dict == null)
                        {
                            AddToErrorMessage("attempted to access null dictionary");
                            return false;
                        }

                        if (!self.Value.d_Dict.ContainsKey(k.GetString()))
                        {
                            self.Value.d_Dict.Add(k.GetString(), new());
                        }
                        self.Value.d_Dict[k.GetString()].Assign(v);
                        return true;
                    }
                case ExObjType.ARRAY:
                    {
                        if (k.IsNumeric())
                        {
                            if (self.Value.l_List == null)
                            {
                                AddToErrorMessage("attempted to access null array");
                                return false;
                            }

                            int n = (int)k.GetInt();
                            int l = self.Value.l_List.Count;
                            if (Math.Abs(n) < l)
                            {
                                if (n < 0)
                                {
                                    n = l + n;
                                }
                                self.Value.l_List[n].Assign(v);
                                return true;
                            }
                            else
                            {
                                AddToErrorMessage("array index error: count " + self.Value.l_List.Count + " idx: " + k.GetInt());
                                return false;
                            }
                        }
                        AddToErrorMessage("can't index array with " + k.Type.ToString());
                        return false;
                    }
                case ExObjType.INSTANCE:
                    {
                        if (self.Value._Instance == null)
                        {
                            AddToErrorMessage("attempted to access null instance");
                            return false;
                        }

                        if (self.Value._Instance.Class.Members.ContainsKey(k.GetString())
                            && self.Value._Instance.Class.Members[k.GetString()].IsField())
                        {
                            self.Value._Instance.MemberValues[self.Value._Instance.Class.Members[k.GetString()].GetMemberID()].Assign(new ExObject(v));
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

                                if (v.GetString().Length != 1)
                                {
                                    AddToErrorMessage("expected single character for string setter");
                                    return false;
                                }

                                self.SetString(self.GetString().Substring(0, n) + v.GetString() + self.GetString()[(n + 1)..l]);
                                return true;
                            }
                            AddToErrorMessage("string index error. count " + self.GetString().Length + " idx " + k.GetInt());
                            return false;
                        }
                        break;
                    }
                case ExObjType.CLOSURE:
                    {
                        if (k.Type == ExObjType.STRING)
                        {
                            foreach (ExClassMem c in self.GetClosure().Base.Methods)
                            {
                                if (c.Value.GetClosure().Function.Name.GetString() == self.GetClosure().Function.Name.GetString())
                                {
                                    if (c.Attributes.Type == ExObjType.DICT && c.Attributes.GetDict().ContainsKey(k.GetString()))
                                    {
                                        c.Attributes.GetDict()[k.GetString()].Assign(v);
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
                if (RootDictionary.Value.d_Dict.ContainsKey(k.GetString()))
                {
                    RootDictionary.Value.d_Dict[k.GetString()].Assign(v);
                    return true;
                }
            }

            AddToErrorMessage("key error: " + k.GetString());
            return false;
        }
        public ExFallback SetterFallback(ExObject self, ExObject k, ref ExObject v)
        {
            switch (self.Type)
            {
                case ExObjType.DICT:
                    {
                        if (self.GetInstance().Delegate != null)
                        {
                            if (Setter(self.GetInstance().Delegate, k, ref v, ExFallback.DONT))
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
                            nMetaCalls++;
                            //TO-DO Auto dec metacalls
                            if (Call(ref cls, 3, StackTop - 3, ref t))
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
            switch (self.Type)
            {
                case ExObjType.CLASS:
                    {
                        del = SharedState.ClassDelegate.Value.d_Dict;
                        break;
                    }
                case ExObjType.INSTANCE:
                    {
                        del = SharedState.InstanceDelegate.Value.d_Dict;
                        break;
                    }
                case ExObjType.DICT:
                    {
                        del = SharedState.DictDelegate.Value.d_Dict;
                        break;
                    }
                case ExObjType.ARRAY:
                    {
                        del = SharedState.ListDelegate.Value.d_Dict;
                        break;
                    }
                case ExObjType.STRING:
                    {
                        del = SharedState.StringDelegate.Value.d_Dict;
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        del = SharedState.ComplexDelegate.Value.d_Dict;
                        break;
                    }
                case ExObjType.INTEGER:
                case ExObjType.FLOAT:
                case ExObjType.BOOL:
                    {
                        del = SharedState.NumberDelegate.Value.d_Dict;
                        break;
                    }
                case ExObjType.CLOSURE:
                case ExObjType.NATIVECLOSURE:
                    {
                        del = SharedState.ClosureDelegate.Value.d_Dict;
                        break;
                    }
                case ExObjType.WEAKREF:
                    {
                        del = SharedState.WeakRefDelegate.Value.d_Dict;
                        break;
                    }
            }
            if (del.ContainsKey(k.GetString()))
            {
                dest = new(del[k.GetString()].GetNClosure());
                return true;
            }
            return false;
        }

        public ExFallback GetterFallback(ExObject self, ExObject k, ref ExObject dest)
        {
            switch (self.Type)
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
                            nMetaCalls++;
                            //TO-DO Auto dec metacalls
                            if (Call(ref cls, 2, StackTop - 2, ref dest))
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
        public bool Getter(ref ExObject self, ref ExObject k, ref ExObject dest, bool raw, ExFallback f, bool bExists = false)
        {
            switch (self.Type)
            {
                case ExObjType.DICT:
                    {
                        if (self.Value.d_Dict == null)
                        {
                            AddToErrorMessage("attempted to access null dictionary");
                            return false;
                        }

                        if (self.Value.d_Dict.ContainsKey(k.GetString()))
                        {
                            dest.Assign(new ExObject(self.Value.d_Dict[k.GetString()]));
                            return true;
                        }

                        break;
                    }
                case ExObjType.ARRAY:
                    {
                        if (self.Value.l_List == null)
                        {
                            AddToErrorMessage("attempted to access null array");
                            return false;
                        }

                        if (k.IsNumeric() && !bExists)
                        {
                            if (!bExists && self.Value.l_List.Count != 0 && self.Value.l_List.Count > k.GetInt())
                            {
                                dest.Assign(new ExObject(self.Value.l_List[(int)k.GetInt()]));
                                return true;
                            }
                            else
                            {
                                if (!bExists)
                                {
                                    AddToErrorMessage("array index error: count " + self.Value.l_List.Count + ", idx: " + k.GetInt());
                                }
                                else
                                {
                                    bool found = false;
                                    foreach (ExObject o in self.Value.l_List)
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
                        else if (bExists)
                        {
                            bool found = false;
                            foreach (ExObject o in self.Value.l_List)
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
                        if (self.Value._Instance == null)
                        {
                            AddToErrorMessage("attempted to access null instance");
                            return false;
                        }

                        if (self.Value._Instance.Class.Members.ContainsKey(k.GetString()))
                        {
                            dest.Assign(new ExObject(self.Value._Instance.Class.Members[k.GetString()]));
                            if (dest.IsField())
                            {
                                ExObject o = new(self.Value._Instance.MemberValues[dest.GetMemberID()]);
                                dest.Assign(o.Type == ExObjType.WEAKREF ? o.Value._WeakRef.ReferencedObject : o);
                            }
                            else
                            {
                                dest.Assign(new ExObject(self.Value._Instance.Class.Methods[dest.GetMemberID()].Value));
                            }
                            return true;
                        }
                        break;
                    }
                case ExObjType.CLASS:
                    {
                        if (self.Value._Class == null)
                        {
                            AddToErrorMessage("attempted to access null class");
                            return false;
                        }
                        if (self.Value._Class.Members.ContainsKey(k.GetString()))
                        {
                            dest.Assign(new ExObject(self.Value._Class.Members[k.GetString()]));
                            if (dest.IsField())
                            {
                                ExObject o = new(self.Value._Class.DefaultValues[dest.GetMemberID()].Value);
                                dest.Assign(o.Type == ExObjType.WEAKREF ? o.Value._WeakRef.ReferencedObject : o);
                            }
                            else
                            {
                                dest.Assign(new ExObject(self.Value._Class.Methods[dest.GetMemberID()].Value));
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
                            if (!bExists)
                            {
                                AddToErrorMessage("string index error. count " + self.GetString().Length + " idx " + k.GetInt());
                            }
                            return false;
                        }
                        else if (bExists)
                        {
                            return self.GetString().IndexOf(k.GetString()) != -1;
                        }
                        break;
                    }
                case ExObjType.SPACE:
                    {
                        if (bExists)
                        {
                            return IsInSpace(k, self.Value.c_Space, 1, false);
                        }

                        goto default;

                    }
                case ExObjType.CLOSURE:
                    {
                        if (bExists)
                        {
                            if (!self.GetClosure().Function.IsCluster())
                            {
                                goto default;
                            }

                            List<ExObject> lis = k.Type != ExObjType.ARRAY
                                    ? new() { k }
                                    : k.Value.l_List;

                            if (!DoClusterParamChecks(self.Value._Closure, lis))
                            {
                                return false;
                            }

                            ExObject tmp = new();
                            Push(self);
                            Push(RootDictionary);

                            int nargs = 2;
                            if (self.Value._Closure.DefaultParams.Count == 1)
                            {
                                Push(lis);
                            }
                            else
                            {
                                nargs += lis.Count - 1;
                                PushParse(lis);
                            }

                            if (!Call(ref self, nargs, StackTop - nargs, ref tmp, true))
                            {
                                Pop(nargs + 1);
                                return false;
                            }
                            Pop(nargs + 1);
                            return tmp.GetBool();
                        }

                        if (k.Type == ExObjType.STRING)
                        {
                            switch (k.GetString())
                            {
                                case "vargs":
                                    {
                                        dest = new(self.GetClosure().Function.HasVargs);
                                        return true;
                                    }
                                case "n_params":
                                    {
                                        dest = new(self.GetClosure().Function.nParams - 1);
                                        return true;
                                    }
                                case "n_defparams":
                                    {
                                        dest = new(self.GetClosure().Function.nDefaultParameters);
                                        return true;
                                    }
                                case "n_minargs":
                                    {
                                        dest = new(self.GetClosure().Function.nParams - 1 - self.GetClosure().Function.nDefaultParameters);
                                        return true;
                                    }
                                case "defparams":
                                    {
                                        int ndef = self.GetClosure().Function.nDefaultParameters;
                                        int npar = self.GetClosure().Function.nParams - 1;
                                        int start = npar - ndef;
                                        Dictionary<string, ExObject> dict = new();
                                        foreach (ExObject d in self.GetClosure().DefaultParams)
                                        {
                                            dict.Add((++start).ToString(), d);
                                        }
                                        dest = new(dict);
                                        return true;
                                    }
                                default:
                                    {
                                        ExClass c = self.GetClosure().Base;

                                        string mem = self.GetClosure().Function.Name.GetString();
                                        string attr = k.GetString();
                                        int memid = c.Members[mem].GetMemberID();

                                        if (c.Methods[memid].Attributes.GetDict().ContainsKey(attr))
                                        {
                                            dest = new ExObject(c.Methods[memid].Attributes.GetDict()[attr]);
                                            return true;
                                        }

                                        AddToErrorMessage("unknown attribute '" + attr + "'");
                                        return false;
                                    }
                            }
                        }
                        goto default;

                    }
                case ExObjType.WEAKREF:
                case ExObjType.COMPLEX:
                    {
                        break;
                    }
                default:
                    {
                        if (!bExists)
                        {
                            AddToErrorMessage("can't index '" + self.Type.ToString() + "' with '" + k.Type.ToString() + "'");
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
                if (RootDictionary.GetDict().ContainsKey(k.GetString()))
                {
                    dest.Assign(RootDictionary.GetDict()[k.GetString()]);
                    return true;
                }
            }

            return false;
        }

        public ExObject GetTargetInStack(ExInstr i)
        {
            return Stack[StackBase + (int)i.arg0];
        }
        public ExObject GetTargetInStack(int i)
        {
            return Stack[StackBase + i];
        }
        public ExObject GetTargetInStack(long i)
        {
            return Stack[StackBase + (int)i];
        }

        public static void SwapObjects(ExObject x, ref ExObject y)
        {
            ExObjType t = x.Type;
            ExObjVal v = x.Value;
            x.Type = y.Type;
            x.Value = y.Value;
            y.Type = t;
            y.Value = v;
        }

        private static bool IncludesType(int t1, int t2)
        {
            return (t1 & t2) != 0;
        }

        public void CloseOuters(int idx)
        {
            ExOuter o;
            while ((o = Outers) != null && idx-- > 0)
            {
                Outers = o._next;
                o.Release();
                if(o.Index == -1)
                {
                    Outers = null;
                }
            }
        }

        public bool EnterFrame(int newb, int newt, bool tail)
        {
            if (!tail)
            {
                if (CallStackSize == AllocatedCallSize)
                {
                    AllocatedCallSize *= 2;
                    ExUtils.ExpandListTo(CallStack, AllocatedCallSize);
                }

                CallInfo = Node<ExCallInfo>.BuildNodesFromList(CallStack, CallStackSize++);

                CallInfo.Value.PrevBase = newb - StackBase;
                CallInfo.Value.PrevTop = StackTop - StackBase;
                CallInfo.Value.nCalls = 1;
                CallInfo.Value.IsRootCall = false;
            }
            else
            {
                CallInfo.Value.nCalls++;
            }

            StackBase = newb;
            StackTop = newt;

            if (newt + 15 > Stack.Count)
            {
                if (nMetaCalls > 0)
                {
                    throw new Exception("stack overflow, cant resize while in metamethod");
                }
                ExUtils.ExpandListTo(Stack, 15 << 2);
                // TO-DO Check if reloacteouters is needed
            }
            return true;
        }

        public bool LeaveFrame(bool reset = false)
        {
            int last_t = StackTop;
            int last_b = StackBase;
            int css = --CallStackSize;

            if (reset)
            {
                if (IsMainCall && (css <= 0 || (css > 0 && CallStack[css - 1].IsRootCall)))
                {   // TO-DO refactor
                    List<ExObject> dp = new();
                    List<ExObject> ps = new();

                    for (int i = 0; i < CallInfo.Value.Closure.Value._Closure.Function.nParams; i++)
                    {
                        ps.Add(new(CallInfo.Value.Closure.Value._Closure.Function.Parameters[i]));
                    }
                    for (int i = 0; i < CallInfo.Value.Closure.Value._Closure.Function.nDefaultParameters; i++)
                    {
                        dp.Add(new(CallInfo.Value.Closure.Value._Closure.DefaultParams[i]));
                    }
                    CallInfo.Value.Closure.Value._Closure.DefaultParams = new(dp);
                    CallInfo.Value.Closure.Value._Closure.Function.Parameters = new(ps);
                }
            }

            CallInfo.Value.Closure.Nullify();
            StackBase -= CallInfo.Value.PrevBase;
            StackTop = StackBase + CallInfo.Value.PrevTop;

            bool leaving_root = CallInfo.Value.IsRootCall;

            if (css > 0)
            {
                CallInfo.Value = CallStack[css - 1];
            }
            else
            {
                CallInfo.Value = null;
            }

            if (Outers != null)
            {
                CloseOuters(last_b);
            }

            if (last_t >= Stack.Count)
            {
                AddToErrorMessage("stack overflow! Allocate more stack room for these operations");
                return false;
            }
            // TO-DO this isnt really necessary, GC will still do its job
            //     leave it as is until a proper way is found for returning values from main without "return" statement
            while (leaving_root && last_t >= StackTop)
            {
                if (Stack[last_t].Type == ExObjType.CLOSURE)
                {
                    if (Stack[last_t].GetClosure().Function.Name.Type == ExObjType.NULL) // TO-DO fix this, lambda funcs get removed
                    {
                        last_t--;
                    }
                    else
                    {
                        Stack[last_t--].Nullify();
                    }
                }
                else
                {
                    Stack[last_t--].Nullify();
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
            int nparamscheck = cls.nParameterChecks;
            int new_top = newb + narg + cls.nOuters;

            if (nNativeCalls + 1 > 100)
            {
                throw new Exception("Native stack overflow");
            }

            if (((nparamscheck > 0) && (nparamscheck != narg)) ||
            ((nparamscheck < 0) && (narg < (-nparamscheck))))
            {
                if (nparamscheck < 0)
                {
                    AddToErrorMessage("'" + cls.Name.GetString() + "' takes minimum " + (-nparamscheck - 1) + " arguments");
                    return false;
                }
                AddToErrorMessage("'" + cls.Name.GetString() + "' takes exactly " + (nparamscheck - 1) + " arguments");
                return false;
            }

            List<int> ts = cls.TypeMasks;
            int t_n = ts.Count;

            if (t_n > 0)
            {
                if (nparamscheck < 0 && t_n < narg)
                {
                    AddToErrorMessage("'" + cls.Name.GetString() + "' takes maximum " + (t_n - 1) + " arguments");
                    return false;
                }

                for (int i = 0; i < narg && i < t_n; i++)
                {
                    ExObjType typ = Stack[newb + i].Type;
                    if (typ == ExObjType.DEFAULT)
                    {
                        if (cls.DefaultValues.ContainsKey(i))
                        {
                            Stack[newb + i].Assign(cls.DefaultValues[i]);
                        }
                        else
                        {
                            AddToErrorMessage("can't use non-existant default value for parameter " + (i));
                            return false;
                        }
                    }
                    else if (ts[i] != -1 && !IncludesType((int)typ, ts[i]))
                    {
                        AddToErrorMessage("invalid parameter type, expected one of " + ExAPI.GetExpectedTypes(ts[i]) + ", got: " + Stack[newb + i].Type.ToString());
                        return false;
                    }
                }
            }
            if (!EnterFrame(newb, new_top, false))
            {
                return false;
            }

            CallInfo.Value.Closure = new(cls);

            int outers = cls.nOuters;
            for (int i = 0; i < outers; i++)
            {
                Stack[newb + narg + i].Assign(cls.OutersList[i]);
            }

            if (cls.WeakReference != null)
            {
                Stack[newb].Assign(cls.WeakReference.ReferencedObject);
            }

            nNativeCalls++;
            int ret = cls.Function.Invoke(this, narg - 1);
            nNativeCalls--;

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
                ExitCode = (int)Stack[StackTop - 1].GetInt();
                ExitCalled = true;
                return false;
            }
            else
            {
                o.Assign(Stack[StackTop - 1]);
            }

            if (!LeaveFrame())
            {
                throw new Exception("something went wrong with the stack!");
            }
            return true;
        }

        public bool _forcereturn;

        public bool Call(ref ExObject cls, long nparams, long stackbase, ref ExObject o, bool forcereturn = false)
        {
            return Call(ref cls, (int)nparams, (int)stackbase, ref o, forcereturn);
        }

        public bool Call(ref ExObject cls, int nparams, int stackbase, ref ExObject o, bool forcereturn = false)
        {
            bool f = _forcereturn;
            _forcereturn = forcereturn;
            switch (cls.Type)
            {
                case ExObjType.CLOSURE:
                    {
                        bool state = Execute(cls, nparams, stackbase, ref o);
                        if (state)
                        {
                            nNativeCalls--;
                        }
                        _forcereturn = f;
                        return state;
                    }
                case ExObjType.NATIVECLOSURE:
                    {
                        bool s = CallNative(cls.Value._NativeClosure, nparams, stackbase, ref o);
                        _forcereturn = f;
                        return s;
                    }
                case ExObjType.CLASS:
                    {
                        ExObject cn = new();
                        ExObject tmp = new();

                        CreateClassInst(cls.Value._Class, ref o, cn);
                        if (cn.Type != ExObjType.NULL)
                        {
                            Stack[stackbase].Assign(o);
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
