using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ExMat.API;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.BaseLib
{
    public static class ExStdIO
    {
        public static Encoding DecideEncodingFromString(string enc)
        {
            Encoding e;
            if (string.IsNullOrEmpty(enc))
            {
                e = Encoding.Default;
            }
            else
            {
                switch (enc.ToLower())
                {
                    case "utf-8":
                    case "utf8":
                        {
                            e = Encoding.UTF8;
                            break;
                        }
                    case "utf32":
                    case "utf-32":
                        {
                            e = Encoding.UTF32;
                            break;
                        }
                    case "latin":
                    case "latin1":
                        {
                            e = Encoding.Latin1;
                            break;
                        }
                    case "be-unicode":
                        {
                            e = Encoding.BigEndianUnicode;
                            break;
                        }
                    case "unicode":
                        {
                            e = Encoding.Unicode;
                            break;
                        }
                    case "ascii":
                        {
                            e = Encoding.ASCII;
                            break;
                        }
                    default:
                        {
                            e = Encoding.Default;
                            break;
                        }
                }
            }

            return e;
        }

        public static int IoWritefile(ExVM vm, int nargs)
        {
            string i = ExAPI.GetFromStack(vm, 2).GetString();
            ExObject c = null;
            vm.ToString(ExAPI.GetFromStack(vm, 3), ref c);
            string code = c.GetString();

            string enc = null;
            if (nargs == 3)
            {
                enc = ExAPI.GetFromStack(vm, 4).GetString();
            }

            Encoding e = DecideEncodingFromString(enc);

            vm.Pop(nargs + 2);
            File.WriteAllText(i, code, e);
            return 0;
        }
        public static int IoWritefilelines(ExVM vm, int nargs)
        {
            string i = ExAPI.GetFromStack(vm, 2).GetString();
            ExObject lis = ExAPI.GetFromStack(vm, 3);

            string enc = null;
            if (nargs == 3)
            {
                enc = ExAPI.GetFromStack(vm, 4).GetString();
            }

            Encoding e = DecideEncodingFromString(enc);

            int n = lis.Value.l_List.Count;

            string[] lines = new string[n];

            for (int l = 0; l < n; l++)
            {
                ExObject line = new();
                vm.ToString(lis.Value.l_List[l], ref line);
                lines[l] = line.GetString();
            }

            vm.Pop(nargs + 2);
            File.WriteAllLines(i, lines, e);
            return 0;
        }
        public static int IoWritefilebytes(ExVM vm, int nargs)
        {
            string i = ExAPI.GetFromStack(vm, 2).GetString();
            ExObject lis = ExAPI.GetFromStack(vm, 3);
            int n = lis.Value.l_List.Count;

            byte[] bytes = new byte[n];

            for (int l = 0; l < n; l++)
            {
                ExObject b = new();
                vm.ToInteger(lis.Value.l_List[l], ref b);
                bytes[l] = Convert.ToByte(b.GetInt());
            }

            vm.Pop(nargs + 2);
            File.WriteAllBytes(i, bytes);
            return 0;
        }

        public static int IoAppendfile(ExVM vm, int nargs)
        {
            string i = ExAPI.GetFromStack(vm, 2).GetString();

            ExObject c = null;
            vm.ToString(ExAPI.GetFromStack(vm, 3), ref c);
            string code = c.GetString();

            string enc = null;
            if (nargs == 3)
            {
                enc = ExAPI.GetFromStack(vm, 4).GetString();
            }

            Encoding e = DecideEncodingFromString(enc);

            vm.Pop(nargs + 2);
            File.AppendAllText(i, code, e);
            return 0;
        }
        public static int IoAppendfilelines(ExVM vm, int nargs)
        {
            string f = ExAPI.GetFromStack(vm, 2).GetString();
            ExObject lis = ExAPI.GetFromStack(vm, 3);

            string enc = null;
            if (nargs == 3)
            {
                enc = ExAPI.GetFromStack(vm, 4).GetString();
            }

            Encoding e = DecideEncodingFromString(enc);

            int n = lis.Value.l_List.Count;

            string[] lines = new string[n];

            for (int l = 0; l < n; l++)
            {
                ExObject line = new();
                vm.ToString(lis.Value.l_List[l], ref line);
                lines[l] = line.GetString();
            }

            vm.Pop(nargs + 2);
            File.AppendAllLines(f, lines, e);
            return 0;
        }

        public static int IoReadfile(ExVM vm, int nargs)
        {
            string f = ExAPI.GetFromStack(vm, 2).GetString();

            if (File.Exists(f))
            {
                string enc = null;
                if (nargs == 2)
                {
                    enc = ExAPI.GetFromStack(vm, 3).GetString();
                }

                Encoding e = DecideEncodingFromString(enc);

                vm.Pop(nargs + 2);
                vm.Push(File.ReadAllText(f, e));
            }
            else
            {
                vm.Pop(nargs + 2);
                vm.Push(new ExObject());
            }
            return 1;
        }

        public static int IoReadfilelines(ExVM vm, int nargs)
        {
            string f = ExAPI.GetFromStack(vm, 2).GetString();

            if (!File.Exists(f))
            {
                vm.Pop(nargs + 2);
                vm.Push(new ExObject());
                return 1;
            }

            string enc = null;
            if (nargs == 2)
            {
                enc = ExAPI.GetFromStack(vm, 3).GetString();
            }

            Encoding e = DecideEncodingFromString(enc);

            string[] lines = File.ReadAllLines(f, e);

            List<ExObject> l_list = new(lines.Length);
            for (int b = 0; b < lines.Length; b++)
            {
                l_list.Add(new(lines[b]));
            }
            ExObject res = new ExList();
            res.Value.l_List = l_list;

            vm.Pop(nargs + 2);
            vm.Push(res);

            return 1;
        }

        public static int IoReadfilebytes(ExVM vm, int nargs)
        {
            string f = ExAPI.GetFromStack(vm, 2).GetString();
            if (!File.Exists(f))
            {
                vm.Pop(nargs + 2);
                vm.Push(new ExObject());
                return 1;
            }

            byte[] bytes = File.ReadAllBytes(f);

            List<ExObject> blist = new(bytes.Length);
            for (int b = 0; b < bytes.Length; b++)
            {
                blist.Add(new(bytes[b]));
            }
            ExObject res = new ExList();
            res.Value.l_List = blist;

            vm.Pop(nargs + 2);
            vm.Push(res);

            return 1;
        }

        public static int IoFileexists(ExVM vm, int nargs)
        {
            string f = ExAPI.GetFromStack(vm, 2).GetString();
            vm.Pop(nargs + 2);
            vm.Push(File.Exists(f));

            return 1;
        }

        public static int IoCurrentdir(ExVM vm, int nargs)
        {
            vm.Pop(nargs + 2);
            vm.Push(Directory.GetCurrentDirectory());

            return 1;
        }

        public static int IoChangedir(ExVM vm, int nargs)
        {
            string dir = ExAPI.GetFromStack(vm, 2).GetString();

            if (!Directory.Exists(dir))
            {
                if (nargs == 2 && ExAPI.GetFromStack(vm, 3).GetBool())
                {
                    Directory.CreateDirectory(dir);
                    vm.Pop(nargs + 2);
                    vm.Push(true);
                    return 1;
                }
                vm.Pop(nargs + 2);
                vm.Push(false);
                return 1;
            }

            Directory.SetCurrentDirectory(dir);

            vm.Pop(nargs + 2);
            vm.Push(Directory.GetCurrentDirectory());
            return 1;
        }

        public static int IoMkdir(ExVM vm, int nargs)
        {
            string dir = ExAPI.GetFromStack(vm, 2).GetString();

            if (Directory.Exists(dir))
            {
                vm.Pop(nargs + 2);
                vm.Push(false);
                return 1;
            }
            Directory.CreateDirectory(dir);

            vm.Pop(nargs + 2);
            vm.Push(true);
            return 1;
        }

        public static int IoOpendir(ExVM vm, int nargs)
        {
            string dir = ExAPI.GetFromStack(vm, 2).GetString();

            try
            {
                if (!Directory.Exists(dir))
                {
                    if (nargs == 2 && ExAPI.GetFromStack(vm, 3).GetBool())
                    {
                        Directory.CreateDirectory(dir);
                        Directory.SetCurrentDirectory(dir);
                        Process.Start("./");

                        vm.Pop(nargs + 2);
                        vm.Push(true);
                        return 1;
                    }

                    vm.Pop(nargs + 2);
                    vm.Push(false);
                    return 1;
                }

                Directory.SetCurrentDirectory(dir);
                Process.Start("./");

                vm.Pop(nargs + 2);
                vm.Push(Directory.GetCurrentDirectory());
                return 1;
            }
            catch (Exception e)
            {
                vm.AddToErrorMessage("Error: " + e.Message);
                return -1;
            }
        }

        public static int IoShowdir(ExVM vm, int nargs)
        {
            string cd;
            if (nargs != 0)
            {
                cd = ExAPI.GetFromStack(vm, 2).GetString();
                if (!Directory.Exists(cd))
                {
                    vm.AddToErrorMessage(cd + " path doesn't exist");
                    return -1;
                }
            }
            else
            {
                cd = Directory.GetCurrentDirectory();
            }

            List<string> all;
            all = new(Directory.GetDirectories(cd));
            all.AddRange(Directory.GetFiles(cd));

            vm.Pop(nargs + 2);
            vm.Push(new ExList(all));

            return 1;
        }

        public static int IoIncludefile(ExVM vm, int nargs)
        {
            string fname = ExAPI.GetFromStack(vm, 2).GetString();
            if (fname == "*")
            {
                fname = Directory.GetCurrentDirectory();
                List<string> all;
                all = new(Directory.GetFiles(fname));
                bool failed = false;
                foreach (string f in all)
                {
                    if (!f.EndsWith(".exmat"))
                    {
                        continue;
                    }

                    if (ExAPI.CompileSource(vm, File.ReadAllText(f)))
                    {
                        ExAPI.PushRootTable(vm);
                        if (!ExAPI.Call(vm, 1, false, false))
                        {
                            ExAPI.WriteErrorMessages(vm, "EXECUTE");
                            failed = true;
                            break;
                        }

                    }
                    else
                    {
                        ExAPI.WriteErrorMessages(vm, "COMPILE");
                        failed = true;
                        break;
                    }
                }

                vm.Pop(nargs + 3);
                vm.Push(!failed);

                return 1;
            }
            else
            {
                if (!File.Exists(fname))
                {
                    if (!File.Exists(fname + ".exmat"))
                    {
                        vm.AddToErrorMessage(fname + " file doesn't exist");
                        return -1;
                    }
                    fname += ".exmat";
                }

                if (ExAPI.CompileSource(vm, File.ReadAllText(fname)))
                {
                    ExAPI.PushRootTable(vm);
                    if (!ExAPI.Call(vm, 1, false, false))
                    {
                        ExAPI.WriteErrorMessages(vm, "EXECUTE");
                        vm.Pop(nargs + 3);
                        vm.Push(false);
                        return 1;
                    }
                    else
                    {
                        vm.Pop(nargs + 3);
                        vm.Push(true);
                        return 1;
                    }
                }
                else
                {
                    ExAPI.WriteErrorMessages(vm, "COMPILE");
                    vm.Pop(nargs + 3);
                    vm.Push(false);
                    return 1;
                }
            }
        }
        public static int IoReloadlib(ExVM vm, int nargs)
        {
            string lname = ExAPI.GetFromStack(vm, 2).GetString();
            string fname = null;
            if (nargs == 2)
            {
                fname = ExAPI.GetFromStack(vm, 3).GetString();
            }
            switch (lname.ToLower())
            {
                case "std":
                    {
                        vm.AddToErrorMessage("use 'reload_base' function to reload the std base functions");
                        return -1;
                    }
                case "io":
                    {
                        if (nargs == 2)
                        {
                            if (fname != ReloadLibFunc)
                            {
                                ExAPI.PushRootTable(vm);
                                ExAPI.ReloadNativeFunction(vm, IOFuncs, fname, true);
                            }
                        }
                        else if (!RegisterStdIO(vm, true))
                        {
                            vm.AddToErrorMessage("something went wrong...");
                            return -1;
                        }
                        break;
                    }
                case "math":
                    {
                        if (nargs == 2)
                        {
                            ExAPI.PushRootTable(vm);
                            ExAPI.ReloadNativeFunction(vm, ExStdMath.MathFuncs, fname, true);
                        }
                        else if (!ExStdMath.RegisterStdMath(vm, true))
                        {
                            vm.AddToErrorMessage("something went wrong...");
                            return -1;
                        }
                        break;
                    }
                case "string":
                    {
                        if (nargs == 2)
                        {
                            ExAPI.PushRootTable(vm);
                            ExAPI.ReloadNativeFunction(vm, ExStdString.StringFuncs, fname, true);
                        }
                        else if (!ExStdString.RegisterStdString(vm, true))
                        {
                            vm.AddToErrorMessage("something went wrong...");
                            return -1;
                        }
                        break;
                    }
            }
            return 0;
        }

        public static int IoReloadlibfunc(ExVM vm, int nargs)
        {
            string fname = ExAPI.GetFromStack(vm, 2).GetString();

            if (fname != ReloadLibFunc
                && fname != ReloadLib
                && ExAPI.ReloadNativeFunction(vm, IOFuncs, fname, true))
            {
                return 0;
            }
            else if (ExAPI.ReloadNativeFunction(vm, ExStdString.StringFuncs, fname, true))
            {
                return 0;
            }
            else if (ExAPI.ReloadNativeFunction(vm, ExStdMath.MathFuncs, fname, true))
            {
                return 0;
            }

            vm.AddToErrorMessage("couldn't find a native function named '" + fname + "', try 'reload_base' function");
            return -1;
        }

        private static readonly List<ExRegFunc> _stdiofuncs = new()
        {
            new()
            {
                Name = "read_bytes",
                Function = IoReadfilebytes,
                nParameterChecks = 2,
                ParameterMask = ".s"
            },
            new()
            {
                Name = "read_text",
                Function = IoReadfile,
                nParameterChecks = -2,
                ParameterMask = ".ss",
                DefaultValues = new()
                {
                    { 2, new("") }
                }
            },
            new()
            {
                Name = "read_lines",
                Function = IoReadfilelines,
                nParameterChecks = -2,
                ParameterMask = ".ss",
                DefaultValues = new()
                {
                    { 2, new("") }
                }
            },

            new()
            {
                Name = "write_bytes",
                Function = IoWritefilebytes,
                nParameterChecks = 3,
                ParameterMask = ".sa"
            },
            new()
            {
                Name = "write_text",
                Function = IoWritefile,
                nParameterChecks = -3,
                ParameterMask = ".sss",
                DefaultValues = new()
                {
                    { 3, new("") }
                }
            },
            new()
            {
                Name = "write_lines",
                Function = IoWritefilelines,
                nParameterChecks = -3,
                ParameterMask = ".sas",
                DefaultValues = new()
                {
                    { 3, new("") }
                }
            },

            new()
            {
                Name = "append_text",
                Function = IoAppendfile,
                nParameterChecks = -3,
                ParameterMask = ".sss",
                DefaultValues = new()
                {
                    { 3, new("") }
                }
            },
            new()
            {
                Name = "append_lines",
                Function = IoAppendfilelines,
                nParameterChecks = -3,
                ParameterMask = ".sas",
                DefaultValues = new()
                {
                    { 3, new("") }
                }
            },

            new()
            {
                Name = "current_dir",
                Function = IoCurrentdir,
                nParameterChecks = 1,
                ParameterMask = "."
            },
            new()
            {
                Name = "dir_content",
                Function = IoShowdir,
                nParameterChecks = -1,
                ParameterMask = ".s",
                DefaultValues = new()
                {
                    { 1, new("") }
                }
            },
            new()
            {
                Name = "change_dir",
                Function = IoChangedir,
                nParameterChecks = -2,
                ParameterMask = ".sb",
                DefaultValues = new()
                {
                    { 2, new(false) }
                }
            },
            new()
            {
                Name = "make_dir",
                Function = IoMkdir,
                nParameterChecks = -2,
                ParameterMask = ".s"
            },
            new()
            {
                Name = "open_dir",
                Function = IoOpendir,
                nParameterChecks = -2,
                ParameterMask = ".sb",
                DefaultValues = new()
                {
                    { 2, new(false) }
                }
            },

            new()
            {
                Name = "raw_input",
                Function = IoRawinput,
                nParameterChecks = -1,
                ParameterMask = ".s",
                DefaultValues = new()
                {
                    { 1, new("") }
                }
            },

            new()
            {
                Name = "file_exists",
                Function = IoFileexists,
                nParameterChecks = 2,
                ParameterMask = ".s"
            },
            new()
            {
                Name = "include_file",
                Function = IoIncludefile,
                nParameterChecks = 2,
                ParameterMask = ".s"
            },
            new()
            {
                Name = ReloadLib,
                Function = IoReloadlib,
                nParameterChecks = -2,
                ParameterMask = ".ss"
            },
            new()
            {
                Name = ReloadLibFunc,
                Function = IoReloadlibfunc,
                nParameterChecks = 2,
                ParameterMask = ".s"
            }
        };

        private const string _reloadlib = "reload_lib";
        public static string ReloadLib => _reloadlib;

        private const string _reloadlibfunc = "reload_func";
        public static string ReloadLibFunc => _reloadlibfunc;

        public static int IoRawinput(ExVM vm, int nargs)
        {
            if (nargs == 1)
            {
                vm.Print(ExAPI.GetFromStack(vm, 2).GetString());
            }

            string input = string.Empty;
            char ch;

            while (!char.IsControl(ch = (char)Console.Read()))
            {
                input += ch;
            }

            if (vm.GotUserInput)
            {
                input = string.Empty;
                while (!char.IsControl(ch = (char)Console.Read()))
                {
                    input += ch;
                }
            }

            vm.Pop(nargs + 2);
            vm.Push(new ExObject(input));

            vm.GotUserInput = true;
            return 1;
        }

        public static List<ExRegFunc> IOFuncs => _stdiofuncs;

        public static bool RegisterStdIO(ExVM vm, bool force = false)
        {
            ExAPI.RegisterNativeFunctions(vm, IOFuncs, force);

            return true;
        }
    }
}
