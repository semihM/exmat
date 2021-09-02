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

        public static ExFunctionStatus StdSysGetEnvVars(ExVM vm, int nargs)
        {
            Dictionary<string, ExObject> vars = new();
            foreach (DictionaryEntry d in Environment.GetEnvironmentVariables())
            {
                vars.Add(d.Key.ToString(), new(d.Value.ToString()));
            }

            return vm.CleanReturn(nargs + 2, vars);
        }

        public static ExFunctionStatus StdSysGetEnvVar(ExVM vm, int nargs)
        {
            string var = Environment.GetEnvironmentVariable(vm.GetArgument(1).GetString());
            if (string.IsNullOrEmpty(var))
            {
                return vm.CleanReturn(nargs + 2, new ExObject());
            }

            return vm.CleanReturn(nargs + 2, var);
        }

        public static ExFunctionStatus StdSysSetEnvVar(ExVM vm, int nargs)
        {
            string target = nargs == 3 ? vm.GetArgument(3).GetString() : "user";
            switch (target)
            {
                case "machine":
                    {
                        Environment.SetEnvironmentVariable(vm.GetArgument(1).GetString(), vm.GetArgument(2).GetString(), EnvironmentVariableTarget.Machine);
                        break;
                    }
                case "user":
                    {
                        Environment.SetEnvironmentVariable(vm.GetArgument(1).GetString(), vm.GetArgument(2).GetString(), EnvironmentVariableTarget.User);
                        break;
                    }
                case "process":
                    {
                        Environment.SetEnvironmentVariable(vm.GetArgument(1).GetString(), vm.GetArgument(2).GetString(), EnvironmentVariableTarget.Process);
                        break;
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, false);
                    }
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
                nParameterChecks = -1,
                ParameterMask = ".ss",
                DefaultValues = new()
                {
                    { 1, new(".") },
                    { 2, new(string.Empty) }
                }
            },
            new()
            {
                Name = "open_dir",
                Function = StdSysOpendir,
                nParameterChecks = -2,
                ParameterMask = ".sb",
                DefaultValues = new()
                {
                    { 2, new(false) }
                }
            },
            new()
            {
                Name = "print_out",
                Function = StdSysPrintOut,
                nParameterChecks = -2,
                ParameterMask = "..n",
                DefaultValues = new()
                {
                    { 2, new(2) }
                }
            },
            new()
            {
                Name = "env_info",
                Function = StdSysEnvInfo,
                nParameterChecks = 1,
                ParameterMask = "."
            },
            new()
            {
                Name = "env_vars",
                Function = StdSysGetEnvVars,
                nParameterChecks = 1,
                ParameterMask = "."
            },
            new()
            {
                Name = "env_var",
                Function = StdSysGetEnvVar,
                nParameterChecks = 2,
                ParameterMask = ".s"
            },
            new()
            {
                Name = "set_env_var",
                Function = StdSysSetEnvVar,
                nParameterChecks = -3,
                ParameterMask = ".sss",
                DefaultValues = new()
                {
                    { 3, new("user") }
                }
            },
            new()
            {
                Name = "env_exit",
                Function = StdSysEnvExit,
                nParameterChecks = -1,
                ParameterMask = ".i|f",
                DefaultValues = new()
                {
                    { 1, new(0) }
                }
            },
            new()
            {
                Name = "can_beep",
                Function = StdSysConsoleCanBeep,
                nParameterChecks = 1,
                ParameterMask = "."
            },
            new()
            {
                Name = "beep",
                Function = StdSysConsoleBeep,
                nParameterChecks = -1,
                ParameterMask = ".i|fi|f",
                DefaultValues = new()
                {
                    { 1, new(800) },
                    { 2, new(200) }
                }
            },
            new()
            {
                Name = "beep_async",
                Function = StdSysConsoleBeepAsync,
                nParameterChecks = -1,
                ParameterMask = ".i|fi|f",
                DefaultValues = new()
                {
                    { 1, new(800) },
                    { 2, new(200) }
                }
            }
        };

        public static List<ExRegFunc> SysFuncs => _stdsysfuncs;

        public static bool RegisterStdSys(ExVM vm)
        {
            ExApi.RegisterNativeFunctions(vm, SysFuncs);

            return true;
        }
    }
}
