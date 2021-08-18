using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using ExcelDataReader;
using ExMat.API;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.BaseLib
{
    public static class ExStdIO
    {
        public static ExFunctionStatus IoClear(ExVM vm, int nargs)
        {
            Console.Clear();
            vm.Pop(nargs + 2);
            return ExFunctionStatus.VOID;
        }

        public static ExFunctionStatus IoPaint(ExVM vm, int nargs)
        {
            string s = vm.GetArgument(1).GetString();

            if (nargs > 2)
            {
                Console.ForegroundColor = ExAPI.GetColorFromName(vm.GetArgument(3).GetString(), ConsoleColor.White);
                Console.BackgroundColor = ExAPI.GetColorFromName(vm.GetArgument(2).GetString());
            }
            else if (nargs > 1)
            {
                Console.BackgroundColor = ExAPI.GetColorFromName(vm.GetArgument(2).GetString());
            }

            vm.Print(s);

            Console.ResetColor();

            return ExFunctionStatus.VOID;
        }

        public static ExFunctionStatus IoWritefile(ExVM vm, int nargs)
        {
            string i = vm.GetArgument(1).GetString();
            ExObject c = null;
            vm.ToString(vm.GetArgument(2), ref c);
            string code = c.GetString();

            string enc = null;
            if (nargs == 3)
            {
                enc = vm.GetArgument(3).GetString();
            }

            Encoding e = ExAPI.DecideEncodingFromString(enc);

            vm.Pop(nargs + 2);
            File.WriteAllText(i, code, e);

            return ExFunctionStatus.VOID;
        }
        public static ExFunctionStatus IoWritefilelines(ExVM vm, int nargs)
        {
            string i = vm.GetArgument(1).GetString();
            ExObject lis = vm.GetArgument(2);

            string enc = null;
            if (nargs == 3)
            {
                enc = vm.GetArgument(3).GetString();
            }

            Encoding e = ExAPI.DecideEncodingFromString(enc);

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
            return ExFunctionStatus.VOID;
        }
        public static ExFunctionStatus IoWritefilebytes(ExVM vm, int nargs)
        {
            string i = vm.GetArgument(1).GetString();
            ExObject lis = vm.GetArgument(2);
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

            return ExFunctionStatus.VOID;
        }

        public static ExFunctionStatus IoAppendfile(ExVM vm, int nargs)
        {
            string i = vm.GetArgument(1).GetString();

            ExObject c = null;
            vm.ToString(vm.GetArgument(2), ref c);
            string code = c.GetString();

            string enc = null;
            if (nargs == 3)
            {
                enc = vm.GetArgument(3).GetString();
            }

            Encoding e = ExAPI.DecideEncodingFromString(enc);

            vm.Pop(nargs + 2);
            File.AppendAllText(i, code, e);

            return ExFunctionStatus.VOID;
        }
        public static ExFunctionStatus IoAppendfilelines(ExVM vm, int nargs)
        {
            string f = vm.GetArgument(1).GetString();
            ExObject lis = vm.GetArgument(2);

            string enc = null;
            if (nargs == 3)
            {
                enc = vm.GetArgument(3).GetString();
            }

            Encoding e = ExAPI.DecideEncodingFromString(enc);

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

            return ExFunctionStatus.VOID;
        }

        public static ExFunctionStatus IoReadfile(ExVM vm, int nargs)
        {
            string f = vm.GetArgument(1).GetString();

            if (File.Exists(f))
            {
                string enc = null;
                if (nargs == 2)
                {
                    enc = vm.GetArgument(2).GetString();
                }

                Encoding e = ExAPI.DecideEncodingFromString(enc);

                return vm.CleanReturn(nargs + 2, File.ReadAllText(f, e));
            }
            else
            {
                return vm.CleanReturn(nargs + 2, new ExObject());
            }
        }

        public static ExFunctionStatus IoReadfilelines(ExVM vm, int nargs)
        {
            string f = vm.GetArgument(1).GetString();

            if (!File.Exists(f))
            {
                return vm.CleanReturn(nargs + 2, new ExObject());
            }

            string enc = null;
            if (nargs == 2)
            {
                enc = vm.GetArgument(2).GetString();
            }

            Encoding e = ExAPI.DecideEncodingFromString(enc);

            string[] lines = File.ReadAllLines(f, e);

            List<ExObject> l_list = new(lines.Length);
            for (int b = 0; b < lines.Length; b++)
            {
                l_list.Add(new(lines[b]));
            }
            ExObject res = new ExList();
            res.Value.l_List = l_list;

            return vm.CleanReturn(nargs + 2, res);
        }

        public static ExFunctionStatus IoReadfilebytes(ExVM vm, int nargs)
        {
            string f = vm.GetArgument(1).GetString();
            if (!File.Exists(f))
            {
                return vm.CleanReturn(nargs + 2, new ExObject());
            }

            byte[] bytes = File.ReadAllBytes(f);

            List<ExObject> blist = new(bytes.Length);
            for (int b = 0; b < bytes.Length; b++)
            {
                blist.Add(new(bytes[b]));
            }
            ExObject res = new ExList();
            res.Value.l_List = blist;

            return vm.CleanReturn(nargs + 2, res);
        }

        public static ExFunctionStatus IoFileexists(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, File.Exists(vm.GetArgument(1).GetString()));
        }

        public static ExFunctionStatus IoCurrentdir(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, Directory.GetCurrentDirectory());
        }

        public static ExFunctionStatus IoChangedir(ExVM vm, int nargs)
        {
            string dir = vm.GetArgument(1).GetString();

            if (!Directory.Exists(dir))
            {
                if (nargs == 2 && vm.GetArgument(2).GetBool())
                {
                    Directory.CreateDirectory(dir);
                    return vm.CleanReturn(nargs + 2, true);
                }
                return vm.CleanReturn(nargs + 2, false);
            }

            Directory.SetCurrentDirectory(dir);

            return vm.CleanReturn(nargs + 2, Directory.GetCurrentDirectory());
        }

        public static ExFunctionStatus IoMkdir(ExVM vm, int nargs)
        {
            string dir = vm.GetArgument(1).GetString();

            if (Directory.Exists(dir))
            {
                return vm.CleanReturn(nargs + 2, false);
            }
            Directory.CreateDirectory(dir);

            return vm.CleanReturn(nargs + 2, true);
        }

        public static ExFunctionStatus IoOpendir(ExVM vm, int nargs)
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
                        Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", "./");

                        return vm.CleanReturn(nargs + 2, true);
                    }

                    return vm.CleanReturn(nargs + 2, false);
                }

                Directory.SetCurrentDirectory(dir);
                Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", "./");

                return vm.CleanReturn(nargs + 2, Directory.GetCurrentDirectory());
            }
            catch (Exception e)
            {
                return vm.AddToErrorMessage("Error: " + e.Message);
            }
        }

        public static ExFunctionStatus IoShowdir(ExVM vm, int nargs)
        {
            string cd;
            if (nargs != 0)
            {
                cd = vm.GetArgument(1).GetString();
                if (!Directory.Exists(cd))
                {
                    return vm.AddToErrorMessage(cd + " path doesn't exist");
                }
            }
            else
            {
                cd = Directory.GetCurrentDirectory();
            }

            List<string> all = new(Directory.GetDirectories(cd));
            all.AddRange(Directory.GetFiles(cd));

            return vm.CleanReturn(nargs + 2, new ExList(all));
        }

        public static ExFunctionStatus IoIncludefile(ExVM vm, int nargs)
        {
            string fname = vm.GetArgument(1).GetString();
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
                            ExAPI.WriteErrorMessages(vm, ExErrorType.RUNTIME);
                            failed = true;
                            break;
                        }

                    }
                    else
                    {
                        ExAPI.WriteErrorMessages(vm, ExErrorType.COMPILE);
                        failed = true;
                        break;
                    }
                }

                return vm.CleanReturn(nargs + 2, !failed);
            }
            else
            {
                if (!File.Exists(fname))
                {
                    if (!File.Exists(fname + ".exmat"))
                    {
                        return vm.AddToErrorMessage(fname + " file doesn't exist");
                    }
                    fname += ".exmat";
                }

                if (ExAPI.CompileSource(vm, File.ReadAllText(fname)))
                {
                    ExAPI.PushRootTable(vm);
                    if (!ExAPI.Call(vm, 1, false, false))
                    {
                        ExAPI.WriteErrorMessages(vm, ExErrorType.RUNTIME);
                        return vm.CleanReturn(nargs + 3, false);
                    }
                    else
                    {
                        return vm.CleanReturn(nargs + 3, true);
                    }
                }
                else
                {
                    ExAPI.WriteErrorMessages(vm, ExErrorType.COMPILE);
                    return vm.CleanReturn(nargs + 3, false);
                }
            }
        }
        public static ExFunctionStatus IoReloadlib(ExVM vm, int nargs)
        {
            string lname = vm.GetArgument(1).GetString();
            string fname = null;
            if (nargs == 2)
            {
                fname = vm.GetArgument(2).GetString();
            }
            switch (lname.ToLower())
            {
                case "std":
                    {
                        return vm.AddToErrorMessage("use 'reload_base' function to reload the std base functions");
                    }
                case "io":
                    {
                        if (nargs == 2)
                        {
                            if (fname != ReloadLibFunc)
                            {
                                ExAPI.PushRootTable(vm);
                                ExAPI.ReloadNativeFunction(vm, IOFuncs, fname);
                            }
                        }
                        else if (!RegisterStdIO(vm))
                        {
                            return vm.AddToErrorMessage("something went wrong...");
                        }
                        break;
                    }
                case "math":
                    {
                        if (nargs == 2)
                        {
                            ExAPI.PushRootTable(vm);
                            ExAPI.ReloadNativeFunction(vm, ExStdMath.MathFuncs, fname);
                        }
                        else if (!ExStdMath.RegisterStdMath(vm))
                        {
                            return vm.AddToErrorMessage("something went wrong...");
                        }
                        break;
                    }
                case "string":
                    {
                        if (nargs == 2)
                        {
                            ExAPI.PushRootTable(vm);
                            ExAPI.ReloadNativeFunction(vm, ExStdString.StringFuncs, fname);
                        }
                        else if (!ExStdString.RegisterStdString(vm))
                        {
                            return vm.AddToErrorMessage("something went wrong...");
                        }
                        break;
                    }
                case "net":
                    {
                        if (nargs == 2)
                        {
                            ExAPI.PushRootTable(vm);
                            ExAPI.ReloadNativeFunction(vm, ExStdNet.NetFuncs, fname);
                        }
                        else if (!ExStdNet.RegisterStdNet(vm))
                        {
                            return vm.AddToErrorMessage("something went wrong...");
                        }
                        break;
                    }
            }
            return 0;
        }

        public static ExFunctionStatus IoReloadlibfunc(ExVM vm, int nargs)
        {
            string fname = vm.GetArgument(1).GetString();

            if (fname != ReloadLibFunc
                && fname != ReloadLib
                && ExAPI.ReloadNativeFunction(vm, IOFuncs, fname))
            {
                return ExFunctionStatus.VOID;
            }
            else if (ExAPI.ReloadNativeFunction(vm, ExStdString.StringFuncs, fname))
            {
                return ExFunctionStatus.VOID;
            }
            else if (ExAPI.ReloadNativeFunction(vm, ExStdMath.MathFuncs, fname))
            {
                return ExFunctionStatus.VOID;
            }

            return vm.AddToErrorMessage("couldn't find a native function named '" + fname + "', try 'reload_base' function");
        }

        private static readonly ExcelReaderConfiguration ExcelReaderConfig = new()
        {
            // Gets or sets the encoding to use when the input XLS lacks a CodePage
            // record, or when the input CSV lacks a BOM and does not parse as UTF8. 
            // Default: cp1252 (XLS BIFF2-5 and CSV only)
            FallbackEncoding = Encoding.GetEncoding(1252),

            // Gets or sets an array of CSV separator candidates. The reader 
            // autodetects which best fits the input data. Default: , ; TAB | # 
            // (CSV only)
            AutodetectSeparators = new char[] { ',', ';', '\t', '|', '#' },

            // Gets or sets a value indicating whether to leave the stream open after
            // the IExcelDataReader object is disposed. Default: false
            LeaveOpen = false,

            // Gets or sets a value indicating the number of rows to analyze for
            // encoding, separator and field count in a CSV. When set, this option
            // causes the IExcelDataReader.RowCount property to throw an exception.
            // Default: 0 - analyzes the entire file (CSV only, has no effect on other
            // formats)
            AnalyzeInitialCsvRows = 0,
        };

        public static ExFunctionStatus IoReadExcel(ExVM vm, int nargs)
        {
            string path = vm.GetArgument(1).GetString();
            ExcelReaderConfig.Password = nargs > 1 ? vm.GetArgument(2).GetString() : string.Empty;

            using FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read);
            using IExcelDataReader reader = ExcelReaderFactory.CreateReader(stream, ExcelReaderConfig);

            // The result of each spreadsheet is in result.Tables
            DataSet result = reader.AsDataSet();

            Dictionary<string, ExObject> res = new(result.Tables.Count);

            foreach (DataTable table in result.Tables)
            {
                List<ExObject> rows = new(table.Rows.Count);
                foreach (DataRow row in table.Rows)
                {
                    List<ExObject> rowcontent = new(table.Columns.Count);
                    foreach (DataColumn column in table.Columns)
                    {
                        rowcontent.Add(new(row[column].ToString()));
                    }
                    rows.Add(new(rowcontent));
                }
                res.Add(table.TableName, new(rows));
            }

            return vm.CleanReturn(nargs + 2, res);
        }

        public static ExFunctionStatus IoWriteExcel(ExVM vm, int nargs)
        {
            string file = vm.GetArgument(1).GetString();
            string sheet = vm.GetArgument(2).GetString();
            List<ExObject> rows = vm.GetArgument(3).GetList();

            int rowoffset = nargs > 3 ? (int)vm.GetArgument(4).GetInt() : 0;
            int coloffset = nargs > 4 ? (int)vm.GetArgument(5).GetInt() : 0;
            rowoffset = rowoffset < 0 ? 0 : rowoffset;
            coloffset = coloffset < 0 ? 0 : coloffset;

            using XLWorkbook workbook = File.Exists(file) ? new XLWorkbook(file, new LoadOptions() { }) : new XLWorkbook();

            IXLWorksheet worksheet = workbook.Worksheets.Contains(sheet) ? workbook.Worksheet(sheet) : workbook.Worksheets.Add(sheet);

            for (int i = 1; i <= rows.Count; i++)
            {
                switch (rows[i - 1].Type)
                {
                    case ExObjType.ARRAY:
                        {
                            List<ExObject> row = rows[i - 1].GetList();
                            for (int j = 1; j <= row.Count; j++)
                            {
                                worksheet.Cell(i + rowoffset, j + coloffset).Value = vm.GetSimpleString(row[j - 1]);
                            }
                            break;
                        }
                }
            }
            workbook.SaveAs(file);
            return vm.CleanReturn(nargs + 2, true);
        }

        public static List<ExRegFunc> IOFuncs => _stdiofuncs;

        private static readonly List<ExRegFunc> _stdiofuncs = new()
        {
            new()
            {
                Name = "clear",
                Function = IoClear,
                nParameterChecks = 1,
                ParameterMask = "."
            },
            new()
            {
                Name = "paint_print",
                Function = IoPaint,
                nParameterChecks = -2,
                ParameterMask = ".sss",
                DefaultValues = new()
                {
                    { 2, new("black") },
                    { 3, new("white") }
                }
            },
            new()
            {
                Name = "read_excel",
                Function = IoReadExcel,
                nParameterChecks = -2,
                ParameterMask = ".ss",
                DefaultValues = new()
                {
                    { 2, new(string.Empty) }
                }
            },
            new()
            {
                Name = "write_excel",
                Function = IoWriteExcel,
                nParameterChecks = -4,
                ParameterMask = ".ssaii",
                DefaultValues = new()
                {
                    { 4, new(0) },
                    { 5, new(0) }
                }
            },
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

        public static ExFunctionStatus IoRawinput(ExVM vm, int nargs)
        {
            if (nargs == 1)
            {
                vm.Print(vm.GetArgument(1).GetString());
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
            vm.GotUserInput = true;

            return vm.CleanReturn(nargs + 2, new ExObject(input));
        }


        public static bool RegisterStdIO(ExVM vm)
        {
            // For read_excel
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ExAPI.RegisterNativeFunctions(vm, IOFuncs);

            return true;
        }
    }
}
