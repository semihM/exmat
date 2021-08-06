using System;
using System.IO;
using ExMat.API;
using ExMat.BaseLib;
using ExMat.VM;

namespace ExMat
{
    /// <summary>
    /// Refer to docs inside this class for more information
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Stack size of the virtual machines. Use higher values for potentially more recursive functions 
        /// </summary>
        private static readonly int VM_STACK_SIZE = 2 << 14;

        /// <summary>
        /// Checks if user started writing multi-line code
        /// </summary>
        /// <param name="code">User input</param>
        /// <returns><see langword="true"/> if input had <c>'\'</c> character at end, otherwise <see langword="false"/></returns>
        private static bool CheckCarryOver(string code)
        {
            return code.Length > 0 && code[^1] == '\\';
        }

        /// <summary>
        /// Compile and execute given code in given virtual machine instance
        /// </summary>
        /// <param name="vm">Virtual machine instance to compile and execute on</param>
        /// <param name="code">Code string to compile</param>
        /// <returns>Value returned by <paramref name="code"/> or last statement's return value if no <see langword="return"/> was specified</returns>
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
                        ExAPI.WriteErrorMessages(vm, ExErrorType.RUNTIME);   // İşleme hatası
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

        /// <summary>
        /// Nulls objects from top of the stack, skipping <paramref name="count"/> amount of them from stack base
        /// </summary>
        /// <param name="vm">Virtual machine to use stack of</param>
        /// <param name="count">Amount of objects to skip</param>
        private static void FixStackTopAfterCalls(ExVM vm, int count)
        {
            int curr = vm.StackTop - vm.StackBase;
            if (curr > count)
            {
                vm.Pop(curr - count);
            }
            else
            {
                while (curr++ < count)
                {
                    vm.Stack[vm.StackTop++].Nullify();
                }
            }
        }

        /// <summary>
        /// Writes version and program info in different colors
        /// </summary>
        /// <param name="vm">Virtual machine to get information from</param>
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

        /// <summary>
        /// Writes <c>OUT[<paramref name="line"/>]</c> for <c><paramref name="line"/></c>th output line's beginning
        /// </summary>
        /// <param name="line">Output line number</param>
        private static void WriteOut(int line)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("OUT[");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(line);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("]: ");
            Console.ResetColor();
        }

        /// <summary>
        /// Writes <c>IN [<paramref name="line"/>]</c> for <c><paramref name="line"/></c>th input line's beginning
        /// </summary>
        /// <param name="line">Input line number</param>
        private static void WriteIn(int line)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if (line > 0)
            {
                Console.Write("\n");
            }
            Console.Write("\nIN [");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(line);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("]: ");
            Console.ResetColor();
        }

        /// <summary>
        /// Reads user input and cleans it up
        /// </summary>
        /// <returns>Cleaned user input</returns>
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

        /// <summary>
        /// To compile and execute a script file use: <code>exmat.exe file_name.exmat</code>
        /// To start the interactive console, use: <code>exmat.exe</code>
        /// </summary>
        /// <param name="args">If any arguments given, first one is taken as file name</param>
        /// <returns>If a file was given and it didn't exists: <code>-1</code>
        /// If given file exists, returns whatever is returned from <see cref="CompileString"/><code></code>
        /// If interactive console is used, only returns when <c>exit</c> function is called</returns>
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
