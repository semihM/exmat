using System;
using ExMat.Token;
using ExMat.Lexer;
using ExMat.States;
using ExMat.OPs;
using ExMat.Objects;
using ExMat.VM;
using ExMat.FuncPrototype;
using System.Collections.Generic;

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

        private ExScope _scope = new();

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

        public bool Compile(ExVM vm, string src, ref ExObjectPtr o)
        {
            _VM = vm;
            _source = src;
            _lexer = new ExLexer(src);

            return Compile(ref o);
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

            ReadAndSetToken();
            while (_currToken != TokenType.ENDLINE)
            {
                ProcessStatement();
                if (_lexer._prevToken != TokenType.CLS_CLOSE && _lexer._prevToken != TokenType.SMC)
                {
                    CheckSMC();
                }
            }

            _Fstate.SetLocalStackSize(s_size);
            _Fstate.AddLineInfo(_lexer._currLine, _lineinfo, true);
            _Fstate.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            _Fstate.SetLocalStackSize(0);
            o._val._FuncPro = _Fstate.CreatePrototype();
            return true;
        }

        public void Debug()
        {
            while (!_lexer._reached_end)
            {
                ReadAndSetToken();
            }
        }
        private void ReadAndSetToken() => _currToken = _lexer.Lex();

        public dynamic Expect(TokenType typ)
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
                                expmsg = ExLexer.GetStringForTokenType(typ);
                                break;
                            }
                    }
                    throw new Exception("Expected " + expmsg);
                }
            }

            ExObjectPtr res = null;
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
            ReadAndSetToken();
            return res;
        }

        public bool IsEOS()
        {
            return _lexer._prevToken == TokenType.NEWLINE
               || _currToken == TokenType.ENDLINE
               || _currToken == TokenType.CLS_CLOSE
               || _currToken == TokenType.SMC;
        }

        public void CheckSMC()
        {
            if (_currToken == TokenType.SMC)
            {
                ReadAndSetToken();
            }
            else if (!IsEOS())
            {
                throw new Exception("Expected end of statement");
            }
        }

        public void ProcessStatements()
        {
            while (_currToken != TokenType.CLS_CLOSE)
            {
                ProcessStatement();
                if (_lexer._prevToken != TokenType.CLS_CLOSE && _lexer._prevToken != TokenType.SMC)
                {
                    CheckSMC();
                }
            }
        }

        public void ProcessStatement(bool cl = true)
        {
            _Fstate.AddLineInfo(_lexer._currLine, _lineinfo, false);
            switch (_currToken)
            {
                case TokenType.SMC:
                    {
                        ReadAndSetToken();
                        break;
                    }
                case TokenType.IF:
                    {
                        ProcessIfStatement();
                        break;
                    }
                case TokenType.FOR:
                    {
                        ProcessForStatement();
                        break;
                    }
                case TokenType.FOREACH:
                    {
                        ProcessForeachStatement();
                        break;
                    }
                case TokenType.VAR:
                    {
                        ProcessLocalAsgStatement();
                        break;
                    }
                case TokenType.FUNCTION:
                    {
                        ProcessFunctionStatement();
                        break;
                    }
                case TokenType.CLASS:
                    {
                        ProcessClassStatement();
                        break;
                    }
                case TokenType.RETURN:
                    {
                        OPC op = OPC.RETURN;
                        ReadAndSetToken();
                        if (IsEOS())
                        {
                            int rexp = _Fstate.GetCurrPos() + 1;
                            ExSepExp();
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
                            throw new Exception("'break' has to be in a breakable block");
                        }

                        if (_Fstate._breaktargs[^1] > 0)
                        {
                            _Fstate.AddInstr(OPC.TRAPPOP, _Fstate._breaktargs[^1], 0, 0, 0);
                        }

                        DoOuterControl();
                        _Fstate.AddInstr(OPC.JMP, 0, -1234, 0, 0);
                        _Fstate._breaks.Add(_Fstate.GetCurrPos());

                        ReadAndSetToken();
                        break;
                    }
                case TokenType.CONTINUE:
                    {
                        if (_Fstate._continuetargs.Count <= 0)
                        {
                            throw new Exception("'continue' has to be in a breakable block");
                        }

                        if (_Fstate._continuetargs[^1] > 0)
                        {
                            _Fstate.AddInstr(OPC.TRAPPOP, _Fstate._continuetargs[^1], 0, 0, 0);
                        }

                        DoOuterControl();
                        _Fstate.AddInstr(OPC.JMP, 0, -1234, 0, 0);
                        _Fstate._continues.Add(_Fstate.GetCurrPos());

                        ReadAndSetToken();
                        break;
                    }
                case TokenType.CLS_OPEN:
                    {
                        ExScope scp = CreateScope();

                        ReadAndSetToken();
                        ProcessStatements();

                        Expect(TokenType.CLS_CLOSE);

                        ReleaseScope(scp, cl);
                        break;
                    }
                default:
                    {
                        ExSepExp();
                        _Fstate.DiscardTopTarget();
                        break;
                    }
            }
            _Fstate._not_snoozed = false;
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

        public void ProcessIfStatement()
        {
            int jpos;
            bool b_else = false;
            ReadAndSetToken();

            Expect(TokenType.R_OPEN);
            ExSepExp();
            Expect(TokenType.R_CLOSE);

            _Fstate.AddInstr(OPC.JZ, _Fstate.PopTarget(), 0, 0, 0);
            int jnpos = _Fstate.GetCurrPos();

            ExScope old = CreateScope();
            ProcessStatement();
            if (_currToken != TokenType.CLS_CLOSE && _currToken != TokenType.ELSE)
            {
                CheckSMC();
            }

            ReleaseScope(old);
            int epos = _Fstate.GetCurrPos();
            if (_currToken == TokenType.ELSE)
            {
                b_else = true;

                old = CreateScope();
                _Fstate.AddInstr(OPC.JMP, 0, 0, 0, 0);
                jpos = _Fstate.GetCurrPos();

                ReadAndSetToken();
                ProcessStatement();

                if (_lexer._prevToken != TokenType.CLS_CLOSE)
                {
                    CheckSMC();
                }
                ReleaseScope(old);

                _Fstate.SetInstrParam(jpos, 1, _Fstate.GetCurrPos() - jpos);
            }
            _Fstate.SetInstrParam(jnpos, 1, epos - jnpos + (b_else ? 1 : 0));
        }

        public void ProcessForStatement()
        {
            ReadAndSetToken();

            ExScope scp = CreateScope();
            Expect(TokenType.R_OPEN);

            if (_currToken == TokenType.VAR)
            {
                ProcessLocalAsgStatement();
            }
            else if (_currToken != TokenType.SMC)
            {
                ExSepExp();
                _Fstate.PopTarget();
            }

            Expect(TokenType.SMC);
            _Fstate._not_snoozed = false;

            int jpos = _Fstate.GetCurrPos();
            int jzpos = -1;

            if (_currToken != TokenType.SMC)
            {
                ExSepExp();
                _Fstate.AddInstr(OPC.JZ, _Fstate.PopTarget(), 0, 0, 0);
                jzpos = _Fstate.GetCurrPos();
            }

            Expect(TokenType.SMC);
            _Fstate._not_snoozed = false;

            int estart = _Fstate.GetCurrPos() + 1;
            if (_currToken != TokenType.R_CLOSE)
            {
                ExSepExp();
                _Fstate.PopTarget();
            }

            Expect(TokenType.R_CLOSE);
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

            ProcessStatement();
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
        }

        public static void ProcessForeachStatement()
        {

        }
        public void ProcessLocalAsgStatement()
        {
            ExObject v;
            ReadAndSetToken();

            if (_currToken == TokenType.FUNCTION)
            {
                ReadAndSetToken();
                v = Expect(TokenType.IDENTIFIER);
                ExFuncCreate((ExObjectPtr)v);
                _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, 0, 0);
                return;
            }

            while (true)
            {
                v = Expect(TokenType.IDENTIFIER);
                if (_currToken == TokenType.ASG)
                {
                    ReadAndSetToken();
                    ExExp();

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
                _Fstate.PushLocal(v);
                if (_currToken == TokenType.SEP)
                {
                    ReadAndSetToken();
                }
                else
                {
                    break;
                }
            }
        }
        public static void ProcessFunctionStatement()
        {

        }

        public void ProcessClassStatement()
        {
            ExEState ex;
            ReadAndSetToken();
            ex = _Estate.Copy();

            _Estate.stop_deref = true;

            ExPrefixed();

            if (_Estate._type == ExEType.EXPRESSION)
            {
                throw new Exception("invalid class name");
            }
            else if (_Estate._type == ExEType.OBJECT || _Estate._type == ExEType.BASE)
            {
                ExClassResolveExp();
                AddBasicDerefInstr(OPC.NEWSLOT);
                _Fstate.PopTarget();
            }
            else
            {
                throw new Exception("can't create a class as local");
            }
            _Estate = ex;
        }

        public void ExInvokeExp(string ex)
        {
            ExEState eState = _Estate.Copy();
            _Estate._type = ExEType.EXPRESSION;
            _Estate._pos = -1;
            _Estate.stop_deref = false;

            Type.GetType("ExMat.Compiler.ExCompiler").GetMethod(ex).Invoke(this, null);

            _Estate = eState;
        }

        public void ExBinaryExp(OPC op, string func, int lastop = 0)
        {
            ReadAndSetToken();
            ExInvokeExp(func);

            int arg1 = _Fstate.PopTarget();
            int arg2 = _Fstate.PopTarget();

            _Fstate.AddInstr(op, _Fstate.PushTarget(), arg1, arg2, lastop);
        }

        public void ExExp()
        {
            ExEState estate = _Estate.Copy();
            _Estate._type = ExEType.EXPRESSION;
            _Estate._pos = -1;
            _Estate.stop_deref = false;

            ExLogicOr();

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
                            throw new Exception("can't assing an expression");
                        }
                        else if (etyp == ExEType.BASE)
                        {
                            throw new Exception("can't modify 'base'");
                        }

                        ReadAndSetToken();
                        ExExp();

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
                                        throw new Exception("can't create a local slot");
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
                        ReadAndSetToken();

                        _Fstate.AddInstr(OPC.JZ, _Fstate.PopTarget(), 0, 0, 0);

                        int jzp = _Fstate.GetCurrPos();
                        int t = _Fstate.PushTarget();

                        ExExp();

                        int f = _Fstate.PopTarget();
                        if (t != f)
                        {
                            _Fstate.AddInstr(OPC.MOVE, t, f, 0, 0);
                        }
                        int end_f = _Fstate.GetCurrPos();

                        _Fstate.AddInstr(OPC.JMP, 0, 0, 0, 0);
                        Expect(TokenType.COL);

                        int jmp = _Fstate.GetCurrPos();

                        ExExp();

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
        }

        public void ExLogicOr()
        {
            ExLogicAnd();
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

                            ReadAndSetToken();
                            ExInvokeExp("ExLogicOr");
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
                        return;
                }
            }
        }
        public void ExLogicAnd()
        {
            ExLogicBOr();
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

                            ReadAndSetToken();
                            ExInvokeExp("ExLogicAnd");
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
                        return;
                }
            }
        }
        public void ExLogicBOr()
        {
            ExLogicBXor();
            for (; ; )
            {
                switch (_currToken)
                {
                    case TokenType.BOR:
                        {
                            ExBinaryExp(OPC.BITWISE, "ExLogicBXor", (int)BitOP.OR);
                            break;
                        }
                    default:
                        return;
                }
            }
        }
        public void ExLogicBXor()
        {
            ExLogicBAnd();
            for (; ; )
            {
                switch (_currToken)
                {
                    case TokenType.BXOR:
                        {
                            ExBinaryExp(OPC.BITWISE, "ExLogicBAnd", (int)BitOP.XOR);
                            break;
                        }
                    default:
                        return;
                }
            }
        }
        public void ExLogicBAnd()
        {
            ExLogicEq();
            for (; ; )
            {
                switch (_currToken)
                {
                    case TokenType.BAND:
                        {
                            ExBinaryExp(OPC.BITWISE, "ExLogicEq", (int)BitOP.AND);
                            break;
                        }
                    default:
                        return;
                }
            }
        }
        public void ExLogicEq()
        {
            ExLogicCmp();
            for (; ; )
            {
                switch (_currToken)
                {
                    case TokenType.EQU:
                        {
                            ExBinaryExp(OPC.EQ, "ExLogicCmp");
                            break;
                        }
                    case TokenType.NEQ:
                        {
                            ExBinaryExp(OPC.NEQ, "ExLogicCmp");
                            break;
                        }
                    default:
                        return;
                }
            }
        }
        public void ExLogicCmp()
        {
            ExLogicShift();
            for (; ; )
            {
                switch (_currToken)
                {
                    case TokenType.GRT:
                        {
                            ExBinaryExp(OPC.CMP, "ExLogicShift", (int)CmpOP.GRT);
                            break;
                        }
                    case TokenType.LST:
                        {
                            ExBinaryExp(OPC.CMP, "ExLogicShift", (int)CmpOP.LST);
                            break;
                        }
                    case TokenType.GET:
                        {
                            ExBinaryExp(OPC.CMP, "ExLogicShift", (int)CmpOP.GET);
                            break;
                        }
                    case TokenType.LET:
                        {
                            ExBinaryExp(OPC.CMP, "ExLogicShift", (int)CmpOP.LET);
                            break;
                        }
                    case TokenType.IN:
                        {
                            ExBinaryExp(OPC.EXISTS, "ExLogicShift");
                            break;
                        }
                    case TokenType.INSTANCEOF:
                        {
                            ExBinaryExp(OPC.INSTANCEOF, "ExLogicShift");
                            break;
                        }
                    default:
                        return;
                }
            }
        }
        public void ExLogicShift()
        {
            ExLogicAdd();
            for (; ; )
            {
                switch (_currToken)
                {
                    case TokenType.LSHF:
                        {
                            ExBinaryExp(OPC.BITWISE, "ExLogicAdd", (int)BitOP.SHIFTL);
                            break;
                        }
                    case TokenType.RSHF:
                        {
                            ExBinaryExp(OPC.BITWISE, "ExLogicAdd", (int)BitOP.SHIFTR);
                            break;
                        }
                    default:
                        return;
                }
            }
        }
        public void ExLogicAdd()
        {
            ExLogicMlt();
            for (; ; )
            {
                switch (_currToken)
                {
                    case TokenType.ADD:
                    case TokenType.SUB:
                        {
                            ExBinaryExp(ExOPDecideArithmetic(_currToken), "ExLogicMlt");
                            break;
                        }
                    default:
                        return;
                }
            }
        }
        public void ExLogicMlt()
        {
            ExPrefixed();
            for (; ; )
            {
                switch (_currToken)
                {
                    case TokenType.MLT:
                    case TokenType.DIV:
                    case TokenType.MOD:
                        {
                            ExBinaryExp(ExOPDecideArithmetic(_currToken), "ExPrefixed");
                            break;
                        }
                    default:
                        return;
                }
            }
        }

        public static OPC ExOPDecideArithmetic(TokenType typ)
        {
            switch (typ)
            {
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0059:Unnecessary assignment of a value", Justification = "<Pending>")]
        public void ExPrefixed()
        {
            int p = ExFactor();
            for (; ; )
            {
                switch (_currToken)
                {
                    case TokenType.DOT:
                        {
                            ReadAndSetToken();

                            _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(Expect(TokenType.IDENTIFIER)), 0, 0);

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
                                throw new Exception("can't break deref OR ',' needed after [exp] = exp decl");
                            }
                            ReadAndSetToken();
                            ExExp();
                            Expect(TokenType.ARR_CLOSE);

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
                                return;
                            }
                            int v = _currToken == TokenType.DEC ? -1 : 1;

                            ReadAndSetToken();

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
                            return;
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
                            ReadAndSetToken();
                            ExFuncCall();
                            break;
                        }
                    default:
                        {
                            return;
                        }
                }
            }
        }

        public int ExFactor()
        {
            _Estate._type = ExEType.EXPRESSION;

            switch (_currToken)
            {
                case TokenType.LITERAL:
                    {
                        _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(_Fstate.CreateString(_lexer.str_val)), 0, 0);
                        ReadAndSetToken();
                        break;
                    }
                case TokenType.BASE:
                    {
                        ReadAndSetToken();
                        _Estate._type = ExEType.BASE;
                        _Estate._pos = _Fstate.TopTarget();
                        return _Estate._pos;
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
                        ReadAndSetToken();

                        if ((p = _Fstate.GetLocal(idx)) != -1)
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
                        return _Estate._pos;
                    }
                case TokenType.GLB:
                    {
                        _Fstate.AddInstr(OPC.LOAD_ROOT, _Fstate.PushTarget(), 0, 0, 0);
                        _Estate._type = ExEType.OBJECT;
                        _currToken = TokenType.DOT;
                        _Estate._pos = -1;
                        return _Estate._pos;
                    }
                case TokenType.NULL:
                    {
                        _Fstate.AddInstr(OPC.LOAD_NULL, _Fstate.PushTarget(), 1, 0, 0);
                        ReadAndSetToken();
                        break;
                    }
                case TokenType.INTEGER:
                    {
                        AddIntConstLoadInstr(_lexer.i_val, -1);
                        ReadAndSetToken();
                        break;
                    }
                case TokenType.FLOAT:
                    {
                        AddFloatConstLoadInstr(new FloatInt() { f = _lexer.f_val }.i, -1);
                        ReadAndSetToken();
                        break;
                    }
                case TokenType.TRUE:
                case TokenType.FALSE:
                    {
                        _Fstate.AddInstr(OPC.LOAD_BOOL, _Fstate.PushTarget(), _currToken == TokenType.TRUE ? 1 : 0, 0, 0);
                        ReadAndSetToken();
                        break;
                    }
                case TokenType.ARR_OPEN:
                    {
                        _Fstate.AddInstr(OPC.NEW_OBJECT, _Fstate.PushTarget(), 0, 0, ExNOT.ARRAY);
                        int p = _Fstate.GetCurrPos();
                        int k = 0;
                        ReadAndSetToken();

                        while (_currToken != TokenType.ARR_CLOSE)
                        {
                            ExExp();
                            if (_currToken == TokenType.SEP)
                            {
                                ReadAndSetToken();
                            }
                            int v = _Fstate.PopTarget();
                            int a = _Fstate.TopTarget();
                            _Fstate.AddInstr(OPC.ARRAY_APPEND, a, v, ArrayAType.STACK, 0);
                            k++;
                        }
                        _Fstate.SetInstrParam(p, 1, k);
                        ReadAndSetToken();
                        break;
                    }
                case TokenType.CLS_OPEN:
                    {
                        _Fstate.AddInstr(OPC.NEW_OBJECT, _Fstate.PushTarget(), 0, ExNOT.DICT, 0);
                        ReadAndSetToken();
                        ParseClusterOrClass(TokenType.SEP, TokenType.CLS_CLOSE);
                        break;
                    }
                case TokenType.FUNCTION:
                    {
                        ExFuncResolveExp(_currToken);
                        break;
                    }
                case TokenType.CLASS:
                    {
                        ReadAndSetToken();
                        ExClassResolveExp();
                        break;
                    }
                case TokenType.SUB:
                    {
                        ReadAndSetToken();
                        switch (_currToken)
                        {
                            case TokenType.INTEGER:
                                {
                                    AddIntConstLoadInstr(-_lexer.i_val, -1);
                                    ReadAndSetToken();
                                    break;
                                }
                            case TokenType.FLOAT:
                                {
                                    AddFloatConstLoadInstr(new FloatInt() { f = -_lexer.f_val }.i, -1);
                                    ReadAndSetToken();
                                    break;
                                }
                            default:
                                {
                                    ExOpUnary(OPC.NEGATE);
                                    break;
                                }
                        }
                        break;
                    }
                case TokenType.EXC:
                    {
                        ReadAndSetToken();
                        ExOpUnary(OPC.NOT);
                        break;
                    }
                case TokenType.TIL:
                    {
                        ReadAndSetToken();
                        if (_currToken == TokenType.INTEGER)
                        {
                            AddIntConstLoadInstr(~_lexer.i_val, -1);
                            ReadAndSetToken();
                            break;
                        }
                        ExOpUnary(OPC.BNOT);
                        break;
                    }
                case TokenType.TYPEOF:
                    {
                        ReadAndSetToken();
                        ExOpUnary(OPC.TYPEOF);
                        break;
                    }
                case TokenType.INC:
                case TokenType.DEC:
                    {
                        ExPrefixedIncDec(_currToken);
                        break;
                    }
                case TokenType.R_OPEN:
                    {
                        ReadAndSetToken();
                        ExSepExp();
                        Expect(TokenType.R_CLOSE);
                        break;
                    }

            }

            return -1;
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

        public void ExPrefixedIncDec(TokenType typ)
        {
            ExEState eState;
            int v = typ == TokenType.DEC ? -1 : 1;

            ReadAndSetToken();

            eState = _Estate.Copy();
            _Estate.stop_deref = true;

            ExPrefixed();

            switch (_Estate._type)
            {
                case ExEType.EXPRESSION:
                    {
                        throw new Exception("can't increment or decrement an expression!");
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
        }

        public void ExSepExp()
        {
            for (ExExp(); _currToken == TokenType.SEP; _Fstate.PopTarget(), ReadAndSetToken(), ExSepExp())
            {
                ;
            }
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

        public void ParseClusterOrClass(TokenType sep, TokenType end)
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
                        ReadAndSetToken();
                        ParseClusterOrClass(TokenType.SEP, TokenType.A_END);
                        a_present = true;
                    }
                }
                switch (_currToken)
                {
                    case TokenType.FUNCTION:
                    case TokenType.CONSTRUCTOR:
                        {
                            TokenType typ = _currToken;
                            ReadAndSetToken();

                            ExObjectPtr o = typ == TokenType.FUNCTION ? Expect(TokenType.IDENTIFIER) : _Fstate.CreateString("constructor");

                            Expect(TokenType.R_OPEN);

                            _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(o), 0, 0);
                            ExFuncCreate(o);
                            _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, 0, 0);
                            break;
                        }
                    case TokenType.ARR_OPEN:
                        {
                            ReadAndSetToken();
                            ExSepExp();
                            Expect(TokenType.ARR_CLOSE);
                            Expect(TokenType.ASG);
                            ExExp();
                            break;
                        }
                    case TokenType.LITERAL:
                        {
                            if (sep == TokenType.SEP)
                            {
                                _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(Expect(TokenType.LITERAL)), 0, 0);
                                Expect(TokenType.COL);
                                ExExp();
                                break;
                            }
                            goto default;
                        }
                    default:
                        {
                            _Fstate.AddInstr(OPC.LOAD, _Fstate.PushTarget(), _Fstate.GetConst(Expect(TokenType.IDENTIFIER)), 0, 0);
                            Expect(TokenType.ASG);
                            ExExp();
                            break;
                        }
                }

                if (_currToken == sep)
                {
                    ReadAndSetToken();
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
            ReadAndSetToken();
        }

        public void ExFuncCall()
        {
            int _this = 1;
            while (_currToken != TokenType.R_CLOSE)
            {
                ExExp();
                TargetLocalMove();
                _this++;
                if (_currToken == TokenType.SEP)
                {
                    ReadAndSetToken();
                    if (_currToken == TokenType.R_CLOSE)
                    {
                        throw new Exception("expression expected, found ')'");
                    }
                }
            }
            ReadAndSetToken();

            for (int i = 0; i < (_this - 1); i++)
            {
                _Fstate.PopTarget();
            }

            int st = _Fstate.PopTarget();
            int cl = _Fstate.PopTarget();

            _Fstate.AddInstr(OPC.CALL, _Fstate.PushTarget(), cl, st, _this);
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

        public void ExFuncResolveExp(TokenType typ)
        {
            ReadAndSetToken();
            Expect(TokenType.R_OPEN);
            ExObjectPtr d = new();
            ExFuncCreate(d);
            _Fstate.AddInstr(OPC.CLOSURE, _Fstate.PushTarget(), _Fstate._funcs.Count - 1, typ == TokenType.FUNCTION ? 0 : 1, 0);
        }
        public void ExClassResolveExp()
        {
            int at = 985;

            if (_currToken == TokenType.A_START)
            {
                ReadAndSetToken();
                _Fstate.AddInstr(OPC.NEW_OBJECT, _Fstate.PushTarget(), 0, ExNOT.DICT, 0);
                ParseClusterOrClass(TokenType.SEP, TokenType.A_END);
                at = _Fstate.TopTarget();
            }

            Expect(TokenType.CLS_OPEN);

            if (at != 985)
            {
                _Fstate.PopTarget();
            }

            _Fstate.AddInstr(OPC.NEW_OBJECT, _Fstate.PushTarget(), -1, at, ExNOT.CLASS);
            ParseClusterOrClass(TokenType.SMC, TokenType.CLS_CLOSE);
        }

        public void ExFuncCreate(ExObjectPtr o)
        {
            ExFState f_state = _Fstate.PushChildState(_VM._sState);
            f_state._name = o;

            ExObject pname;
            f_state.AddParam(_Fstate.CreateString("this"));
            f_state._source = new(_source);

            int def_param_count = 0;

            while (_currToken != TokenType.R_CLOSE)
            {
                pname = Expect(TokenType.IDENTIFIER);
                f_state.AddParam(pname);

                if (_currToken == TokenType.ASG)
                {
                    ReadAndSetToken();
                    ExExp();
                    _Fstate.AddDefParam(_Fstate.TopTarget());
                    def_param_count++;
                }
                else
                {
                    if (def_param_count > 0)
                    {
                        throw new Exception("expected = for a default value");
                    }
                }

                if (_currToken == TokenType.SEP)
                {
                    ReadAndSetToken();
                }
                else if (_currToken != TokenType.R_CLOSE)
                {
                    throw new Exception("expected ')' or ',' for function decl");
                }
            }
            Expect(TokenType.R_CLOSE);
            for (int i = 0; i < def_param_count; i++)
            {
                _Fstate.PopTarget();
            }

            ExFState tmp = _Fstate.Copy();
            _Fstate = f_state;

            ProcessStatement(false);

            f_state.AddLineInfo(_lexer._prevToken == TokenType.NEWLINE ? _lexer._lastTokenLine : _lexer._currLine, _lineinfo, true);
            f_state.AddInstr(OPC.RETURN, 985, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExFuncPro fpro = f_state.CreatePrototype();

            _Fstate = tmp;
            _Fstate._funcs.Add(fpro);
            _Fstate.PopChildState();
        }

        public void ExOpUnary(OPC op)
        {
            ExPrefixed();
            int s = _Fstate.PopTarget();
            _Fstate.AddInstr(op, _Fstate.PushTarget(), s, 0, 0);
        }

    }
}
