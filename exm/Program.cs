using System;
using System.IO;
using System.Text;
using System.Threading;
using ExMat.API;
using ExMat.BaseLib;
using ExMat.Exceptions;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat
{
    /// <summary>
    /// Refer to docs inside this class for more information
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// Stack size of the virtual machines. Use higher values for potentially more recursive functions 
        /// </summary>
        private static readonly int VM_STACK_SIZE = 2 << 14;

        /// <summary>
        /// Time in ms to delay output so CTRL+C doesn't mess up
        /// </summary>
        private static readonly int CANCELKEY_THREAD_TIMER = 50;

        /// <summary>
        /// Active virtual machine
        /// </summary>
        private static ExVM ActiveVM;

        /// <summary>
        /// Active thread for <see cref="ActiveVM"/>
        /// </summary>
        private static Thread ActiveThread;

        /// <summary>
        /// Value returned from <see cref="ActiveThread"/>
        /// </summary>
        private static ExObject ReturnValue;

        /// <summary>
        /// File contents read from console call
        /// </summary>
        private static string FileContents;

        /// <summary>
        /// Amount of objects to nullify after VM completes executing
        /// </summary>
        private static int ResetInStack;

        private static void ResetAfterCompilation(ExVM vm, int n)
        {
            FixStackTopAfterCalls(vm, n);  // Kullanılmayan değerleri temizle
            vm.PrintedToConsole = false;

            ActiveVM.RemoveFlag(ExInteractiveConsoleFlag.CURRENTLYEXECUTING);
        }

        /// <summary>
        /// Compile and execute given code in given virtual machine instance
        /// </summary>
        /// <param name="vm">Virtual machine instance to compile and execute on</param>
        /// <param name="code">Code string to compile</param>
        /// <returns>Value returned by <paramref name="code"/> or last statement's return value if no <see langword="return"/> was specified</returns>
        private static int CompileString(ExVM vm, string code)
        {
            ResetInStack = vm.StackTop - vm.StackBase;
            int ret;

            ActiveVM.SetFlag(ExInteractiveConsoleFlag.CURRENTLYEXECUTING);

            if (ExApi.CompileSource(vm, code))      // Derle
            {
                ret = ExApi.CallTop(vm);
            }
            else
            {
                ret = ExApi.WriteErrorMessages(vm, ExErrorType.COMPILE);
            }

            ResetAfterCompilation(vm, ResetInStack);
            return ret;
        }

        /// <summary>
        /// Nulls stack, resets base and top to given index
        /// </summary>
        /// <param name="vm">Virtual machine to use stack of</param>
        /// <param name="count">Amount of objects to skip</param>
        private static void FixStackTopAfterCalls(ExVM vm, int count)
        {
            for (int idx = vm.Stack.Allocated - 1; idx >= count; idx--)
            {
                vm.Stack[idx].Nullify();
                vm.Stack[idx] = new();
            }
            vm.StackBase = count - 1;
            vm.StackTop = count;
        }

        /// <summary>
        /// Reads user input, cleans it up, sets console flags
        /// </summary>
        /// <returns>Cleaned user input</returns>
        private static string GetInput()
        {
            string code = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(code))
            {
                ActiveVM.SetFlag(ExInteractiveConsoleFlag.EMPTYINPUT);
                return string.Empty;
            }
            else
            {
                code = TrimCode(code, false);

                if (string.IsNullOrWhiteSpace(code))
                {
                    ActiveVM.SetFlag(ExInteractiveConsoleFlag.EMPTYINPUT);
                    code = string.Empty;
                }

                if (code.EndsWith('\\'))
                {
                    ActiveVM.ToggleFlag(ExInteractiveConsoleFlag.LINECARRY);
                }

                return TrimCode(code);
            }
        }

        /// <summary>
        /// Adds cancel event handler to the console
        /// </summary>
        private static void AddCancelEventHandler()
        {
            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelEventHandler);
        }

        private static void SetupDummyInterruptFrame()
        {
            ActiveVM.CallStack = new(4)
            {
                new()
                {
                    Closure = new(
                            new Closure.ExClosure()
                            {
                                DefaultParams = new(),
                                Function = new(),
                                OutersList = new(),
                                SharedState = ActiveVM.SharedState
                            }
                        ),
                    Literals = new(),
                    Instructions = new(2) { new(OPs.ExOperationCode.RETURN, ExMat.InvalidArgument, 0, 2, 0), new(OPs.ExOperationCode.RETURN, ExMat.InvalidArgument, 2, 1, 0) },
                    InstructionsIndex = 0,
                    IsRootCall = true,
                    PrevBase = 2,
                    PrevTop = 3,
                    Target = 2,
                    nCalls = 1
                },
                new(),
                new(),
                new()
            };

            ActiveVM.CallInfo = InfoVar.ExNode<InfoVar.ExCallInfo>.BuildNodesFromList(ActiveVM.CallStack);
        }

        // TO-DO Find a way to interrupt .NET operations
        /// <summary>
        /// Handler for cancel event CTRLC and CTRLBREAK
        /// </summary>
        private static void CancelEventHandler(object sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;

            ActiveVM.SetFlag(ExInteractiveConsoleFlag.CANCELEVENT);

            if (ActiveVM.HasFlag(ExInteractiveConsoleFlag.CURRENTLYEXECUTING))
            {
                ActiveVM.RemoveFlag(ExInteractiveConsoleFlag.CURRENTLYEXECUTING);
                ActiveVM.SetFlag(ExInteractiveConsoleFlag.RECENTLYINTERRUPTED);

                ActiveVM.ForceThrow = true;
                ActiveVM.ErrorOverride = ExErrorType.INTERRUPT;
                ActiveVM.AddToErrorMessage(string.Format("Output '{0}' was interrupted by control character: '{1}'", ActiveVM.InputCount, args.SpecialKey.ToString()));

                SetupDummyInterruptFrame();

                if (ActiveVM.IsSleeping)
                {
                    ActiveVM.ActiveThread.Interrupt();
                    ActiveVM.IsSleeping = false;
                }

                ExApi.SleepVM(ActiveVM, CANCELKEY_THREAD_TIMER);
                ResetAfterCompilation(ActiveVM, ResetInStack);
            }
            else
            {
                ActiveVM.AddToErrorMessage(string.Format("Input stream '{0}' was interrupted by control character: '{1}'", ActiveVM.InputCount, args.SpecialKey.ToString()));
                ExApi.WriteErrorMessages(ActiveVM, ExErrorType.INTERRUPTINPUT);
                ActiveVM.PrintedToConsole = false; // Hack, allow writing stack top after interruption
            }
        }

        /// <summary>
        /// Registers math, io, string and net standard libraries
        /// </summary>
        /// <param name="vm">Virtual machine to register libraries for</param>
        private static void RegisterStdLibraries(ExVM vm)
        {
            ExStdMath.RegisterStdMath(vm);      // Matematik kütüphanesi
            ExStdIO.RegisterStdIO(vm);          // Girdi/çıktı, dosya kütüphanesi
            ExStdString.RegisterStdString(vm);  // Yazı dizisi işleme kütüphanesi
            ExStdNet.RegisterStdNet(vm);        // Ağ işlemleri kütüphanesi
            ExStdSys.RegisterStdSys(vm);          // Arayüz kütüphanesi
        }

        private static void Indent(int n = 1)
        {
            Console.Write(new string('\t', n));
        }

        private static void DelayAfterInterruption()
        {
            if (ActiveVM.HasFlag(ExInteractiveConsoleFlag.RECENTLYINTERRUPTED))
            {
                ActiveVM.RemoveFlag(ExInteractiveConsoleFlag.RECENTLYINTERRUPTED);
                ExApi.SleepVM(ActiveVM, CANCELKEY_THREAD_TIMER);
            }
        }

        private static void ContinueDelayedInput(StringBuilder code)
        {
            ExApi.SleepVM(ActiveVM, CANCELKEY_THREAD_TIMER);

            while (ActiveVM.HasFlag(ExInteractiveConsoleFlag.CANCELEVENT))
            {
                ActiveVM.RemoveFlag(ExInteractiveConsoleFlag.CANCELEVENT);
                code.Append(GetInput());
                ExApi.SleepVM(ActiveVM, CANCELKEY_THREAD_TIMER);
            }
        }

        private static void Initialize()
        {
            ActiveVM = ExApi.Start(VM_STACK_SIZE, true); // Sanal makineyi başlat
            ActiveVM.ActiveThread = ActiveThread;
            ExApi.PushRootTable(ActiveVM);               // Global tabloyu ekle

            RegisterStdLibraries(ActiveVM);   // Standard kütüphaneler

            // Event handlers
            AddCancelEventHandler();

            // Title, Version info
            Console.Title = ExMat.ConsoleTitle;
            Console.ResetColor();
            ExApi.WriteVersion(ActiveVM);
        }

        private static void KeepConsoleUpAtEnd(string msg = "")
        {
            if (!string.IsNullOrWhiteSpace(msg))
            {
                Console.WriteLine("Error: " + msg);
            }
            Console.WriteLine("Press any key to close the console...");
            Console.ReadKey(false);
        }

        private static void CreateSimpleVMThread()
        {
            ActiveThread = new(() =>
            {
                Initialize();

                try
                {
                    ReturnValue = new(CompileString(ActiveVM, FileContents));
                }
                catch (ThreadInterruptedException) { } //lgtm [cs/empty-catch-block]
                catch (ExException exp)
                {
                    ActiveVM.AddToErrorMessage(exp.Message);
                    ExApi.WriteErrorMessages(ActiveVM, ExErrorType.INTERRUPT);
                }

                KeepConsoleUpAtEnd();
            });
        }

        private static void HandleUserInputFunction(ref StringBuilder code)
        {
            // En son işlenen kodda kullanıcıdan girdi aldıysa tekrardan okuma işlemi başlat
            if (ActiveVM.GotUserInput && ActiveVM.HasFlag(ExInteractiveConsoleFlag.EMPTYINPUT))
            {
                ActiveVM.RemoveFlag(ExInteractiveConsoleFlag.EMPTYINPUT);
                ActiveVM.GotUserInput = false;

                code = new(GetInput());
                ContinueDelayedInput(code);
            }
        }

        private static bool HandleLineCarryInput(ref StringBuilder code)
        {
            if (ActiveVM.HasFlag(ExInteractiveConsoleFlag.LINECARRY))  // Çok satırlı kod oku
            {
                code.Append(GetInput());
                ContinueDelayedInput(code);
            }
            else
            {
                ExApi.WriteIn(ActiveVM.InputCount); // Girdi numarası yaz

                code = new(GetInput());
                ContinueDelayedInput(code);

                HandleUserInputFunction(ref code);
            }

            if (ActiveVM.HasFlag(ExInteractiveConsoleFlag.LINECARRY))  // Çok satırlı ise okumaya devam et
            {
                Indent();
                code.Append("\r\n");
                return true;
            }

            return false;
        }

        private static void HandlePostVMExecution(int ret)
        {
            if (ActiveVM.HasFlag(ExInteractiveConsoleFlag.CANCELEVENT))
            {
                ActiveVM.RemoveFlag(ExInteractiveConsoleFlag.CANCELEVENT);
            }

            ExApi.CollectGarbage(); // Çöp toplayıcıyı çağır

            if (ActiveVM.ExitCalled)  // exit fonksiyonu çağırıldıysa bitir
            {
                ReturnValue = new(ret);
            }
        }

        private static string TrimCode(string code, bool includeCarry = true)
        {
            if (includeCarry)
            {
                return code.TrimEnd('\\', ' ', '\t');
            }
            else
            {
                return code.TrimEnd(' ', '\t');
            }
        }

        private static string TrimCode(StringBuilder code, bool includeCarry = true)
        {
            if (includeCarry)
            {
                return code.ToString().TrimEnd('\\', ' ', '\t');
            }
            else
            {
                return code.ToString().TrimEnd(' ', '\t');
            }
        }

        private static void CreateInteractiveVMThread()
        {
            ActiveThread = new(() =>
            {
                Initialize();

                int ret = -1;
                StringBuilder code = new();

                while (!ActiveVM.ExitCalled)    // Sürekli olarak girdi al
                {
                    try
                    {
                        DelayAfterInterruption();

                        if (HandleLineCarryInput(ref code))
                        {
                            continue;
                        }

                        ContinueDelayedInput(code);

                        ret = CompileString(ActiveVM, TrimCode(code));  // Derle ve işle

                    }
                    catch (ThreadInterruptedException) { } //lgtm [cs/empty-catch-block]
                    catch (ExException exp)
                    {
                        ActiveVM.AddToErrorMessage(exp.Message);
                        ExApi.WriteErrorMessages(ActiveVM, ExErrorType.INTERRUPT);
                    }
                    finally
                    {
                        HandlePostVMExecution(ret);
                    }
                }
            });
        }

        private static void LoopUntilThreadStops()
        {
            while (ActiveThread.IsAlive) { }
        }

        private static bool ReadFileContents(string path)
        {
            if (File.Exists(path))
            {
                FileContents = File.ReadAllText(path);
            }
            else
            {
                FileContents = string.Empty;
            }
            return !string.IsNullOrWhiteSpace(FileContents);
        }

        private static void SetupForInteractiveThread()
        {
            CreateInteractiveVMThread();
        }

        private static void SetupForFileExecuterThread()
        {
            CreateSimpleVMThread();
        }

        /// <summary>
        /// To compile and execute a script file use: <code>exmat.exe file_name.exmat</code>
        /// To start the interactive console, use: <code>exmat.exe</code>
        /// </summary>
        /// <param name="args">If any arguments given, first one is taken as file name</param>
        /// <returns>If a file was given and it didn't exists: <c>-1</c>
        /// <para>If given file exists, returns whatever is returned from <see cref="CompileString"/></para>
        /// <para>If interactive console is used, only returns when <c>exit</c> function is called</para></returns>
        private static int Main(string[] args)
        {
            // File
            if (args.Length >= 1)
            {
                if (!ReadFileContents(args[0])) // start exm.exe SCRIPT_PATH
                {
                    KeepConsoleUpAtEnd(string.Format("File '{0}' doesn't exist!", args[0]));
                    return -1;
                }

                SetupForFileExecuterThread();
            }
            // Interactive
            else
            {
                SetupForInteractiveThread();
            }

            ActiveThread.Start();

            LoopUntilThreadStops();

            return ReturnValue != null ? (int)ReturnValue.GetInt() : -1;
        }
    }
}
