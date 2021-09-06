using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ExMat.API;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.BaseLib
{
    public static class ExStdSys
    {
        public static ExFunctionStatus StdSysProcess(ExVM vm, int nargs)
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

        public static ExFunctionStatus StdSysPrintOut(ExVM vm, int nargs)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return vm.AddToErrorMessage($"'{nameof(StdSysPrintOut)}' function is only available for Windows operating systems");
            }

            if (!ExApi.ConvertAndGetString(vm, 1, nargs == 2 ? (int)vm.GetPositiveIntegerArgument(2, 1) : 2, out string output))
            {
                return ExFunctionStatus.ERROR;
            }

            StringBuilder echostr = new();
            foreach (string line in ("echo " + ExApi.EscapeCmdEchoString(output.Trim())).Split("\n", StringSplitOptions.RemoveEmptyEntries))
            {
                if (echostr.Length + line.Length > ExMat.ECHOLIMIT)
                {
                    echostr.Append("&&echo ... (TOO LONG TO ECHO)");
                    break;
                }
                else if (echostr.Length == 0)
                {
                    echostr.Append(line.TrimEnd());
                }
                else
                {
                    echostr.Append($"&&echo {line.TrimEnd()}");
                }
            }

            try
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/k \"{echostr}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception exp)
            {
                return vm.AddToErrorMessage("Error printing: " + exp.Message);
            }
            return vm.CleanReturn(nargs + 2, true);
        }

        public static ExFunctionStatus StdSysEnvInfo(ExVM vm, int nargs)
        {
            Dictionary<string, ExObject> info = new()
            {
                { "cmd_line", new(Environment.CommandLine) },
                { "curr_dir", new(Environment.CurrentDirectory) },
                { "sys_dir", new(Environment.SystemDirectory) },
                { "is_64bit", new(Environment.Is64BitOperatingSystem) },
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
                { "logic_drives", new(ExApi.ListObjFromStringArray(Environment.GetLogicalDrives())) }
            };
            return vm.CleanReturn(nargs + 2, info);
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

        public static ExFunctionStatus StdSysGetEnvVar(ExVM vm, int nargs)
        {
            EnvironmentVariableTarget target = nargs == 2
                ? GetEnviromentTargetFromString(vm.GetArgument(2).GetString())
                : EnvironmentVariableTarget.Process;

            try
            {
                string var = Environment.GetEnvironmentVariable(vm.GetArgument(1).GetString(), target);

                if (string.IsNullOrEmpty(var))
                {
                    return vm.CleanReturn(nargs + 2, new ExObject());
                }

                return vm.CleanReturn(nargs + 2, var);
            }
            catch (Exception err)
            {
                return vm.AddToErrorMessage("Error getting variable: " + err.Message);
            }
        }

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

        public static ExFunctionStatus StdSysEnvExit(ExVM vm, int nargs)
        {
            Environment.Exit((int)vm.GetArgument(1).GetInt());
            return vm.CleanReturn(nargs + 2, true);
        }

        public static ExFunctionStatus StdSysConsoleCanBeep(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, ExApi.CanBeep());
        }

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

        private static readonly List<ExRegFunc> _stdsysfuncs = new()
        {
            new()
            {
                Name = "process",
                Function = StdSysProcess,
                Parameters = new()
                {
                    new("filename", "s", "File to execute", new(".")),
                    new("arguments", "s", "Arguments for the file", new(string.Empty))
                },
                Returns = ExBaseType.BOOL,
                Description = "Execute a file with given arguments"
            },
            new()
            {
                Name = "open_dir",
                Function = StdSysOpendir,
                Parameters = new()
                {
                    new("directory", "s", "Directory to open", new(".")),
                    new("force_create", ".", "Wheter to create the directory if it doesn't exist", new(false))
                },
                Returns = ExBaseType.BOOL,
                Description = "Open a directory in the explorer"
            },
            new()
            {
                Name = "print_out",
                Function = StdSysPrintOut,
                Parameters = new()
                {
                    new("message", ".", "Message or object to print"),
                    new("depth", "n", "Depth of stringification for objects", new(2))
                },
                Description = "Print a message or an object to a new external terminal instead of the immediate terminal"
            },
            new()
            {
                Name = "env_info",
                Function = StdSysEnvInfo,
                Parameters = new(),
                Returns = ExBaseType.DICT,
                Description = "Get information about the runtime and the current enviroment in a dictionary"
            },
            new()
            {
                Name = "env_vars",
                Function = StdSysGetEnvVars,
                Parameters = new()
                {
                    new("target", "s", "Target enviroment: user, machine or process", new("process"))
                },
                Returns = ExBaseType.DICT,
                Description = "Get a dictionary of enviroment variables of given target."
            },
            new()
            {
                Name = "env_var",
                Function = StdSysGetEnvVar,
                Parameters = new()
                {
                    new("variable", "s", "Variable name"),
                    new("target", "s", "Target enviroment: user, machine or process", new("process"))
                },
                Returns = ExBaseType.STRING,
                Description = "Get the value of an enviroment variables of given target."
            },
            new()
            {
                Name = "set_env_var",
                Function = StdSysSetEnvVar,
                Parameters = new()
                {
                    new("variable", "s", "Variable name"),
                    new("new_value", "s", "New value"),
                    new("target", "s", "Target enviroment: user, machine or process", new("process"))
                },
                Returns = ExBaseType.STRING,
                Description = "Set the value of an enviroment variables of given target to given value."
            },
            new()
            {
                Name = "env_exit",
                Function = StdSysEnvExit,
                Parameters = new()
                {
                    new("exit_code", "r", "Exit code to return while exiting", new(0))
                },
                Returns = ExBaseType.BOOL,
                Description = "Exit from the current enviroment immediately. Works similarly but faster than the 'exit' function."
            },
            new()
            {
                Name = "can_beep",
                Function = StdSysConsoleCanBeep,
                Parameters = new(),
                Returns = ExBaseType.BOOL,
                Description = "Check if the console beep function is available."
            },
            new()
            {
                Name = "beep",
                Function = StdSysConsoleBeep,
                Parameters = new()
                {
                    new("frequency", "r", "Frequency to beep at in range [37, 32767]", new(800)),
                    new("duration", "r", "Beeping duration in miliseconds", new(200))
                },
                Returns = ExBaseType.BOOL,
                Description = "Beep the console for the given duration. This function is not async and stops the flow for the given 'duration'."
            },
            new()
            {
                Name = "beep_async",
                Function = StdSysConsoleBeepAsync,
                Parameters = new()
                {
                    new("frequency", "r", "Frequency to beep at in range [37, 32767]", new(800)),
                    new("duration", "r", "Beeping duration in miliseconds", new(200))
                },
                Returns = ExBaseType.BOOL,
                Description = "Beep the console for the given duration async. This function doesn't stop the flow."
            }
        };

        public static List<ExRegFunc> SysFuncs => _stdsysfuncs;

        public static bool RegisterStdSys(ExVM vm)
        {
            ExApi.RegisterNativeFunctions(vm, SysFuncs, ExStdLibType.SYSTEM);

            return true;
        }
    }
}
