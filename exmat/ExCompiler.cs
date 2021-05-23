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

    public class ExCompiler
    {
        private ExVM _VM;
        private string _source;

        private ExEState _Estate = new();
        private ExFState _Fstate;

        private ExLexer _lexer;
        private TokenType _currToken;

        private readonly bool _lineinfo;
        private bool _inblockmacro;

        private ExScope _scope = new();

        public string _error;


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
            _scope.stack_size = _Fstate.GetLocalStackSize();
            _scope.outers = _Fstate._nouters;
            return old;
        }

        public void ResolveScopeOuters()
        {
            if (_Fstate.GetLocalStackSize() != _scope.stack_size)
            {
                if (_Fstate.GetOuterSize(_scope.stack_size) != 0)
                {
                    _Fstate.AddInstr(OPC.CLOSE, 0, _scope.stack_size, 0, 0);
                }
            }
        }

        public void ReleaseScope(ExScope old, bool close = true)
        {
            int old_nout = _Fstate._nouters;
            if (_Fstate.GetLocalStackSize() != _scope.stack_size)
            {
                _Fstate.SetLocalStackSize(_scope.stack_size);
                if (close && old_nout != _Fstate._nouters)
                {
                    _Fstate.AddInstr(OPC.CLOSE, 0, _scope.stack_size, 0, 0);
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

        public bool Compile(ExVM vm, string src, ref ExObjectPtr o)
        {
            _VM = vm;
            _source = src;
            _lexer = new ExLexer(src);

            bool state = Compile(ref o);
            vm.n_return = _Fstate.n_statement;

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

        public bool Compile(ref ExObjectPtr o)
        {
            ExFState fst = new(_VM._sState, null);
            fst._name = _VM.CreateString("main");
            _Fstate = fst;

            _Fstate.AddParam(_Fstate.CreateString("this"));
            _Fstate.AddParam(_Fstate.CreateString("vargv"));
            _Fstate._pvars = true;
            _Fstate._source = new(_source);

            int s_size = _Fstate.GetLocalStackSize();

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
            _Fstate.n_statement = s_count;

            _Fstate.SetLocalStackSize(s_size);
            _Fstate.AddLineInfo(_lexer._currLine, _lineinfo, true);
            _Fstate.AddInstr(OPC.RETURN, 1, _Fstate._localvs.Count + _Fstate._localinfos.Count, 0, 0);   //0,1 = root, main | 2 == stackbase == this | 3: varg | 4: result
            _Fstate.SetLocalStackSize(0);

            o._val._FuncPro = _Fstate.CreatePrototype();

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

        public ExObjectPtr Expect(TokenType typ)
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

            ExObjectPtr res = new();
            switch (typ)
            {
                case TokenType.IDENTIFIER:
                    {
                        res = _Fstate.CreateString(_lexer.str_val);
                        break;
                    }
                case TokenType.LITERAL:
                    {
                        res = _Fstate.CreateString(_lexer.str_val, _lexer._aStr.Length - 1);
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
            _Fstate.AddLineInfo(_lexer._currLine, _lineinfo, false);
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
                        ProcessForeachStatement();
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
                case TokenType.RETURN:
                    {
                        OPC op = OPC.RETURN;
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        if (!IsEOS())
                        {
                            int rexp = _Fstate.GetCurrPos() + 1;
                            if (!ExSepExp())
                            {
                                return false;
                            }
                            // TO-DO trap count check and pop
                            _Fstate._returnE = rexp;
                            _Fstate.AddInstr(op, 1, _Fstate.PopTarget(), _Fstate.GetLocalStackSize(), 0);
                        }
                        else
                        {
                            _Fstate._returnE = -1;
                            _Fstate.AddInstr(op, 985, 0, _Fstate.GetLocalStackSize(), 0);
                        }
                        break;
                    }
                case TokenType.BREAK:
                    {
                        if (_Fstate._breaktargs.Count <= 0)
                        {
                            AddToErrorMessage("'break' has to be in a breakable block");
                            return false;
                        }

                        if (_Fstate._breaktargs[^1] > 0)
                        {
                            _Fstate.AddInstr(OPC.TRAPPOP, _Fstate._breaktargs[^1], 0, 0, 0);
                        }

                        DoOuterControl();
                        _Fstate.AddInstr(OPC.JMP, 0, -1234, 0, 0);
                        _Fstate._breaks.Add(_Fstate.GetCurrPos());

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.CONTINUE:
                    {
                        if (_Fstate._continuetargs.Count <= 0)
                        {
                            AddToErrorMessage("'continue' has to be in a breakable block");
                            return false;
                        }

                        if (_Fstate._continuetargs[^1] > 0)
                        {
                            _Fstate.AddInstr(OPC.TRAPPOP, _Fstate._continuetargs[^1], 0, 0, 0);
                        }

                        DoOuterControl();
                        _Fstate.AddInstr(OPC.JMP, 0, -1234, 0, 0);
                        _Fstate._continues.Add(_Fstate.GetCurrPos());

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
                            _Fstate.DiscardTopTarget();
                        }
                        break;
                    }
            }
            _Fstate._not_snoozed = false;
            return true;
        }

        public void DoOuterControl()
        {
            if (_Fstate.GetLocalStackSize() != _scope.stack_size)
            {
                if (_Fstate.GetOuterSize(_scope.stack_size) > 0)
                {
                    _Fstate.AddInstr(OPC.CLOSE, 0, _scope.stack_size, 0, 0);
                }
            }
        }
        public List<int> CreateBreakableBlock()
        {
            _Fstate._breaktargs.Add(0);
            _Fstate._continuetargs.Add(0);
            return new(2) { _Fstate._breaks.Count, _Fstate._continues.Count };
        }

        public void ReleaseBreakableBlock(List<int> bc, int t)
        {
            bc[0] = _Fstate._breaks.Count - bc[0];
            bc[1] = _Fstate._continues.Count - bc[1];

            if (bc[0] > 0)
            {
                DoBreakControl(_Fstate, bc[0]);
            }

            if (bc[1] > 0)
            {
                DoContinueControl(_Fstate, bc[1], t);
            }

            if (_Fstate._breaks.Count > 0)
            {
                _Fstate._breaks.RemoveAt(_Fstate._breaks.Count - 1);
            }
            if (_Fstate._continues.Count > 0)
            {
                _Fstate._continues.RemoveAt(_Fstate._continues.Count - 1);
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

            _Fstate.AddInstr(OPC.JZ, _Fstate.PopTarget(), 0, 0, 0);
            int jnpos = _Fstate.GetCurrPos();

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
            int epos = _Fstate.GetCurrPos();
            if (_currToken == TokenType.ELSE)
            {
                b_else = true;

                old = CreateScope();
                _Fstate.AddInstr(OPC.JMP, 0, 0, 0, 0);
                jpos = _Fstate.GetCurrPos();

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

                _Fstate.SetInstrParam(jpos, 1, _Fstate.GetCurrPos() - jpos);
            }
            _Fstate.SetInstrParam(jnpos, 1, epos - jnpos + (b_else ? 1 : 0));
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
                _Fstate.PopTarget();
            }

            if (Expect(TokenType.SMC) == null)
            {
                return false;
            }

            _Fstate._not_snoozed = false;

            int jpos = _Fstate.GetCurrPos();
            int jzpos = -1;

            if (_currToken != TokenType.SMC)
            {
                if (!ExSepExp())
                {
                    return false;
                }
                _Fstate.AddInstr(OPC.JZ, _Fstate.PopTarget(), 0, 0, 0);
                jzpos = _Fstate.GetCurrPos();
            }

            if (Expect(TokenType.SMC) == null)
            {
                return false;
            }
            _Fstate._not_snoozed = false;

            int estart = _Fstate.GetCurrPos() + 1;
            if (_currToken != TokenType.R_CLOSE)
            {
                if (!ExSepExp())
                {
                    return false;
                }
                _Fstate.PopTarget();
            }

            if (Expect(TokenType.R_CLOSE) == null)
            {
                return false;
            }

            _Fstate._not_snoozed = false;

            int eend = _Fstate.GetCurrPos();
            int esize = eend - estart + 1;
            List<ExInstr> instrs = null;

            if (esize > 0)
            {
                instrs = new(esize);
                int n_instr = _Fstate._instructions.Count;
                for (int i = 0; i < esize; i++)
                {
                    instrs.Add(_Fstate._instructions[estart + i]);
                }
                for (int i = 0; i < esize; i++)
                {
                    _Fstate._instructions.RemoveAt(_Fstate._instructions.Count - 1);
                }
            }

            List<int> bc = CreateBreakableBlock();

            if (!ProcessStatement())
            {
                return false;
            }
            int ctarg = _Fstate.GetCurrPos();

            if (esize > 0)
            {
                for (int i = 0; i < esize; i++)
                {
                    _Fstate.AddInstr(instrs[i]);
                }
            }

            _Fstate.AddInstr(OPC.JMP, 0, jpos - _Fstate.GetCurrPos() - 1, 0, 0);

            if (jzpos > 0)
            {
                _Fstate.SetInstrParam(jzpos, 1, _Fstate.GetCurrPos() - jzpos);
            }

            ReleaseScope(scp);
            ReleaseBreakableBlock(bc, ctarg);

            return true;
        }

        public static void ProcessForeachStatement()
        {

        }

        public bool ExClusterCreate(ExObjectPtr o)
        {
            ExFState f_state = _Fstate.PushChildState(_VM._sState);
            f_state._name = o;

            ExObjectPtr pname;
            f_state.AddParam(_Fstate.CreateString("this"));
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

                if (_currToken == TokenType.COL)
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }

                    if (_currToken != TokenType.SPACE)
                    {
                        AddToErrorMessage("expected a constant SPACE for parameter " + pcount + "'s domain");
                        return false;
                    }

                    AddSpaceConstLoadInstr(_lexer._space, -1);
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }

                    f_state.AddDefParam(_Fstate.TopTarget());
                }
                else // TO-DO add = for referencing global and do get ops
                {
                    AddToErrorMessage("expected ':' for a domain reference");
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
                _Fstate.PopTarget();
            }

            ExFState tmp = _Fstate.Copy();
            _Fstate = f_state;

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
            _Fstate.AddInstr(OPC.JZ, _Fstate.PopTarget(), 0, 0, 0);

            int jzp = _Fstate.GetCurrPos();
            int t = _Fstate.PushTarget();

            if (!ReadAndSetToken() || !ExExp())
            {
                return false;
            }

            int f = _Fstate.PopTarget();
            if (t != f)
            {
                _Fstate.AddInstr(OPC.MOVE, t, f, 0, 0);
            }
            int end_f = _Fstate.GetCurrPos();

            _Fstate.AddInstr(OPC.JMP, 0, 0, 0, 0);

            int jmp = _Fstate.GetCurrPos();

            _Fstate.AddInstr(OPC.LOAD_BOOL, _Fstate.PushTarget(), 0, 0, 0);

            int s = _Fstate.PopTarget();
            if (t != s)
            {
                _Fstate.AddInstr(OPC.MOVE, t, s, 0, 0);
            }

            _Fstate.SetInstrParam(jmp, 1, _Fstate.GetCurrPos() - jmp);
            _Fstate.SetInstrParam(jzp, 1, end_f - jzp + 1);
            _Fstate._not_snoozed = false;
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

            f_state.AddInstr(OPC.RETURN, 1, _Fstate.PopTarget(), 0, 0);
            f_state.AddLineInfo(_lexer._prevToken == TokenType.NEWLINE ? _lexer._lastTokenLine : _lexer._currLine, _lineinfo, true);
            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExFuncPro fpro = f_state.CreatePrototype();
            fpro.is_cluster = true;

            _Fstate = tmp;
            _Fstate._funcs.Add(fpro);
            _Fstate.PopChildState();

            return true;
        }

        public bool ProcessClusterAsgStatement()
        {
            ExObjectPtr v;
            if (!ReadAndSetToken() || (v = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            _Fstate.PushTarget(0);
            _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(v), 0, 0);

            if (Expect(TokenType.CLS_OPEN) == null || !ExClusterCreate(v))
            {
                return false;
            }

            _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            _Fstate.PopTarget();

            return true;
        }

        public bool ExRuleCreate(ExObjectPtr o)
        {
            ExFState f_state = _Fstate.PushChildState(_VM._sState);
            f_state._name = o;

            ExObject pname;
            f_state.AddParam(_Fstate.CreateString("this"));
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

            ExFState tmp = _Fstate.Copy();
            _Fstate = f_state;

            if (!ExExp())
            {
                return false;
            }
            f_state.AddInstr(OPC.RETURNBOOL, 1, _Fstate.PopTarget(), 0, 0);

            f_state.AddLineInfo(_lexer._prevToken == TokenType.NEWLINE ? _lexer._lastTokenLine : _lexer._currLine, _lineinfo, true);
            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExFuncPro fpro = f_state.CreatePrototype();
            fpro.is_rule = true;

            _Fstate = tmp;
            _Fstate._funcs.Add(fpro);
            _Fstate.PopChildState();

            return true;
        }

        public bool ProcessRuleAsgStatement()
        {
            ExObjectPtr v;
            if (!ReadAndSetToken() || (v = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            _Fstate.PushTarget(0);
            _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(v), 0, 0);

            if (Expect(TokenType.R_OPEN) == null || !ExRuleCreate(v))
            {
                return false;
            }

            _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, 0, 0);

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
                    || !ExFuncCreate((ExObjectPtr)v))
                {
                    return false;
                }

                _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, 0, 0);
                _Fstate.PopTarget();
                _Fstate.PushVar(v);
                return true;
            }
            else if (_currToken == TokenType.RULE)
            {
                if (!ReadAndSetToken()
                    || (v = Expect(TokenType.IDENTIFIER)) == null
                    || Expect(TokenType.R_OPEN) == null
                    || !ExRuleCreate((ExObjectPtr)v))
                {
                    return false;
                }

                _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, 0, 0);
                _Fstate.PopTarget();
                _Fstate.PushVar(v);
                return true;
            }
            else if (_currToken == TokenType.CLUSTER)
            {
                if (!ReadAndSetToken()
                    || (v = Expect(TokenType.IDENTIFIER)) == null
                    || Expect(TokenType.CLS_OPEN) == null
                    || !ExClusterCreate((ExObjectPtr)v))
                {
                    return false;
                }

                _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, 0, 0);
                _Fstate.PopTarget();
                _Fstate.PushVar(v);
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

                    int s = _Fstate.PopTarget();
                    int d = _Fstate.PushTarget();

                    if (d != s)
                    {
                        _Fstate.AddInstr(OPC.MOVE, d, s, 0, 0);
                    }
                }
                else
                {
                    _Fstate.AddInstr(OPC.LOAD_NULL, _Fstate.PushTarget(), 1, 0, 0);
                }
                _Fstate.PopTarget();
                _Fstate.PushVar(v);
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
            ExObjectPtr idx;

            if (!ReadAndSetToken() || (idx = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            _Fstate.PushTarget(0);
            _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(idx), 0, 0);

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

                _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(idx), 0, 0);

                if (_currToken == TokenType.GLB)
                {
                    AddBasicOpInstr(OPC.GET);
                }
            }

            if (Expect(TokenType.R_OPEN) == null || !ExFuncCreate(idx))
            {
                return false;
            }

            _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            _Fstate.PopTarget();

            return true;
        }

        public bool ProcessClassStatement()
        {
            ExEState ex;
            if (!ReadAndSetToken())
            {
                return false;
            }
            ex = _Estate.Copy();

            _Estate.stop_deref = true;

            if (!ExPrefixed())
            {
                return false;
            }

            if (_Estate._type == ExEType.EXPRESSION)
            {
                AddToErrorMessage("invalid class name");
                return false;
            }
            else if (_Estate._type == ExEType.OBJECT || _Estate._type == ExEType.BASE)
            {
                if (!ExClassResolveExp())
                {
                    return false;
                }
                AddBasicDerefInstr(OPC.NEWSLOT);
                _Fstate.PopTarget();
            }
            else
            {
                AddToErrorMessage("can't create a class as local");
                return false;
            }
            _Estate = ex;
            return true;
        }

        public bool ExInvokeExp(string ex)
        {
            ExEState eState = _Estate.Copy();
            _Estate._type = ExEType.EXPRESSION;
            _Estate._pos = -1;
            _Estate.stop_deref = false;

            if (!(bool)Type.GetType("ExMat.Compiler.ExCompiler").GetMethod(ex).Invoke(this, null))
            {
                return false;
            }

            _Estate = eState;
            return true;
        }

        public bool ExBinaryExp(OPC op, string func, int lastop = 0)
        {
            if (!ReadAndSetToken() || !ExInvokeExp(func))
            {
                return false;
            }

            int arg1 = _Fstate.PopTarget();
            int arg2 = _Fstate.PopTarget();

            _Fstate.AddInstr(op, _Fstate.PushTarget(), arg1, arg2, lastop);
            return true;
        }

        public bool ExExp()
        {
            ExEState estate = _Estate.Copy();
            _Estate._type = ExEType.EXPRESSION;
            _Estate._pos = -1;
            _Estate.stop_deref = false;

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
                        ExEType etyp = _Estate._type;
                        int pos = _Estate._pos;

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
                                                int s = _Fstate.PopTarget();
                                                int d = _Fstate.TopTarget();
                                                _Fstate.AddInstr(OPC.MOVE, d, s, 0, 0);
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
                                                int s = _Fstate.PopTarget();
                                                int d = _Fstate.PushTarget();
                                                _Fstate.AddInstr(OPC.SETOUTER, d, pos, s, 0);
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

                        _Fstate.AddInstr(OPC.JZ, _Fstate.PopTarget(), 0, 0, 0);

                        int jzp = _Fstate.GetCurrPos();
                        int t = _Fstate.PushTarget();

                        if (!ExExp())
                        {
                            return false;
                        }

                        int f = _Fstate.PopTarget();
                        if (t != f)
                        {
                            _Fstate.AddInstr(OPC.MOVE, t, f, 0, 0);
                        }
                        int end_f = _Fstate.GetCurrPos();

                        _Fstate.AddInstr(OPC.JMP, 0, 0, 0, 0);
                        if (Expect(TokenType.COL) == null)
                        {
                            return false;
                        }

                        int jmp = _Fstate.GetCurrPos();

                        if (!ExExp())
                        {
                            return false;
                        }

                        int s = _Fstate.PopTarget();
                        if (t != s)
                        {
                            _Fstate.AddInstr(OPC.MOVE, t, s, 0, 0);
                        }

                        _Fstate.SetInstrParam(jmp, 1, _Fstate.GetCurrPos() - jmp);
                        _Fstate.SetInstrParam(jzp, 1, end_f - jzp + 1);
                        _Fstate._not_snoozed = false;

                        break;
                    }
            }
            _Estate = estate;
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
                            int f = _Fstate.PopTarget();
                            int t = _Fstate.PushTarget();

                            _Fstate.AddInstr(OPC.OR, t, 0, f, 0);

                            int j = _Fstate.GetCurrPos();

                            if (t != f)
                            {
                                _Fstate.AddInstr(OPC.MOVE, t, f, 0, 0);
                            }

                            if (!ReadAndSetToken() || !ExInvokeExp("ExLogicOr"))
                            {
                                return false;
                            }

                            _Fstate._not_snoozed = false;

                            int s = _Fstate.PopTarget();

                            if (t != s)
                            {
                                _Fstate.AddInstr(OPC.MOVE, t, s, 0, 0);
                            }

                            _Fstate._not_snoozed = false;
                            _Fstate.SetInstrParam(j, 1, _Fstate.GetCurrPos() - j);
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
                            int f = _Fstate.PopTarget();
                            int t = _Fstate.PushTarget();

                            _Fstate.AddInstr(OPC.AND, t, 0, f, 0);

                            int j = _Fstate.GetCurrPos();

                            if (t != f)
                            {
                                _Fstate.AddInstr(OPC.MOVE, t, f, 0, 0);
                            }
                            if (!ReadAndSetToken() || !ExInvokeExp("ExLogicAnd"))
                            {
                                return false;
                            }

                            _Fstate._not_snoozed = false;

                            int s = _Fstate.PopTarget();

                            if (t != s)
                            {
                                _Fstate.AddInstr(OPC.MOVE, t, s, 0, 0);
                            }

                            _Fstate._not_snoozed = false;
                            _Fstate.SetInstrParam(j, 1, _Fstate.GetCurrPos() - j);
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

                            ExObjectPtr tmp;
                            if ((tmp = Expect(TokenType.IDENTIFIER)) == null)
                            {
                                return false;
                            }

                            _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(tmp), 0, 0);

                            if (_Estate._type == ExEType.BASE)
                            {
                                AddBasicOpInstr(OPC.GET);
                                p = _Fstate.TopTarget();
                                _Estate._type = ExEType.EXPRESSION;
                                _Estate._pos = p;
                            }
                            else
                            {
                                if (ExRequiresGetter())
                                {
                                    AddBasicOpInstr(OPC.GET);
                                }
                                _Estate._type = ExEType.OBJECT;
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

                            if (_Estate._type == ExEType.BASE)
                            {
                                AddBasicOpInstr(OPC.GET);
                                p = _Fstate.TopTarget();
                                _Estate._type = ExEType.EXPRESSION;
                                _Estate._pos = p;
                            }
                            else
                            {
                                if (ExRequiresGetter())
                                {
                                    AddBasicOpInstr(OPC.GET);
                                }
                                _Estate._type = ExEType.OBJECT;
                            }
                            break;
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

                            switch (_Estate._type)
                            {
                                case ExEType.EXPRESSION:
                                    {
                                        throw new Exception("can't increment or decrement an expression");
                                    }
                                case ExEType.OBJECT:
                                case ExEType.BASE:
                                    {
                                        AddBasicOpInstr(OPC.PINC, v);
                                        break;
                                    }
                                case ExEType.VAR:
                                    {
                                        int s = _Fstate.PopTarget();
                                        _Fstate.AddInstr(OPC.PINCL, _Fstate.PushTarget(), s, 0, v);
                                        break;
                                    }
                                case ExEType.OUTER:
                                    {
                                        int t1 = _Fstate.PushTarget();
                                        int t2 = _Fstate.PushTarget();
                                        _Fstate.AddInstr(OPC.GETOUTER, t2, _Estate._pos, 0, 0);
                                        _Fstate.AddInstr(OPC.PINCL, t1, t2, 0, v);
                                        _Fstate.AddInstr(OPC.SETOUTER, t2, _Estate._pos, t2, 0);
                                        _Fstate.PopTarget();
                                        break;
                                    }
                            }
                            return true;
                        }
                    case TokenType.R_OPEN:
                        {
                            switch (_Estate._type)
                            {
                                case ExEType.OBJECT:
                                    {
                                        int k_loc = _Fstate.PopTarget();
                                        int obj_loc = _Fstate.PopTarget();
                                        int closure = _Fstate.PushTarget();
                                        int target = _Fstate.PushTarget();
                                        _Fstate.AddInstr(OPC.PREPCALL, closure, k_loc, obj_loc, target);
                                        break;
                                    }
                                case ExEType.BASE:
                                    {
                                        _Fstate.AddInstr(OPC.MOVE, _Fstate.PushTarget(), 0, 0, 0);
                                        break;
                                    }
                                case ExEType.OUTER:
                                    {
                                        _Fstate.AddInstr(OPC.GETOUTER, _Fstate.PushTarget(), _Estate._pos, 0, 0);
                                        _Fstate.AddInstr(OPC.MOVE, _Fstate.PushTarget(), 0, 0, 0);
                                        break;
                                    }
                                default:
                                    {
                                        _Fstate.AddInstr(OPC.MOVE, _Fstate.PushTarget(), 0, 0, 0);
                                        break;
                                    }
                            }
                            _Estate._type = ExEType.EXPRESSION;
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
                                int k_loc = _Fstate.PopTarget();
                                int obj_loc = _Fstate.PopTarget();
                                int closure = _Fstate.PushTarget();
                                int target = _Fstate.PushTarget();

                                _Fstate.AddInstr(OPC.PREPCALL, closure, k_loc, obj_loc, target);

                                _Estate._type = ExEType.EXPRESSION;

                                int st = _Fstate.PopTarget();
                                int cl = _Fstate.PopTarget();

                                _Fstate.AddInstr(OPC.CALL, _Fstate.PushTarget(), cl, st, 1);
                            }
                            return true;
                        }
                }
            }
        }

        public bool ExFactor(ref int pos, ref bool macro)
        {
            _Estate._type = ExEType.EXPRESSION;

            switch (_currToken)
            {
                case TokenType.LITERAL:
                    {
                        _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(_Fstate.CreateString(_lexer.str_val)), 0, 0);

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
                        _Fstate.AddInstr(OPC.GETBASE, _Fstate.PushTarget(), 0, 0, 0);

                        _Estate._type = ExEType.BASE;
                        _Estate._pos = _Fstate.TopTarget();
                        pos = _Estate._pos;
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
                                    idx = _Fstate.CreateString(_lexer.str_val);
                                    break;
                                }
                            case TokenType.THIS:
                                {
                                    idx = _Fstate.CreateString("this");
                                    break;
                                }
                            case TokenType.CONSTRUCTOR:
                                {
                                    idx = _Fstate.CreateString("constructor");
                                    break;
                                }
                        }

                        int p;
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }

                        if (_Fstate.IsBlockMacro(idx.GetString()))
                        {
                            _Fstate.PushTarget(0);
                            _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst((ExObjectPtr)idx), 0, 0);
                            //macro = true;
                            _Estate._type = ExEType.OBJECT;
                        }
                        else if (_Fstate.IsMacro(idx) && !_Fstate.IsFuncMacro(idx))
                        {
                            _Fstate.PushTarget(0);
                            _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst((ExObjectPtr)idx), 0, 0);
                            macro = true;
                            _Estate._type = ExEType.OBJECT;
                        }
                        else if ((p = _Fstate.GetLocal(idx)) != -1)
                        {
                            _Fstate.PushTarget(p);
                            _Estate._type = ExEType.VAR;
                            _Estate._pos = p;
                        }
                        else if ((p = _Fstate.GetOuter(idx)) != -1)
                        {
                            if (ExRequiresGetter())
                            {
                                _Estate._pos = _Fstate.PushTarget();
                                _Fstate.AddInstr(OPC.GETOUTER, _Estate._pos, p, 0, 0);
                            }
                            else
                            {
                                _Estate._type = ExEType.OUTER;
                                _Estate._pos = p;
                            }
                        }
                        else if (_Fstate.IsConstArg(idx._val.s_String, ref c))
                        {
                            ExObjectPtr cval = (ExObjectPtr)c;
                            _Estate._pos = _Fstate.PushTarget();

                            switch (cval._type)
                            {
                                case ExObjType.INTEGER:
                                    {
                                        AddIntConstLoadInstr(cval._val.i_Int, _Estate._pos);
                                        break;
                                    }
                                case ExObjType.FLOAT:
                                    {
                                        AddFloatConstLoadInstr(new FloatInt() { f = cval._val.f_Float }.i, _Estate._pos);
                                        break;
                                    }
                                default:
                                    {
                                        _Fstate.AddInstr(OPC.LOAD, _Estate._pos, _Fstate.GetConst(cval), 0, 0);
                                        break;
                                    }
                            }
                            _Estate._type = ExEType.EXPRESSION;
                        }
                        else
                        {
                            _Fstate.PushTarget(0);
                            _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst((ExObjectPtr)idx), 0, 0);

                            if (ExRequiresGetter())
                            {
                                AddBasicOpInstr(OPC.GET);
                            }
                            _Estate._type = ExEType.OBJECT;
                        }
                        pos = _Estate._pos;
                        return true;
                    }
                case TokenType.GLB:
                    {
                        _Fstate.AddInstr(OPC.LOAD_ROOT, _Fstate.PushTarget(), 0, 0, 0);
                        _Estate._type = ExEType.OBJECT;
                        _currToken = TokenType.DOT;
                        _Estate._pos = -1;
                        pos = _Estate._pos;
                        return true;
                    }
                case TokenType.NULL:
                    {
                        _Fstate.AddInstr(OPC.LOAD_NULL, _Fstate.PushTarget(), 1, 0, 0);
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.MACROPARAM_NUM:
                case TokenType.MACROPARAM_STR:
                    {
                        _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(_Fstate.CreateString(_lexer.str_val)), 0, 0);

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
                        _Fstate.AddInstr(OPC.LOAD_BOOL, _Fstate.PushTarget(), _currToken == TokenType.TRUE ? 1 : 0, 0, 0);

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
                        _Fstate.AddInstr(OPC.NEW_OBJECT, _Fstate.PushTarget(), 0, 0, ExNOT.ARRAY);
                        int p = _Fstate.GetCurrPos();
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
                            int v = _Fstate.PopTarget();
                            int a = _Fstate.TopTarget();
                            _Fstate.AddInstr(OPC.ARRAY_APPEND, a, v, ArrayAType.STACK, 0);
                            k++;
                        }
                        _Fstate.SetInstrParam(p, 1, k);
                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.CLS_OPEN:
                    {
                        _Fstate.AddInstr(OPC.NEW_OBJECT, _Fstate.PushTarget(), 0, ExNOT.DICT, 0);

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

            es = _Estate.Copy();
            _Estate.stop_deref = true;

            if (!ExPrefixed())
            {
                return false;
            }

            if (_Estate._type == ExEType.EXPRESSION)
            {
                AddToErrorMessage("cant 'delete' and expression");
                return false;
            }

            if (_Estate._type == ExEType.OBJECT || _Estate._type == ExEType.BASE)
            {
                AddBasicOpInstr(OPC.DELETE);
            }
            else
            {
                AddToErrorMessage("can't delete an outer local variable");
                return false;
            }

            _Estate = es;
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
            return !_Estate.stop_deref || (_Estate.stop_deref && (_currToken == TokenType.DOT || _currToken == TokenType.ARR_OPEN));
        }

        public bool ExPrefixedIncDec(TokenType typ)
        {
            ExEState eState;
            int v = typ == TokenType.DEC ? -1 : 1;

            if (!ReadAndSetToken())
            {
                return false;
            }

            eState = _Estate.Copy();
            _Estate.stop_deref = true;

            if (!ExPrefixed())
            {
                return false;
            }

            switch (_Estate._type)
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
                        int s = _Fstate.TopTarget();
                        _Fstate.AddInstr(OPC.INCL, s, s, 0, v);
                        break;
                    }
                case ExEType.OUTER:
                    {
                        int tmp = _Fstate.PushTarget();
                        _Fstate.AddInstr(OPC.GETOUTER, tmp, _Estate._pos, 0, 0);
                        _Fstate.AddInstr(OPC.INCL, tmp, tmp, 0, _Estate._pos);
                        _Fstate.AddInstr(OPC.SETOUTER, tmp, _Estate._pos, 0, tmp);
                        break;
                    }
            }
            _Estate = eState;
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

                _Fstate.PopTarget();

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
                p = _Fstate.PushTarget();
            }
            ExObjectPtr tspc = new() { _type = ExObjType.SPACE };
            tspc._val.c_Space = _lexer._space;
            _Fstate.AddInstr(OPC.LOAD_SPACE, p, _Fstate.GetConst(tspc), 0, 0);
        }

        public void AddIntConstLoadInstr(int cval, int p)
        {
            if (p < 0)
            {
                p = _Fstate.PushTarget();
            }
            _Fstate.AddInstr(OPC.LOAD_INT, p, cval, 0, 0);
        }
        public void AddFloatConstLoadInstr(int cval, int p)
        {
            if (p < 0)
            {
                p = _Fstate.PushTarget();
            }

            _Fstate.AddInstr(OPC.LOAD_FLOAT, p, cval, 0, 0);
        }

        public void AddBasicDerefInstr(OPC op)
        {
            int v = _Fstate.PopTarget();
            int k = _Fstate.PopTarget();
            int s = _Fstate.PopTarget();
            _Fstate.AddInstr(op, _Fstate.PushTarget(), s, k, v);
        }

        public void AddBasicOpInstr(OPC op, int last_arg = 0)
        {
            int arg2 = _Fstate.PopTarget();
            int arg1 = _Fstate.PopTarget();
            _Fstate.AddInstr(op, _Fstate.PushTarget(), arg1, arg2, last_arg);
        }

        public void AddCompoundOpInstr(TokenType typ, ExEType etyp, int pos)
        {
            switch (etyp)
            {
                case ExEType.VAR:
                    {
                        int s = _Fstate.PopTarget();
                        int d = _Fstate.PopTarget();
                        _Fstate.PushTarget(d);

                        _Fstate.AddInstr(ExOPDecideArithmetic(typ), d, s, d, 0);
                        _Fstate._not_snoozed = false;
                        break;
                    }
                case ExEType.BASE:
                case ExEType.OBJECT:
                    {
                        int v = _Fstate.PopTarget();
                        int k = _Fstate.PopTarget();
                        int s = _Fstate.PopTarget();
                        _Fstate.AddInstr(OPC.CMP_ARTH, _Fstate.PushTarget(), (s << 16) | v, k, ExOPDecideArithmeticInt(typ));
                        break;
                    }
                case ExEType.OUTER:
                    {
                        int v = _Fstate.TopTarget();
                        int tmp = _Fstate.PushTarget();

                        _Fstate.AddInstr(OPC.GETOUTER, tmp, pos, 0, 0);
                        _Fstate.AddInstr(ExOPDecideArithmetic(typ), tmp, v, tmp, 0);
                        _Fstate.AddInstr(OPC.SETOUTER, tmp, pos, tmp, 0);

                        break;
                    }
            }
        }

        public bool ParseDictClusterOrClass(TokenType sep, TokenType end)
        {
            int p = _Fstate.GetCurrPos();
            int n = 0;

            while (_currToken != end)
            {
                bool a_present = false;
                if (sep == TokenType.SMC)
                {
                    if (_currToken == TokenType.A_START)
                    {
                        _Fstate.AddInstr(OPC.NEW_OBJECT, _Fstate.PushTarget(), 0, ExNOT.DICT, 0);

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

                            ExObjectPtr o = typ == TokenType.FUNCTION ? Expect(TokenType.IDENTIFIER) : _Fstate.CreateString("constructor");

                            if (o == null || Expect(TokenType.R_OPEN) == null)
                            {
                                return false;
                            }

                            _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(o), 0, 0);

                            if (!ExFuncCreate(o))
                            {
                                return false;
                            }

                            _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, 0, 0);
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
                                ExObjectPtr o;
                                if ((o = Expect(TokenType.LITERAL)) == null)
                                {
                                    return false;
                                }
                                _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(o), 0, 0);

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
                            ExObjectPtr o;
                            if ((o = Expect(TokenType.IDENTIFIER)) == null)
                            {
                                return false;
                            }

                            _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(o), 0, 0);

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

                int v = _Fstate.PopTarget();
                int k = _Fstate.PopTarget();
                int a = a_present ? _Fstate.PopTarget() : -1;

                if (!((a_present && (a == k - 1)) || !a_present))
                {
                    throw new Exception("attributes present count error");
                }

                int flg = a_present ? (int)ExNewSlotFlag.ATTR : 0; // to-do static flag
                int t = _Fstate.TopTarget();
                if (sep == TokenType.SEP)
                {
                    _Fstate.AddInstr(OPC.NEWSLOT, 985, t, k, v);
                }
                else
                {
                    _Fstate.AddInstr(OPC.NEWSLOTA, flg, t, k, v);
                }
            }

            if (sep == TokenType.SEP)
            {
                _Fstate.SetInstrParam(p, 1, n);
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
                _Fstate.PopTarget();
            }

            int st = _Fstate.PopTarget();
            int cl = _Fstate.PopTarget();

            _Fstate.AddInstr(OPC.CALL, _Fstate.PushTarget(), cl, st, _this);
            return true;
        }

        public void TargetLocalMove()
        {
            int t = _Fstate.TopTarget();
            if (_Fstate.IsLocalArg(t))
            {
                t = _Fstate.PopTarget();
                _Fstate.AddInstr(OPC.MOVE, _Fstate.PushTarget(), t, 0, 0);
            }
        }

        public bool ExClusterResolveExp()
        {
            if (!ReadAndSetToken() || Expect(TokenType.R_OPEN) == null)
            {
                return false;
            }

            ExObjectPtr d = new();
            if (!ExClusterCreate(d))
            {
                return false;
            }

            _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, 1, 0);

            return true;
        }

        public bool ExRuleResolveExp()
        {
            if (!ReadAndSetToken() || Expect(TokenType.R_OPEN) == null)
            {
                return false;
            }

            ExObjectPtr d = new();
            if (!ExRuleCreate(d))
            {
                return false;
            }

            _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, 1, 0);

            return true;
        }

        public bool ExFuncResolveExp(TokenType typ)
        {
            bool lambda = _currToken == TokenType.LAMBDA;
            if (!ReadAndSetToken() || Expect(TokenType.R_OPEN) == null)
            {
                return false;
            }

            ExObjectPtr d = new();
            if (!ExFuncCreate(d, lambda))
            {
                return false;
            }

            _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, typ == TokenType.FUNCTION ? 0 : 1, 0);

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
                _Fstate.AddInstr(OPC.NEW_OBJECT, _Fstate.PushTarget(), 0, ExNOT.DICT, 0);

                if (!ParseDictClusterOrClass(TokenType.SEP, TokenType.A_END))
                {
                    return false;
                }

                at = _Fstate.TopTarget();
            }

            if (Expect(TokenType.CLS_OPEN) == null)
            {
                return false;
            }

            if (at != 985)
            {
                _Fstate.PopTarget();
            }

            _Fstate.AddInstr(OPC.NEW_OBJECT, _Fstate.PushTarget(), -1, at, ExNOT.CLASS);

            return ParseDictClusterOrClass(TokenType.SMC, TokenType.CLS_CLOSE);
        }

        public bool ExMacroCreate(ExObjectPtr o, bool isfunc = false)
        {
            ExFState f_state = _Fstate.PushChildState(_VM._sState);
            f_state._name = o;

            ExObject pname;
            f_state.AddParam(_Fstate.CreateString("this"));
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

            ExFState tmp = _Fstate.Copy();
            _Fstate = f_state;

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

                f_state.AddInstr(OPC.RETURN, 1, _Fstate.PopTarget(), 0, 0);

                f_state.AddLineInfo(_lexer._prevToken == TokenType.NEWLINE ? _lexer._lastTokenLine : _lexer._currLine, _lineinfo, true);

            }
            else
            {
                // TO-DO
                f_state.AddInstr(OPC.RETURNMACRO, 1, _Fstate.PopTarget(), 0, 0);
            }

            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExFuncPro fpro = f_state.CreatePrototype();
            fpro.is_macro = true;

            _Fstate = tmp;
            _Fstate._funcs.Add(fpro);
            _Fstate.PopChildState();

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
            ExObjectPtr idx;
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

            _Fstate.PushTarget(0);
            _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(idx), 0, 0);
            bool isfunc = _currToken == TokenType.R_OPEN;

            _inblockmacro = true;

            if (!ExMacroCreate(idx, isfunc))
            {
                FixAfterMacro(old_lex, old_currToken, old_src);
                return false;
            }

            if (_Fstate.IsBlockMacro(idx.GetString()))
            {
                AddToErrorMessage("macro '" + idx.GetString() + "' already exists!");
                FixAfterMacro(old_lex, old_currToken, old_src);
                return false;
            }
            else
            {
                _Fstate.AddBlockMacro(idx.GetString(), new() { name = idx.GetString(), source = _lexer.m_block, _params = _lexer.m_params });
            }

            _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            _Fstate.PopTarget();

            FixAfterMacro(old_lex, old_currToken, old_src);
            return true;
        }

        public bool ProcessMacroStatement()
        {
            ExObjectPtr idx;

            if (!ReadAndSetToken() || (idx = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            if (idx.GetString().ToUpper() != idx.GetString())
            {
                AddToErrorMessage("macro names should be all uppercase characters!");
                return false;
            }

            _Fstate.PushTarget(0);
            _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(idx), 0, 0);
            bool isfunc = _currToken == TokenType.R_OPEN;
            if (!ExMacroCreate(idx, isfunc))
            {
                return false;
            }

            if (!_Fstate.AddMacro(idx, isfunc, true))   // TO-DO stop using forced param
            {
                AddToErrorMessage("macro " + idx.GetString() + " already exists");
                return false;
            }

            _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            _Fstate.PopTarget();

            return true;
        }

        public bool ExFuncCreate(ExObjectPtr o, bool lambda = false)
        {
            ExFState f_state = _Fstate.PushChildState(_VM._sState);
            f_state._name = o;

            ExObject pname;
            f_state.AddParam(_Fstate.CreateString("this"));
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

                    f_state.AddDefParam(_Fstate.TopTarget());
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
                _Fstate.PopTarget();
            }

            ExFState tmp = _Fstate.Copy();
            _Fstate = f_state;

            if (lambda)
            {
                if (!ExExp())
                {
                    return false;
                }
                f_state.AddInstr(OPC.RETURN, 1, _Fstate.PopTarget(), 0, 0);
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

            _Fstate = tmp;
            _Fstate._funcs.Add(fpro);
            _Fstate.PopChildState();

            return true;
        }

        public bool ExOpUnary(OPC op)
        {
            if (!ExPrefixed())
            {
                return false;
            }
            int s = _Fstate.PopTarget();
            _Fstate.AddInstr(op, _Fstate.PushTarget(), s, 0, 0);

            return true;
        }

    }
}
