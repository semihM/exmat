using System;
using System.Collections.Generic;
using System.Linq;
using ExMat.Exceptions;
using ExMat.FuncPrototype;
using ExMat.InfoVar;
using ExMat.Objects;
using ExMat.OPs;
using ExMat.Utils;

namespace ExMat.States
{
    /// <summary>
    /// Function state tracking for compiler
    /// </summary>
    internal class ExFState : IDisposable
    {
        /// <summary>
        /// Shared state
        /// </summary>
        public ExSState SharedState;

        /// <summary>
        /// Source code
        /// </summary>
        // Kod dizisi kopyası
        public ExObject Source;

        /// <summary>
        /// Current closure's name
        /// </summary>
        // İçinde bulunulan fonksiyon ismi
        public ExObject Name;

        /// <summary>
        /// Literals and objects storing the literals
        /// </summary>
        // Kullanılan değişken isimleri ve yazı dizileri
        public Dictionary<string, ExObject> Literals = new();
        /// <summary>
        /// Literals count
        /// </summary>
        public int nLiterals;

        /// <summary>
        /// Local variable information
        /// </summary>
        // Değişken bilgisi listeleri
        private List<ExLocalInfo> LocalVariables = new();
        private List<ExLocalInfo> LocalVariableInfos = new();

        // Bilinmeyen değişken bilgisi listesi
        internal int nOuters;
        internal List<ExOuterInfo> OuterInfos = new();

        // Parametre bilgileri
        private List<ExObject> Parameters = new();
        private List<int> DefaultParameters = new();

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
        public bool NotSnoozed = true;
        // Belirsiz sayıda parametreli ?
        public bool HasVargs;

        // Değerin dönüleceği bellek pozisyonu
        public int ReturnExpressionTarget;

        private const int MAX_STACK_SIZE = 255;
        private const int MAX_LITERALS = ushort.MaxValue;

        internal List<ExLineInfo> LineInfos = new();
        public int LastLine;

        private readonly int Invalid = ExMat.InvalidArgument; // To get rid of CA1822 warnings

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

                    Instructions.RemoveAll((ExInstr i) => true);
                    Instructions = null;

                    ExDisposer.DisposeList(ref ChildrenFStates);
                    ExDisposer.DisposeList(ref LocalVariables);
                    ExDisposer.DisposeList(ref LocalVariableInfos);
                    ExDisposer.DisposeList(ref OuterInfos);
                    ExDisposer.DisposeList(ref Parameters);
                    ExDisposer.DisposeList(ref Functions);

                    ExDisposer.DisposeDict(ref Literals);
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

            ParentFState = parent;

            StackSize = 0;
            ReturnExpressionTarget = 0;
            nOuters = 0;

            HasVargs = false;
        }

        public void SetInstrParams(int pos, int p1, int p2, int p3, int p4)
        {
            ExInstr inst = Instructions[pos];
            inst.arg0 = p1;
            inst.arg1 = p2;
            inst.arg2 = p3;
            inst.arg3 = p4;
            Instructions[pos] = inst;
        }
        public void UpdateInstructionArgument(int pos, int pno, int val)
        {
            ExInstr inst = Instructions[pos];
            switch (pno)
            {
                case 0:
                    {
                        inst.arg0 = val;
                        break;
                    }
                case 1:
                case 4:
                    {
                        inst.arg1 = val;
                        break;
                    }
                case 2:
                    {
                        inst.arg2 = val;
                        break;
                    }
                case 3:
                    {
                        inst.arg3 = val;
                        break;
                    }
            }
            Instructions[pos] = inst;
        }

        public bool IsConst(ExObject idx, out ExObject cnst)
        {
            if (SharedState.Consts.ContainsKey(idx.GetString()))
            {
                cnst = SharedState.Consts[idx.GetString()];
                return true;
            }
            cnst = null;
            return false;
        }

        public long GetLiteral(ExObject o)
        {
            string name = o.Type == ExObjType.SPACE ? o.GetSpace().GetString() : o.GetString();
            ExObject v = new();
            if (!Literals.ContainsKey(name))
            {
                v.Value.i_Int = nLiterals;
                Literals.Add(name, v);
                nLiterals++;
                if (nLiterals > MAX_LITERALS)
                {
                    v.Nullify();
                    throw new ExException("too many literals");
                }
            }
            else
            {
                ExObject val = Literals[name];
                v = val.Type == ExObjType.WEAKREF ? val.ValueCustom._WeakRef.ReferencedObject : val;
            }

            return v.Value.i_Int;
        }

        public int GetCurrPos()
        {
            return Instructions.Count - 1;
        }

        public int GetLocalVariablesCount()
        {
            return LocalVariables.Count;
        }

        public void SetLocalStackSize(int s)
        {
            int c_s = GetLocalVariablesCount();

            while (c_s > s)
            {
                c_s--;
                ExLocalInfo li = LocalVariables.Last();
                if (ExTypeCheck.IsNotNull(li.Name))
                {
                    if (li.EndOPC == int.MaxValue)  //
                    {
                        nOuters--;
                    }
                    li.EndOPC = GetCurrPos();   // = Komut listesi uzunluğu
                    LocalVariableInfos.Add(li);
                }
                LocalVariables.RemoveAt(GetLocalVariablesCount() - 1);
            }
        }

        public int GetOuterSize(int start)
        {
            int c = 0;
            int ls = GetLocalVariablesCount() - 1;
            while (ls >= start)
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
            if (LastLine != line || op || forced)
            {
                ExLineInfo li = new();
                li.Line = line;
                li.Position = GetCurrPos() + 1;

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
                    case ExOperationCode.SET:
                    case ExOperationCode.NEWSLOT:
                    case ExOperationCode.SETOUTER:
                    case ExOperationCode.CALL:
                        {
                            if (instr.arg0 == dissed)
                            {
                                instr.arg0 = ExMat.InvalidArgument;
                            }
                            break;
                        }
                }
            }
        }

        public int TopTarget() // Üstteki değeri dön
        {
            return (int)Stack.Back().GetInt();
        }

        public int PopTarget() // Üstteki objeyi çıkart ve dön
        {
            int n = (int)Stack.Back().GetInt();
            if (ExTypeCheck.IsNull(LocalVariables[n].Name))
            {
                LocalVariables.RemoveAt(GetLocalVariablesCount() - 1);
            }
            Stack.Pop();
            return n;
        }

        public int PushTarget(int n = -1) // Üste obje ekle
        {
            if (n != -1)
            {
                Stack.Push(new(n));
                return n;
            }
            n = FindAStackPos(); // Boş değişken oluştur, pozisyonu dön
            Stack.Push(new(n));
            return n;
        }

        public int FindAStackPos()
        {
            int size = GetLocalVariablesCount();
            LocalVariables.Add(new());
            if (size > StackSize)
            {
                if (StackSize > MAX_STACK_SIZE) // Çok fazla değişken tanımı
                {
                    throw new ExException("Too many locals!");
                }
                StackSize = size;
            }
            return size;
        }

        public bool IsLocalArg(long pos)
        {
            return pos < GetLocalVariablesCount() && ExTypeCheck.IsNotNull(LocalVariables[(int)pos].Name);
        }
        public bool IsLocalArg(int pos)
        {
            return pos < GetLocalVariablesCount() && ExTypeCheck.IsNotNull(LocalVariables[pos].Name);
        }

        public int GetLocal(ExObject local)
        {
            int c = GetLocalVariablesCount();
            string varname = local.GetString();
            while (c > 0)
            {
                if (LocalVariables[--c].Name.GetString() == varname)
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
                if (OuterInfos[i].Name.ValueCustom.s_String == obj.ValueCustom.s_String)
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

        public ExObject CreateString(string s)
        {
            if (!SharedState.Strings.ContainsKey(s))
            {
                ExObject str = new(s);
                SharedState.Strings.Add(s, str);
                return str;
            }
            return SharedState.Strings[s];
        }

        private bool UpdateInstrOpcMove(ExInstr prev, ExInstr curr, int size)
        {
            switch (prev.op)
            {
                case ExOperationCode.ADD:       // Toplama işlemi
                case ExOperationCode.SUB:
                case ExOperationCode.MLT:
                case ExOperationCode.EXP:
                case ExOperationCode.DIV:
                case ExOperationCode.MOD:
                case ExOperationCode.MMLT:
                case ExOperationCode.TRANSPOSE:
                case ExOperationCode.CARTESIAN:
                case ExOperationCode.BITWISE:
                case ExOperationCode.LOAD:      // Yazı dizisi ata
                case ExOperationCode.LOADINTEGER:
                case ExOperationCode.LOADFLOAT:
                case ExOperationCode.LOADBOOLEAN:
                case ExOperationCode.LOADCOMPLEX:
                case ExOperationCode.LOADSPACE:
                case ExOperationCode.GET:       // Objeye ait özelliği ata
                    {
                        if (prev.arg0 == curr.arg1) // Önceki hedef == şimdiki kaynak
                        {
                            prev.arg0 = curr.arg0;  // Önceki hedef = şimdiki hedef
                            NotSnoozed = false;     // Bir sonraki komuttan bağımsız yap
                            Instructions[size - 1] = prev;
                            return true;
                        }
                        break;
                    }
                case ExOperationCode.MOVE:
                    {
                        prev.op = ExOperationCode.DMOVE;
                        prev.arg2 = curr.arg0;
                        prev.arg3 = curr.arg1;
                        Instructions[size - 1] = prev;
                        return true;
                    }
            }
            return false;
        }

        private bool UpdateInstrOpcLoad(ExInstr prev, ExInstr curr, int size)
        {
            if (prev.op == ExOperationCode.LOAD && curr.arg1 <= ExMat.InvalidArgument)
            {
                prev.op = ExOperationCode.DLOAD;
                prev.arg2 = curr.arg0;
                prev.arg3 = curr.arg1;
                Instructions[size - 1] = prev;
                return true;
            }
            return false;
        }

        private bool UpdateInstrOpcLoadConstDict(ExInstr curr, int size)
        {
            curr.arg1 = curr.arg1 != ExMat.InvalidArgument && curr.arg1 <= size && Instructions[size - (int)curr.arg1].op == ExOperationCode.GETK
                ? Instructions[size - (int)curr.arg1].arg1
                : ExMat.InvalidArgument;
            return false;
        }

        private bool UpdateInstrOpcJumpZero(ExInstr prev, ExInstr curr, int size)
        {
            if (prev.op == ExOperationCode.CMP && prev.arg1 != ExMat.InvalidArgument)
            {
                prev.op = ExOperationCode.JCMP;
                prev.arg0 = prev.arg1;
                prev.arg1 = curr.arg1;
                Instructions[size - 1] = prev;
                return true;
            }
            return UpdateInstrOpcSet(curr);
        }

        private bool UpdateInstrOpcSet(ExInstr curr, bool outer = false)
        {
            if (outer)
            {
                if (curr.arg0 == curr.arg2)
                {
                    curr.arg0 = Invalid;
                }
            }
            else if (curr.arg0 == curr.arg3)
            {
                curr.arg0 = Invalid;
            }
            return false;
        }

        private bool UpdateInstrOpcReturn(ExInstr prev, ExInstr curr, int size)
        {
            if (ParentFState != null && curr.arg0 != ExMat.InvalidArgument && prev.op == ExOperationCode.CALL && ReturnExpressionTarget < size - 1)
            {
                prev.op = ExOperationCode.CALLTAIL;
                Instructions[size - 1] = prev;
            }
            else if (prev.op == ExOperationCode.CLOSE)
            {
                Instructions[size - 1] = curr;
                return true;
            }
            return false;
        }

        private bool UpdateInstrOpcGet(ExInstr prev, ExInstr curr, int size)
        {
            if (prev.op == ExOperationCode.LOAD && prev.arg0 == curr.arg2 && (!IsLocalArg((int)prev.arg0)))
            {
                prev.arg2 = curr.arg1;
                prev.op = ExOperationCode.GETK;
                prev.arg0 = curr.arg0;
                Instructions[size - 1] = prev;
                return true;
            }
            return false;
        }

        private bool UpdateInstrOpcPrepCall(ExInstr prev, ExInstr curr, int size)
        {
            if (prev.op == ExOperationCode.LOAD && prev.arg0 == curr.arg1 && (!IsLocalArg((int)prev.arg0)))
            {
                prev.op = ExOperationCode.PREPCALLK;
                prev.arg0 = curr.arg0;
                prev.arg2 = curr.arg2;
                prev.arg3 = curr.arg3;
                Instructions[size - 1] = prev;
                return true;
            }
            return false;
        }

        private bool UpdateInstrOpcAppend(ExInstr prev, ExInstr curr, int size)
        {
            ArrayAType idx = ArrayAType.INVALID;
            switch (prev.op)
            {
                case ExOperationCode.LOAD:
                    {
                        idx = ArrayAType.LITERAL;
                        break;
                    }
                case ExOperationCode.LOADINTEGER:
                    {
                        idx = ArrayAType.INTEGER;
                        break;
                    }
                case ExOperationCode.LOADFLOAT:
                    {
                        idx = ArrayAType.FLOAT;
                        break;
                    }
                case ExOperationCode.LOADBOOLEAN:
                    {
                        idx = ArrayAType.BOOL;
                        break;
                    }
                default:
                    break;
            }

            if (idx != ArrayAType.INVALID && prev.arg0 == curr.arg1 && (!IsLocalArg((int)prev.arg0)))
            {
                prev.op = ExOperationCode.APPENDTOARRAY;
                prev.arg0 = curr.arg0;
                prev.arg2 = (int)idx;
                prev.arg3 = ExMat.InvalidArgument;
                Instructions[size - 1] = prev;
                return true;
            }
            return false;
        }

        private bool UpdateInstrOpcEq(ExInstr prev, ExInstr curr, int size)
        {
            if (prev.op == ExOperationCode.LOAD && prev.arg0 == curr.arg1 && (!IsLocalArg((int)prev.arg0)))
            {
                prev.op = curr.op;
                prev.arg0 = curr.arg0;
                prev.arg2 = curr.arg2;
                prev.arg3 = ExMat.InvalidArgument;
                Instructions[size - 1] = prev;
                return true;
            }
            return false;
        }

        private bool UpdateInstrOpcLoadNull(ExInstr prev, ExInstr curr, int size)
        {
            if (prev.op == ExOperationCode.LOADNULL && (prev.arg0 + prev.arg1 == curr.arg0))
            {
                prev.arg1++;
                prev.op = ExOperationCode.LOADNULL;
                Instructions[size - 1] = prev;
                return true;
            }
            return false;
        }

        // Bir önceki komut ile bağlantıları inceler, gerekli değerleri değiştirir
        public void AddInstr(ExInstr curr)
        {
            int size = Instructions.Count;
            bool shouldReturn = false;

            if (size > 0 && NotSnoozed)
            {
                ExInstr prev = Instructions[size - 1];
                switch (curr.op)
                {
                    case ExOperationCode.MOVE:
                        {
                            shouldReturn = UpdateInstrOpcMove(prev, curr, size);
                            break;
                        }
                    case ExOperationCode.LOAD:
                        {
                            shouldReturn = UpdateInstrOpcLoad(prev, curr, size);
                            break;
                        }
                    case ExOperationCode.LOADCONSTDICT:
                        {
                            shouldReturn = UpdateInstrOpcLoadConstDict(curr, size);
                            break;
                        }
                    case ExOperationCode.JZ:
                        {
                            shouldReturn = UpdateInstrOpcJumpZero(prev, curr, size);
                            break;
                        }
                    case ExOperationCode.SET:
                        {
                            shouldReturn = UpdateInstrOpcSet(curr);
                            break;
                        }
                    case ExOperationCode.SETOUTER:
                        {
                            shouldReturn = UpdateInstrOpcSet(curr, true);
                            break;
                        }
                    case ExOperationCode.RETURN:
                        {
                            shouldReturn = UpdateInstrOpcReturn(prev, curr, size);
                            break;
                        }
                    case ExOperationCode.GET:
                        {
                            shouldReturn = UpdateInstrOpcGet(prev, curr, size);
                            break;
                        }
                    case ExOperationCode.PREPCALL:
                        {
                            shouldReturn = UpdateInstrOpcPrepCall(prev, curr, size);
                            break;
                        }
                    case ExOperationCode.APPENDTOARRAY:
                        {
                            shouldReturn = UpdateInstrOpcAppend(prev, curr, size);
                            break;
                        }
                    case ExOperationCode.EQ:
                    case ExOperationCode.NEQ:
                        {
                            shouldReturn = UpdateInstrOpcEq(prev, curr, size);
                            break;
                        }
                    case ExOperationCode.LOADNULL:
                        {
                            shouldReturn = UpdateInstrOpcLoadNull(prev, curr, size);
                            break;
                        }
                }
            }

            if (shouldReturn)
            {
                return;
            }

            NotSnoozed = true;      // Tekrardan bağımlılığa izin ver
            Instructions.Add(curr); // Komut listesine ekle
        }

        // Komut listesinin sonuna yeni bir komut ekler
        public void AddInstr(ExOperationCode op, int arg0, long arg1, int arg2, int arg3)
        {
            ExInstr instr = new() { op = op, arg0 = arg0, arg1 = arg1, arg2 = arg2, arg3 = arg3 };
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

        public int PushVar(ExObject v)
        {
            int n = GetLocalVariablesCount();
            LocalVariables.Add(new() { Name = new(v.GetString()), StartOPC = GetCurrPos() + 1, Position = n });
            if (n >= StackSize)
            {
                StackSize = n + 1;
            }
            return n;
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
                int ind = pair.Value.Type == ExObjType.WEAKREF
                    ? (int)pair.Value.GetWeakRef().ReferencedObject.Value.i_Int
                    : (int)pair.Value.Value.i_Int;

                while (funcPro.Literals.Count <= ind)
                {
                    funcPro.Literals.Add(new(string.Empty));
                }
                funcPro.Literals[ind].SetString(pair.Key);
            }

            ExUtils.ShallowAppend(Functions, funcPro.Functions);
            ExUtils.ShallowAppend(Parameters, funcPro.Parameters);
            ExUtils.ShallowAppend(OuterInfos, funcPro.Outers);
            ExUtils.ShallowAppend(LocalVariableInfos, funcPro.LocalInfos);
            ExUtils.ShallowAppend(LineInfos, funcPro.LineInfos);
            ExUtils.ShallowAppend(DefaultParameters, funcPro.DefaultParameters);
            ExUtils.ShallowAppend(Functions, funcPro.Functions);

            foreach (ExInstr it in Instructions)
            {
                funcPro.Instructions.Add(new(it));
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

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
