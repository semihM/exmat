using System;
using System.Collections.Generic;
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

        public static int IO_writefile(ExVM vm, int nargs)
        {
            string i = ExAPI.GetFromStack(vm, 2).GetString();
            ExObjectPtr c = null;
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
        public static int IO_writefilelines(ExVM vm, int nargs)
        {
            string i = ExAPI.GetFromStack(vm, 2).GetString();
            ExObjectPtr lis = ExAPI.GetFromStack(vm, 3);

            string enc = null;
            if (nargs == 3)
            {
                enc = ExAPI.GetFromStack(vm, 4).GetString();
            }

            Encoding e = DecideEncodingFromString(enc);

            int n = lis._val.l_List.Count;

            string[] lines = new string[n];

            for (int l = 0; l < n; l++)
            {
                ExObjectPtr line = new();
                vm.ToString(lis._val.l_List[l], ref line);
                lines[l] = line.GetString();
            }

            vm.Pop(nargs + 2);
            File.WriteAllLines(i, lines, e);
            return 0;
        }
        public static int IO_writefilebytes(ExVM vm, int nargs)
        {
            string i = ExAPI.GetFromStack(vm, 2).GetString();
            ExObjectPtr lis = ExAPI.GetFromStack(vm, 3);
            int n = lis._val.l_List.Count;

            byte[] bytes = new byte[n];

            for (int l = 0; l < n; l++)
            {
                ExObjectPtr b = new();
                vm.ToInteger(lis._val.l_List[l], ref b);
                bytes[l] = Convert.ToByte(b.GetInt());
            }

            vm.Pop(nargs + 2);
            File.WriteAllBytes(i, bytes);
            return 0;
        }

        public static int IO_appendfile(ExVM vm, int nargs)
        {
            string i = ExAPI.GetFromStack(vm, 2).GetString();

            ExObjectPtr c = null;
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
        public static int IO_appendfilelines(ExVM vm, int nargs)
        {
            string f = ExAPI.GetFromStack(vm, 2).GetString();
            ExObjectPtr lis = ExAPI.GetFromStack(vm, 3);

            string enc = null;
            if (nargs == 3)
            {
                enc = ExAPI.GetFromStack(vm, 4).GetString();
            }

            Encoding e = DecideEncodingFromString(enc);

            int n = lis._val.l_List.Count;

            string[] lines = new string[n];

            for (int l = 0; l < n; l++)
            {
                ExObjectPtr line = new();
                vm.ToString(lis._val.l_List[l], ref line);
                lines[l] = line.GetString();
            }

            vm.Pop(nargs + 2);
            File.AppendAllLines(f, lines, e);
            return 0;
        }

        public static int IO_readfile(ExVM vm, int nargs)
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
                vm.Push(new ExObjectPtr());
            }
            return 1;
        }

        public static int IO_readfilelines(ExVM vm, int nargs)
        {
            string f = ExAPI.GetFromStack(vm, 2).GetString();

            if (!File.Exists(f))
            {
                vm.Pop(nargs + 2);
                vm.Push(new ExObjectPtr());
                return 1;
            }

            string enc = null;
            if (nargs == 2)
            {
                enc = ExAPI.GetFromStack(vm, 3).GetString();
            }

            Encoding e = DecideEncodingFromString(enc);

            string[] lines = File.ReadAllLines(f, e);

            List<ExObjectPtr> l_list = new(lines.Length);
            for (int b = 0; b < lines.Length; b++)
            {
                l_list.Add(new(lines[b]));
            }
            ExObjectPtr res = new ExList();
            res._val.l_List = l_list;

            vm.Pop(nargs + 2);
            vm.Push(res);

            return 1;
        }

        public static int IO_readfilebytes(ExVM vm, int nargs)
        {
            string f = ExAPI.GetFromStack(vm, 2).GetString();
            if (!File.Exists(f))
            {
                vm.Pop(nargs + 2);
                vm.Push(new ExObjectPtr());
                return 1;
            }

            byte[] bytes = File.ReadAllBytes(f);

            List<ExObjectPtr> blist = new(bytes.Length);
            for (int b = 0; b < bytes.Length; b++)
            {
                blist.Add(new(bytes[b]));
            }
            ExObjectPtr res = new ExList();
            res._val.l_List = blist;

            vm.Pop(nargs + 2);
            vm.Push(res);

            return 1;
        }

        public static int IO_fileexists(ExVM vm, int nargs)
        {
            string f = ExAPI.GetFromStack(vm, 2).GetString();
            vm.Pop(nargs + 2);
            vm.Push(File.Exists(f));

            return 1;
        }

        public static int IO_currentdir(ExVM vm, int nargs)
        {
            vm.Pop(nargs + 2);
            vm.Push(Directory.GetCurrentDirectory());

            return 1;
        }

        public static int IO_changedir(ExVM vm, int nargs)
        {
            string dir = ExAPI.GetFromStack(vm, 2).GetString();

            if (!Directory.Exists(dir))
            {
                if (nargs == 2 && ExAPI.GetFromStack(vm, 3).GetBool())
                {
                    Directory.CreateDirectory(dir);
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

        public static int IO_showdir(ExVM vm, int nargs)
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

        public static int IO_includefile(ExVM vm, int nargs)
        {
            string fname = ExAPI.GetFromStack(vm, 2).GetString();
            if (!File.Exists(fname))
            {
                if (!File.Exists(fname + ".exmat"))
                {
                    vm.AddToErrorMessage(fname + " file doesn't exist");
                    return -1;
                }
                fname += ".exmat";
            }

            if (ExAPI.CompileFile(vm, File.ReadAllText(fname)))
            {
                ExAPI.PushRootTable(vm);
                if (!ExAPI.Call(vm, 1, false, false))
                {
                    ExAPI.WriteErrorMessages(vm, "EXECUTE");
                }

                vm.Pop(nargs + 2);
                vm.Push(new ExObjectPtr(true));

                return 1;
            }

            vm.Pop(nargs + 2);
            vm.Push(new ExObjectPtr(false));
            return -1;
        }
        public static int IO_reloadlib(ExVM vm, int nargs)
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

        public static int IO_reloadlibfunc(ExVM vm, int nargs)
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
            new() { name = "read_bytes", func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_readfilebytes")), n_pchecks = 2, mask = ".s" },
            new() { name = "read_text", func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_readfile")), n_pchecks = -2, mask = ".ss" },
            new() { name = "read_lines", func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_readfilelines")), n_pchecks = -2, mask = ".ss" },

            new() { name = "write_bytes", func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_writefilebytes")), n_pchecks = -2, mask = ".sa" },
            new() { name = "write_text", func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_writefile")), n_pchecks = -3, mask = ".sss" },
            new() { name = "write_lines", func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_writefilelines")), n_pchecks = -3, mask = ".sas" },

            new() { name = "append_text", func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_appendfile")), n_pchecks = -3, mask = ".sss" },
            new() { name = "append_lines", func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_appendfilelines")), n_pchecks = -3, mask = ".sas" },

            new() { name = "current_dir", func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_currentdir")), n_pchecks = 1, mask = "." },
            new() { name = "dir_content", func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_showdir")), n_pchecks = -1, mask = ".s" },
            new() { name = "change_dir", func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_changedir")), n_pchecks = -2, mask = ".sb" },

            new() { name = "raw_input", func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_rawinput")), n_pchecks = -1, mask = ".s" },

            new() { name = "file_exists", func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_fileexists")), n_pchecks = 2, mask = ".s" },
            new() { name = "include_file", func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_includefile")), n_pchecks = 2, mask = ".s" },
            new() { name = ReloadLib, func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_reloadlib")), n_pchecks = -2, mask = ".ss" },
            new() { name = ReloadLibFunc, func = new(Type.GetType("ExMat.BaseLib.ExStdIO").GetMethod("IO_reloadlibfunc")), n_pchecks = 2, mask = ".s" },

            new() { name = string.Empty }
        };

        private const string _reloadlib = "reload_lib";
        public static string ReloadLib { get => _reloadlib; }

        private const string _reloadlibfunc = "reload_func";
        public static string ReloadLibFunc { get => _reloadlibfunc; }

        public static int IO_rawinput(ExVM vm, int nargs)
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

            if (vm._got_input)
            {
                input = string.Empty;
                while (!char.IsControl(ch = (char)Console.Read()))
                {
                    input += ch;
                }
            }

            vm.Pop(nargs + 2);
            vm.Push(new ExObjectPtr(input));

            vm._got_input = true;
            return 1;
        }

        public static List<ExRegFunc> IOFuncs { get => _stdiofuncs; }

        public static bool RegisterStdIO(ExVM vm, bool force = false)
        {
            ExAPI.RegisterNativeFunctions(vm, IOFuncs, force);

            return true;
        }
    }
}
