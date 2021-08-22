using System;
using System.IO;
using System.Text;
using System.Threading;
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
        /// Compile and execute given code in given virtual machine instance
        /// </summary>
        /// <param name="vm">Virtual machine instance to compile and execute on</param>
        /// <param name="code">Code string to compile</param>
        /// <returns>Value returned by <paramref name="code"/> or last statement's return value if no <see langword="return"/> was specified</returns>
        private static int CompileString(ExVM vm, string code)
        {
            int tp = vm.StackTop - vm.StackBase;
            int ret = 0;

            SetFlag(ExInteractiveConsoleFlag.CURRENTLYEXECUTING);

            if (ExAPI.CompileSource(vm, code))      // Derle
            {
                ExAPI.PushRootTable(vm);            // Global tabloyu belleğe yükle
                if (ExAPI.Call(vm, 1, true, true))  // main 'i çağır
                {
                    if (!vm.PrintedToConsole
                        && vm.GetAbove(-1).Type != ExObjType.NULL
                        && ExAPI.ToString(vm, -1, 2))
                    {
                        Console.Write(vm.GetAbove(-1).GetString());
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

            RemoveFlag(ExInteractiveConsoleFlag.CURRENTLYEXECUTING);

            return ret;
        }

        /// <summary>
        /// Nulls stack, resets base and top to given index
        /// </summary>
        /// <param name="vm">Virtual machine to use stack of</param>
        /// <param name="count">Amount of objects to skip</param>
        private static void FixStackTopAfterCalls(ExVM vm, int count)
        {
            for (int idx = vm.Stack.Count - 1; idx >= count; idx--)
            {
                vm.Stack[idx].Nullify();
                vm.Stack[idx] = new();
            }
            vm.StackBase = count - 1;
            vm.StackTop = count;
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

            Console.Write(new string(' ', (width - vlen) / 2) + version + new string(' ', ((width - vlen) / 2) + (vlen % 2 == 1 ? 1 : 0)));
            Console.WriteLine("/");
            Console.Write("/" + new string(' ', (width - dlen) / 2) + date + new string(' ', ((width - dlen) / 2) + (dlen % 2 == 1 ? 1 : 0)) + "/\n");

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
        /// Time in ms to delay output so CTRL+C doesn't mess up
        /// </summary>
        private static readonly int CANCELKEY_THREAD_TIMER = 50;

        /// <summary>
        /// Title of the interactive console
        /// </summary>
        private static readonly string ConsoleTitle = "[] ExMat Interactive";

        /// <summary>
        /// Reads user input, cleans it up, sets console flags
        /// </summary>
        /// <returns>Cleaned user input</returns>
        private static string GetInput()
        {
            string code = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(code))
            {
                SetFlag(ExInteractiveConsoleFlag.EMPTYINPUT);
                return string.Empty;
            }
            else
            {
                code = code.TrimEnd(' ', '\t');

                if (string.IsNullOrWhiteSpace(code))
                {
                    SetFlag(ExInteractiveConsoleFlag.EMPTYINPUT);
                    code = string.Empty;
                }

                if (code.EndsWith('\\'))
                {
                    ToggleFlag(ExInteractiveConsoleFlag.LINECARRY);
                }

                return code.TrimEnd('\\', ' ', '\t');
            }
        }

        /// <summary>
        /// Checks if interactive console has the given flag
        /// </summary>
        /// <param name="flag">Flag to check</param>
        protected static bool HasFlag(ExInteractiveConsoleFlag flag)
        {
            return ((int)flag & InteractiveConsoleFlags) != 0;
        }

        /// <summary>
        /// Sets given interactive console flag
        /// </summary>
        /// <param name="flag">Flag to set</param>
        protected static void SetFlag(ExInteractiveConsoleFlag flag)
        {
            InteractiveConsoleFlags |= (int)flag;
        }

        /// <summary>
        /// Removes given interactive console flag
        /// </summary>
        /// <param name="flag">Flag to remove</param>
        protected static void RemoveFlag(ExInteractiveConsoleFlag flag)
        {
            InteractiveConsoleFlags &= ~(int)flag;
        }

        /// <summary>
        /// Toggles given interactive console flag
        /// </summary>
        /// <param name="flag">Flag to switch/toggle</param>
        protected static void ToggleFlag(ExInteractiveConsoleFlag flag)
        {
            InteractiveConsoleFlags ^= (int)flag;
        }

        /// <summary>
        /// Interactive console flags
        /// </summary>
        protected static int InteractiveConsoleFlags;

        /// <summary>
        /// Adds cancel event handler to the console
        /// </summary>
        protected static void AddCancelEventHandler()
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelEventHandler);
        }

        /// <summary>
        /// Handler for cancel event CTRLC and CTRLBREAK
        /// </summary>
        protected static void CancelEventHandler(object sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;
            SetFlag(ExInteractiveConsoleFlag.CANCELEVENT);
        }

        /// <summary>
        /// Registers math, io, string and net standard libraries
        /// </summary>
        /// <param name="vm">Virtual machine to register libraries for</param>
        private static void RegisterStdLibraries(ExVM vm)
        {
            ExStdMath.RegisterStdMath(vm);          // Matematik kütüphanesi
            ExStdIO.RegisterStdIO(vm);              // Girdi/çıktı, dosya kütüphanesi
            ExStdString.RegisterStdString(vm);      // Yazı dizisi işleme kütüphanesi
            ExStdNet.RegisterStdNet(vm);            // Ağ işlemleri kütüphanesi
        }

        private static void Indent(int n = 1)
        {
            Console.Write(new string('\t', n));
        }

        private static void ContinueDelayedInput(StringBuilder code)
        {
            Thread.Sleep(CANCELKEY_THREAD_TIMER);
            while (HasFlag(ExInteractiveConsoleFlag.CANCELEVENT))
            {
                RemoveFlag(ExInteractiveConsoleFlag.CANCELEVENT);
                code.Append(GetInput());
                Thread.Sleep(CANCELKEY_THREAD_TIMER);
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
                RegisterStdLibraries(v);

                return CompileString(v, f); // Derle ve işle, sonucu dön
            }
            #endregion

            // Interactive
            #region Interactive
            ExVM vm = ExAPI.Start(VM_STACK_SIZE, true); // Sanal makineyi başlat
            ExAPI.PushRootTable(vm);                    // Global tabloyu ekle

            RegisterStdLibraries(vm);   // Standard kütüphaneler

            int count = 0;              // Gönderilen kod dizisi sayısı
            StringBuilder code = new(); // Kod yazı dizisi

            ///////////
            // Event handlers
            AddCancelEventHandler();
            // Title, Version info
            Console.Title = ConsoleTitle;
            Console.ResetColor();
            WriteVersion(vm);
            ///////////

            while (true)    // Sürekli olarak girdi al
            {
                if (HasFlag(ExInteractiveConsoleFlag.LINECARRY))  // Çok satırlı kod oku
                {
                    code.Append(GetInput());
                    ContinueDelayedInput(code);
                }
                else
                {
                    if (!HasFlag(ExInteractiveConsoleFlag.CANCELEVENT))
                    {
                        ///////////
                        WriteIn(count); // Girdi numarası yaz
                        ///////////
                    }

                    code = new(GetInput());
                    ContinueDelayedInput(code);

                    // En son işlenen kodda kullanıcıdan girdi aldıysa tekrardan okuma işlemi başlat
                    if (vm.GotUserInput && HasFlag(ExInteractiveConsoleFlag.EMPTYINPUT))
                    {
                        RemoveFlag(ExInteractiveConsoleFlag.EMPTYINPUT);
                        vm.GotUserInput = false;

                        code = new(GetInput());
                        ContinueDelayedInput(code);
                    }
                }

                if (HasFlag(ExInteractiveConsoleFlag.LINECARRY))  // Çok satırlı ise okumaya devam et
                {
                    Indent();
                    code.Append("\r\n");
                    continue;
                }

                ContinueDelayedInput(code);

                ///////////
                WriteOut(count++);    // Çıktı numarası yaz
                ///////////

                int ret = CompileString(vm, code.ToString().TrimEnd('\\', ' ', '\t'));  // Derle ve işle

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
