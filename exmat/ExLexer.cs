using System;
using System.Collections.Generic;
using System.Linq;
using ExMat.Objects;
using ExMat.Token;

namespace ExMat.Lexer
{
    public class ExMacroParam : IDisposable
    {
        public List<int> lines = new();
        public List<int> cols = new();
        public string name;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    name = null;
                    cols = null;
                    lines = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExMacroParam()
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

    public class ExMacro : IDisposable
    {
        public List<ExMacroParam> _params = new();
        public string name;
        public string source;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    name = null;
                    source = null;
                    Disposer.DisposeList(ref _params);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExMacro()
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

    public class ExLexer : IDisposable
    {
        private string _source_;
        public string Source => _source_;

        private readonly int _sourcelen = 0;
        private int _sourceidx = 0;

        public int _currLine;
        public int _currCol;
        public char _currChar;

        public int _lastTokenLine;

        public string _aStr;
        public string str_val;
        public string m_pname;
        public double f_val;
        public long i_val;
        public Objects.ExSpace _space;
        public string m_block;
        public List<ExMacroParam> m_params = new();

        public bool reading_macro = false;

        public Dictionary<string, TokenType> _keyWordsDict = new();

        public bool _reached_end = false;

        public TokenType _prevToken;
        public TokenType _currToken;

        public string _error;

        private ExLexer _lookahead;
        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disposer.DisposeList(ref m_params);

                    _keyWordsDict = null;
                    _lookahead.Dispose();
                    _space.Dispose();

                    _error = null;
                    _aStr = null;
                    str_val = null;
                    m_pname = null;
                    m_block = null;

                    _source_ = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public ExLexer(string source)
        {
            CreateKeyword("if", TokenType.IF);
            CreateKeyword("else", TokenType.ELSE);
            CreateKeyword("for", TokenType.FOR);
            //CreateKeyword("foreach", TokenType.FOREACH);
            //CreateKeyword("while", TokenType.WHILE);

            CreateKeyword("break", TokenType.BREAK);
            CreateKeyword("continue", TokenType.CONTINUE);
            CreateKeyword("return", TokenType.RETURN);

            CreateKeyword("in", TokenType.IN);
            CreateKeyword("and", TokenType.AND);
            CreateKeyword("or", TokenType.OR);
            CreateKeyword("is", TokenType.EQU);
            CreateKeyword("not", TokenType.NEQ);

            CreateKeyword("null", TokenType.NULL);
            CreateKeyword("true", TokenType.TRUE);
            CreateKeyword("false", TokenType.FALSE);

            CreateKeyword("function", TokenType.FUNCTION);
            CreateKeyword("class", TokenType.CLASS);
            CreateKeyword("var", TokenType.VAR);

            CreateKeyword(ExMat._CONSTRUCTOR, TokenType.CONSTRUCTOR);
            CreateKeyword(ExMat._THIS, TokenType.THIS);
            CreateKeyword("base", TokenType.BASE);

            CreateKeyword("rule", TokenType.RULE);
            CreateKeyword("cluster", TokenType.CLUSTER);

            CreateKeyword("typeof", TokenType.TYPEOF);
            CreateKeyword("instanceof", TokenType.INSTANCEOF);
            CreateKeyword("delete", TokenType.DELETE);

            CreateKeyword("seq", TokenType.SEQUENCE);

            //CreateKeyword("sum", TokenType.SUM);
            //CreateKeyword("mul", TokenType.MUL);

            _lastTokenLine = 1;
            _currLine = 1;
            _prevToken = TokenType.NONE;
            _currCol = 0;
            _reached_end = false;

            _source_ = source;
            _sourceidx = 0;
            _sourcelen = source.Length;

            Next();
        }

        private void CreateKeyword(string name, TokenType typ)
        {
            if (_keyWordsDict.Keys.Contains(name))
            {
                throw new Exception(name + " keyword already exists!");
            }

            _keyWordsDict.Add(name, typ);
        }

        private void SkipComment()
        {
            do
            {
                Next();
            } while (_currChar != '\n' && _currChar != ExMat._END);
        }

        private bool SkipBlockComment()
        {
            bool finished = false;
            while (!finished)
            {
                switch (_currChar)
                {
                    case '*':
                        {
                            Next();
                            if (_currChar == '/')
                            {
                                finished = true;
                                Next();
                            }
                            continue;
                        }
                    case '\n':
                        {
                            _currLine++;
                            Next();
                            continue;
                        }
                    case ExMat._END:
                        {
                            _error = "expected '*/' to finish the block comment";
                            return false;
                        }
                    default:
                        {
                            Next();
                            break;
                        }
                }
            }
            return true;
        }

        private char ReadSourceChar()
        {
            if (_sourceidx == _sourcelen)
            {
                return ExMat._END;
            }

            char next = Source[_sourceidx];
            _sourceidx++;
            return next;
        }

        private TokenType SetAndReturnToken(TokenType typ)
        {
            _prevToken = _currToken;
            _currToken = typ;
            return typ;
        }

        private void Next()
        {
            char c = ReadSourceChar();
            if (c == ExMat._END)
            {
                _reached_end = true;
            }
            _currCol++;
            _currChar = c;
        }

        private TokenType ReadSpaceDim(char curr)
        {
            Next();
            switch (ReadNumber())
            {
                case TokenType.INTEGER:
                    {
                        if (i_val < 0)
                        {
                            _error = "dimension can't be less than zero";
                            return TokenType.UNKNOWN;
                        }
                        _space.dim = (int)i_val;
                        break;
                    }
                default:
                    {
                        _error = "expected integer as dimension";
                        return TokenType.UNKNOWN;
                    }
            }
            if (_currChar != curr)
            {
                _error = "expected '" + curr + "' to finish space reference after dimension";
                return TokenType.UNKNOWN;
            }
            return TokenType.SPACE;
        }


        private TokenType ReadSpace(char curr)
        {
            Next();
            if (ReadId() != TokenType.IDENTIFIER)
            {
                _error = "expected space identifier";
                return TokenType.UNKNOWN;
            }

            _space = new();
            _space.space = str_val;

            if (_currChar == ExMat._END)
            {
                return TokenType.UNKNOWN;
            }

            if (_currChar == curr)
            {
                return TokenType.SPACE;
            }

            switch (_currChar)
            {
                case '+':
                case '-':
                    //case '*':
                    {
                        _space.sign = _currChar;
                        Next();
                        break;
                    }
                case '\'':
                    {
                        return ReadSpaceDim(curr);
                    }
                default:
                    {
                        _error = "expected sign(+,-) or dimension(') characters";
                        return TokenType.UNKNOWN;
                    }
            }

            if (_currChar == curr)
            {
                return TokenType.SPACE;
            }

            if (_currChar != '\'')
            {
                _error = "unexpected space character '" + _currChar + "'";
                return TokenType.UNKNOWN;
            }

            return ReadSpaceDim(curr);
        }

        private string ReadMacroParam()
        {
            string pname = string.Empty;
            do
            {
                pname += _currChar;
                Next();

            } while (char.IsLetterOrDigit(_currChar) || _currChar == '_');

            if (_currChar != '#')
            {
                return null;
            }
            Next();
            if (_currChar != '#')
            {
                return null;
            }
            Next();

            return string.IsNullOrWhiteSpace(pname) ? null : pname;
        }

        private TokenType ReadMacroBlock()
        {
            m_block = string.Empty;
            for (; ; )
            {
                while (_currChar != ExMat._END && _currChar != '#')
                {
                    m_block += _currChar;
                    Next();
                }

                if (_currChar == '#')
                {
                    string mtag = string.Empty;
                    Next();

                    string pname = string.Empty;
                    if (_currChar == '#')    // ##param##
                    {
                        Next();
                        pname = ReadMacroParam();
                        if (pname == null)
                        {
                            return TokenType.UNKNOWN;
                        }

                        ExMacroParam ep;
                        if ((ep = m_params.Find((ExMacroParam e) => e.name == pname)) != null)
                        {
                            ep.cols.Add(_currCol);
                            ep.lines.Add(_currLine);
                        }
                        else
                        {
                            m_params.Add(new() { name = pname, cols = new() { _currCol }, lines = new() { _currLine } });
                        }
                        m_block += "##" + pname + "##";
                    }
                    else // #end 
                    {
                        while (char.IsLetterOrDigit(_currChar) || _currChar == '_')
                        {
                            mtag += _currChar;
                            Next();
                        }
                    }

                    switch (mtag)
                    {
                        case "end":
                            {
                                return TokenType.MACROBLOCK;
                            }
                        case "":
                            {
                                continue;
                            }
                        default:
                            {
                                return TokenType.UNKNOWN;
                            }
                    }
                }
            }
        }

        private TokenType ReadString(char curr)
        {
            _aStr = string.Empty;
            Next();
            if (_currChar == ExMat._END)
            {
                return TokenType.UNKNOWN;
            }
            for (; ; )
            {
                while (_currChar != curr)
                {
                    switch (_currChar)
                    {
                        case ExMat._END:
                            {
                                _error = "unfinished string";
                                return TokenType.UNKNOWN;
                            }
                        //case '\n':
                        //    {
                        //        _aStr += _currChar;
                        //        Next();
                        //        _currLine++;
                        //        break;
                        //    }
                        case '\\':
                            {
                                Next();
                                switch (_currChar)
                                {
                                    case 't':
                                        {
                                            _aStr += '\t';
                                            Next();
                                            break;
                                        }
                                    case 'a':
                                        {
                                            _aStr += '\a';
                                            Next();
                                            break;
                                        }
                                    case 'b':
                                        {
                                            _aStr += '\b';
                                            Next();
                                            break;
                                        }
                                    case 'n':
                                        {
                                            _aStr += '\n';
                                            Next();
                                            break;
                                        }
                                    case 'r':
                                        {
                                            _aStr += '\r';
                                            Next();
                                            break;
                                        }
                                    case 'v':
                                        {
                                            _aStr += '\v';
                                            Next();
                                            break;
                                        }
                                    case 'f':
                                        {
                                            _aStr += '\f';
                                            Next();
                                            break;
                                        }
                                    case '0':
                                        {
                                            _aStr += ExMat._END;
                                            Next();
                                            break;
                                        }
                                    case '\\':
                                        {
                                            _aStr += '\\';
                                            Next();
                                            break;
                                        }
                                    case '"':
                                        {
                                            _aStr += '\"';
                                            Next();
                                            break;
                                        }
                                    case '\'':
                                        {
                                            _aStr += '\'';
                                            Next();
                                            break;
                                        }
                                    default:
                                        {
                                            _error = "unknown escape char '" + _currChar + "'";
                                            return TokenType.UNKNOWN;
                                        }
                                }
                                break;
                            }
                        case '\r':
                        case '\n':
                            {
                                _error = "unfinished string";
                                return TokenType.UNKNOWN;
                            }
                        default:
                            {
                                _aStr += _currChar;
                                Next();
                                break;
                            }
                    }
                }
                Next();
                break;
            }
            // TO-DO maybe handle '' as char ?

            str_val = _aStr;
            return TokenType.LITERAL;
        }

        private TokenType GetIdType()
        {
            if (_keyWordsDict.Keys.Contains(_aStr))
            {
                return _keyWordsDict[_aStr];
            }

            return TokenType.IDENTIFIER;
        }

        private TokenType ReadId(bool macro = false)
        {
            TokenType typ;
            _aStr = string.Empty;
            if (!reading_macro)
            {
                do
                {
                    _aStr += _currChar;
                    Next();

                } while (char.IsLetterOrDigit(_currChar) || _currChar == '_');
            }
            else
            {
                do
                {
                    if (_currChar == '#')
                    {
                        Next();
                        if (_currChar != '#')
                        {
                            return TokenType.UNKNOWN;
                        }
                        Next();
                        string pname = ReadMacroParam();
                        if (string.IsNullOrWhiteSpace(pname))
                        {
                            return TokenType.UNKNOWN;
                        }

                        _aStr += "##" + pname + "##";
                    }
                    else
                    {
                        _aStr += _currChar;
                        Next();
                    }

                } while (char.IsLetterOrDigit(_currChar) || _currChar == '_' || _currChar == '#');

                return TokenType.MACROPARAM_STR;
            }

            if (macro)
            {
                switch (_aStr)
                {
                    case "define":
                        return TokenType.MACROSTART;
                    case "end":
                        return TokenType.MACROEND;
                    case "block":
                        return TokenType.MACROBLOCK;
                    default:
                        return TokenType.UNKNOWN;
                }
            }

            typ = GetIdType();
            if (typ == TokenType.EQU)
            {
                _lookahead = new(Source) { _sourceidx = _sourceidx, _currChar = _currChar, _currCol = _currCol, _currToken = _currToken, _lastTokenLine = _lastTokenLine, _aStr = _aStr };
                _lookahead.Lex();
                if (_lookahead._currToken == TokenType.NEQ && _lookahead._aStr == "not")
                {
                    typ = TokenType.NEQ;
                    Lex();
                }
                _lookahead = null;
            }

            if (typ == TokenType.IDENTIFIER)
            {
                str_val = _aStr + "";
            }

            return typ;
        }

        private static bool IsExp(char c)
        {
            return c == 'e' || c == 'E';
        }

        private static bool IsSign(char c)
        {
            return c == '+' || c == '-';
        }

        private TokenType ReadNumber()
        {
            TokenType typ = TokenType.INTEGER;
            char start = _currChar;
            _aStr = string.Empty;
            Next();

            bool m_typed = false;
            _aStr += start;

            if (reading_macro)
            {
                while (_currChar == '.'
                       || char.IsDigit(_currChar)
                       || IsExp(_currChar)
                       || _currChar == '#')
                {

                    if (_currChar == '#')
                    {
                        Next();
                        if (_currChar != '#')
                        {
                            return TokenType.UNKNOWN;
                        }
                        Next();
                        string pname = ReadMacroParam();
                        if (string.IsNullOrWhiteSpace(pname))
                        {
                            return TokenType.UNKNOWN;
                        }

                        _aStr += "##" + pname + "##";
                        typ = TokenType.FLOAT;
                    }

                    if (!m_typed && (_currChar == '.' || IsExp(_currChar)))
                    {
                        typ = TokenType.FLOAT;
                    }

                    if (IsExp(_currChar))
                    {
                        if (typ != TokenType.FLOAT)
                        {
                            _error = "Wrong double number format";
                            return TokenType.UNKNOWN;
                        }

                        typ = TokenType.SCI;

                        _aStr += _currChar;
                        Next();

                        if (IsSign(_currChar))
                        {
                            _aStr += _currChar;
                            Next();
                        }

                        if (!char.IsDigit(_currChar) && _currChar != '#')
                        {
                            _error = "Wrong exponent value format";
                            return TokenType.UNKNOWN;
                        }
                    }

                    _aStr += _currChar;
                    Next();
                }

                return TokenType.MACROPARAM_NUM;
            }
            else
            {
                while (_currChar == '.' || char.IsDigit(_currChar) || IsExp(_currChar))
                {
                    if (_currChar == '.' || IsExp(_currChar))
                    {
                        typ = TokenType.FLOAT;
                    }

                    if (IsExp(_currChar))
                    {
                        if (typ != TokenType.FLOAT)
                        {
                            _error = "Wrong double number format";
                            return TokenType.UNKNOWN;
                        }

                        typ = TokenType.SCI;

                        _aStr += _currChar;
                        Next();

                        if (IsSign(_currChar))
                        {
                            _aStr += _currChar;
                            Next();
                        }

                        if (!char.IsDigit(_currChar))
                        {
                            _error = "Wrong exponent value format";
                            return TokenType.UNKNOWN;
                        }
                    }

                    _aStr += _currChar;
                    Next();
                }
                if (_aStr[^1] == '.')
                {
                    _error = "expected digits after '.' ";
                    return TokenType.UNKNOWN;
                }
                switch (typ)
                {
                    case TokenType.FLOAT:
                    case TokenType.SCI:
                        {
                            if (!double.TryParse(_aStr, out f_val))
                            {
                                _error = "failed to parse as float";
                                return TokenType.UNKNOWN;
                            }
                            return TokenType.FLOAT;
                        }
                    case TokenType.INTEGER:
                        {
                            if (!long.TryParse(_aStr, out i_val))
                            {
                                if (!double.TryParse(_aStr, out f_val))
                                {
                                    _error = "failed to parse as integer";
                                    return TokenType.UNKNOWN;
                                }
                                return TokenType.FLOAT;
                            }
                            return TokenType.INTEGER;
                        }
                }
                return TokenType.ENDLINE;
            }
        }

        public static TokenType GetTokenTypeForChar(char c)
        {
            switch (c)
            {
                case ExMat._END:
                    return TokenType.ENDLINE;
                case '+':
                    return TokenType.ADD;
                case '-':
                    return TokenType.SUB;
                case '*':
                    return TokenType.MLT;
                case '%':
                    return TokenType.MOD;
                case '&':
                    return TokenType.BAND;
                case '|':
                    return TokenType.BOR;
                case '^':
                    return TokenType.BXOR;
                case ':':
                    return TokenType.COL;
                case '=':
                    return TokenType.ASG;
                case '<':
                    return TokenType.LET;
                case '>':
                    return TokenType.GET;
                case '!':
                    return TokenType.EXC;
                case '.':
                    return TokenType.DOT;
                case ';':
                    return TokenType.SMC;
                case ',':
                    return TokenType.SEP;
                case '{':
                    return TokenType.CLS_OPEN;
                case '}':
                    return TokenType.CLS_CLOSE;
                case '[':
                    return TokenType.ARR_OPEN;
                case ']':
                    return TokenType.ARR_CLOSE;
                case '(':
                    return TokenType.R_OPEN;
                case ')':
                    return TokenType.R_CLOSE;
                case '?':
                    return TokenType.QMARK;
                case '\'':
                    return TokenType.MTRS;
                case '$':
                    return TokenType.LAMBDA;
                default:
                    return TokenType.UNKNOWN;
            }
        }

        public TokenType Lex()
        {
            _lastTokenLine = _currLine;
            while (_currChar != ExMat._END)
            {
                switch (_currChar)
                {
                    case '\t':
                    case '\r':
                    case ' ':
                        {
                            Next();
                            continue;
                        }
                    case '\n':
                        {
                            _currLine++;
                            _prevToken = _currToken;
                            _currToken = TokenType.NEWLINE;
                            Next();
                            _currCol = 1;
                            continue;
                        }
                    case '#':
                        {
                            if (reading_macro)
                            {
                                Next();
                                if (_currChar != '#')
                                {
                                    // SHOULDNT GO HERE 
                                    return TokenType.UNKNOWN;
                                }

                                m_pname = ReadMacroParam();
                                if (string.IsNullOrWhiteSpace(m_pname))
                                {
                                    return TokenType.UNKNOWN;
                                }
                                return SetAndReturnToken(TokenType.MACROPARAM);
                            }
                            else
                            {
                                Next();
                                TokenType typ = ReadId(true);
                                switch (typ)
                                {
                                    case TokenType.MACROSTART:
                                    case TokenType.MACROEND:
                                        {
                                            return SetAndReturnToken(typ);
                                        }
                                    case TokenType.MACROBLOCK:
                                        {
                                            return SetAndReturnToken(ReadMacroBlock());
                                        }
                                    default:
                                        {
                                            _error = "expected 'define' or 'end' after '#'";
                                            return TokenType.UNKNOWN;
                                        }
                                }
                            }

                        }
                    case '=':
                        {
                            Next();
                            if (_currChar == '=')
                            {
                                Next();
                                return SetAndReturnToken(TokenType.EQU);
                            }
                            else if (_currChar == '>')
                            {
                                Next();
                                return SetAndReturnToken(TokenType.ELEMENT_DEF);
                            }
                            else
                            {
                                return SetAndReturnToken(TokenType.ASG);
                            }
                        }
                    case '<':
                        {
                            Next();
                            switch (_currChar)
                            {
                                case '=':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.LET);
                                    }
                                case '<':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.LSHF);
                                    }
                                case '>':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.NEWSLOT);
                                    }
                            }
                            return SetAndReturnToken(TokenType.LST);
                        }
                    case '>':
                        {
                            Next();
                            switch (_currChar)
                            {
                                case '=':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.GET);
                                    }
                                case '>':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.RSHF);
                                    }
                            }
                            return SetAndReturnToken(TokenType.GRT);
                        }
                    case '!':
                        {
                            Next();
                            if (_currChar == '=')
                            {
                                Next();
                                return SetAndReturnToken(TokenType.NEQ);
                            }
                            else
                            {
                                return SetAndReturnToken(TokenType.EXC);
                            }
                        }
                    case '\\':
                        {
                            _error = "escape char outside string";
                            return TokenType.UNKNOWN;
                        }
                    case '\'':
                        {
                            Next();
                            return SetAndReturnToken(TokenType.MTRS);
                        }
                    case '"':
                        {
                            TokenType res;
                            if ((res = ReadString(_currChar)) != TokenType.UNKNOWN)
                            {
                                return SetAndReturnToken(res);
                            }
                            return TokenType.UNKNOWN;
                        }
                    case '$':
                        {
                            Next();
                            return SetAndReturnToken(TokenType.LAMBDA);
                        }
                    case '@':
                        {
                            TokenType res;
                            if ((res = ReadSpace(_currChar)) != TokenType.UNKNOWN)
                            {
                                Next();
                                return SetAndReturnToken(TokenType.SPACE);
                            }
                            else
                            {
                                _error = "expected the pattern @(Z|R|N|A)[+-]?('\\d+)?@ for spaces";
                                return TokenType.UNKNOWN;
                            }
                        }
                    case '{':
                    case '}':
                    case '(':
                    case ')':
                    case '[':
                    case ']':
                    case ';':
                    case ',':
                    case '?':
                    case '~':
                        {
                            TokenType tmp = GetTokenTypeForChar(_currChar);
                            Next();
                            return SetAndReturnToken(tmp);
                        }
                    case '.':
                        {
                            Next();
                            switch (_currChar)
                            {
                                case '/':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.A_END);
                                    }
                                case '*':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.MMLT);
                                    }
                                case '.':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.DEFAULT);
                                    }
                                default:
                                    {
                                        return SetAndReturnToken(TokenType.DOT);
                                    }
                            }
                        }
                    case '&':
                        {
                            Next();
                            if (_currChar == '&')
                            {
                                Next();
                                return SetAndReturnToken(TokenType.AND);
                            }
                            else
                            {
                                return SetAndReturnToken(TokenType.BAND);
                            }
                        }
                    case '|':
                        {
                            Next();
                            if (_currChar == '|')
                            {
                                Next();
                                return SetAndReturnToken(TokenType.OR);
                            }
                            else
                            {
                                return SetAndReturnToken(TokenType.BOR);
                            }
                        }
                    case '^':
                        {
                            Next();
                            if (_currChar == '^')
                            {
                                Next();
                                return SetAndReturnToken(TokenType.XOR);
                            }
                            else
                            {
                                return SetAndReturnToken(TokenType.BXOR);
                            }
                        }
                    case ':':
                        {
                            Next();
                            if (_currChar == ':')
                            {
                                Next();
                                return SetAndReturnToken(TokenType.GLB);
                            }
                            else
                            {
                                return SetAndReturnToken(TokenType.COL);
                            }
                        }
                    case '+':
                        {
                            Next();
                            switch (_currChar)
                            {
                                case '=':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.ADDEQ);
                                    }
                                case '+':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.INC);
                                    }
                                default:
                                    {
                                        return SetAndReturnToken(TokenType.ADD);
                                    }
                            }
                        }
                    case '-':
                        {
                            Next();
                            switch (_currChar)
                            {
                                case '=':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.SUBEQ);
                                    }
                                case '-':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.DEC);
                                    }
                                default:
                                    {
                                        return SetAndReturnToken(TokenType.SUB);
                                    }
                            }
                        }
                    case '*':
                        {
                            Next();
                            switch (_currChar)
                            {
                                case '.':
                                    {
                                        Next();
                                        if (_currChar != '*')
                                        {
                                            return TokenType.UNKNOWN;
                                        }
                                        Next();
                                        return SetAndReturnToken(TokenType.CARTESIAN);
                                    }
                                case '*':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.EXP);
                                    }
                                case '=':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.MLTEQ);
                                    }
                                default:
                                    {
                                        return SetAndReturnToken(TokenType.MLT);
                                    }
                            }
                        }
                    case '/':
                        {
                            Next();
                            switch (_currChar)
                            {
                                case '=':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.DIVEQ);
                                    }
                                case '/':
                                    {
                                        SkipComment();
                                        continue;
                                    }
                                case '*':
                                    {
                                        Next();
                                        if (!SkipBlockComment())
                                        {
                                            return TokenType.UNKNOWN;
                                        }
                                        continue;
                                    }
                                case '.':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.A_START);
                                    }
                                default:
                                    {
                                        return SetAndReturnToken(TokenType.DIV);
                                    }
                            }
                        }
                    case '%':
                        {
                            Next();
                            switch (_currChar)
                            {
                                case '=':
                                    {
                                        Next();
                                        return SetAndReturnToken(TokenType.MODEQ);
                                    }
                                default:
                                    {
                                        return SetAndReturnToken(TokenType.MOD);
                                    }
                            }
                        }
                    case ExMat._END:
                        return TokenType.ENDLINE;
                    default:
                        {
                            if (char.IsDigit(_currChar))
                            {
                                return SetAndReturnToken(ReadNumber());
                            }
                            else if (char.IsLetter(_currChar) || _currChar == '_')
                            {
                                return SetAndReturnToken(ReadId());
                            }
                            else
                            {
                                char tmp = _currChar;
                                if (char.IsControl(tmp))
                                {
                                    throw new Exception("Unexpected control character");
                                }

                                Next();
                                return SetAndReturnToken(GetTokenTypeForChar(tmp));
                            }
                        }
                }
            }
            return TokenType.ENDLINE;
        }


        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExLexer()
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
