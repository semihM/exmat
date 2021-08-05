using System;
using System.IO;
using ExMat.API;
using ExMat.BaseLib;
using ExMat.VM;

namespace ExMat
{
    internal class Program
    {
        private static readonly int VM_STACK_SIZE = 2 << 14;

        private static bool CheckCarryOver(string code)
        {
            return code.Length > 0 && code[^1] == '\\';
        }

        private static int CompileString(ExVM vm, string code)
        {
            int tp = vm.StackTop - vm.StackBase;
            int ret = 0;

            if (ExAPI.CompileSource(vm, code))      // Derle
            {
                ExAPI.PushRootTable(vm);            // Global tabloyu belleğe yükle
                if (ExAPI.Call(vm, 1, true, true))  // main 'i çağır
                {
                    // En üstteki değer boş değil ve konsola başka değer yazılmadıysa 
                    //      değeri konsola yaz
                    if (!vm.PrintedToConsole && vm.GetAbove(-1).Type != ExObjType.NULL)
                    {
                        if (ExAPI.ToString(vm, -1, 2)) // Değeri yazı dizisine çevir
                        {
                            Console.Write(vm.GetAbove(-1).GetString());
                        }
                    }
                }
                else
                {
                    if (vm.ExitCalled)      // Konsoldan çıkış fonksiyonu çağırıldı
                    {
                        ret = vm.ExitCode;
                    }
                    else
                    {
                        ExAPI.WriteErrorMessages(vm, ExErrorType.EXECUTE);   // İşleme hatası
                        ret = -1;
                    }
                }
            }
            else
            {
                ExAPI.WriteErrorMessages(vm, ExErrorType.COMPILE);            // Derleme hatası 
                ret = -1;
            }

            FixStackTopAfterCalls(vm, tp);  // Kullanılmayan değerleri temizle
            vm.PrintedToConsole = false;
            return ret;
        }

        private static void FixStackTopAfterCalls(ExVM vm, int t)
        {
            int curr = vm.StackTop - vm.StackBase;
            if (curr > t)
            {
                vm.Pop(curr - t);
            }
            else
            {
                while (curr++ < t)
                {
                    vm.Stack[vm.StackTop++].Nullify();
                }
            }
        }

        private static void WriteVersion(ExVM vm)
        {
            string version = vm.RootDictionary.GetDict()["_version_"].GetString();
            string date = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
            int width = 60;
            int vlen = version.Length;
            int dlen = date.Length;

            Console.BackgroundColor = ConsoleColor.DarkBlue;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(new string('/', width + 2));
            Console.Write("/");

            Console.Write(new string(' ', (width - vlen) / 2) + version + new string(' ', (width - vlen) / 2 + (vlen % 2 == 1 ? 1 : 0)));
            Console.WriteLine("/");
            Console.Write("/" + new string(' ', (width - dlen) / 2) + date + new string(' ', (width - dlen) / 2 + (dlen % 2 == 1 ? 1 : 0)) + "/\n");

            Console.WriteLine(new string('/', width + 2));

            Console.ResetColor();
        }

        private static void WriteOut(int count)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("OUT[");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(count);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("]: ");
            Console.ResetColor();
        }

        private static void WriteIn(int count)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if (count > 0)
            {
                Console.Write("\n");
            }
            Console.Write("\nIN [");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(count);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("]: ");
            Console.ResetColor();
        }

        private static string GetInput()
        {
            string code = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(code))
            {
                return string.Empty;
            }
            else
            {
                return code.TrimEnd(' ', '\t');
            }
        }

        private static int Main(string[] args)
        {
            #region File
            if (args.Length >= 1)
            {
                // Dosya kontrolü
                string f = File.Exists(args[0]) ? File.ReadAllText(args[0]) : string.Empty;
                if (string.IsNullOrWhiteSpace(f))
                {
                    return -1;
                }

                // Sanal makineyi başlat
                ExVM v = ExAPI.Start(VM_STACK_SIZE);
                // Versiyon numarası
                WriteVersion(v);

                // Global tabloyu belleğe yükle
                ExAPI.PushRootTable(v);
                // Standart kütüphaneleri tabloya kaydet
                ExStdMath.RegisterStdMath(v);
                ExStdIO.RegisterStdIO(v);
                ExStdString.RegisterStdString(v);

                return CompileString(v, f); // Derle ve işle, sonucu dön
            }
            #endregion

            // Interactive
            #region Interactive
            ExVM vm = ExAPI.Start(VM_STACK_SIZE, true); // Sanal makineyi başlat
            ExAPI.PushRootTable(vm);                    // Global tabloyu ekle

            ExStdMath.RegisterStdMath(vm);          // Matematik kütüphanesi
            ExStdIO.RegisterStdIO(vm);              // Girdi/çıktı, dosya kütüphanesi
            ExStdString.RegisterStdString(vm);      // Yazı dizisi işleme kütüphanesi

            int count = 0;              // Gönderilen kod dizisi sayısı
            bool carryover = false;     // \ ile çok satırlı kod başlatıldı ?
            string code = string.Empty; // Kod yazı dizisi

            ///////////
            Console.Title = "[] ExMat Interactive";
            Console.ResetColor();
            WriteVersion(vm);
            ///////////

            while (true)    // Sürekli olarak girdi al
            {
                if (carryover)  // Çok satırlı kod oku
                {
                    Console.Write("\t");
                    code += GetInput();
                    if (CheckCarryOver(code)) // Tekrardan \ verildiyse bitir
                    {
                        carryover = false;
                    }
                    code = code.TrimEnd('\\', ' ', '\t');
                }
                else
                {
                    // Girdi numarası yaz
                    WriteIn(count);
                    // Kod dizisini oku
                    code = GetInput();
                    // En son işlenen kodda kullanıcıdan girdi aldıysa tekrardan okuma işlemi başlat
                    if (vm.GotUserInput && string.IsNullOrWhiteSpace(code))
                    {
                        vm.GotUserInput = false;
                        code = GetInput();
                    }

                    if (CheckCarryOver(code))   // Çok satırlı mı ?
                    {
                        carryover = true;
                    }
                }

                if (carryover)  // Çok satırlı ise okumaya devam et
                {
                    code = code.TrimEnd('\\', ' ', '\t') + "\r\n";
                    continue;
                }

                ///////////
                WriteOut(count++);    // Çıktı numarası yaz
                carryover = false;
                ///////////

                int ret = CompileString(vm, code);  // Derle ve işle

                ExAPI.CollectGarbage(); // Çöp toplayıcıyı çağır

                if (vm.ExitCalled)  // exit fonksiyonu çağırıldıysa bitir
                {
                    return ret;
                }
            }
            #endregion
        }
    }
}
