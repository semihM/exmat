using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using ExcelDataReader;
using ExMat.API;
using ExMat.Objects;
using ExMat.VM;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ExMat.BaseLib
{
    public static class ExStdIO
    {

        public static ExObject GetJsonContent(JToken item)
        {
            if (item is JValue)
            {
                return new(item.ToString());
            }
            else if (item is JProperty i)
            {
                return new(GetJsonContent(i.Value));
            }
            else if (item is JArray ja)
            {
                List<ExObject> res = new();
                foreach (JToken tkn in ja)
                {
                    res.Add(GetJsonContent(tkn));
                }
                return new(res);
            }
            else if (item is JObject)
            {
                Dictionary<string, ExObject> res = new();
                foreach (JProperty tkn in item.Children())
                {
                    res.Add(tkn.Name, GetJsonContent(tkn));
                }
                return new(res);
            }
            return new();
        }

        private static readonly string Indentation = " ";

        private static string IndentPrefix(string prefix)
        {
            return string.Format("{0}{1}", prefix, Indentation);
        }

        public static string ConvertToJson(ExObject obj, string prev = "", string prefix = "")
        {
            ExObjType typ = obj.Type;
            if (prefix == "")
            {
                switch (typ)
                {
                    case ExObjType.DICT:
                        {
                            prev += "{\n";
                            break;
                        }
                    case ExObjType.ARRAY:
                        {
                            prev += "[\n";
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }

                prefix = Indentation;
            }

            switch (typ)
            {
                case ExObjType.DICT:
                    {
                        int i = -1;
                        int last = obj.GetDict().Count - 1;
                        foreach (KeyValuePair<string, ExObject> pair in obj.GetDict())
                        {
                            i++;
                            prev = string.Format("{0}\"{1}\": {2}{3}", prefix, pair.Key, ConvertToJson(pair.Value, prev, IndentPrefix(prefix)), i != last ? ",\n" : "\n");
                        }

                        break;
                    }
                case ExObjType.ARRAY:
                    {
                        int i = -1;
                        int last = obj.GetList().Count - 1;
                        foreach (ExObject o in obj.GetList())
                        {
                            i++;
                            prev = string.Format("{0}{1}", ConvertToJson(o, prev, IndentPrefix(prefix)), i != last ? ", " : "\n");
                        }
                        break;
                    }
                case ExObjType.INTEGER:
                case ExObjType.FLOAT:
                    {
                        prev += prefix + "\"" + obj.GetFloat() + "\"";
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        prev += prefix + "\"" + obj.GetComplexString() + "\"";
                        break;
                    }
                case ExObjType.BOOL:
                    {
                        prev += prefix + (obj.GetBool() ? "true" : "false");
                        break;
                    }
                case ExObjType.STRING:
                    {
                        prev += prefix + "\"" + obj.GetString() + "\"";
                        break;
                    }
                case ExObjType.SPACE:
                    {
                        prev += prefix + "\"" + obj.Value.c_Space.GetSpaceString() + "\"";
                        break;
                    }
                default:
                    {
                        prev += prefix + "\"" + typ.ToString() + "\"";
                        break;
                    }
            }


            if (prefix == " ")
            {
                switch (typ)
                {
                    case ExObjType.DICT:
                        {
                            prev += "}\n";
                            break;
                        }
                    case ExObjType.ARRAY:
                        {
                            prev += "]\n";
                            break;
                        }
                    default:
                        {
                            break;
                        }
                }
            }
            return prev;
        }

        public static ExFunctionStatus IoReadjson(ExVM vm, int nargs)
        {
            string f = vm.GetArgument(1).GetString();

            if (File.Exists(f))
            {
                string enc = null;
                if (nargs == 2)
                {
                    enc = vm.GetArgument(2).GetString();
                }

                Encoding e = ExApi.DecideEncodingFromString(enc);
                try
                {
                    ExObject res = GetJsonContent((JObject)JsonConvert.DeserializeObject(File.ReadAllText(f, e)));
                    return vm.CleanReturn(nargs + 2, res);
                }
                catch (Exception err)
                {
                    return vm.AddToErrorMessage(err.Message);
                }
            }
            else
            {
                return vm.CleanReturn(nargs + 2, new ExObject());
            }
        }

        public static ExFunctionStatus IoWritejson(ExVM vm, int nargs)
        {
            string i = vm.GetArgument(1).GetString();

            string enc = null;
            if (nargs == 3)
            {
                enc = vm.GetArgument(3).GetString();
            }

            Encoding e = ExApi.DecideEncodingFromString(enc);

            string js = ConvertToJson(vm.GetArgument(2));
            try
            {
                File.WriteAllText(i.EndsWith(".json") ? i : (i + ".json"), js, e);
                return vm.CleanReturn(nargs + 2, true);
            }
            catch (Exception err)
            {
                return vm.AddToErrorMessage(err.Message);
            }
        }

        public static ExFunctionStatus IoClear(ExVM vm, int nargs)
        {
            Console.Clear();
            return vm.CleanReturn(nargs + 2, true);
        }

        public static ExFunctionStatus IoPaint(ExVM vm, int nargs)
        {
            string s = vm.GetArgument(1).GetString();

            if (nargs > 2)
            {
                Console.ForegroundColor = ExApi.GetColorFromName(vm.GetArgument(3).GetString(), ConsoleColor.White);
                Console.BackgroundColor = ExApi.GetColorFromName(vm.GetArgument(2).GetString());
            }
            else if (nargs > 1)
            {
                Console.BackgroundColor = ExApi.GetColorFromName(vm.GetArgument(2).GetString());
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

            Encoding e = ExApi.DecideEncodingFromString(enc);

            try
            {
                File.WriteAllText(i, code, e);
                return vm.CleanReturn(nargs + 2, true);
            }
            catch (Exception err)
            {
                return vm.AddToErrorMessage("Error writing file: " + err.Message);
            }
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

            Encoding e = ExApi.DecideEncodingFromString(enc);

            int n = lis.Value.l_List.Count;

            string[] lines = new string[n];

            for (int l = 0; l < n; l++)
            {
                ExObject line = new();
                vm.ToString(lis.Value.l_List[l], ref line);
                lines[l] = line.GetString();
            }

            try
            {
                File.WriteAllLines(i, lines, e);
                return vm.CleanReturn(nargs + 2, true);
            }
            catch (Exception err)
            {
                return vm.AddToErrorMessage("Error writing file: " + err.Message);
            }
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

                if (!ExApi.ToInteger(vm, lis.Value.l_List[l], ref b))
                {
                    return vm.AddToErrorMessage(string.Format("Failed to convert '{0}' to byte", ExApi.GetSimpleString(lis.Value.l_List[l])));
                }

                bytes[l] = Convert.ToByte(b.GetInt());
            }

            try
            {
                File.WriteAllBytes(i, bytes);
                return vm.CleanReturn(nargs + 2, true);
            }
            catch (Exception err)
            {
                return vm.AddToErrorMessage("Error writing file: " + err.Message);
            }
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

            Encoding e = ExApi.DecideEncodingFromString(enc);

            try
            {
                File.AppendAllText(i, code, e);
                return vm.CleanReturn(nargs + 2, true);
            }
            catch (Exception err)
            {
                return vm.AddToErrorMessage("Error writing file: " + err.Message);
            }
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

            Encoding e = ExApi.DecideEncodingFromString(enc);

            int n = lis.Value.l_List.Count;

            string[] lines = new string[n];

            for (int l = 0; l < n; l++)
            {
                ExObject line = new();
                vm.ToString(lis.Value.l_List[l], ref line);
                lines[l] = line.GetString();
            }

            try
            {
                File.AppendAllLines(f, lines, e);
                return vm.CleanReturn(nargs + 2, true);
            }
            catch (Exception err)
            {
                return vm.AddToErrorMessage("Error writing file: " + err.Message);
            }
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

                Encoding e = ExApi.DecideEncodingFromString(enc);

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

            Encoding e = ExApi.DecideEncodingFromString(enc);

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

                    if (ExApi.CompileSource(vm, File.ReadAllText(f)))
                    {
                        ExApi.PushRootTable(vm);
                        if (!ExApi.Call(vm, 1, false, false))
                        {
                            ExApi.WriteErrorMessages(vm, ExErrorType.RUNTIME);
                            failed = true;
                            break;
                        }

                    }
                    else
                    {
                        ExApi.WriteErrorMessages(vm, ExErrorType.COMPILE);
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

                if (ExApi.CompileSource(vm, File.ReadAllText(fname)))
                {
                    ExApi.PushRootTable(vm);
                    if (!ExApi.Call(vm, 1, false, false))
                    {
                        ExApi.WriteErrorMessages(vm, ExErrorType.RUNTIME);
                        return vm.CleanReturn(nargs + 3, false);
                    }
                    else
                    {
                        return vm.CleanReturn(nargs + 3, true);
                    }
                }
                else
                {
                    ExApi.WriteErrorMessages(vm, ExErrorType.COMPILE);
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
                        return vm.AddToErrorMessage("use '" + ExMat.ReloadBaseLib + "' function to reload the std base functions");
                    }
                case "io":
                    {
                        if (nargs == 2)
                        {
                            if (fname != ExMat.ReloadLibFunc)
                            {
                                ExApi.PushRootTable(vm);
                                if (!ExApi.ReloadNativeFunction(vm, IOFuncs, fname, ExStdLibType.IO))
                                {
                                    return vm.AddToErrorMessage(string.Format("unknown IO function {0}", fname));
                                }
                            }
                        }
                        else if (!RegisterStdIO(vm))
                        {
                            break;
                        }
                        return vm.CleanReturn(nargs + 2, true);
                    }
                case "math":
                    {
                        if (nargs == 2)
                        {
                            ExApi.PushRootTable(vm);
                            if (!ExApi.ReloadNativeFunction(vm, ExStdMath.MathFuncs, fname, ExStdLibType.MATH))
                            {
                                return vm.AddToErrorMessage(string.Format("unknown Math function {0}", fname));
                            }
                        }
                        else if (!ExStdMath.RegisterStdMath(vm))
                        {
                            break;
                        }
                        return vm.CleanReturn(nargs + 2, true);
                    }
                case "string":
                    {
                        if (nargs == 2)
                        {
                            ExApi.PushRootTable(vm);
                            if (!ExApi.ReloadNativeFunction(vm, ExStdString.StringFuncs, fname, ExStdLibType.STRING))
                            {
                                return vm.AddToErrorMessage(string.Format("unknown String function {0}", fname));
                            }
                        }
                        else if (!ExStdString.RegisterStdString(vm))
                        {
                            break;
                        }
                        return vm.CleanReturn(nargs + 2, true);
                    }
                case "sys":
                    {
                        if (nargs == 2)
                        {
                            ExApi.PushRootTable(vm);
                            if (!ExApi.ReloadNativeFunction(vm, ExStdSys.SysFuncs, fname, ExStdLibType.SYSTEM))
                            {
                                return vm.AddToErrorMessage(string.Format("unknown Sys function {0}", fname));
                            }
                        }
                        else if (!ExStdSys.RegisterStdSys(vm))
                        {
                            break;
                        }
                        return vm.CleanReturn(nargs + 2, true);
                    }
                case "net":
                    {
                        if (nargs == 2)
                        {
                            ExApi.PushRootTable(vm);
                            if (!ExApi.ReloadNativeFunction(vm, ExStdNet.NetFuncs, fname, ExStdLibType.NETWORK))
                            {
                                return vm.AddToErrorMessage(string.Format("unknown Net function {0}", fname));
                            }
                        }
                        else if (!ExStdNet.RegisterStdNet(vm))
                        {
                            break;
                        }
                        return vm.CleanReturn(nargs + 2, true);
                    }
            }

            return vm.AddToErrorMessage("something went wrong...");
        }

        public static ExFunctionStatus IoReloadlibfunc(ExVM vm, int nargs)
        {
            string fname = vm.GetArgument(1).GetString();

            if (fname != ExMat.ReloadLibFunc
                && fname != ExMat.ReloadLib
                && ExApi.ReloadNativeFunction(vm, IOFuncs, fname, ExStdLibType.IO))
            {
                return ExFunctionStatus.VOID;
            }
            else if (ExApi.ReloadNativeFunction(vm, ExStdString.StringFuncs, fname, ExStdLibType.STRING))
            {
                return ExFunctionStatus.VOID;
            }
            else if (ExApi.ReloadNativeFunction(vm, ExStdMath.MathFuncs, fname, ExStdLibType.MATH))
            {
                return ExFunctionStatus.VOID;
            }
            else if (ExApi.ReloadNativeFunction(vm, ExStdNet.NetFuncs, fname, ExStdLibType.NETWORK))
            {
                return ExFunctionStatus.VOID;
            }
            else if (ExApi.ReloadNativeFunction(vm, ExStdSys.SysFuncs, fname, ExStdLibType.SYSTEM))
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

            try
            {
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
            catch (Exception e)
            {
                return vm.AddToErrorMessage("Error reading .xslx: " + e.Message);
            }
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

            try
            {
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
            catch (Exception e)
            {
                return vm.AddToErrorMessage("Error writing .xslx: " + e.Message);
            }
        }

        public static List<ExRegFunc> IOFuncs => _stdiofuncs;

        private static readonly List<ExRegFunc> _stdiofuncs = new()
        {
            new()
            {
                Name = "clear",
                Function = IoClear,
                Parameters = new(),
                Returns = ExObjType.BOOL,
                Safe = true,
                Description = "Clear the interactive console"
            },
            new()
            {
                Name = "paint_print",
                Function = IoPaint,
                Parameters = new()
                {
                    new("message", "s", "Message to print"),
                    new("background", "s", "Background color, one of console color values present in 'COLORS' dictionary", new("black")),
                    new("foreground", "s", "Foreground color, one of console color values present in 'COLORS' dictionary", new("white"))
                },
                Safe = true,
                Description = "Prints given string with given background and foreground colors"
            },
            new()
            {
                Name = "read_excel",
                Function = IoReadExcel,
                Parameters = new()
                {
                    new("path", "s", "File path to read"),
                    new("password", "s", "Password to use for reading, if there is any", new(string.Empty))
                },
                Returns = ExObjType.BOOL,
                Description = "Read an '.xslx' worksheet into a list of dictionaries with sheet names and list of lists"
            },
            new()
            {
                Name = "write_excel",
                Function = IoWriteExcel,
                Parameters = new()
                {
                    new("path", "s", "File path to write to"),
                    new("sheet_name", "s", "Sheet name in the file. If it already exists, it will get overwritten unless offset."),
                    new("rows_of_cols", "a", "List of lists to write as an excel sheet"),
                    new("rowoffset", "i", "File path to write to", new(0)),
                    new("coloffset", "i", "File path to write to", new(0))
                },
                Returns = ExObjType.BOOL,
                Description = "Write a list lists to/as an '.xslx' worksheet. Starting row and column can be offset"
            },
            new()
            {
                Name = "read_json",
                Function = IoReadjson,
                Parameters = new()
                {
                    new("path", "s", "File path to read"),
                    new("encoding", "s", "Encoding to use while reading", new(string.Empty))
                },
                Returns = ExObjType.DICT | ExObjType.ARRAY,
                Description = "Read a json file into dictionaries and lists"
            },
            new()
            {
                Name = "write_json",
                Function = IoWritejson,
                Parameters = new()
                {
                    new("path", "s", "File path to write to"),
                    new("object", ".", "Object to convert to json"),
                    new("encoding", "s", "Encoding to use while writing", new(string.Empty))
                },
                Returns = ExObjType.BOOL,
                Description = "Write an object as a json file"
            },
            new()
            {
                Name = "read_bytes",
                Function = IoReadfilebytes,
                Parameters = new()
                {
                    new("path", "s", "File path to read")
                },
                Safe = true,
                Returns = ExObjType.ARRAY | ExObjType.NULL,
                Description = "Read a file's contents as a bytes list"
            },
            new()
            {
                Name = "read_text",
                Function = IoReadfile,
                Parameters = new()
                {
                    new("path", "s", "File path to read"),
                    new("encoding", "s", "Encoding to use while reading", new(string.Empty))
                },
                Safe = true,
                Returns = ExObjType.STRING | ExObjType.NULL,
                Description = "Read a file's contents"
            },
            new()
            {
                Name = "read_lines",
                Function = IoReadfilelines,
                Parameters = new()
                {
                    new("path", "s", "File path to read"),
                    new("encoding", "s", "Encoding to use while reading", new(string.Empty))
                },
                Safe = true,
                Returns = ExObjType.ARRAY | ExObjType.NULL,
                Description = "Read a file's contents line by line into a list"
            },

            new()
            {
                Name = "write_bytes",
                Function = IoWritefilebytes,
                Parameters = new()
                {
                    new("path", "s", "File path to write"),
                    new("byte_list", "a", "Byte list to use")
                },
                Returns = ExObjType.BOOL,
                Description = "Write a byte list into a file"
            },
            new()
            {
                Name = "write_text",
                Function = IoWritefile,
                Parameters = new()
                {
                    new("path", "s", "File path to write"),
                    new("content", "s", "File content"),
                    new("encoding", "s", "Encoding of the content", new(string.Empty))
                },
                Returns = ExObjType.BOOL,
                Description = "Write a string into a file"
            },
            new()
            {
                Name = "write_lines",
                Function = IoWritefilelines,
                Parameters = new()
                {
                    new("path", "s", "File path to write"),
                    new("lines", "a", "File content lines list"),
                    new("encoding", "s", "Encoding of the content", new(string.Empty))
                },
                Returns = ExObjType.BOOL,
                Description = "Write a list of strings into a file as individual lines"
            },

            new()
            {
                Name = "append_text",
                Function = IoAppendfile,
                Parameters = new()
                {
                    new("path", "s", "File path to write"),
                    new("content", "s", "File content to append"),
                    new("encoding", "s", "Encoding of the content", new(string.Empty))
                },
                Returns = ExObjType.BOOL,
                Description = "Append a string into an existing file"
            },
            new()
            {
                Name = "append_lines",
                Function = IoAppendfilelines,
                Parameters = new()
                {
                    new("path", "s", "File path to append"),
                    new("lines", "a", "File content lines list"),
                    new("encoding", "s", "Encoding of the content", new(string.Empty))
                },
                Returns = ExObjType.BOOL,
                Description = "Append a list of strings to an existing file as individual lines"
            },

            new()
            {
                Name = "current_dir",
                Function = IoCurrentdir,
                Parameters = new(),
                Safe = true,
                Returns = ExObjType.STRING,
                Description = "Get current working directory"
            },
            new()
            {
                Name = "dir_content",
                Function = IoShowdir,
                Parameters = new()
                {
                    new("directory", "s", "Directory to get contents of", new(string.Empty))
                },
                Returns = ExObjType.ARRAY,
                Description = "Get a list of directories and files present in the given directory. If nothing given, current directory is used"
            },
            new()
            {
                Name = "change_dir",
                Function = IoChangedir,
                Parameters = new()
                {
                    new("directory", "s", "Directory to change into")
                },
                Safe = true,
                Returns = ExObjType.BOOL | ExObjType.STRING,
                Description = "Change directory into given directory."
            },
            new()
            {
                Name = "make_dir",
                Function = IoMkdir,
                Parameters = new()
                {
                    new("directory", "s", "Directory to create")
                },
                Safe = true,
                Returns = ExObjType.BOOL,
                Description = "Create a new directory. If directory already exists, returns false."
            },

            new()
            {
                Name = "raw_input",
                Function = IoRawinput,
                Parameters = new()
                {
                    new("message", "s", "Message to print before input starts", new(string.Empty))
                },
                Safe = true,
                Returns = ExObjType.STRING,
                Description = "Start getting the user input, stops until user pressed ENTER"
            },

            new()
            {
                Name = "raw_key",
                Function = IoRawinputkey,
                Parameters = new()
                {
                    new("message", "s", "Message to print before input starts", new(string.Empty)),
                    new("hide_key", ".", "Hide the key pressed", new(true))
                },
                Safe = true,
                Returns = ExObjType.STRING,
                Description = "Read the next key pressed by the user"
            },

            new()
            {
                Name = "file_exists",
                Function = IoFileexists,
                Parameters = new()
                {
                    new("file", "s", "File path to check")
                },
                Safe = true,
                Returns = ExObjType.BOOL,
                Description = "Check if a file exists"
            },
            new()
            {
                Name = "include_file",
                Function = IoIncludefile,
                Parameters = new()
                {
                    new("file", "s", "Script file with '.exmat' extension")
                },
                Returns = ExObjType.BOOL,
                Description = "Compile and execute an external '.exmat' file using the current VM"
            },
            new()
            {
                Name = ExMat.ReloadLib,
                Function = IoReloadlib,
                Parameters = new()
                {
                    new("library", "s", "Library to reload: io, math, string, net, system"),
                    new("function_name", "s", "A specific function from the library to reload alone", new(string.Empty))
                },
                Returns = ExObjType.BOOL,
                Description = "Reload a standard library or a function of a standard library"
            },
            new()
            {
                Name = ExMat.ReloadLibFunc,
                Function = IoReloadlibfunc,
                Parameters = new()
                {
                    new("library", "s", "Library to reload: io, math, string, net, system")
                },
                Returns = ExObjType.BOOL,
                Description = "Reload a standard library"
            }
        };

        private static void GetUserInput(ref string res, bool single = false, bool intercept = false)
        {
            char ch;
            if (single)
            {
                if (!char.IsControl(ch = Console.ReadKey(intercept).KeyChar))
                {
                    res = ch.ToString();
                }
            }
            else
            {
                StringBuilder s = new();
                while (!char.IsControl(ch = (char)Console.Read()))
                {
                    s.Append(ch);
                }
                res = s.ToString();
            }
        }

        public static ExFunctionStatus IoRawinput(ExVM vm, int nargs)
        {
            if (nargs == 1)
            {
                vm.Print(vm.GetArgument(1).GetString());
            }

            string input = string.Empty;
            GetUserInput(ref input);

            vm.GotUserInput = true;
            vm.PrintedToConsole = true;

            return vm.CleanReturn(nargs + 2, new ExObject(input));
        }

        public static ExFunctionStatus IoRawinputkey(ExVM vm, int nargs)
        {
            bool intercept = true;
            if (nargs >= 2)
            {
                intercept = vm.GetArgument(1).GetBool();
            }

            if (nargs >= 1)
            {
                vm.Print(vm.GetArgument(1).GetString());
            }

            string input = string.Empty;
            GetUserInput(ref input, true, intercept);

            vm.GotUserInput = true;
            vm.PrintedToConsole = true;

            return vm.CleanReturn(nargs + 2, new ExObject(input.ToString()));
        }

        public static bool RegisterStdIO(ExVM vm)
        {
            // For read_excel
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            ExApi.RegisterNativeFunctions(vm, IOFuncs, ExStdLibType.IO);

            return true;
        }
    }
}
