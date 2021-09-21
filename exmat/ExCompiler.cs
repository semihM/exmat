using System;
using System.Collections.Generic;
using System.Globalization;
using ExMat.API;
using ExMat.Exceptions;
using ExMat.FuncPrototype;
using ExMat.Lexer;
using ExMat.Objects;
using ExMat.OPs;
using ExMat.States;
using ExMat.Token;
using ExMat.VM;

namespace ExMat.Compiler
{
    internal sealed class ExCompiler : IDisposable
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

        internal void Dispose(bool disposing)
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

        public bool AddToErrorMessage(string msg)
        {
            if (string.IsNullOrEmpty(ErrorString))
            {
                ErrorString = "[ERROR] " + msg;
            }
            else
            {
                ErrorString += "\n[ERROR] " + msg;
            }

            return false;
        }

        public void Throw(string msg, ExExceptionType type = ExExceptionType.COMPILER)
        {
            ExApi.Throw(msg, VM, type);
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
                    FunctionState.AddInstr(ExOperationCode.CLOSE, 0, CurrentScope.nLocal, 0, 0);
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

        public bool Compile(ref ExObject main)
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

            // Kod dizisi sonuna kadar derle
            while (CurrentToken != TokenType.ENDLINE)
            {
                if (!ProcessStatement())
                {
                    return false;
                }
                if (Lexer.TokenPrev != TokenType.CURLYCLOSE
                    && Lexer.TokenPrev != TokenType.SMC
                    && !CheckSMC())
                {
                    return false;
                }
            }
            // Değişkenleri yığını temizle
            FunctionState.SetLocalStackSize(0);
            FunctionState.AddLineInfo(Lexer.CurrentLine, StoreLineInfos, true);
            // Sanal makine yığınında kalan son değeri dön
            //      -> İlk argüman(ExMat.InvalidArgument): Bir önceki komutun referans azaltma işlemini iptal eder
            //      -> İkinci argüman: Sanal bellek taban indeksinin üzerine eklenerek en son değeri bulur
            //      -> Üçüncü argüman: Yalnızca interaktif konsolda değer dönülmesini sağlar
            FunctionState.AddInstr(ExOperationCode.RETURN, ExMat.InvalidArgument, 2, VM.IsInteractive ? 1 : 0, 0);

            // "main"'i çağırabilir bir fonkisyon haline getir 
            main = new(FunctionState.CreatePrototype());
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

        public ExObject ExpectConstableValue()
        {
            ExObject res;

            switch (CurrentToken)
            {
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
                case TokenType.LITERAL:
                    {
                        res = new(Lexer.ValString);
                        break;
                    }
                case TokenType.SPACE:
                    {
                        res = new(Lexer.ValSpace);
                        break;
                    }
                case TokenType.SUB:
                    {
                        if (!ReadAndSetToken())
                        {
                            return null;
                        }
                        switch (CurrentToken)
                        {
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
                            default:
                                {
                                    AddToErrorMessage("expected scalar after '-'");
                                    return null;
                                }
                        }
                        break;
                    }
                default:
                    {
                        AddToErrorMessage("expected const-able: int, float, string, space");
                        return null;
                    }
            }

            ReadAndSetToken();
            return res;
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

            ExObject res;
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
                default:
                    {
                        res = new();
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
                    && Lexer.TokenPrev != TokenType.SMC
                    && !CheckSMC())                // && (!macro || (CurrentToken != TokenType.MACROEND)))
                {
                    // İfade sonu kontrolü
                    return false;
                }
            }
            return true;
        }

        public bool ProcessCurlyOpenStatement(bool closeScope)
        {
            ExScope scp = CreateScope();

            if (!ReadAndSetToken()
                || !ProcessStatements()
                || Expect(TokenType.CURLYCLOSE) == null)
            {
                return false;
            }

            ReleaseScope(scp, closeScope);
            return true;
        }

        public bool ProcessConstStatement()
        {
            if (!ReadAndSetToken())
            {
                return false;
            }

            ExObject name = Expect(TokenType.IDENTIFIER);
            if (name == null)
            {
                return false;
            }

            if (FunctionState.SharedState.Consts.ContainsKey(name.GetString()))
            {
                return AddToErrorMessage($"a constant named '{name.GetString()}' already exists, use 'consts' function to force-update constants");
            }

            if (Expect(TokenType.ASG) == null)
            {
                return AddToErrorMessage("expected '=' after const identifier");
            }

            ExObject val = ExpectConstableValue();
            if (val == null || !CheckSMC())
            {
                return false;
            }

            FunctionState.SharedState.Consts.Add(name.GetString(), new(val));
            return true;
        }

        public bool ProcessReturnStatement()
        {
            ExOperationCode op = ExOperationCode.RETURN;
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
                FunctionState.AddInstr(op, ExMat.InvalidArgument, 0, FunctionState.GetLocalVariablesCount(), 0);
            }
            return true;
        }

        public bool ProcessBreakStatement()
        {
            if (FunctionState.BreakTargetsList.Count <= 0)      // iterasyon bloğu kontrolü
            {
                AddToErrorMessage("'break' has to be in a breakable block");
                return false;
            }

            DoOuterControl();   // Dış değişken referansları varsa referansları azalt
            FunctionState.AddInstr(ExOperationCode.JMP, 0, ExMat.InvalidArgument, 0, 0);
            // Komut indeksini listeye ekle
            FunctionState.BreakList.Add(FunctionState.GetCurrPos());

            return ReadAndSetToken();
        }

        public bool ProcessContinueStatement()
        {
            if (FunctionState.ContinueTargetList.Count <= 0)    // iterasyon bloğu kontrolü
            {
                AddToErrorMessage("'continue' has to be in a breakable block");
                return false;
            }
            DoOuterControl();   // Dış değişken referansları varsa referansları azalt
            FunctionState.AddInstr(ExOperationCode.JMP, 0, ExMat.InvalidArgument, 0, 0);
            // Komut indeksini listeye ekle
            FunctionState.ContinueList.Add(FunctionState.GetCurrPos());

            return ReadAndSetToken();
        }

        public bool ProcessStatement(bool closeScope = true)   // İfadeyi derle                             //, bool macro = false)
        {
            bool processed;
            switch (CurrentToken)
            {
                case TokenType.SMC:         // ;
                    {
                        processed = ReadAndSetToken();
                        break;
                    }
                case TokenType.CURLYOPEN:   // {
                    {
                        processed = ProcessCurlyOpenStatement(closeScope);
                        break;
                    }
                case TokenType.VAR:     // var
                    {
                        processed = ProcessVarAsgStatement();
                        break;
                    }
                case TokenType.CONST:     // const
                    {
                        processed = ProcessConstStatement();
                        break;
                    }
                case TokenType.FUNCTION:
                    {
                        processed = ProcessFunctionStatement();
                        break;
                    }
                case TokenType.IF:      // if
                    {
                        processed = ProcessIfStatement();
                        break;
                    }

                case TokenType.FOR:     // for
                    {
                        processed = ProcessForStatement();
                        break;
                    }
                case TokenType.FOREACH:     // for
                    {
                        processed = ProcessForEachStatement();
                        break;
                    }
                case TokenType.RULE:
                    {
                        processed = ProcessRuleAsgStatement();
                        break;
                    }
                case TokenType.CLASS:
                    {
                        processed = ProcessClassStatement();
                        break;
                    }
                case TokenType.SEQUENCE:
                    {
                        processed = ProcessSequenceStatement();
                        break;
                    }
                case TokenType.CLUSTER:
                    {
                        processed = ProcessClusterAsgStatement();
                        break;
                    }
                case TokenType.RETURN:
                    {
                        processed = ProcessReturnStatement();
                        break;
                    }
                case TokenType.BREAK:
                    {
                        processed = ProcessBreakStatement();
                        break;
                    }
                case TokenType.CONTINUE:
                    {
                        processed = ProcessContinueStatement();
                        break;
                    }
                default:
                    {
                        processed = ExSepExp();
                        if (processed)
                        {
                            FunctionState.DiscardTopTarget();
                            FunctionState.NotSnoozed = false;
                            return true;
                        }
                        return false;
                    }
            }

            if (!processed)
            {
                return false;
            }

            FunctionState.NotSnoozed = false;
            return true;
        }

        public void DoOuterControl()
        {
            if (FunctionState.GetLocalVariablesCount() != CurrentScope.nLocal
                && FunctionState.GetOuterSize(CurrentScope.nLocal) > 0)
            {
                FunctionState.AddInstr(ExOperationCode.CLOSE, 0, CurrentScope.nLocal, 0, 0);
            }
        }

        public List<int> CreateBreakableBlock()
        {
            // Geçici hedef indeksi oluştur. 'break' ve 'continue' izin verir
            FunctionState.BreakTargetsList.Add(0);
            FunctionState.ContinueTargetList.Add(0);
            return new(2) { FunctionState.BreakList.Count, FunctionState.ContinueList.Count };
        }

        public void ReleaseBreakableBlock(List<int> bc, int index)
        {
            if ((bc[0] = FunctionState.BreakList.Count - bc[0]) > 0)
            {
                DoBreakControl(FunctionState, bc[0]);   // İterasyonu durdur
            }
            if ((bc[1] = FunctionState.ContinueList.Count - bc[1]) > 0)
            {
                DoContinueControl(FunctionState, bc[1], index); // İterasyonu atla
            }

            if (FunctionState.BreakList.Count > 0)  // Varsa son durdurma listesini sil
            {
                FunctionState.BreakList.RemoveAt(FunctionState.BreakList.Count - 1);
            }
            if (FunctionState.ContinueList.Count > 0)  // Varsa son atlama listesini sil
            {
                FunctionState.ContinueList.RemoveAt(FunctionState.ContinueList.Count - 1);
            }
        }

        public static void DoBreakControl(ExFState fs, int count)
        {
            while (count-- > 0) // Gerekli atlama işlemleri(JMP vb.) argümanlarını güncelle
            {
                int p = fs.BreakList[^1]; fs.BreakList.RemoveAt(fs.BreakList.Count - 1);
                fs.SetInstrParams(p, 0, fs.GetCurrPos() - p, 0, 0);
            }
        }

        public static void DoContinueControl(ExFState fs, int count, int index)
        {
            while (count-- > 0) // Gerekli atlama işlemleri(JMP vb.) argümanlarını güncelle
            {
                int p = fs.ContinueList[^1]; fs.ContinueList.RemoveAt(fs.ContinueList.Count - 1);
                fs.SetInstrParams(p, 0, index - p, 0, 0);
            }
        }

        public bool ProcessIfStatement()
        {
            int jpos;
            bool has_else = false;

            // '(' bekle, sıralı ifadeleri derle , ')' bekle
            if (!ReadAndSetToken() || Expect(TokenType.ROUNDOPEN) == null || !ExSepExp() || Expect(TokenType.ROUNDCLOSE) == null)
            {
                return false;
            }
            // Son ifadenin doğru/yanlış kontrolünü yap, yanlışsa komutları atla
            FunctionState.AddInstr(ExOperationCode.JZ, FunctionState.PopTarget(), 0, 0, 0);
            int jnpos = FunctionState.GetCurrPos();     // Son komutun listedeki indeksini sakla

            ExScope old = CreateScope();    // Yeni bir çerçeve başlat
            if (!ProcessStatement())        // İçerdeki ifadeleri derle
            {
                return false;
            }
            if (CurrentToken != TokenType.CURLYCLOSE
                && CurrentToken != TokenType.ELSE
                && !CheckSMC())
            {
                // Tek satırlık ifade ise ifadenin sonlandırıldığını kontrol et
                return false;
            }
            ReleaseScope(old);      // Eski çerçeveye dön

            int epos = FunctionState.GetCurrPos();  // 'else' komutu olmasına karşın son komut indeksini sakla
            if (CurrentToken == TokenType.ELSE)
            {
                has_else = true;          // 'else' bulundu
                old = CreateScope();    // Yeni bir çerçeve başlat
                // Komutları atla, metot sonunda atlanacak sayı UpdateInstructionArgument ile hesaplanır
                FunctionState.AddInstr(ExOperationCode.JMP, 0, 0, 0, 0);
                jpos = FunctionState.GetCurrPos();      // Atlama komutu liste indeksini sakla

                if (!ReadAndSetToken() || !ProcessStatement())  // İçerdeki ifadeleri derle
                {
                    return false;
                }

                if (Lexer.TokenPrev != TokenType.CURLYCLOSE
                    && !CheckSMC())    // Tek satırlık ifade ise sonlandırıldığından emin ol
                {
                    return false;
                }
                ReleaseScope(old);  // Eski çerçeveye dön
                // OPC.JMP komutunun 1. argümanı =  atlanacak komut sayısı
                FunctionState.UpdateInstructionArgument(jpos, 1, FunctionState.GetCurrPos() - jpos);
            }
            // 'if' bloğunun koşul sağlanmadığın atlanması için:
            //           OPC.JZ komutunun 1. argümanı = içerdeki komut sayısı + (else varsa 1 yoksa 0)
            FunctionState.UpdateInstructionArgument(jnpos, 1, epos - jnpos + (has_else ? 1 : 0));
            return true;
        }

        private bool ProcessForStatementInit(out ExScope scp)
        {
            scp = null;

            if (!ReadAndSetToken())
            {
                return false;
            }

            scp = CreateScope();                // Yeni çerçeve oluştur

            if (Expect(TokenType.ROUNDOPEN) == null)    // '(' bekle
            {
                return false;
            }

            if (CurrentToken == TokenType.VAR)          // Varsa değişken ataması yap
            {
                if (!ProcessVarAsgStatement())
                {
                    return false;
                }
            }
            else if (CurrentToken != TokenType.SMC)     // ';' değilse atama ifadelerini derle
            {
                if (!ExSepExp())
                {
                    return false;
                }
                FunctionState.PopTarget();
            }

            return true;
        }

        private bool ProcessForStatementCond(out int jpos, out int jzpos)
        {
            // ilk kısımdaki atamaları bağımsız yap
            FunctionState.NotSnoozed = false;
            // koşullar doğru olduğunda atlanılacak indeksi sakla
            jpos = FunctionState.GetCurrPos();
            jzpos = -1;

            if (CurrentToken != TokenType.SMC)  // ';' yoksa koşul ifadesi verilmiştir
            {
                if (!ExSepExp())    // Koşulları derle
                {
                    return false;
                }
                // Koşul yanlış ise komutları atla
                FunctionState.AddInstr(ExOperationCode.JZ, FunctionState.PopTarget(), 0, 0, 0);
                // Atlanacak komut sayısını sakla
                jzpos = FunctionState.GetCurrPos();
            }

            return true;
        }

        private bool ProcessForStatementIncr(out List<ExInstr> instrs, out int exp_size)
        {
            instrs = null;
            exp_size = 0;

            FunctionState.NotSnoozed = false;   // ikinci kısımdaki koşulları bağımsız yap

            int exp_start = FunctionState.GetCurrPos() + 1; // son kısmın başlangıç indeksi
            if (CurrentToken != TokenType.ROUNDCLOSE) // son kısımda varsa ifadeleri derle
            {
                if (!ExSepExp())
                {
                    return false;
                }
                FunctionState.PopTarget();
            }

            FunctionState.NotSnoozed = false;   // son komutu bağımsız yap
            // son kısımda kaç komut oluşturulduğunu hesapla
            int exp_end = FunctionState.GetCurrPos();
            exp_size = exp_end - exp_start + 1;

            // her bir iterasyondan sonra işlenecek komutların listesini oluştur
            instrs = new();

            if (exp_size > 0)
            {
                instrs = new(exp_size);
                for (int i = 0; i < exp_size; i++)
                {
                    instrs.Add(FunctionState.Instructions[exp_start + i]);
                }
                for (int i = 0; i < exp_size; i++)
                {
                    FunctionState.Instructions.RemoveAt(FunctionState.Instructions.Count - 1);
                }
            }

            return true;
        }

        public bool ProcessForEachStatement()
        {
            ExObject indexname, idname;
            if (!ReadAndSetToken()
                || Expect(TokenType.ROUNDOPEN) == null
                || (idname = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            if (CurrentToken is TokenType.SEP)
            {
                indexname = new(idname);
                if (!ReadAndSetToken()
                    || (idname = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }
            }
            else
            {
                indexname = new(FunctionState.CreateString(ExMat.ForeachSingleIdxName));
            }

            if (Expect(TokenType.IN) == null)
            {
                return false;
            }

            ExScope scp = CreateScope();

            if (!ExExp()
                || Expect(TokenType.ROUNDCLOSE) == null)
            {
                return false;
            }

            int block = FunctionState.TopTarget();

            int index = FunctionState.PushVar(indexname);
            FunctionState.AddInstr(ExOperationCode.LOADNULL, index, 1, 0, 0);

            int id = FunctionState.PushVar(idname);
            FunctionState.AddInstr(ExOperationCode.LOADNULL, id, 1, 0, 0);

            int iter = FunctionState.PushVar(FunctionState.CreateString(ExMat.ForeachIteratorName));
            FunctionState.AddInstr(ExOperationCode.LOADNULL, iter, 1, 0, 0);

            int jmp = FunctionState.GetCurrPos();
            FunctionState.AddInstr(ExOperationCode.FOREACH, block, 0, index, 0);

            int fpos = FunctionState.GetCurrPos();
            FunctionState.AddInstr(ExOperationCode.POSTFOREACH, block, 0, index, 0);

            List<int> bc = CreateBreakableBlock();

            if (!ProcessStatement())
            {
                return false;
            }

            FunctionState.AddInstr(ExOperationCode.JMP, 0, jmp - FunctionState.GetCurrPos() - 1, 0, 0);
            FunctionState.UpdateInstructionArgument(fpos, 1, FunctionState.GetCurrPos() - fpos);
            FunctionState.UpdateInstructionArgument(fpos + 1, 1, FunctionState.GetCurrPos() - fpos);

            ReleaseBreakableBlock(bc, fpos - 1);

            FunctionState.PopTarget();

            ReleaseScope(scp);

            return true;
        }

        public bool ProcessForStatement()
        {
            if (!ProcessForStatementInit(out ExScope scp)
                || Expect(TokenType.SMC) == null

                || !ProcessForStatementCond(out int jpos, out int jzpos)
                || Expect(TokenType.SMC) == null

                || !ProcessForStatementIncr(out List<ExInstr> instrs, out int exp_size)
                || Expect(TokenType.ROUNDCLOSE) == null)
            {
                return false;
            }

            // Durdurulabilen ve atlanabilen blok oluştur
            List<int> bc = CreateBreakableBlock();

            if (!ProcessStatement())    // Her iterasyonda işlenecek komutları oluştur
            {
                return false;
            }
            // iterasyon durdurulmasında atlanacak komut sayısı
            int exit_target = FunctionState.GetCurrPos();
            // Varsa '(' ve  ')' arasında 2. ';' sonrası ifadelerin komutlarını sona ekle 
            if (exp_size > 0)
            {
                for (int i = 0; i < exp_size; i++)
                {
                    FunctionState.AddInstr(instrs[i]);
                }
            }

            // İterasyon bitiminde komutların atlanmasını sağla
            FunctionState.AddInstr(ExOperationCode.JMP, 0, jpos - FunctionState.GetCurrPos() - 1, 0, 0);

            if (jzpos > 0)  // Koşul verişmiş ise atlanacak komut sayısını güncelle
            {
                FunctionState.UpdateInstructionArgument(jzpos, 1, FunctionState.GetCurrPos() - jzpos);
            }

            ReleaseScope(scp);                      // Eski çerçeveye dön
            ReleaseBreakableBlock(bc, exit_target); // İterasyon bloğundan çık
            return true;
        }

        private bool SequenceAddParamDefVal(ExFState f_state)
        {
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
                return true;
            }
            else // TO-DO add = for referencing global and do get ops
            {
                AddToErrorMessage("expected '=' for a sequence parameter default value");
                return false;
            }
        }

        private bool SequenceAddParam(ref ExObject pname, ref int pcount, ExFState f_state, bool neg) // TO-DO Allow extra parameters ?
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

                if (!SequenceAddParamDefVal(f_state))
                {
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

            return true;
        }

        private bool SequenceConstraint(ref ExObject pname, ref int pcount, ExFState f_state, bool neg)
        {
            if (pname.Type == ExObjType.INTEGER)
            {
                if (neg)
                {
                    pname.Value.i_Int *= -1;
                }

                pname = new(pname.GetInt().ToString(CultureInfo.CurrentCulture));
            }
            else if (pname.Type == ExObjType.FLOAT)
            {
                if (neg)
                {
                    pname.Value.f_Float *= -1;
                }

                pname = new(pname.GetFloat().ToString(CultureInfo.CurrentCulture));
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

            return true;
        }

        private bool SequenceInit(ref int pcount, ExFState f_state, bool neg)
        {
            ExObject pname;
            if ((pname = Expect(TokenType.INTEGER)) == null
                && (pname = Expect(TokenType.FLOAT)) == null)
            {
                if (!SequenceAddParam(ref pname, ref pcount, f_state, neg))
                {
                    return false;
                }
            }
            else
            {
                if (!SequenceConstraint(ref pname, ref pcount, f_state, neg))
                {
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
            return true;
        }

        public bool ExSequenceCreate(ExObject o)
        {
            ExFState f_state = FunctionState.PushChildState(VM.SharedState);
            f_state.Name = o;

            f_state.AddParam(FunctionState.CreateString(ExMat.ThisName));
            f_state.AddParam(FunctionState.CreateString(ExMat.SequenceParameter));

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

                if (!SequenceInit(ref pcount, f_state, neg))
                {
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
            f_state.AddInstr(ExOperationCode.RETURN, 1, FunctionState.PopTarget(), 0, 0);

            f_state.AddLineInfo(Lexer.TokenPrev == TokenType.NEWLINE ? Lexer.PrevTokenLine : Lexer.CurrentLine, StoreLineInfos, true);
            f_state.AddInstr(ExOperationCode.RETURN, ExMat.InvalidArgument, 0, 0, 0);
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
            FunctionState.AddInstr(ExOperationCode.LOAD, FunctionState.PushTarget(), FunctionState.GetLiteral(v), 0, 0);

            if (Expect(TokenType.ROUNDOPEN) == null || !ExSequenceCreate(v))
            {
                return false;
            }

            FunctionState.AddInstr(ExOperationCode.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);

            AddBasicDerefInstr(ExOperationCode.NEWSLOT);

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
            FunctionState.AddInstr(ExOperationCode.LOAD, FunctionState.PushTarget(), FunctionState.GetLiteral(v), 0, 0);

            if (Expect(TokenType.CURLYOPEN) == null || !ExClusterCreate(v))
            {
                return false;
            }

            FunctionState.AddInstr(ExOperationCode.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);

            AddBasicDerefInstr(ExOperationCode.NEWSLOT);

            FunctionState.PopTarget();

            return true;
        }

        private bool ClusterInit(ref int pcount, ExFState f_state)
        {
            ExObject pname;
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

            return ClusterInitSep();
        }

        private bool ClusterInitSep()
        {
            if (CurrentToken == TokenType.SEP)
            {
                if (!ReadAndSetToken())
                {
                    return false;
                }
            }
            else if (CurrentToken is not TokenType.SMC and not TokenType.CURLYCLOSE)
            {
                AddToErrorMessage("expected '}' ',' '=>' or ';' for cluster definition");
                return false;
            }

            return true;
        }

        private bool ClusterElementDef()
        {
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

            return CurrentToken == TokenType.ELEMENTDEF
                || AddToErrorMessage("expected '=>' to define elements of cluster");
        }

        public bool ExClusterCreate(ExObject o)
        {
            ExFState f_state = FunctionState.PushChildState(VM.SharedState);
            f_state.Name = o;

            f_state.AddParam(FunctionState.CreateString(ExMat.ThisName));
            f_state.Source = new(Source);
            int pcount = 0;

            while (CurrentToken != TokenType.SMC)
            {
                if (!ClusterInit(ref pcount, f_state))
                {
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

            if (!ClusterElementDef())
            {
                return false;
            }

            //
            FunctionState.AddInstr(ExOperationCode.JZ, FunctionState.PopTarget(), 0, 0, 0);

            int jzp = FunctionState.GetCurrPos();
            int t = FunctionState.PushTarget();

            if (!ReadAndSetToken() || !ExExp())
            {
                return false;
            }

            int f = FunctionState.PopTarget();
            if (t != f)
            {
                FunctionState.AddInstr(ExOperationCode.MOVE, t, f, 0, 0);
            }
            int end_f = FunctionState.GetCurrPos();

            FunctionState.AddInstr(ExOperationCode.JMP, 0, 0, 0, 0);

            int jmp = FunctionState.GetCurrPos();

            FunctionState.AddInstr(ExOperationCode.LOADBOOLEAN, FunctionState.PushTarget(), 0, 0, 0);

            int s = FunctionState.PopTarget();
            if (t != s)
            {
                FunctionState.AddInstr(ExOperationCode.MOVE, t, s, 0, 0);
            }

            FunctionState.UpdateInstructionArgument(jmp, 1, FunctionState.GetCurrPos() - jmp);
            FunctionState.UpdateInstructionArgument(jzp, 1, end_f - jzp + 1);
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

            f_state.AddInstr(ExOperationCode.RETURN, 1, FunctionState.PopTarget(), 0, 0);
            f_state.AddLineInfo(Lexer.TokenPrev == TokenType.NEWLINE ? Lexer.PrevTokenLine : Lexer.CurrentLine, StoreLineInfos, true);
            f_state.AddInstr(ExOperationCode.RETURN, ExMat.InvalidArgument, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExPrototype fpro = f_state.CreatePrototype();
            fpro.ClosureType = ExClosureType.CLUSTER;

            FunctionState = tmp;
            FunctionState.Functions.Add(fpro);
            FunctionState.PopChildState();

            return true;
        }

        private bool RuleInit(ExFState f_state)
        {
            ExObject pname;
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

            return true;
        }

        public bool ExRuleCreate(ExObject o)
        {
            ExFState f_state = FunctionState.PushChildState(VM.SharedState);
            f_state.Name = o;

            f_state.AddParam(FunctionState.CreateString(ExMat.ThisName));
            f_state.Source = new(Source);

            while (CurrentToken != TokenType.ROUNDCLOSE)
            {
                if (!RuleInit(f_state))
                {
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
            f_state.AddInstr(ExOperationCode.RETURNBOOL, 1, FunctionState.PopTarget(), 0, 0);

            f_state.AddLineInfo(Lexer.TokenPrev == TokenType.NEWLINE ? Lexer.PrevTokenLine : Lexer.CurrentLine, StoreLineInfos, true);
            f_state.AddInstr(ExOperationCode.RETURN, ExMat.InvalidArgument, 0, 0, 0);
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
            FunctionState.AddInstr(ExOperationCode.LOAD, FunctionState.PushTarget(), FunctionState.GetLiteral(v), 0, 0);

            if (Expect(TokenType.ROUNDOPEN) == null || !ExRuleCreate(v))
            {
                return false;
            }

            FunctionState.AddInstr(ExOperationCode.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);

            AddBasicDerefInstr(ExOperationCode.NEWSLOT);

            return true;
        }

        private bool ProcessVarAsgSpecialInit(out bool done)
        {
            ExObject v;
            done = true;
            if (CurrentToken == TokenType.FUNCTION)
            {
                if (!ReadAndSetToken()
                    || (v = Expect(TokenType.IDENTIFIER)) == null
                    || Expect(TokenType.ROUNDOPEN) == null
                    || !ExFuncCreate(v))
                {
                    return false;
                }

                FunctionState.AddInstr(ExOperationCode.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);
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

                FunctionState.AddInstr(ExOperationCode.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);
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

                FunctionState.AddInstr(ExOperationCode.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);
                FunctionState.PopTarget();
                FunctionState.PushVar(v);
                return true;
            }
            else
            {
                done = false;
                return true;
            }
        }

        private bool VarAsg()
        {
            if (CurrentToken == TokenType.ASG)      // '=' bekle
            {
                if (!ReadAndSetToken() || !ExExp())     // Sağ tarafı derle
                {
                    return false;
                }

                int source = FunctionState.PopTarget();
                int destination = FunctionState.PushTarget();

                // kaynak != hedef ise başka bir değişkenin değeri bu değişkene atanıyordur
                if (destination != source)
                {
                    FunctionState.AddInstr(ExOperationCode.MOVE, destination, source, 0, 0);
                }
            }
            else // '=' yoksa 'null' ata
            {
                FunctionState.AddInstr(ExOperationCode.LOADNULL, FunctionState.PushTarget(), 1, 0, 0);
            }
            return true;
        }

        public bool ProcessVarAsgStatement()
        {
            if (!ReadAndSetToken()
                || !ProcessVarAsgSpecialInit(out bool done))
            {
                return false;
            }

            if (done)
            {
                return true;
            }

            ExObject v;
            while (true)
            {
                if ((v = Expect(TokenType.IDENTIFIER)) == null
                    || !VarAsg())
                {
                    return false;
                }

                FunctionState.PopTarget();      // Yığını temizle
                FunctionState.PushVar(v);       // Değişkeni ekle
                if (CurrentToken == TokenType.SEP)  // ',' ile ayrılmış sıradaki değişkeni ara
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                else // değişken oluşturmayı bitir
                {
                    break;
                }
            }
            return true;
        }

        public bool ProcessFunctionStatement()  // Fonksiyonu global tabloda tanımlar
        {
            ExObject idx;

            if (!ReadAndSetToken() || (idx = Expect(TokenType.IDENTIFIER)) == null) // Tanımlayıcı bekle
            {
                return false;
            }
            // Global tabloya referans, meetot sonunda bulunan OPC.NEWSLOT komutunun
            //      yeni bir slot oluşturmak için bu referensa ihtiyacı vardır
            FunctionState.PushTarget(0);
            // Fonksiyon isminin sanal belleğe yüklenme
            FunctionState.AddInstr(ExOperationCode.LOAD, FunctionState.PushTarget(), FunctionState.GetLiteral(idx), 0, 0);

            #region _
            // Sınıfa ait metot olarak yazılıyor ise sınıfı bulma komutu ekle
            if (CurrentToken == TokenType.GLOBAL)
            {
                AddBasicOpInstr(ExOperationCode.GET);   // Okunan son 2 tanımlayıcı ile istenen metot ve sınıfın alınması sağlanır
            }

            while (CurrentToken == TokenType.GLOBAL)
            {
                if (!ReadAndSetToken() || (idx = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                FunctionState.AddInstr(ExOperationCode.LOAD, FunctionState.PushTarget(), FunctionState.GetLiteral(idx), 0, 0);

                if (CurrentToken == TokenType.GLOBAL)
                {
                    AddBasicOpInstr(ExOperationCode.GET);
                }
            }
            #endregion
            // '(' sembolü bekle, bulunursa ExFuncCreate metotunu fonksiyonun ismi ile çağır
            if (Expect(TokenType.ROUNDOPEN) == null || !ExFuncCreate(idx))
            {
                return false;
            }

            // Hazırlanan fonksiyonu sanal belleğe yerleştir
            FunctionState.AddInstr(ExOperationCode.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);
            // Global tabloda yeni slot oluşturma işlemi
            AddBasicDerefInstr(ExOperationCode.NEWSLOT);
            // Yığından çıkart
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
            else if (ExpressionState.Type is ExEType.OBJECT or ExEType.BASE)
            {
                if (!ExClassResolveExp())
                {
                    return false;
                }
                AddBasicDerefInstr(ExOperationCode.NEWSLOT);
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

        public delegate bool CompilingFunctionRef();

        public bool ExInvokeExp(CompilingFunctionRef func)
        {
            // İfade takipçisini kopyasını al ve resetle
            ExEState eState = ExpressionState.Copy();
            ExpressionState.Type = ExEType.EXPRESSION;
            ExpressionState.Position = -1;
            ExpressionState.ShouldStopDeref = false;

            if (!func())    // Derleyici fonksiyonunu çağır
            {
                return false;
            }

            ExpressionState = eState;
            return true;
        }

        public bool ExBinaryExp(ExOperationCode op, CompilingFunctionRef func, int arg3 = 0) // 1 işleç, 2 değer ile işlemi
        {
            if (!ReadAndSetToken() || !ExInvokeExp(func))   // Verilen derleyici metotunu çağır
            {
                return false;
            }

            int arg1 = FunctionState.PopTarget();   // sağ taraf
            int arg2 = FunctionState.PopTarget();   // sol taraf

            FunctionState.AddInstr(op, FunctionState.PushTarget(), arg1, arg2, arg3);
            return true;
        }

        private bool ExExpAsgECheck(ExEType etyp)
        {
            switch (etyp)    // İfade tipini kontrol et
            {
                case ExEType.CONSTDELEG: return AddToErrorMessage("can't modify a constant's delegate");
                case ExEType.EXPRESSION: return AddToErrorMessage("can't assing an expression");
                case ExEType.BASE: return AddToErrorMessage("can't modify 'base'");
                default: return true;
            }
        }

        private bool ExExpAsgInit(TokenType op, ExEType etyp, int targetpos)
        {
            switch (op)
            {
                case TokenType.ASG:
                    {
                        ExExpAsg(etyp, targetpos);
                        break;
                    }
                case TokenType.ADDEQ:
                case TokenType.SUBEQ:
                case TokenType.MLTEQ:
                case TokenType.DIVEQ:
                case TokenType.MODEQ:
                    {
                        AddCompoundOpInstr(op, etyp, targetpos);
                        break;
                    }
                case TokenType.NEWSLOT:
                    {
                        if (etyp is ExEType.OBJECT or ExEType.BASE)
                        {
                            AddBasicDerefInstr(ExOperationCode.NEWSLOT);
                        }
                        else
                        {
                            AddToErrorMessage("can't create a local slot");
                            return false;
                        }
                        break;
                    }
            }
            return true;
        }

        private void ExExpAsg(ExEType etyp, int targetpos)
        {
            switch (etyp)
            {
                case ExEType.VAR:   // Değişkene değer ata
                    {
                        int source = FunctionState.PopTarget();
                        int destination = FunctionState.TopTarget();
                        FunctionState.AddInstr(ExOperationCode.MOVE, destination, source, 0, 0);
                        break;
                    }
                case ExEType.OBJECT:    // Objenin sahip olduğu bir değeri değiştir
                case ExEType.BASE:
                    {
                        AddBasicDerefInstr(ExOperationCode.SET);
                        break;
                    }
                case ExEType.OUTER:     // Dışardaki bir değişkene değer ata
                    {
                        int source = FunctionState.PopTarget();
                        int destination = FunctionState.PushTarget();
                        FunctionState.AddInstr(ExOperationCode.SETOUTER, destination, targetpos, source, 0);
                        break;
                    }
            }
        }

        private bool ExExpTernary()
        {
            if (!ReadAndSetToken())
            {
                return false;
            }

            FunctionState.AddInstr(ExOperationCode.JZ, FunctionState.PopTarget(), 0, 0, 0);

            int jzp = FunctionState.GetCurrPos();
            int t = FunctionState.PushTarget();

            if (!ExExp())
            {
                return false;
            }

            int f = FunctionState.PopTarget();
            if (t != f)
            {
                FunctionState.AddInstr(ExOperationCode.MOVE, t, f, 0, 0);
            }
            int end_f = FunctionState.GetCurrPos();

            FunctionState.AddInstr(ExOperationCode.JMP, 0, 0, 0, 0);
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
                FunctionState.AddInstr(ExOperationCode.MOVE, t, s, 0, 0);
            }

            FunctionState.UpdateInstructionArgument(jmp, 1, FunctionState.GetCurrPos() - jmp);
            FunctionState.UpdateInstructionArgument(jzp, 1, end_f - jzp + 1);
            FunctionState.NotSnoozed = false;
            return true;
        }

        public bool ExExp()
        {
            // İfade takipçisini kopyasını alıp sıfırla
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
                case TokenType.ASG:     // =
                case TokenType.ADDEQ:   // +=
                case TokenType.SUBEQ:   // -=
                case TokenType.DIVEQ:   // /=
                case TokenType.MLTEQ:   // *=
                case TokenType.MODEQ:   // %=
                case TokenType.NEWSLOT: // <>
                    {
                        TokenType op = CurrentToken;
                        ExEType etyp = ExpressionState.Type;
                        int targetpos = ExpressionState.Position;

                        if (!ExExpAsgECheck(etyp)   // LHS valid
                            || !ReadAndSetToken()   // Next
                            || !ExExp()             // RHS valid
                            || !ExExpAsgInit(op, etyp, targetpos))
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.QMARK:   // ?
                    {
                        if (!ExExpTernary())
                        {
                            return false;
                        }
                        break;
                    }
            }
            // Eski ifade takibine geri dön
            ExpressionState = estate;
            return true;
        }

        public bool ExLogicOr()
        {
            if (!ExLogicAnd())
            {
                return false;
            }
            while (true)
            {
                switch (CurrentToken)
                {
                    case TokenType.OR:
                        {
                            int f = FunctionState.PopTarget();
                            int t = FunctionState.PushTarget();

                            FunctionState.AddInstr(ExOperationCode.OR, t, 0, f, 0);

                            int j = FunctionState.GetCurrPos();

                            if (t != f)
                            {
                                FunctionState.AddInstr(ExOperationCode.MOVE, t, f, 0, 0);
                            }

                            if (!ReadAndSetToken() || !ExInvokeExp(ExLogicOr))
                            {
                                return false;
                            }

                            FunctionState.NotSnoozed = false;

                            int s = FunctionState.PopTarget();

                            if (t != s)
                            {
                                FunctionState.AddInstr(ExOperationCode.MOVE, t, s, 0, 0);
                            }

                            FunctionState.NotSnoozed = false;
                            FunctionState.UpdateInstructionArgument(j, 1, FunctionState.GetCurrPos() - j);
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
            while (true)
            {
                switch (CurrentToken)
                {
                    case TokenType.AND:
                        {
                            int f = FunctionState.PopTarget();
                            int t = FunctionState.PushTarget();

                            FunctionState.AddInstr(ExOperationCode.AND, t, 0, f, 0);

                            int j = FunctionState.GetCurrPos();

                            if (t != f)
                            {
                                FunctionState.AddInstr(ExOperationCode.MOVE, t, f, 0, 0);
                            }
                            if (!ReadAndSetToken() || !ExInvokeExp(ExLogicAnd))
                            {
                                return false;
                            }

                            FunctionState.NotSnoozed = false;

                            int s = FunctionState.PopTarget();

                            if (t != s)
                            {
                                FunctionState.AddInstr(ExOperationCode.MOVE, t, s, 0, 0);
                            }

                            FunctionState.NotSnoozed = false;
                            FunctionState.UpdateInstructionArgument(j, 1, FunctionState.GetCurrPos() - j);
                            break;
                        }
                    default:
                        return true;
                }
            }
        }
        public bool ExLogicBOr()
        {
            if (!ExLogicBXOr())
            {
                return false;
            }
            while (true)
            {
                switch (CurrentToken)
                {
                    case TokenType.BOR:
                        {
                            if (!ExBinaryExp(ExOperationCode.BITWISE, ExLogicBXOr, (int)BitOP.OR))
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
        public bool ExLogicBXOr()
        {
            if (!ExLogicBAnd())
            {
                return false;
            }
            while (true)
            {
                switch (CurrentToken)
                {
                    case TokenType.BXOR:
                        {
                            if (!ExBinaryExp(ExOperationCode.BITWISE, ExLogicBAnd, (int)BitOP.XOR))
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
            while (true)
            {
                switch (CurrentToken)
                {
                    case TokenType.BAND:
                        {
                            if (!ExBinaryExp(ExOperationCode.BITWISE, ExLogicEq, (int)BitOP.AND))
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
            while (true)
            {
                switch (CurrentToken)
                {
                    case TokenType.EQU:
                        {
                            if (!ExBinaryExp(ExOperationCode.EQ, ExLogicCmp))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.NEQ:
                        {
                            if (!ExBinaryExp(ExOperationCode.NEQ, ExLogicCmp))
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
            while (true)
            {
                bool valid = false;
                switch (CurrentToken)
                {
                    case TokenType.GRT:
                        {
                            valid = ExBinaryExp(ExOperationCode.CMP, ExLogicShift, (int)CmpOP.GRT);
                            break;
                        }
                    case TokenType.LST:
                        {
                            valid = ExBinaryExp(ExOperationCode.CMP, ExLogicShift, (int)CmpOP.LST);
                            break;
                        }
                    case TokenType.GET:
                        {
                            valid = ExBinaryExp(ExOperationCode.CMP, ExLogicShift, (int)CmpOP.GET);
                            break;
                        }
                    case TokenType.LET:
                        {
                            valid = ExBinaryExp(ExOperationCode.CMP, ExLogicShift, (int)CmpOP.LET);
                            break;
                        }
                    case TokenType.NOTIN:
                    case TokenType.IN:
                        {
                            valid = ExBinaryExp(ExOperationCode.EXISTS, ExLogicShift, CurrentToken == TokenType.NOTIN ? 1 : 0);
                            break;
                        }
                    case TokenType.INSTANCEOF:
                        {
                            valid = ExBinaryExp(ExOperationCode.INSTANCEOF, ExLogicShift);
                            break;
                        }
                    default:
                        return true;
                }
                if (!valid)
                {
                    return false;
                }
            }
        }
        public bool ExLogicShift()
        {
            if (!ExArithAdd())
            {
                return false;
            }
            while (true)
            {
                switch (CurrentToken)
                {
                    case TokenType.LSHF:
                        {
                            if (!ExBinaryExp(ExOperationCode.BITWISE, ExArithAdd, (int)BitOP.SHIFTL))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.RSHF:
                        {
                            if (!ExBinaryExp(ExOperationCode.BITWISE, ExArithAdd, (int)BitOP.SHIFTR))
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
        public bool ExArithAdd()
        {
            if (!ExArithMlt())
            {
                return false;
            }
            while (true)
            {
                switch (CurrentToken)
                {
                    case TokenType.ADD:
                    case TokenType.SUB:
                        {
                            if (!ExBinaryExp(CurrentToken == TokenType.ADD ? ExOperationCode.ADD : ExOperationCode.SUB, ExArithMlt))
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
        public bool ExArithMlt()
        {
            if (!ExPrefixed())
            {
                return false;
            }

            while (true)
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
                            if (!ExBinaryExp(ExOPDecideArithmetic(CurrentToken), ExPrefixed))
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

        public static ExOperationCode ExOPDecideArithmetic(TokenType typ)
        {
            switch (typ)
            {
                case TokenType.EXP:
                    return ExOperationCode.EXP;
                case TokenType.SUB:
                case TokenType.SUBEQ:
                    return ExOperationCode.SUB;
                case TokenType.MLT:
                case TokenType.MLTEQ:
                    return ExOperationCode.MLT;
                case TokenType.DIV:
                case TokenType.DIVEQ:
                    return ExOperationCode.DIV;
                case TokenType.MOD:
                case TokenType.MODEQ:
                    return ExOperationCode.MOD;
                case TokenType.MATMLT:
                    return ExOperationCode.MMLT;
                case TokenType.CARTESIAN:
                    return ExOperationCode.CARTESIAN;
                default:
                    return ExOperationCode.ADD;
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

        private bool ExPrefixedDot(ref int p)
        {
            if (!ReadAndSetToken())
            {
                return false;
            }

            ExObject tmp;
            if ((tmp = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            FunctionState.AddInstr(ExOperationCode.LOAD, FunctionState.PushTarget(), FunctionState.GetLiteral(tmp), 0, 0);

            if (ExpressionState.Type == ExEType.BASE)
            {
                AddBasicOpInstr(ExOperationCode.GET);
                p = FunctionState.TopTarget();
                ExpressionState.Type = ExEType.EXPRESSION;
                ExpressionState.Position = p;
            }
            else
            {
                if (ExRequiresGetter())
                {
                    AddBasicOpInstr(ExOperationCode.GET);
                }
                ExpressionState.Type = ExEType.OBJECT;
            }
            return true;
        }

        private bool ExPrefixedSquareOpen(ref int p)
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
                AddBasicOpInstr(ExOperationCode.GET);
                p = FunctionState.TopTarget();
                ExpressionState.Type = ExEType.EXPRESSION;
                ExpressionState.Position = p;
            }
            else
            {
                if (ExRequiresGetter())
                {
                    AddBasicOpInstr(ExOperationCode.GET);
                }
                ExpressionState.Type = ExEType.OBJECT;
            }
            return true;
        }

        private bool ExPrefixMatrixTranspose()
        {
            if (!ReadAndSetToken())
            {
                return false;
            }

            switch (ExpressionState.Type)
            {
                case ExEType.EXPRESSION:
                case ExEType.OBJECT:
                case ExEType.BASE:
                case ExEType.VAR:
                    {
                        int s = FunctionState.PopTarget();
                        FunctionState.AddInstr(ExOperationCode.TRANSPOSE, FunctionState.PushTarget(), s, 0, 0);
                        break;
                    }
                case ExEType.OUTER:
                    {
                        int t1 = FunctionState.PushTarget();
                        int t2 = FunctionState.PushTarget();
                        FunctionState.AddInstr(ExOperationCode.GETOUTER, t2, ExpressionState.Position, 0, 0);
                        FunctionState.AddInstr(ExOperationCode.TRANSPOSE, t1, t2, 0, 0);
                        FunctionState.PopTarget();
                        break;
                    }
                default:
                    {
                        return AddToErrorMessage("can't transpose a delegate!");
                    }
            }
            return true;
        }

        private bool ExPrefixedIncDec()
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
                case ExEType.CONSTDELEG:
                    {
                        return AddToErrorMessage("can't increment or decrement a delegate");
                    }
                case ExEType.EXPRESSION:
                    {
                        return AddToErrorMessage("can't increment or decrement an expression");
                    }
                case ExEType.OBJECT:
                case ExEType.BASE:
                    {
                        AddBasicOpInstr(ExOperationCode.PINC, v);
                        break;
                    }
                case ExEType.VAR:
                    {
                        int s = FunctionState.PopTarget();
                        FunctionState.AddInstr(ExOperationCode.PINCL, FunctionState.PushTarget(), s, 0, v);
                        break;
                    }
                case ExEType.OUTER:
                    {
                        int t1 = FunctionState.PushTarget();
                        int t2 = FunctionState.PushTarget();
                        FunctionState.AddInstr(ExOperationCode.GETOUTER, t2, ExpressionState.Position, 0, 0);
                        FunctionState.AddInstr(ExOperationCode.PINCL, t1, t2, 0, v);
                        FunctionState.AddInstr(ExOperationCode.SETOUTER, t2, ExpressionState.Position, t2, 0);
                        FunctionState.PopTarget();
                        break;
                    }
            }
            return true;
        }

        private bool ExPrefixedRoundOpen()
        {
            switch (ExpressionState.Type)
            {
                case ExEType.OBJECT:
                    {
                        int k_loc = FunctionState.PopTarget();
                        int obj_loc = FunctionState.PopTarget();
                        int closure = FunctionState.PushTarget();
                        int target = FunctionState.PushTarget();
                        FunctionState.AddInstr(ExOperationCode.PREPCALL, closure, k_loc, obj_loc, target);
                        break;
                    }
                case ExEType.BASE:
                    {
                        FunctionState.AddInstr(ExOperationCode.MOVE, FunctionState.PushTarget(), 0, 0, 0);
                        break;
                    }
                case ExEType.OUTER:
                    {
                        FunctionState.AddInstr(ExOperationCode.GETOUTER, FunctionState.PushTarget(), ExpressionState.Position, 0, 0);
                        FunctionState.AddInstr(ExOperationCode.MOVE, FunctionState.PushTarget(), 0, 0, 0);
                        break;
                    }
                default:
                    {
                        FunctionState.AddInstr(ExOperationCode.MOVE, FunctionState.PushTarget(), 0, 0, 0);
                        break;
                    }
            }
            ExpressionState.Type = ExEType.EXPRESSION;

            return ReadAndSetToken() && ExFuncCall();
        }

        public bool ExPrefixed()
        {
            int p = -1;
            if (!ExFactor(ref p))
            {
                return false;
            }
            while (true)
            {
                bool valid;

                switch (CurrentToken)
                {
                    case TokenType.DOT:
                        {
                            valid = ExPrefixedDot(ref p);
                            break;
                        }
                    case TokenType.SQUAREOPEN:
                        {
                            valid = ExPrefixedSquareOpen(ref p);
                            break;
                        }
                    case TokenType.MATTRANSPOSE:
                        {
                            return ExPrefixMatrixTranspose();
                        }
                    case TokenType.INC:
                    case TokenType.DEC:
                        {
                            return ExPrefixedIncDec();
                        }
                    case TokenType.ROUNDOPEN:
                        {
                            valid = ExPrefixedRoundOpen();
                            break;
                        }
                    default:
                        {
                            return true;
                        }
                }

                if (!valid)
                {
                    return false;
                }
            }
        }

        private bool AccessConstDict(ExObject dict, ref ExObject cid, ref ExObject cval)
        {
            if (CurrentToken != TokenType.DOT)
            {
                return AddToErrorMessage("expected '.' for constant dict");
            }

            if (!ReadAndSetToken())
            {
                return false;
            }

            cid = Expect(TokenType.IDENTIFIER);

            if (cid == null || cid.Type != ExObjType.STRING)
            {
                return AddToErrorMessage("Invalid constant name, expected string type name.");
            }
            else if (!dict.GetDict().ContainsKey(cid.GetString()))
            {
                if (!VM.InvokeDefaultDeleg(dict, cid, ref cval))
                {
                    return AddToErrorMessage($"Unknown constant name '{cid.GetString()}'");
                }
            }
            else
            {
                cval = new(dict.GetDict()[cid.GetString()]);
            }

            return true;
        }

        private void AddConstLoadInstr(ExObject cid, ExObject cval, ExObject idx)
        {
            switch (cval.Type)
            {
                case ExObjType.INTEGER:
                    {
                        AddIntConstLoadInstr(cval.GetInt(), ExpressionState.Position);
                        break;
                    }
                case ExObjType.FLOAT:
                    {
                        AddFloatConstLoadInstr(new DoubleLong() { f = cval.GetFloat() }.i, ExpressionState.Position);
                        break;
                    }
                case ExObjType.SPACE:
                    {
                        AddSpaceConstLoadInstr(cval.GetSpace(), ExpressionState.Position);
                        break;
                    }
                case ExObjType.STRING:
                    {
                        FunctionState.AddInstr(ExOperationCode.LOAD, ExpressionState.Position, FunctionState.GetLiteral(cval), 0, 0);
                        break;
                    }
                default:
                    {
                        int cno = cid == null ? 0 : (int)FunctionState.GetLiteral(cid);
                        FunctionState.AddInstr(ExOperationCode.LOADCONSTDICTOBJDELEG, ExpressionState.Position, FunctionState.GetLiteral(idx), cno, 0);
                        break;
                    }
            }
        }

        private bool CompileConstFactor(ExObject idx, ExObject cnst)
        {
            ExObject cid = null, cval = null;

            if (cnst.Type == ExObjType.DICT)
            {
                if (!AccessConstDict(cnst, ref cid, ref cval))
                {
                    return false;
                }
            }
            else
            {
                cval = new(cnst);
            }

            ExpressionState.Position = FunctionState.PushTarget();

            AddConstLoadInstr(cid, cval, idx);

            ExpressionState.Type = ExEType.EXPRESSION;

            return true;
        }

        private bool FactorReload()
        {
            if (!ReadAndSetToken())
            {
                return false;
            }

            int l, s = FunctionState.PushTarget();

            if (CurrentToken is TokenType.LITERAL or TokenType.IDENTIFIER)
            {
                l = (int)FunctionState.GetLiteral(FunctionState.CreateString(Lexer.ValString));
                if (!ReadAndSetToken())
                {
                    return false;
                }
            }
            else
            {
                l = (int)FunctionState.GetLiteral(FunctionState.CreateString(ExMat.StandardBaseLibraryName));
            }
            if (!IsEOS())
            {
                return AddToErrorMessage("expected end of statement.");
            }

            FunctionState.AddInstr(ExOperationCode.RELOADLIB, s, l, 0, 0);
            ExpressionState.Type = ExEType.OBJECT;
            return true;
        }

        private bool FactorBase(ref int pos)
        {
            if (!ReadAndSetToken())
            {
                return false;
            }
            FunctionState.AddInstr(ExOperationCode.GETBASE, FunctionState.PushTarget(), 0, 0, 0);

            ExpressionState.Type = ExEType.BASE;
            ExpressionState.Position = FunctionState.TopTarget();
            pos = ExpressionState.Position;
            return true;
        }
        private bool FactorIdentifiers(ref int pos)
        {
            ExObject idx;
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
                default:
                    {
                        idx = new();
                        break;
                    }
            }

            int p;
            if (!ReadAndSetToken())
            {
                return false;
            }

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
                    FunctionState.AddInstr(ExOperationCode.GETOUTER, ExpressionState.Position, p, 0, 0);
                }
                else
                {
                    ExpressionState.Type = ExEType.OUTER;
                    ExpressionState.Position = p;
                }
            }
            else if (FunctionState.IsConst(idx, out ExObject cnst))
            {
                if (!CompileConstFactor(idx, cnst))
                {
                    return false;
                }
            }
            else // Yerli olmayan
            {
                FunctionState.PushTarget(0); // Hack, push 'this'
                FunctionState.AddInstr(ExOperationCode.LOAD, FunctionState.PushTarget(), FunctionState.GetLiteral(idx), 0, 0);

                if (ExRequiresGetter())
                {
                    AddBasicOpInstr(ExOperationCode.GET);
                }
                ExpressionState.Type = ExEType.OBJECT;
            }
            pos = ExpressionState.Position;
            return true;
        }

        private bool FactorComplexLoad()
        {
            if (Lexer.TokenComplex == TokenType.INTEGER)    // Tamsayı katsayılı ?
            {
                AddComplexConstLoadInstr(Lexer.ValInteger, -1);
            }
            else // Ondalıklı sayı katsayılı
            {
                AddComplexConstLoadInstr(new DoubleLong() { f = Lexer.ValFloat }.i, -1, true);
            }

            return ReadAndSetToken();
        }

        private bool FactorSquareOpen()
        {
            FunctionState.AddInstr(ExOperationCode.NEWOBJECT, FunctionState.PushTarget(), 0, 0, (int)ExNewObjectType.ARRAY);
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
                if (CurrentToken == TokenType.SEP
                    && !ReadAndSetToken())
                {
                    return false;
                }

                int v = FunctionState.PopTarget();
                int a = FunctionState.TopTarget();
                FunctionState.AddInstr(ExOperationCode.APPENDTOARRAY, a, v, (int)ArrayAType.STACK, 0);
                k++;
            }
            FunctionState.UpdateInstructionArgument(p, 1, k);
            return ReadAndSetToken();
        }

        private bool FactorSub()
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
                        return ReadAndSetToken();
                    }
                case TokenType.FLOAT:
                    {
                        AddFloatConstLoadInstr(new DoubleLong() { f = -Lexer.ValFloat }.i, -1);
                        return ReadAndSetToken();
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

                        return ReadAndSetToken();
                    }
                default:
                    {
                        return ExOpUnary(ExOperationCode.NEGATE);
                    }
            }
        }

        private bool FactorBitNot()
        {
            if (!ReadAndSetToken())
            {
                return false;
            }

            if (CurrentToken == TokenType.INTEGER)
            {
                AddIntConstLoadInstr(~Lexer.ValInteger, -1);
                return ReadAndSetToken();
            }

            return ExOpUnary(ExOperationCode.BNOT);
        }

        public bool ExFactor(ref int pos)
        {
            ExpressionState.Type = ExEType.EXPRESSION;

            bool valid;
            switch (CurrentToken)
            {
                #region Diğer Semboller
                case TokenType.LAMBDA:
                case TokenType.FUNCTION:
                    {
                        valid = ExFuncResolveExp(CurrentToken);
                        break;
                    }
                case TokenType.LITERAL:
                    {
                        FunctionState.AddInstr(ExOperationCode.LOAD, FunctionState.PushTarget(), FunctionState.GetLiteral(FunctionState.CreateString(Lexer.ValString)), 0, 0);

                        valid = ReadAndSetToken();
                        break;
                    }
                case TokenType.RELOAD:
                    {
                        valid = FactorReload();
                        break;
                    }
                case TokenType.BASE:
                    {
                        return FactorBase(ref pos);
                    }
                case TokenType.IDENTIFIER:
                case TokenType.CONSTRUCTOR:
                case TokenType.THIS:
                    {
                        return FactorIdentifiers(ref pos);
                    }
                case TokenType.GLOBAL:
                    {
                        FunctionState.AddInstr(ExOperationCode.LOADROOT, FunctionState.PushTarget(), 0, 0, 0);
                        ExpressionState.Type = ExEType.OBJECT;
                        CurrentToken = TokenType.DOT;
                        ExpressionState.Position = -1;
                        pos = ExpressionState.Position;
                        return true;
                    }
                case TokenType.DEFAULT:
                case TokenType.NULL:
                    {
                        FunctionState.AddInstr(ExOperationCode.LOADNULL, FunctionState.PushTarget(), 1, CurrentToken == TokenType.DEFAULT ? 1 : 0, 0);
                        valid = ReadAndSetToken();
                        break;
                    }
                #endregion
                case TokenType.INTEGER:
                    {
                        AddIntConstLoadInstr(Lexer.ValInteger, -1); // Sembol ayırıcıdaki tamsayı değerini al
                        valid = ReadAndSetToken();
                        break;
                    }
                case TokenType.FLOAT:
                    {
                        AddFloatConstLoadInstr(new DoubleLong() { f = Lexer.ValFloat }.i, -1);
                        valid = ReadAndSetToken();
                        break;
                    }
                case TokenType.COMPLEX:
                    {
                        valid = FactorComplexLoad();
                        break;
                    }
                case TokenType.SPACE:
                    {
                        AddSpaceConstLoadInstr(Lexer.ValSpace, -1);
                        valid = ReadAndSetToken();
                        break;
                    }
                case TokenType.TRUE:
                case TokenType.FALSE:
                    {
                        FunctionState.AddInstr(ExOperationCode.LOADBOOLEAN, FunctionState.PushTarget(), CurrentToken == TokenType.TRUE ? 1 : 0, 0, 0);
                        valid = ReadAndSetToken();
                        break;
                    }
                case TokenType.SQUAREOPEN:
                    {
                        valid = FactorSquareOpen();
                        break;
                    }
                case TokenType.CURLYOPEN:
                    {
                        FunctionState.AddInstr(ExOperationCode.NEWOBJECT, FunctionState.PushTarget(), 0, (int)ExNewObjectType.DICT, 0);
                        valid = ReadAndSetToken() && ParseDictClusterOrClass(TokenType.SEP, TokenType.CURLYCLOSE);
                        break;
                    }
                case TokenType.SUB:
                    {
                        valid = FactorSub();
                        break;
                    }
                case TokenType.EXC:
                    {
                        valid = ReadAndSetToken() && ExOpUnary(ExOperationCode.NOT);
                        break;
                    }
                case TokenType.BNOT:
                    {
                        valid = FactorBitNot();
                        break;
                    }
                case TokenType.TYPEOF:
                    {
                        valid = ReadAndSetToken() && ExOpUnary(ExOperationCode.TYPEOF);
                        break;
                    }
                case TokenType.INC:
                case TokenType.DEC:
                    {
                        valid = ExPrefixedIncDec(CurrentToken);
                        break;
                    }
                case TokenType.ROUNDOPEN:
                    {
                        valid = ReadAndSetToken()
                                && ExSepExp()
                                && Expect(TokenType.ROUNDCLOSE) != null;
                        break;
                    }
                case TokenType.DELETE:
                    {
                        valid = ExDeleteExp();
                        break;
                    }
                default:
                    {
                        return AddToErrorMessage("expression expected");
                    }
            }

            if (!valid)
            {
                return false;
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

            if (ExpressionState.Type is ExEType.OBJECT or ExEType.BASE)
            {
                AddBasicOpInstr(ExOperationCode.DELETE);
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
                case ExEType.CONSTDELEG:
                    {
                        return AddToErrorMessage("can't increment or decrement a delegate!");
                    }
                case ExEType.EXPRESSION:
                    {
                        return AddToErrorMessage("can't increment or decrement an expression!");
                    }
                case ExEType.OBJECT:
                case ExEType.BASE:
                    {
                        AddBasicOpInstr(ExOperationCode.INC, v);
                        break;
                    }
                case ExEType.VAR:
                    {
                        int s = FunctionState.TopTarget();
                        FunctionState.AddInstr(ExOperationCode.INCL, s, s, 0, v);
                        break;
                    }
                case ExEType.OUTER:
                    {
                        int tmp = FunctionState.PushTarget();
                        FunctionState.AddInstr(ExOperationCode.GETOUTER, tmp, ExpressionState.Position, 0, 0);
                        FunctionState.AddInstr(ExOperationCode.INCL, tmp, tmp, 0, ExpressionState.Position);
                        FunctionState.AddInstr(ExOperationCode.SETOUTER, tmp, ExpressionState.Position, 0, tmp);
                        break;
                    }
            }
            ExpressionState = eState;
            return true;
        }

        public bool ExSepExp()  // "," ile ayrılmış ifadeleri derle
        {
            if (!ExExp())       // İfadeyi derle
            {
                return false;
            }

            while (true)
            {
                if (CurrentToken != TokenType.SEP)  // "," değilse bitir
                {
                    return true;
                }

                FunctionState.PopTarget();  // Son derlemeden kalan değeri çıkart

                if (!ReadAndSetToken() || !ExSepExp())  // Sıradaki ifadeyi derle
                {
                    return false;
                }
            }
        }

        public void AddIntConstLoadInstr(long cval, int p)                  // Tamsayı yükle
        {
            FunctionState.AddInstr(ExOperationCode.LOADINTEGER,                         // Tamsayı yükle komutu
                                   p < 0 ? FunctionState.PushTarget() : p,  // Hedef
                                   cval,                                    // Kaynak 64bit değer
                                   0,
                                   0);
        }
        public void AddFloatConstLoadInstr(long cval, int p)                // Ondalıklı sayı yükle
        {
            FunctionState.AddInstr(ExOperationCode.LOADFLOAT,               // Ondalıklı sayı yükle komutu
                                   p < 0 ? FunctionState.PushTarget() : p,  // Hedef
                                   cval,                                    // Kaynak 64bit değer
                                   0,
                                   0);
        }
        public void AddComplexConstLoadInstr(long cval, int p, bool useFloat = false)    // Kompleks sayı yükle
        {
            FunctionState.AddInstr(ExOperationCode.LOADCOMPLEX,                         // Kompleks sayı yükle komutu
                                   p < 0 ? FunctionState.PushTarget() : p,  // Hedef
                                   cval,                                    // Kaynak 64bit değer
                                   useFloat ? 1 : 0,                        // Kaynak ondalıklı mı kullanılmalı ?
                                   0);
        }
        public void AddSpaceConstLoadInstr(ExSpace s, int p)                // Uzay değeri yükle
        {
            FunctionState.AddInstr(ExOperationCode.LOADSPACE,               // Uzay yükle komutu
                                   p < 0 ? FunctionState.PushTarget() : p,  // Hedef
                                   FunctionState.GetLiteral(new(s)),   // Uzay temsilinin indeksi
                                   0,
                                   0);
        }

        public void AddBasicDerefInstr(ExOperationCode op)
        {
            int value = FunctionState.PopTarget();
            int key = FunctionState.PopTarget();
            int source = FunctionState.PopTarget();
            FunctionState.AddInstr(op, FunctionState.PushTarget(), source, key, value);
        }

        public void AddBasicOpInstr(ExOperationCode op, int last = 0)
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
                        FunctionState.AddInstr(ExOperationCode.COMPOUNDARITH, FunctionState.PushTarget(), (s << 16) | v, k, ExOPDecideArithmeticInt(typ));
                        break;
                    }
                case ExEType.OUTER:
                    {
                        int v = FunctionState.TopTarget();
                        int tmp = FunctionState.PushTarget();

                        FunctionState.AddInstr(ExOperationCode.GETOUTER, tmp, pos, 0, 0);
                        FunctionState.AddInstr(ExOPDecideArithmetic(typ), tmp, v, tmp, 0);
                        FunctionState.AddInstr(ExOperationCode.SETOUTER, tmp, pos, tmp, 0);

                        break;
                    }
            }
        }

        private bool ParseAttributePresenceCheck(ref bool a_present)
        {
            if (CurrentToken == TokenType.ATTRIBUTEBEGIN)
            {
                FunctionState.AddInstr(ExOperationCode.NEWOBJECT, FunctionState.PushTarget(), 0, (int)ExNewObjectType.DICT, 0);

                if (!ReadAndSetToken() || !ParseDictClusterOrClass(TokenType.SEP, TokenType.ATTRIBUTEFINISH))
                {
                    return AddToErrorMessage("failed to parse attribute");
                }
                a_present = true;
            }
            return true;
        }
        private bool ParseMethod()
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

            FunctionState.AddInstr(ExOperationCode.LOAD, FunctionState.PushTarget(), FunctionState.GetLiteral(o), 0, 0);

            if (!ExFuncCreate(o, typ == TokenType.FUNCTION ? ExFuncType.DEFAULT : ExFuncType.CONSTRUCTOR))
            {
                return false;
            }

            FunctionState.AddInstr(ExOperationCode.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);
            return true;
        }

        private bool ParseKeyOrMember(TokenType sep)
        {
            return sep == TokenType.SEP ? ParseLiteralDictKey() : ParseIdentifierKeyOrMember();
        }

        private bool ParseLiteralDictKey()
        {
            ExObject o;
            if ((o = Expect(TokenType.LITERAL)) == null)
            {
                return false;
            }
            FunctionState.AddInstr(ExOperationCode.LOAD, FunctionState.PushTarget(), FunctionState.GetLiteral(o), 0, 0);

            return Expect(TokenType.COL) != null && ExExp();
        }
        private bool ParseIdentifierKeyOrMember()
        {
            ExObject o;
            if ((o = Expect(TokenType.IDENTIFIER)) == null)
            {
                return false;
            }

            FunctionState.AddInstr(ExOperationCode.LOAD, FunctionState.PushTarget(), FunctionState.GetLiteral(o), 0, 0);

            return Expect(TokenType.ASG) != null && ExExp();
        }

        private bool NewSlotFromParsing(bool a_present, TokenType sep)
        {
            int v = FunctionState.PopTarget();
            int k = FunctionState.PopTarget();
            int a = a_present ? FunctionState.PopTarget() : -1;

            if (!((a_present && (a == k - 1)) || !a_present))
            {
                return AddToErrorMessage("attributes present count error");
            }

            int flg = a_present ? (int)ExNewSlotFlag.ATTR : 0; // to-do static flag
            int t = FunctionState.TopTarget();
            if (sep == TokenType.SEP)
            {
                FunctionState.AddInstr(ExOperationCode.NEWSLOT, ExMat.InvalidArgument, t, k, v);
            }
            else
            {
                FunctionState.AddInstr(ExOperationCode.NEWSLOTA, flg, t, k, v);
            }
            return true;
        }

        public bool ParseDictClusterOrClass(TokenType sep, TokenType end)
        {
            int p = FunctionState.GetCurrPos();
            int n = 0;

            while (CurrentToken != end)
            {
                bool valid, a_present = false;
                if (sep == TokenType.SMC
                    && !ParseAttributePresenceCheck(ref a_present))
                {
                    return false;
                }

                switch (CurrentToken)
                {
                    case TokenType.FUNCTION:
                    case TokenType.CONSTRUCTOR:
                        {
                            valid = ParseMethod();
                            break;
                        }
                    case TokenType.SQUAREOPEN:
                        {
                            valid = ReadAndSetToken()
                                    && ExSepExp()
                                    && Expect(TokenType.SQUARECLOSE) != null
                                    && Expect(TokenType.ASG) != null
                                    && ExExp();
                            break;
                        }
                    case TokenType.LITERAL:
                        {
                            valid = ParseKeyOrMember(sep);
                            break;
                        }
                    default:
                        {
                            valid = ParseIdentifierKeyOrMember();
                            break;
                        }
                }

                n++;

                if (!valid
                    || (CurrentToken == sep && !ReadAndSetToken())
                    || !NewSlotFromParsing(a_present, sep))
                {
                    return false;
                }
            }

            if (sep == TokenType.SEP)
            {
                FunctionState.UpdateInstructionArgument(p, 1, n);
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

            FunctionState.AddInstr(ExOperationCode.CALL, FunctionState.PushTarget(), cl, st, _this);
            return true;
        }

        public void TargetLocalMove()
        {
            int t = FunctionState.TopTarget();
            if (FunctionState.IsLocalArg(t))
            {
                t = FunctionState.PopTarget();
                FunctionState.AddInstr(ExOperationCode.MOVE, FunctionState.PushTarget(), t, 0, 0);
            }
        }

        public ExFuncType GetFuncTypeFromToken()
        {
            switch (CurrentToken)
            {
                case TokenType.LAMBDA:
                    {
                        return ExFuncType.LAMBDA;
                    }
                default:
                    {
                        return ExFuncType.DEFAULT;
                    }
            }
        }

        public bool ExFuncResolveExp(TokenType typ)
        {
            ExFuncType ftyp = GetFuncTypeFromToken();
            if (!ReadAndSetToken() || Expect(TokenType.ROUNDOPEN) == null)
            {
                return false;
            }

            ExObject d = new();
            if (!ExFuncCreate(d, ftyp))
            {
                return false;
            }

            FunctionState.AddInstr(ExOperationCode.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, typ == TokenType.FUNCTION ? 0 : 1, 0);

            return true;
        }
        public bool ExClassResolveExp()
        {
            int at = ExMat.InvalidArgument;

            if (CurrentToken == TokenType.ATTRIBUTEBEGIN)
            {
                if (!ReadAndSetToken())
                {
                    return false;
                }
                FunctionState.AddInstr(ExOperationCode.NEWOBJECT, FunctionState.PushTarget(), 0, (int)ExNewObjectType.DICT, 0);

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

            if (at != ExMat.InvalidArgument)
            {
                FunctionState.PopTarget();
            }

            FunctionState.AddInstr(ExOperationCode.NEWOBJECT, FunctionState.PushTarget(), -1, at, (int)ExNewObjectType.CLASS);

            return ParseDictClusterOrClass(TokenType.SMC, TokenType.CURLYCLOSE);
        }

        private bool FuncDoVargsCheck(int defParams, ExFState f_state, ref bool done)
        {
            if (CurrentToken == TokenType.VARGS)     // Belirsiz sayıda parametre sembolü "..."
            {
                if (defParams > 0)             // Varsayılan değerler ile "..." kullanılamaz
                {
                    return AddToErrorMessage("can't use vargs alongside default valued parameters");
                }

                // "vargs" argüman listesini ekle
                f_state.AddParam(FunctionState.CreateString(ExMat.VargsName));
                f_state.HasVargs = true;

                if (!ReadAndSetToken())
                {
                    return false;
                }

                if (CurrentToken != TokenType.ROUNDCLOSE)   // "..." sonrası ')' bekle
                {
                    return AddToErrorMessage("expected ')' after vargs '...'");
                }

                return done = true;  // Parametre bölümünü bitir
            }
            return true;
        }

        private bool FuncDoParamAddCheck(ref int defParams, ExFState f_state)
        {
            if (CurrentToken == TokenType.ASG)  // '=' bekle, bulunursa varsayılan değer vardır 
            {
                if (!ReadAndSetToken() || !ExExp()) // Varsayılan değeri derle
                {
                    return false;
                }

                f_state.AddDefParam(FunctionState.TopTarget()); // Varsayılan değeri ekle
                defParams++;
            }
            else
            {
                if (defParams > 0)    // Varsayılan değerli parametreden sonraki parametrelerin kontrolü
                {
                    return AddToErrorMessage("expected '=' for a default value");
                }
            }
            return true;
        }

        private bool FuncCreateInit(ref int defParams, ExFState f_state, out bool done)
        {
            done = false;
            ExObject pname;

            if (!FuncDoVargsCheck(defParams, f_state, ref done))
            {
                return false;
            }

            if (done)   // (...)
            {
                return true;
            }

            if ((pname = Expect(TokenType.IDENTIFIER)) == null) // Parametre ismi bekle
            {
                return false;
            }

            f_state.AddParam(pname);    // Uygun parametre ismini ekle

            if (!FuncDoParamAddCheck(ref defParams, f_state))
            {
                return false;
            }

            if (CurrentToken == TokenType.SEP)  // ',' bekle, var ise başka parametre vardır
            {
                return ReadAndSetToken();
            }
            else if (CurrentToken != TokenType.ROUNDCLOSE)  // ',' yoksa ')' beklenmek zorundadır
            {
                return AddToErrorMessage("expected ')' or ',' for function declaration");
            }

            return true;
        }

        private bool FuncCreateProcessBody(ExFuncType typ, ExFState f_state)
        {
            switch (typ)
            {
                case ExFuncType.DEFAULT:
                    return ProcessStatement(false);
                case ExFuncType.LAMBDA:
                    {
                        if (!ExExp())
                        {
                            return false;
                        }
                        f_state.AddInstr(ExOperationCode.RETURN, 1, FunctionState.PopTarget(), 0, 0);
                        return true;
                    }
                case ExFuncType.CONSTRUCTOR:
                    {
                        bool status = ProcessStatement(false);
                        if (!status)
                        {
                            return false;
                        }
                        f_state.AddInstr(ExOperationCode.RETURN, 1, 0, 0, 0);
                        return true;
                    }
                default:
                    return false;
            }
        }

        // Fonksiyon takipçisine ait bir alt fonksiyon oluştur
        public bool ExFuncCreate(ExObject o, ExFuncType typ = ExFuncType.DEFAULT)
        {
            // Bir alt fonksiyon takipçisi oluştur ve istenen fonksiyon ismini ver
            ExFState f_state = FunctionState.PushChildState(VM.SharedState);
            f_state.Name = o;

            // Fonksiyonun kendi değişkenlerine referans edecek 'this' referansını ekle
            f_state.AddParam(FunctionState.CreateString(ExMat.ThisName));
            f_state.Source = new(Source);

            int defParams = 0;    // Varsayılan parametre değeri sayısı

            while (CurrentToken != TokenType.ROUNDCLOSE) // Parametreleri oku
            {
                if (!FuncCreateInit(ref defParams, f_state, out bool done))
                {
                    return false;
                }

                if (done)
                {
                    break;
                }
            }

            if (Expect(TokenType.ROUNDCLOSE) == null)   // ')' bulunduğunu kontrol et
            {
                return false;
            }

            for (int i = 0; i < defParams; i++)   // Varsayılan parametre indekslerini çıkart
            {
                FunctionState.PopTarget();
            }

            ExFState tmp = FunctionState.Copy();    // Fonksiyon takipçisinin kopyasını al
            FunctionState = f_state;

            if (!FuncCreateProcessBody(typ, f_state))
            {
                return false;
            }

            f_state.AddLineInfo(Lexer.TokenPrev == TokenType.NEWLINE ? Lexer.PrevTokenLine : Lexer.CurrentLine, StoreLineInfos, true);

            f_state.AddInstr(ExOperationCode.RETURN, ExMat.InvalidArgument, 0, 0, 0); // Fonksiyondan çıkıldığından emin olmak için OPC.RETURN komutu ekle
            f_state.SetLocalStackSize(0);                   // Yığını temizle
            ExPrototype fpro = f_state.CreatePrototype();  // Prototipi oluştur

            FunctionState = tmp;                // Bir önceki takipçiye dön
            FunctionState.Functions.Add(fpro);  // Prototipi ekle
            FunctionState.PopChildState();      // Alt fonksiyonu çıkart
            return true;
        }

        public bool ExOpUnary(ExOperationCode op)
        {
            if (!ExPrefixed())
            {
                return false;
            }
            int s = FunctionState.PopTarget();
            FunctionState.AddInstr(op, FunctionState.PushTarget(), s, 0, 0);

            return true;
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
