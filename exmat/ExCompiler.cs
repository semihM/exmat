using System;
using System.Collections.Generic;
using ExMat.FuncPrototype;
using ExMat.Lexer;
using ExMat.Objects;
using ExMat.OPs;
using ExMat.States;
using ExMat.Token;
using ExMat.VM;

namespace ExMat.Compiler
{
    public class ExCompiler : IDisposable
    {
        private ExVM _VM;
        private string _source;

        private ExEState _ExpState = new();
        private ExFState _FuncState;

        private ExLexer _lexer;
        private TokenType _currToken;

        private readonly bool _lineinfo;
        private bool _inblockmacro;

        private ExScope _scope = new();

        public string _error;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _FuncState = null;
                    _ExpState = null;
                    _lexer.Dispose();
                    _VM = null;
                    _scope = null;
                    _error = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void AddToErrorMessage(string msg)
        {
            if (string.IsNullOrEmpty(_error))
            {
                _error = "[ERROR] " + msg;
            }
            else
            {
                _error += "\n[ERROR] " + msg;
            }
        }

        public ExScope CreateScope()
        {
            ExScope old = new() { outers = _scope.outers, stack_size = _scope.stack_size };
            _scope.stack_size = _FuncState.GetLocalStackSize();
            _scope.outers = _FuncState._nouters;
            return old;
        }

        public void ResolveScopeOuters()
        {
            if (_FuncState.GetLocalStackSize() != _scope.stack_size)
            {
                if (_FuncState.GetOuterSize(_scope.stack_size) != 0)
                {
                    _FuncState.AddInstr(OPC.CLOSE, 0, _scope.stack_size, 0, 0);
                }
            }
        }

        public void ReleaseScope(ExScope old, bool close = true)
        {
            int old_nout = _FuncState._nouters;
            if (_FuncState.GetLocalStackSize() != _scope.stack_size)
            {
                _FuncState.SetLocalStackSize(_scope.stack_size);
                if (close && old_nout != _FuncState._nouters)
                {
                    _FuncState.AddInstr(OPC.CLOSE, 0, _scope.stack_size, 0, 0);
                }
            }
            _scope = old;
        }

        public ExCompiler()
        {
            _lineinfo = false;
            _scope.outers = 0;
            _scope.stack_size = 0;
        }

        public void AddErrorInfo()
        {
            AddToErrorMessage("[LINE: " + _lexer._currLine + ", COL: " + _lexer._currCol + "] " + _lexer._error);
        }

        public bool Compile(ExVM vm, string src, ref ExObject o)
        {
            _VM = vm;
            _source = src;
            _lexer = new ExLexer(src);

            bool state = Compile(ref o);
            vm.n_return = _FuncState.n_statement;

            if (!state)
            {
                if (_currToken == TokenType.ENDLINE)
                {
                    AddToErrorMessage("syntax error");
                }
                AddErrorInfo();
            }
            return state;
        }

        public bool Compile(ref ExObject o)
        {
            ExFState fst = new(_VM._sState, null);
            fst._name = _VM.CreateString("main");
            _FuncState = fst;

            _FuncState.AddParam(_FuncState.CreateString(ExMat._THIS));
            _FuncState.AddParam(_FuncState.CreateString("vargv"));
            _FuncState._pvars = true;
            _FuncState._source = new(_source);

            int s_size = _FuncState.GetLocalStackSize();

            if (!ReadAndSetToken())
            {
                return false;
            }
            int s_count = 0;
            while (_currToken != TokenType.ENDLINE)
            {
                if (!ProcessStatement())
                {
                    return false;
                }
                s_count++;
                if (_lexer._prevToken != TokenType.CLS_CLOSE && _lexer._prevToken != TokenType.SMC)
                {
                    if (!CheckSMC())
                    {
                        return false;
                    }
                }
            }
            _FuncState.n_statement = s_count;

            _FuncState.SetLocalStackSize(s_size);
            _FuncState.AddLineInfo(_lexer._currLine, _lineinfo, true);
            _FuncState.AddInstr(OPC.RETURN, 1, _FuncState._localvs.Count + _FuncState._localinfos.Count, 0, 0);   //0,1 = root, main | 2 == stackbase == this | 3: varg | 4: result
            _FuncState.SetLocalStackSize(0);

            o._val._FuncPro = _FuncState.CreatePrototype();

            return true;
        }

        private bool ReadAndSetToken()
        {
            _currToken = _lexer.Lex();
            if (_currToken == TokenType.UNKNOWN)
            {
                return false;
            }
            return true;
        }

        public string GetStringForTokenType(TokenType typ)
        {
            foreach (KeyValuePair<string, TokenType> pair in _lexer._keyWordsDict)
            {
                if (pair.Value == typ)
                {
                    return pair.Key;
                }
            }
            return typ.ToString();
        }

        public ExObject Expect(TokenType typ)
        {
            if (typ != _currToken)
            {
                if (typ != TokenType.IDENTIFIER || _currToken != TokenType.CONSTRUCTOR)
                {
                    string expmsg;
                    switch (typ)
                    {
                        case TokenType.IDENTIFIER:
                            {
                                expmsg = "IDENTIFIER";
                                break;
                            }
                        case TokenType.LITERAL:
                            {
                                expmsg = "STRING_LITERAL";
                                break;
                            }
                        case TokenType.INTEGER:
                            {
                                expmsg = "INTEGER";
                                break;
                            }
                        case TokenType.FLOAT:
                            {
                                expmsg = "FLOAT";
                                break;
                            }
                        default:
                            {
                                expmsg = GetStringForTokenType(typ);
                                break;
                            }
                    }
                    AddToErrorMessage("Expected " + expmsg);
                    return null;
                }
            }

            ExObject res = new();
            switch (typ)
            {
                case TokenType.IDENTIFIER:
                    {
                        res = _FuncState.CreateString(_lexer.str_val);
                        break;
                    }
                case TokenType.LITERAL:
                    {
                        res = _FuncState.CreateString(_lexer.str_val, _lexer._aStr.Length - 1);
                        break;
                    }
                case TokenType.INTEGER:
                    {
                        res = new(_lexer.i_val);
                        break;
                    }
                case TokenType.FLOAT:
                    {
                        res = new(_lexer.f_val);
                        break;
                    }
            }
            if (!ReadAndSetToken())
            {
                return null;
            }
            return res;
        }

        public bool IsEOS()
        {
            return _lexer._prevToken == TokenType.NEWLINE
               || _currToken == TokenType.ENDLINE
               || _currToken == TokenType.CLS_CLOSE
               || _currToken == TokenType.SMC;
        }

        public bool CheckSMC()
        {
            if (_currToken == TokenType.SMC || _currToken == TokenType.MACROBLOCK)
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

        public bool ProcessStatements(bool macro = false)
        {
            while (_currToken != TokenType.CLS_CLOSE && (!macro || (_currToken != TokenType.MACROEND)))
            {
                if (!ProcessStatement(macro: macro))
                {
                    return false;
                }
                if (_lexer._prevToken != TokenType.CLS_CLOSE && _lexer._prevToken != TokenType.SMC && (!macro || (_currToken != TokenType.MACROEND)))
                {
                    if (!CheckSMC())
                    {
                        return false;
                    }
                }
                if (macro && _currToken == TokenType.MACROEND)
                {
                    return true;
                }
            }
            return true;
        }

        public bool ProcessStatement(bool cl = true, bool macro = false)
        {
            _FuncState.AddLineInfo(_lexer._currLine, _lineinfo, false);
            switch (_currToken)
            {
                case TokenType.SMC:
                    {
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
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
                case TokenType.FOREACH:
                    {
                        if (!ProcessForeachStatement())
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
                case TokenType.SUM:
                    {
                        if (!ProcessSumStatement())
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
                case TokenType.RETURN:
                    {
                        OPC op = OPC.RETURN;
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        if (!IsEOS())
                        {
                            int rexp = _FuncState.GetCurrPos() + 1;
                            if (!ExSepExp())
                            {
                                return false;
                            }
                            // TO-DO trap count check and pop
                            _FuncState._returnE = rexp;
                            _FuncState.AddInstr(op, 1, _FuncState.PopTarget(), _FuncState.GetLocalStackSize(), 0);
                        }
                        else
                        {
                            _FuncState._returnE = -1;
                            _FuncState.AddInstr(op, 985, 0, _FuncState.GetLocalStackSize(), 0);
                        }
                        break;
                    }
                case TokenType.BREAK:
                    {
                        if (_FuncState._breaktargs.Count <= 0)
                        {
                            AddToErrorMessage("'break' has to be in a breakable block");
                            return false;
                        }

                        if (_FuncState._breaktargs[^1] > 0)
                        {
                            _FuncState.AddInstr(OPC.TRAPPOP, _FuncState._breaktargs[^1], 0, 0, 0);
                        }

                        DoOuterControl();
                        _FuncState.AddInstr(OPC.JMP, 0, -1234, 0, 0);
                        _FuncState._breaks.Add(_FuncState.GetCurrPos());

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.CONTINUE:
                    {
                        if (_FuncState._continuetargs.Count <= 0)
                        {
                            AddToErrorMessage("'continue' has to be in a breakable block");
                            return false;
                        }

                        if (_FuncState._continuetargs[^1] > 0)
                        {
                            _FuncState.AddInstr(OPC.TRAPPOP, _FuncState._continuetargs[^1], 0, 0, 0);
                        }

                        DoOuterControl();
                        _FuncState.AddInstr(OPC.JMP, 0, -1234, 0, 0);
                        _FuncState._continues.Add(_FuncState.GetCurrPos());

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
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
                case TokenType.CLS_OPEN:
                    {
                        ExScope scp = CreateScope();

                        if (!ReadAndSetToken()
                            || !ProcessStatements()
                            || Expect(TokenType.CLS_CLOSE) == null)
                        {
                            return false;
                        }

                        ReleaseScope(scp, cl);
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
                default:
                    {
                        if (!ExSepExp())
                        {
                            return false;
                        }
                        if (!macro)
                        {
                            _FuncState.DiscardTopTarget();
                        }
                        break;
                    }
            }
            _FuncState._not_snoozed = false;
            return true;
        }

        public void DoOuterControl()
        {
            if (_FuncState.GetLocalStackSize() != _scope.stack_size)
            {
                if (_FuncState.GetOuterSize(_scope.stack_size) > 0)
                {
                    _FuncState.AddInstr(OPC.CLOSE, 0, _scope.stack_size, 0, 0);
                }
            }
        }
        public List<int> CreateBreakableBlock()
        {
            _FuncState._breaktargs.Add(0);
            _FuncState._continuetargs.Add(0);
            return new(2) { _FuncState._breaks.Count, _FuncState._continues.Count };
        }

        public void ReleaseBreakableBlock(List<int> bc, int t)
        {
            bc[0] = _FuncState._breaks.Count - bc[0];
            bc[1] = _FuncState._continues.Count - bc[1];

            if (bc[0] > 0)
            {
                DoBreakControl(_FuncState, bc[0]);
            }

            if (bc[1] > 0)
            {
                DoContinueControl(_FuncState, bc[1], t);
            }

            if (_FuncState._breaks.Count > 0)
            {
                _FuncState._breaks.RemoveAt(_FuncState._breaks.Count - 1);
            }
            if (_FuncState._continues.Count > 0)
            {
                _FuncState._continues.RemoveAt(_FuncState._continues.Count - 1);
            }
        }

        public static void DoBreakControl(ExFState fs, int count)
        {
            while (count > 0)
            {
                int p = fs._breaks[^1];
                fs._breaks.RemoveAt(fs._breaks.Count - 1);
                fs.SetInstrParams(p, 0, fs.GetCurrPos() - p, 0, 0);
                count--;
            }
        }

        public static void DoContinueControl(ExFState fs, int count, int t)
        {
            while (count > 0)
            {
                int p = fs._continues[^1];
                fs._continues.RemoveAt(fs._continues.Count - 1);
                fs.SetInstrParams(p, 0, t - p, 0, 0);
                count--;
            }
        }

        public bool ProcessIfStatement()
        {
            int jpos;
            bool b_else = false;
            if (!ReadAndSetToken()
                || Expect(TokenType.R_OPEN) == null
                || !ExSepExp()
                || Expect(TokenType.R_CLOSE) == null)
            {
                return false;
            }

            _FuncState.AddInstr(OPC.JZ, _FuncState.PopTarget(), 0, 0, 0);
            int jnpos = _FuncState.GetCurrPos();

            ExScope old = CreateScope();
            if (!ProcessStatement())
            {
                return false;
            }
            if (_currToken != TokenType.CLS_CLOSE && _currToken != TokenType.ELSE)
            {
                if (!CheckSMC())
                {
                    return false;
                }
            }

            ReleaseScope(old);
            int epos = _FuncState.GetCurrPos();
            if (_currToken == TokenType.ELSE)
            {
                b_else = true;

                old = CreateScope();
                _FuncState.AddInstr(OPC.JMP, 0, 0, 0, 0);
                jpos = _FuncState.GetCurrPos();

                if (!ReadAndSetToken() || !ProcessStatement())
                {
                    return false;
                }

                if (_lexer._prevToken != TokenType.CLS_CLOSE)
                {
                    if (!CheckSMC())
                    {
                        return false;
                    }
                }
                ReleaseScope(old);

                _FuncState.SetInstrParam(jpos, 1, _FuncState.GetCurrPos() - jpos);
            }
            _FuncState.SetInstrParam(jnpos, 1, epos - jnpos + (b_else ? 1 : 0));
            return true;
        }

        public bool ProcessForStatement()
        {
            if (!ReadAndSetToken())
            {
                return false;
            }

            ExScope scp = CreateScope();
            if (Expect(TokenType.R_OPEN) == null)
            {
                return false;
            }

            if (_currToken == TokenType.VAR)
            {
                if (!ProcessVarAsgStatement())
                {
                    return false;
                }
            }
            else if (_currToken != TokenType.SMC)
            {
                if (!ExSepExp())
                {
                    return false;
                }
                _FuncState.PopTarget();
            }

            if (Expect(TokenType.SMC) == null)
            {
                return false;
            }

            _FuncState._not_snoozed = false;

            int jpos = _FuncState.GetCurrPos();
            int jzpos = -1;

            if (_currToken != TokenType.SMC)
            {
                if (!ExSepExp())
                {
                    return false;
                }
                _FuncState.AddInstr(OPC.JZ, _FuncState.PopTarget(), 0, 0, 0);
                jzpos = _FuncState.GetCurrPos();
            }

            if (Expect(TokenType.SMC) == null)
            {
                return false;
            }
            _FuncState._not_snoozed = false;

            int estart = _FuncState.GetCurrPos() + 1;
            if (_currToken != TokenType.R_CLOSE)
            {
                if (!ExSepExp())
                {
                    return false;
                }
                _FuncState.PopTarget();
            }

            if (Expect(TokenType.R_CLOSE) == null)
            {
                return false;
            }

            _FuncState._not_snoozed = false;

            int eend = _FuncState.GetCurrPos();
            int esize = eend - estart + 1;
            List<ExInstr> instrs = null;

            if (esize > 0)
            {
                instrs = new(esize);
                int n_instr = _FuncState._instructions.Count;
                for (int i = 0; i < esize; i++)
                {
                    instrs.Add(_FuncState._instructions[estart + i]);
                }
                for (int i = 0; i < esize; i++)
                {
                    _FuncState._instructions.RemoveAt(_FuncState._instructions.Count - 1);
                }
            }

            List<int> bc = CreateBreakableBlock();

            if (!ProcessStatement())
            {
                return false;
            }
            int ctarg = _FuncState.GetCurrPos();

            if (esize > 0)
            {
                for (int i = 0; i < esize; i++)
                {
                    _FuncState.AddInstr(instrs[i]);
                }
            }

            _FuncState.AddInstr(OPC.JMP, 0, jpos - _FuncState.GetCurrPos() - 1, 0, 0);

            if (jzpos > 0)
            {
                _FuncState.SetInstrParam(jzpos, 1, _FuncState.GetCurrPos() - jzpos);
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
            ExFState f_state = _FuncState.PushChildState(_VM._sState);
            f_state._name = o;

            ExObject pname;
            f_state.AddParam(_FuncState.CreateString(ExMat._THIS));
            f_state.AddParam(_FuncState.CreateString("n"));

            f_state._source = new(_source);
            int pcount = 0;


            while (_currToken != TokenType.R_CLOSE)
            {
                bool neg = false;
                if (_currToken == TokenType.SUB)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                    neg = true;
                }
                if ((pname = Expect(TokenType.INTEGER)) == null)
                {
                    return false;
                }
                if (neg)
                {
                    pname._val.i_Int *= -1;
                }

                pname = new(pname.GetInt().ToString());

                pcount++;
                f_state.AddParam(pname);

                if (_currToken == TokenType.COL)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }

                    if (!ExExp())
                    {
                        return false;
                    }

                    f_state.AddDefParam(_FuncState.TopTarget());
                }
                else // TO-DO add = for referencing global and do get ops
                {
                    AddToErrorMessage("expected ':' for a sequence constants");
                    return false;
                }

                if (_currToken == TokenType.SEP)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                else if (_currToken != TokenType.R_CLOSE)
                {
                    AddToErrorMessage("expected ')' for sequence constants definition end");
                    return false;
                }
            }
            if (Expect(TokenType.R_CLOSE) == null)
            {
                return false;
            }

            for (int i = 0; i < pcount; i++)
            {
                _FuncState.PopTarget();
            }

            ExFState tmp = _FuncState.Copy();
            _FuncState = f_state;

            if (!ExExp())
            {
                return false;
            }
            f_state.AddInstr(OPC.RETURN, 1, _FuncState.PopTarget(), 0, 0);

            f_state.AddLineInfo(_lexer._prevToken == TokenType.NEWLINE ? _lexer._lastTokenLine : _lexer._currLine, _lineinfo, true);
            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExFuncPro fpro = f_state.CreatePrototype();
            fpro.type = ExClosureType.SEQUENCE;

            _FuncState = tmp;
            _FuncState._funcs.Add(fpro);
            _FuncState.PopChildState();

            return true;
        }

        public bool ProcessSequenceStatement()
        {
            ExObject v;
            if (!ReadAndSetToken() || (v = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            _FuncState.PushTarget(0);
            _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(v), 0, 0);

            if (Expect(TokenType.R_OPEN) == null || !ExSequenceCreate(v))
            {
                return false;
            }

            _FuncState.AddInstr(OPC.CLOSURE, _FuncState.PushTarget(), _FuncState._funcs.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            _FuncState.PopTarget();

            return true;
        }

        public bool ProcessClusterAsgStatement()
        {
            ExObject v;
            if (!ReadAndSetToken() || (v = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            _FuncState.PushTarget(0);
            _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(v), 0, 0);

            if (Expect(TokenType.CLS_OPEN) == null || !ExClusterCreate(v))
            {
                return false;
            }

            _FuncState.AddInstr(OPC.CLOSURE, _FuncState.PushTarget(), _FuncState._funcs.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            _FuncState.PopTarget();

            return true;
        }

        public bool ExClusterCreate(ExObject o)
        {
            ExFState f_state = _FuncState.PushChildState(_VM._sState);
            f_state._name = o;

            ExObject pname;
            f_state.AddParam(_FuncState.CreateString(ExMat._THIS));
            f_state._source = new(_source);
            int pcount = 0;

            while (_currToken != TokenType.SMC)
            {
                if ((pname = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                pcount++;
                f_state.AddParam(pname);

                if (_currToken == TokenType.IN)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }

                    if (_currToken != TokenType.SPACE)
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
                        AddSpaceConstLoadInstr(_lexer._space, -1);
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                    }

                    f_state.AddDefParam(_FuncState.TopTarget());
                }
                else // TO-DO add = for referencing global and do get ops
                {
                    AddToErrorMessage("expected 'in' for a domain reference");
                    return false;
                }

                if (_currToken == TokenType.SEP)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                else if (_currToken != TokenType.SMC && _currToken != TokenType.CLS_CLOSE)
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
                _FuncState.PopTarget();
            }

            ExFState tmp = _FuncState.Copy();
            _FuncState = f_state;

            if (_currToken == TokenType.ELEMENT_DEF)
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

            if (_currToken != TokenType.ELEMENT_DEF)
            {
                AddToErrorMessage("expected '=>' to define elements of cluster");
                return false;
            }
            //
            _FuncState.AddInstr(OPC.JZ, _FuncState.PopTarget(), 0, 0, 0);

            int jzp = _FuncState.GetCurrPos();
            int t = _FuncState.PushTarget();

            if (!ReadAndSetToken() || !ExExp())
            {
                return false;
            }

            int f = _FuncState.PopTarget();
            if (t != f)
            {
                _FuncState.AddInstr(OPC.MOVE, t, f, 0, 0);
            }
            int end_f = _FuncState.GetCurrPos();

            _FuncState.AddInstr(OPC.JMP, 0, 0, 0, 0);

            int jmp = _FuncState.GetCurrPos();

            _FuncState.AddInstr(OPC.LOAD_BOOL, _FuncState.PushTarget(), 0, 0, 0);

            int s = _FuncState.PopTarget();
            if (t != s)
            {
                _FuncState.AddInstr(OPC.MOVE, t, s, 0, 0);
            }

            _FuncState.SetInstrParam(jmp, 1, _FuncState.GetCurrPos() - jmp);
            _FuncState.SetInstrParam(jzp, 1, end_f - jzp + 1);
            _FuncState._not_snoozed = false;
            //

            if (_currToken != TokenType.CLS_CLOSE)
            {
                AddToErrorMessage("expected '}' to declare a cluster");
                return false;
            }

            if (!ReadAndSetToken())
            {
                return false;
            }

            f_state.AddInstr(OPC.RETURN, 1, _FuncState.PopTarget(), 0, 0);
            f_state.AddLineInfo(_lexer._prevToken == TokenType.NEWLINE ? _lexer._lastTokenLine : _lexer._currLine, _lineinfo, true);
            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExFuncPro fpro = f_state.CreatePrototype();
            fpro.type = ExClosureType.CLUSTER;

            _FuncState = tmp;
            _FuncState._funcs.Add(fpro);
            _FuncState.PopChildState();

            return true;
        }


        public bool ExRuleCreate(ExObject o)
        {
            ExFState f_state = _FuncState.PushChildState(_VM._sState);
            f_state._name = o;

            ExObject pname;
            f_state.AddParam(_FuncState.CreateString(ExMat._THIS));
            f_state._source = new(_source);

            while (_currToken != TokenType.R_CLOSE)
            {
                if ((pname = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                f_state.AddParam(pname);

                if (_currToken == TokenType.SEP)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                else if (_currToken != TokenType.R_CLOSE)
                {
                    if (_currToken == TokenType.ASG)
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

            if (Expect(TokenType.R_CLOSE) == null)
            {
                return false;
            }

            ExFState tmp = _FuncState.Copy();
            _FuncState = f_state;

            if (!ExExp())
            {
                return false;
            }
            f_state.AddInstr(OPC.RETURNBOOL, 1, _FuncState.PopTarget(), 0, 0);

            f_state.AddLineInfo(_lexer._prevToken == TokenType.NEWLINE ? _lexer._lastTokenLine : _lexer._currLine, _lineinfo, true);
            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExFuncPro fpro = f_state.CreatePrototype();
            fpro.type = ExClosureType.RULE;

            _FuncState = tmp;
            _FuncState._funcs.Add(fpro);
            _FuncState.PopChildState();

            return true;
        }

        public bool ProcessRuleAsgStatement()
        {
            ExObject v;
            if (!ReadAndSetToken() || (v = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            _FuncState.PushTarget(0);
            _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(v), 0, 0);

            if (Expect(TokenType.R_OPEN) == null || !ExRuleCreate(v))
            {
                return false;
            }

            _FuncState.AddInstr(OPC.CLOSURE, _FuncState.PushTarget(), _FuncState._funcs.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            return true;
        }

        public static bool ProcessSumStatement()
        {
            return false;
        }

        public bool ProcessVarAsgStatement()
        {
            ExObject v;
            if (!ReadAndSetToken())
            {
                return false;
            }

            if (_currToken == TokenType.FUNCTION)
            {
                if (!ReadAndSetToken()
                    || (v = Expect(TokenType.IDENTIFIER)) == null
                    || Expect(TokenType.R_OPEN) == null
                    || !ExFuncCreate(v))
                {
                    return false;
                }

                _FuncState.AddInstr(OPC.CLOSURE, _FuncState.PushTarget(), _FuncState._funcs.Count - 1, 0, 0);
                _FuncState.PopTarget();
                _FuncState.PushVar(v);
                return true;
            }
            else if (_currToken == TokenType.RULE)
            {
                if (!ReadAndSetToken()
                    || (v = Expect(TokenType.IDENTIFIER)) == null
                    || Expect(TokenType.R_OPEN) == null
                    || !ExRuleCreate(v))
                {
                    return false;
                }

                _FuncState.AddInstr(OPC.CLOSURE, _FuncState.PushTarget(), _FuncState._funcs.Count - 1, 0, 0);
                _FuncState.PopTarget();
                _FuncState.PushVar(v);
                return true;
            }
            else if (_currToken == TokenType.CLUSTER)
            {
                if (!ReadAndSetToken()
                    || (v = Expect(TokenType.IDENTIFIER)) == null
                    || Expect(TokenType.CLS_OPEN) == null
                    || !ExClusterCreate(v))
                {
                    return false;
                }

                _FuncState.AddInstr(OPC.CLOSURE, _FuncState.PushTarget(), _FuncState._funcs.Count - 1, 0, 0);
                _FuncState.PopTarget();
                _FuncState.PushVar(v);
                return true;
            }

            while (true)
            {

                if ((v = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                if (_currToken == TokenType.ASG)
                {
                    if (!ReadAndSetToken() || !ExExp())
                    {
                        return false;
                    }

                    int s = _FuncState.PopTarget();
                    int d = _FuncState.PushTarget();

                    if (d != s)
                    {
                        _FuncState.AddInstr(OPC.MOVE, d, s, 0, 0);
                    }
                }
                else
                {
                    _FuncState.AddInstr(OPC.LOAD_NULL, _FuncState.PushTarget(), 1, 0, 0);
                }
                _FuncState.PopTarget();
                _FuncState.PushVar(v);
                if (_currToken == TokenType.SEP)
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

            _FuncState.PushTarget(0);
            _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(idx), 0, 0);

            if (_currToken == TokenType.GLB)
            {
                AddBasicOpInstr(OPC.GET);
            }

            while (_currToken == TokenType.GLB)
            {
                if (!ReadAndSetToken() || (idx = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(idx), 0, 0);

                if (_currToken == TokenType.GLB)
                {
                    AddBasicOpInstr(OPC.GET);
                }
            }

            if (Expect(TokenType.R_OPEN) == null || !ExFuncCreate(idx))
            {
                return false;
            }

            _FuncState.AddInstr(OPC.CLOSURE, _FuncState.PushTarget(), _FuncState._funcs.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            _FuncState.PopTarget();

            return true;
        }

        public bool ProcessClassStatement()
        {
            ExEState ex;
            if (!ReadAndSetToken())
            {
                return false;
            }
            ex = _ExpState.Copy();

            _ExpState.stop_deref = true;

            if (!ExPrefixed())
            {
                return false;
            }

            if (_ExpState._type == ExEType.EXPRESSION)
            {
                AddToErrorMessage("invalid class name");
                return false;
            }
            else if (_ExpState._type == ExEType.OBJECT || _ExpState._type == ExEType.BASE)
            {
                if (!ExClassResolveExp())
                {
                    return false;
                }
                AddBasicDerefInstr(OPC.NEWSLOT);
                _FuncState.PopTarget();
            }
            else
            {
                AddToErrorMessage("can't create a class as local");
                return false;
            }
            _ExpState = ex;
            return true;
        }

        public bool ExInvokeExp(string ex)
        {
            ExEState eState = _ExpState.Copy();
            _ExpState._type = ExEType.EXPRESSION;
            _ExpState._pos = -1;
            _ExpState.stop_deref = false;

            if (!(bool)Type.GetType("ExMat.Compiler.ExCompiler").GetMethod(ex).Invoke(this, null))
            {
                return false;
            }

            _ExpState = eState;
            return true;
        }

        public bool ExBinaryExp(OPC op, string func, int lastop = 0)
        {
            if (!ReadAndSetToken() || !ExInvokeExp(func))
            {
                return false;
            }

            int arg1 = _FuncState.PopTarget();
            int arg2 = _FuncState.PopTarget();

            _FuncState.AddInstr(op, _FuncState.PushTarget(), arg1, arg2, lastop);
            return true;
        }

        public bool ExExp()
        {
            ExEState estate = _ExpState.Copy();
            _ExpState._type = ExEType.EXPRESSION;
            _ExpState._pos = -1;
            _ExpState.stop_deref = false;

            if (!ExLogicOr())
            {
                return false;
            }

            switch (_currToken)
            {
                case TokenType.ASG:
                case TokenType.ADDEQ:
                case TokenType.SUBEQ:
                case TokenType.DIVEQ:
                case TokenType.MLTEQ:
                case TokenType.MODEQ:
                case TokenType.NEWSLOT:
                    {
                        TokenType op = _currToken;
                        ExEType etyp = _ExpState._type;
                        int pos = _ExpState._pos;

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
                                                int s = _FuncState.PopTarget();
                                                int d = _FuncState.TopTarget();
                                                _FuncState.AddInstr(OPC.MOVE, d, s, 0, 0);
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
                                                int s = _FuncState.PopTarget();
                                                int d = _FuncState.PushTarget();
                                                _FuncState.AddInstr(OPC.SETOUTER, d, pos, s, 0);
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

                        _FuncState.AddInstr(OPC.JZ, _FuncState.PopTarget(), 0, 0, 0);

                        int jzp = _FuncState.GetCurrPos();
                        int t = _FuncState.PushTarget();

                        if (!ExExp())
                        {
                            return false;
                        }

                        int f = _FuncState.PopTarget();
                        if (t != f)
                        {
                            _FuncState.AddInstr(OPC.MOVE, t, f, 0, 0);
                        }
                        int end_f = _FuncState.GetCurrPos();

                        _FuncState.AddInstr(OPC.JMP, 0, 0, 0, 0);
                        if (Expect(TokenType.COL) == null)
                        {
                            return false;
                        }

                        int jmp = _FuncState.GetCurrPos();

                        if (!ExExp())
                        {
                            return false;
                        }

                        int s = _FuncState.PopTarget();
                        if (t != s)
                        {
                            _FuncState.AddInstr(OPC.MOVE, t, s, 0, 0);
                        }

                        _FuncState.SetInstrParam(jmp, 1, _FuncState.GetCurrPos() - jmp);
                        _FuncState.SetInstrParam(jzp, 1, end_f - jzp + 1);
                        _FuncState._not_snoozed = false;

                        break;
                    }
            }
            _ExpState = estate;
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
                switch (_currToken)
                {
                    case TokenType.OR:
                        {
                            int f = _FuncState.PopTarget();
                            int t = _FuncState.PushTarget();

                            _FuncState.AddInstr(OPC.OR, t, 0, f, 0);

                            int j = _FuncState.GetCurrPos();

                            if (t != f)
                            {
                                _FuncState.AddInstr(OPC.MOVE, t, f, 0, 0);
                            }

                            if (!ReadAndSetToken() || !ExInvokeExp("ExLogicOr"))
                            {
                                return false;
                            }

                            _FuncState._not_snoozed = false;

                            int s = _FuncState.PopTarget();

                            if (t != s)
                            {
                                _FuncState.AddInstr(OPC.MOVE, t, s, 0, 0);
                            }

                            _FuncState._not_snoozed = false;
                            _FuncState.SetInstrParam(j, 1, _FuncState.GetCurrPos() - j);
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
                switch (_currToken)
                {
                    case TokenType.AND:
                        {
                            int f = _FuncState.PopTarget();
                            int t = _FuncState.PushTarget();

                            _FuncState.AddInstr(OPC.AND, t, 0, f, 0);

                            int j = _FuncState.GetCurrPos();

                            if (t != f)
                            {
                                _FuncState.AddInstr(OPC.MOVE, t, f, 0, 0);
                            }
                            if (!ReadAndSetToken() || !ExInvokeExp("ExLogicAnd"))
                            {
                                return false;
                            }

                            _FuncState._not_snoozed = false;

                            int s = _FuncState.PopTarget();

                            if (t != s)
                            {
                                _FuncState.AddInstr(OPC.MOVE, t, s, 0, 0);
                            }

                            _FuncState._not_snoozed = false;
                            _FuncState.SetInstrParam(j, 1, _FuncState.GetCurrPos() - j);
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
                switch (_currToken)
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
                switch (_currToken)
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
                switch (_currToken)
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
                switch (_currToken)
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
                switch (_currToken)
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
                    case TokenType.IN:
                        {
                            if (!ExBinaryExp(OPC.EXISTS, "ExLogicShift"))
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
                switch (_currToken)
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
                switch (_currToken)
                {
                    case TokenType.ADD:
                    case TokenType.SUB:
                        {
                            if (!ExBinaryExp(ExOPDecideArithmetic(_currToken), "ExLogicMlt"))
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
                switch (_currToken)
                {
                    case TokenType.CARTESIAN:
                    case TokenType.EXP:
                    case TokenType.MLT:
                    case TokenType.MMLT:
                    case TokenType.DIV:
                    case TokenType.MOD:
                        {
                            if (!ExBinaryExp(ExOPDecideArithmetic(_currToken), "ExPrefixed"))
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
                case TokenType.MMLT:
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
                switch (_currToken)
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

                            _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(tmp), 0, 0);

                            if (_ExpState._type == ExEType.BASE)
                            {
                                AddBasicOpInstr(OPC.GET);
                                p = _FuncState.TopTarget();
                                _ExpState._type = ExEType.EXPRESSION;
                                _ExpState._pos = p;
                            }
                            else
                            {
                                if (ExRequiresGetter())
                                {
                                    AddBasicOpInstr(OPC.GET);
                                }
                                _ExpState._type = ExEType.OBJECT;
                            }
                            break;
                        }
                    case TokenType.ARR_OPEN:
                        {
                            if (_lexer._prevToken == TokenType.NEWLINE)
                            {
                                AddToErrorMessage("can't break deref OR ',' needed after [exp] = exp decl");
                                return false;
                            }
                            if (!ReadAndSetToken() || !ExExp() || Expect(TokenType.ARR_CLOSE) == null)
                            {
                                return false;
                            }

                            if (_ExpState._type == ExEType.BASE)
                            {
                                AddBasicOpInstr(OPC.GET);
                                p = _FuncState.TopTarget();
                                _ExpState._type = ExEType.EXPRESSION;
                                _ExpState._pos = p;
                            }
                            else
                            {
                                if (ExRequiresGetter())
                                {
                                    AddBasicOpInstr(OPC.GET);
                                }
                                _ExpState._type = ExEType.OBJECT;
                            }
                            break;
                        }
                    case TokenType.MTRS:
                        {
                            if (!ReadAndSetToken())
                            {
                                return false;
                            }

                            switch (_ExpState._type)
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
                                        int s = _FuncState.PopTarget();
                                        _FuncState.AddInstr(OPC.TRANSPOSE, _FuncState.PushTarget(), s, 0, 0);
                                        break;
                                    }
                                case ExEType.OUTER:
                                    {
                                        int t1 = _FuncState.PushTarget();
                                        int t2 = _FuncState.PushTarget();
                                        _FuncState.AddInstr(OPC.GETOUTER, t2, _ExpState._pos, 0, 0);
                                        _FuncState.AddInstr(OPC.TRANSPOSE, t1, t2, 0, 0);
                                        _FuncState.PopTarget();
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
                            int v = _currToken == TokenType.DEC ? -1 : 1;

                            if (!ReadAndSetToken())
                            {
                                return false;
                            }

                            switch (_ExpState._type)
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
                                        int s = _FuncState.PopTarget();
                                        _FuncState.AddInstr(OPC.PINCL, _FuncState.PushTarget(), s, 0, v);
                                        break;
                                    }
                                case ExEType.OUTER:
                                    {
                                        int t1 = _FuncState.PushTarget();
                                        int t2 = _FuncState.PushTarget();
                                        _FuncState.AddInstr(OPC.GETOUTER, t2, _ExpState._pos, 0, 0);
                                        _FuncState.AddInstr(OPC.PINCL, t1, t2, 0, v);
                                        _FuncState.AddInstr(OPC.SETOUTER, t2, _ExpState._pos, t2, 0);
                                        _FuncState.PopTarget();
                                        break;
                                    }
                            }
                            return true;
                        }
                    case TokenType.R_OPEN:
                        {
                            switch (_ExpState._type)
                            {
                                case ExEType.OBJECT:
                                    {
                                        int k_loc = _FuncState.PopTarget();
                                        int obj_loc = _FuncState.PopTarget();
                                        int closure = _FuncState.PushTarget();
                                        int target = _FuncState.PushTarget();
                                        _FuncState.AddInstr(OPC.PREPCALL, closure, k_loc, obj_loc, target);
                                        break;
                                    }
                                case ExEType.BASE:
                                    {
                                        _FuncState.AddInstr(OPC.MOVE, _FuncState.PushTarget(), 0, 0, 0);
                                        break;
                                    }
                                case ExEType.OUTER:
                                    {
                                        _FuncState.AddInstr(OPC.GETOUTER, _FuncState.PushTarget(), _ExpState._pos, 0, 0);
                                        _FuncState.AddInstr(OPC.MOVE, _FuncState.PushTarget(), 0, 0, 0);
                                        break;
                                    }
                                default:
                                    {
                                        _FuncState.AddInstr(OPC.MOVE, _FuncState.PushTarget(), 0, 0, 0);
                                        break;
                                    }
                            }
                            _ExpState._type = ExEType.EXPRESSION;
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
                                int k_loc = _FuncState.PopTarget();
                                int obj_loc = _FuncState.PopTarget();
                                int closure = _FuncState.PushTarget();
                                int target = _FuncState.PushTarget();

                                _FuncState.AddInstr(OPC.PREPCALL, closure, k_loc, obj_loc, target);

                                _ExpState._type = ExEType.EXPRESSION;

                                int st = _FuncState.PopTarget();
                                int cl = _FuncState.PopTarget();

                                _FuncState.AddInstr(OPC.CALL, _FuncState.PushTarget(), cl, st, 1);
                            }
                            return true;
                        }
                }
            }
        }

        public bool ExFactor(ref int pos, ref bool macro)
        {
            _ExpState._type = ExEType.EXPRESSION;

            switch (_currToken)
            {
                case TokenType.LITERAL:
                    {
                        _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(_FuncState.CreateString(_lexer.str_val)), 0, 0);

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
                        _FuncState.AddInstr(OPC.GETBASE, _FuncState.PushTarget(), 0, 0, 0);

                        _ExpState._type = ExEType.BASE;
                        _ExpState._pos = _FuncState.TopTarget();
                        pos = _ExpState._pos;
                        return true;
                    }
                case TokenType.IDENTIFIER:
                case TokenType.CONSTRUCTOR:
                case TokenType.THIS:
                    {
                        ExObject idx = new();
                        ExObject c = new();
                        switch (_currToken)
                        {
                            case TokenType.IDENTIFIER:
                                {
                                    idx = _FuncState.CreateString(_lexer.str_val);
                                    break;
                                }
                            case TokenType.THIS:
                                {
                                    idx = _FuncState.CreateString(ExMat._THIS);
                                    break;
                                }
                            case TokenType.CONSTRUCTOR:
                                {
                                    idx = _FuncState.CreateString(ExMat._CONSTRUCTOR);
                                    break;
                                }
                        }

                        int p;
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }

                        if (_FuncState.IsBlockMacro(idx.GetString()))
                        {
                            _FuncState.PushTarget(0);
                            _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(idx), 0, 0);
                            //macro = true;
                            _ExpState._type = ExEType.OBJECT;
                        }
                        else if (_FuncState.IsMacro(idx) && !_FuncState.IsFuncMacro(idx))
                        {
                            _FuncState.PushTarget(0);
                            _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(idx), 0, 0);
                            macro = true;
                            _ExpState._type = ExEType.OBJECT;
                        }
                        else if ((p = _FuncState.GetLocal(idx)) != -1)
                        {
                            _FuncState.PushTarget(p);
                            _ExpState._type = ExEType.VAR;
                            _ExpState._pos = p;
                        }
                        else if ((p = _FuncState.GetOuter(idx)) != -1)
                        {
                            if (ExRequiresGetter())
                            {
                                _ExpState._pos = _FuncState.PushTarget();
                                _FuncState.AddInstr(OPC.GETOUTER, _ExpState._pos, p, 0, 0);
                            }
                            else
                            {
                                _ExpState._type = ExEType.OUTER;
                                _ExpState._pos = p;
                            }
                        }
                        else if (_FuncState.IsConstArg(idx._val.s_String, ref c))
                        {
                            ExObject cval = c;
                            _ExpState._pos = _FuncState.PushTarget();

                            switch (cval._type)
                            {
                                case ExObjType.INTEGER:
                                    {
                                        AddIntConstLoadInstr((int)cval._val.i_Int, _ExpState._pos);
                                        break;
                                    }
                                case ExObjType.FLOAT:
                                    {
                                        AddFloatConstLoadInstr(new FloatInt() { f = cval._val.f_Float }.i, _ExpState._pos);
                                        break;
                                    }
                                default:
                                    {
                                        _FuncState.AddInstr(OPC.LOAD, _ExpState._pos, (int)_FuncState.GetConst(cval), 0, 0);
                                        break;
                                    }
                            }
                            _ExpState._type = ExEType.EXPRESSION;
                        }
                        else
                        {
                            _FuncState.PushTarget(0);
                            _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(idx), 0, 0);

                            if (ExRequiresGetter())
                            {
                                AddBasicOpInstr(OPC.GET);
                            }
                            _ExpState._type = ExEType.OBJECT;
                        }
                        pos = _ExpState._pos;
                        return true;
                    }
                case TokenType.GLB:
                    {
                        _FuncState.AddInstr(OPC.LOAD_ROOT, _FuncState.PushTarget(), 0, 0, 0);
                        _ExpState._type = ExEType.OBJECT;
                        _currToken = TokenType.DOT;
                        _ExpState._pos = -1;
                        pos = _ExpState._pos;
                        return true;
                    }
                case TokenType.DEFAULT:
                case TokenType.NULL:
                    {
                        _FuncState.AddInstr(OPC.LOAD_NULL, _FuncState.PushTarget(), 1, _currToken == TokenType.DEFAULT ? 1 : 0, 0);
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.MACROPARAM_NUM:
                case TokenType.MACROPARAM_STR:
                    {
                        _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(_FuncState.CreateString(_lexer.str_val)), 0, 0);

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.INTEGER:
                    {
                        AddIntConstLoadInstr(_lexer.i_val, -1);
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.FLOAT:
                    {
                        AddFloatConstLoadInstr(new FloatInt() { f = _lexer.f_val }.i, -1);
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.TRUE:
                case TokenType.FALSE:
                    {
                        _FuncState.AddInstr(OPC.LOAD_BOOL, _FuncState.PushTarget(), _currToken == TokenType.TRUE ? 1 : 0, 0, 0);

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.SPACE:
                    {
                        AddSpaceConstLoadInstr(_lexer._space, -1);
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.ARR_OPEN:
                    {
                        _FuncState.AddInstr(OPC.NEW_OBJECT, _FuncState.PushTarget(), 0, 0, (int)ExNOT.ARRAY);
                        int p = _FuncState.GetCurrPos();
                        int k = 0;

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        while (_currToken != TokenType.ARR_CLOSE)
                        {
                            if (!ExExp())
                            {
                                return false;
                            }
                            if (_currToken == TokenType.SEP)
                            {
                                if (!ReadAndSetToken())
                                {
                                    return false;
                                }
                            }
                            int v = _FuncState.PopTarget();
                            int a = _FuncState.TopTarget();
                            _FuncState.AddInstr(OPC.ARRAY_APPEND, a, v, (int)ArrayAType.STACK, 0);
                            k++;
                        }
                        _FuncState.SetInstrParam(p, 1, k);
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.CLS_OPEN:
                    {
                        _FuncState.AddInstr(OPC.NEW_OBJECT, _FuncState.PushTarget(), 0, (int)ExNOT.DICT, 0);

                        if (!ReadAndSetToken() || !ParseDictClusterOrClass(TokenType.SEP, TokenType.CLS_CLOSE))
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.LAMBDA:
                case TokenType.FUNCTION:
                    {
                        if (!ExFuncResolveExp(_currToken))
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
                        switch (_currToken)
                        {
                            case TokenType.INTEGER:
                                {
                                    AddIntConstLoadInstr(-_lexer.i_val, -1);
                                    if (!ReadAndSetToken())
                                    {
                                        return false;
                                    }
                                    break;
                                }
                            case TokenType.FLOAT:
                                {
                                    AddFloatConstLoadInstr(new FloatInt() { f = -_lexer.f_val }.i, -1);
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
                case TokenType.TIL:
                    {
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        if (_currToken == TokenType.INTEGER)
                        {
                            AddIntConstLoadInstr(~_lexer.i_val, -1);
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
                        if (!ExPrefixedIncDec(_currToken))
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.R_OPEN:
                    {
                        if (!ReadAndSetToken()
                            || !ExSepExp()
                            || Expect(TokenType.R_CLOSE) == null)
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
                case TokenType.MACROSTART:
                    {
                        AddToErrorMessage("macros can only be defined on new lines");
                        return false;
                    }
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

            es = _ExpState.Copy();
            _ExpState.stop_deref = true;

            if (!ExPrefixed())
            {
                return false;
            }

            if (_ExpState._type == ExEType.EXPRESSION)
            {
                AddToErrorMessage("cant 'delete' and expression");
                return false;
            }

            if (_ExpState._type == ExEType.OBJECT || _ExpState._type == ExEType.BASE)
            {
                AddBasicOpInstr(OPC.DELETE);
            }
            else
            {
                AddToErrorMessage("can't delete an outer local variable");
                return false;
            }

            _ExpState = es;
            return true;
        }

        public bool ExRequiresGetter()
        {
            switch (_currToken)
            {
                case TokenType.R_OPEN:
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
            return !_ExpState.stop_deref
                   || (_ExpState.stop_deref && (_currToken == TokenType.DOT || _currToken == TokenType.ARR_OPEN));
        }

        public bool ExPrefixedIncDec(TokenType typ)
        {
            ExEState eState;
            int v = typ == TokenType.DEC ? -1 : 1;

            if (!ReadAndSetToken())
            {
                return false;
            }

            eState = _ExpState.Copy();
            _ExpState.stop_deref = true;

            if (!ExPrefixed())
            {
                return false;
            }

            switch (_ExpState._type)
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
                        int s = _FuncState.TopTarget();
                        _FuncState.AddInstr(OPC.INCL, s, s, 0, v);
                        break;
                    }
                case ExEType.OUTER:
                    {
                        int tmp = _FuncState.PushTarget();
                        _FuncState.AddInstr(OPC.GETOUTER, tmp, _ExpState._pos, 0, 0);
                        _FuncState.AddInstr(OPC.INCL, tmp, tmp, 0, _ExpState._pos);
                        _FuncState.AddInstr(OPC.SETOUTER, tmp, _ExpState._pos, 0, tmp);
                        break;
                    }
            }
            _ExpState = eState;
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
                if (_currToken != TokenType.SEP)
                {
                    break;
                }

                _FuncState.PopTarget();

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
                p = _FuncState.PushTarget();
            }
            ExObject tspc = new() { _type = ExObjType.SPACE };
            tspc._val.c_Space = _lexer._space;
            _FuncState.AddInstr(OPC.LOAD_SPACE, p, (int)_FuncState.GetConst(tspc), 0, 0);
        }

        public void AddIntConstLoadInstr(long cval, int p)
        {
            if (p < 0)
            {
                p = _FuncState.PushTarget();
            }
            _FuncState.AddInstr(OPC.LOAD_INT, p, cval, 0, 0);
        }
        public void AddFloatConstLoadInstr(long cval, int p)
        {
            if (p < 0)
            {
                p = _FuncState.PushTarget();
            }

            _FuncState.AddInstr(OPC.LOAD_FLOAT, p, cval, 0, 0);
        }

        public void AddBasicDerefInstr(OPC op)
        {
            int v = _FuncState.PopTarget();
            int k = _FuncState.PopTarget();
            int s = _FuncState.PopTarget();
            _FuncState.AddInstr(op, _FuncState.PushTarget(), s, k, v);
        }

        public void AddBasicOpInstr(OPC op, int last_arg = 0)
        {
            int arg2 = _FuncState.PopTarget();
            int arg1 = _FuncState.PopTarget();
            _FuncState.AddInstr(op, _FuncState.PushTarget(), arg1, arg2, last_arg);
        }

        public void AddCompoundOpInstr(TokenType typ, ExEType etyp, int pos)
        {
            switch (etyp)
            {
                case ExEType.VAR:
                    {
                        int s = _FuncState.PopTarget();
                        int d = _FuncState.PopTarget();
                        _FuncState.PushTarget(d);

                        _FuncState.AddInstr(ExOPDecideArithmetic(typ), d, s, d, 0);
                        _FuncState._not_snoozed = false;
                        break;
                    }
                case ExEType.BASE:
                case ExEType.OBJECT:
                    {
                        int v = _FuncState.PopTarget();
                        int k = _FuncState.PopTarget();
                        int s = _FuncState.PopTarget();
                        _FuncState.AddInstr(OPC.CMP_ARTH, _FuncState.PushTarget(), (s << 16) | v, k, ExOPDecideArithmeticInt(typ));
                        break;
                    }
                case ExEType.OUTER:
                    {
                        int v = _FuncState.TopTarget();
                        int tmp = _FuncState.PushTarget();

                        _FuncState.AddInstr(OPC.GETOUTER, tmp, pos, 0, 0);
                        _FuncState.AddInstr(ExOPDecideArithmetic(typ), tmp, v, tmp, 0);
                        _FuncState.AddInstr(OPC.SETOUTER, tmp, pos, tmp, 0);

                        break;
                    }
            }
        }

        public bool ParseDictClusterOrClass(TokenType sep, TokenType end)
        {
            int p = _FuncState.GetCurrPos();
            int n = 0;

            while (_currToken != end)
            {
                bool a_present = false;
                if (sep == TokenType.SMC)
                {
                    if (_currToken == TokenType.A_START)
                    {
                        _FuncState.AddInstr(OPC.NEW_OBJECT, _FuncState.PushTarget(), 0, (int)ExNOT.DICT, 0);

                        if (!ReadAndSetToken() || !ParseDictClusterOrClass(TokenType.SEP, TokenType.A_END))
                        {
                            AddToErrorMessage("failed to parse attribute");
                            return false;
                        }
                        a_present = true;
                    }
                }
                switch (_currToken)
                {
                    case TokenType.FUNCTION:
                    case TokenType.CONSTRUCTOR:
                        {
                            TokenType typ = _currToken;
                            if (!ReadAndSetToken())
                            {
                                return false;
                            }

                            ExObject o = typ == TokenType.FUNCTION ? Expect(TokenType.IDENTIFIER) : _FuncState.CreateString(ExMat._CONSTRUCTOR);

                            if (o == null || Expect(TokenType.R_OPEN) == null)
                            {
                                return false;
                            }

                            _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(o), 0, 0);

                            if (!ExFuncCreate(o))
                            {
                                return false;
                            }

                            _FuncState.AddInstr(OPC.CLOSURE, _FuncState.PushTarget(), _FuncState._funcs.Count - 1, 0, 0);
                            break;
                        }
                    case TokenType.ARR_OPEN:
                        {
                            if (!ReadAndSetToken()
                                || !ExSepExp()
                                || Expect(TokenType.ARR_CLOSE) == null
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
                                _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(o), 0, 0);

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

                            _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(o), 0, 0);

                            if (Expect(TokenType.ASG) == null || !ExExp())
                            {
                                return false;
                            }

                            break;
                        }
                }

                if (_currToken == sep)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                n++;

                int v = _FuncState.PopTarget();
                int k = _FuncState.PopTarget();
                int a = a_present ? _FuncState.PopTarget() : -1;

                if (!((a_present && (a == k - 1)) || !a_present))
                {
                    throw new Exception("attributes present count error");
                }

                int flg = a_present ? (int)ExNewSlotFlag.ATTR : 0; // to-do static flag
                int t = _FuncState.TopTarget();
                if (sep == TokenType.SEP)
                {
                    _FuncState.AddInstr(OPC.NEWSLOT, 985, t, k, v);
                }
                else
                {
                    _FuncState.AddInstr(OPC.NEWSLOTA, flg, t, k, v);
                }
            }

            if (sep == TokenType.SEP)
            {
                _FuncState.SetInstrParam(p, 1, n);
            }

            return ReadAndSetToken();
        }

        public bool ExFuncCall()
        {
            int _this = 1;

            while (_currToken != TokenType.R_CLOSE)
            {
                if (!ExExp())
                {
                    return false;
                }
                TargetLocalMove();
                _this++;
                if (_currToken == TokenType.SEP)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                    if (_currToken == TokenType.R_CLOSE)
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
                _FuncState.PopTarget();
            }

            int st = _FuncState.PopTarget();
            int cl = _FuncState.PopTarget();

            _FuncState.AddInstr(OPC.CALL, _FuncState.PushTarget(), cl, st, _this);
            return true;
        }

        public void TargetLocalMove()
        {
            int t = _FuncState.TopTarget();
            if (_FuncState.IsLocalArg(t))
            {
                t = _FuncState.PopTarget();
                _FuncState.AddInstr(OPC.MOVE, _FuncState.PushTarget(), t, 0, 0);
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
            if (!ReadAndSetToken() || Expect(TokenType.R_OPEN) == null)
            {
                return false;
            }

            ExObject d = new();
            if (!ExRuleCreate(d))
            {
                return false;
            }

            _FuncState.AddInstr(OPC.CLOSURE, _FuncState.PushTarget(), _FuncState._funcs.Count - 1, 1, 0);

            return true;
        }

        public bool ExFuncResolveExp(TokenType typ)
        {
            bool lambda = _currToken == TokenType.LAMBDA;
            if (!ReadAndSetToken() || Expect(TokenType.R_OPEN) == null)
            {
                return false;
            }

            ExObject d = new();
            if (!ExFuncCreate(d, lambda))
            {
                return false;
            }

            _FuncState.AddInstr(OPC.CLOSURE, _FuncState.PushTarget(), _FuncState._funcs.Count - 1, typ == TokenType.FUNCTION ? 0 : 1, 0);

            return true;
        }
        public bool ExClassResolveExp()
        {
            int at = 985;

            if (_currToken == TokenType.A_START)
            {
                if (!ReadAndSetToken())
                {
                    return false;
                }
                _FuncState.AddInstr(OPC.NEW_OBJECT, _FuncState.PushTarget(), 0, (int)ExNOT.DICT, 0);

                if (!ParseDictClusterOrClass(TokenType.SEP, TokenType.A_END))
                {
                    return false;
                }

                at = _FuncState.TopTarget();
            }

            if (Expect(TokenType.CLS_OPEN) == null)
            {
                return false;
            }

            if (at != 985)
            {
                _FuncState.PopTarget();
            }

            _FuncState.AddInstr(OPC.NEW_OBJECT, _FuncState.PushTarget(), -1, at, (int)ExNOT.CLASS);

            return ParseDictClusterOrClass(TokenType.SMC, TokenType.CLS_CLOSE);
        }

        public bool ExMacroCreate(ExObject o, bool isfunc = false)
        {
            ExFState f_state = _FuncState.PushChildState(_VM._sState);
            f_state._name = o;

            ExObject pname;
            f_state.AddParam(_FuncState.CreateString(ExMat._THIS));
            f_state._source = new(_source);

            if (isfunc && !ReadAndSetToken())
            {
                return false;
            }

            while (isfunc && _currToken != TokenType.R_CLOSE)
            {
                if ((pname = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                f_state.AddParam(pname);

                if (_currToken == TokenType.SEP)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                else if (_currToken != TokenType.R_CLOSE)
                {
                    if (_currToken == TokenType.ASG)
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

            if (isfunc && Expect(TokenType.R_CLOSE) == null)
            {
                return false;
            }

            ExFState tmp = _FuncState.Copy();
            _FuncState = f_state;

            if (!_inblockmacro)
            {
                while (_currToken != TokenType.NEWLINE
                     && _currToken != TokenType.ENDLINE
                     && _currToken != TokenType.MACROEND
                     && _currToken != TokenType.UNKNOWN)
                {
                    if (!ProcessStatements(true))
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

                f_state.AddInstr(OPC.RETURN, 1, _FuncState.PopTarget(), 0, 0);

                f_state.AddLineInfo(_lexer._prevToken == TokenType.NEWLINE ? _lexer._lastTokenLine : _lexer._currLine, _lineinfo, true);

            }
            else
            {
                // TO-DO
                f_state.AddInstr(OPC.RETURNMACRO, 1, _FuncState.PopTarget(), 0, 0);
            }

            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExFuncPro fpro = f_state.CreatePrototype();
            fpro.type = ExClosureType.MACRO;

            _FuncState = tmp;
            _FuncState._funcs.Add(fpro);
            _FuncState.PopChildState();

            return true;
        }

        private void FixAfterMacro(ExLexer old_lex, TokenType old_currToken, string old_src)
        {
            _lexer = old_lex;
            _currToken = old_currToken;
            _source = old_src;
            _inblockmacro = false;
        }

        public bool ProcessMacroBlockStatement()
        {
            if (true)    // TO-DO
            {
                return false;
            }
#pragma warning disable CS0162 // Unreachable code detected
            ExObject idx;
#pragma warning restore CS0162 // Unreachable code detected

            ExLexer old_lex = _lexer;
            TokenType old_currToken = _currToken;
            string old_src = _source;

            _lexer = new(_lexer.m_block) { m_params = _lexer.m_params, m_block = _lexer.m_block };

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

            _FuncState.PushTarget(0);
            _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(idx), 0, 0);
            bool isfunc = _currToken == TokenType.R_OPEN;

            _inblockmacro = true;

            if (!ExMacroCreate(idx, isfunc))
            {
                FixAfterMacro(old_lex, old_currToken, old_src);
                return false;
            }

            if (_FuncState.IsBlockMacro(idx.GetString()))
            {
                AddToErrorMessage("macro '" + idx.GetString() + "' already exists!");
                FixAfterMacro(old_lex, old_currToken, old_src);
                return false;
            }
            else
            {
                _FuncState.AddBlockMacro(idx.GetString(), new() { name = idx.GetString(), source = _lexer.m_block, _params = _lexer.m_params });
            }

            _FuncState.AddInstr(OPC.CLOSURE, _FuncState.PushTarget(), _FuncState._funcs.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            _FuncState.PopTarget();

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

            _FuncState.PushTarget(0);
            _FuncState.AddInstr(OPC.LOAD, _FuncState.PushTarget(), (int)_FuncState.GetConst(idx), 0, 0);
            bool isfunc = _currToken == TokenType.R_OPEN;
            if (!ExMacroCreate(idx, isfunc))
            {
                return false;
            }

            if (!_FuncState.AddMacro(idx, isfunc, true))   // TO-DO stop using forced param
            {
                AddToErrorMessage("macro " + idx.GetString() + " already exists");
                return false;
            }

            _FuncState.AddInstr(OPC.CLOSURE, _FuncState.PushTarget(), _FuncState._funcs.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            _FuncState.PopTarget();

            return true;
        }

        public bool ExFuncCreate(ExObject o, bool lambda = false)
        {
            ExFState f_state = _FuncState.PushChildState(_VM._sState);
            f_state._name = o;

            ExObject pname;
            f_state.AddParam(_FuncState.CreateString(ExMat._THIS));
            f_state._source = new(_source);

            int def_param_count = 0;

            while (_currToken != TokenType.R_CLOSE)
            {
                if ((pname = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                f_state.AddParam(pname);

                if (_currToken == TokenType.ASG)
                {
                    if (!ReadAndSetToken() || !ExExp())
                    {
                        return false;
                    }

                    f_state.AddDefParam(_FuncState.TopTarget());
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

                if (_currToken == TokenType.SEP)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                else if (_currToken != TokenType.R_CLOSE)
                {
                    AddToErrorMessage("expected ')' or ',' for function declaration");
                    return false;
                }
            }

            if (Expect(TokenType.R_CLOSE) == null)
            {
                return false;
            }

            for (int i = 0; i < def_param_count; i++)
            {
                _FuncState.PopTarget();
            }

            ExFState tmp = _FuncState.Copy();
            _FuncState = f_state;

            if (lambda)
            {
                if (!ExExp())
                {
                    return false;
                }
                f_state.AddInstr(OPC.RETURN, 1, _FuncState.PopTarget(), 0, 0);
            }
            else
            {
                if (!ProcessStatement(false))
                {
                    return false;
                }
            }

            f_state.AddLineInfo(_lexer._prevToken == TokenType.NEWLINE ? _lexer._lastTokenLine : _lexer._currLine, _lineinfo, true);
            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExFuncPro fpro = f_state.CreatePrototype();

            _FuncState = tmp;
            _FuncState._funcs.Add(fpro);
            _FuncState.PopChildState();

            return true;
        }

        public bool ExOpUnary(OPC op)
        {
            if (!ExPrefixed())
            {
                return false;
            }
            int s = _FuncState.PopTarget();
            _FuncState.AddInstr(op, _FuncState.PushTarget(), s, 0, 0);

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
