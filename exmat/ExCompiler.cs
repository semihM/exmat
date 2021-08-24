using System;
using System.Collections.Generic;
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
            FunctionState.AddInstr(OPC.RETURN, ExMat.InvalidArgument, 2, VM.IsInteractive ? 1 : 0, 0);

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
                    && Lexer.TokenPrev != TokenType.SMC
                    && !CheckSMC())                // && (!macro || (CurrentToken != TokenType.MACROEND)))
                {
                    // İfade sonu kontrolü
                    return false;
                }
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
                case TokenType.VAR:     // var
                    {
                        if (!ProcessVarAsgStatement())
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
                case TokenType.IF:      // if
                    {
                        if (!ProcessIfStatement())
                        {
                            return false;
                        }
                        break;
                    }

                case TokenType.FOR:     // for
                    {
                        if (!ProcessForStatement())
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
                            FunctionState.AddInstr(op, ExMat.InvalidArgument, 0, FunctionState.GetLocalVariablesCount(), 0);
                        }
                        break;
                    }
                case TokenType.BREAK:
                    {
                        if (FunctionState.BreakTargetsList.Count <= 0)      // iterasyon bloğu kontrolü
                        {
                            AddToErrorMessage("'break' has to be in a breakable block");
                            return false;
                        }

                        DoOuterControl();   // Dış değişken referansları varsa referansları azalt
                        FunctionState.AddInstr(OPC.JMP, 0, -ExMat.InvalidArgument, 0, 0);
                        // Komut indeksini listeye ekle
                        FunctionState.BreakList.Add(FunctionState.GetCurrPos());

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.CONTINUE:
                    {
                        if (FunctionState.ContinueTargetList.Count <= 0)    // iterasyon bloğu kontrolü
                        {
                            AddToErrorMessage("'continue' has to be in a breakable block");
                            return false;
                        }
                        DoOuterControl();   // Dış değişken referansları varsa referansları azalt
                        FunctionState.AddInstr(OPC.JMP, 0, -ExMat.InvalidArgument, 0, 0);
                        // Komut indeksini listeye ekle
                        FunctionState.ContinueList.Add(FunctionState.GetCurrPos());

                        if (!ReadAndSetToken())
                        {
                            return false;
                        }
                        break;
                    }
                #endregion 
                default:
                    {
                        if (!ExSepExp())
                        {
                            return false;
                        }
                        break;
                    }
            }
            FunctionState.NotSnoozed = false;
            return true;
        }

        public void DoOuterControl()
        {
            if (FunctionState.GetLocalVariablesCount() != CurrentScope.nLocal
                && FunctionState.GetOuterSize(CurrentScope.nLocal) > 0)
            {
                FunctionState.AddInstr(OPC.CLOSE, 0, CurrentScope.nLocal, 0, 0);
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
            FunctionState.AddInstr(OPC.JZ, FunctionState.PopTarget(), 0, 0, 0);
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
                FunctionState.AddInstr(OPC.JMP, 0, 0, 0, 0);
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

        public bool ProcessForStatement()
        {
            if (!ReadAndSetToken())
            {
                return false;
            }

            ExScope scp = CreateScope();                // Yeni çerçeve oluştur
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

            // ilk ';' bulunduğunu kontrol et
            if (Expect(TokenType.SMC) == null)
            {
                return false;
            }

            // ilk kısımdaki atamaları bağımsız yap
            FunctionState.NotSnoozed = false;
            // koşullar doğru olduğunda atlanılacak indeksi sakla
            int jpos = FunctionState.GetCurrPos();
            int jzpos = -1;

            if (CurrentToken != TokenType.SMC)  // ';' yoksa koşul ifadesi verilmiştir
            {
                if (!ExSepExp())    // Koşulları derle
                {
                    return false;
                }
                // Koşul yanlış ise komutları atla
                FunctionState.AddInstr(OPC.JZ, FunctionState.PopTarget(), 0, 0, 0);
                // Atlanacak komut sayısını sakla
                jzpos = FunctionState.GetCurrPos();
            }

            if (Expect(TokenType.SMC) == null)  // 2. ';' kontrolü yap
            {
                return false;
            }
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

            if (Expect(TokenType.ROUNDCLOSE) == null)   // ')' kontrolü yap
            {
                return false;
            }

            FunctionState.NotSnoozed = false;   // son komutu bağımsız yap
            // son kısımda kaç komut oluşturulduğunu hesapla
            int exp_end = FunctionState.GetCurrPos();
            int exp_size = exp_end - exp_start + 1;
            // her bir iterasyondan sonra işlenecek komutların listesini oluştur
            List<ExInstr> instrs = new();

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
            FunctionState.AddInstr(OPC.JMP, 0, jpos - FunctionState.GetCurrPos() - 1, 0, 0);

            if (jzpos > 0)  // Koşul verişmiş ise atlanacak komut sayısını güncelle
            {
                FunctionState.UpdateInstructionArgument(jzpos, 1, FunctionState.GetCurrPos() - jzpos);
            }

            ReleaseScope(scp);                      // Eski çerçeveye dön
            ReleaseBreakableBlock(bc, exit_target); // İterasyon bloğundan çık
            return true;
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
            f_state.AddInstr(OPC.RETURN, ExMat.InvalidArgument, 0, 0, 0);
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
            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetLiteral(v), 0, 0);

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
            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetLiteral(v), 0, 0);

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

            f_state.AddInstr(OPC.RETURN, 1, FunctionState.PopTarget(), 0, 0);
            f_state.AddLineInfo(Lexer.TokenPrev == TokenType.NEWLINE ? Lexer.PrevTokenLine : Lexer.CurrentLine, StoreLineInfos, true);
            f_state.AddInstr(OPC.RETURN, ExMat.InvalidArgument, 0, 0, 0);
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
            f_state.AddInstr(OPC.RETURN, ExMat.InvalidArgument, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExPrototype fpro = f_state.CreatePrototype();

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
            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetLiteral(v), 0, 0);

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
            f_state.AddInstr(OPC.RETURN, ExMat.InvalidArgument, 0, 0, 0);
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
            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetLiteral(v), 0, 0);

            if (Expect(TokenType.ROUNDOPEN) == null || !ExRuleCreate(v))
            {
                return false;
            }

            FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);

            AddBasicDerefInstr(OPC.NEWSLOT);

            return true;
        }

        public bool ProcessVarAsgStatement()
        {
            ExObject v;
            if (!ReadAndSetToken()) // okunan 'var' sembolünden sonraki sembolü oku
            {
                return false;
            }
            #region _
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
            #endregion
            while (true)
            {
                if ((v = Expect(TokenType.IDENTIFIER)) == null) // Tanımlayıcı bekle
                {
                    return false;
                }

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
                        FunctionState.AddInstr(OPC.MOVE, destination, source, 0, 0);
                    }
                }
                else // '=' yoksa 'null' ata
                {
                    FunctionState.AddInstr(OPC.LOADNULL, FunctionState.PushTarget(), 1, 0, 0);
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
            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetLiteral(idx), 0, 0);

            #region _
            // Sınıfa ait metot olarak yazılıyor ise sınıfı bulma komutu ekle
            if (CurrentToken == TokenType.GLOBAL)
            {
                AddBasicOpInstr(OPC.GET);   // Okunan son 2 tanımlayıcı ile istenen metot ve sınıfın alınması sağlanır
            }

            while (CurrentToken == TokenType.GLOBAL)
            {
                if (!ReadAndSetToken() || (idx = Expect(TokenType.IDENTIFIER)) == null)
                {
                    return false;
                }

                FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetLiteral(idx), 0, 0);

                if (CurrentToken == TokenType.GLOBAL)
                {
                    AddBasicOpInstr(OPC.GET);
                }
            }
            #endregion
            // '(' sembolü bekle, bulunursa ExFuncCreate metotunu fonksiyonun ismi ile çağır
            if (Expect(TokenType.ROUNDOPEN) == null || !ExFuncCreate(idx))
            {
                return false;
            }

            // Hazırlanan fonksiyonu sanal belleğe yerleştir
            FunctionState.AddInstr(OPC.CLOSURE, FunctionState.PushTarget(), FunctionState.Functions.Count - 1, 0, 0);
            // Global tabloda yeni slot oluşturma işlemi
            AddBasicDerefInstr(OPC.NEWSLOT);
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

        public bool ExBinaryExp(OPC op, CompilingFunctionRef func, int arg3 = 0) // 1 işleç, 2 değer ile işlemi
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
                #region Other tokens: += -= /= *= %= <>
                case TokenType.ADDEQ:   // +=
                case TokenType.SUBEQ:   // -=
                case TokenType.DIVEQ:   // /=
                case TokenType.MLTEQ:   // *=
                case TokenType.MODEQ:   // %=
                case TokenType.NEWSLOT: // <>
                    #endregion
                    {
                        TokenType op = CurrentToken; ExEType etyp = ExpressionState.Type;
                        int targetpos = ExpressionState.Position;

                        switch (etyp)    // İfade tipini kontrol et
                        {
                            case ExEType.EXPRESSION: AddToErrorMessage("can't assing an expression"); return false;
                            case ExEType.BASE: AddToErrorMessage("can't modify 'base'"); return false;
                        }
                        if (!ReadAndSetToken() || !ExExp()) // Sağ tarafı derle
                        {
                            return false;
                        }

                        switch (op)
                        {
                            case TokenType.ASG:
                                {
                                    switch (etyp)
                                    {
                                        case ExEType.VAR:   // Değişkene değer ata
                                            {
                                                int source = FunctionState.PopTarget();
                                                int destination = FunctionState.TopTarget();
                                                FunctionState.AddInstr(OPC.MOVE, destination, source, 0, 0);
                                                break;
                                            }
                                        case ExEType.OBJECT:    // Objenin sahip olduğu bir değeri değiştir
                                        case ExEType.BASE:
                                            {
                                                AddBasicDerefInstr(OPC.SET);
                                                break;
                                            }
                                        case ExEType.OUTER:     // Dışardaki bir değişkene değer ata
                                            {
                                                int source = FunctionState.PopTarget();
                                                int destination = FunctionState.PushTarget();
                                                FunctionState.AddInstr(OPC.SETOUTER, destination, targetpos, source, 0);
                                                break;
                                            }
                                    }
                                    break;
                                }
                            #region Rest of the tokens
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
                                #endregion
                        }
                        break;
                    }
                case TokenType.QMARK:   // ?
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

                        FunctionState.UpdateInstructionArgument(jmp, 1, FunctionState.GetCurrPos() - jmp);
                        FunctionState.UpdateInstructionArgument(jzp, 1, end_f - jzp + 1);
                        FunctionState.NotSnoozed = false;

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

                            FunctionState.AddInstr(OPC.OR, t, 0, f, 0);

                            int j = FunctionState.GetCurrPos();

                            if (t != f)
                            {
                                FunctionState.AddInstr(OPC.MOVE, t, f, 0, 0);
                            }

                            if (!ReadAndSetToken() || !ExInvokeExp(ExLogicOr))
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

                            FunctionState.AddInstr(OPC.AND, t, 0, f, 0);

                            int j = FunctionState.GetCurrPos();

                            if (t != f)
                            {
                                FunctionState.AddInstr(OPC.MOVE, t, f, 0, 0);
                            }
                            if (!ReadAndSetToken() || !ExInvokeExp(ExLogicAnd))
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
                            if (!ExBinaryExp(OPC.BITWISE, ExLogicBXOr, (int)BitOP.OR))
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
                            if (!ExBinaryExp(OPC.BITWISE, ExLogicBAnd, (int)BitOP.XOR))
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
                            if (!ExBinaryExp(OPC.BITWISE, ExLogicEq, (int)BitOP.AND))
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
                            if (!ExBinaryExp(OPC.EQ, ExLogicCmp))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.NEQ:
                        {
                            if (!ExBinaryExp(OPC.NEQ, ExLogicCmp))
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
                switch (CurrentToken)
                {
                    case TokenType.GRT:
                        {
                            if (!ExBinaryExp(OPC.CMP, ExLogicShift, (int)CmpOP.GRT))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.LST:
                        {
                            if (!ExBinaryExp(OPC.CMP, ExLogicShift, (int)CmpOP.LST))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.GET:
                        {
                            if (!ExBinaryExp(OPC.CMP, ExLogicShift, (int)CmpOP.GET))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.LET:
                        {
                            if (!ExBinaryExp(OPC.CMP, ExLogicShift, (int)CmpOP.LET))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.NOTIN:
                    case TokenType.IN:
                        {
                            if (!ExBinaryExp(OPC.EXISTS, ExLogicShift, CurrentToken == TokenType.NOTIN ? 1 : 0))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.INSTANCEOF:
                        {
                            if (!ExBinaryExp(OPC.INSTANCEOF, ExLogicShift))
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
                            if (!ExBinaryExp(OPC.BITWISE, ExArithAdd, (int)BitOP.SHIFTL))
                            {
                                return false;
                            }
                            break;
                        }
                    case TokenType.RSHF:
                        {
                            if (!ExBinaryExp(OPC.BITWISE, ExArithAdd, (int)BitOP.SHIFTR))
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
                            if (!ExBinaryExp(CurrentToken == TokenType.ADD ? OPC.ADD : OPC.SUB, ExArithMlt))
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
            int p = -1;
            if (!ExFactor(ref p))
            {
                return false;
            }
            while (true)
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

                            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetLiteral(tmp), 0, 0);

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
                            return true;
                        }
                }
            }
        }

        public bool ExFactor(ref int pos)
        {
            ExpressionState.Type = ExEType.EXPRESSION;

            switch (CurrentToken)
            {
                #region Diğer Semboller
                case TokenType.LAMBDA:
                case TokenType.FUNCTION:
                    {
                        if (!ExFuncResolveExp(CurrentToken))
                        {
                            return false;
                        }
                        break;
                    }
                case TokenType.LITERAL:
                    {
                        FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetLiteral(FunctionState.CreateString(Lexer.ValString)), 0, 0);

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
                            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetLiteral(idx), 0, 0);

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
                #endregion
                case TokenType.INTEGER:
                    {
                        AddIntConstLoadInstr(Lexer.ValInteger, -1); // Sembol ayırıcıdaki tamsayı değerini al
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
                        if (Lexer.TokenComplex == TokenType.INTEGER)    // Tamsayı katsayılı ?
                        {
                            AddComplexConstLoadInstr(Lexer.ValInteger, -1);
                        }
                        else // Ondalıklı sayı katsayılı
                        {
                            AddComplexConstLoadInstr(new DoubleLong() { f = Lexer.ValFloat }.i, -1, true);
                        }

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
                            if (CurrentToken == TokenType.SEP
                                && !ReadAndSetToken())
                            {
                                return false;
                            }

                            int v = FunctionState.PopTarget();
                            int a = FunctionState.TopTarget();
                            FunctionState.AddInstr(OPC.APPENDTOARRAY, a, v, (int)ArrayAType.STACK, 0);
                            k++;
                        }
                        FunctionState.UpdateInstructionArgument(p, 1, k);
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
            FunctionState.AddInstr(OPC.LOADINTEGER,                         // Tamsayı yükle komutu
                                   p < 0 ? FunctionState.PushTarget() : p,  // Hedef
                                   cval,                                    // Kaynak 64bit değer
                                   0,
                                   0);
        }
        public void AddFloatConstLoadInstr(long cval, int p)                // Ondalıklı sayı yükle
        {
            FunctionState.AddInstr(OPC.LOADFLOAT,                           // Ondalıklı sayı yükle komutu
                                   p < 0 ? FunctionState.PushTarget() : p,  // Hedef
                                   cval,                                    // Kaynak 64bit değer
                                   0,
                                   0);
        }
        public void AddComplexConstLoadInstr(long cval, int p, bool useFloat = false)    // Kompleks sayı yükle
        {
            FunctionState.AddInstr(OPC.LOADCOMPLEX,                         // Kompleks sayı yükle komutu
                                   p < 0 ? FunctionState.PushTarget() : p,  // Hedef
                                   cval,                                    // Kaynak 64bit değer
                                   useFloat ? 1 : 0,                        // Kaynak ondalıklı mı kullanılmalı ?
                                   0);
        }
        public void AddSpaceConstLoadInstr(ExSpace s, int p)                            // Uzay değeri yükle
        {
            FunctionState.AddInstr(OPC.LOADSPACE,                                       // Uzay yükle komutu
                                   p < 0 ? FunctionState.PushTarget() : p,              // Hedef
                                   (int)FunctionState.GetLiteral(new(Lexer.ValSpace)),  // Uzay temsilinin indeksi
                                   0,
                                   0);
        }

        public void AddBasicDerefInstr(OPC op)
        {
            int value = FunctionState.PopTarget();
            int key = FunctionState.PopTarget();
            int source = FunctionState.PopTarget();
            FunctionState.AddInstr(op, FunctionState.PushTarget(), source, key, value);
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
                if (sep == TokenType.SMC
                    && CurrentToken == TokenType.ATTRIBUTEBEGIN)
                {
                    FunctionState.AddInstr(OPC.NEWOBJECT, FunctionState.PushTarget(), 0, (int)ExNOT.DICT, 0);

                    if (!ReadAndSetToken() || !ParseDictClusterOrClass(TokenType.SEP, TokenType.ATTRIBUTEFINISH))
                    {
                        AddToErrorMessage("failed to parse attribute");
                        return false;
                    }
                    a_present = true;
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

                            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetLiteral(o), 0, 0);

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
                                FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetLiteral(o), 0, 0);

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

                            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetLiteral(o), 0, 0);

                            if (Expect(TokenType.ASG) == null || !ExExp())
                            {
                                return false;
                            }

                            break;
                        }
                }

                if (CurrentToken == sep
                    && !ReadAndSetToken())
                {
                    return false;
                }
                n++;

                int v = FunctionState.PopTarget();
                int k = FunctionState.PopTarget();
                int a = a_present ? FunctionState.PopTarget() : -1;

                if (!((a_present && (a == k - 1)) || !a_present))
                {
                    throw new ExException("attributes present count error");
                }

                int flg = a_present ? (int)ExNewSlotFlag.ATTR : 0; // to-do static flag
                int t = FunctionState.TopTarget();
                if (sep == TokenType.SEP)
                {
                    FunctionState.AddInstr(OPC.NEWSLOT, ExMat.InvalidArgument, t, k, v);
                }
                else
                {
                    FunctionState.AddInstr(OPC.NEWSLOTA, flg, t, k, v);
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
            int at = ExMat.InvalidArgument;

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

            if (at != ExMat.InvalidArgument)
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

            f_state.AddInstr(OPC.RETURN, ExMat.InvalidArgument, 0, 0, 0);
            f_state.SetLocalStackSize(0);

            ExPrototype fpro = f_state.CreatePrototype();

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

            Lexer = new(Lexer.MacroBlock.ToString()) { MacroParams = Lexer.MacroParams, MacroBlock = Lexer.MacroBlock, IsReadingMacroBlock = true };

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
            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetLiteral(idx), 0, 0);
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
                FunctionState.AddBlockMacro(idx.GetString(), new() { Name = idx.GetString(), Source = Lexer.MacroBlock.ToString(), Parameters = Lexer.MacroParams });
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
            FunctionState.AddInstr(OPC.LOAD, FunctionState.PushTarget(), (int)FunctionState.GetLiteral(idx), 0, 0);
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

        // Fonksiyon takipçisine ait bir alt fonksiyon oluştur
        public bool ExFuncCreate(ExObject o, bool lambda = false)
        {
            // Bir alt fonksiyon takipçisi oluştur ve istenen fonksiyon ismini ver
            ExFState f_state = FunctionState.PushChildState(VM.SharedState);
            f_state.Name = o;

            ExObject pname;
            // Fonksiyonun kendi değişkenlerine referans edecek 'this' referansını ekle
            f_state.AddParam(FunctionState.CreateString(ExMat.ThisName));
            f_state.Source = new(Source);

            int def_param_count = 0;    // Varsayılan parametre değeri sayısı

            while (CurrentToken != TokenType.ROUNDCLOSE) // Parametreleri oku
            {
                if (CurrentToken == TokenType.VARGS)     // Belirsiz sayıda parametre sembolü "..."
                {
                    if (def_param_count > 0)             // Varsayılan değerler ile "..." kullanılamaz
                    {
                        AddToErrorMessage("can't use vargs alongside default valued parameters");
                        return false;
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
                        AddToErrorMessage("expected ')' after vargs '...'");
                        return false;
                    }
                    break;  // Parametre bölümünü bitir
                }
                if ((pname = Expect(TokenType.IDENTIFIER)) == null) // Parametre ismi bekle
                {
                    return false;
                }

                f_state.AddParam(pname);    // Uygun parametre ismini ekle

                if (CurrentToken == TokenType.ASG)  // '=' bekle, bulunursa varsayılan değer vardır 
                {
                    if (!ReadAndSetToken() || !ExExp()) // Varsayılan değeri derle
                    {
                        return false;
                    }

                    f_state.AddDefParam(FunctionState.TopTarget()); // Varsayılan değeri ekle
                    def_param_count++;
                }
                else
                {
                    if (def_param_count > 0)    // Varsayılan değerli parametreden sonraki parametrelerin kontrolü
                    {
                        AddToErrorMessage("expected = for a default value");
                        return false;
                    }
                }

                if (CurrentToken == TokenType.SEP)  // ',' bekle, var ise başka parametre vardır
                {
                    if (!ReadAndSetToken())
                    {
                        return false;
                    }
                }
                else if (CurrentToken != TokenType.ROUNDCLOSE)  // ',' yoksa ')' beklenmek zorundadır
                {
                    AddToErrorMessage("expected ')' or ',' for function declaration");
                    return false;
                }
            }

            if (Expect(TokenType.ROUNDCLOSE) == null)   // ')' bulunduğunu kontrol et
            {
                return false;
            }

            for (int i = 0; i < def_param_count; i++)   // Varsayılan parametre indekslerini çıkart
            {
                FunctionState.PopTarget();
            }

            ExFState tmp = FunctionState.Copy();    // Fonksiyon takipçisinin kopyasını al
            FunctionState = f_state;
            if (lambda)     // Lambda ifadesi ise sıradaki ifadeyi derle ve OPC.RETURN ile sonucu döndür
            {
                if (!ExExp())
                {
                    return false;
                }
                f_state.AddInstr(OPC.RETURN, 1, FunctionState.PopTarget(), 0, 0);
            }
            else
            {
                if (!ProcessStatement(false))       // Normal fonksiyon ise bütün ifadeleri derle
                {
                    return false;
                }
            }

            f_state.AddLineInfo(Lexer.TokenPrev == TokenType.NEWLINE ? Lexer.PrevTokenLine : Lexer.CurrentLine, StoreLineInfos, true);

            f_state.AddInstr(OPC.RETURN, ExMat.InvalidArgument, 0, 0, 0); // Fonksiyondan çıkıldığından emin olmak için OPC.RETURN komutu ekle
            f_state.SetLocalStackSize(0);                   // Yığını temizle
            ExPrototype fpro = f_state.CreatePrototype();  // Prototipi oluştur

            FunctionState = tmp;                // Bir önceki takipçiye dön
            FunctionState.Functions.Add(fpro);  // Prototipi ekle
            FunctionState.PopChildState();      // Alt fonksiyonu çıkart
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

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
