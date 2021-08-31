using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ExMat.Objects;
using ExMat.Token;

namespace ExMat.Lexer
{

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExLexer : IDisposable
    {
        /// <summary>
        /// Source code
        /// </summary>
        private string _source;
        /// <summary>
        /// Current reading index of <see cref="_source"/>
        /// </summary>
        private int _source_idx;
        /// <summary>
        /// Length of source code <see cref="_source"/>
        /// </summary>
        private readonly int _source_len;

        /// <summary>
        /// Line count
        /// </summary>
        public int CurrentLine;
        /// <summary>
        /// Last line number a token was found
        /// </summary>
        public int PrevTokenLine;
        /// <summary>
        /// Current column index
        /// </summary>
        public int CurrentCol;
        /// <summary>
        /// Last character read
        /// </summary>
        public char CurrentChar;

        /// <summary>
        /// Previous token found
        /// </summary>
        public TokenType TokenPrev;
        /// <summary>
        /// Last token found
        /// </summary>
        public TokenType TokenCurr;


        /// <summary>
        /// Currently read integer value
        /// </summary>
        public long ValInteger;
        /// <summary>
        /// Currently read float value
        /// </summary>
        public double ValFloat;
        /// <summary>
        /// Currently read string value
        /// </summary>
        public string ValString;
        /// <summary>
        /// Currently read space value
        /// </summary>
        public ExSpace ValSpace;

        /// <summary>
        /// Temporary string builder while reading strings
        /// </summary>
        public StringBuilder ValTempString;

        /// <summary>
        /// Type of the currently read complex number's real part, either <see cref="TokenType.INTEGER"/> or <see cref="TokenType.FLOAT"/>
        /// </summary>
        public TokenType TokenComplex;

        public StringBuilder MacroBlock;
        public string MacroParamName;
        public List<ExMacroParam> MacroParams = new();

        public bool IsReadingMacroBlock;

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorString;

        private bool disposedValue;

        /// <summary>
        /// Keywords dictionary
        /// </summary>
        public readonly Dictionary<string, TokenType> KeyWords = new()
        {
            // Sabit değerler
            { "null", TokenType.NULL },
            { "true", TokenType.TRUE },
            { "false", TokenType.FALSE },
            // Koşullu veya döngüsel
            { "if", TokenType.IF },
            { "else", TokenType.ELSE },
            { "for", TokenType.FOR },
            { "continue", TokenType.CONTINUE },
            { "break", TokenType.BREAK },
            // Tanımsal
            { "var", TokenType.VAR },
            { "function", TokenType.FUNCTION },
            { "cluster", TokenType.CLUSTER },
            { "rule", TokenType.RULE },
            { "class", TokenType.CLASS },
            { "seq", TokenType.SEQUENCE },
            // Değer dönen
            { "return", TokenType.RETURN },
            { "typeof", TokenType.TYPEOF },
            { "instanceof", TokenType.INSTANCEOF },
            { "delete", TokenType.DELETE },
            // Karşılaştırma operatörleri alternatifi
            { "and", TokenType.AND },
            { "or", TokenType.OR },
            { "is", TokenType.EQU },
            { "not", TokenType.NEQ },
            // Diğer
            { "in", TokenType.IN },     // Değer varlığı kontrolü
            { "base", TokenType.BASE },
            { ExMat.ConstructorName, TokenType.CONSTRUCTOR },
            { ExMat.ThisName, TokenType.THIS }
        };

        public ExLexer(string source)
        {
            PrevTokenLine = 1;
            CurrentLine = 1;
            CurrentCol = 0;
            TokenPrev = TokenType.STARTERTOKEN;

            _source = source;
            _source_idx = 0;
            _source_len = source.Length;

            NextChar();
        }

        public ExLexer(string source, int sourceIdx, char currentChar, int currentCol, TokenType tokenCurr, int prevTokenLine, string valTempString)
        {
            PrevTokenLine = 1;
            CurrentLine = 1;
            CurrentCol = 0;
            TokenPrev = TokenType.STARTERTOKEN;

            _source = source;
            _source_idx = 0;
            _source_len = source.Length;
            _source_idx = sourceIdx;
            CurrentChar = currentChar;
            CurrentCol = currentCol;
            TokenCurr = tokenCurr;
            PrevTokenLine = prevTokenLine;
            ValTempString = new(valTempString);
        }

        public ExLexer GetCopy()
        {
            return new(_source,
                       _source_idx,
                       CurrentChar,
                       CurrentCol,
                       TokenCurr,
                       PrevTokenLine,
                       ValTempString.ToString());
        }

        private void SkipComment()
        {
            do
            {
                NextChar();
            } while (CurrentChar != '\n' && CurrentChar != ExMat.EndChar);
        }

        private bool SkipBlockComment()
        {
            bool finished = false;
            while (!finished)
            {
                switch (CurrentChar)
                {
                    case '*':
                        {
                            NextChar();
                            if (CurrentChar == '/')
                            {
                                finished = true;
                                NextChar();
                            }
                            continue;
                        }
                    case '\n':
                        {
                            CurrentLine++;
                            NextChar();
                            continue;
                        }
                    case ExMat.EndChar:
                        {
                            ErrorString = "expected '*/' to finish the block comment";
                            return false;
                        }
                    default:
                        {
                            NextChar();
                            break;
                        }
                }
            }
            return true;
        }

        private void NextChar(int n = 1)
        {
            while (n-- > 0)
            {
                if (_source_idx == _source_len)
                {
                    CurrentChar = ExMat.EndChar;
                    break;
                }
                else
                {
                    CurrentChar = _source[_source_idx++];
                    CurrentCol++;
                }
            }
        }

        private TokenType SetAndReturnToken(TokenType typ)
        {
            TokenPrev = TokenCurr;
            TokenCurr = typ;
            return typ;
        }

        private TokenType ReadSpaceDim(char curr)
        {
            NextChar();
            char currchar = CurrentChar;
            switch (ReadNumber(CurrentChar))
            {
                case TokenType.INTEGER:
                    {
                        if (ValInteger < 0)
                        {
                            ErrorString = "dimension can't be less than zero";
                            return TokenType.UNKNOWN;
                        }
                        ValSpace.Dimension = (int)ValInteger;
                        if (CurrentChar == '*')
                        {
                            ExSpace parent = new();
                            while (CurrentChar == '*')
                            {
                                ExSpace.Copy(parent, ValSpace);

                                if (ReadSpaceDim(curr) != TokenType.SPACE)
                                {
                                    return TokenType.UNKNOWN;
                                }
                                parent.AddDimension(ValSpace);
                            }
                            ValSpace = parent;
                        }
                        break;
                    }
                default:
                    {
                        if (CurrentChar == '@' && char.IsLetter(currchar))  // @A'b@
                        {
                            ValSpace.Dimension = -1;
                            ErrorString = string.Empty;
                            return TokenType.SPACE;
                        }
                        else if (CurrentChar == '*')    // @A'b*...
                        {
                            if (char.IsLetter(currchar))
                            {
                                ValSpace.Dimension = -1;
                            }

                            ExSpace parent = null;
                            while (CurrentChar == '*')
                            {
                                parent = new(-1, ValSpace.Domain, ValSpace.Sign, ValSpace.Child);

                                if (ReadSpaceDim(curr) != TokenType.SPACE)
                                {
                                    return TokenType.UNKNOWN;
                                }
                                parent.AddDimension(ValSpace);
                            }
                            ValSpace = parent;
                            break;
                        }
                        else
                        {
                            ErrorString = "expected integer as dimension";
                            return TokenType.UNKNOWN;
                        }
                    }
            }
            if (CurrentChar != curr)
            {
                ErrorString = "expected '" + curr + "' to finish space reference after dimension";
                return TokenType.UNKNOWN;
            }
            return TokenType.SPACE;
        }


        private TokenType ReadSpace(char curr)
        {
            NextChar();
            if (ReadId() != TokenType.IDENTIFIER)
            {
                ErrorString = "expected space identifier";
                return TokenType.UNKNOWN;
            }

            ValSpace = new();
            switch (ValString)
            {
                case "R":
                case "r":
                case "Z":
                case "z":
                case "C":
                case "E":
                    {
                        break;
                    }
                default:
                    {
                        ErrorString = "unknown domain '" + ValString + "'";
                        return TokenType.UNKNOWN;
                    }
            }

            ValSpace.Domain = ValString;

            if (CurrentChar == ExMat.EndChar)
            {
                return TokenType.UNKNOWN;
            }

            if (CurrentChar == curr)
            {
                return TokenType.SPACE;
            }

            switch (CurrentChar)
            {
                case '+':
                case '-':
                    //case '*':
                    {
                        ValSpace.Sign = CurrentChar;
                        NextChar();
                        break;
                    }
                case '\'':
                    {
                        return ReadSpaceDim(curr);
                    }
                default:
                    {
                        ErrorString = "expected sign(+,-) or dimension(') characters";
                        return TokenType.UNKNOWN;
                    }
            }

            if (CurrentChar == curr)
            {
                return TokenType.SPACE;
            }

            if (CurrentChar != '\'')
            {
                ErrorString = "unexpected space character '" + CurrentChar + "'";
                return TokenType.UNKNOWN;
            }

            return ReadSpaceDim(curr);
        }

        private string ReadMacroParam()
        {
            StringBuilder pname = new();
            do
            {
                pname.Append(CurrentChar);
                NextChar();

            } while (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_');

            if (CurrentChar != '#')
            {
                return null;
            }
            NextChar();
            if (CurrentChar != '#')
            {
                return null;
            }
            NextChar();

            return string.IsNullOrWhiteSpace(pname.ToString()) ? null : pname.ToString();
        }

        private TokenType ReadMacroBlock()
        {
            MacroBlock = new();
            StringBuilder mtag = new();
            while (true)
            {
                while (CurrentChar != ExMat.EndChar && CurrentChar != '#')
                {
                    MacroBlock.Append(CurrentChar);
                    NextChar();
                }

                if (CurrentChar == '#')
                {
                    mtag.Clear();
                    NextChar();

                    string pname = string.Empty;
                    if (CurrentChar == '#')    // ##param##
                    {
                        NextChar();
                        pname = ReadMacroParam();
                        if (pname == null)
                        {
                            return TokenType.UNKNOWN;
                        }

                        ExMacroParam ep;
                        if ((ep = MacroParams.Find((ExMacroParam e) => e.Name == pname)) != null)
                        {
                            ep.Columns.Add(CurrentCol);
                            ep.Lines.Add(CurrentLine);
                        }
                        else
                        {
                            MacroParams.Add(new() { Name = pname, Columns = new() { CurrentCol }, Lines = new() { CurrentLine } });
                        }
                        MacroBlock.Append("##" + pname + "##");
                    }
                    else // #end 
                    {
                        while (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_')
                        {
                            mtag.Append(CurrentChar);
                            NextChar();
                        }
                    }

                    switch (mtag.ToString())
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

        private static int GetHexCharVal(char curr)
        {
            return curr - (char.IsDigit(curr) ? 48 : 87);
        }
        private static bool IsValidHexChar(ref char curr)
        {
            return char.IsDigit(curr) || (char.IsLetter(curr) && (curr = char.ToLower(curr)) <= 'f' && curr >= 'a');
        }

        private bool ReadAsHex(int Base, out int Result)
        {
            NextChar();

            if (Base <= 0)
            {
                Result = 0;
                return true;
            }

            if (IsValidHexChar(ref CurrentChar))
            {
                int s = GetHexCharVal(CurrentChar) * Base;
                if (!ReadAsHex(Base / 16, out Result))
                {
                    return false;
                }
                Result += s;
                return true;
            }
            else
            {
                ErrorString = "expected hexadecimal characters [0-9A-Fa-f], got '" + CurrentChar + "'";
                Result = 0;
                return false;
            }
        }

        private bool AreNextCharacters(string these)
        {
            if (_source_idx + these.Length <= _source_len)
            {
                return _source.Substring(_source_idx - 1, these.Length) == these;
            }
            return false;
        }

        private TokenType ReadVerboseString(char end)
        {
            ValTempString = new();
            NextChar();
            if (CurrentChar == ExMat.EndChar)
            {
                return TokenType.UNKNOWN;
            }

            string ending = new(end, 2);
            bool reading = true;

            while (reading)
            {
                switch (CurrentChar)
                {
                    case ExMat.EndChar:
                        {
                            ErrorString = "unfinished string";
                            return TokenType.UNKNOWN;
                        }
                    case '\"':
                        {
                            if (AreNextCharacters(ending))
                            {
                                ValTempString.Append(CurrentChar);
                                NextChar(ending.Length);
                            }
                            else
                            {
                                reading = false;
                            }
                            break;
                        }
                    default:
                        {
                            ValTempString.Append(CurrentChar);
                            NextChar();
                            break;
                        }
                }
            }
            ValString = ValTempString.ToString(); NextChar();
            return TokenType.LITERAL;

        }

        private TokenType ReadString(char end)
        {
            ValTempString = new();
            NextChar();
            if (CurrentChar == ExMat.EndChar)
            {
                return TokenType.UNKNOWN;
            }

            while (CurrentChar != end)
            {
                switch (CurrentChar)
                {
                    case ExMat.EndChar:
                    case '\r':
                    case '\n':
                        {
                            ErrorString = "unfinished string";
                            return TokenType.UNKNOWN;
                        }
                    case '\\':
                        {
                            NextChar();
                            switch (CurrentChar)
                            {
                                case 't':
                                    ValTempString.Append('\t'); NextChar(); break;
                                #region Rest of the known escape characters \n \r \a \v \f \0 \\ \" \'
                                case 'a':
                                    ValTempString.Append('\a'); NextChar(); break;
                                case 'b':
                                    ValTempString.Append('\b'); NextChar(); break;
                                case 'n':
                                    ValTempString.Append('\n'); NextChar(); break;
                                case 'r':
                                    ValTempString.Append('\r'); NextChar(); break;
                                case 'v':
                                    ValTempString.Append('\v'); NextChar(); break;
                                case 'f':
                                    ValTempString.Append('\f'); NextChar(); break;
                                case '0':
                                    ValTempString.Append(ExMat.EndChar); NextChar(); break;
                                case '\\':
                                    ValTempString.Append('\\'); NextChar(); break;
                                case '"':
                                    ValTempString.Append('\"'); NextChar(); break;
                                case '\'':
                                    ValTempString.Append('\''); NextChar(); break;
                                #endregion

                                #region Hexadecimal \xnn \unnnn
                                case 'u':
                                case 'x':
                                    {
                                        if (ReadAsHex(CurrentChar == 'u' ? 4096 : 16, out int res))
                                        {
                                            ValTempString.Append((char)res);
                                            break;
                                        }
                                        else
                                        {
                                            return TokenType.UNKNOWN;
                                        }
                                    }
                                #endregion
                                default:    // Unknown
                                    {
                                        ErrorString = "unknown escape char '" + CurrentChar + "'";
                                        return TokenType.UNKNOWN;
                                    }
                            }
                            break;
                        }
                    default:
                        {
                            ValTempString.Append(CurrentChar);
                            NextChar();
                            break;
                        }
                }
            }
            ValString = ValTempString.ToString(); NextChar();
            return TokenType.LITERAL;
        }

        private TokenType GetIdType()
        {
            return KeyWords.ContainsKey(ValTempString.ToString()) ? KeyWords[ValTempString.ToString()] : TokenType.IDENTIFIER;
        }

        private void LookForNext(TokenType lookfor, string name, TokenType then, ref TokenType res)
        {
            using ExLexer ahead = GetCopy();
            ahead.Lex();

            if (ahead.TokenCurr == lookfor && ahead.ValTempString.ToString() == name)
            {
                res = then;
                Lex();
            }
        }

        private TokenType ReadId()
        {
            ValTempString = new();
            do
            {
                ValTempString.Append(CurrentChar);
                NextChar();

            } while (char.IsLetterOrDigit(CurrentChar) || CurrentChar == '_');

            TokenType typ = GetIdType();
            if (typ == TokenType.EQU)
            {
                LookForNext(TokenType.NEQ, "not", TokenType.NEQ, ref typ);  // x is not y => x != y
            }
            else if (typ == TokenType.NEQ)
            {
                LookForNext(TokenType.IN, "in", TokenType.NOTIN, ref typ); // x not in y => !(x in y)
            }

            if (typ == TokenType.IDENTIFIER)
            {
                ValString = ValTempString.ToString();
            }

            return typ;
        }


        private static bool IsExponent(char c)
        {
            return c == 'e' || c == 'E';
        }
        private static bool IsSign(char c)
        {
            return c == '+' || c == '-';
        }
        private static bool IsDotNetNumberChar(char c)
        {
            return c == '∞';
        }

        private TokenType ParseNumberString(TokenType typ, bool isComplex = false, bool is32bit = false)
        {
            switch (typ)
            {
                case TokenType.BINARY:
                    {
                        ValInteger = is32bit ? Convert.ToInt32(ValTempString.ToString(), 2)
                                             : Convert.ToInt64(ValTempString.ToString(), 2);
                        return isComplex ? TokenType.COMPLEX : TokenType.INTEGER;
                    }
                case TokenType.HEX:
                    {
                        if (!long.TryParse(ValTempString.ToString(), System.Globalization.NumberStyles.HexNumber, null, out ValInteger))
                        {
                            ErrorString = "failed to parse as hexadecimal";
                            return TokenType.UNKNOWN;
                        }
                        return isComplex ? TokenType.COMPLEX : TokenType.INTEGER;
                    }
                case TokenType.FLOAT:
                case TokenType.SCIENTIFIC:
                    {
                        if (!double.TryParse(ValTempString.ToString(), out ValFloat))
                        {
                            ErrorString = "failed to parse as float";
                            return TokenType.UNKNOWN;
                        }
                        return isComplex ? TokenType.COMPLEX : TokenType.FLOAT;
                    }
                case TokenType.INTEGER:
                    {
                        if (!long.TryParse(ValTempString.ToString(), out ValInteger))
                        {
                            if (!double.TryParse(ValTempString.ToString(), out ValFloat))
                            {
                                ErrorString = "failed to parse as integer";
                                return TokenType.UNKNOWN;
                            }
                            if (isComplex)
                            {
                                TokenComplex = TokenType.FLOAT;
                                return TokenType.COMPLEX;
                            }
                            return TokenType.FLOAT;
                        }
                        return isComplex ? TokenType.COMPLEX : TokenType.INTEGER;
                    }
            }
            return TokenType.UNKNOWN;
        }

        private TokenType ReadNumber(char start)
        {
            bool is32bit = false;
            TokenType typ = TokenType.INTEGER;
            ValTempString = new(start.ToString());   // ilk karakter

            NextChar();
            if (IsDotNetNumberChar(start)) // .NET nümerik karakteri
            {
                return ParseNumberString(TokenType.FLOAT, false);
            }

            switch (CurrentChar)
            {
                case 'x':  // Hexadecimal
                    {
                        typ = TokenType.HEX;

                        if (ValTempString[0] != '0')
                        {
                            ErrorString = "hexadecimal numbers has to start with a zero -> 0x...";
                            return TokenType.UNKNOWN;
                        }

                        NextChar();
                        ValTempString = new();

                        while (IsValidHexChar(ref CurrentChar))
                        {
                            ValTempString.Append(CurrentChar); NextChar();
                        }

                        int length = ValTempString.Length;
                        if (length > 16)
                        {
                            ErrorString = "hexadecimal number too long, max length is 16 for 64bit integer";
                            return TokenType.UNKNOWN;
                        }

                        ValTempString.Insert(0, new string('0', 16 - length));
                        break;
                    }
                case 'B':   // 64bit Binary
                case 'b':   // 32bit Binary
                    {
                        is32bit = CurrentChar == 'b';

                        typ = TokenType.BINARY;

                        if (ValTempString[0] != '0')
                        {
                            ErrorString = "binary numbers has to start with a zero -> 0b...";
                            return TokenType.UNKNOWN;
                        }

                        NextChar();
                        ValTempString = new();

                        while (CurrentChar == '0' || CurrentChar == '1')
                        {
                            ValTempString.Append(CurrentChar); NextChar();
                        }

                        int length = ValTempString.Length;
                        if (is32bit)
                        {
                            if (length > 32)
                            {
                                ErrorString = "32bit binary number too long, max length is 32";
                                return TokenType.UNKNOWN;
                            }
                        }
                        else if (length > 64)
                        {
                            ErrorString = "64bit binary number too long, max length is 64";
                            return TokenType.UNKNOWN;
                        }

                        ValTempString.Insert(0, new string('0', (is32bit ? 32 : 64) - length));
                        break;
                    }
                default:
                    {
                        while (CurrentChar == '.' || char.IsDigit(CurrentChar) || IsExponent(CurrentChar))
                        {
                            if (CurrentChar == '.')
                            {
                                typ = TokenType.FLOAT;
                            }
                            else if (IsExponent(CurrentChar))
                            {
                                if (ValTempString[^1] == '.')
                                {
                                    ErrorString = "expected digits after '.' ";
                                    return TokenType.UNKNOWN;
                                }

                                typ = TokenType.SCIENTIFIC;

                                ValTempString.Append(CurrentChar); NextChar();

                                if (IsSign(CurrentChar))
                                {
                                    ValTempString.Append(CurrentChar); NextChar();
                                }

                                if (!char.IsDigit(CurrentChar))
                                {
                                    ErrorString = "Wrong exponent value format";
                                    return TokenType.UNKNOWN;
                                }
                            }

                            ValTempString.Append(CurrentChar); NextChar();
                        }
                        break;
                    }
            }

            if (ValTempString[^1] == '.')
            {
                ErrorString = "expected digits after '.' ";
                return TokenType.UNKNOWN;
            }
            else if (CurrentChar == 'i')
            {
                switch (typ)
                {
                    case TokenType.INTEGER:
                    case TokenType.HEX:
                    case TokenType.BINARY:
                        {
                            TokenComplex = TokenType.INTEGER;
                            break;
                        }
                    default:
                        {
                            TokenComplex = TokenType.FLOAT;
                            break;
                        }
                }
                NextChar();
                return ParseNumberString(typ, true, is32bit);
            }
            return ParseNumberString(typ, false, is32bit);
        }

        public TokenType GetTokenTypeForChar(char c)
        {
            switch (c)
            {
                case ExMat.EndChar:
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
                case '~':
                    return TokenType.BNOT;
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
                    return TokenType.CURLYOPEN;
                case '}':
                    return TokenType.CURLYCLOSE;
                case '[':
                    return TokenType.SQUAREOPEN;
                case ']':
                    return TokenType.SQUARECLOSE;
                case '(':
                    return TokenType.ROUNDOPEN;
                case ')':
                    return TokenType.ROUNDCLOSE;
                case '?':
                    return TokenType.QMARK;
                case '\'':
                    return TokenType.MATTRANSPOSE;
                case '$':
                    return TokenType.LAMBDA;
                default:
                    ErrorString = "unknown symbol '" + c + "'";
                    return TokenType.UNKNOWN;
            }
        }

        public TokenType Lex()
        {
            PrevTokenLine = CurrentLine;
            while (CurrentChar != ExMat.EndChar)
            {
                switch (CurrentChar)
                {
                    #region Special characters
                    case '\t':
                    case '\r':
                    case ' ':
                        {
                            NextChar();
                            continue;
                        }
                    case '\n':
                        {
                            CurrentLine++;
                            SetAndReturnToken(TokenType.NEWLINE);
                            NextChar();
                            CurrentCol = 1;
                            continue;
                        }
                    case '#':
                        {
                            if (IsReadingMacroBlock)    // TO-DO
                            {
                                NextChar();
                                if (CurrentChar != '#')
                                {
                                    // SHOULDNT GO HERE 
                                    return TokenType.UNKNOWN;
                                }

                                MacroParamName = ReadMacroParam();
                                if (string.IsNullOrWhiteSpace(MacroParamName))
                                {
                                    return TokenType.UNKNOWN;
                                }
                                return SetAndReturnToken(TokenType.MACROPARAM);
                            }
                            else
                            {
                                NextChar();
                                TokenType typ = ReadId();
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
                                            ErrorString = "expected 'define' or 'end' after '#'";
                                            return TokenType.UNKNOWN;
                                        }
                                }
                            }

                        }
                    case '=':
                        {
                            NextChar();
                            if (CurrentChar == '=')
                            {
                                NextChar();
                                return SetAndReturnToken(TokenType.EQU);
                            }
                            else if (CurrentChar == '>')
                            {
                                NextChar();
                                return SetAndReturnToken(TokenType.ELEMENTDEF);
                            }
                            else
                            {
                                return SetAndReturnToken(TokenType.ASG);
                            }
                        }
                    case '<':
                        {
                            NextChar();
                            switch (CurrentChar)
                            {
                                case '=':
                                    {
                                        NextChar();
                                        return SetAndReturnToken(TokenType.LET);
                                    }
                                case '<':
                                    {
                                        NextChar();
                                        return SetAndReturnToken(TokenType.LSHF);
                                    }
                                case '>':
                                    {
                                        NextChar();
                                        return SetAndReturnToken(TokenType.NEWSLOT);
                                    }
                            }
                            return SetAndReturnToken(TokenType.LST);
                        }
                    case '>':
                        {
                            NextChar();
                            switch (CurrentChar)
                            {
                                case '=':
                                    {
                                        NextChar();
                                        return SetAndReturnToken(TokenType.GET);
                                    }
                                case '>':
                                    {
                                        NextChar();
                                        return SetAndReturnToken(TokenType.RSHF);
                                    }
                            }
                            return SetAndReturnToken(TokenType.GRT);
                        }
                    case '!':
                        {
                            NextChar();
                            if (CurrentChar == '=')
                            {
                                NextChar();
                                return SetAndReturnToken(TokenType.NEQ);
                            }
                            else
                            {
                                return SetAndReturnToken(TokenType.EXC);
                            }
                        }
                    case '\\':
                        {
                            ErrorString = "escape char outside string";
                            return TokenType.UNKNOWN;
                        }
                    case '\'':
                        {
                            NextChar();
                            return SetAndReturnToken(TokenType.MATTRANSPOSE);
                        }
                    case '"':
                        {
                            TokenType res;
                            if ((res = ReadString(CurrentChar)) != TokenType.UNKNOWN)
                            {
                                return SetAndReturnToken(res);
                            }
                            return TokenType.UNKNOWN;
                        }
                    case '$':
                        {
                            NextChar();
                            if (CurrentChar == '\"')
                            {
                                TokenType res;
                                if ((res = ReadVerboseString(CurrentChar)) != TokenType.UNKNOWN)
                                {
                                    return SetAndReturnToken(res);
                                }
                                return TokenType.UNKNOWN;
                            }
                            else
                            {
                                return SetAndReturnToken(TokenType.LAMBDA);
                            }
                        }
                    case '@':
                        {
                            if (ReadSpace(CurrentChar) != TokenType.UNKNOWN)
                            {
                                NextChar();
                                return SetAndReturnToken(TokenType.SPACE);
                            }
                            else
                            {
                                ErrorString = "expected the pattern @(Z|R|N|C|E)[+-]?('\\d(\\*\\d)*)?@ for spaces";
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
                            TokenType tmp = GetTokenTypeForChar(CurrentChar);
                            NextChar();
                            return SetAndReturnToken(tmp);
                        }
                    case '.':
                        {
                            NextChar();
                            switch (CurrentChar)
                            {
                                case '/':
                                    {
                                        NextChar();
                                        return SetAndReturnToken(TokenType.ATTRIBUTEFINISH);
                                    }
                                case '*':
                                    {
                                        NextChar();
                                        return SetAndReturnToken(TokenType.MATMLT);
                                    }
                                case '.':
                                    {
                                        NextChar();
                                        if (CurrentChar == '.')
                                        {
                                            NextChar();
                                            return SetAndReturnToken(TokenType.VARGS);
                                        }
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
                            NextChar();
                            if (CurrentChar == '&')
                            {
                                NextChar();
                                return SetAndReturnToken(TokenType.AND);
                            }
                            else
                            {
                                return SetAndReturnToken(TokenType.BAND);
                            }
                        }
                    case '|':
                        {
                            NextChar();
                            if (CurrentChar == '|')
                            {
                                NextChar();
                                return SetAndReturnToken(TokenType.OR);
                            }
                            else
                            {
                                return SetAndReturnToken(TokenType.BOR);
                            }
                        }
                    case '^':
                        {
                            NextChar();
                            return SetAndReturnToken(TokenType.BXOR);
                        }
                    case ':':
                        {
                            NextChar();
                            if (CurrentChar == ':')
                            {
                                NextChar();
                                return SetAndReturnToken(TokenType.GLOBAL);
                            }
                            else
                            {
                                return SetAndReturnToken(TokenType.COL);
                            }
                        }
                    case '+':
                        {
                            NextChar();
                            switch (CurrentChar)
                            {
                                case '=':
                                    {
                                        NextChar();
                                        return SetAndReturnToken(TokenType.ADDEQ);
                                    }
                                case '+':
                                    {
                                        NextChar();
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
                            NextChar();
                            switch (CurrentChar)
                            {
                                case '=':
                                    {
                                        NextChar();
                                        return SetAndReturnToken(TokenType.SUBEQ);
                                    }
                                case '-':
                                    {
                                        NextChar();
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
                            NextChar();
                            switch (CurrentChar)
                            {
                                case '.':
                                    {
                                        NextChar();
                                        if (CurrentChar != '*')
                                        {
                                            return TokenType.UNKNOWN;
                                        }
                                        NextChar();
                                        return SetAndReturnToken(TokenType.CARTESIAN);
                                    }
                                case '*':
                                    {
                                        NextChar();
                                        return SetAndReturnToken(TokenType.EXP);
                                    }
                                case '=':
                                    {
                                        NextChar();
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
                            NextChar();
                            switch (CurrentChar)
                            {
                                case '=':
                                    {
                                        NextChar();
                                        return SetAndReturnToken(TokenType.DIVEQ);
                                    }
                                case '/':
                                    {
                                        SkipComment();
                                        continue;
                                    }
                                case '*':
                                    {
                                        NextChar();
                                        if (!SkipBlockComment())
                                        {
                                            return TokenType.UNKNOWN;
                                        }
                                        continue;
                                    }
                                case '.':
                                    {
                                        NextChar();
                                        return SetAndReturnToken(TokenType.ATTRIBUTEBEGIN);
                                    }
                                default:
                                    {
                                        return SetAndReturnToken(TokenType.DIV);
                                    }
                            }
                        }
                    case '%':
                        {
                            NextChar();
                            switch (CurrentChar)
                            {
                                case '=':
                                    {
                                        NextChar();
                                        return SetAndReturnToken(TokenType.MODEQ);
                                    }
                                default:
                                    {
                                        return SetAndReturnToken(TokenType.MOD);
                                    }
                            }
                        }
                    case ExMat.EndChar:
                        return TokenType.ENDLINE;
                    #endregion

                    #region Number, Identifier, Other Special character or Unknown
                    default:
                        {
                            if (char.IsDigit(CurrentChar) || IsDotNetNumberChar(CurrentChar))
                            {
                                return SetAndReturnToken(ReadNumber(CurrentChar));  // Sayı
                            }
                            else if (char.IsLetter(CurrentChar) || CurrentChar == '_')
                            {
                                return SetAndReturnToken(ReadId());     // Tanımlayıcı
                            }
                            else  // Bilinmeyen
                            {
                                char tmp = CurrentChar;
                                if (char.IsControl(tmp))
                                {
                                    ErrorString = "Unexpected control character " + tmp.ToString();
                                    return TokenType.UNKNOWN;
                                }

                                NextChar();
                                return SetAndReturnToken(GetTokenTypeForChar(tmp));
                            }
                        }
                        #endregion
                }
            }
            return TokenType.ENDLINE;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disposer.DisposeList(ref MacroParams);
                    Disposer.DisposeObjects(ValSpace);

                    ErrorString = null;
                    ValTempString = null;
                    ValString = null;
                    MacroParamName = null;
                    MacroBlock = null;

                    _source = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private object GetDebuggerDisplay()
        {
            return string.Format("Line: {0}, Column: {1}, Char: {2}, Token: {3}", CurrentLine, CurrentCol, CurrentChar, TokenCurr);
        }
    }
}
