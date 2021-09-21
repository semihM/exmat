using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using ExcelDataReader;
using ExMat.API;
using ExMat.Attributes;
using ExMat.Objects;
using ExMat.VM;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ExMat.StdLib
{
    [ExStdLibBase(ExStdLibType.IO)]
    [ExStdLibName("io")]
    [ExStdLibRegister(nameof(Registery))]
    public static class ExStdIO
    {
        #region UTILITY
        private static readonly string Indentation = " ";

        private static string IndentPrefix(string prefix)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0}{1}", prefix, Indentation);
        }

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
                            prev = string.Format(CultureInfo.CurrentCulture, "{0}\"{1}\": {2}{3}", prefix, pair.Key, ConvertToJson(pair.Value, prev, IndentPrefix(prefix)), i != last ? ",\n" : "\n");
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
                            prev = string.Format(CultureInfo.CurrentCulture, "{0}{1}", ConvertToJson(o, prev, IndentPrefix(prefix)), i != last ? ", " : "\n");
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
                        prev += prefix + "\"" + obj.GetSpace().GetSpaceString() + "\"";
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

        private static void GetUserInput(ExVM vm, ref string res, bool single = false, bool intercept = false)
        {
            char ch;
            if (single)
            {
                if (!char.IsControl(ch = vm.KeyReader(intercept).KeyChar))
                {
                    res = ch.ToString(CultureInfo.CurrentCulture);
                }
            }
            else
            {
                StringBuilder s = new();
                while (!char.IsControl(ch = (char)vm.IntKeyReader()))
                {
                    s.Append(ch);
                }
                res = s.ToString();
            }
        }
        #endregion

        #region IO FUNCTIONS
        [ExNativeFuncBase("read_json", ExBaseType.DICT | ExBaseType.ARRAY, "Read a json file into dictionaries and lists")]
        [ExNativeParamBase(1, "path", "s", "File path to read")]
        [ExNativeParamBase(2, "encoding", "s", "Encoding to use while reading", def: "")]
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

        [ExNativeFuncBase("write_json", ExBaseType.BOOL, "Write an object as a json file")]
        [ExNativeParamBase(1, "path", "s", "File path to write to")]
        [ExNativeParamBase(2, "object", ".", "Object to convert to json")]
        [ExNativeParamBase(3, "encoding", "s", "Encoding to use while writing", def: "")]
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
                File.WriteAllText(i.EndsWith(".json", StringComparison.Ordinal) ? i : (i + ".json"), js, e);
                return vm.CleanReturn(nargs + 2, true);
            }
            catch (Exception err)
            {
                return vm.AddToErrorMessage(err.Message);
            }
        }

        [ExNativeFuncBase("clear", "Clear the interactive console")]
        public static ExFunctionStatus IoClear(ExVM vm, int nargs)
        {
            Console.Clear();
            return ExFunctionStatus.VOID;
        }

        [ExNativeFuncBase("paint_print", "Prints given string with given background and foreground colors")]
        [ExNativeParamBase(1, "message", "s", "Message to print")]
        [ExNativeParamBase(2, "background", "s", "Background color, one of console color values present in 'COLORS' dictionary", "black")]
        [ExNativeParamBase(3, "foreground", "s", "Foreground color, one of console color values present in 'COLORS' dictionary", "white")]
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

        [ExNativeFuncBase("write_text", ExBaseType.BOOL, "Write a string into a file")]
        [ExNativeParamBase(1, "path", "s", "File path to write")]
        [ExNativeParamBase(2, "content", "s", "File content")]
        [ExNativeParamBase(3, "encoding", "s", "Encoding of the content", def: "")]
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

        [ExNativeFuncBase("write_lines", ExBaseType.BOOL, "Write a list of strings into a file as individual lines")]
        [ExNativeParamBase(1, "path", "s", "File path to write")]
        [ExNativeParamBase(2, "lines", "a", "File content lines list")]
        [ExNativeParamBase(3, "encoding", "s", "Encoding of the content", def: "")]
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

            int n = lis.GetList().Count;

            string[] lines = new string[n];

            for (int l = 0; l < n; l++)
            {
                ExObject line = new();
                vm.ToString(lis.GetList()[l], ref line);
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

        [ExNativeFuncBase("write_bytes", ExBaseType.BOOL, "Write a byte list into a file")]
        [ExNativeParamBase(1, "path", "s", "File path to write")]
        [ExNativeParamBase(2, "byte_list", "a", "Byte list to use")]
        public static ExFunctionStatus IoWritefilebytes(ExVM vm, int nargs)
        {
            string i = vm.GetArgument(1).GetString();
            ExObject lis = vm.GetArgument(2);
            int n = lis.GetList().Count;

            byte[] bytes = new byte[n];

            for (int l = 0; l < n; l++)
            {
                ExObject b = new();

                if (!ExApi.ToInteger(vm, lis.GetList()[l], ref b))
                {
                    return vm.AddToErrorMessage("Failed to convert '{0}' to byte", ExApi.GetSimpleString(lis.GetList()[l]));
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

        [ExNativeFuncBase("append_text", ExBaseType.BOOL, "Append a string into an existing file")]
        [ExNativeParamBase(1, "path", "s", "File path to write")]
        [ExNativeParamBase(2, "content", "s", "File content to append")]
        [ExNativeParamBase(3, "encoding", "s", "Encoding of the content", def: "")]
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

        [ExNativeFuncBase("append_lines", ExBaseType.BOOL, "Append a list of strings to an existing file as individual lines")]
        [ExNativeParamBase(1, "path", "s", "File path to append")]
        [ExNativeParamBase(2, "lines", "a", "File content lines list")]
        [ExNativeParamBase(3, "encoding", "s", "Encoding of the content", def: "")]
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

            int n = lis.GetList().Count;

            string[] lines = new string[n];

            for (int l = 0; l < n; l++)
            {
                ExObject line = new();
                vm.ToString(lis.GetList()[l], ref line);
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

        [ExNativeFuncBase("read_text", ExBaseType.STRING | ExBaseType.NULL, "Read a file's contents")]
        [ExNativeParamBase(1, "path", "s", "File path to read")]
        [ExNativeParamBase(2, "encoding", "s", "Encoding to use while reading", def: "")]
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

        [ExNativeFuncBase("read_lines", ExBaseType.ARRAY | ExBaseType.NULL, "Read a file's contents line by line into a list")]
        [ExNativeParamBase(1, "path", "s", "File path to read")]
        [ExNativeParamBase(2, "encoding", "s", "Encoding to use while reading", def: "")]
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
            res.ValueCustom.l_List = l_list;

            return vm.CleanReturn(nargs + 2, res);
        }

        [ExNativeFuncBase("read_bytes", ExBaseType.ARRAY | ExBaseType.NULL, "Read a file's contents as a bytes list")]
        [ExNativeParamBase(1, "path", "s", "File path to read")]
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
            res.ValueCustom.l_List = blist;

            return vm.CleanReturn(nargs + 2, res);
        }

        [ExNativeFuncBase("file_exists", ExBaseType.BOOL, "Check if a file exists")]
        [ExNativeParamBase(1, "file", "s", "File path to check")]
        public static ExFunctionStatus IoFileexists(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, File.Exists(vm.GetArgument(1).GetString()));
        }

        [ExNativeFuncBase("current_dir", ExBaseType.STRING, "Get current working directory")]
        public static ExFunctionStatus IoCurrentdir(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, Directory.GetCurrentDirectory());
        }

        [ExNativeFuncBase("change_dir", ExBaseType.BOOL | ExBaseType.STRING, "Change directory into given directory.")]
        [ExNativeParamBase(1, "directory", "s", "Directory to change into")]
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

        [ExNativeFuncBase("make_dir", ExBaseType.BOOL, "Create a new directory. If directory already exists, returns false.")]
        [ExNativeParamBase(1, "directory", "s", "Directory to create")]
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

        [ExNativeFuncBase("dir_content", ExBaseType.ARRAY, "Get a list of directories and files present in the given directory. If nothing given, current directory is used")]
        [ExNativeParamBase(1, "directory", "s", "Directory to get contents of", def: "")]
        public static ExFunctionStatus IoShowdir(ExVM vm, int nargs)
        {
            string cd;
            if (nargs != 0)
            {
                cd = vm.GetArgument(1).GetString();
                if (!Directory.Exists(cd))
                {
                    return vm.AddToErrorMessage("Path '{0}' doesn't exist", cd);
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

        [ExNativeFuncBase("include_file", ExBaseType.BOOL, "Compile and execute an external '.exmat' file using the current VM. Returns false if there were any errors, otherwise true.")]
        [ExNativeParamBase(1, "file", "s", "Script file with '.exmat' extension")]
        [ExNativeParamBase(2, "write_errors", ".", "Wheter to write error messages", true)]
        [ExNativeParamBase(3, "stop_on_error", ".", "Wheter to stop when an error is thrown", true)]
        public static ExFunctionStatus IoIncludefile(ExVM vm, int nargs) // TO-DO Refactor
        {
            string fname = vm.GetArgument(1).GetString();
            bool werrors = nargs < 2 || vm.GetArgument(2).GetBool();
            if (fname == "*")
            {
                bool failed = false;
                bool stops = nargs < 3 || vm.GetArgument(3).GetBool();

                fname = Directory.GetCurrentDirectory();

                List<string> all = new(Directory.GetFiles(fname));

                foreach (string f in all)
                {
                    if (!f.EndsWith(".exmat", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (ExApi.CompileSource(vm, File.ReadAllText(f)))
                    {
                        ExApi.PushRootTable(vm);
                        if (!ExApi.Call(vm, 1, false, false))
                        {
                            failed = true;
                            if (werrors)
                            {
                                vm.AddToErrorMessage("File: '{0}'", f);
                                ExApi.WriteErrorMessages(vm, ExErrorType.RUNTIME);
                            }
                            else
                            {
                                ExApi.ClearErrorMessages(vm);
                            }

                            if (stops)
                            {
                                break;
                            }
                        }

                    }
                    else
                    {
                        failed = true;
                        if (werrors)
                        {
                            vm.AddToErrorMessage("File: '{0}'", f);
                            ExApi.WriteErrorMessages(vm, ExErrorType.COMPILE);
                        }
                        else
                        {
                            ExApi.ClearErrorMessages(vm);
                        }

                        if (stops)
                        {
                            break;
                        }
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
                        return vm.AddToErrorMessage("File '{0}' doesn't exist", fname);
                    }
                    fname += ".exmat";
                }

                if (ExApi.CompileSource(vm, File.ReadAllText(fname)))
                {
                    ExApi.PushRootTable(vm);
                    if (!ExApi.Call(vm, 1, false, false))
                    {
                        if (werrors)
                        {
                            vm.AddToErrorMessage("File: '{0}'", fname);
                            ExApi.WriteErrorMessages(vm, ExErrorType.RUNTIME);
                        }
                        else
                        {
                            ExApi.ClearErrorMessages(vm);
                        }
                        return vm.CleanReturn(nargs + 3, false);
                    }
                    else
                    {
                        return vm.CleanReturn(nargs + 3, true);
                    }
                }
                else
                {
                    if (werrors)
                    {
                        vm.AddToErrorMessage("File: '{0}'", fname);
                        ExApi.WriteErrorMessages(vm, ExErrorType.COMPILE);
                    }
                    else
                    {
                        ExApi.ClearErrorMessages(vm);
                    }
                    return vm.CleanReturn(nargs + 3, false);
                }
            }
        }

        [ExNativeFuncBase("read_excel", ExBaseType.BOOL, "Read an '.xslx' worksheet into a list of dictionaries with sheet names and list of lists")]
        [ExNativeParamBase(1, "path", "s", "File path to read")]
        [ExNativeParamBase(2, "password", "s", "Password to use for reading, if there is any", def: "")]
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

        [ExNativeFuncBase("write_excel", ExBaseType.BOOL, "Write a list lists to/as an '.xslx' worksheet. Starting row and column can be offset")]
        [ExNativeParamBase(1, "path", "s", "File path to write to")]
        [ExNativeParamBase(2, "sheet_name", "s", "Sheet name in the file. If it already exists, it will get overwritten unless offset.")]
        [ExNativeParamBase(3, "rows_of_cols", "a", "List of lists to write as an excel sheet")]
        [ExNativeParamBase(4, "rowoffset", "i", "File path to write to", 0)]
        [ExNativeParamBase(5, "coloffset", "i", "File path to write to", 0)]
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

        [ExNativeFuncBase("raw_input", ExBaseType.STRING, "Start getting the user input, stops until user pressed ENTER")]
        [ExNativeParamBase(1, "message", "s", "Message to print before input starts", def: "")]
        public static ExFunctionStatus IoRawinput(ExVM vm, int nargs)
        {
            if (nargs == 1)
            {
                vm.Print(vm.GetArgument(1).GetString());
            }

            string input = string.Empty;
            GetUserInput(vm, ref input);

            vm.GotUserInput = true;
            vm.PrintedToConsole = true;

            return vm.CleanReturn(nargs + 2, new ExObject(input));
        }

        [ExNativeFuncBase("raw_key", ExBaseType.STRING, "Read the next key pressed by the user")]
        [ExNativeParamBase(1, "message", "s", "Message to print before input starts", def: "")]
        [ExNativeParamBase(2, "hide_key", ".", "Hide the key pressed", true)]
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
            GetUserInput(vm, ref input, true, intercept);

            vm.GotUserInput = true;
            vm.PrintedToConsole = !intercept;

            return vm.CleanReturn(nargs + 2, new ExObject(input.ToString(CultureInfo.CurrentCulture)));
        }
        #endregion

        // MAIN

        public static ExMat.StdLibRegistery Registery => (ExVM vm) =>
        {
            // For read_excel
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            return true;
        };
    }
}
