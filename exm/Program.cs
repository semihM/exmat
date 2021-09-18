using System;
using System.Globalization;
using System.IO;
using System.Linq;
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
        /// Stack size of the virtual machines. Use higher values for potentially more recursive functions 
        /// </summary>
        public static int VM_STACK_SIZE = 2 << 14;

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

        /// <summary>
        /// Console flags
        /// </summary>
        private static int Flags;

        /// <summary>
        /// Expected names for the console flags
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, ExConsoleFlag> FlagNames = new()
        {
            { "--no-title", ExConsoleFlag.NOTITLE },
            { "--no-exit-hold", ExConsoleFlag.DONTKEEPOPEN }
        };

        /// <summary>
        /// Expected names for the console flags
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<System.Text.RegularExpressions.Regex, ExConsoleParameter> ConsoleParameters = new()
        {
            { new(@"\-stacksize:""?([\w\d\t ]+)""?"), a => int.TryParse(a, out int res) ? VM_STACK_SIZE = res : null }
        };

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
                        ExApi.SleepVM(ActiveVM, CANCELKEY_THREAD_TIMER);
                    }
                }
                ExApi.SleepVM(ActiveVM, CANCELKEY_THREAD_TIMER);
                ResetAfterCompilation(ActiveVM, ResetInStack);
            }
            else
            {
                ActiveVM.AddToErrorMessage("Input stream '{0}' was interrupted by control character: '{1}'", ActiveVM.InputCount, args.SpecialKey.ToString());
                ExApi.WriteErrorMessages(ActiveVM, ExErrorType.INTERRUPTINPUT);
                ActiveVM.PrintedToConsole = false; // Hack, allow writing stack top after interruption
            }
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

        private static void InitializeConsole()
        {
            // Title, Version info
            Console.ResetColor();

            if (!HasFlag(ExConsoleFlag.NOTITLE))
            {
                Console.Title = ExMat.ConsoleTitle;
                ExApi.WriteInfoString(ActiveVM);
            }
        }

        private static bool Initialize()
        {
            ActiveVM = ExApi.Start(VM_STACK_SIZE, true); // Sanal makineyi başlat
            ActiveVM.ActiveThread = ActiveThread;

            if (!ExApi.RegisterStdLibraries(ActiveVM)) // Standard kütüphaneler
            {
                return false;
            }

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
                Console.WriteLine("Error: " + msg);
            }

            if (!HasFlag(ExConsoleFlag.DONTKEEPOPEN))
            {
                Console.WriteLine("Press any key to close the console...");
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
            ActiveVM.Flags = 0;

            ExApi.CollectGarbage(); // Çöp toplayıcıyı çağır

            if (ActiveVM.ExitCalled)  // exit fonksiyonu çağırıldıysa bitir
            {
                ReturnValue = new(ret);
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
            ExApi.SleepVM(ActiveVM, CANCELKEY_THREAD_TIMER);

            ActiveVM.ForceThrow = false;
            ResetAfterCompilation(ActiveVM, ResetInStack);
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
                        ReturnValue = new(CompileString(ActiveVM, FileContents));
                    }
                    catch (ThreadInterruptedException)
                    {
                        HandleSleepInterruption();
                    }
                    catch (ExException exp)
                    {
                        ExApi.HandleException(exp, ActiveVM, ExApi.GetErrorTypeFromException(exp));
                    }
                    catch (Exception exp)
                    {
                        ExApi.HandleException(exp, ActiveVM);
                    }
                }
                KeepConsoleUpAtEnd();
            });
        }
        private static void CreateInteractiveVMThread()
        {
            ActiveThread = new(() =>
            {
                int ret = -1;
                StringBuilder code = new();

                if (!Initialize())
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
                        ExApi.HandleException(exp, ActiveVM, ExApi.GetErrorTypeFromException(exp));
                        break;
                    }
                    catch (Exception exp)
                    {
                        ExApi.HandleException(exp, ActiveVM);
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
        }

        private static bool ReadFileContents(string path)
        {
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
        /// Sets given console flag
        /// </summary>
        /// <param name="flag">Flag to set</param>
        private static void SetFlag(ExConsoleFlag flag)
        {
            Flags |= (int)flag;
        }

        private static bool SetFlagFromArgument(string arg)
        {
            if (FlagNames.ContainsKey(arg))
            {
                SetFlag(FlagNames[arg]);
                return true;
            }
            return false;
        }

        private static bool SetOptionFromArgument(string arg)
        {
            foreach (System.Collections.Generic.KeyValuePair<System.Text.RegularExpressions.Regex, ExConsoleParameter> pair in ConsoleParameters)
            {
                System.Text.RegularExpressions.Match m = pair.Key.Match(arg);
                if (!m.Success)
                {
                    continue;
                }

                return pair.Value(arg.Substring(m.Groups[1].Index, m.Groups[1].Length)) is not null;
            }
            return false;
        }

        private static void SetOptionsFromArguments(ref string[] args)
        {
            args = args.Where(a => !SetFlagFromArgument(a) && !SetOptionFromArgument(a)).ToArray();
        }

        /// <summary>
        /// To compile and execute a script file use:
        /// <code>    exmat.exe {file_name}.exmat [--no-title] [--no-exit-hold] [-stacksize:{integer}]</code>
        /// 
        /// To start the interactive console, use:
        /// <code>    exmat.exe [--no-title] [--no-exit-hold] [-stacksize:{integer}]</code>
        /// 
        /// </summary>
        /// <param name="args">If any arguments given, first one is taken as file name</param>
        /// <returns>If a file was given and it didn't exists: <c>-1</c>
        /// <para>If given file exists, returns whatever is returned from <see cref="CompileString"/></para>
        /// <para>If interactive console is used, only returns when <c>exit</c> function is called</para></returns>
        private static int Main(string[] args)
        {
            SetOptionsFromArguments(ref args);

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

            return ReturnValue != null ? (int)ReturnValue.GetInt() : -1;
        }
    }
}
