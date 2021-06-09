using System;
using System.Collections.Generic;
using System.Diagnostics;
using ExMat.FuncPrototype;
using ExMat.Lexer;
using ExMat.Objects;
using ExMat.OPs;
using ExMat.States;
using ExMat.Token;
using ExMat.VM;

namespace ExMat.Compiler
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExScope
    {
        public int nOuters;     // Referans edilen dışardaki değişken sayısı
        public int nLocal;      // Çerçevede tanımlı değişken sayısı

        public string GetDebuggerDisplay()
        {
            return "SCOPE(nOuters: " + nOuters + ", StackSize: " + nLocal + ")";
        }
    }

    public class ExCompiler : IDisposable
    {
        private ExVM VM;                // Derleyicinin hedef sanal makinesi

        private string Source;          // Kod dizisi
        private ExLexer Lexer;          // Sembol ayırıcı
        private TokenType CurrentToken; // En son okunan sembol

        private ExEState ExpressionState = new();   // İfade durumu takibi
        private ExFState FunctionState;             // Fonksiyon durumu takibi

        private ExScope CurrentScope = new();       // Kod bloğu takibi

        public string ErrorString;      // Hata mesajı

        private readonly bool StoreLineInfos;
        private bool disposedValue;
        private bool IsInMacroBlock;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    FunctionState = null;
                    ExpressionState = null;
                    Lexer.Dispose();
                    VM = null;
                    CurrentScope = null;
                    ErrorString = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void AddToErrorMessage(string msg)
        {
            if (string.IsNullOrEmpty(ErrorString))
            {
                ErrorString = "[ERROR] " + msg;
            }
            else
            {
                ErrorString += "\n[ERROR] " + msg;
            }
        }

        public ExScope CreateScope()
        {
            ExScope old = new() { nOuters = CurrentScope.nOuters, nLocal = CurrentScope.nLocal };
            CurrentScope.nLocal = FunctionState.GetLocalVariablesCount();
            CurrentScope.nOuters = FunctionState.nOuters;
            return old;
        }

        public void ReleaseScope(ExScope old, bool close = true)
        {
            int old_nout = FunctionState.nOuters;
            if (FunctionState.GetLocalVariablesCount() != CurrentScope.nLocal)
            {
                FunctionState.SetLocalStackSize(CurrentScope.nLocal);
                if (close && old_nout != FunctionState.nOuters)
                {
                    FunctionState.AddInstr(OPC.CLOSE, 0, CurrentScope.nLocal, 0, 0);
                }
            }
            CurrentScope = old;
        }

        public ExCompiler(bool linfos = false)
        {
            StoreLineInfos = linfos;
            CurrentScope.nOuters = 0;
            CurrentScope.nLocal = 0;
        }

        public void AddErrorInfo()
        {
            AddToErrorMessage("[LINE: " + Lexer.CurrentLine + ", COL: " + Lexer.CurrentCol + "] " + Lexer.ErrorString);
        }

        public bool InitializeCompiler(ExVM vm, string src, ref ExObject o)
        {
            VM = vm;
            Source = src;
            Lexer = new ExLexer(src);

            bool state = Compile(ref o);

            if (!state)
            {
                if (CurrentToken == TokenType.ENDLINE)
                {
                    AddToErrorMessage("syntax error");
                }
                AddErrorInfo();
            }
            return state;
        }

        public bool Compile(ref ExObject o)
        {
            // Ana fonksiyon "main" oluştur
            FunctionState = new(sState: VM.SharedState, parent: null);   
            FunctionState.Name = VM.CreateString("main");

            // Fonksiyonun kendisine işaret eden "this" parametresi
            FunctionState.AddParam(FunctionState.CreateString(ExMat.ThisName));

            // Belirsiz sayıda argüman ile çağırılır yap, "vargs" listesine erişim ver
            FunctionState.AddParam(FunctionState.CreateString(ExMat.VargsName));
            FunctionState.HasVargs = true;

            // Kaynak kod dizisi kopyası
            FunctionState.Source = new(Source);

            // İlk sembolü oku
            if (!ReadAndSetToken())
            {
                return false;
            }

            while (CurrentToken != TokenType.ENDLINE)
            {
                if (!ProcessStatement())
                {
                    return false;
                }
                if (Lexer.TokenPrev != TokenType.CURLYCLOSE && Lexer.TokenPrev != TokenType.SMC)
                {
                    if (!CheckSMC())
                    {
                        return false;
                    }
                }
            }

            FunctionState.SetLocalStackSize(0);
            FunctionState.AddLineInfo(Lexer.CurrentLine, StoreLineInfos, true);
            FunctionState.AddInstr(OPC.RETURN, 985, 2, VM.IsInteractive ? 1 : 0, 0);
            FunctionState.SetLocalStackSize(0);

            o = new(FunctionState.CreatePrototype());

            return true;
        }

        private bool ReadAndSetToken()
        {
            return (CurrentToken = Lexer.Lex()) != TokenType.UNKNOWN;
        }

        public string GetStringForTokenType(TokenType typ)
        {
            foreach (KeyValuePair<string, TokenType> pair in Lexer.KeyWords)
            {
                if (pair.Value == typ)
                {
                    return pair.Key;
                }
            }
            return typ.ToString();
        }

        public ExObject Expect(TokenType expected)
        {
            if (CurrentToken != expected                        // Beklenmedik sembol
                && (expected != TokenType.IDENTIFIER            // Tanımsal bir sembol veya sınıf inşa fonksiyonu değil
                    || CurrentToken != TokenType.CONSTRUCTOR))
            {
                    AddToErrorMessage("Expected " + GetStringForTokenType(expected) + ", got " + CurrentToken.ToString());
                    return null;    // boş değer dönerek hata bildir
            }

            ExObject res = new();
            switch (expected)       // Sembol ayırıcıdan denk gelen değeri al
            {
                case TokenType.IDENTIFIER:
                case TokenType.LITERAL:
                    {
                        res = FunctionState.CreateString(Lexer.ValString);
                        break;
                    }
                case TokenType.INTEGER:
                    {
                        res = new(Lexer.ValInteger);
                        break;
                    }
                case TokenType.FLOAT:
                    {
                        res = new(Lexer.ValFloat);
                        break;
                    }
                case TokenType.COMPLEX:
                    {
                        res = new(Lexer.TokenComplex == TokenType.INTEGER ? Lexer.ValInteger : Lexer.ValFloat);
                        break;
                    }
                case TokenType.SPACE:
                    {
                        res = new(Lexer.ValSpace);
                        break;
                    }
            }
            if (!ReadAndSetToken())
            {
                return null;    // Bilinmeyen sembol bulundu, boş değer dönerek bildir
            }
            return res;     // Sembol ayırıcının bulduğu, beklenilen değeri taşıyan objeyi dön
        }

        public bool IsEOS()
        {
            return Lexer.TokenPrev == TokenType.NEWLINE     // \n
               || CurrentToken == TokenType.ENDLINE         // \0
               || CurrentToken == TokenType.CURLYCLOSE      // }
               || CurrentToken == TokenType.SMC;            // ;
        }

        public bool CheckSMC()
        {
            if (CurrentToken == TokenType.SMC)                          // || CurrentToken == TokenType.MACROBLOCK)
            {
                if (!ReadAndSetToken())
                {
                    return false;
                }
            }
            else if (!IsEOS())
            {
                AddToErrorMessage("Expected end of statement");
                return false;
            }
            return true;
        }

        public bool ProcessStatements()                                 // bool macro = false)
        {
            while (CurrentToken != TokenType.CURLYCLOSE)   // } ?             // && (!macro || (CurrentToken != TokenType.MACROEND)))
            {
                if (!ProcessStatement())  // İfadeyi derle                               // macro: macro))
                {
                    return false;
                }

                if (Lexer.TokenPrev != TokenType.CURLYCLOSE
                    && Lexer.TokenPrev != TokenType.SMC)                // && (!macro || (CurrentToken != TokenType.MACROEND)))
                {
                    if (!CheckSMC())    // İfade sonu kontrolü
                    {
                        return false;
                    }
                }
                #region _
                /*
                if (macro && CurrentToken == TokenType.MACROEND)
                {
                    return true;
                }
                */
                #endregion
            }
            return true;
        }

        
        public bool ProcessStatement(bool closeScope = true)   // İfadeyi derle                             //, bool macro = false)
        {
            switch (CurrentToken)
            {
                case TokenType.SMC:         // ;
                    {
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.CURLYOPEN:   // {
                    {
                        ExScope scp = CreateScope();

                        if (!ReadAndSetToken()
                            || !ProcessStatements()
                            || Expect(TokenType.CURLYCLOSE) == null)
                        {
                            return false;
                        }

                        ReleaseScope(scp, closeScope);
                        break;
                    }
                // Diğer ifadeler
                #region Other Statements: IF, FOR, VAR, FUNCTION, CLASS, RETURN, BREAK, CONTINUE
                case TokenType.IF:
                    {
                        if (!ProcessIfStatement())
                        {
                            return false;
                        }
                        break;
                    }

                case TokenType.FOR:
                    {
                        if (!ProcessForStatement())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.VAR:
                    {
                        if (!ProcessVarAsgStatement())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.RULE:
                    {
                        if (!ProcessRuleAsgStatement())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.FUNCTION:
                    {
                        if (!ProcessFunctionStatement())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.CLASS:
                    {
                        if (!ProcessClassStatement())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.SEQUENCE:
                    {
                        if (!ProcessSequenceStatement())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.CLUSTER:
                    {
                        if (!ProcessClusterAsgStatement())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.RETURN:
                    {
                        OPC op = OPC.RETURN;
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        if (!IsEOS())
                        {
                            int rettarget = FunctionState.GetCurrPos() + 1;
                            if (!ExSepExp())
                            {
                                return false;
                            }

                            FunctionState.ReturnExpressionTarget = rettarget;
                            FunctionState.AddInstr(op, 1, FunctionState.PopTarget(), FunctionState.GetLocalVariablesCount(), 0);
                        }
                        else
                        {
                            FunctionState.ReturnExpressionTarget = -1;
                            FunctionState.AddInstr(op, 985, 0, FunctionState.GetLocalVariablesCount(), 0);
                        }
                        break;
                    }
                case TokenType.BREAK:
                    {
                        if (FunctionState.BreakTargetsList.Count <= 0)
                        {
                            AddToErrorMessage("'break' has to be in a breakable block");
                            return false;
                        }

                        DoOuterControl();
                        FunctionState.AddInstr(OPC.JMP, 0, -1234, 0, 0);
                        FunctionState.BreakList.Add(FunctionState.GetCurrPos());

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.CONTINUE:
                    {
                        if (FunctionState.ContinueTargetList.Count <= 0)
                        {
                            AddToErrorMessage("'continue' has to be in a breakable block");
                            return false;
                        }

                        DoOuterControl();
                        FunctionState.AddInstr(OPC.JMP, 0, -1234, 0, 0);
                        FunctionState.ContinueList.Add(FunctionState.GetCurrPos());

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                #endregion 
                #region SEQUENCE, CLUSTER, RULE

                #endregion
                #region _
                /*
            case TokenType.MACROBLOCK:
                {
                    if (!ProcessMacroBlockStatement())
                    {
                        return false;
                    }
                    break;
                }
            case TokenType.MACROSTART:
                {
                    if (!ProcessMacroStatement())
                    {
                        return false;
                    }
                    break;
                }
                */
                #endregion
                // 
                default:
                    {
                        if (!ExSepExp())
                        {
                            return false;
                        }
                        #region _
                        /*
                        if (!macro)
                        {
                            FunctionState.DiscardTopTarget();
                        }
                        */
                        #endregion
                        break;
                    }
            }
            FunctionState.NotSnoozed = false;
            return true;
        }

        public void DoOuterControl()
        {
            if (FunctionState.GetLocalVariablesCount() != CurrentScope.nLocal)
            {
                if (FunctionState.GetOuterSize(CurrentScope.nLocal) > 0)
                {
                    FunctionState.AddInstr(OPC.CLOSE, 0, CurrentScope.nLocal, 0, 0);
                }
            }
        }
        public List<int> CreateBreakableBlock()
        {
            FunctionState.BreakTargetsList.Add(0);
            FunctionState.ContinueTargetList.Add(0);
            return new(2) { FunctionState.BreakList.Count, FunctionState.ContinueList.Count };
        }

        public void ReleaseBreakableBlock(List<int> bc, int t)
        {
            bc[0] = FunctionState.BreakList.Count - bc[0];
            bc[1] = FunctionState.ContinueList.Count - bc[1];

            if (bc[0] > 0)
            {
                DoBreakControl(FunctionState, bc[0]);
            }

            if (bc[1] > 0)
            {
                DoContinueControl(FunctionState, bc[1], t);
            }

            if (FunctionState.BreakList.Count > 0)
            {
                FunctionState.BreakList.RemoveAt(FunctionState.BreakList.Count - 1);
            }
            if (FunctionState.ContinueList.Count > 0)
            {
                FunctionState.ContinueList.RemoveAt(FunctionState.ContinueList.Count - 1);
            }
        }

        public static void DoBreakControl(ExFState fs, int count)
        {
            while (count > 0)
            {
                int p = fs.BreakList[^1];
                fs.BreakList.RemoveAt(fs.BreakList.Count - 1);
                fs.SetInstrParams(p, 0, fs.GetCurrPos() - p, 0, 0);
                count--;
            }
        }

        public static void DoContinueControl(ExFState fs, int count, int t)
        {
            while (count > 0)
            {
                int p = fs.ContinueList[^1];
                fs.ContinueList.RemoveAt(fs.ContinueList.Count - 1);
                fs.SetInstrParams(p, 0, t - p, 0, 0);
                count--;
            }
        }

        public bool ProcessIfStatement()
        {
            int jpos;
            bool b_else = false;
            if (!ReadAndSetToken()
                || Expect(TokenType.ROUNDOPEN) == null
                || !ExSepExp()
                || Expect(TokenType.ROUNDCLOSE) == null)
            {
                return false;
            }

            FunctionState.AddInstr(OPC.JZ, FunctionState.PopTarget(), 0, 0, 0);
            int jnpos = FunctionState.GetCurrPos();

            ExScope old = CreateScope();
            if (!ProcessStatement())
            {
                return false;
            }
            if (CurrentToken != TokenType.CURLYCLOSE && CurrentToken != TokenType.ELSE)
            {
                if (!CheckSMC())
                {
                    return false;
                }
            }

            ReleaseScope(old);
            int epos = FunctionState.GetCurrPos();
            if (CurrentToken == TokenType.ELSE)
            {
                b_else = true;

                old = CreateScope();
                FunctionState.AddInstr(OPC.JMP, 0, 0, 0, 0);
                jpos = FunctionState.GetCurrPos();

                if (!ReadAndSetToken() || !ProcessStatement())
                {
                    return false;
                }

                if (Lexer.TokenPrev != TokenType.CURLYCLOSE)
                {
                    if (!CheckSMC())
                    {
                        return false;
                    }
                }
                ReleaseScope(old);

                FunctionState.SetInstrParam(jpos, 1, FunctionState.GetCurrPos() - jpos);
            }
            FunctionState.SetInstrParam(jnpos, 1, epos - jnpos + (b_else ? 1 : 0));
            return true;
        }

        public bool ProcessForStatement()
        {
            if (!ReadAndSetToken())
            {
                return false;
            }

            ExScope scp = CreateScope();
            if (Expect(TokenType.ROUNDOPEN) == null)
            {
                return false;
            }

            if (CurrentToken == TokenType.VAR)
            {
                if (!ProcessVarAsgStatement())
                {
                    return false;
                }
            }
            else if (CurrentToken != TokenType.SMC)
            {
                if (!ExSepExp())
                {
                    return false;
                }
                FunctionState.PopTarget();
            }

            if (Expect(TokenType.SMC) == null)
            {
                return false;
            }

            FunctionState.NotSnoozed = false;

            int jpos = FunctionState.GetCurrPos();
            int jzpos = -1;

            if (CurrentToken != TokenType.SMC)
            {
                if (!ExSepExp())
                {
                    return false;
                }
                FunctionState.AddInstr(OPC.JZ, FunctionState.PopTarget(), 0, 0, 0);
                jzpos = FunctionState.GetCurrPos();
            }

            if (Expect(TokenType.SMC) == null)
            {
                return false;
            }
            FunctionState.NotSnoozed = false;

            int estart = FunctionState.GetCurrPos() + 1;
            if (CurrentToken != TokenType.ROUNDCLOSE)
            {
                if (!ExSepExp())
                {
                    return false;
                }
                FunctionState.PopTarget();
            }

            if (Expect(TokenType.ROUNDCLOSE) == null)
            {
                return false;
            }

            FunctionState.NotSnoozed = false;

            int eend = FunctionState.GetCurrPos();
            int esize = eend - estart + 1;
            List<ExInstr> instrs = null;

            if (esize > 0)
            {
                instrs = new(esize);
                int n_instr = FunctionState.Instructions.Count;
                for (int i = 0; i < esize; i++)
                {
                    instrs.Add(FunctionState.Instructions[estart + i]);
                }
                for (int i = 0; i < esize; i++)
                {
                    FunctionState.Instructions.RemoveAt(FunctionState.Instructions.Count - 1);
                }
            }

            List<int> bc = CreateBreakableBlock();

            if (!ProcessStatement())
            {
                return false;
            }
            int ctarg = FunctionState.GetCurrPos();

            if (esize > 0)
            {
                for (int i = 0; i < esize; i++)
                {
                    FunctionState.AddInstr(instrs[i]);
                }
            }

            FunctionState.AddInstr(OPC.JMP, 0, jpos - FunctionState.GetCurrPos() - 1, 0, 0);

            if (jzpos > 0)
            {
                FunctionState.SetInstrParam(jzpos, 1, FunctionState.GetCurrPos() - jzpos);
            }

            ReleaseScope(scp);
            ReleaseBreakableBlock(bc, ctarg);

            return true;
        }

        public static bool ProcessForeachStatement()
        {
            return false;
        }
        public bool ExSequenceCreate(ExObject o)
        {
            ExFState f_state = FunctionState.PushChildState(VM.SharedState);
            f_state.Name = o;

            ExObject pname;
            f_state.AddParam(FunctionState.CreateString(ExMat.ThisName));
            f_state.AddParam(FunctionState.CreateString("n"));

            f_state.Source = new(Source);
            int pcount = 0;

            while (CurrentToken != TokenType.ROUNDCLOSE)
            {
                bool neg = false;
                if (CurrentToken == TokenType.SUB)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                    neg = true;
                }

                if ((pname = Expect(TokenType.INTEGER)) == null && (pname = Expect(TokenType.FLOAT)) == null)    // CONTINUE HERE, ALLOW PARAMETERS
                {
                    if ((pname = Expect(TokenType.IDENTIFIER)) != null)
                    {
                        if (pcount > 0)
                        {
                            AddToErrorMessage("can't add sequence parameters after sequence constraints");
                            return false;
                        }

                        if (neg)
                        {
                            AddToErrorMessage("can't negate IDENTIFIER");
                            return false;
                        }

                        pcount++;
                        f_state.AddParam(pname);

                        if (CurrentToken == TokenType.ASG)
                        {
                            if (!ReadAndSetToken())
                            {
                                return false;
                            }

                            if (!ExExp())
                            {
                                return false;
                            }

                            f_state.AddDefParam(FunctionState.TopTarget());
                        }
                        else // TO-DO add = for referencing global and do get ops
                        {
                            AddToErrorMessage("expected '=' for a sequence parameter default value");
                            return false;
                        }
                    }
                    else
                    {
                        if (pcount > 0)
                        {
                            AddToErrorMessage("expected integer constraint for sequence declaration");
                            return false;
                        }
                        else
                        {
                            AddToErrorMessage("expected identifier or integer constraints for sequence declaration");
                            return false;
                        }
                    }
                }
                else
                {
                    if (pname.Type == ExObjType.INTEGER)
                    {
                        if (neg)
                        {
                            pname.Value.i_Int *= -1;
                        }

                        pname = new(pname.GetInt().ToString());
                    }
                    else if (pname.Type == ExObjType.FLOAT)
                    {
                        if (neg)
                        {
                            pname.Value.f_Float *= -1;
                        }

                        pname = new(pname.GetFloat().ToString());
                    }
                    else
                    {
                        AddToErrorMessage("expected integer or float for sequence constraint");
                        return false;
                    }
                    pcount++;
                    f_state.AddParam(pname);

                    if (CurrentToken == TokenType.COL)
                    {
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }

                        if (!ExExp())
                        {
                            return false;
                        }

                        f_state.AddDefParam(FunctionState.TopTarget());
                    }
                    else // TO-DO add = for referencing global and do get ops
                    {
                        AddToErrorMessage("expected ':' for a sequence constants");
                        return false;
                    }
                }


                if (CurrentToken == TokenType.SEP)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                else if (CurrentToken != TokenType.ROUNDCLOSE)
                {
                    AddToErrorMessage("expected ')' for sequence constants definition end");
                    return false;
                }
            }
            if (Expect(TokenType.ROUNDCLOSE) == null)
            {
                return false;
            }

            for (int i = 0; i < pcount; i++)
            {
                FunctionState.PopTarget();
            }

            ExFState tmp = FunctionState.Copy();
            FunctionState = f_state;

            if (!ExExp())
            {
                return false;
            }
            f_state.AddInstr(OPC.RETURN, 1, FunctionState.PopTarget(), 0, 0);

            f_state.AddLineInfo(Lexer.TokenPrev == TokenType.NEWLINE ? Lexer.PrevTokenLine : Lexer.CurrentLine, StoreLineInfos, true);
            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExPrototype fpro = f_state.CreatePrototype();
            fpro.ClosureType = ExClosureType.SEQUENCE;

            FunctionState = tmp;
            FunctionState.Functions.Add(fpro);
            FunctionState.PopChildState();

            return true;
        }

        public bool ProcessSequenceStatement()
        {
            ExObject v;
            if (!ReadAndSetToken() || (v = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            FunctionState.PushTarget(0);
            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(v), 0, 0);

            if (Expect(TokenType.ROUNDOPEN) == null || !ExSequenceCreate(v))
            {
                return false;
            }

            FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            FunctionState.PopTarget();

            return true;
        }

        public bool ProcessClusterAsgStatement()
        {
            ExObject v;
            if (!ReadAndSetToken() || (v = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            FunctionState.PushTarget(0);
            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(v), 0, 0);

            if (Expect(TokenType.CURLYOPEN) == null || !ExClusterCreate(v))
            {
                return false;
            }

            FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            FunctionState.PopTarget();

            return true;
        }

        public bool ExClusterCreate(ExObject o)
        {
            ExFState f_state = FunctionState.PushChildState(VM.SharedState);
            f_state.Name = o;

            ExObject pname;
            f_state.AddParam(FunctionState.CreateString(ExMat.ThisName));
            f_state.Source = new(Source);
            int pcount = 0;

            while (CurrentToken != TokenType.SMC)
            {
                if ((pname = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                pcount++;
                f_state.AddParam(pname);

                if (CurrentToken == TokenType.IN)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }

                    if (CurrentToken != TokenType.SPACE)
                    {
                        //AddToErrorMessage("expected a constant SPACE for parameter " + pcount + "'s domain");
                        //return false;
                        if (!ExExp())
                        {
                            return false;
                        }
                    }
                    else
                    {
                        AddSpaceConstLoadInstr(Lexer.ValSpace, -1);
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                    }

                    f_state.AddDefParam(FunctionState.TopTarget());
                }
                else // TO-DO add = for referencing global and do get ops
                {
                    AddToErrorMessage("expected 'in' for a domain reference");
                    return false;
                }

                if (CurrentToken == TokenType.SEP)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                else if (CurrentToken != TokenType.SMC && CurrentToken != TokenType.CURLYCLOSE)
                {
                    AddToErrorMessage("expected '}' ',' '=>' or ';' for cluster definition");
                    return false;
                }
            }

            if (!ReadAndSetToken())
            {
                return false;
            }

            for (int i = 0; i < pcount; i++)
            {
                FunctionState.PopTarget();
            }

            ExFState tmp = FunctionState.Copy();
            FunctionState = f_state;

            if (CurrentToken == TokenType.ELEMENTDEF)
            {
                AddToErrorMessage("expected '; [expression] => [return_value] }' after space specifications of cluster");
                return false;
            }
            else
            {
                if (!ExExp())
                {
                    return false;
                }
            }

            if (CurrentToken != TokenType.ELEMENTDEF)
            {
                AddToErrorMessage("expected '=>' to define elements of cluster");
                return false;
            }
            //
            FunctionState.AddInstr(OPC.JZ, FunctionState.PopTarget(), 0, 0, 0);

            int jzp = FunctionState.GetCurrPos();
            int t = FunctionState.PushTarget();

            if (!ReadAndSetToken() || !ExExp())
            {
                return false;
            }

            int f = FunctionState.PopTarget();
            if (t != f)
            {
                FunctionState.AddInstr(OPC.MOVE, t, f, 0, 0);
            }
            int end_f = FunctionState.GetCurrPos();

            FunctionState.AddInstr(OPC.JMP, 0, 0, 0, 0);

            int jmp = FunctionState.GetCurrPos();

            FunctionState.AddInstr(OPC.LOADBOOLEAN, FunctionState.PushTarget(), 0, 0, 0);

            int s = FunctionState.PopTarget();
            if (t != s)
            {
                FunctionState.AddInstr(OPC.MOVE, t, s, 0, 0);
            }

            FunctionState.SetInstrParam(jmp, 1, FunctionState.GetCurrPos() - jmp);
            FunctionState.SetInstrParam(jzp, 1, end_f - jzp + 1);
            FunctionState.NotSnoozed = false;
            //

            if (CurrentToken != TokenType.CURLYCLOSE)
            {
                AddToErrorMessage("expected '}' to declare a cluster");
                return false;
            }

            if (!ReadAndSetToken())
            {
                return false;
            }

            f_state.AddInstr(OPC.RETURN, 1, FunctionState.PopTarget(), 0, 0);
            f_state.AddLineInfo(Lexer.TokenPrev == TokenType.NEWLINE ? Lexer.PrevTokenLine : Lexer.CurrentLine, StoreLineInfos, true);
            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExPrototype fpro = f_state.CreatePrototype();
            fpro.ClosureType = ExClosureType.CLUSTER;

            FunctionState = tmp;
            FunctionState.Functions.Add(fpro);
            FunctionState.PopChildState();

            return true;
        }

        public bool ExSymbolCreate(ExObject o)
        {
            ExFState f_state = FunctionState.PushChildState(VM.SharedState);
            f_state.Name = o;

            ExObject pname;
            f_state.AddParam(FunctionState.CreateString(ExMat.ThisName));
            f_state.Source = new(Source);

            while (CurrentToken != TokenType.ROUNDCLOSE)
            {
                if ((pname = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                f_state.AddParam(pname);

                if (CurrentToken == TokenType.SEP)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                else if (CurrentToken != TokenType.ROUNDCLOSE)
                {
                    if (CurrentToken == TokenType.ASG)
                    {
                        AddToErrorMessage("default values are not supported for symbols");
                    }
                    else
                    {
                        AddToErrorMessage("expected ')' or ',' for symbol declaration");
                    }
                    return false;
                }
            }

            if (Expect(TokenType.ROUNDCLOSE) == null)
            {
                return false;
            }

            ExFState tmp = FunctionState.Copy();
            FunctionState = f_state;

            if (!ExExp())
            {
                return false;
            }
            f_state.AddInstr(OPC.RETURN, 1, FunctionState.PopTarget(), 0, 0);

            f_state.AddLineInfo(Lexer.TokenPrev == TokenType.NEWLINE ? Lexer.PrevTokenLine : Lexer.CurrentLine, StoreLineInfos, true);
            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExPrototype fpro = f_state.CreatePrototype();
            //fpro.ClosureType = ExClosureType.FORMULA;

            FunctionState = tmp;
            FunctionState.Functions.Add(fpro);
            FunctionState.PopChildState();

            return true;
        }

        public bool ProcessFormulaStatement()
        {
            ExObject v;
            if (!ReadAndSetToken() || (v = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            FunctionState.PushTarget(0);
            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(v), 0, 0);

            if (Expect(TokenType.ROUNDOPEN) == null || !ExSymbolCreate(v))
            {
                return false;
            }

            FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            return true;
        }
        public bool ExRuleCreate(ExObject o)
        {
            ExFState f_state = FunctionState.PushChildState(VM.SharedState);
            f_state.Name = o;

            ExObject pname;
            f_state.AddParam(FunctionState.CreateString(ExMat.ThisName));
            f_state.Source = new(Source);

            while (CurrentToken != TokenType.ROUNDCLOSE)
            {
                if ((pname = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                f_state.AddParam(pname);

                if (CurrentToken == TokenType.SEP)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                else if (CurrentToken != TokenType.ROUNDCLOSE)
                {
                    if (CurrentToken == TokenType.ASG)
                    {
                        AddToErrorMessage("default values are not supported for rules");
                    }
                    else
                    {
                        AddToErrorMessage("expected ')' or ',' for rule declaration");
                    }
                    return false;
                }
            }

            if (Expect(TokenType.ROUNDCLOSE) == null)
            {
                return false;
            }

            ExFState tmp = FunctionState.Copy();
            FunctionState = f_state;

            if (!ExExp())
            {
                return false;
            }
            f_state.AddInstr(OPC.RETURNBOOL, 1, FunctionState.PopTarget(), 0, 0);

            f_state.AddLineInfo(Lexer.TokenPrev == TokenType.NEWLINE ? Lexer.PrevTokenLine : Lexer.CurrentLine, StoreLineInfos, true);
            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExPrototype fpro = f_state.CreatePrototype();
            fpro.ClosureType = ExClosureType.RULE;

            FunctionState = tmp;
            FunctionState.Functions.Add(fpro);
            FunctionState.PopChildState();

            return true;
        }

        public bool ProcessRuleAsgStatement()
        {
            ExObject v;
            if (!ReadAndSetToken() || (v = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            FunctionState.PushTarget(0);
            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(v), 0, 0);

            if (Expect(TokenType.ROUNDOPEN) == null || !ExRuleCreate(v))
            {
                return false;
            }

            FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            return true;
        }

        public static bool ProcessSumStatement()
        {
            return false;
        }

        public bool ProcessVarAsgStatement(bool sym = false)
        {
            ExObject v;
            if (!ReadAndSetToken())
            {
                return false;
            }

            if (CurrentToken == TokenType.FUNCTION)
            {
                if (!ReadAndSetToken()
                    || (v = Expect(TokenType.IDENTIFIER)) == null
                    || Expect(TokenType.ROUNDOPEN) == null
                    || !ExFuncCreate(v))
                {
                    return false;
                }

                FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);
                FunctionState.PopTarget();
                FunctionState.PushVar(v);
                return true;
            }
            else if (CurrentToken == TokenType.RULE)
            {
                if (!ReadAndSetToken()
                    || (v = Expect(TokenType.IDENTIFIER)) == null
                    || Expect(TokenType.ROUNDOPEN) == null
                    || !ExRuleCreate(v))
                {
                    return false;
                }

                FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);
                FunctionState.PopTarget();
                FunctionState.PushVar(v);
                return true;
            }
            else if (CurrentToken == TokenType.CLUSTER)
            {
                if (!ReadAndSetToken()
                    || (v = Expect(TokenType.IDENTIFIER)) == null
                    || Expect(TokenType.CURLYOPEN) == null
                    || !ExClusterCreate(v))
                {
                    return false;
                }

                FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);
                FunctionState.PopTarget();
                FunctionState.PushVar(v);
                return true;
            }

            while (true)
            {
                if ((v = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                if (CurrentToken == TokenType.ASG)
                {
                    if (!ReadAndSetToken() || !ExExp())
                    {
                        return false;
                    }

                    int s = FunctionState.PopTarget();
                    int d = FunctionState.PushTarget();

                    if (d != s)
                    {
                        FunctionState.AddInstr(OPC.MOVE, d, s, 0, 0);
                    }
                }
                else
                {
                    FunctionState.AddInstr(OPC.LOADNULL, FunctionState.PushTarget(), 1, 0, 0);
                }
                FunctionState.PopTarget();
                FunctionState.PushVar(v);
                if (CurrentToken == TokenType.SEP)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                else
                {
                    break;
                }
            }
            return true;
        }

        public bool ProcessFunctionStatement()
        {
            ExObject idx;

            if (!ReadAndSetToken() || (idx = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            FunctionState.PushTarget(0);
            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(idx), 0, 0);

            if (CurrentToken == TokenType.GLOBAL)
            {
                AddBasicOpInstr(OPC.GET);
            }

            while (CurrentToken == TokenType.GLOBAL)
            {
                if (!ReadAndSetToken() || (idx = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(idx), 0, 0);

                if (CurrentToken == TokenType.GLOBAL)
                {
                    AddBasicOpInstr(OPC.GET);
                }
            }

            if (Expect(TokenType.ROUNDOPEN) == null || !ExFuncCreate(idx))
            {
                return false;
            }

            FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            FunctionState.PopTarget();

            return true;
        }

        public bool ProcessClassStatement()
        {
            ExEState ex;
            if (!ReadAndSetToken())
            {
                return false;
            }
            ex = ExpressionState.Copy();

            ExpressionState.ShouldStopDeref = true;

            if (!ExPrefixed())
            {
                return false;
            }

            if (ExpressionState.Type == ExEType.EXPRESSION)
            {
                AddToErrorMessage("invalid class name");
                return false;
            }
            else if (ExpressionState.Type == ExEType.OBJECT || ExpressionState.Type == ExEType.BASE)
            {
                if (!ExClassResolveExp())
                {
                    return false;
                }
                AddBasicDerefInstr(OPC.NEWSLOT);
                FunctionState.PopTarget();
            }
            else
            {
                AddToErrorMessage("can't create a class as local");
                return false;
            }
            ExpressionState = ex;
            return true;
        }

        public bool ExInvokeExp(string ex)
        {
            ExEState eState = ExpressionState.Copy();
            ExpressionState.Type = ExEType.EXPRESSION;
            ExpressionState.Position = -1;
            ExpressionState.ShouldStopDeref = false;

            if (!(bool)Type.GetType("ExMat.Compiler.ExCompiler").GetMethod(ex).Invoke(this, null))
            {
                return false;
            }

            ExpressionState = eState;
            return true;
        }

        public bool ExBinaryExp(OPC op, string func, int lastop = 0)
        {
            if (!ReadAndSetToken() || !ExInvokeExp(func))
            {
                return false;
            }

            int arg1 = FunctionState.PopTarget();
            int arg2 = FunctionState.PopTarget();

            FunctionState.AddInstr(op, FunctionState.PushTarget(), arg1, arg2, lastop);
            return true;
        }

        public bool ExExp()
        {
            ExEState estate = ExpressionState.Copy();
            ExpressionState.Type = ExEType.EXPRESSION;
            ExpressionState.Position = -1;
            ExpressionState.ShouldStopDeref = false;

            if (!ExLogicOr())
            {
                return false;
            }

            switch (CurrentToken)
            {
                case TokenType.ASG:
                case TokenType.ADDEQ:
                case TokenType.SUBEQ:
                case TokenType.DIVEQ:
                case TokenType.MLTEQ:
                case TokenType.MODEQ:
                case TokenType.NEWSLOT:
                    {
                        TokenType op = CurrentToken;
                        ExEType etyp = ExpressionState.Type;
                        int pos = ExpressionState.Position;

                        if (etyp == ExEType.EXPRESSION)
                        {
                            AddToErrorMessage("can't assing an expression");
                            return false;
                        }
                        else if (etyp == ExEType.BASE)
                        {
                            AddToErrorMessage("can't modify 'base'");
                            return false;
                        }

                        if (!ReadAndSetToken() || !ExExp())
                        {
                            return false;
                        }

                        switch (op)
                        {
                            case TokenType.NEWSLOT:
                                {
                                    if (etyp == ExEType.OBJECT || etyp == ExEType.BASE)
                                    {
                                        AddBasicDerefInstr(OPC.NEWSLOT);
                                    }
                                    else
                                    {
                                        AddToErrorMessage("can't create a local slot");
                                        return false;
                                    }
                                    break;
                                }
                            case TokenType.ASG:
                                {
                                    switch (etyp)
                                    {
                                        case ExEType.VAR:
                                            {
                                                int s = FunctionState.PopTarget();
                                                int d = FunctionState.TopTarget();
                                                FunctionState.AddInstr(OPC.MOVE, d, s, 0, 0);
                                                break;
                                            }
                                        case ExEType.OBJECT:
                                        case ExEType.BASE:
                                            {
                                                AddBasicDerefInstr(OPC.SET);
                                                break;
                                            }
                                        case ExEType.OUTER:
                                            {
                                                int s = FunctionState.PopTarget();
                                                int d = FunctionState.PushTarget();
                                                FunctionState.AddInstr(OPC.SETOUTER, d, pos, s, 0);
                                                break;
                                            }
                                    }
                                    break;
                                }
                            case TokenType.ADDEQ:
                            case TokenType.SUBEQ:
                            case TokenType.MLTEQ:
                            case TokenType.DIVEQ:
                            case TokenType.MODEQ:
                                {
                                    AddCompoundOpInstr(op, etyp, pos);
                                    break;
                                }
                        }
                        break;
                    }
                case TokenType.QMARK:
                    {
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }

                        FunctionState.AddInstr(OPC.JZ, FunctionState.PopTarget(), 0, 0, 0);

                        int jzp = FunctionState.GetCurrPos();
                        int t = FunctionState.PushTarget();

                        if (!ExExp())
                        {
                            return false;
                        }

                        int f = FunctionState.PopTarget();
                        if (t != f)
                        {
                            FunctionState.AddInstr(OPC.MOVE, t, f, 0, 0);
                        }
                        int end_f = FunctionState.GetCurrPos();

                        FunctionState.AddInstr(OPC.JMP, 0, 0, 0, 0);
                        if (Expect(TokenType.COL) == null)
                        {
                            return false;
                        }

                        int jmp = FunctionState.GetCurrPos();

                        if (!ExExp())
                        {
                            return false;
                        }

                        int s = FunctionState.PopTarget();
                        if (t != s)
                        {
                            FunctionState.AddInstr(OPC.MOVE, t, s, 0, 0);
                        }

                        FunctionState.SetInstrParam(jmp, 1, FunctionState.GetCurrPos() - jmp);
                        FunctionState.SetInstrParam(jzp, 1, end_f - jzp + 1);
                        FunctionState.NotSnoozed = false;

                        break;
                    }
            }
            ExpressionState = estate;
            return true;
        }

        public bool ExLogicOr()
        {
            if (!ExLogicAnd())
            {
                return false;
            }
            for (; ; )
            {
                switch (CurrentToken)
                {
                    case TokenType.OR:
                        {
                            int f = FunctionState.PopTarget();
                            int t = FunctionState.PushTarget();

                            FunctionState.AddInstr(OPC.OR, t, 0, f, 0);

                            int j = FunctionState.GetCurrPos();

                            if (t != f)
                            {
                                FunctionState.AddInstr(OPC.MOVE, t, f, 0, 0);
                            }

                            if (!ReadAndSetToken() || !ExInvokeExp("ExLogicOr"))
                            {
                                return false;
                            }

                            FunctionState.NotSnoozed = false;

                            int s = FunctionState.PopTarget();

                            if (t != s)
                            {
                                FunctionState.AddInstr(OPC.MOVE, t, s, 0, 0);
                            }

                            FunctionState.NotSnoozed = false;
                            FunctionState.SetInstrParam(j, 1, FunctionState.GetCurrPos() - j);
                            break;
                        }
                    default:
                        return true;
                }
            }
        }
        public bool ExLogicAnd()
        {
            if (!ExLogicBOr())
            {
                return false;
            }
            for (; ; )
            {
                switch (CurrentToken)
                {
                    case TokenType.AND:
                        {
                            int f = FunctionState.PopTarget();
                            int t = FunctionState.PushTarget();

                            FunctionState.AddInstr(OPC.AND, t, 0, f, 0);

                            int j = FunctionState.GetCurrPos();

                            if (t != f)
                            {
                                FunctionState.AddInstr(OPC.MOVE, t, f, 0, 0);
                            }
                            if (!ReadAndSetToken() || !ExInvokeExp("ExLogicAnd"))
                            {
                                return false;
                            }

                            FunctionState.NotSnoozed = false;

                            int s = FunctionState.PopTarget();

                            if (t != s)
                            {
                                FunctionState.AddInstr(OPC.MOVE, t, s, 0, 0);
                            }

                            FunctionState.NotSnoozed = false;
                            FunctionState.SetInstrParam(j, 1, FunctionState.GetCurrPos() - j);
                            break;
                        }
                    default:
                        return true;
                }
            }
        }
        public bool ExLogicBOr()
        {
            if (!ExLogicBXor())
            {
                return false;
            }
            for (; ; )
            {
                switch (CurrentToken)
                {
                    case TokenType.BOR:
                        {
                            if (!ExBinaryExp(OPC.BITWISE, "ExLogicBXor", (int)BitOP.OR))
                            {
                                return false;
                            }
                            break;
                        }
                    default:
                        return true;
                }
            }
        }
        public bool ExLogicBXor()
        {
            if (!ExLogicBAnd())
            {
                return false;
            }
            for (; ; )
            {
                switch (CurrentToken)
                {
                    case TokenType.BXOR:
                        {
                            if (!ExBinaryExp(OPC.BITWISE, "ExLogicBAnd", (int)BitOP.XOR))
                            {
                                return false;
                            }
                            break;
                        }
                    default:
                        return true;
                }
            }
        }
        public bool ExLogicBAnd()
        {
            if (!ExLogicEq())
            {
                return false;
            }
            for (; ; )
            {
                switch (CurrentToken)
                {
                    case TokenType.BAND:
                        {
                            if (!ExBinaryExp(OPC.BITWISE, "ExLogicEq", (int)BitOP.AND))
                            {
                                return false;
                            }
                            break;
                        }
                    default:
                        return true;
                }
            }
        }
        public bool ExLogicEq()
        {
            if (!ExLogicCmp())
            {
                return false;
            }
            for (; ; )
            {
                switch (CurrentToken)
                {
                    case TokenType.EQU:
                        {
                            if (!ExBinaryExp(OPC.EQ, "ExLogicCmp"))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.NEQ:
                        {
                            if (!ExBinaryExp(OPC.NEQ, "ExLogicCmp"))
                            {
                                return false;
                            }
                            break;
                        }
                    default:
                        return true;
                }
            }
        }
        public bool ExLogicCmp()
        {
            if (!ExLogicShift())
            {
                return false;
            }
            for (; ; )
            {
                switch (CurrentToken)
                {
                    case TokenType.GRT:
                        {
                            if (!ExBinaryExp(OPC.CMP, "ExLogicShift", (int)CmpOP.GRT))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.LST:
                        {
                            if (!ExBinaryExp(OPC.CMP, "ExLogicShift", (int)CmpOP.LST))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.GET:
                        {
                            if (!ExBinaryExp(OPC.CMP, "ExLogicShift", (int)CmpOP.GET))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.LET:
                        {
                            if (!ExBinaryExp(OPC.CMP, "ExLogicShift", (int)CmpOP.LET))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.NOTIN:
                    case TokenType.IN:
                        {
                            if (!ExBinaryExp(OPC.EXISTS, "ExLogicShift", CurrentToken == TokenType.NOTIN ? 1 : 0))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.INSTANCEOF:
                        {
                            if (!ExBinaryExp(OPC.INSTANCEOF, "ExLogicShift"))
                            {
                                return false;
                            }
                            break;
                        }
                    default:
                        return true;
                }
            }
        }
        public bool ExLogicShift()
        {
            if (!ExLogicAdd())
            {
                return false;
            }
            for (; ; )
            {
                switch (CurrentToken)
                {
                    case TokenType.LSHF:
                        {
                            if (!ExBinaryExp(OPC.BITWISE, "ExLogicAdd", (int)BitOP.SHIFTL))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.RSHF:
                        {
                            if (!ExBinaryExp(OPC.BITWISE, "ExLogicAdd", (int)BitOP.SHIFTR))
                            {
                                return false;
                            }
                            break;
                        }
                    default:
                        return true;
                }
            }
        }
        public bool ExLogicAdd()
        {
            if (!ExLogicMlt())
            {
                return false;
            }
            for (; ; )
            {
                switch (CurrentToken)
                {
                    case TokenType.ADD:
                    case TokenType.SUB:
                        {
                            if (!ExBinaryExp(ExOPDecideArithmetic(CurrentToken), "ExLogicMlt"))
                            {
                                return false;
                            }
                            break;
                        }
                    default:
                        return true;
                }
            }
        }
        public bool ExLogicMlt()
        {
            if (!ExPrefixed())
            {
                return false;
            }

            for (; ; )
            {
                switch (CurrentToken)
                {
                    case TokenType.CARTESIAN:
                    case TokenType.EXP:
                    case TokenType.MLT:
                    case TokenType.MATMLT:
                    case TokenType.DIV:
                    case TokenType.MOD:
                        {
                            if (!ExBinaryExp(ExOPDecideArithmetic(CurrentToken), "ExPrefixed"))
                            {
                                return false;
                            }
                            break;
                        }
                    default:
                        return true;
                }
            }
        }

        public static OPC ExOPDecideArithmetic(TokenType typ)
        {
            switch (typ)
            {
                case TokenType.EXP:
                    return OPC.EXP;
                case TokenType.SUB:
                case TokenType.SUBEQ:
                    return OPC.SUB;
                case TokenType.MLT:
                case TokenType.MLTEQ:
                    return OPC.MLT;
                case TokenType.DIV:
                case TokenType.DIVEQ:
                    return OPC.DIV;
                case TokenType.MOD:
                case TokenType.MODEQ:
                    return OPC.MOD;
                case TokenType.MATMLT:
                    return OPC.MMLT;
                case TokenType.CARTESIAN:
                    return OPC.CARTESIAN;
                default:
                    return OPC.ADD;
            }
        }

        public static int ExOPDecideArithmeticInt(TokenType typ)
        {
            switch (typ)
            {
                case TokenType.ADDEQ:
                    return '+';
                case TokenType.SUBEQ:
                    return '-';
                case TokenType.MLTEQ:
                    return '*';
                case TokenType.DIVEQ:
                    return '/';
                case TokenType.MODEQ:
                    return '%';
                default:
                    return 0;
            }
        }

        public bool ExPrefixed()
        {
            return ExPrefixedInner(false);
        }

        public bool ExPrefixedInner(bool macro)
        {
            int p = -1;
            if (!ExFactor(ref p, ref macro))
            {
                return false;
            }
            for (; ; )
            {
                switch (CurrentToken)
                {
                    case TokenType.DOT:
                        {
                            p = -1;
                            if (!ReadAndSetToken())
                            {
                                return false;
                            }

                            ExObject tmp;
                            if ((tmp = Expect(TokenType.IDENTIFIER)) == null)
                            {
                                return false;
                            }

                            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(tmp), 0, 0);

                            if (ExpressionState.Type == ExEType.BASE)
                            {
                                AddBasicOpInstr(OPC.GET);
                                p = FunctionState.TopTarget();
                                ExpressionState.Type = ExEType.EXPRESSION;
                                ExpressionState.Position = p;
                            }
                            else
                            {
                                if (ExRequiresGetter())
                                {
                                    AddBasicOpInstr(OPC.GET);
                                }
                                ExpressionState.Type = ExEType.OBJECT;
                            }
                            break;
                        }
                    case TokenType.SQUAREOPEN:
                        {
                            if (Lexer.TokenPrev == TokenType.NEWLINE)
                            {
                                AddToErrorMessage("can't break deref OR ',' needed after [exp] = exp decl");
                                return false;
                            }
                            if (!ReadAndSetToken() || !ExExp() || Expect(TokenType.SQUARECLOSE) == null)
                            {
                                return false;
                            }

                            if (ExpressionState.Type == ExEType.BASE)
                            {
                                AddBasicOpInstr(OPC.GET);
                                p = FunctionState.TopTarget();
                                ExpressionState.Type = ExEType.EXPRESSION;
                                ExpressionState.Position = p;
                            }
                            else
                            {
                                if (ExRequiresGetter())
                                {
                                    AddBasicOpInstr(OPC.GET);
                                }
                                ExpressionState.Type = ExEType.OBJECT;
                            }
                            break;
                        }
                    case TokenType.MATTRANSPOSE:
                        {
                            if (!ReadAndSetToken())
                            {
                                return false;
                            }

                            switch (ExpressionState.Type)
                            {
                                case ExEType.EXPRESSION:
                                //{
                                //    AddToErrorMessage("can't get transpose of an expression");
                                //    return false;
                                //}
                                case ExEType.OBJECT:
                                case ExEType.BASE:
                                case ExEType.VAR:
                                    {
                                        int s = FunctionState.PopTarget();
                                        FunctionState.AddInstr(OPC.TRANSPOSE, FunctionState.PushTarget(), s, 0, 0);
                                        break;
                                    }
                                case ExEType.OUTER:
                                    {
                                        int t1 = FunctionState.PushTarget();
                                        int t2 = FunctionState.PushTarget();
                                        FunctionState.AddInstr(OPC.GETOUTER, t2, ExpressionState.Position, 0, 0);
                                        FunctionState.AddInstr(OPC.TRANSPOSE, t1, t2, 0, 0);
                                        FunctionState.PopTarget();
                                        break;
                                    }
                            }
                            return true;
                        }
                    case TokenType.INC:
                    case TokenType.DEC:
                        {
                            if (IsEOS())
                            {
                                return true;
                            }
                            int v = CurrentToken == TokenType.DEC ? -1 : 1;

                            if (!ReadAndSetToken())
                            {
                                return false;
                            }

                            switch (ExpressionState.Type)
                            {
                                case ExEType.EXPRESSION:
                                    {
                                        AddToErrorMessage("can't increment or decrement an expression");
                                        return false;
                                    }
                                case ExEType.OBJECT:
                                case ExEType.BASE:
                                    {
                                        AddBasicOpInstr(OPC.PINC, v);
                                        break;
                                    }
                                case ExEType.VAR:
                                    {
                                        int s = FunctionState.PopTarget();
                                        FunctionState.AddInstr(OPC.PINCL, FunctionState.PushTarget(), s, 0, v);
                                        break;
                                    }
                                case ExEType.OUTER:
                                    {
                                        int t1 = FunctionState.PushTarget();
                                        int t2 = FunctionState.PushTarget();
                                        FunctionState.AddInstr(OPC.GETOUTER, t2, ExpressionState.Position, 0, 0);
                                        FunctionState.AddInstr(OPC.PINCL, t1, t2, 0, v);
                                        FunctionState.AddInstr(OPC.SETOUTER, t2, ExpressionState.Position, t2, 0);
                                        FunctionState.PopTarget();
                                        break;
                                    }
                            }
                            return true;
                        }
                    case TokenType.ROUNDOPEN:
                        {
                            switch (ExpressionState.Type)
                            {
                                case ExEType.OBJECT:
                                    {
                                        int k_loc = FunctionState.PopTarget();
                                        int obj_loc = FunctionState.PopTarget();
                                        int closure = FunctionState.PushTarget();
                                        int target = FunctionState.PushTarget();
                                        FunctionState.AddInstr(OPC.PREPCALL, closure, k_loc, obj_loc, target);
                                        break;
                                    }
                                case ExEType.BASE:
                                    {
                                        FunctionState.AddInstr(OPC.MOVE, FunctionState.PushTarget(), 0, 0, 0);
                                        break;
                                    }
                                case ExEType.OUTER:
                                    {
                                        FunctionState.AddInstr(OPC.GETOUTER, FunctionState.PushTarget(), ExpressionState.Position, 0, 0);
                                        FunctionState.AddInstr(OPC.MOVE, FunctionState.PushTarget(), 0, 0, 0);
                                        break;
                                    }
                                default:
                                    {
                                        FunctionState.AddInstr(OPC.MOVE, FunctionState.PushTarget(), 0, 0, 0);
                                        break;
                                    }
                            }
                            ExpressionState.Type = ExEType.EXPRESSION;
                            if (!ReadAndSetToken() || !ExFuncCall())
                            {
                                return false;
                            }
                            break;
                        }
                    default:
                        {
                            if (macro)
                            {
                                int k_loc = FunctionState.PopTarget();
                                int obj_loc = FunctionState.PopTarget();
                                int closure = FunctionState.PushTarget();
                                int target = FunctionState.PushTarget();

                                FunctionState.AddInstr(OPC.PREPCALL, closure, k_loc, obj_loc, target);

                                ExpressionState.Type = ExEType.EXPRESSION;

                                int st = FunctionState.PopTarget();
                                int cl = FunctionState.PopTarget();

                                FunctionState.AddInstr(OPC.CALL, FunctionState.PushTarget(), cl, st, 1);
                            }
                            return true;
                        }
                }
            }
        }

        public bool ExFactor(ref int pos, ref bool macro)
        {
            ExpressionState.Type = ExEType.EXPRESSION;

            switch (CurrentToken)
            {
                case TokenType.LITERAL:
                    {
                        FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(FunctionState.CreateString(Lexer.ValString)), 0, 0);

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.BASE:
                    {
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        FunctionState.AddInstr(OPC.GETBASE, FunctionState.PushTarget(), 0, 0, 0);

                        ExpressionState.Type = ExEType.BASE;
                        ExpressionState.Position = FunctionState.TopTarget();
                        pos = ExpressionState.Position;
                        return true;
                    }
                case TokenType.IDENTIFIER:
                case TokenType.CONSTRUCTOR:
                case TokenType.THIS:
                    {
                        ExObject idx = new();
                        ExObject c = new();
                        switch (CurrentToken)
                        {
                            case TokenType.IDENTIFIER:
                                {
                                    idx = FunctionState.CreateString(Lexer.ValString);
                                    break;
                                }
                            case TokenType.THIS:
                                {
                                    idx = FunctionState.CreateString(ExMat.ThisName);
                                    break;
                                }
                            case TokenType.CONSTRUCTOR:
                                {
                                    idx = FunctionState.CreateString(ExMat.ConstructorName);
                                    break;
                                }
                        }

                        int p;
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        #region _
                        /*
                        if (FunctionState.IsBlockMacro(idx.GetString()))
                        {
                            FunctionState.PushTarget(0);
                            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(idx), 0, 0);
                            //macro = true;
                            ExpressionState.Type = ExEType.OBJECT;
                        }
                        else if (FunctionState.IsMacro(idx) && !FunctionState.IsFuncMacro(idx))
                        {
                            FunctionState.PushTarget(0);
                            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(idx), 0, 0);
                            macro = true;
                            ExpressionState.Type = ExEType.OBJECT;
                        }
                        */
                        #endregion
                        if ((p = FunctionState.GetLocal(idx)) != -1)
                        {
                            FunctionState.PushTarget(p);
                            ExpressionState.Type = ExEType.VAR;
                            ExpressionState.Position = p;
                        }
                        else if ((p = FunctionState.GetOuter(idx)) != -1)
                        {
                            if (ExRequiresGetter())
                            {
                                ExpressionState.Position = FunctionState.PushTarget();
                                FunctionState.AddInstr(OPC.GETOUTER, ExpressionState.Position, p, 0, 0);
                            }
                            else
                            {
                                ExpressionState.Type = ExEType.OUTER;
                                ExpressionState.Position = p;
                            }
                        }
                        else
                        {
                            FunctionState.PushTarget(0);
                            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(idx), 0, 0);

                            if (ExRequiresGetter())
                            {
                                AddBasicOpInstr(OPC.GET);
                            }
                            ExpressionState.Type = ExEType.OBJECT;
                        }
                        pos = ExpressionState.Position;
                        return true;
                    }
                case TokenType.GLOBAL:
                    {
                        FunctionState.AddInstr(OPC.LOADROOT, FunctionState.PushTarget(), 0, 0, 0);
                        ExpressionState.Type = ExEType.OBJECT;
                        CurrentToken = TokenType.DOT;
                        ExpressionState.Position = -1;
                        pos = ExpressionState.Position;
                        return true;
                    }
                case TokenType.DEFAULT:
                case TokenType.NULL:
                    {
                        FunctionState.AddInstr(OPC.LOADNULL, FunctionState.PushTarget(), 1, CurrentToken == TokenType.DEFAULT ? 1 : 0, 0);
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                #region _
                /*
            case TokenType.MACROPARAMNUM:
            case TokenType.MACROPARAMSTR:
                {
                    FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(FunctionState.CreateString(Lexer.ValString)), 0, 0);

                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                    break;
                }
                */
                #endregion
                case TokenType.INTEGER:
                    {
                        AddIntConstLoadInstr(Lexer.ValInteger, -1);
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.FLOAT:
                    {
                        AddFloatConstLoadInstr(new DoubleLong() { f = Lexer.ValFloat }.i, -1);

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.COMPLEX:
                    {
                        if (Lexer.TokenComplex == TokenType.INTEGER)
                        {
                            AddComplexConstLoadInstr(Lexer.ValInteger, -1);
                        }
                        else
                        {
                            AddComplexConstLoadInstr(new DoubleLong() { f = Lexer.ValFloat }.i, -1, true);
                        }

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.TRUE:
                case TokenType.FALSE:
                    {
                        FunctionState.AddInstr(OPC.LOADBOOLEAN, FunctionState.PushTarget(), CurrentToken == TokenType.TRUE ? 1 : 0, 0, 0);

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.SPACE:
                    {
                        AddSpaceConstLoadInstr(Lexer.ValSpace, -1);
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.SQUAREOPEN:
                    {
                        FunctionState.AddInstr(OPC.NEWOBJECT, FunctionState.PushTarget(), 0, 0, (int)ExNOT.ARRAY);
                        int p = FunctionState.GetCurrPos();
                        int k = 0;

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        while (CurrentToken != TokenType.SQUARECLOSE)
                        {
                            if (!ExExp())
                            {
                                return false;
                            }
                            if (CurrentToken == TokenType.SEP)
                            {
                                if (!ReadAndSetToken())
                                {
                                    return false;
                                }
                            }
                            int v = FunctionState.PopTarget();
                            int a = FunctionState.TopTarget();
                            FunctionState.AddInstr(OPC.APPENDTOARRAY, a, v, (int)ArrayAType.STACK, 0);
                            k++;
                        }
                        FunctionState.SetInstrParam(p, 1, k);
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.CURLYOPEN:
                    {
                        FunctionState.AddInstr(OPC.NEWOBJECT, FunctionState.PushTarget(), 0, (int)ExNOT.DICT, 0);

                        if (!ReadAndSetToken() || !ParseDictClusterOrClass(TokenType.SEP, TokenType.CURLYCLOSE))
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.LAMBDA:
                case TokenType.FUNCTION:
                    {
                        if (!ExFuncResolveExp(CurrentToken))
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.RULE:
                    {
                        if (!ExRuleResolveExp())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.CLUSTER:
                    {
                        if (!ExClusterResolveExp())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.SEQUENCE:
                    {
                        if (!ExSequenceResolveExp())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.CLASS:
                    {
                        if (!ReadAndSetToken() || !ExClassResolveExp())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.SUB:
                    {
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        switch (CurrentToken)
                        {
                            case TokenType.INTEGER:
                                {
                                    AddIntConstLoadInstr(-Lexer.ValInteger, -1);
                                    if (!ReadAndSetToken())
                                    {
                                        return false;
                                    }
                                    break;
                                }
                            case TokenType.FLOAT:
                                {
                                    AddFloatConstLoadInstr(new DoubleLong() { f = -Lexer.ValFloat }.i, -1);
                                    if (!ReadAndSetToken())
                                    {
                                        return false;
                                    }
                                    break;
                                }
                            case TokenType.COMPLEX:
                                {
                                    if (Lexer.TokenComplex == TokenType.INTEGER)
                                    {
                                        AddComplexConstLoadInstr(Lexer.ValInteger, -1);
                                    }
                                    else
                                    {
                                        AddComplexConstLoadInstr(new DoubleLong() { f = Lexer.ValFloat }.i, -1, true);
                                    }

                                    if (!ReadAndSetToken())
                                    {
                                        return false;
                                    }
                                    break;
                                }
                            default:
                                {
                                    if (!ExOpUnary(OPC.NEGATE))
                                    {
                                        return false;
                                    }
                                    break;
                                }
                        }
                        break;
                    }
                case TokenType.EXC:
                    {
                        if (!ReadAndSetToken() || !ExOpUnary(OPC.NOT))
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.BNOT:
                    {
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        if (CurrentToken == TokenType.INTEGER)
                        {
                            AddIntConstLoadInstr(~Lexer.ValInteger, -1);
                            if (!ReadAndSetToken())
                            {
                                return false;
                            }
                            break;
                        }
                        if (!ExOpUnary(OPC.BNOT))
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.TYPEOF:
                    {
                        if (!ReadAndSetToken() || !ExOpUnary(OPC.TYPEOF))
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.INC:
                case TokenType.DEC:
                    {
                        if (!ExPrefixedIncDec(CurrentToken))
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.ROUNDOPEN:
                    {
                        if (!ReadAndSetToken()
                            || !ExSepExp()
                            || Expect(TokenType.ROUNDCLOSE) == null)
                        {
                            return false;
                        }

                        break;
                    }
                case TokenType.DELETE:
                    {
                        if (!ExDeleteExp())
                        {
                            return false;
                        }
                        break;
                    }
                #region _
                /*
            case TokenType.MACROSTART:
                {
                    AddToErrorMessage("macros can only be defined on new lines");
                    return false;
                }
                */
                #endregion
                default:
                    {
                        AddToErrorMessage("expression expected");
                        return false;
                    }
            }
            pos = -1;
            return true;
        }

        public bool ExDeleteExp()
        {
            ExEState es;

            if (!ReadAndSetToken())
            {
                return false;
            }

            es = ExpressionState.Copy();
            ExpressionState.ShouldStopDeref = true;

            if (!ExPrefixed())
            {
                return false;
            }

            if (ExpressionState.Type == ExEType.EXPRESSION)
            {
                AddToErrorMessage("cant 'delete' and expression");
                return false;
            }

            if (ExpressionState.Type == ExEType.OBJECT || ExpressionState.Type == ExEType.BASE)
            {
                AddBasicOpInstr(OPC.DELETE);
            }
            else
            {
                AddToErrorMessage("can't delete an outer local variable");
                return false;
            }

            ExpressionState = es;
            return true;
        }

        public bool ExRequiresGetter()
        {
            switch (CurrentToken)
            {
                case TokenType.ROUNDOPEN:
                case TokenType.ASG:
                case TokenType.ADDEQ:
                case TokenType.SUBEQ:
                case TokenType.MLTEQ:
                case TokenType.DIVEQ:
                case TokenType.MODEQ:
                case TokenType.INC:
                case TokenType.DEC:
                case TokenType.NEWSLOT:
                    {
                        return false;
                    }
            }
            return !ExpressionState.ShouldStopDeref
                   || (ExpressionState.ShouldStopDeref
                        && (CurrentToken == TokenType.DOT || CurrentToken == TokenType.SQUAREOPEN)
                      );
        }

        public bool ExPrefixedIncDec(TokenType typ)
        {
            ExEState eState;
            int v = typ == TokenType.DEC ? -1 : 1;

            if (!ReadAndSetToken())
            {
                return false;
            }

            eState = ExpressionState.Copy();
            ExpressionState.ShouldStopDeref = true;

            if (!ExPrefixed())
            {
                return false;
            }

            switch (ExpressionState.Type)
            {
                case ExEType.EXPRESSION:
                    {
                        AddToErrorMessage("can't increment or decrement an expression!");
                        return false;
                    }
                case ExEType.OBJECT:
                case ExEType.BASE:
                    {
                        AddBasicOpInstr(OPC.INC, v);
                        break;
                    }
                case ExEType.VAR:
                    {
                        int s = FunctionState.TopTarget();
                        FunctionState.AddInstr(OPC.INCL, s, s, 0, v);
                        break;
                    }
                case ExEType.OUTER:
                    {
                        int tmp = FunctionState.PushTarget();
                        FunctionState.AddInstr(OPC.GETOUTER, tmp, ExpressionState.Position, 0, 0);
                        FunctionState.AddInstr(OPC.INCL, tmp, tmp, 0, ExpressionState.Position);
                        FunctionState.AddInstr(OPC.SETOUTER, tmp, ExpressionState.Position, 0, tmp);
                        break;
                    }
            }
            ExpressionState = eState;
            return true;
        }

        public bool ExSepExp()
        {
            while (true)
            {
                if (!ExExp())
                {
                    return false;
                }
                if (CurrentToken != TokenType.SEP)
                {
                    break;
                }

                FunctionState.PopTarget();

                if (!ReadAndSetToken() || !ExSepExp())
                {
                    return false;
                }
            }
            return true;
        }

        public void AddSpaceConstLoadInstr(ExSpace s, int p)
        {
            if (p < 0)
            {
                p = FunctionState.PushTarget();
            }
            ExObject tspc = new() { Type = ExObjType.SPACE };
            tspc.Value.c_Space = Lexer.ValSpace;
            FunctionState.AddInstr(OPC.LOADSPACE, p, (int)FunctionState.GetConst(tspc), 0, 0);
        }

        public void AddIntConstLoadInstr(long cval, int p)
        {
            if (p < 0)
            {
                p = FunctionState.PushTarget();
            }
            FunctionState.AddInstr(OPC.LOADINTEGER, p, cval, 0, 0);
        }
        public void AddFloatConstLoadInstr(long cval, int p)
        {
            if (p < 0)
            {
                p = FunctionState.PushTarget();
            }

            FunctionState.AddInstr(OPC.LOADFLOAT, p, cval, 0, 0);
        }

        public void AddComplexConstLoadInstr(long cval, int p, bool isfloat = false)
        {
            if (p < 0)
            {
                p = FunctionState.PushTarget();
            }

            FunctionState.AddInstr(OPC.LOADCOMPLEX, p, cval, isfloat ? 1 : 0, 0);
        }

        public void AddBasicDerefInstr(OPC op)
        {
            int v = FunctionState.PopTarget();
            int k = FunctionState.PopTarget();
            int s = FunctionState.PopTarget();
            FunctionState.AddInstr(op, FunctionState.PushTarget(), s, k, v);
        }

        public void AddBasicOpInstr(OPC op, int last = 0)
        {
            int arg2 = FunctionState.PopTarget();
            int arg1 = FunctionState.PopTarget();
            FunctionState.AddInstr(op, FunctionState.PushTarget(), arg1, arg2, last);
        }

        public void AddCompoundOpInstr(TokenType typ, ExEType etyp, int pos)
        {
            switch (etyp)
            {
                case ExEType.VAR:
                    {
                        int s = FunctionState.PopTarget();
                        int d = FunctionState.PopTarget();
                        FunctionState.PushTarget(d);

                        FunctionState.AddInstr(ExOPDecideArithmetic(typ), d, s, d, 0);
                        FunctionState.NotSnoozed = false;
                        break;
                    }
                case ExEType.BASE:
                case ExEType.OBJECT:
                    {
                        int v = FunctionState.PopTarget();
                        int k = FunctionState.PopTarget();
                        int s = FunctionState.PopTarget();
                        FunctionState.AddInstr(OPC.COMPOUNDARITH, FunctionState.PushTarget(), (s << 16) | v, k, ExOPDecideArithmeticInt(typ));
                        break;
                    }
                case ExEType.OUTER:
                    {
                        int v = FunctionState.TopTarget();
                        int tmp = FunctionState.PushTarget();

                        FunctionState.AddInstr(OPC.GETOUTER, tmp, pos, 0, 0);
                        FunctionState.AddInstr(ExOPDecideArithmetic(typ), tmp, v, tmp, 0);
                        FunctionState.AddInstr(OPC.SETOUTER, tmp, pos, tmp, 0);

                        break;
                    }
            }
        }

        public bool ParseDictClusterOrClass(TokenType sep, TokenType end)
        {
            int p = FunctionState.GetCurrPos();
            int n = 0;

            while (CurrentToken != end)
            {
                bool a_present = false;
                if (sep == TokenType.SMC)
                {
                    if (CurrentToken == TokenType.ATTRIBUTEBEGIN)
                    {
                        FunctionState.AddInstr(OPC.NEWOBJECT, FunctionState.PushTarget(), 0, (int)ExNOT.DICT, 0);

                        if (!ReadAndSetToken() || !ParseDictClusterOrClass(TokenType.SEP, TokenType.ATTRIBUTEFINISH))
                        {
                            AddToErrorMessage("failed to parse attribute");
                            return false;
                        }
                        a_present = true;
                    }
                }
                switch (CurrentToken)
                {
                    case TokenType.FUNCTION:
                    case TokenType.CONSTRUCTOR:
                        {
                            TokenType typ = CurrentToken;
                            if (!ReadAndSetToken())
                            {
                                return false;
                            }

                            ExObject o = typ == TokenType.FUNCTION ? Expect(TokenType.IDENTIFIER) : FunctionState.CreateString(ExMat.ConstructorName);

                            if (o == null || Expect(TokenType.ROUNDOPEN) == null)
                            {
                                return false;
                            }

                            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(o), 0, 0);

                            if (!ExFuncCreate(o))
                            {
                                return false;
                            }

                            FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);
                            break;
                        }
                    case TokenType.SQUAREOPEN:
                        {
                            if (!ReadAndSetToken()
                                || !ExSepExp()
                                || Expect(TokenType.SQUARECLOSE) == null
                                || Expect(TokenType.ASG) == null
                                || !ExExp())
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.LITERAL:
                        {
                            if (sep == TokenType.SEP)
                            {
                                ExObject o;
                                if ((o = Expect(TokenType.LITERAL)) == null)
                                {
                                    return false;
                                }
                                FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(o), 0, 0);

                                if (Expect(TokenType.COL) == null || !ExExp())
                                {
                                    return false;
                                }

                                break;
                            }
                            goto default;
                        }
                    default:
                        {
                            ExObject o;
                            if ((o = Expect(TokenType.IDENTIFIER)) == null)
                            {
                                return false;
                            }

                            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(o), 0, 0);

                            if (Expect(TokenType.ASG) == null || !ExExp())
                            {
                                return false;
                            }

                            break;
                        }
                }

                if (CurrentToken == sep)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                n++;

                int v = FunctionState.PopTarget();
                int k = FunctionState.PopTarget();
                int a = a_present ? FunctionState.PopTarget() : -1;

                if (!((a_present && (a == k - 1)) || !a_present))
                {
                    throw new Exception("attributes present count error");
                }

                int flg = a_present ? (int)ExNewSlotFlag.ATTR : 0; // to-do static flag
                int t = FunctionState.TopTarget();
                if (sep == TokenType.SEP)
                {
                    FunctionState.AddInstr(OPC.NEWSLOT, 985, t, k, v);
                }
                else
                {
                    FunctionState.AddInstr(OPC.NEWSLOTA, flg, t, k, v);
                }
            }

            if (sep == TokenType.SEP)
            {
                FunctionState.SetInstrParam(p, 1, n);
            }

            return ReadAndSetToken();
        }

        public bool ExFuncCall()
        {
            int _this = 1;

            while (CurrentToken != TokenType.ROUNDCLOSE)
            {
                if (!ExExp())
                {
                    return false;
                }
                TargetLocalMove();
                _this++;
                if (CurrentToken == TokenType.SEP)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                    if (CurrentToken == TokenType.ROUNDCLOSE)
                    {
                        AddToErrorMessage("expression expected, found ')'");
                        return false;
                    }
                }
            }
            if (!ReadAndSetToken())
            {
                return false;
            }

            for (int i = 0; i < (_this - 1); i++)
            {
                FunctionState.PopTarget();
            }

            int st = FunctionState.PopTarget();
            int cl = FunctionState.PopTarget();

            FunctionState.AddInstr(OPC.CALL, FunctionState.PushTarget(), cl, st, _this);
            return true;
        }

        public void TargetLocalMove()
        {
            int t = FunctionState.TopTarget();
            if (FunctionState.IsLocalArg(t))
            {
                t = FunctionState.PopTarget();
                FunctionState.AddInstr(OPC.MOVE, FunctionState.PushTarget(), t, 0, 0);
            }
        }

        public bool ExSequenceResolveExp()
        {
            AddToErrorMessage("can't create sequences from expressions");
            return false;
        }

        public bool ExClusterResolveExp()
        {
            AddToErrorMessage("can't create clusters from expressions");
            return false;
        }

        public bool ExRuleResolveExp()
        {
            if (!ReadAndSetToken() || Expect(TokenType.ROUNDOPEN) == null)
            {
                return false;
            }

            ExObject d = new();
            if (!ExRuleCreate(d))
            {
                return false;
            }

            FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 1, 0);

            return true;
        }

        public bool ExFuncResolveExp(TokenType typ)
        {
            bool lambda = CurrentToken == TokenType.LAMBDA;
            if (!ReadAndSetToken() || Expect(TokenType.ROUNDOPEN) == null)
            {
                return false;
            }

            ExObject d = new();
            if (!ExFuncCreate(d, lambda))
            {
                return false;
            }

            FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, typ == TokenType.FUNCTION ? 0 : 1, 0);

            return true;
        }
        public bool ExClassResolveExp()
        {
            int at = 985;

            if (CurrentToken == TokenType.ATTRIBUTEBEGIN)
            {
                if (!ReadAndSetToken())
                {
                    return false;
                }
                FunctionState.AddInstr(OPC.NEWOBJECT, FunctionState.PushTarget(), 0, (int)ExNOT.DICT, 0);

                if (!ParseDictClusterOrClass(TokenType.SEP, TokenType.ATTRIBUTEFINISH))
                {
                    return false;
                }

                at = FunctionState.TopTarget();
            }

            if (Expect(TokenType.CURLYOPEN) == null)
            {
                return false;
            }

            if (at != 985)
            {
                FunctionState.PopTarget();
            }

            FunctionState.AddInstr(OPC.NEWOBJECT, FunctionState.PushTarget(), -1, at, (int)ExNOT.CLASS);

            return ParseDictClusterOrClass(TokenType.SMC, TokenType.CURLYCLOSE);
        }

        public bool ExMacroCreate(ExObject o, bool isfunc = false)
        {
            ExFState f_state = FunctionState.PushChildState(VM.SharedState);
            f_state.Name = o;

            ExObject pname;
            f_state.AddParam(FunctionState.CreateString(ExMat.ThisName));
            f_state.Source = new(Source);

            if (isfunc && !ReadAndSetToken())
            {
                return false;
            }

            while (isfunc && CurrentToken != TokenType.ROUNDCLOSE)
            {
                if ((pname = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                f_state.AddParam(pname);

                if (CurrentToken == TokenType.SEP)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                else if (CurrentToken != TokenType.ROUNDCLOSE)
                {
                    if (CurrentToken == TokenType.ASG)
                    {
                        AddToErrorMessage("default values are not supported for macros");
                    }
                    else
                    {
                        AddToErrorMessage("expected ')' or ',' for macro function declaration");
                    }
                    return false;
                }
            }

            if (isfunc && Expect(TokenType.ROUNDCLOSE) == null)
            {
                return false;
            }

            ExFState tmp = FunctionState.Copy();
            FunctionState = f_state;

            if (!IsInMacroBlock)
            {
                while (CurrentToken != TokenType.NEWLINE
                     && CurrentToken != TokenType.ENDLINE
                     && CurrentToken != TokenType.MACROEND
                     && CurrentToken != TokenType.UNKNOWN)
                {
                    if (!ProcessStatements())//true))
                    {
                        return false;
                    }
                    //if (!ProcessStatement(true,true))
                    //{
                    //    return false;
                    //}
                }
                if (!ReadAndSetToken())
                {
                    return false;
                }

                f_state.AddInstr(OPC.RETURN, 1, FunctionState.PopTarget(), 0, 0);

                f_state.AddLineInfo(Lexer.TokenPrev == TokenType.NEWLINE ? Lexer.PrevTokenLine : Lexer.CurrentLine, StoreLineInfos, true);

            }
            else
            {
                // TO-DO
                f_state.AddInstr(OPC.RETURNMACRO, 1, FunctionState.PopTarget(), 0, 0);
            }

            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExPrototype fpro = f_state.CreatePrototype();
            //fpro.ClosureType = ExClosureType.MACRO;

            FunctionState = tmp;
            FunctionState.Functions.Add(fpro);
            FunctionState.PopChildState();

            return true;
        }

        private void FixAfterMacro(ExLexer old_lex, TokenType old_currToken, string old_src)
        {
            Lexer = old_lex;
            CurrentToken = old_currToken;
            Source = old_src;
            IsInMacroBlock = false;
        }

        public bool ProcessMacroBlockStatement()    // TO-DO
        {
            ExObject idx;

            ExLexer old_lex = Lexer;
            TokenType old_currToken = CurrentToken;
            string old_src = Source;

            Lexer = new(Lexer.MacroBlock) { MacroParams = Lexer.MacroParams, MacroBlock = Lexer.MacroBlock, IsReadingMacroBlock = true };

            if (!ReadAndSetToken() || (idx = Expect(TokenType.IDENTIFIER)) == null)
            {
                FixAfterMacro(old_lex, old_currToken, old_src);
                return false;
            }

            if (idx.GetString().ToUpper() != idx.GetString())
            {
                FixAfterMacro(old_lex, old_currToken, old_src);
                AddToErrorMessage("macro names should be all uppercase characters!");
                return false;
            }

            FunctionState.PushTarget(0);
            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(idx), 0, 0);
            bool isfunc = CurrentToken == TokenType.ROUNDOPEN;

            IsInMacroBlock = true;

            if (!ExMacroCreate(idx, isfunc))
            {
                FixAfterMacro(old_lex, old_currToken, old_src);
                return false;
            }

            if (FunctionState.IsBlockMacro(idx.GetString()))
            {
                AddToErrorMessage("macro '" + idx.GetString() + "' already exists!");
                FixAfterMacro(old_lex, old_currToken, old_src);
                return false;
            }
            else
            {
                FunctionState.AddBlockMacro(idx.GetString(), new() { Name = idx.GetString(), Source = Lexer.MacroBlock, Parameters = Lexer.MacroParams });
            }

            FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            FunctionState.PopTarget();

            FixAfterMacro(old_lex, old_currToken, old_src);
            return true;
        }

        public bool ProcessMacroStatement()
        {
            ExObject idx;

            if (!ReadAndSetToken() || (idx = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            if (idx.GetString().ToUpper() != idx.GetString())
            {
                AddToErrorMessage("macro names should be all uppercase characters!");
                return false;
            }

            FunctionState.PushTarget(0);
            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetConst(idx), 0, 0);
            bool isfunc = CurrentToken == TokenType.ROUNDOPEN;
            if (!ExMacroCreate(idx, isfunc))
            {
                return false;
            }

            if (!FunctionState.AddMacro(idx, isfunc, true))   // TO-DO stop using forced param
            {
                AddToErrorMessage("macro " + idx.GetString() + " already exists");
                return false;
            }

            FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            FunctionState.PopTarget();

            return true;
        }

        public bool ExFuncCreate(ExObject o, bool lambda = false)
        {
            ExFState f_state = FunctionState.PushChildState(VM.SharedState);
            f_state.Name = o;

            ExObject pname;
            f_state.AddParam(FunctionState.CreateString(ExMat.ThisName));
            f_state.Source = new(Source);

            int def_param_count = 0;

            while (CurrentToken != TokenType.ROUNDCLOSE)
            {
                if (CurrentToken == TokenType.VARGS)
                {
                    if (def_param_count > 0)
                    {
                        AddToErrorMessage("can't use vargs alongside default valued parameters");
                        return false;
                    }
                    f_state.AddParam(FunctionState.CreateString(ExMat.VargsName));
                    f_state.HasVargs = true;
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                    if (CurrentToken != TokenType.ROUNDCLOSE)
                    {
                        AddToErrorMessage("expected ')' after vargs '...'");
                        return false;
                    }
                    break;
                }
                if ((pname = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                f_state.AddParam(pname);

                if (CurrentToken == TokenType.ASG)
                {
                    if (!ReadAndSetToken() || !ExExp())
                    {
                        return false;
                    }

                    f_state.AddDefParam(FunctionState.TopTarget());
                    def_param_count++;
                }
                else
                {
                    if (def_param_count > 0)
                    {
                        AddToErrorMessage("expected = for a default value");
                        return false;
                    }
                }

                if (CurrentToken == TokenType.SEP)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                else if (CurrentToken != TokenType.ROUNDCLOSE)
                {
                    AddToErrorMessage("expected ')' or ',' for function declaration");
                    return false;
                }
            }

            if (Expect(TokenType.ROUNDCLOSE) == null)
            {
                return false;
            }

            for (int i = 0; i < def_param_count; i++)
            {
                FunctionState.PopTarget();
            }

            ExFState tmp = FunctionState.Copy();
            FunctionState = f_state;

            if (lambda)
            {
                if (!ExExp())
                {
                    return false;
                }
                f_state.AddInstr(OPC.RETURN, 1, FunctionState.PopTarget(), 0, 0);
            }
            else
            {
                if (!ProcessStatement(false))
                {
                    return false;
                }
            }

            f_state.AddLineInfo(Lexer.TokenPrev == TokenType.NEWLINE ? Lexer.PrevTokenLine : Lexer.CurrentLine, StoreLineInfos, true);
            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExPrototype fpro = f_state.CreatePrototype();

            FunctionState = tmp;
            FunctionState.Functions.Add(fpro);
            FunctionState.PopChildState();

            return true;
        }

        public bool ExOpUnary(OPC op)
        {
            if (!ExPrefixed())
            {
                return false;
            }
            int s = FunctionState.PopTarget();
            FunctionState.AddInstr(op, FunctionState.PushTarget(), s, 0, 0);

            return true;
        }


        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExCompiler()
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
