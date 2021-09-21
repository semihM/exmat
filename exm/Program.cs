using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using ExMat.API;
using ExMat.Exceptions;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat
{
    /// <summary>
    /// Refer to docs inside this class for more information!
    /// </summary>
    internal static class Program
    {
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
        private static int ReturnValue = -1;

        /// <summary>
        /// File contents path
        /// </summary>
        private static string FilePath;

        /// <summary>
        /// File contents read from console call
        /// </summary>
        private static string FileContents;

        /// <summary>
        /// Amount of objects to nullify after VM completes executing
        /// </summary>
        private static int ResetInStack;

        /// <summary>
        /// Console flags
        /// </summary>
        private static int Flags;

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

            ret = ExApi.CompileSource(vm, code) ? ExApi.CallTop(vm) : ExApi.WriteErrorMessages(vm, ExErrorType.COMPILE);

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
            if (count < 0)
            {
                count = 0;
            }
            for (int idx = vm.StackSize - 1; idx >= count; idx--)
            {
                vm.Stack[idx].Nullify();
            }
            vm.StackBase = count;
            vm.StackTop = count;
        }

        /// <summary>
        /// Reads user input, cleans it up, sets console flags
        /// </summary>
        /// <returns>Cleaned user input</returns>
        private static string GetInput()
        {
            string code = null;
            if (ActiveVM.HasLineReader)
            {
                code = ActiveVM.LineReader();
            }
            else
            {
                Console.WriteLine("No input line reader found!");
                Console.ReadKey(false);
            }

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
                ActiveVM.AddToErrorMessage("Output '{0}' was interrupted by control character: '{1}'", ActiveVM.InputCount, args.SpecialKey.ToString());

                if (ActiveVM.IsSleeping)
                {
                    ActiveVM.SetFlag(ExInteractiveConsoleFlag.INTERRUPTEDINSLEEP);
                    ActiveVM.ActiveThread.Interrupt();
                    ActiveVM.IsSleeping = false;
                    while (ActiveVM.ActiveThread.ThreadState != ThreadState.Running)
                    {
                        ExApi.SleepVM(ActiveVM, ExMat.CANCELKEYTHREADTIMER);
                    }
                }
                ExApi.SleepVM(ActiveVM, ExMat.CANCELKEYTHREADTIMER);
                ResetAfterCompilation(ActiveVM, ResetInStack);
            }
            else
            {
                ActiveVM.AddToErrorMessage("Input stream '{0}' was interrupted by control character: '{1}'", ActiveVM.InputCount, args.SpecialKey.ToString());
                ExApi.WriteErrorMessages(ActiveVM, ExErrorType.INTERRUPTINPUT);
                ActiveVM.PrintedToConsole = false; // Hack, allow writing stack top after interruption
            }
        }

        private static ExErrorType GetErrorTypeFromException(ExException exp)
        {
            switch (exp.Type)
            {
                case ExExceptionType.BASE:
                    {
                        return ExErrorType.INTERNAL;
                    }
                case ExExceptionType.RUNTIME:
                    {
                        return ExErrorType.RUNTIME;
                    }
                case ExExceptionType.COMPILER:
                    {
                        return ExErrorType.COMPILE;
                    }
                default:
                    {
                        return ExErrorType.INTERNAL;
                    }
            }
        }

        private static void Indent(int n = 1)
        {
            ActiveVM.Printer(new string('\t', n));
        }

        private static void DelayAfterInterruption()
        {
            if (ActiveVM.HasFlag(ExInteractiveConsoleFlag.RECENTLYINTERRUPTED))
            {
                ActiveVM.RemoveFlag(ExInteractiveConsoleFlag.RECENTLYINTERRUPTED);
                ExApi.SleepVM(ActiveVM, ExMat.CANCELKEYTHREADTIMER);
            }
        }

        private static void ContinueDelayedInput(StringBuilder code)
        {
            ExApi.SleepVM(ActiveVM, ExMat.CANCELKEYTHREADTIMER);

            while (ActiveVM.HasFlag(ExInteractiveConsoleFlag.CANCELEVENT))
            {
                ActiveVM.RemoveFlag(ExInteractiveConsoleFlag.CANCELEVENT);
                code.Append(GetInput());
                ExApi.SleepVM(ActiveVM, ExMat.CANCELKEYTHREADTIMER);
            }
        }

        private static void InitializeConsole()
        {
            Console.ResetColor();

            if (!HasFlag(ExConsoleFlag.NOTITLE))
            {
                ExApi.ApplyPreferredConsoleTitle();
            }

            if (!HasFlag(ExConsoleFlag.NOINFO))
            {
                ExApi.WriteInfoString(ActiveVM);
            }
        }

        private static bool Initialize(bool interactive = false)
        {
            ActiveVM = ExApi.Start(interactive); // Sanal makineyi başlat
            ActiveVM.ActiveThread = ActiveThread;

            if (!ExApi.RegisterStdLibraries(ActiveVM)) // Standard kütüphaneler
            {
                return false;
            }

            if (HasFlag(ExConsoleFlag.NOINOUT))
            {
                ActiveVM.SetFlag(ExInteractiveConsoleFlag.DONTPRINTOUTPREFIX);
            }

            // Printer
            ActiveVM.Printer = Console.Write;

            // Reader
            ActiveVM.IntKeyReader = Console.Read;
            ActiveVM.KeyReader = Console.ReadKey;
            ActiveVM.LineReader = Console.ReadLine;

            // Event handlers
            AddCancelEventHandler();

            // Setup the console
            InitializeConsole();

            return true;
        }

        private static void KeepConsoleUpAtEnd(string msg = "")
        {
            Console.WriteLine(string.Empty);

            if (!string.IsNullOrWhiteSpace(msg))
            {
                Console.WriteLine(msg);
            }

            if (!HasFlag(ExConsoleFlag.DONTKEEPOPEN))
            {
                Console.WriteLine("Press any key to exit the virtual machine...");
                Console.ReadKey(false);
            }
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
                if (!HasFlag(ExConsoleFlag.NOINOUT))
                {
                    ExApi.WriteIn(ActiveVM); // Girdi numarası yaz
                }

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
            ActiveVM.Flags = 0;

            if (HasFlag(ExConsoleFlag.NOINOUT))
            {
                ActiveVM.SetFlag(ExInteractiveConsoleFlag.DONTPRINTOUTPREFIX);
            }

            ExApi.CollectGarbage(); // Çöp toplayıcıyı çağır

            if (ActiveVM.ExitCalled)  // exit fonksiyonu çağırıldıysa bitir
            {
                ReturnValue = ret;
            }

            ActiveVM.nNativeCalls = 0;
            ActiveVM.nMetaCalls = 0;
        }

        private static string TrimCode(string code, bool includeCarry = true)
        {
            return includeCarry ? code.TrimEnd('\\', ' ', '\t') : code.TrimEnd(' ', '\t');
        }

        private static string TrimCode(StringBuilder code, bool includeCarry = true)
        {
            return TrimCode(code.ToString(), includeCarry);
        }

        private static void HandleSleepInterruption()
        {
            if (ActiveVM.HasFlag(ExInteractiveConsoleFlag.INTERRUPTEDINSLEEP))
            {
                ExApi.WriteErrorMessages(ActiveVM, ExErrorType.INTERRUPT);
                ActiveVM.RemoveFlag(ExInteractiveConsoleFlag.INTERRUPTEDINSLEEP);
            }
            ExApi.SleepVM(ActiveVM, ExMat.CANCELKEYTHREADTIMER);

            ActiveVM.ForceThrow = false;
            ResetAfterCompilation(ActiveVM, ResetInStack);
        }

        public static void HandleException(Exception exp, ExVM vm)
        {
            vm.AddToErrorMessage(exp.Message);
            vm.AddToErrorMessage(exp.StackTrace);
            ExApi.WriteErrorMessages(vm, ExErrorType.INTERNAL);
        }

        public static void HandleException(ExException exp, ExVM vm, ExErrorType typeOverride = ExErrorType.INTERNAL)
        {
            vm.AddToErrorMessage(exp.Message);
            vm.AddToErrorMessage(exp.StackTrace);
            ExApi.WriteErrorMessages(vm, typeOverride);
        }

        private static void CheckFileDelete()
        {
            if (HasFlag(ExConsoleFlag.DELETEONPOST))
            {
                File.Delete(FilePath);
            }

            KeepConsoleUpAtEnd();
        }

        private static void CreateSimpleVMThread()
        {
            ActiveThread = new(() =>
            {
                if (!Initialize())
                {
                    ExApi.WriteErrorMessages(ActiveVM, ExErrorType.INTERNAL);
                }
                else
                {
                    try
                    {
                        ReturnValue = CompileString(ActiveVM, FileContents);
                    }
                    catch (ThreadInterruptedException)
                    {
                        HandleSleepInterruption();
                    }
                    catch (ExException exp)
                    {
                        HandleException(exp, ActiveVM, GetErrorTypeFromException(exp));
                    }
                    catch (Exception exp)
                    {
                        HandleException(exp, ActiveVM);
                    }
                }

                CheckFileDelete();
            });
        }
        private static void CreateInteractiveVMThread()
        {
            ActiveThread = new(() =>
            {
                int ret = -1;
                StringBuilder code = new();

                if (!Initialize(true))
                {
                    ExApi.WriteErrorMessages(ActiveVM, ExErrorType.INTERNAL);
                    return;
                }

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
                    catch (ThreadInterruptedException)
                    {
                        HandleSleepInterruption();
                    }
                    catch (ExException exp)
                    {
                        HandleException(exp, ActiveVM, GetErrorTypeFromException(exp));
                        break;
                    }
                    catch (Exception exp)
                    {
                        HandleException(exp, ActiveVM);
                        break;
                    }
                    finally
                    {
                        if (!ActiveVM.HasFlag(ExInteractiveConsoleFlag.LINECARRY))
                        {
                            HandlePostVMExecution(ret);
                        }
                    }
                }
                KeepConsoleUpAtEnd();
            });
        }

        private static void LoopUntilThreadStops()
        {
            ActiveThread.Start();

            while (ActiveThread.IsAlive) { }

            ExDisposer.DisposeObject(ref ActiveVM);
            ActiveThread = null;
            FilePath = null;
            FileContents = null;

        }

        private static bool ReadFileContents(string path)
        {
            FilePath = path;
            FileContents = File.Exists(path) ? File.ReadAllText(path) : string.Empty;
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
        /// Checks if console has the given flag
        /// </summary>
        /// <param name="flag">Flag to check</param>
        private static bool HasFlag(ExConsoleFlag flag)
        {
            return ((int)flag & Flags) != 0;
        }

        /// <summary>
        /// To compile and execute a script file use:
        /// <code>    exm {file_name}.exmat [flag] [parameter:"argument"]</code>
        /// 
        /// To start the interactive console, use:
        /// <code>    exm [flag] [parameter:"argument"]</code>
        /// 
        /// To get help:
        /// <code>    exm --help</code>
        /// 
        /// <para>Available flags: <see cref="FlagNames"/></para>
        /// <para>Available parameters: <see cref="ConsoleParameters"/></para>
        /// </summary>
        /// <param name="args">If any arguments given, first one is taken as file name</param>
        /// <returns>If a file was given and it didn't exists: <c>-1</c>
        /// <para>If given file exists, returns whatever is returned from <see cref="CompileString"/></para>
        /// <para>If interactive console is used, only returns when <c>exit</c> function is called</para></returns>
        private static int Main(string[] args)
        {
            ExApi.SetOptionsFromArguments(ref args, ref Flags);

            if (HasFlag(ExConsoleFlag.HELP))
            {
                Console.WriteLine("To compile and execute a script file use the format:\n\texm {file_name}.exmat [flag] [parameter:\"argument\"]"
                                + "\n\nTo start the interactive console use the format:\n\texm [flag] [parameter:\"argument\"]"
                                + "\n\nAvailable flags (--flag):\n\t"
                                + string.Join("\n\t", ExApi.GetConsoleFlagHelperValues())
                                + "\n\nAvailable parameters (-parameter:\"argument\"):\n\t"
                                + string.Join("\n\t", ExApi.GetConsoleParameterHelperValues()));

                KeepConsoleUpAtEnd();
                return 0;
            }

            // File
            if (args.Length >= 1)
            {
                if (!ReadFileContents(args[0]))
                {
                    KeepConsoleUpAtEnd(string.Format(CultureInfo.CurrentCulture, "File '{0}' doesn't exist!", args[0]));
                    return -1;
                }

                SetupForFileExecuterThread();
            }
            // Interactive
            else
            {
                SetupForInteractiveThread();
            }

            LoopUntilThreadStops();

            return ReturnValue;
        }
    }
}
