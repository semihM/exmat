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
    public class ExFState : IDisposable
    {
        public ExSState SharedState;

        // Kod dizisi kopyası
        public ExObject Source;

        // İçinde bulunulan fonksiyon ismi
        public ExObject Name;

        // Kullanılan değişken isimleri ve yazı dizileri
        public Dictionary<string, ExObject> Literals = new();
        public int nLiterals;

        // Değişken bilgisi listeleri
        public List<ExLocalInfo> LocalVariables = new();
        public List<ExLocalInfo> LocalVariableInfos = new();

        // Bilinmeyen değişken bilgisi listesi
        public int nOuters;
        public List<ExOuterInfo> OuterInfos = new();

        // Parametre bilgileri
        public List<ExObject> Parameters = new();
        public List<int> DefaultParameters = new();

        // Fonksiyon(lar)
        public List<ExPrototype> Functions = new();

        // Sanal bellekteki pozisyonların ayarlanacağı ufak bellek
        public int StackSize;
        public ExStack Stack = new();

        // Oluşturulan komutlar
        public List<ExInstr> Instructions = new();

        // Fonksiyonun ait olduğu üst fonksiyon
        public ExFState ParentFState;
        // Fonksiyonun sahip olduğu alt fonksiyonlar
        public List<ExFState> ChildrenFStates = new();

        // İterasyon manipulasyonu pozisyon ve hedefleri
        public List<int> BreakList = new();
        public List<int> ContinueList = new();
        public List<int> BreakTargetsList = new();
        public List<int> ContinueTargetList = new();

        // Bir önceki komuta bağımlı ?
        public bool NotSnoozed;
        // Belirsiz sayıda parametreli ?
        public bool HasVargs;

        // Değerin dönüleceği bellek pozisyonu
        public int ReturnExpressionTarget;

        private const int MAX_STACK_SIZE = 255;
        private const int MAX_LITERALS = int.MaxValue;

        public List<ExLineInfo> LineInfos = new();
        public int LastLine;

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    BreakList = null;
                    ContinueList = null;
                    BreakTargetsList = null;
                    ContinueTargetList = null;
                    SharedState = null;
                    LineInfos = null;

                    ParentFState.Dispose();
                    Stack.Dispose();
                    Source.Dispose();
                    Name.Dispose();

                    Disposer.DisposeList(ref ChildrenFStates);
                    Disposer.DisposeList(ref Instructions);
                    Disposer.DisposeList(ref LocalVariables);
                    Disposer.DisposeList(ref LocalVariableInfos);
                    Disposer.DisposeList(ref OuterInfos);
                    Disposer.DisposeList(ref Parameters);
                    Disposer.DisposeList(ref Functions);

                    Disposer.DisposeDict(ref Literals);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }
        public ExFState() { }

        public ExFState(ExSState sState, ExFState parent)
        {
            nLiterals = 0;

            SharedState = sState;

            NotSnoozed = true;
            ParentFState = parent;

            StackSize = 0;
            ReturnExpressionTarget = 0;
            nOuters = 0;

            HasVargs = false;
        }

        public void SetInstrParams(int pos, int p1, int p2, int p3, dynamic p4)
        {
            Instructions[pos].arg0.Value.i_Int = p1;
            Instructions[pos].arg1 = p2;
            Instructions[pos].arg2.Value.i_Int = p3;
            Instructions[pos].arg3.Value.i_Int = p4;
        }
        public void SetInstrParam(int pos, int pno, int val)
        {
            switch (pno)
            {
                case 0:
                    {
                        Instructions[pos].arg0.Value.i_Int = val;
                        break;
                    }
                case 1:
                case 4:
                    {
                        Instructions[pos].arg1 = val;
                        break;
                    }
                case 2:
                    {
                        Instructions[pos].arg2.Value.i_Int = val;
                        break;
                    }
                case 3:
                    {
                        Instructions[pos].arg3.Value.i_Int = val;
                        break;
                    }
            }
        }

        public bool IsBlockMacro(string name)
        {
            return SharedState.BlockMacros.ContainsKey(name);
        }

        public bool AddBlockMacro(string name, ExMacro mac)
        {
            SharedState.BlockMacros.Add(name, mac);
            return true;
        }

        public bool IsMacro(ExObject o)
        {
            return SharedState.Macros.ContainsKey(o.GetString());
        }

        public bool IsFuncMacro(ExObject o)
        {
            return SharedState.Macros[o.GetString()].GetBool();
        }

        public bool AddMacro(ExObject o, bool isfunc, bool forced = false)
        {
            if (SharedState.Macros.ContainsKey(o.GetString()))
            {
                if (forced)
                {
                    SharedState.Macros[o.GetString()].Assign(isfunc);
                    return true;
                }
                return false;
            }
            else
            {
                SharedState.Macros.Add(o.GetString(), new(isfunc));
                return true;
            }
        }

        public long GetConst(ExObject o)
        {
            string name;
            if (o.Type == ExObjType.SPACE)
            {
                name = o.Value.c_Space.GetString();
            }
            else
            {
                name = o.GetString();
            }

            ExObject v = new();
            if (!Literals.ContainsKey(name))
            {
                v.Value.i_Int = nLiterals;
                Literals.Add(name, v);
                nLiterals++;
                if (nLiterals > MAX_LITERALS)
                {
                    v.Nullify();
                    throw new Exception("too many literals");
                }
            }
            else
            {
                ExObject val = Literals[name];
                if (val.Type == ExObjType.WEAKREF)
                {
                    v = val.Value._WeakRef.ReferencedObject;
                }
                else
                {
                    v = val;
                }
            }

            return v.Value.i_Int;
        }

        public int GetCurrPos()
        {
            return Instructions.Count - 1;
        }

        public int GetLocalStackSize()
        {
            return LocalVariables.Count;
        }

        public void SetLocalStackSize(int s)
        {
            int c_s = LocalVariables.Count;

            while (c_s > s)
            {
                c_s--;
                ExLocalInfo li = LocalVariables.Last();
                if (li.Name.Type != ExObjType.NULL)
                {
                    if (li.EndOPC == int.MaxValue)
                    {
                        nOuters--;
                    }
                    li.EndOPC = GetCurrPos();
                    LocalVariableInfos.Add(li);
                }
                LocalVariables.RemoveAt(LocalVariables.Count - 1);
            }
        }

        public int GetOuterSize(int size)
        {
            int c = 0;
            int ls = LocalVariables.Count - 1;
            while (ls >= size)
            {
                if (LocalVariables[ls--].EndOPC == int.MaxValue)
                {
                    c++;
                }
            }
            return c;
        }

        public void AddLineInfo(int line, bool op, bool forced)
        {
            if (LastLine != line || forced)
            {
                ExLineInfo li = new();
                li.Line = line;
                li.Position = GetCurrPos() + 1;
                if (op)
                {
                    AddInstr(OPC.LINE, 0, line, 0, 0);
                }
                if (LastLine != line)
                {
                    LineInfos.Add(li);
                }
                LastLine = line;
            }
        }
        public void DiscardTopTarget()
        {
            int dissed = PopTarget();
            int s = Instructions.Count;
            if (s > 0 && NotSnoozed)
            {
                ExInstr instr = Instructions[s - 1];
                switch (instr.op)
                {
                    case OPC.SET:
                    case OPC.NEWSLOT:
                    case OPC.SETOUTER:
                    case OPC.CALL:
                        {
                            if (instr.arg0.Value.i_Int == dissed)
                            {
                                instr.arg0.Value.i_Int = 985;
                            }
                            break;
                        }
                }
            }
        }

        public int TopTarget()
        {
            return (int)Stack.Back().GetInt();
        }

        public int PopTarget()
        {
            int n = (int)Stack.Back().GetInt();

            ExLocalInfo l = LocalVariables[n];
            if (l.Name.Type == ExObjType.NULL)
            {
                LocalVariables.RemoveAt(LocalVariables.Count - 1);
            }
            Stack.Pop();

            return n;
        }

        public int PushTarget(int n = -1)
        {
            if (n != -1)
            {
                Stack.Push(new(n));
                return n;
            }

            n = FindAStackPos();
            Stack.Push(new(n));

            return n;
        }

        public int FindAStackPos()
        {
            int size = LocalVariables.Count;
            LocalVariables.Add(new ExLocalInfo());
            if (LocalVariables.Count > StackSize)
            {
                if (StackSize > MAX_STACK_SIZE)
                {
                    throw new Exception("Too many locals!");
                }
                StackSize = LocalVariables.Count;
            }
            return size;
        }

        public bool IsLocalArg(long pos)
        {
            return pos < LocalVariables.Count && LocalVariables[(int)pos].Name.Type != ExObjType.NULL;
        }
        public bool IsLocalArg(int pos)
        {
            return pos < LocalVariables.Count && LocalVariables[pos].Name.Type != ExObjType.NULL;
        }

        public int PushVar(ExObject v)
        {
            int n = LocalVariables.Count;
            ExLocalInfo l = new();
            l.Name.Assign(v.GetString());
            l.StartOPC = GetCurrPos() + 1;
            l.Position = LocalVariables.Count;
            LocalVariables.Add(l);
            if (LocalVariables.Count > StackSize)
            {
                StackSize = LocalVariables.Count;
            }
            return n;
        }

        public int GetLocal(ExObject local)
        {
            int c = LocalVariables.Count;
            while (c > 0)
            {
                if (LocalVariables[--c].Name.Value.s_String == local.Value.s_String)
                {
                    return c;
                }
            }
            return -1;
        }
        public int GetOuter(ExObject obj)
        {
            int c = OuterInfos.Count;
            for (int i = 0; i < c; i++)
            {
                if (OuterInfos[i].Name.Value.s_String == obj.Value.s_String)
                {
                    return i;
                }
            }

            int p;
            if (ParentFState != null)
            {
                p = ParentFState.GetLocal(obj);
                if (p == -1)
                {
                    p = ParentFState.GetOuter(obj);
                    if (p != -1)
                    {
                        OuterInfos.Add(new ExOuterInfo(obj, new(p), ExOuterType.OUTER));
                        return OuterInfos.Count - 1;
                    }
                }
                else
                {
                    ParentFState.SetLocalToOuter(p);
                    OuterInfos.Add(new ExOuterInfo(obj, new(p), ExOuterType.LOCAL));
                    return OuterInfos.Count - 1;
                }
            }
            return -1;
        }

        public void SetLocalToOuter(int p)
        {
            LocalVariables[p].EndOPC = int.MaxValue;
            nOuters++;
        }

        public ExObject CreateString(string s, int len = -1)
        {
            if (!SharedState.Strings.ContainsKey(s))
            {
                ExObject str = new() { Type = ExObjType.STRING };
                str.Value.s_String = s;
                SharedState.Strings.Add(s, str);
                return str;
            }
            return SharedState.Strings[s];
        }
        public void AddInstr(ExInstr curr)
        {
            int size = Instructions.Count;

            if (size > 0 && NotSnoozed)
            {
                ExInstr prev = Instructions[size - 1];

                switch (curr.op)
                {
                    case OPC.JZ:
                        {
                            if (prev.op == OPC.CMP && prev.arg1 < 985)
                            {
                                prev.op = OPC.JCMP;
                                prev.arg0 = new(prev.arg1);
                                prev.arg1 = curr.arg1;
                                Instructions[size - 1] = prev;
                                return;
                            }
                            goto case OPC.SET;
                        }
                    case OPC.SET:
                        {
                            if (curr.arg0.Value.i_Int == curr.arg3.Value.i_Int)
                            {
                                curr.arg0.Value.i_Int = 985;
                            }
                            break;
                        }
                    case OPC.SETOUTER:
                        {
                            if (curr.arg0.Value.i_Int == curr.arg2.Value.i_Int)
                            {
                                curr.arg0.Value.i_Int = 985;
                            }
                            break;
                        }
                    case OPC.RETURN:
                        {
                            if (ParentFState != null && curr.arg0.Value.i_Int != 985 && prev.op == OPC.CALL && ReturnExpressionTarget < size - 1)
                            {
                                prev.op = OPC.CALLTAIL;
                                Instructions[size - 1] = prev;
                            }
                            else if (prev.op == OPC.CLOSE)
                            {
                                Instructions[size - 1] = curr;
                                return;
                            }
                            break;
                        }
                    case OPC.GET:
                        {
                            if (prev.op == OPC.LOAD && prev.arg0.Value.i_Int == curr.arg2.Value.i_Int && (!IsLocalArg((int)prev.arg0.Value.i_Int)))
                            {
                                prev.arg2 = new(curr.arg1);
                                prev.op = OPC.GETK;
                                prev.arg0.Value.i_Int = curr.arg0.Value.i_Int;
                                Instructions[size - 1] = prev;
                                return;
                            }
                            break;
                        }
                    case OPC.PREPCALL:
                        {
                            if (prev.op == OPC.LOAD && prev.arg0.Value.i_Int == curr.arg1 && (!IsLocalArg((int)prev.arg0.Value.i_Int)))
                            {
                                prev.op = OPC.PREPCALLK;
                                prev.arg0.Value.i_Int = curr.arg0.Value.i_Int;
                                prev.arg2.Value.i_Int = curr.arg2.Value.i_Int;
                                prev.arg3.Value.i_Int = curr.arg3.Value.i_Int;
                                Instructions[size - 1] = prev;
                                return;
                            }
                            break;
                        }
                    case OPC.APPENDTOARRAY:
                        {
                            ArrayAType idx = ArrayAType.INVALID;
                            switch (prev.op)
                            {
                                case OPC.LOAD:
                                    {
                                        idx = ArrayAType.LITERAL;
                                        break;
                                    }
                                case OPC.LOADINTEGER:
                                    {
                                        idx = ArrayAType.INTEGER;
                                        break;
                                    }
                                case OPC.LOADFLOAT:
                                    {
                                        idx = ArrayAType.FLOAT;
                                        break;
                                    }
                                case OPC.LOADBOOLEAN:
                                    {
                                        idx = ArrayAType.BOOL;
                                        break;
                                    }
                                default:
                                    break;
                            }

                            if (idx != ArrayAType.INVALID && prev.arg0.Value.i_Int == curr.arg1 && (!IsLocalArg((int)prev.arg0.Value.i_Int)))
                            {
                                prev.op = OPC.APPENDTOARRAY;
                                prev.arg0.Value.i_Int = curr.arg0.Value.i_Int;
                                prev.arg2.Value.i_Int = (int)idx;
                                prev.arg3.Value.i_Int = 985;
                                Instructions[size - 1] = prev;
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
                                case OPC.MMLT:
                                case OPC.TRANSPOSE:
                                case OPC.CARTESIAN:
                                case OPC.BITWISE:
                                case OPC.LOAD:
                                case OPC.LOADINTEGER:
                                case OPC.LOADFLOAT:
                                case OPC.LOADBOOLEAN:
                                case OPC.LOADCOMPLEX:
                                case OPC.LOADSPACE:
                                    {
                                        if (prev.arg0.Value.i_Int == curr.arg1)
                                        {
                                            prev.arg0.Value.i_Int = curr.arg0.Value.i_Int;
                                            NotSnoozed = false;
                                            Instructions[size - 1] = prev;
                                            return;
                                        }
                                        break;
                                    }
                            }

                            if (prev.op == OPC.MOVE)
                            {
                                prev.op = OPC.DMOVE;
                                prev.arg2.Value.i_Int = curr.arg0.Value.i_Int;
                                prev.arg3.Value.i_Int = curr.arg1;
                                Instructions[size - 1] = prev;
                                return;
                            }

                            break;
                        }
                    case OPC.LOAD:
                        {
                            if (prev.op == OPC.LOAD && curr.arg1 <= 985)
                            {
                                prev.op = OPC.DLOAD;
                                prev.arg2.Value.i_Int = curr.arg0.Value.i_Int;
                                prev.arg3.Value.i_Int = curr.arg1;
                                Instructions[size - 1] = prev;
                                return;
                            }
                            break;
                        }
                    case OPC.EQ:
                    case OPC.NEQ:
                        {
                            if (prev.op == OPC.LOAD && prev.arg0.Value.i_Int == curr.arg1 && (!IsLocalArg((int)prev.arg0.Value.i_Int)))
                            {
                                prev.op = curr.op;
                                prev.arg0.Value.i_Int = curr.arg0.Value.i_Int;
                                prev.arg2.Value.i_Int = curr.arg2.Value.i_Int;
                                prev.arg3.Value.i_Int = 985;
                                Instructions[size - 1] = prev;
                                return;
                            }
                            break;
                        }
                    case OPC.LOADNULL:
                        {
                            if (prev.op == OPC.LOADNULL && (prev.arg0.Value.i_Int + prev.arg1 == curr.arg0.Value.i_Int))
                            {
                                prev.arg1++;
                                prev.op = OPC.LOADNULL;
                                Instructions[size - 1] = prev;
                                return;
                            }
                            break;
                        }
                    case OPC.LINE:
                        {
                            if (prev.op == OPC.LINE)
                            {
                                Instructions.RemoveAt(size - 1);
                                LineInfos.RemoveAt(LineInfos.Count - 1);
                            }
                            break;
                        }
                }
            }

            NotSnoozed = true;
            Instructions.Add(curr);
        }

        public void AddInstr(OPC op, int arg0, long arg1, int arg2, int arg3)
        {
            ExInstr instr = new() { op = op, arg0 = new(arg0), arg1 = arg1, arg2 = new(arg2), arg3 = new(arg3) };
            AddInstr(instr);
        }

        public ExFState PushChildState(ExSState es)
        {
            ExFState ch = new() { SharedState = es, ParentFState = this };
            ChildrenFStates.Add(ch);
            return ch;
        }

        public void PopChildState()
        {
            ExFState ch = ChildrenFStates.Last();
            while (ch.ChildrenFStates.Count > 0)
            {
                ch.PopChildState();
            }
            ChildrenFStates.RemoveAt(ChildrenFStates.Count - 1);
        }

        public void AddParam(ExObject p)
        {
            PushVar(p);
            Parameters.Add(p);
        }

        public void AddDefParam(int p)
        {
            DefaultParameters.Add(p);
        }

        public int GetDefParamCount()
        {
            return DefaultParameters.Count;
        }

        public ExPrototype CreatePrototype()
        {
            ExPrototype funcPro = ExPrototype.Create(SharedState,
                                                 Instructions.Count,
                                                 nLiterals,
                                                 Parameters.Count,
                                                 Functions.Count,
                                                 OuterInfos.Count,
                                                 LineInfos.Count,
                                                 LocalVariableInfos.Count,
                                                 DefaultParameters.Count);

            funcPro.StackSize = StackSize;
            funcPro.Source = Source;
            funcPro.Name = Name;

            foreach (KeyValuePair<string, ExObject> pair in Literals)
            {
                if (pair.Value.Type == ExObjType.WEAKREF)
                {
                    int ind = (int)(pair.Value.Value._WeakRef.ReferencedObject.Value.i_Int);
                    while (funcPro.Literals.Count <= ind)
                    {
                        funcPro.Literals.Add(new(string.Empty));
                    }
                    funcPro.Literals[ind].Value.s_String = pair.Key;
                }
                else
                {
                    int ind = (int)pair.Value.Value.i_Int;
                    while (funcPro.Literals.Count <= ind)
                    {
                        funcPro.Literals.Add(new(string.Empty));
                    }
                    funcPro.Literals[ind].Value.s_String = pair.Key;
                }
            }

            int i;
            for (i = 0; i < Functions.Count; i++)
            {
                funcPro.Functions.Add(Functions[i]);
            }
            for (i = 0; i < Parameters.Count; i++)
            {
                funcPro.Parameters.Add(Parameters[i]);
            }
            for (i = 0; i < OuterInfos.Count; i++)
            {
                funcPro.Outers.Add(OuterInfos[i]);
            }
            for (i = 0; i < LocalVariableInfos.Count; i++)
            {
                funcPro.LocalInfos.Add(LocalVariableInfos[i]);
            }
            for (i = 0; i < LineInfos.Count; i++)
            {
                funcPro.LineInfos.Add(LineInfos[i]);
            }
            for (i = 0; i < DefaultParameters.Count; i++)
            {
                funcPro.DefaultParameters.Add(DefaultParameters[i]);
            }

            foreach (ExInstr it in Instructions)
            {
                funcPro.Instructions.Add(new ExInstr() { op = it.op, arg0 = it.arg0, arg1 = it.arg1, arg2 = it.arg2, arg3 = it.arg3 });
            }

            funcPro.HasVargs = HasVargs;

            return funcPro;
        }

        public ExFState Copy()
        {
            return new()
            {
                ChildrenFStates = ChildrenFStates,
                DefaultParameters = DefaultParameters,
                Functions = Functions,
                Instructions = Instructions,
                LastLine = LastLine,
                LineInfos = LineInfos,
                Literals = Literals,
                LocalVariableInfos = LocalVariableInfos,
                LocalVariables = LocalVariables,
                Name = Name,
                nLiterals = nLiterals,
                NotSnoozed = NotSnoozed,
                nOuters = nOuters,
                OuterInfos = OuterInfos,
                Parameters = Parameters,
                ParentFState = ParentFState,
                HasVargs = HasVargs,
                ReturnExpressionTarget = ReturnExpressionTarget,
                Source = Source,
                SharedState = SharedState,
                StackSize = StackSize,
                Stack = Stack
            };
        }


        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExFState()
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
