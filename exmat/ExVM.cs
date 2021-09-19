using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using ExMat.API;
using ExMat.Closure;
using ExMat.Exceptions;
using ExMat.ExClass;
using ExMat.FuncPrototype;
using ExMat.InfoVar;
using ExMat.Objects;
using ExMat.OPs;
using ExMat.Outer;
using ExMat.States;
using ExMat.Utils;

namespace ExMat.VM
{
    /// <summary>
    /// A virtual machine model to execute instructions which use <see cref="ExOperationCode"/>
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExVM : IDisposable
    {
        /// <summary>
        /// Time when the virtual machine was first initialized
        /// </summary>
        public readonly DateTime StartingTime = DateTime.Now;

        /// <summary>
        /// Stores the starting directory
        /// </summary>
        public readonly string StartDirectory = Directory.GetCurrentDirectory();

        /// <summary>
        /// Shared state model to access some of compilation time variables
        /// </summary>
        public ExSState SharedState = new();

        /// <summary>
        /// Virtual memory stack to do push and pop operations with objects
        /// </summary>
        public ExObject[] Stack;            // Sanal bellek
        /// <summary>
        /// Virtual memory size
        /// </summary>
        public int StackSize;
        /// <summary>
        /// Virtual memory stack's base index used by the current scope
        /// </summary>
        public int StackBase;                   // Anlık bellek taban indeksi
        /// <summary>
        /// Virtual memory stack's top index used by the current scope
        /// </summary>
        public int StackTop;                    // Anlık bellek tavan indeksi

        /// <summary>
        /// Call stack for scoped instructions
        /// </summary>
        public List<ExCallInfo> CallStack;      // Çağrı yığını
        /// <summary>
        /// Call stack as linked list
        /// </summary>
        public ExNode<ExCallInfo> CallInfo;       // Çağrı bağlı listesi
        /// <summary>
        /// Allocated initial size for call stack
        /// </summary>
        public int AllocatedCallSize;           // Yığının ilk boyutu
        /// <summary>
        /// Current size of call stack
        /// </summary>
        public int CallStackSize;               // Yığının anlık boyutu

        /// <summary>
        /// Global(root) dictionary
        /// </summary>
        public ExObject RootDictionary;         // Global tablo
        /// <summary>
        /// Temporary value
        /// </summary>
        public ExObject TempRegistery = new();  // Geçici değer
        /// <summary>
        /// Outer variable information
        /// </summary>
        public ExOuter Outers;                  // Bilinmeyen değişken takibi

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorString;                  // Hata mesajı
        /// <summary>
        /// Error traces
        /// </summary>
        public List<List<int>> ErrorTrace = new();  // Hata izi
        /// <summary>
        /// Error type override from external interrupts
        /// </summary>
        public ExErrorType ErrorOverride = ExErrorType.DEFAULT;
        /// <summary>
        /// Wheter to force raise error after execution
        /// </summary>
        public bool ForceThrow;

        /// <summary>
        /// Native call counter
        /// </summary>
        public int nNativeCalls;                // Yerel fonksiyon çağrı sayısı
        /// <summary>
        /// Meta call counter
        /// </summary>
        public int nMetaCalls;                  // Meta metot çağrı sayısı

        /// <summary>
        /// User using input functions?
        /// </summary>
        public bool GotUserInput;               // Girdi alma fonksiyonu kullanıldı ?
        /// <summary>
        /// Has anything been printed?
        /// </summary>
        public bool PrintedToConsole;           // Konsola çıktı yazıldı ?
        /// <summary>
        /// Is current call the root call?
        /// </summary>
        public bool IsMainCall = true;          // İçinde bulunulan çağrı kök çağrı mı ?
        /// <summary>
        /// Is this VM activated in an interactive console?
        /// </summary>
        public bool IsInteractive;              // İnteraktif konsol kullanımda mı ?

        /// <summary>
        /// Input count for interactive console
        /// </summary>
        public int InputCount;

        /// <summary>
        /// Must exit at the end of current call stack frame
        /// </summary>
        public bool ExitCalled;                 // Çıkış fonksiyonu çağırıldı ?
        /// <summary>
        /// Exit return value
        /// </summary>
        public int ExitCode;                    // Çıkışta dönülecek değer
        /// <summary>
        /// Temporary storage for the state of force returning
        /// </summary>
        public bool _forcereturn;

        /// <summary>
        /// VM's thread
        /// </summary>
        public Thread ActiveThread;

        /// <summary>
        /// Is VM thread currently sleeping?
        /// </summary>
        public bool IsSleeping;

        /// <summary>
        /// Interactive console flags using <see cref="ExInteractiveConsoleFlag"/>
        /// </summary>
        public int Flags;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ErrorTrace.Clear();
                    ErrorTrace = null;
                    ErrorString = null;

                    ExDisposer.DisposeObject(ref TempRegistery);
                    ExDisposer.DisposeObject(ref RootDictionary);
                    ExDisposer.DisposeObject(ref Outers);
                    ExDisposer.DisposeObject(ref SharedState);
                    ExDisposer.DisposeObject(ref CallInfo);

                    ExDisposer.DisposeList(ref CallStack);

                    if (Stack != null)
                    {
                        for (int i = 0; i < StackSize; i++)
                        {
                            ExDisposer.DisposeObject(ref Stack[i]);
                        }

                        Stack = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        /// <summary>
        /// Print given string without new-line at the end
        /// </summary>
        /// <param name="str">Message to print</param>
        public void Print(string str)
        {
            Console.Write(str);
            PrintedToConsole = true;
        }

        /// <summary>
        /// Print given string with new-line at the end
        /// </summary>
        /// <param name="str">Message to print</param>
        public void PrintLine(string str)
        {
            Console.WriteLine(str);
            PrintedToConsole = true;
        }

        /// <summary>
        /// Add message to current error messages
        /// </summary>
        /// <param name="msg">Error message</param>
        /// <returns>Always returns <see cref="ExFunctionStatus.ERROR"/> to make the code more compact</returns>
        public ExFunctionStatus AddToErrorMessage(string msg)
        {
            if (string.IsNullOrEmpty(ErrorString))
            {
                ErrorString = "[ERROR] " + msg;
            }
            else
            {
                ErrorString += "\n[ERROR] " + msg;
            }
            return ExFunctionStatus.ERROR;
        }

        public ExFunctionStatus AddToErrorMessage(string format, params object[] msgs)
        {
            return AddToErrorMessage(string.Format(CultureInfo.CurrentCulture, format, msgs));
        }

        public void Throw(string msg, ExVM vm = null, ExExceptionType type = ExExceptionType.RUNTIME)
        {
            ExApi.Throw(msg, vm ?? this, type);
        }

        /// <summary>
        /// Checks if interactive console has the given flag
        /// </summary>
        /// <param name="flag">Flag to check</param>
        public bool HasFlag(ExInteractiveConsoleFlag flag)
        {
            return ((int)flag & Flags) != 0;
        }

        /// <summary>
        /// Sets given interactive console flag
        /// </summary>
        /// <param name="flag">Flag to set</param>
        public void SetFlag(ExInteractiveConsoleFlag flag)
        {
            Flags |= (int)flag;
        }

        /// <summary>
        /// Removes given interactive console flag
        /// </summary>
        /// <param name="flag">Flag to remove</param>
        public void RemoveFlag(ExInteractiveConsoleFlag flag)
        {
            Flags &= ~(int)flag;
        }

        /// <summary>
        /// Toggles given interactive console flag
        /// </summary>
        /// <param name="flag">Flag to switch/toggle</param>
        public void ToggleFlag(ExInteractiveConsoleFlag flag)
        {
            Flags ^= (int)flag;
        }

        /// <summary>
        /// Initialize the stacks 
        /// </summary>
        /// <param name="stacksize">Virtual stack size</param>
        public void Initialize(int stacksize)
        {
            // Sanal belleği oluştur
            if (stacksize < 0)
            {
                stacksize = 0;
            }
            StackSize = stacksize;

            Stack = new ExObject[stacksize];

            for (int i = 0; i < stacksize; i++)
            {
                Stack[i] = new();
            }

            // Çağrı yığınını oluştur
            AllocatedCallSize = 4;
            ExUtils.InitList(ref CallStack, AllocatedCallSize);
            // Global tabloyu oluştur
            RootDictionary = new(new Dictionary<string, ExObject>());
        }

        /// <summary>
        /// Get <paramref name="nargs"/> values from the top of the stack and return them in a list
        /// </summary>
        /// <param name="args">List to put values in</param>
        /// <param name="nargs">Amount of arguments</param>
        public void FillArgumentArray(List<ExObject> args, int nargs)
        {
            while (nargs > 1)
            {
                args.Add(new(GetAbove(-1)));
                Pop();
                nargs--;
            }
        }

        public string GetSimpleString(ExObject obj)
        {
            switch (obj.Type)
            {
                case ExObjType.COMPLEX:
                case ExObjType.INTEGER:
                case ExObjType.FLOAT:
                case ExObjType.STRING:
                case ExObjType.BOOL:
                case ExObjType.NULL:
                case ExObjType.ARRAY:
                case ExObjType.DICT:
                case ExObjType.NATIVECLOSURE:
                case ExObjType.CLOSURE:
                case ExObjType.SPACE:
                    {
                        return ExApi.GetSimpleString(obj);
                    }
                default:
                    {
                        if (obj.Type == ExObjType.INSTANCE)
                        {
                            ExObject c = new();
                            ExObject res = new();
                            if (obj.GetInstance().GetMetaM(this, ExMetaMethod.STRING, ref c))
                            {
                                return CallMeta(ref c, ExMetaMethod.STRING, 1, ref res) ? res.GetString() : string.Empty;
                            }
                        }
                        return obj.Type.ToString();
                    }
            }
        }

        public string GetArrayString(List<ExObject> lis, bool beauty = false, bool isdictval = false, int maxdepth = 2, string prefix = "")
        {
            if (maxdepth == 0)
            {
                return "ARRAY(" + (lis == null ? "empty" : lis.Count) + ")";
            }
            ExObject temp = new(string.Empty);
            StringBuilder s = new("[");
            int n = 0;

            if (lis == null)
            {
                lis = new();
            }
            int c = lis.Count;

            maxdepth--;

            if (beauty
                && !isdictval
                && c > 0
                && prefix != string.Empty)
            {
                s = new("\n" + prefix + s);
            }

            foreach (ExObject o in lis)
            {
                ToString(o, ref temp, maxdepth, !beauty, beauty, prefix + " ");

                string ts = temp.GetString();
                if (beauty && !isdictval)
                {
                    if (ts.Length < 4)
                    {
                        ts = new string(' ', 8 - ts.Length) + ts;
                    }
                    s.AppendFormat(CultureInfo.CurrentCulture, "{0}{1}", prefix, ts);
                }
                else
                {
                    s.Append(ts);
                }

                n++;
                if (n != c)
                {
                    s.Append(", ");
                }
            }

            if (beauty && !isdictval)
            {
                if (prefix == string.Empty)
                {
                    s.Append(']');
                }
                else
                {
                    s.AppendFormat(CultureInfo.CurrentCulture, "{0}{1}", prefix, ']');
                }
            }
            else
            {
                s.Append(']');
            }

            return s.ToString();
        }

        public string GetDictString(Dictionary<string, ExObject> dict, bool isdictval = false, int maxdepth = 2, int currentdepth = 1)
        {
            if (maxdepth == 0)
            {
                return "DICT(" + (dict == null ? "empty" : dict.Count) + ")";
            }

            ExObject temp = new(string.Empty);
            StringBuilder s = new();

            if (dict == null)
            {
                dict = new();
            }
            int c = dict.Count;

            if (c > 0)
            {
                s.Append('\n').Append('\t', currentdepth - 1).Append('{');
            }
            else
            {
                s.Append('{');
            }

            maxdepth--;

            if (c > 0)
            {
                s.Append('\n').Append('\t', currentdepth - 1);
            }

            foreach (KeyValuePair<string, ExObject> pair in dict)
            {
                ToString(pair.Value, ref temp, maxdepth, true, true, string.Empty, currentdepth + 1);

                s.AppendFormat(CultureInfo.CurrentCulture, "\t{0} = {1}\n", pair.Key, temp.GetString());

            }
            s.Append('\t', currentdepth - 1).Append('}');

            return s.ToString();
        }

        public bool ToString(ExObject obj,
                             ref ExObject res,
                             int maxdepth = 2,
                             bool dval = false,
                             bool beauty = false,
                             string prefix = "",
                             int currentdepth = 1)
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
                        res = new(obj.GetInt().ToString(CultureInfo.CurrentCulture));
                        break;
                    }
                case ExObjType.FLOAT:
                    {
                        res = new(ExApi.GetFloatString(obj));
                        break;
                    }
                case ExObjType.STRING:
                    {
                        res = maxdepth <= 1 || dval ? new("\"" + obj.GetString() + "\"") : new(obj.GetString());
                        break;
                    }
                case ExObjType.BOOL:
                    {
                        res = new(obj.GetBool() ? "true" : "false");
                        break;
                    }
                case ExObjType.NULL:
                    {
                        res = new(obj.ValueCustom.s_String ?? ExMat.NullName);
                        break;
                    }
                case ExObjType.ARRAY:
                    {
                        res = new(GetArrayString(obj.GetList(), beauty, dval, maxdepth, prefix));
                        break;
                    }
                case ExObjType.DICT:
                    {
                        res = new(GetDictString(obj.GetDict(), dval, maxdepth, currentdepth));
                        break;
                    }
                case ExObjType.NATIVECLOSURE:
                    {
                        res = new(string.Format(CultureInfo.CurrentCulture, "NATIVECLOSURE({0}) <{1}> {2}({3})", obj.GetNClosure().Base, obj.GetNClosure().Returns, obj.GetNClosure().Name.GetString(), obj.GetNClosure().GetInfoString()));
                        break;
                    }
                case ExObjType.CLOSURE:
                    {
                        res = new(obj.GetClosure().GetInfoString());
                        break;
                    }
                case ExObjType.SPACE:
                    {
                        res = new(obj.GetSpace().GetSpaceString());
                        break;
                    }
                default:
                    {
                        if (obj.Type == ExObjType.INSTANCE)
                        {
                            ExObject c = new();

                            if (obj.GetInstance().GetMetaM(this, ExMetaMethod.STRING, ref c))
                            {
                                Push(obj);
                                return CallMeta(ref c, ExMetaMethod.STRING, 1, ref res);
                            }
                        }
                        res = new(obj.Type.ToString());
                        break;
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

            ExClass.ExClass cls = self.GetClass();

            if (!braw)
            {
                ExObject meta = cls.MetaFuncs[(int)ExMetaMethod.NEWMEMBER];
                if (ExTypeCheck.IsNotNull(meta))
                {
                    Push(self);
                    Push(key);
                    Push(val);
                    Push(attrs);
                    Push(bstat);
                    return CallMeta(ref meta, ExMetaMethod.NEWMEMBER, 5, ref TempRegistery);
                }
            }

            if (!NewSlot(self, key, val, bstat))
            {
                AddToErrorMessage("failed to create a slot named " + (key.Type == ExObjType.STRING ? "'" + key.GetString() + "'" : ("non-string type <" + key.Type.ToString() + ">")));
                return false;
            }

            if (ExTypeCheck.IsNotNull(attrs))
            {
                cls.SetAttrs(key, attrs);
            }
            return true;
        }

        public bool NewSlot(ExObject self, ExObject key, ExObject val, bool bstat)
        {
            if (ExTypeCheck.IsNull(key))
            {
                AddToErrorMessage("'null' can't be used as index");
                return false;
            }

            switch (self.Type)
            {
                case ExObjType.DICT:
                    {
                        bool raw = true;
                        // TO-DO Check deleg

                        if (raw)
                        {
                            ExObject v = new();
                            v.Assign(val);
                            if (self.GetDict().ContainsKey(key.GetString()))
                            {
                                self.GetDict()[key.GetString()].Assign(v);    // TO-DO should i really allow this ?
                            }
                            else
                            {
                                self.GetDict().Add(key.GetString(), new(v));
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
                        if (!self.GetClass().NewSlot(SharedState, key, val, bstat))
                        {
                            if (self.GetClass().GotInstanced)
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
        public void Pop()
        {
            Stack[--StackTop].Nullify();
        }

        public ExFunctionStatus CleanReturn(long n, Complex o)
        {
            Pop(n); Push(o);
            return ExFunctionStatus.SUCCESS;
        }

        public ExFunctionStatus CleanReturn(long n, string o)
        {
            Pop(n); Push(o);
            return ExFunctionStatus.SUCCESS;
        }

        public ExFunctionStatus CleanReturn(long n, double o)
        {
            Pop(n); Push(o);
            return ExFunctionStatus.SUCCESS;
        }

        public ExFunctionStatus CleanReturn(long n, long o)
        {
            Pop(n); Push(o);
            return ExFunctionStatus.SUCCESS;
        }

        public ExFunctionStatus CleanReturn(long n, bool o)
        {
            Pop(n); Push(o);
            return ExFunctionStatus.SUCCESS;
        }

        public ExFunctionStatus CleanReturn(long n, string[] o)
        {
            Pop(n); Push(o);
            return ExFunctionStatus.SUCCESS;
        }

        public ExFunctionStatus CleanReturn(long n, List<ExObject> o)
        {
            Pop(n); Push(o);
            return ExFunctionStatus.SUCCESS;
        }

        public ExFunctionStatus CleanReturn(long n, Dictionary<string, ExObject> o)
        {
            Pop(n); Push(o);
            return ExFunctionStatus.SUCCESS;
        }

        public ExFunctionStatus CleanReturn<T>(long n, T o) where T : ExObject
        {
            Pop(n); Push(o);
            return ExFunctionStatus.SUCCESS;
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
        public void Push(string[] o)
        {
            Push(ExApi.ListObjFromStringArray(o));
        }

        public void Push(ExInstance o)
        {
            Stack[StackTop++].Assign(o);
        }

        public void Push(ExClass.ExClass o)
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
        public ExObject GetArgument(int idx)
        {
            return Stack[idx + StackBase];
        }
        public ExObject GetRootArgument()
        {
            return Stack[StackBase];
        }
        public ExNativeClosure GetRootClosure()
        {
            return Stack[StackBase - 1].GetNClosure();
        }
        public long GetPositiveIntegerArgument(int idx, long defaultVal = 1)
        {
            long val = GetArgument(idx).GetInt();
            return val <= 0 ? defaultVal : val;
        }

        public long GetPositiveRangedIntegerArgument(int idx, long min = 1, long max = long.MaxValue)
        {
            long val = GetArgument(idx).GetInt();
            return val <= min ? min : val >= max ? max : val;
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

        public static bool CreateClassInst(ExClass.ExClass cls, ref ExObject o, out ExObject cns)
        {
            cns = null;
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
                    if (!IsInSpace(new(lis), ts[0].GetSpace(), 1, false))
                    {
                        return false;
                    }
                }
                else
                {
                    if (t_n != nargs)
                    {
                        AddToErrorMessage("'" + pro.Name.GetString() + "' takes " + t_n + " arguments");
                        return false;
                    }

                    for (int i = 0; i < nargs; i++)
                    {
                        if (!IsInSpace(lis[i], ts[i].GetSpace(), i + 1, false))
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
                    AddToErrorMessage("'" + pro.Name.GetString() + "' takes " + t_n + " arguments");
                    return false;
                }

                for (int i = 0; i < nargs && i < t_n; i++)
                {
                    if (!IsInSpace(Stack[sbase + i + 1], ts[i].GetSpace(), i + 1))
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

        private bool SetVargsInStack(int stackBase, int nArguments, ref int nParameters)
        {
            if (nArguments < --nParameters)    // Yetersiz argüman sayısı
            {
                return false;
            }

            List<ExObject> varglis = new();

            int nVargsArguments = nArguments - nParameters;   // vargs listesine eklenecek argüman sayısı
            int vargsStartIndex = stackBase + nParameters;        // argümanların bellek indeksi başlangıcı
            for (int n = 0; n < nVargsArguments; n++)
            {
                varglis.Add(new(Stack[vargsStartIndex]));    // Argümanları 'vargs' listesine kopyala
                Stack[vargsStartIndex].Nullify();            // Eski objeyi sıfırla
                vargsStartIndex++;
            }

            Stack[stackBase + nParameters].Assign(varglis);       // vargs listesini belleğe yerleştir
            return true;
        }

        private bool DoArgumentChecksInStack(ExClosure closure, int nParameters, int stackBase, int defaultsIndex, ref int nArguments)
        {
            for (int pno = 1; pno < nParameters; pno++)
            {
                if (Stack[stackBase + pno].Type == ExObjType.DEFAULT)
                {
                    if (pno >= defaultsIndex)
                    {
                        Stack[stackBase + pno].Assign(closure.DefaultParams[pno - defaultsIndex]);
                    }
                    else
                    {
                        AddToErrorMessage("can't use non-existant default value for parameter no {0}", pno);
                        return false;
                    }
                }
                else if (pno >= nArguments)
                {
                    Stack[stackBase + pno].Assign(closure.DefaultParams[pno - defaultsIndex]);
                    nArguments++;
                }
            }
            return true;
        }

        private bool SetDefaultValuesInStack(ExPrototype prototype, ExClosure closure, int stackBase, int nParameters, ref int nArguments)
        {
            bool valid;
            int nDefaultParams = prototype.nDefaultParameters, defaultsIndex = nParameters - nDefaultParams;
            if (nParameters != nArguments)       // Argüman sayısı != parametre sayısından
            {
                // Minimum sayıda argüman sağlandığını kontrol et
                if (nDefaultParams > 0 && nArguments < nParameters && (nParameters - nArguments) <= nDefaultParams)
                {
                    valid = DoArgumentChecksInStack(closure, nParameters, stackBase, defaultsIndex, ref nArguments);
                }
                // Beklenen sayıda argüman verilmedi, hata mesajı yaz
                else
                {
                    if (nDefaultParams > 0 && !prototype.IsCluster())
                    {
                        AddToErrorMessage("'" + prototype.Name.GetString() + "' takes min: " + (nParameters - nDefaultParams - 1) + ", max:" + (nParameters - 1) + " arguments");
                    }
                    else // 
                    {
                        AddToErrorMessage("'" + prototype.Name.GetString() + "' takes exactly " + (nParameters - 1) + " arguments");
                    }
                    return false;
                }
            }
            else // Argüman sayısı == Parametre sayısı, ".." sembollerini kontrol et
            {
                valid = DoArgumentChecksInStack(closure, nParameters, stackBase, defaultsIndex, ref nArguments);
            }
            return valid;
        }

        private ExFunctionStatus SetDefaultValuesInStackForSequence(ExClosure closure, int stackBase, int nArguments)
        {
            if (nArguments < 2)
            {
                return AddToErrorMessage("sequences require at least 1 argument to be called");
            }
            // TO-DO CONTINUE HERE, ALLOW PARAMETERS FOR SEQUENCES
            if (!ExTypeCheck.IsNumeric(Stack[stackBase + 1]))
            {
                return AddToErrorMessage("expected integer or float as sequence argument");
            }

            double ind;
            if (Stack[stackBase + 1].Type == ExObjType.INTEGER)
            {
                ind = Stack[stackBase + 1].GetInt();
            }
            else if (Stack[stackBase + 1].Type == ExObjType.FLOAT)
            {
                ind = Stack[stackBase + 1].GetFloat();
            }
            else
            {
                return AddToErrorMessage("expected INTEGER or FLOAT as sequence index");
            }

            string idx = ind.ToString(CultureInfo.CurrentCulture);
            for (int i = 2; i < closure.Function.Parameters.Count; i++)
            {
                ExObject c = closure.Function.Parameters[i];
                if (c.GetString() == idx)
                {
                    // TO-DO doesnt return to main, also refactor this
                    Stack[stackBase - 1].Assign(closure.DefaultParams[i - 2]);
                    return ExFunctionStatus.SUCCESS;
                }
            }
            return ind < 0 ? AddToErrorMessage("index can't be negative, unless its a default value") : ExFunctionStatus.VOID;
        }

        private ExFunctionStatus DoSpecialStartCallChecks(ExClosure closure, int nArguments, int stackBase)
        {
            ExPrototype prototype = closure.Function;
            if (prototype.IsRule())
            {
                int t_n = prototype.LocalInfos.Count;
                if (t_n != nArguments)
                {
                    return AddToErrorMessage("'" + prototype.Name.GetString() + "' takes " + (t_n - 1) + " arguments");
                }
            }
            else if (prototype.IsCluster())
            {
                if (!DoClusterParamChecks(closure, nArguments, stackBase))
                {
                    return ExFunctionStatus.ERROR;
                }
            }
            else if (prototype.IsSequence())
            {
                return SetDefaultValuesInStackForSequence(closure, stackBase, nArguments);
            }

            return ExFunctionStatus.VOID;
        }

        public bool StartCall(ExClosure closure, int targetIndex, int argumentCount, int stackBase, bool isTailCall)
        {
            ExPrototype prototype = closure.Function;     // Fonksiyon bilgisi

            int nParameters = prototype.nParams;           // Parametre sayısı kontrolü
            int newTop = stackBase + prototype.StackSize; // Yeni tavan indeksi
            int nArguments = argumentCount;               // Argüman sayısı

            if (prototype.HasVargs)   // Belirsiz parametre sayısı
            {
                if (!SetVargsInStack(stackBase, nArguments, ref nParameters))
                {
                    AddToErrorMessage("'" + prototype.Name.GetString() + "' takes at least " + (nParameters - 1) + " arguments");
                    return false;
                }
            }
            else if (prototype.IsFunction()
                    && !SetDefaultValuesInStack(prototype, closure, stackBase, nParameters, ref nArguments))        // Parametre sayısı sınırlı fonksiyon
            {
                return false;
            }

            #region Küme, dizi vs. için özel kontroller
            switch (DoSpecialStartCallChecks(closure, nArguments, stackBase))
            {
                case ExFunctionStatus.ERROR: return false;
                case ExFunctionStatus.SUCCESS: return true;
                default: break;
            }

            if (closure.WeakReference != null)
            {
                Stack[stackBase].Assign(closure.WeakReference.ReferencedObject);
            }
            #endregion

            // Çağrı için çerçeve aç
            if (!EnterFrame(stackBase, newTop, isTailCall))
            {
                AddToErrorMessage("failed to create a scope");
                return false;
            }

            // Çağrı bilgisini verilen fonksiyon ile güncelle
            CallInfo.Value.Closure = new(closure);
            CallInfo.Value.Literals = prototype.Literals;
            CallInfo.Value.Instructions = prototype.Instructions;
            CallInfo.Value.InstructionsIndex = 0;
            CallInfo.Value.Target = targetIndex;

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
                            AddToErrorMessage("can't use 'CLUSTER' or 'SPACE' as an argument for parameter {0}", i);
                        }
                        return false;
                    }
                case ExObjType.ARRAY:
                    {
                        if (argument.GetList().Count != space.Dimension && space.Dimension != -1)
                        {
                            if (raise)
                            {
                                AddToErrorMessage("expected {0} dimensions for parameter {1}", space.Dimension, i);
                            }
                            return false;
                        }

                        if (space.Child != null)
                        {
                            foreach (ExObject val in argument.GetList())
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
                                    foreach (ExObject val in argument.GetList())
                                    {
                                        if (!ExTypeCheck.IsNumeric(val))
                                        {
                                            if (raise)
                                            {
                                                AddToErrorMessage("expected real or complex numbers for parameter {0}", i);
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
                                                foreach (ExObject val in argument.GetList())
                                                {
                                                    if (!ExTypeCheck.IsRealNumber(val) || val.GetFloat() <= 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected numeric positive non-zero values for parameter {0}", i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '-':
                                            {
                                                foreach (ExObject val in argument.GetList())
                                                {
                                                    if (!ExTypeCheck.IsRealNumber(val) || val.GetFloat() >= 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected numeric negative non-zero values for parameter {0}", i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '\\':
                                            {
                                                foreach (ExObject val in argument.GetList())
                                                {
                                                    if (!ExTypeCheck.IsRealNumber(val) || val.GetFloat() == 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected numeric non-zero values for parameter {0}", i);
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
                                                foreach (ExObject val in argument.GetList())
                                                {
                                                    if (!ExTypeCheck.IsRealNumber(val) || val.GetFloat() < 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected numeric positive or zero values for parameter {0}", i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '-':
                                            {
                                                foreach (ExObject val in argument.GetList())
                                                {
                                                    if (!ExTypeCheck.IsRealNumber(val) || val.GetFloat() > 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected numeric negative or zero values for parameter {0}", i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '\\':
                                            {
                                                foreach (ExObject val in argument.GetList())
                                                {
                                                    if (!ExTypeCheck.IsRealNumber(val))
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected numeric values for parameter {0}", i);
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
                                                foreach (ExObject val in argument.GetList())
                                                {
                                                    if (val.Type != ExObjType.INTEGER || val.GetFloat() < 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected integer positive or zero values for parameter {0}", i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '-':
                                            {
                                                foreach (ExObject val in argument.GetList())
                                                {
                                                    if (val.Type != ExObjType.INTEGER || val.GetFloat() > 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected integer negative or zero values for parameter {0}", i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '\\':
                                            {
                                                foreach (ExObject val in argument.GetList())
                                                {
                                                    if (val.Type != ExObjType.INTEGER)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected integer values for parameter {0}", i);
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
                                                foreach (ExObject val in argument.GetList())
                                                {
                                                    if (val.Type != ExObjType.INTEGER || val.GetInt() <= 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected integer positive non-zero values for parameter {0}", i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '-':
                                            {
                                                foreach (ExObject val in argument.GetList())
                                                {
                                                    if (val.Type != ExObjType.INTEGER || val.GetInt() >= 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected integer negative non-zero values for parameter {0}", i);
                                                        }
                                                        return false;
                                                    }
                                                }
                                                break;
                                            }
                                        case '\\':
                                            {
                                                foreach (ExObject val in argument.GetList())
                                                {
                                                    if (val.Type != ExObjType.INTEGER || val.GetInt() == 0)
                                                    {
                                                        if (raise)
                                                        {
                                                            AddToErrorMessage("expected integer non-zero values for parameter {0}", i);
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
                                AddToErrorMessage("expected {0} dimensions for parameter {1}", space.Dimension, i);
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

                        if (space.Domain is "E" or "C")
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
                                AddToErrorMessage("expected non-complex number for parameter {0}", i);
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
                                AddToErrorMessage("expected {0} dimensions for parameter {1}", space.Dimension, i);
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
                                                        AddToErrorMessage("expected numeric positive non-zero value for parameter {0}", i);
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
                                                        AddToErrorMessage("expected numeric negative non-zero value for parameter {0}", i);
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
                                                        AddToErrorMessage("expected numeric non-zero value for parameter {0}", i);
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
                                                        AddToErrorMessage("expected numeric positive or zero value for parameter {0}", i);
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
                                                        AddToErrorMessage("expected numeric negative or zero value for parameter {0}", i);
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
                                                        AddToErrorMessage("expected integer positive or zero value for parameter {0}", i);
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
                                                        AddToErrorMessage("expected integer negative or zero value for parameter {0}", i);
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
                                                        AddToErrorMessage("expected integer value for parameter {0}", i);
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
                                                        AddToErrorMessage("expected integer positive non-zero value for parameter {0}", i);
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
                                                        AddToErrorMessage("expected integer negative non-zero value for parameter {0}", i);
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
                                                        AddToErrorMessage("expected integer non-zero value for parameter {0}", i);
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

        public static int IterDictNext(ExObject obj, ExObject rpos, ExObject outk, ExObject outv) // TO-DO optimize
        {
            Dictionary<string, ExObject>.Enumerator e = obj.GetDict().GetEnumerator();
            if (!e.MoveNext())
            {
                return -1;
            }

            if (ExTypeCheck.IsNull(rpos))
            {
                outk.Assign(e.Current.Key);
                outv.Assign(e.Current.Value);
                return 0;
            }
            else
            {
                int idx = (int)rpos.GetInt();
                int c = idx;

                while (c >= 0)
                {
                    c--;
                    if (!e.MoveNext())
                    {
                        return -1;
                    }
                }

                outk.Assign(e.Current.Key);
                outv.Assign(e.Current.Value);
                return ++idx;
            }
        }

        public static int IterStringNext(ExObject obj, ExObject rpos, ExObject outk, ExObject outv)
        {
            string e = obj.GetString();
            if (e.Length == 0)
            {
                return -1;
            }

            if (ExTypeCheck.IsNull(rpos))
            {
                outk.Assign(0);
                outv.Assign(e[0].ToString());
                return 1;
            }
            else
            {
                int idx = (int)rpos.GetInt();
                if (idx >= obj.GetString().Length)
                {
                    return -1;
                }

                outk.Assign(idx);
                outv.Assign(e[idx].ToString());
                return ++idx;
            }
        }

        public static int IterArrayNext(ExObject obj, ExObject rpos, ExObject outk, ExObject outv)
        {
            List<ExObject> e = obj.GetList();
            if (e.Count == 0)
            {
                return -1;
            }

            if (ExTypeCheck.IsNull(rpos))
            {
                outk.Assign(0);
                outv.Assign(e[0]);
                return 1;
            }
            else
            {
                int idx = (int)rpos.GetInt();
                if (idx >= obj.GetList().Count)
                {
                    return -1;
                }

                outk.Assign(idx);
                outv.Assign(e[idx]);
                return ++idx;
            }
        }

        private delegate int Iterator(ExObject obj, ExObject rpos, ExObject outk, ExObject outv);

        private bool DoForeach(ExObject obj, ExObject obj2, ExObject obj3, ExObject obj4, int exit, ref int jmp)
        {
            int ridx;
            Iterator iterator;

            switch (obj.Type)
            {
                case ExObjType.DICT:
                    {
                        iterator = IterDictNext;
                        break;
                    }
                case ExObjType.ARRAY:
                    {
                        iterator = IterArrayNext;
                        break;
                    }
                case ExObjType.STRING:
                    {
                        iterator = IterStringNext;
                        break;
                    }
                case ExObjType.CLASS: // TO-DO iterable class and instances
                case ExObjType.INSTANCE:
                default:
                    {
                        AddToErrorMessage("type '{0}' is not iterable", obj.Type);
                        return false;
                    }
            }


            if ((ridx = iterator(obj, obj4, obj2, obj3)) == -1)
            {
                jmp = exit;
            }
            else
            {
                obj4.Assign(ridx);
                jmp = 1;
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
            if (ForceThrow)
            {
                ForceThrow = false;
            }
            if (ExitCalled)
            {
                return false;
            }
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
                    Throw("something went wrong with the stack!", type: ExExceptionType.BASE);
                }
                if (end)
                {
                    break;
                }
            }

            return false;
        }

        public bool Execute(ExObject closure, int nArguments, int stackBase, ref ExObject resultObject)
        {
            if (nNativeCalls++ > 100)   // Sonsuz döngüye girildi
            {
                Throw("Native stack overflow", type: ExExceptionType.BASE);
            }
            // Fonksiyon kopyasını al
            TempRegistery = new(closure);
            ExNode<ExCallInfo> prevCallInfo = CallInfo;
            // Fonksiyonu çağır
            if (!StartCall(TempRegistery.GetClosure(), StackTop - nArguments, nArguments, stackBase, false))
            {
                return false;
            }
            // Çağrı bilgisi güncellenmediyse işlenecek komut yoktur 
            if (CallInfo == prevCallInfo)
            {
                resultObject.Assign(Stack[StackBase + StackTop - nArguments]);
                return true;
            }
            CallInfo.Value.IsRootCall = true;

            // Komutlar bitene kadar işlemeye başla
            while (true)
            {
                if (ForceThrow)
                {
                    return ForceThrow = false;
                }

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

                // İşlenecek olan komut
                ExInstr instruction = CallInfo.Value.Instructions[CallInfo.Value.InstructionsIndex++];

                switch (instruction.op)
                {
                    case ExOperationCode.LOADINTEGER:   // Tamsayı yükle
                        {
                            GetTargetInStack(instruction).Assign(instruction.arg1);
                            continue;
                        }
                    case ExOperationCode.LOADFLOAT:     // Ondalıklı sayı yükle
                        {
                            GetTargetInStack(instruction).Assign(new DoubleLong() { i = instruction.arg1 }.f);
                            continue;
                        }
                    case ExOperationCode.LOADCOMPLEX:   // Kompleks sayı yükle
                        {
                            if (instruction.arg2 == 1)  // Argüman 2, argüman 1'in ondalıklı olup olmadığını belirtir
                            {
                                GetTargetInStack(instruction).Assign(new Complex(0.0, new DoubleLong() { i = instruction.arg1 }.f));
                            }
                            else
                            {
                                GetTargetInStack(instruction).Assign(new Complex(0.0, instruction.arg1));
                            }
                            continue;
                        }
                    case ExOperationCode.LOADBOOLEAN:   // Boolean yükle
                        {
                            // Derleyicide hazırlanırken 'true' isteniyorsa arg1 = 1, 'false' isteniyorsa arg1 = 0 kullanıldı
                            GetTargetInStack(instruction).Assign(instruction.arg1 == 1);
                            continue;
                        }
                    case ExOperationCode.LOADSPACE:     // Uzay yükle
                        {
                            GetTargetInStack(instruction).Assign(FindSpaceObject((int)instruction.arg1));
                            continue;
                        }
                    case ExOperationCode.LOAD:          // Yazı dizisi, değişken ismi vb. değer yükle
                        {
                            GetTargetInStack(instruction).Assign(CallInfo.Value.Literals[(int)instruction.arg1]);
                            continue;
                        }
                    case ExOperationCode.DLOAD:
                        {
                            GetTargetInStack(instruction).Assign(CallInfo.Value.Literals[(int)instruction.arg1]);
                            GetTargetInStack(instruction.arg2).Assign(CallInfo.Value.Literals[(int)instruction.arg3]);
                            continue;
                        }
                    case ExOperationCode.CALLTAIL:
                        {
                            ExObject tmp = GetTargetInStack(instruction.arg1);
                            if (tmp.Type == ExObjType.CLOSURE)
                            {
                                ExObject c = new(tmp);
                                if (Outers != null)
                                {
                                    CloseOuters(StackBase);
                                }
                                for (int j = 0; j < instruction.arg3; j++)
                                {
                                    GetTargetInStack(j).Assign(GetTargetInStack(instruction.arg2 + j));
                                }
                                if (!StartCall(c.GetClosure(), CallInfo.Value.Target, instruction.arg3, StackBase, true))
                                {
                                    return FixStackAfterError();
                                }
                                continue;
                            }
                            goto case ExOperationCode.CALL;
                        }
                    case ExOperationCode.CALL:  // Fonksiyon veya başka bir obje çağrısı
                        {
                            ExObject obj = GetTargetInStack(instruction.arg1);
                            switch (obj.Type)
                            {
                                case ExObjType.CLOSURE: // Kullanıcı fonksiyonu
                                    {
                                        if (!StartCall(obj.GetClosure(), instruction.arg0, instruction.arg3, StackBase + instruction.arg2, false))
                                        {
                                            return FixStackAfterError();
                                        }
                                        continue;
                                    }
                                case ExObjType.NATIVECLOSURE:   // Yerli fonksiyon
                                    {
                                        if (!CallNative(obj.GetNClosure(), instruction.arg3, StackBase + instruction.arg2, ref obj))
                                        {
                                            return FixStackAfterError();
                                        }

                                        if (instruction.arg0 != ExMat.InvalidArgument)
                                        {
                                            GetTargetInStack(instruction.arg0).Assign(obj);
                                        }
                                        continue;
                                    }
                                case ExObjType.CLASS:   // Sınıf (yeni bir obje oluşturmaya yarar)
                                    {
                                        ExObject instance = new();
                                        if (!CreateClassInst(obj.GetClass(), ref instance, out ExObject constructor))
                                        {
                                            return FixStackAfterError();
                                        }
                                        if (instruction.arg0 != -1)
                                        {
                                            GetTargetInStack(instruction.arg0).Assign(instance);
                                        }

                                        int sbase;
                                        switch (constructor.Type)
                                        {
                                            case ExObjType.CLOSURE:
                                                {
                                                    sbase = StackBase + (int)instruction.arg2;
                                                    Stack[sbase].Assign(instance);
                                                    if (!StartCall(constructor.GetClosure(), -1, instruction.arg3, sbase, false))
                                                    {
                                                        return FixStackAfterError();
                                                    }
                                                    break;
                                                }
                                            case ExObjType.NATIVECLOSURE:
                                                {
                                                    sbase = StackBase + (int)instruction.arg2;
                                                    Stack[sbase].Assign(instance);
                                                    if (!CallNative(constructor.GetNClosure(), instruction.arg3, sbase, ref constructor))
                                                    {
                                                        return FixStackAfterError();
                                                    }
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                case ExObjType.INSTANCE:    // Sınıfa ait obje(gerekli meta metota sahio olması beklenir)
                                    {
                                        ExObject cls2 = null;
                                        if (obj.GetInstance().GetMetaM(this, ExMetaMethod.CALL, ref cls2))
                                        {
                                            Push(obj);
                                            for (int j = 0; j < instruction.arg3; j++)
                                            {
                                                Push(GetTargetInStack(j + instruction.arg2));
                                            }

                                            if (!CallMeta(ref cls2, ExMetaMethod.CALL, instruction.arg3 + 1, ref obj))
                                            {
                                                AddToErrorMessage("meta method failed call");
                                                return FixStackAfterError();
                                            }

                                            if (instruction.arg0 != -1)
                                            {
                                                GetTargetInStack(instruction.arg0).Assign(obj);
                                            }
                                            break;
                                        }
                                        goto default;
                                    }
                                case ExObjType.SPACE:   // Uzay (değişken boyutlara değer atamak amaçlıdır)
                                    {
                                        ExSpace sp_org = GetTargetInStack(instruction.arg1).GetSpace();
                                        ExSpace sp = sp_org.DeepCopy();

                                        int varcount = sp.VarCount();
                                        int argcount = (int)instruction.arg3 - 1;
                                        if (argcount > varcount)
                                        {
                                            AddToErrorMessage("expected maximum " + varcount + " arguments for space");
                                            return FixStackAfterError();
                                        }

                                        ExSpace child = sp;
                                        int sb = (int)instruction.arg2;
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

                                        GetTargetInStack(instruction.arg0).Assign(sp);
                                        break;
                                    }
                                default:    // Bilinmeyen tip
                                    {
                                        AddToErrorMessage("attempt to call " + obj.Type.ToString());
                                        return FixStackAfterError();
                                    }
                            }
                            continue;
                        }
                    case ExOperationCode.PREPCALL:
                    case ExOperationCode.PREPCALLK: // Fonksiyonun bir sonraki komutlar için bulunup hazırlanması
                        {
                            // Aranan metot/fonksiyon veya özellik ismi
                            ExObject name = instruction.op == ExOperationCode.PREPCALLK ? CallInfo.Value.Literals[(int)instruction.arg1] : GetTargetInStack(instruction.arg1);
                            // İçinde ismin aranacağı obje ( tablo veya sınıf gibi )
                            ExObject lookUp = GetTargetInStack(instruction.arg2);
                            // 'lookUp' objesi 'name' in taşıdığı isimli bir değere sahipse 'TempRegistery' içine yükler
                            if (!Getter(lookUp, name, ref TempRegistery, false, (ExFallback)instruction.arg2))
                            {
                                AddToErrorMessage("unknown method or field '" + name.GetString() + "'");
                                return FixStackAfterError();
                            }
                            // Arama yapılan değeri, bulunan değerin kullanması için arg3 hedefine ata
                            GetTargetInStack(instruction.arg3).Assign(lookUp);
                            SwapObjects(GetTargetInStack(instruction), TempRegistery);  // Fonksiyon indeks hedefine ata
                            continue;
                        }
                    case ExOperationCode.DMOVE:
                        {
                            GetTargetInStack(instruction.arg0).Assign(GetTargetInStack(instruction.arg1));
                            GetTargetInStack(instruction.arg2).Assign(GetTargetInStack(instruction.arg3));
                            continue;
                        }
                    case ExOperationCode.MOVE:  // Bir objeyi/değişkeni başka bir objeye/değişkene atama işlemi
                        {
                            // GetTargetInStack(instruction) çağrısı arg0'ı kullanır : Hedef indeks
                            // arg1 : Kaynak indeks
                            GetTargetInStack(instruction).Assign(GetTargetInStack(instruction.arg1));
                            continue;
                        }
                    case ExOperationCode.NEWSLOT:   // Yeni slot oluşturma işlemi
                        {
                            // Komut argümanları:
                            //  arg0 = hedef indeks, ExMat.InvalidArgument ise slota atanan değeri dönmez
                            //  arg1 = slot oluşturulacak objenin indeksi
                            //  arg2 = oluşturulacak slotun isminin indeksi
                            //  arg3 = slota atanacak değerin indeksi
                            if (!NewSlot(GetTargetInStack(instruction.arg1), GetTargetInStack(instruction.arg2), GetTargetInStack(instruction.arg3), false))
                            {
                                return FixStackAfterError();
                            }
                            if (instruction.arg0 != ExMat.InvalidArgument)
                            {
                                GetTargetInStack(instruction).Assign(GetTargetInStack(instruction.arg3));
                            }
                            continue;
                        }
                    case ExOperationCode.DELETE:
                        {
                            ExObject r = new(GetTargetInStack(instruction));
                            if (!RemoveObjectSlot(GetTargetInStack(instruction.arg1), GetTargetInStack(instruction.arg2), ref r))
                            {
                                AddToErrorMessage("failed to delete a slot");
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case ExOperationCode.SET:
                        {
                            ExObject t = new(GetTargetInStack(instruction.arg3));
                            if (!Setter(GetTargetInStack(instruction.arg1), GetTargetInStack(instruction.arg2), ref t, ExFallback.OK))
                            {
                                return FixStackAfterError();
                            }
                            if (instruction.arg0 != ExMat.InvalidArgument)
                            {
                                GetTargetInStack(instruction).Assign(GetTargetInStack(instruction.arg3));
                            }
                            continue;
                        }
                    case ExOperationCode.GET:
                        {
                            if (!Getter(new(GetTargetInStack(instruction.arg1)), new(GetTargetInStack(instruction.arg2)), ref TempRegistery, false, (ExFallback)instruction.arg1))
                            {
                                return FixStackAfterError();
                            }
                            SwapObjects(GetTargetInStack(instruction), TempRegistery);
                            continue;
                        }
                    case ExOperationCode.GETK:
                        {
                            if (!Getter(GetTargetInStack(instruction.arg2), CallInfo.Value.Literals[(int)instruction.arg1], ref TempRegistery, false, (ExFallback)instruction.arg2))
                            {
                                AddToErrorMessage("unknown variable '" + CallInfo.Value.Literals[(int)instruction.arg1].GetString() + "'"); // access to local var decl before
                                return FixStackAfterError();
                            }
                            SwapObjects(GetTargetInStack(instruction), TempRegistery);
                            continue;
                        }
                    case ExOperationCode.EQ:
                    case ExOperationCode.NEQ:
                        {
                            bool res = false;
                            if (!ExApi.CheckEqual(GetTargetInStack(instruction.arg2), GetConditionFromInstr(instruction), ref res))
                            {
                                AddToErrorMessage("equal op failed");
                                return FixStackAfterError();
                            }
                            GetTargetInStack(instruction).Assign(instruction.op == ExOperationCode.EQ ? res : !res);
                            continue;
                        }
                    case ExOperationCode.ADD:
                    case ExOperationCode.SUB:
                    case ExOperationCode.MLT:
                    case ExOperationCode.EXP:
                    case ExOperationCode.DIV:
                    case ExOperationCode.MOD:
                        {
                            // arg1 = sağ tarafın indeksi
                            // arg2 = sol tarafın indeksi
                            ExObject res = new();   // sonucun saklanacağı obje
                            if (!DoArithmeticOP(instruction.op, GetTargetInStack(instruction.arg2), GetTargetInStack(instruction.arg1), ref res))
                            {
                                return FixStackAfterError();
                            }
                            GetTargetInStack(instruction).Assign(res);  // arg0 hedef indeksi
                            continue;
                        }
                    case ExOperationCode.MMLT:
                        {
                            ExObject res = new();
                            if (!DoMatrixMltOP(ExOperationCode.MMLT, GetTargetInStack(instruction.arg2), GetTargetInStack(instruction.arg1), ref res))
                            {
                                return FixStackAfterError();
                            }
                            GetTargetInStack(instruction).Assign(res);
                            continue;
                        }
                    case ExOperationCode.CARTESIAN:
                        {
                            ExObject res = new();
                            if (!DoCartesianProductOP(GetTargetInStack(instruction.arg2), GetTargetInStack(instruction.arg1), ref res))
                            {
                                return FixStackAfterError();
                            }
                            GetTargetInStack(instruction).Assign(res);
                            continue;
                        }
                    case ExOperationCode.BITWISE:
                        {
                            if (!DoBitwiseOP(instruction.arg3, GetTargetInStack(instruction.arg2), GetTargetInStack(instruction.arg1), GetTargetInStack(instruction)))
                            {
                                AddToErrorMessage("bitwise op between '" + GetTargetInStack(instruction.arg2).Type.ToString() + "' and '" + GetTargetInStack(instruction.arg1).Type.ToString() + "'");
                                return FixStackAfterError();
                            }

                            continue;
                        }
                    case ExOperationCode.RETURNBOOL:
                    case ExOperationCode.RETURN:
                        {
                            if (ReturnValue((int)instruction.arg0, (int)instruction.arg1, ref TempRegistery, instruction.op == ExOperationCode.RETURNBOOL, instruction.arg2 == 1))
                            {
                                SwapObjects(resultObject, TempRegistery);

                                if (ForceThrow)
                                {
                                    ForceThrow = false;
                                    return false;
                                }
                                return true;
                            }
                            continue;
                        }
                    case ExOperationCode.LOADNULL:
                        {
                            if (instruction.arg2 == 1)
                            {
                                for (int n = 0; n < instruction.arg1; n++)
                                {
                                    GetTargetInStack(instruction.arg0 + n).Nullify();
                                    GetTargetInStack(instruction.arg0 + n).Assign(new ExObject() { Type = ExObjType.DEFAULT });
                                }
                            }
                            else
                            {
                                for (int n = 0; n < instruction.arg1; n++)
                                {
                                    GetTargetInStack(instruction.arg0 + n).Nullify();
                                }
                            }
                            continue;
                        }
                    case ExOperationCode.LOADCONSTDICT:
                        {
                            if (instruction.arg1 != ExMat.InvalidArgument)
                            {
                                GetTargetInStack(instruction).Assign(SharedState.Consts[CallInfo.Value.Literals[(int)instruction.arg1].GetString()]);
                            }
                            else
                            {
                                GetTargetInStack(instruction).Assign(SharedState.Consts);
                            }
                            continue;
                        }
                    case ExOperationCode.RELOADLIB:
                        {
                            if (!ExApi.ReloadLibrary(this, CallInfo.Value.Literals[(int)instruction.arg1].GetString()))
                            {
                                return false;
                            }
                            GetTargetInStack(instruction).Assign(RootDictionary);
                            continue;
                        }
                    case ExOperationCode.LOADROOT:
                        {
                            GetTargetInStack(instruction).Assign(RootDictionary);
                            continue;
                        }
                    case ExOperationCode.JMP:   // arg1 adet komutu atla
                        {
                            CallInfo.Value.InstructionsIndex += (int)instruction.arg1;
                            continue;
                        }

                    case ExOperationCode.JZS:
                    case ExOperationCode.JZ:    // Hedef(arg0 indeksli) boolean olarak 'false' ise arg1 adet komutu atla
                        {
                            if (!GetTargetInStack(instruction.arg0).GetBool())
                            {
                                CallInfo.Value.InstructionsIndex += (int)instruction.arg1;
                            }
                            continue;
                        }
                    case ExOperationCode.JCMP:
                        {
                            if (!DoCompareOP((CmpOP)instruction.arg3, GetTargetInStack(instruction.arg2), GetTargetInStack(instruction.arg0), TempRegistery))
                            {
                                return FixStackAfterError();
                            }
                            if (!TempRegistery.GetBool())
                            {
                                CallInfo.Value.InstructionsIndex += (int)instruction.arg1;
                            }
                            continue;
                        }
                    case ExOperationCode.GETOUTER:
                        {
                            ExClosure currcls = CallInfo.Value.Closure.GetClosure();
                            ExOuter outr = currcls.OutersList[(int)instruction.arg1].ValueCustom._Outer;
                            GetTargetInStack(instruction).Assign(outr.ValueRef);
                            continue;
                        }
                    case ExOperationCode.SETOUTER:
                        {
                            ExClosure currcls = CallInfo.Value.Closure.GetClosure();
                            ExOuter outr = currcls.OutersList[(int)instruction.arg1].ValueCustom._Outer;
                            outr.ValueRef.Assign(GetTargetInStack(instruction.arg2));
                            if (instruction.arg0 != ExMat.InvalidArgument)
                            {
                                GetTargetInStack(instruction).Assign(GetTargetInStack(instruction.arg2));
                            }
                            continue;
                        }
                    case ExOperationCode.NEWOBJECT:
                        {
                            switch (instruction.arg3)
                            {
                                case (int)ExNewObjectType.DICT:
                                    {
                                        GetTargetInStack(instruction).Assign(new Dictionary<string, ExObject>());
                                        continue;
                                    }
                                case (int)ExNewObjectType.ARRAY:
                                    {
                                        GetTargetInStack(instruction).Assign(new List<ExObject>((int)instruction.arg1));
                                        continue;
                                    }
                                case (int)ExNewObjectType.CLASS:
                                    {
                                        if (!DoClassOP(GetTargetInStack(instruction), (int)instruction.arg1, (int)instruction.arg2))
                                        {
                                            AddToErrorMessage("failed to create class");
                                            return FixStackAfterError();
                                        }
                                        continue;
                                    }
                                default:
                                    {
                                        AddToErrorMessage("unknown object type " + instruction.arg3);
                                        return FixStackAfterError();
                                    }
                            }
                        }
                    case ExOperationCode.APPENDTOARRAY:
                        {
                            ExObject val = new();
                            switch (instruction.arg2)
                            {
                                case (int)ArrayAType.STACK:
                                    val.Assign(GetTargetInStack(instruction.arg1)); break;
                                case (int)ArrayAType.LITERAL:
                                    val.Assign(CallInfo.Value.Literals[(int)instruction.arg1]); break;
                                case (int)ArrayAType.INTEGER:
                                    val.Assign(instruction.arg1); break;
                                case (int)ArrayAType.FLOAT:
                                    val.Assign(new DoubleLong() { i = instruction.arg1 }.f); break;
                                case (int)ArrayAType.BOOL:
                                    val.Assign(instruction.arg1 == 1); break;
                                default:
                                    {
                                        Throw("unknown array append method", type: ExExceptionType.BASE);
                                        break;
                                    }
                            }
                            GetTargetInStack(instruction.arg0).GetList().Add(val);
                            continue;
                        }
                    case ExOperationCode.TRANSPOSE:
                        {
                            ExObject s1 = new(GetTargetInStack(instruction.arg1));
                            if (!DoMatrixTranspose(GetTargetInStack(instruction), ref s1, (ExFallback)instruction.arg1))
                            {
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case ExOperationCode.INC:
                    case ExOperationCode.PINC:
                        {
                            ExObject ob = new(instruction.arg3);

                            ExObject s1 = new(GetTargetInStack(instruction.arg1));
                            ExObject s2 = new(GetTargetInStack(instruction.arg2));
                            if (!DoDerefInc(ExOperationCode.ADD, GetTargetInStack(instruction), ref s1, ref s2, ob, instruction.op == ExOperationCode.PINC, (ExFallback)instruction.arg1))
                            {
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case ExOperationCode.INCL:
                    case ExOperationCode.PINCL:
                        {
                            ExObject ob = GetTargetInStack(instruction.arg1);
                            if (ob.Type == ExObjType.INTEGER)
                            {
                                GetTargetInStack(instruction).Assign(ob);
                                ob.Value.i_Int += instruction.arg3;
                            }
                            else
                            {
                                ob = new(instruction.arg3);
                                if (instruction.op == ExOperationCode.INCL)
                                {
                                    ExObject res = new();
                                    if (!DoArithmeticOP(ExOperationCode.ADD, ob, resultObject, ref res))
                                    {
                                        return FixStackAfterError();
                                    }
                                    ob.Assign(res);
                                }
                                else
                                {
                                    ExObject targ = new(GetTargetInStack(instruction));
                                    ExObject val = new(GetTargetInStack(instruction.arg1));
                                    if (!DoVarInc(ExOperationCode.ADD, ref targ, ref val, ob))
                                    {
                                        return FixStackAfterError();
                                    }
                                }
                            }
                            continue;
                        }
                    case ExOperationCode.EXISTS:
                        {
                            bool b = Getter(new(GetTargetInStack(instruction.arg1)), new(GetTargetInStack(instruction.arg2)), ref TempRegistery, true, ExFallback.DONT, true);

                            GetTargetInStack(instruction).Assign(instruction.arg3 == 0 ? b : !b);

                            continue;
                        }
                    case ExOperationCode.CMP:
                        {
                            if (!DoCompareOP((CmpOP)instruction.arg3, GetTargetInStack(instruction.arg2), GetTargetInStack(instruction.arg1), GetTargetInStack(instruction)))
                            {
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case ExOperationCode.CLOSE:
                        {
                            if (Outers != null)
                            {
                                CloseOuters((int)GetTargetInStack(instruction.arg1).GetInt());
                            }
                            continue;
                        }
                    case ExOperationCode.AND:
                        {
                            if (!GetTargetInStack(instruction.arg2).GetBool())
                            {
                                GetTargetInStack(instruction).Assign(GetTargetInStack(instruction.arg2));
                                CallInfo.Value.InstructionsIndex += (int)instruction.arg1;
                            }
                            continue;
                        }
                    case ExOperationCode.OR:
                        {
                            if (GetTargetInStack(instruction.arg2).GetBool())
                            {
                                GetTargetInStack(instruction).Assign(GetTargetInStack(instruction.arg2));
                                CallInfo.Value.InstructionsIndex += (int)instruction.arg1;
                            }
                            continue;
                        }
                    case ExOperationCode.NOT:
                        {
                            GetTargetInStack(instruction).Assign(!GetTargetInStack(instruction.arg1).GetBool());
                            continue;
                        }
                    case ExOperationCode.NEGATE:
                        {
                            if (!DoNegateOP(GetTargetInStack(instruction), GetTargetInStack(instruction.arg1)))
                            {
                                AddToErrorMessage("attempted to negate '" + GetTargetInStack(instruction.arg1).Type.ToString() + "'");
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case ExOperationCode.CLOSURE:
                        {
                            ExClosure cl = CallInfo.Value.Closure.GetClosure();
                            ExPrototype fp = cl.Function;
                            if (!DoClosureOP(GetTargetInStack(instruction), fp.Functions[(int)instruction.arg1]))
                            {
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case ExOperationCode.NEWSLOTA:
                        {
                            if (!NewSlotA(GetTargetInStack(instruction.arg1),
                                         GetTargetInStack(instruction.arg2),
                                         GetTargetInStack(instruction.arg3),
                                         (instruction.arg0 & (int)ExNewSlotFlag.ATTR) > 0 ? GetTargetInStack(instruction.arg2 - 1) : new(),
                                         (instruction.arg0 & (int)ExNewSlotFlag.STATIC) > 0,
                                         false))
                            {
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case ExOperationCode.COMPOUNDARITH:
                        {
                            // TO-DO somethings wrong here
                            int idx = (int)((instruction.arg1 & 0xFFFF0000) >> 16);

                            ExObject si = GetTargetInStack(idx);
                            ExObject s2 = GetTargetInStack(instruction.arg2);
                            ExObject s1v = GetTargetInStack(instruction.arg1 & 0x0000FFFF);

                            if (!DoDerefInc((ExOperationCode)instruction.arg3, GetTargetInStack(instruction), ref si, ref s2, s1v, false, (ExFallback)idx))
                            {
                                return FixStackAfterError();
                            }
                            continue;
                        }
                    case ExOperationCode.FOREACH:
                        {
                            int jidx = 0;
                            if (!DoForeach(GetTargetInStack(instruction.arg0),
                                            GetTargetInStack(instruction.arg2),
                                            GetTargetInStack(instruction.arg2 + 1),
                                            GetTargetInStack(instruction.arg2 + 2),
                                            (int)instruction.arg1,
                                            ref jidx))
                            {
                                return false;
                            }
                            CallInfo.Value.InstructionsIndex += jidx;
                            continue;
                        }
                    case ExOperationCode.POSTFOREACH:
                        {
                            if (GetTargetInStack(instruction.arg1).Type != ExObjType.GENERATOR)
                            {
                                return false;
                            }
                            // TO-DO generators
                            continue;
                        }
                    case ExOperationCode.TYPEOF:
                        {
                            ExObject obj = GetTargetInStack(instruction.arg1);
                            ExObject res = new();
                            if (obj.Type == ExObjType.INSTANCE && obj.GetInstance().GetMetaM(this, ExMetaMethod.TYPEOF, ref res))
                            {
                                ExObject r = new();
                                Push(obj);
                                if (!CallMeta(ref res, ExMetaMethod.TYPEOF, 1, ref r))
                                {
                                    AddToErrorMessage("'typeof' failed for the instance");
                                    return FixStackAfterError();
                                }
                                else
                                {
                                    GetTargetInStack(instruction).Assign(GetSimpleString(r));
                                }
                            }
                            else
                            {
                                GetTargetInStack(instruction).Assign(obj.Type.ToString());
                            }
                            continue;
                        }
                    case ExOperationCode.INSTANCEOF:
                        {
                            if (GetTargetInStack(instruction.arg1).Type != ExObjType.CLASS)
                            {
                                AddToErrorMessage("instanceof operation can only be done with a 'class' type");
                                return FixStackAfterError();
                            }
                            GetTargetInStack(instruction).Assign(
                                GetTargetInStack(instruction.arg2).Type == ExObjType.INSTANCE
                                && GetTargetInStack(instruction.arg2).GetInstance().IsInstanceOf(GetTargetInStack(instruction.arg1).GetClass()));
                            continue;
                        }
                    case ExOperationCode.RETURNMACRO:   // TO-DO
                        {
                            if (ReturnValue((int)instruction.arg0, (int)instruction.arg1, ref TempRegistery, false, true))
                            {
                                SwapObjects(resultObject, TempRegistery);
                                return true;
                            }
                            continue;
                        }
                    case ExOperationCode.GETBASE:
                        {
                            ExClosure c = CallInfo.Value.Closure.GetClosure();
                            if (c.Base != null)
                            {
                                GetTargetInStack(instruction).Assign(c.Base);
                            }
                            else
                            {
                                GetTargetInStack(instruction).Nullify();
                            }
                            continue;
                        }
                    default:
                        {
                            Throw("unknown operator " + instruction.op, type: ExExceptionType.BASE);
                            break;
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
                        if (self.Type == ExObjType.INSTANCE && self.GetInstance().GetMetaM(this, ExMetaMethod.DELSLOT, ref cls))
                        {
                            Push(self);
                            Push(k);
                            return CallMeta(ref cls, ExMetaMethod.DELSLOT, 2, ref r);
                        }
                        else
                        {
                            if (self.Type == ExObjType.DICT)
                            {
                                if (self.GetDict().ContainsKey(k.GetString()))
                                {
                                    tmp = new(self.GetDict()[k.GetString()]);

                                    self.GetDict().Remove(k.GetString());
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
                        if (!ExTypeCheck.IsNumeric(k))
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
                    target.Assign(Outers);
                    return;
                }
                Outers = Outers._next;
            }

            tmp = ExOuter.Create(SharedState, sidx);
            tmp._next = Outers;
            tmp.Index = (int)sidx.GetInt() - FindFirstNullInStack();
            tmp.ReferenceCount++;
            Outers = tmp;
            target.Assign(tmp);
        }

        public int FindFirstNullInStack()
        {
            for (int i = 0; i < StackSize; i++)
            {
                if (ExTypeCheck.IsNull(Stack[i]))
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
                                cl.OutersList[i].Assign(CallInfo.Value.Closure.GetClosure().OutersList[(int)ov.Index.GetInt()]);
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

            if (!ExApi.DoMatrixTransposeChecks(this, vals, ref cols))
            {
                return false;
            }

            t.Assign(ExApi.TransposeMatrix(rows, cols, vals));

            return true;
        }

        public bool DoDerefInc(ExOperationCode op, ExObject t, ref ExObject self, ref ExObject k, ExObject inc, bool post, ExFallback idx)
        {
            ExObject tmp = new();
            ExObject tmpk = k;
            ExObject tmps = self;
            if (!Getter(self, tmpk, ref tmp, false, idx))
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

        public bool DoVarInc(ExOperationCode op, ref ExObject t, ref ExObject o, ExObject diff)
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
            ExClass.ExClass cb = null;
            ExObject atrs = new();
            if (bcls != -1)
            {
                // TO-DO extends or sth
                return false;
            }

            if (attr != ExMat.InvalidArgument)
            {
                atrs.Assign(Stack[StackBase + attr]);
            }

            target.Assign(ExClass.ExClass.Create(SharedState, cb));

            if (ExTypeCheck.IsNotNull(target.GetClass().MetaFuncs[(int)ExMetaMethod.INHERIT]))
            {
                int np = 2;
                ExObject r = new();
                Push(target);
                Push(atrs);
                ExObject mm = target.GetClass().MetaFuncs[(int)ExMetaMethod.INHERIT];
                Call(ref mm, np, StackTop - np, ref r);
                Pop(np);
            }
            target.GetClass().Attributes.Assign(atrs);
            return true;
        }

        private static bool InnerNumericTypeCmp(ExObject a, ExObject b, ref int t)
        {
            t = a.Type == ExObjType.INTEGER && b.Type == ExObjType.FLOAT
                ? a.GetInt() == b.GetFloat() ? 0 : a.GetInt() < b.GetFloat() ? -1 : 1
                : a.GetFloat() == b.GetInt() ? 0 : a.GetFloat() < b.GetInt() ? -1 : 1;

            return true;
        }

        private static bool InnerSameTypeCmp(ExObject a, ExObject b, ref int t)
        {
            switch (a.Type)
            {
                case ExObjType.STRING:
                    {
                        t = a.GetString() == b.GetString() ? 0 : -1;
                        return true;
                    }
                case ExObjType.INTEGER:
                    {
                        t = a.GetInt() == b.GetInt() ? 0 : a.GetInt() < b.GetInt() ? -1 : 1;
                        return true;
                    }
                case ExObjType.FLOAT:
                    {
                        t = a.GetFloat() == b.GetFloat() ? 0 : a.GetFloat() < b.GetFloat() ? -1 : 1;
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        public static bool InnerDoCompareOP(ExObject a, ExObject b, ref int t)
        {
            if (a.Type == ExObjType.COMPLEX || b.Type == ExObjType.COMPLEX)
            {
                return false;
            }
            if (a.Type == b.Type)
            {
                return InnerSameTypeCmp(a, b, ref t);
            }
            else if (ExTypeCheck.IsNumeric(a) && ExTypeCheck.IsNumeric(b))
            {
                return InnerNumericTypeCmp(a, b, ref t);
            }
            return false;
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
            else
            {
                if (a.Type == ExObjType.COMPLEX || b.Type == ExObjType.COMPLEX)
                {
                    AddToErrorMessage("can't compare complex numbers");
                }
                else
                {
                    AddToErrorMessage("failed to compare " + a.Type.ToString() + " and " + b.Type.ToString());
                }
            }
            return false;
        }

        public bool ReturnValue(int a0, int a1, ref ExObject res, bool makeBoolean = false, bool interactive = false)                            //  bool mac = false)
        {
            bool root = CallInfo.Value.IsRootCall;
            int cbase = StackBase - CallInfo.Value.PrevBase;

            ExObject p = root ? res : CallInfo.Value.Target == -1 ? (new()) : Stack[cbase + CallInfo.Value.Target];

            // Argüman 0'a göre değeri sıfırla ya da konsol için değeri dön
            if (ExTypeCheck.IsNotNull(p) || _forcereturn)
            {
                if (a0 != ExMat.InvalidArgument || interactive)
                {
                    // Kaynak değeri hedefe ata
                    p.Assign(makeBoolean ? new(Stack[StackBase + a1].GetBool()) : Stack[StackBase + a1]);

                    // Dizi kontrolü ve optimizasyonu
                    bool isSequence = CallInfo.Value.Closure.GetClosure().Function.IsSequence();
                    #region Dizi Optimizasyonu
                    if (isSequence)
                    {
                        CallInfo.Value.Closure.GetClosure().DefaultParams.Add(new(p));
                        CallInfo.Value.Closure.GetClosure().Function.Parameters.Add(new(Stack[StackBase + 1].GetInt().ToString(CultureInfo.CurrentCulture)));
                    }
                    #endregion

                    if (!LeaveFrame(isSequence))
                    {
                        Throw("something went wrong with the stack!", type: ExExceptionType.BASE);
                    }
                    return root;
                }
                else
                {
                    p.Nullify();
                }
            }

            if (!LeaveFrame())
            {
                Throw("something went wrong with the stack!", type: ExExceptionType.BASE);
            }
            return root;
        }

        public static bool DoBitwiseOP(long iop, ExObject a, ExObject b, ExObject res)
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
                            return false;
                        }
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        [ExcludeFromCodeCoverage]
        private static double HandleZeroInDivision(double a)
        {
            return a > 0 ? double.PositiveInfinity : a == 0 ? double.NaN : double.NegativeInfinity;
        }

        public static bool InnerDoArithmeticOPInt(ExOperationCode op, long a, long b, ref ExObject res)
        {
            switch (op)
            {
                case ExOperationCode.ADD: res = new(a + b); break;
                case ExOperationCode.SUB: res = new(a - b); break;
                case ExOperationCode.MLT: res = new(a * b); break;
                case ExOperationCode.EXP: res = new(Math.Pow(a, b)); break;
                case ExOperationCode.MOD:
                case ExOperationCode.DIV:
                    {
                        res = b == 0 ? (new(HandleZeroInDivision(a))) : (new(op == ExOperationCode.DIV ? (a / b) : (a % b)));
                        break;
                    }
                default: return false;
            }
            return true;
        }

        public static bool InnerDoArithmeticOPFloat(ExOperationCode op, double a, double b, ref ExObject res)
        {
            switch (op)
            {
                case ExOperationCode.ADD: res = new(a + b); break;
                case ExOperationCode.SUB: res = new(a - b); break;
                case ExOperationCode.MLT: res = new(a * b); break;
                case ExOperationCode.EXP: res = new(Math.Pow(a, b)); break;
                case ExOperationCode.DIV:
                    {
                        res = new(b == 0 ? HandleZeroInDivision(a) : (a / b));
                        break;
                    }
                case ExOperationCode.MOD:
                    {
                        res = new(b == 0 ? HandleZeroInDivision(a) : (a % b));
                        break;
                    }
                default: return false;
            }
            return true;
        }

        public static bool InnerDoArithmeticOPComplex(ExOperationCode op, Complex a, Complex b, ref ExObject res)
        {
            switch (op)
            {
                case ExOperationCode.ADD: res = new(a + b); break;
                case ExOperationCode.SUB: res = new(a - b); break;
                case ExOperationCode.MLT: res = new(a * b); break;
                case ExOperationCode.MOD: return false;
                case ExOperationCode.DIV:
                    {
                        Complex c = Complex.Divide(a, b);
                        c = new Complex(Math.Round(c.Real, 15), Math.Round(c.Imaginary, 15));
                        res = new(c);
                        break;
                    }
                case ExOperationCode.EXP:
                    {
                        Complex c = Complex.Pow(a, b);
                        c = new Complex(Math.Round(c.Real, 15), Math.Round(c.Imaginary, 15));
                        res = new(c);
                        break;
                    }
                default: return false;
            }
            return true;
        }

        [ExcludeFromCodeCoverage]
        private static bool InnerDoArithmeticOPComplex(ExOperationCode op, Complex a, double b, ref ExObject res)
        {
            switch (op)
            {
                case ExOperationCode.ADD: res = new(a + b); break;
                case ExOperationCode.SUB: res = new(a - b); break;
                case ExOperationCode.MLT: res = new(a * b); break;
                case ExOperationCode.MOD: return false;
                case ExOperationCode.DIV:
                    {
                        Complex c = Complex.Divide(a, b);
                        c = new Complex(Math.Round(c.Real, 15), Math.Round(c.Imaginary, 15));
                        res = new(c);
                        break;
                    }
                case ExOperationCode.EXP:
                    {
                        Complex c = Complex.Pow(a, b);
                        c = new Complex(Math.Round(c.Real, 15), Math.Round(c.Imaginary, 15));
                        res = new(c);
                        break;
                    }
                default: return false;
            }
            return true;
        }

        [ExcludeFromCodeCoverage]
        private static bool InnerDoArithmeticOPComplex(ExOperationCode op, double a, Complex b, ref ExObject res)
        {
            switch (op)
            {
                case ExOperationCode.ADD: res = new(a + b); break;
                case ExOperationCode.SUB: res = new(a - b); break;
                case ExOperationCode.MLT: res = new(a * b); break;
                case ExOperationCode.MOD: return false;
                case ExOperationCode.DIV:
                    {
                        Complex c = Complex.Divide(a, b);
                        c = new Complex(Math.Round(c.Real, 15), Math.Round(c.Imaginary, 15));
                        res = new(c);
                        break;
                    }
                case ExOperationCode.EXP:
                    {
                        Complex c = Complex.Pow(a, b);
                        c = new Complex(Math.Round(c.Real, 15), Math.Round(c.Imaginary, 15));
                        res = new(c);
                        break;
                    }
                default: return false;
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
                        if (!ExTypeCheck.IsNumeric(num))
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
                        total += row[k].GetFloat() * B[k].GetList()[j].GetFloat();
                    }

                    r[i].GetList().Add(new(total));
                }

            }

            res = new(r);
            return true;
        }

        public bool DoMatrixMltOP(ExOperationCode op, ExObject a, ExObject b, ref ExObject res)
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
                ExObject ar = a.GetList()[i];
                for (int j = 0; j < bc; j++)
                {
                    r.Add(new(new List<ExObject>(2) { new(ar), new(b.GetList()[j]) }));
                }
            }
            res = new(r);

            return true;
        }

        private bool DoArithmeticOPStrings(ExOperationCode op, int a_mask, ExObject a, ExObject b, ref ExObject res)
        {
            if (op != ExOperationCode.ADD)
            {
                return DoArithmeticOPDefault(op, a, b, ref res);
            }
            switch (a_mask)
            {
                case (int)ArithmeticMask.STRING:
                    {
                        res = new(a.GetString() + b.GetString());
                        break;
                    }
                case (int)ArithmeticMask.STRINGNULL:
                    {
                        res = new(ExTypeCheck.IsNull(a)
                            ? ExMat.NullName + b.GetString()
                            : a.GetString() + ExMat.NullName);
                        break;
                    }
                case (int)ArithmeticMask.STRINGBOOL:
                    {
                        res = new(a.Type == ExObjType.BOOL
                            ? a.GetBool().ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture) + b.GetString()
                            : a.GetString() + b.GetBool().ToString(CultureInfo.CurrentCulture).ToLower(CultureInfo.CurrentCulture));
                        break;
                    }
                case (int)ArithmeticMask.STRINGCOMPLEX:
                    {
                        res = a.Type == ExObjType.STRING
                            ? new(a.GetString() + b.GetComplexString())
                            : new(a.GetComplexString() + b.GetString());
                        break;
                    }
                case (int)ArithmeticMask.STRINGINT:
                case (int)ArithmeticMask.STRINGFLOAT:
                    {
                        res = a.Type == ExObjType.STRING
                            ? new(a.GetString() + b.GetFloat())
                            : new(a.GetFloat() + b.GetString());
                        break;
                    }
                default: return false;
            }
            return true;
        }

        private bool DoArithmeticOPDefault(ExOperationCode op, ExObject a, ExObject b, ref ExObject res)
        {
            if (DoArithmeticMetaOP(op, a, b, ref res))
            {
                return true;
            }
            AddToErrorMessage("can't do " + op.ToString() + " operation between " + a.Type.ToString() + " and " + b.Type.ToString());
            return false;
        }

        public bool DoArithmeticOP(ExOperationCode op, ExObject a, ExObject b, ref ExObject res)
        {
            // Veri tiplerini maskeleyerek işlemler basitleştirilir
            switch ((int)a.Type | (int)b.Type)
            {
                case (int)ArithmeticMask.INT:       // Tamsayılar arası aritmetik işlem
                    {
                        return InnerDoArithmeticOPInt(op, a.GetInt(), b.GetInt(), ref res);
                    }
                case (int)ArithmeticMask.FLOATCOMPLEX:
                case (int)ArithmeticMask.INTCOMPLEX: // Tamsayı/ondalıklı ve kompleks sayı arası aritmetik işlem
                    {
                        return ExTypeCheck.IsRealNumber(a)
                            ? InnerDoArithmeticOPComplex(op, a.GetFloat(), b.GetComplex(), ref res)
                            : InnerDoArithmeticOPComplex(op, a.GetComplex(), b.GetFloat(), ref res);
                    }
                case (int)ArithmeticMask.FLOATINT:      // Ondalıklı sayı ve ondalıklı sayı/tamsayı arası
                case (int)ArithmeticMask.FLOAT:
                    {
                        // GetFloat metotu, tamsayı veri tipi objelerde tamsayıyı ondalıklı olarak döner
                        return InnerDoArithmeticOPFloat(op, a.GetFloat(), b.GetFloat(), ref res);
                    }
                case (int)ArithmeticMask.COMPLEX:
                    {
                        return InnerDoArithmeticOPComplex(op, a.GetComplex(), b.GetComplex(), ref res);
                    }
                case (int)ArithmeticMask.STRING:
                case (int)ArithmeticMask.STRINGNULL:
                case (int)ArithmeticMask.STRINGBOOL:
                case (int)ArithmeticMask.STRINGCOMPLEX:
                case (int)ArithmeticMask.STRINGINT:
                case (int)ArithmeticMask.STRINGFLOAT:
                    {
                        return DoArithmeticOPStrings(op, (int)a.Type | (int)b.Type, a, b, ref res);
                    }
                default:
                    {
                        return DoArithmeticOPDefault(op, a, b, ref res);
                    }
            }
        }
        public bool DoArithmeticMetaOP(ExOperationCode op, ExObject a, ExObject b, ref ExObject res)
        {
            ExMetaMethod meta;
            switch (op)
            {
                case ExOperationCode.ADD:
                    {
                        meta = ExMetaMethod.ADD;
                        break;
                    }
                case ExOperationCode.SUB:
                    {
                        meta = ExMetaMethod.SUB;
                        break;
                    }
                case ExOperationCode.DIV:
                    {
                        meta = ExMetaMethod.DIV;
                        break;
                    }
                case ExOperationCode.MLT:
                    {
                        meta = ExMetaMethod.MLT;
                        break;
                    }
                case ExOperationCode.MOD:
                    {
                        meta = ExMetaMethod.MOD;
                        break;
                    }
                case ExOperationCode.EXP:
                    {
                        meta = ExMetaMethod.EXP;
                        break;
                    }
                default:
                    {
                        meta = ExMetaMethod.ADD;
                        break;
                    }
            }
            if (a.Type == ExObjType.INSTANCE)
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

        public bool CallMeta(ref ExObject cls, ExMetaMethod meta, long nargs, ref ExObject res)
        {
            nMetaCalls++;
            bool b = Call(ref cls, nargs, StackTop - nargs, ref res, true);
            nMetaCalls--;
            Pop(nargs);
            return b;
        }

        public ExObject GetConditionFromInstr(ExInstr i)
        {
            return i.arg3 != 0 ? CallInfo.Value.Literals[(int)i.arg1] : GetTargetInStack(i.arg1);
        }

        private ExSetterStatus SetterDict(Dictionary<string, ExObject> dict, string key, ExObject val)
        {
            if (SharedState.DictDelegate.GetDict().ContainsKey(key))
            {
                return ExSetterStatus.NOTSETDELEGATE;
            }
            if (!dict.ContainsKey(key))
            {
                AddToErrorMessage("use '<>' operator to create new dictionary slots");
                return ExSetterStatus.NOTSETUNKNOWN;
            }
            dict[key].Assign(val);
            return ExSetterStatus.SET;
        }

        private ExSetterStatus SetterList(List<ExObject> lis, ExObject idx, ExObject val)
        {
            if (ExTypeCheck.IsNumeric(idx))
            {
                int n = (int)idx.GetInt();
                int l = lis.Count;
                if (Math.Abs(n) < l)
                {
                    if (n < 0)
                    {
                        n = l + n;
                    }
                    lis[n].Assign(val);
                    return ExSetterStatus.SET;
                }
                else
                {
                    AddToErrorMessage("array index error: count {0}, given index: {1}", lis.Count, idx.GetInt());
                    return ExSetterStatus.ERROR;
                }
            }

            if (idx.Type == ExObjType.STRING && SharedState.ListDelegate.GetDict().ContainsKey(idx.GetString()))
            {
                return ExSetterStatus.NOTSETDELEGATE;
            }

            AddToErrorMessage("can't index array with '{0}'", idx.Type.ToString());
            return ExSetterStatus.ERROR;
        }

        private ExSetterStatus SetterInstance(ExInstance inst, string key, ExObject val)
        {
            if (inst.Class.Members.ContainsKey(key)
                && inst.Class.Members[key].IsField())
            {
                inst.MemberValues[inst.Class.Members[key].GetMemberID()].Assign(val);
                return ExSetterStatus.SET;
            }

            return SharedState.InstanceDelegate.GetDict().ContainsKey(key) ? ExSetterStatus.NOTSETDELEGATE : ExSetterStatus.NOTSETUNKNOWN;
        }

        private ExSetterStatus SetterString(ExObject str, ExObject k, ExObject v)
        {
            if (ExTypeCheck.IsNumeric(k))
            {
                int n = (int)k.GetInt();
                int l = str.GetString().Length;
                if (Math.Abs(n) < l)
                {
                    if (n < 0)
                    {
                        n = l + n;
                    }

                    if (v.GetString().Length != 1)
                    {
                        AddToErrorMessage("expected single character for string setter");
                        return ExSetterStatus.ERROR;
                    }

                    str.SetString(str.GetString().Substring(0, n) + v.GetString() + str.GetString()[(n + 1)..l]);
                    return ExSetterStatus.SET;
                }
                AddToErrorMessage("array index error: count {0}, given index: {1}", str.GetString().Length, k.GetInt());
                return ExSetterStatus.ERROR;
            }

            if (k.Type == ExObjType.STRING && SharedState.StringDelegate.GetDict().ContainsKey(k.GetString()))
            {
                return ExSetterStatus.NOTSETDELEGATE;
            }

            AddToErrorMessage("can't index string with '{0}'", k.Type.ToString());
            return ExSetterStatus.ERROR;
        }

        private ExSetterStatus SetterClosure(ExClosure cls, string key)
        {
            if (SharedState.ClosureDelegate.GetDict().ContainsKey(key))
            {
                return ExSetterStatus.NOTSETDELEGATE;
            }
            else if (cls.GetAttribute(key) != null)
            {
                AddToErrorMessage("can't change closure attribute '{0}'", key);
                return ExSetterStatus.ERROR;
            }

            AddToErrorMessage("can't index CLOSURE with '{0}'", key);
            return ExSetterStatus.ERROR;
        }

        public bool Setter(ExObject self, ExObject k, ref ExObject v, ExFallback f)
        {
            ExSetterStatus status = ExSetterStatus.ERROR;
            switch (self.Type)
            {
                case ExObjType.DICT:
                    {
                        status = SetterDict(self.GetDict(), k.GetString(), v);
                        break;
                    }
                case ExObjType.ARRAY:
                    {
                        status = SetterList(self.GetList(), k, v);
                        break;
                    }
                case ExObjType.INSTANCE:
                    {
                        status = SetterInstance(self.GetInstance(), k.GetString(), v);
                        break;
                    }
                case ExObjType.STRING:
                    {
                        status = SetterString(self, k, v);
                        break;
                    }
                case ExObjType.CLOSURE:
                    {
                        status = SetterClosure(self.GetClosure(), k.GetString());
                        break;
                    }
                default:
                    {
                        break;
                    }
            }

            switch (status)
            {
                case ExSetterStatus.ERROR:
                    return false;
                case ExSetterStatus.SET:
                    return true;
                case ExSetterStatus.NOTSETDELEGATE: // TO-DO set_delegate
                    {
                        AddToErrorMessage("can't overwrite delegate '{0}' via indexing type '{1}'", k.GetString(), self.Type.ToString());
                        return false;
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

            AddToErrorMessage("unknown key '{0}'", k.GetString());
            return false;
        }

        public ExFallback SetterFallback(ExObject self, ExObject k, ref ExObject v)
        {
            switch (self.Type)
            {
                case ExObjType.INSTANCE:
                    {
                        ExObject cls = null;
                        ExObject t = new();
                        if (self.GetInstance().GetMetaM(this, ExMetaMethod.SET, ref cls))
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
            if (!ExTypeCheck.IsDelegable(self))
            {
                return false;
            }

            Dictionary<string, ExObject> del = new();
            switch (self.Type)
            {
                case ExObjType.CLASS:
                    {
                        del = SharedState.ClassDelegate.GetDict();
                        break;
                    }
                case ExObjType.INSTANCE:
                    {
                        del = SharedState.InstanceDelegate.GetDict();
                        break;
                    }
                case ExObjType.DICT:
                    {
                        del = SharedState.DictDelegate.GetDict();
                        break;
                    }
                case ExObjType.ARRAY:
                    {
                        del = SharedState.ListDelegate.GetDict();
                        break;
                    }
                case ExObjType.STRING:
                    {
                        del = SharedState.StringDelegate.GetDict();
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        del = SharedState.ComplexDelegate.GetDict();
                        break;
                    }
                case ExObjType.INTEGER:
                case ExObjType.FLOAT:
                    {
                        del = SharedState.NumberDelegate.GetDict();
                        break;
                    }
                case ExObjType.CLOSURE:
                case ExObjType.NATIVECLOSURE:
                    {
                        del = SharedState.ClosureDelegate.GetDict();
                        break;
                    }
                case ExObjType.WEAKREF:
                    {
                        del = SharedState.WeakRefDelegate.GetDict();
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
                case ExObjType.INSTANCE:
                    {
                        ExObject cls = null;
                        if (self.GetInstance().GetMetaM(this, ExMetaMethod.GET, ref cls))
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

        public ExGetterStatus Getter(Dictionary<string, ExObject> dict, ExObject key, ref ExObject dest, bool isUsingIn)
        {
            if (dict == null)
            {
                AddToErrorMessage("attempted to access null dictionary");
                return ExGetterStatus.ERROR;
            }

            if (dict.ContainsKey(key.GetString()))
            {
                dest.Assign(dict[key.GetString()]);
                return ExGetterStatus.FOUND;
            }

            if (isUsingIn || SharedState.DictDelegate.GetDict().ContainsKey(key.GetString()))
            {
                return ExGetterStatus.NOTFOUND;
            }

            AddToErrorMessage("unknown key '{0}'", key.GetString());
            return ExGetterStatus.ERROR;
        }


        public ExGetterStatus Getter(List<ExObject> lis, ExObject key, ref ExObject dest, bool isUsingIn)
        {
            if (lis == null)
            {
                AddToErrorMessage("attempted to access null array");
                return ExGetterStatus.ERROR;
            }

            if (ExTypeCheck.IsNumeric(key) && !isUsingIn)
            {
                int idx = (int)key.GetInt();
                idx += idx < 0 ? lis.Count : 0;

                if (idx >= 0 && lis.Count != 0 && lis.Count > idx)
                {
                    dest.Assign(lis[idx]);
                    return ExGetterStatus.FOUND;
                }
                else
                {
                    AddToErrorMessage("array index error: count " + lis.Count + ", idx: " + key.GetInt());
                    return ExGetterStatus.ERROR;
                }
            }
            else if (isUsingIn)
            {
                return ExApi.FindInArray(lis, key) ? ExGetterStatus.FOUND : ExGetterStatus.NOTFOUND;
            }

            return ExGetterStatus.NOTFOUND;
        }

        public ExGetterStatus Getter(ExInstance instance, ExObject key, ref ExObject dest, bool isUsingIn)
        {
            if (instance == null)
            {
                AddToErrorMessage("attempted to access null instance");
                return ExGetterStatus.ERROR;
            }

            if (instance.Class.Members.ContainsKey(key.GetString()))
            {
                dest.Assign(instance.Class.Members[key.GetString()]);
                if (dest.IsField())
                {
                    ExObject o = new(instance.MemberValues[dest.GetMemberID()]);
                    dest = o.Type == ExObjType.WEAKREF ? o.GetWeakRef().ReferencedObject : o;
                }
                else
                {
                    dest.Assign(instance.Class.Methods[dest.GetMemberID()].Value);
                }
                return ExGetterStatus.FOUND;
            }

            return ExGetterStatus.NOTFOUND;
        }

        public ExGetterStatus Getter(ExClass.ExClass cls, ExObject key, ref ExObject dest, bool isUsingIn)
        {
            if (cls == null)
            {
                AddToErrorMessage("attempted to access null class");
                return ExGetterStatus.ERROR;
            }
            if (cls.Members.ContainsKey(key.GetString()))
            {
                dest.Assign(cls.Members[key.GetString()]);
                if (dest.IsField())
                {
                    ExObject o = new(cls.DefaultValues[dest.GetMemberID()].Value);
                    dest = o.Type == ExObjType.WEAKREF ? o.GetWeakRef().ReferencedObject : o;
                }
                else
                {
                    dest.Assign(cls.Methods[dest.GetMemberID()].Value);
                }
                return ExGetterStatus.FOUND;
            }

            return ExGetterStatus.NOTFOUND;
        }

        private ExGetterStatus GetterCallCluster(ExObject fbase, ExObject key)
        {
            List<ExObject> lis = key.Type != ExObjType.ARRAY
                        ? new() { key }
                        : key.GetList();

            if (!DoClusterParamChecks(fbase.GetClosure(), lis))
            {
                return ExGetterStatus.ERROR;
            }

            ExObject tmp = new();
            Push(fbase);
            Push(RootDictionary);

            int nargs = 2;
            if (fbase.GetClosure().DefaultParams.Count == 1)
            {
                Push(lis);
            }
            else
            {
                nargs += lis.Count - 1;
                PushParse(lis);
            }

            if (!Call(ref fbase, nargs, StackTop - nargs, ref tmp, true))
            {
                Pop(nargs + 1);
                return ExGetterStatus.ERROR;
            }
            Pop(nargs + 1);
            return tmp.GetBool() ? ExGetterStatus.FOUND : ExGetterStatus.NOTFOUND;
        }

        public ExGetterStatus Getter(ExObject fbase, ExObject key, ref ExObject dest, bool isUsingIn, bool isNative = false)
        {
            ExClosure func = fbase.GetClosure();
            if (!isNative && isUsingIn)
            {
                return !func.Function.IsCluster() ? ExGetterStatus.ERROR : GetterCallCluster(fbase, key);
            }

            if (key.Type == ExObjType.STRING)
            {
                ExGetterStatus status = isNative ? ExApi.GetFunctionAttribute(fbase.GetNClosure(), key.GetString(), ref dest) : ExApi.GetFunctionAttribute(func, key.GetString(), ref dest);
                if (status == ExGetterStatus.ERROR)
                {
                    AddToErrorMessage("unknown function attribute '" + key.GetString() + "'");
                }
                return status;
            }

            if (!isUsingIn)
            {
                AddToErrorMessage("can't index '{0}' with '{1}'", isNative ? "NATIVECLOSURE" : "CLOSURE", key.Type.ToString());
            }
            return ExGetterStatus.ERROR;
        }

        public ExGetterStatus Getter(string str, ExObject key, ref ExObject dest, bool isUsingIn)
        {
            if (ExTypeCheck.IsNumeric(key))
            {
                int n = (int)key.GetInt();
                if (Math.Abs(n) < str.Length)
                {
                    if (n < 0)
                    {
                        n = str.Length + n;
                    }
                    dest = new ExObject(str[n].ToString(CultureInfo.CurrentCulture));
                    return ExGetterStatus.FOUND;
                }
                else if (!isUsingIn)
                {
                    AddToErrorMessage("string index error. count " + str.Length + " idx " + key.GetInt());
                    return ExGetterStatus.ERROR;
                }
                else
                {
                    return ExGetterStatus.NOTFOUND;
                }
            }
            else if (isUsingIn)
            {
                return str.IndexOf(key.GetString(), StringComparison.Ordinal) != -1 ? ExGetterStatus.FOUND : ExGetterStatus.NOTFOUND;
            }

            return ExGetterStatus.NOTFOUND;
        }

        public bool Getter(ExObject self, ExObject k, ref ExObject dest, bool raw, ExFallback f, bool isUsingIn = false)
        {
            ExGetterStatus status = ExGetterStatus.NOTFOUND;
            switch (self.Type)
            {
                case ExObjType.DICT:
                    {
                        status = Getter(self.GetDict(), k, ref dest, isUsingIn);
                        break;
                    }
                case ExObjType.ARRAY:
                    {
                        status = Getter(self.GetList(), k, ref dest, isUsingIn);
                        break;
                    }
                case ExObjType.INSTANCE:
                    {
                        status = Getter(self.GetInstance(), k, ref dest, isUsingIn);
                        break;
                    }
                case ExObjType.CLASS:
                    {
                        status = Getter(self.GetClass(), k, ref dest, isUsingIn);
                        break;
                    }
                case ExObjType.STRING:
                    {
                        status = Getter(self.GetString(), k, ref dest, isUsingIn);
                        break;
                    }
                case ExObjType.SPACE:
                    {
                        status = GetterSpace(self.GetSpace(), k, isUsingIn);
                        break;
                    }
                case ExObjType.NATIVECLOSURE:
                case ExObjType.CLOSURE:
                    {
                        status = Getter(fbase: self, k, ref dest, isUsingIn, self.Type == ExObjType.NATIVECLOSURE);
                        break;
                    }
                case ExObjType.WEAKREF:
                case ExObjType.COMPLEX:
                case ExObjType.FLOAT:
                case ExObjType.INTEGER:
                    {
                        break;
                    }
                default:
                    {
                        if (!isUsingIn)
                        {
                            AddToErrorMessage("can't index '" + self.Type.ToString() + "' with '" + k.Type.ToString() + "'");
                        }
                        return false;
                    }
            }

            if (status != ExGetterStatus.NOTFOUND)
            {
                return status == ExGetterStatus.FOUND;
            }

            if (!raw)
            {
                switch (GetterFallback(self, k, ref dest))
                {
                    case ExFallback.OK:
                        return true;
                    case ExFallback.ERROR:
                        return false;
                    default:
                        break;
                }
                if (InvokeDefaultDeleg(self, k, ref dest))
                {
                    return true;
                }
            }
            if (f == ExFallback.OK
                && RootDictionary.GetDict().ContainsKey(k.GetString()))
            {
                dest.Assign(RootDictionary.GetDict()[k.GetString()]);
                return true;
            }

            if (!isUsingIn && k.Type == ExObjType.STRING && self.Type != ExObjType.DICT)
            {
                AddToErrorMessage("index not found for type '" + self.Type.ToString() + "' named '" + k.GetString() + "'");
            }
            return false;
        }

        private ExGetterStatus GetterSpace(ExSpace self, ExObject k, bool isUsingIn)
        {
            if (isUsingIn)
            {
                return IsInSpace(k, self, 1, false) ? ExGetterStatus.FOUND : ExGetterStatus.ERROR;
            }
            else
            {
                AddToErrorMessage("can't index 'SPACE' with '" + k.Type.ToString() + "'");
                return ExGetterStatus.ERROR;
            }
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

        public static void SwapObjects(ExObject x, ExObject y)
        {
            ExObjType t = x.Type;
            ExObjVal v = x.Value;
            ExObjValCustom vc = x.ValueCustom;

            x.Type = y.Type;
            x.Value = y.Value;
            x.ValueCustom = y.ValueCustom;

            y.Type = t;
            y.Value = v;
            y.ValueCustom = vc;
        }

        public void CloseOuters(int idx)
        {
            ExOuter o;
            while ((o = Outers) != null && idx-- > 0)
            {
                Outers = o._next;   // Bir sonraki referans edileni ata
                o.Release();        // Referansı azalt
                if (o.Index == -1)  // Referanslar bitti, dön
                {
                    Outers = null;
                }
            }
        }

        public bool EnterFrame(int newBase, int newTop, bool isTailCall)
        {
            if (!isTailCall)    // zincirleme çağrı değil
            {
                // Çağrı zinciri yığını boyutunu, yığın dolduysa genişlet
                if (CallStackSize == AllocatedCallSize)
                {
                    AllocatedCallSize *= 2;
                    ExUtils.ExpandListTo(CallStack, AllocatedCallSize);
                }

                // Çağrı zinciri listesini sıralı liste halinde CallInfo içerisinde sakla
                CallInfo = new(CallStack, CallStackSize++);

                CallInfo.Value.PrevBase = newBase - StackBase;  // tabanı kaydet
                CallInfo.Value.PrevTop = StackTop - StackBase;  // tavanı kaydet
                CallInfo.Value.nCalls = 1;                      // çağrı sayısını takip et
                CallInfo.Value.IsRootCall = false;              // ana çağrı değil
            }
            else
            {
                CallInfo.Value.nCalls++;        // zincirleme çağrı, çağrı sayısını arttır
            }

            StackBase = newBase;        // yeni tabanı ata
            StackTop = newTop;          // yeni tavanı ata

            if (newTop >= StackSize)   // bellek yetersiz
            {
                Throw("stack overflow!", type: ExExceptionType.BASE);
            }
            return true;
        }

        public bool LeaveFrame(bool sequenceOptimize = false)
        {
            int last_top = StackTop;        // Tavan
            int last_base = StackBase;      // Taban
            int css = --CallStackSize;      // Çağrı yığını sayısı

            #region Dizi optimizasyonu
            if (sequenceOptimize
                && IsMainCall
                && (css <= 0 || (css > 0 && CallStack[css - 1].IsRootCall)))
            {
                List<ExObject> dp = new();
                List<ExObject> ps = new();

                for (int i = 0; i < CallInfo.Value.Closure.GetClosure().Function.nParams; i++)
                {
                    ps.Add(new(CallInfo.Value.Closure.GetClosure().Function.Parameters[i]));
                }
                for (int i = 0; i < CallInfo.Value.Closure.GetClosure().Function.nDefaultParameters; i++)
                {
                    dp.Add(new(CallInfo.Value.Closure.GetClosure().DefaultParams[i]));
                }
                CallInfo.Value.Closure.GetClosure().DefaultParams = new(dp);
                CallInfo.Value.Closure.GetClosure().Function.Parameters = new(ps);
            }
            #endregion

            if (CallInfo.Value == null)
            {
                return false;
            }

            CallInfo.Value.Closure.Nullify();               // Fonksiyonu sıfırla
            StackBase -= CallInfo.Value.PrevBase;           // Tabanı ayarla
            StackTop = StackBase + CallInfo.Value.PrevTop;  // Tavanı ayarla

            CallInfo.Value = css > 0 && css < CallStack.Count ? CallStack[css - 1] : null;

            if (Outers != null)         // Dış değişken referanslarını azalt
            {
                CloseOuters(last_base);
            }

            if (last_top >= StackSize)
            {
                AddToErrorMessage("stack overflow! Allocate more stack room for these operations");
                return false;
            }

            return true;
        }

        public bool CallNative(ExNativeClosure cls, long narg, long newb, ref ExObject o)
        {
            return CallNative(cls, (int)narg, (int)newb, ref o);
        }

        private static bool NativeCallParamCheck(int nParameterChecks, int nArguments)
        {
            return (nParameterChecks <= 0 || nParameterChecks == nArguments) &&
            (nParameterChecks >= 0 || nArguments >= (-nParameterChecks));
        }

        private bool DoArgumentChecksInStackNative(ExNativeClosure cls, int nParameterChecks, int nArguments, int newBase, List<int> ts)
        {
            int t_n = ts.Count;
            if (nParameterChecks < 0 && t_n < nArguments)   // Maksimum argüman sayısı kontrolü yap
            {
                AddToErrorMessage("'" + cls.Name.GetString() + "' takes maximum " + (t_n - 1) + " arguments");
                return false;
            }

            for (int i = 0; i < nArguments && i < t_n; i++) // Argümanların tiplerini maskeler ile kontrol et
            {
                ExObjType argumentType = Stack[newBase + i].Type;
                if (argumentType == ExObjType.DEFAULT)
                {
                    if (cls.DefaultValues.ContainsKey(i))
                    {
                        Stack[newBase + i].Assign(cls.DefaultValues[i]);
                    }
                    else
                    {
                        AddToErrorMessage("can't use non-existant default value for parameter " + i);
                        return false;
                    }
                } // ".." sembollerini varsayılan değer kontrolü yaparak değiştir

                // argumentType tipi tamsayı olarak ts[i] ile maskelendiğinde 0 oluyorsa beklenmedik bir tiptir 
                else if (ts[i] != -1 && ((int)argumentType & ts[i]) == 0)
                {
                    AddToErrorMessage("invalid parameter type for parameter " + i + ", expected one of "
                                      + ExApi.GetExpectedTypes(ts[i]) + ", got: " + Stack[newBase + i].Type.ToString());
                    return false;
                }
            }
            return true;
        }

        public bool CallNative(ExNativeClosure cls, int nArguments, int newBase, ref ExObject result)
        {
            int nParameterChecks = cls.nParameterChecks;        // Parametre sayısı kontrolü
            int newTop = newBase + nArguments + cls.nOuters;    // Yeni tavan indeksi

            if (nNativeCalls + 1 > 100)
            {
                Throw("Native stack overflow", type: ExExceptionType.BASE);
            }

            // nParameterChecks > 0 => tam nParameterChecks adet argüman gerekli
            // nParameterChecks < 0 => minimum (-nParameterChecks) adet argüman gerekli
            if (!NativeCallParamCheck(nParameterChecks, nArguments))
            {
                if (nParameterChecks < 0)
                {
                    AddToErrorMessage("'" + cls.Name.GetString() + "' takes minimum " + (-nParameterChecks - 1) + " arguments");
                    return false;
                }
                AddToErrorMessage("'" + cls.Name.GetString() + "' takes exactly " + (nParameterChecks - 1) + " arguments");
                return false;
            }

            // CompileTypeMask ile derlenen maskeler listesi
            List<int> ts = cls.TypeMasks;

            // Tanımlı maske varsa argümanları maskeler ile kontrol et
            if (ts.Count > 0
                && !DoArgumentChecksInStackNative(cls, nParameterChecks, nArguments, newBase, ts))
            {
                return false;
            }

            // Çerçeve başlat
            if (!EnterFrame(newBase, newTop, false))
            {
                return false;
            }
            CallInfo.Value.Closure = new(cls);  // Fonksiyonu çağrı zincirinde yerine koy

            // Dış değişkenleri belleğe yükle
            int outers = cls.nOuters;
            for (int i = 0; i < outers; i++)
            {
                Stack[newBase + nArguments + i].Assign(cls.OutersList[i]);
            }

            // Zayıf referansa sahipse fonksiyonun bellekteki yerine referans edilen fonksiyonu koy
            if (cls.WeakReference != null)
            {
                Stack[newBase].Assign(cls.WeakReference.ReferencedObject);
            }

            // Fonksiyonu çağır
            nNativeCalls++;
            ExFunctionStatus returnValue = cls.Function(this, nArguments - 1);
            nNativeCalls--;

            // Negatif değer = hata bulundu
            switch (returnValue)
            {
                case ExFunctionStatus.ERROR:
                    {
                        if (!LeaveFrame())  // Çerçeveyi kapat ve hata dönme sürecini başlat
                        {
                            Throw("something went wrong with the stack!", type: ExExceptionType.BASE);
                        }
                        return false;
                    }
                case ExFunctionStatus.VOID:
                    {
                        result.Nullify();
                        break;
                    }
                case ExFunctionStatus.EXIT:
                    {
                        ExitCode = (int)Stack[StackTop - 1].GetInt();
                        ExitCalled = true;
                        return false;
                    }
                case ExFunctionStatus.SUCCESS:
                    {
                        result.Assign(Stack[StackTop - 1]);
                        break;
                    }
            }

            // Çerçeveden çık
            if (!LeaveFrame())
            {
                Throw("something went wrong with the stack!", type: ExExceptionType.BASE);
            }
            return true;
        }

        public bool Call(ref ExObject cls, long nparams, long stackbase, ref ExObject o, bool forcereturn = false)
        {
            return Call(ref cls, (int)nparams, (int)stackbase, ref o, forcereturn);
        }

        public bool Call(ref ExObject cls, int nArguments, int stackBase, ref ExObject result, bool forcereturn = false)
        {
            // İnteraktif konsola çıktı yardımcısı 
            bool forceStatus = _forcereturn;
            _forcereturn = forcereturn;

            switch (cls.Type)
            {
                case ExObjType.CLOSURE:         // Kullanıcı fonksiyonu ya da main çağır
                    {
                        bool state = Execute(cls, nArguments, stackBase, ref result);
                        if (state)
                        {
                            nNativeCalls--;
                        }
                        _forcereturn = forceStatus;
                        return state;
                    }
                case ExObjType.NATIVECLOSURE:   // Yerli fonksiyon çağır
                    {
                        bool state = CallNative(cls.GetNClosure(), nArguments, stackBase, ref result);
                        _forcereturn = forceStatus;
                        return state;
                    }
                case ExObjType.CLASS:           // Sınıfa ait obje oluştur
                    {
                        ExObject tmp = new();

                        CreateClassInst(cls.GetClass(), ref result, out ExObject cn);
                        if (ExTypeCheck.IsNotNull(cn))
                        {
                            Stack[stackBase].Assign(result);
                            bool s = Call(ref cn, nArguments, stackBase, ref tmp);
                            _forcereturn = forceStatus;
                            return s;
                        }
                        _forcereturn = forceStatus;
                        return true;
                    }
                default:
                    return _forcereturn = forceStatus;
            }

        }

        private object GetDebuggerDisplay()
        {
            return "VM" + (IsMainCall ? "<main>" : "<inner>") + "(Base: " + StackBase + ", Top: " + StackTop + ")";
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
