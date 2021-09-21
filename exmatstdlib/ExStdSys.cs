using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using ExMat.API;
using ExMat.Attributes;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.StdLib
{
    [ExStdLibBase(ExStdLibType.SYSTEM)]
    [ExStdLibName("system")]
    [ExStdLibRegister(nameof(Registery))]
    public static class ExStdSys
    {
        #region UTILITY
        private static string SafeGetStringProcessInfo(ref Process p, ExProcessInfo type)
        {
            try
            {
                switch (type)
                {
                    case ExProcessInfo.DATE:
                        return string.Format(CultureInfo.CurrentCulture, "{0} {1}", p.StartTime.ToShortTimeString(), p.StartTime.ToShortDateString());
                    case ExProcessInfo.MODULE:
                        return p.MainModule.FileName;
                    case ExProcessInfo.ARGS:
                        return p.StartInfo.Arguments;
                    default:
                        return string.Empty;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static Process GetProcessFromArgument(ExObject arg)
        {
            try
            {
                return arg == null
                    ? Process.GetProcessById(Environment.ProcessId, Environment.MachineName)
                    : Process.GetProcessById((int)arg.GetInt(), Environment.MachineName);
            }
            catch
            {
                return null;
            }
        }

        private static Dictionary<string, ExObject> GetProcessData(Process p)
        {
            if (p == null)
            {
                return null;
            }

            Dictionary<string, ExObject> dict;
            dict = new()
            {
                { "name", new(p.ProcessName) },
                { "id", new(p.Id) },
                { "priority", new(p.BasePriority) },
                { "session", new(p.SessionId) },
                { "window_title", new(p.MainWindowTitle) },
                { "is_responding", new(p.Responding) }
            };

            dict.Add("start_date", new(SafeGetStringProcessInfo(ref p, ExProcessInfo.DATE)));
            dict.Add("start_module", new(SafeGetStringProcessInfo(ref p, ExProcessInfo.MODULE)));
            dict.Add("start_arguments", new(SafeGetStringProcessInfo(ref p, ExProcessInfo.ARGS)));

            return dict;
        }

        private static EnvironmentVariableTarget GetEnviromentTargetFromString(string target)
        {
            switch (target)
            {
                case "user":
                    {
                        return EnvironmentVariableTarget.User;
                    }
                case "machine":
                    {
                        return EnvironmentVariableTarget.Machine;
                    }
                default:
                    {
                        return EnvironmentVariableTarget.Process;
                    }
            }
        }
        #endregion

        #region SYSTEM FUNCTIONS
        [ExNativeFuncBase("get_process", ExBaseType.ARRAY | ExBaseType.DICT, "Get information about a process or multiple processes active on the machine, searching by process id or name. Give null to get the current process. If there are multiple processes with the same name, a list is returned.")]
        [ExNativeParamBase(1, "id_or_name", "s|i", "Process ID or name", def: null)]
        public static ExFunctionStatus StdSysGetProcess(ExVM vm, int nargs)
        {
            ExObject arg1 = nargs == 1 ? vm.GetArgument(1) : new();

            try
            {
                if (arg1.Type == ExObjType.STRING)
                {
                    string name = arg1.GetString();
                    List<ExObject> lis = new();

                    foreach (Process p in Process.GetProcessesByName(name, Environment.MachineName))
                    {
                        lis.Add(new(GetProcessData(p)));
                    }

                    return vm.CleanReturn(nargs + 2, lis);
                }
                else
                {
                    return arg1.Type == ExObjType.INTEGER
                        ? vm.CleanReturn(nargs + 2, GetProcessData(GetProcessFromArgument(arg1)))
                        : vm.CleanReturn(nargs + 2, GetProcessData(GetProcessFromArgument(null)));
                }
            }
            catch (Exception err)
            {
                return vm.AddToErrorMessage("Error getting process: " + err.Message);
            }
        }

        [ExNativeFuncBase("get_processes", ExBaseType.ARRAY, "Get a list of dictionaries containing information about processes currently active on the machine.")]
        public static ExFunctionStatus StdSysGetProcesses(ExVM vm, int nargs)
        {
            try
            {
                List<ExObject> lis = new();

                foreach (Process p in Process.GetProcesses(Environment.MachineName))
                {
                    lis.Add(new(GetProcessData(p)));
                }

                return vm.CleanReturn(nargs + 2, lis);
            }
            catch (Exception err)
            {
                return vm.AddToErrorMessage("Error getting processes: " + err.Message);
            }
        }

        [ExNativeFuncBase("start_process", ExBaseType.BOOL, "Execute a file with given arguments")]
        [ExNativeParamBase(1, "filename", "s", "File to execute", ".")]
        [ExNativeParamBase(2, "arguments", "s", "Arguments for the file", def: "")]
        public static ExFunctionStatus StdSysProcessStart(ExVM vm, int nargs)
        {
            ProcessStartInfo psi = new()
            {
                FileName = vm.GetArgument(1).GetString(),
                Arguments = vm.GetArgument(2).GetString(),
                UseShellExecute = true
            };

            try
            {
                Process.Start(psi);
            }
            catch (Exception exp)
            {
                return vm.AddToErrorMessage("Error starting process: " + exp.Message);
            }
            return vm.CleanReturn(nargs + 2, true);
        }

        [ExNativeFuncBase("stop_process", ExBaseType.BOOL, "Stop a process immediately")]
        [ExNativeParamBase(1, "id", "i|e", "Process ID or null to kill current process", def: null)]
        public static ExFunctionStatus StdSysProcessStop(ExVM vm, int nargs)
        {
            using Process p = GetProcessFromArgument(nargs == 1 ? vm.GetArgument(1) : null);

            try
            {
                if (p != null)
                {
                    p.Kill();
                    return vm.CleanReturn(nargs + 2, true);
                }
            }
            catch (Exception exp)
            {
                return vm.AddToErrorMessage("Error stopping process: " + exp.Message);
            }
            return vm.CleanReturn(nargs + 2, false);
        }

        [ExNativeFuncBase("open_dir", ExBaseType.BOOL, "Open a directory in the explorer")]
        [ExNativeParamBase(1, "directory", "s", "Directory to open", ".")]
        [ExNativeParamBase(2, "force_create", ".", "Wheter to create the directory if it doesn't exist", false)]
        public static ExFunctionStatus StdSysOpendir(ExVM vm, int nargs)
        {
            string dir = vm.GetArgument(1).GetString();

            try
            {
                if (!Directory.Exists(dir))
                {
                    if (nargs == 2 && vm.GetArgument(2).GetBool())
                    {
                        Directory.CreateDirectory(dir);
                        Directory.SetCurrentDirectory(dir);
                        Process.Start(new ProcessStartInfo()
                        {
                            FileName = dir,
                            UseShellExecute = true
                        });

                        return vm.CleanReturn(nargs + 2, true);
                    }

                    return vm.CleanReturn(nargs + 2, false);
                }

                Directory.SetCurrentDirectory(dir);
                Process.Start(new ProcessStartInfo()
                {
                    FileName = dir,
                    UseShellExecute = true
                });

                return vm.CleanReturn(nargs + 2, Directory.GetCurrentDirectory());
            }
            catch (Exception e)
            {
                return vm.AddToErrorMessage("Error starting explorer: " + e.Message);
            }
        }

        [ExNativeFuncBase("print_out", "Print a message or an object to a new external terminal instead of the immediate terminal. Returns true on success.")]
        [ExNativeParamBase(1, "message", ".", "Message or object to print")]
        [ExNativeParamBase(2, "console_title", "s", "Custom external console title", ExMat.ConsoleTitle)]
        [ExNativeParamBase(3, "depth", "n", "Depth of stringification for objects", 2)]
        public static ExFunctionStatus StdSysPrintOut(ExVM vm, int nargs)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return vm.AddToErrorMessage($"'{nameof(StdSysPrintOut)}' function is only available for Windows operating systems");
            }

            int depth = nargs == 3 ? (int)vm.GetPositiveIntegerArgument(3, 1) : 2;

            if (!ExApi.ConvertAndGetString(vm, 1, depth, out string output))
            {
                return ExFunctionStatus.ERROR;
            }
            ExFunctionStatus stat;

            using Process p = new();

            string fname = vm.StartDirectory + "\\" + ExApi.RandomString(48);
            File.WriteAllText(fname,
                string.Format(CultureInfo.CurrentCulture, "// This file is a temporary file for created by 'print_out'\nprint(\"{0}\");exit()", ExApi.Escape(output)));

            File.SetAttributes(fname, FileAttributes.ReadOnly | FileAttributes.Hidden);

            try
            {
                p.StartInfo.FileName = Process.GetCurrentProcess().MainModule.FileName;
                p.StartInfo.Arguments = $"{fname} --no-inout --no-info --delete-onpost -stacksize:16 -title:\"{(nargs >= 2 ? vm.GetArgument(2).GetString() : ExMat.ConsoleTitle)}\"";
                p.StartInfo.UseShellExecute = true;
                p.Start();

                stat = vm.CleanReturn(nargs + 2, true);
            }
            catch (Exception exp)
            {
                stat = vm.AddToErrorMessage("Error printing: " + exp.Message);
            }
            finally
            {
                FileInfo fileInfo = new(fname);
                fileInfo.IsReadOnly = false;

                System.Threading.Thread.Sleep(250); // To prevent spam in infinite loops
            }
            return stat;
        }

        [ExNativeFuncBase("env_info", ExBaseType.DICT, "Get information about the runtime and the current enviroment in a dictionary")]
        public static ExFunctionStatus StdSysEnvInfo(ExVM vm, int nargs)
        {
            Dictionary<string, ExObject> info = new()
            {
                { "cmd_line", new(Environment.CommandLine) },
                { "curr_dir", new(Environment.CurrentDirectory) },
                { "sys_dir", new(Environment.SystemDirectory) },
                { "is_64bit", new(Environment.Is64BitOperatingSystem) },
                { "processor_count", new(Environment.ProcessorCount) },
                { "machine_name", new(Environment.MachineName) },
                { "user_name", new(Environment.UserName) },
                { "user_domain", new(Environment.UserDomainName) },
                { "os", new(Environment.OSVersion.VersionString) },
                { "os_description", new(RuntimeInformation.OSDescription) },
                { "os_architecture", new(RuntimeInformation.OSArchitecture.ToString()) },
                { "os_version", new(Environment.OSVersion.Version.ToString()) },
                { "platform", new(RuntimeInformation.RuntimeIdentifier) },
                { "framework", new(RuntimeInformation.FrameworkDescription) },
                { "framework_version", new(Environment.Version.ToString()) },
                { "clr_dir", new(RuntimeEnvironment.GetRuntimeDirectory()) },
                { "clr_version", new(RuntimeEnvironment.GetSystemVersion()) },
                { "logic_drives", new(ExApi.ListObjFromStringArray(Environment.GetLogicalDrives())) },
                { "process_id", new(Environment.ProcessId) }
            };
            return vm.CleanReturn(nargs + 2, info);
        }

        [ExNativeFuncBase("env_vars", ExBaseType.DICT, "Get a dictionary of enviroment variables of given target.")]
        [ExNativeParamBase(1, "target", "s", "Target enviroment: user, machine or process", "process")]
        public static ExFunctionStatus StdSysGetEnvVars(ExVM vm, int nargs)
        {
            Dictionary<string, ExObject> vars = new();

            EnvironmentVariableTarget target = nargs == 1
                ? GetEnviromentTargetFromString(vm.GetArgument(1).GetString())
                : EnvironmentVariableTarget.Process;

            try
            {
                foreach (DictionaryEntry d in Environment.GetEnvironmentVariables(target))
                {
                    vars.Add(d.Key.ToString(), new(d.Value.ToString()));
                }
                return vm.CleanReturn(nargs + 2, vars);
            }
            catch (Exception err)
            {
                return vm.AddToErrorMessage("Error getting variables: " + err.Message);
            }
        }

        [ExNativeFuncBase("env_var", ExBaseType.STRING, "Get the value of an enviroment variables of given target.")]
        [ExNativeParamBase(1, "variable", "s", "Variable name")]
        [ExNativeParamBase(2, "target", "s", "Target enviroment: user, machine or process", "process")]
        public static ExFunctionStatus StdSysGetEnvVar(ExVM vm, int nargs)
        {
            EnvironmentVariableTarget target = nargs == 2
                ? GetEnviromentTargetFromString(vm.GetArgument(2).GetString())
                : EnvironmentVariableTarget.Process;

            try
            {
                string var = Environment.GetEnvironmentVariable(vm.GetArgument(1).GetString(), target);

                return string.IsNullOrEmpty(var) ? vm.CleanReturn(nargs + 2, new ExObject()) : vm.CleanReturn(nargs + 2, var);
            }
            catch (Exception err)
            {
                return vm.AddToErrorMessage("Error getting variable: " + err.Message);
            }
        }

        [ExNativeFuncBase("set_env_var", ExBaseType.STRING, "Set the value of an enviroment variables of given target to given value.")]
        [ExNativeParamBase(1, "variable", "s", "Variable name")]
        [ExNativeParamBase(2, "new_value", "s", "New value")]
        [ExNativeParamBase(3, "target", "s", "Target enviroment: user, machine or process", "process")]
        public static ExFunctionStatus StdSysSetEnvVar(ExVM vm, int nargs)
        {
            EnvironmentVariableTarget target = nargs == 3
                ? GetEnviromentTargetFromString(vm.GetArgument(3).GetString())
                : EnvironmentVariableTarget.Process;

            try
            {
                Environment.SetEnvironmentVariable(vm.GetArgument(1).GetString(), vm.GetArgument(2).GetString(), target);
            }
            catch (Exception err)
            {
                return vm.AddToErrorMessage("Error setting variable: " + err.Message);
            }

            return vm.CleanReturn(nargs + 2, true);
        }

        [ExNativeFuncBase("env_exit", ExBaseType.BOOL, "Exit from the current enviroment immediately. Works similarly but faster than the 'exit' function.")]
        [ExNativeParamBase(1, "exit_code", "r", "Exit code to return while exiting", 0)]
        public static ExFunctionStatus StdSysEnvExit(ExVM vm, int nargs)
        {
            Environment.Exit((int)vm.GetArgument(1).GetInt());
            return vm.CleanReturn(nargs + 2, true);
        }

        [ExNativeFuncBase("can_beep", ExBaseType.BOOL, "Check if the console beep function is available.")]
        public static ExFunctionStatus StdSysConsoleCanBeep(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, ExApi.CanBeep());
        }

        [ExNativeFuncBase("beep", ExBaseType.BOOL, "Beep the console for the given duration. This function is not async and stops the flow for the given 'duration'.")]
        [ExNativeParamBase(1, "frequency", "r", "Frequency to beep at in range [37, 32767]", 800)]
        [ExNativeParamBase(2, "duration", "r", "Beeping duration in miliseconds", 200)]
        public static ExFunctionStatus StdSysConsoleBeep(ExVM vm, int nargs)
        {
            if (nargs != 0 && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return vm.AddToErrorMessage($"'{nameof(StdSysConsoleBeep)}(int, int)' function is only available for Windows operating systems");
            }

            switch (nargs)
            {
                case 2:
                    {
                        return vm.CleanReturn(nargs + 2, ExApi.Beep((int)vm.GetPositiveRangedIntegerArgument(1, 37, 32767), (int)vm.GetPositiveRangedIntegerArgument(2, 50, int.MaxValue)));
                    }
                case 1:
                    {
                        return vm.CleanReturn(nargs + 2, ExApi.Beep((int)vm.GetPositiveRangedIntegerArgument(1, 37, 32767), 200));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, ExApi.Beep());
                    }
            }
        }

        [ExNativeFuncBase("beep_async", ExBaseType.BOOL, "Beep the console for the given duration async. This function doesn't stop the flow.")]
        [ExNativeParamBase(1, "frequency", "r", "Frequency to beep at in range [37, 32767]", 800)]
        [ExNativeParamBase(2, "duration", "r", "Beeping duration in miliseconds", 200)]
        public static ExFunctionStatus StdSysConsoleBeepAsync(ExVM vm, int nargs)
        {
            if (nargs != 0 && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return vm.AddToErrorMessage($"'{nameof(StdSysConsoleBeepAsync)}(int, int)' function is only available for Windows operating systems");
            }

            switch (nargs)
            {
                case 2:
                    {
                        return vm.CleanReturn(nargs + 2, ExApi.BeepAsync((int)vm.GetPositiveRangedIntegerArgument(1, 37, 32767), (int)vm.GetPositiveRangedIntegerArgument(2, 50, int.MaxValue)));
                    }
                case 1:
                    {
                        return vm.CleanReturn(nargs + 2, ExApi.BeepAsync((int)vm.GetPositiveRangedIntegerArgument(1, 37, 32767), 200));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, ExApi.BeepAsync());
                    }
            }
        }

        #endregion

        // MAIN
        public static ExMat.StdLibRegistery Registery => (ExVM vm) =>
        {
            return true;
        };
    }
}
