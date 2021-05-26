using System;
using System.IO;
using ExMat.API;
using ExMat.BaseLib;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat
{
    internal class Program
    {
        private static readonly int VM_STACK_SIZE = 20384;

        private static bool CheckCarryOver(string code)
        {
            return code.Length > 0 && code[^1] == '\\';
        }

        private static int CompileString(ExVM vm, string code)
        {
            int tp = vm._top - vm._stackbase;
            int ret = 0;

            if (ExAPI.CompileFile(vm, code))
            {
                ExAPI.PushRootTable(vm);
                if (ExAPI.Call(vm, 1, true, true))
                {
                    ExObjType type = vm._lastreturn._type;
                    if (type != ExObjType.NULL && !vm._printed)
                    {
                        ExObject s = new();
                        vm.ToString(vm._lastreturn, ref s);
                        Console.Write(s.GetString());
                    }
                }
                else
                {
                    ExAPI.WriteErrorMessages(vm, "EXECUTE");
                    ret = -1;
                }
            }
            else
            {
                ExAPI.WriteErrorMessages(vm, "COMPILE");
                ret = -1;
            }

            FixStackTopAfterCalls(vm, tp);
            vm._printed = false;
            vm._lastreturn.Assign(new ExObject());
            return ret;
        }

        private static void FixStackTopAfterCalls(ExVM vm, int t)
        {
            int curr = vm._top - vm._stackbase;
            if (curr > t)
            {
                vm.Pop(curr - t);
            }
            else
            {
                while (curr++ < t)
                {
                    vm._stack[vm._top++].Nullify();
                }
            }
        }

        private static void WriteVersion(ExVM vm)
        {
            string version = vm._rootdict.GetDict()["_version_"].GetString();
            string date = DateTime.Now.ToLongDateString() + " " + DateTime.Now.ToLongTimeString();
            int width = 60;
            int vlen = version.Length;
            int dlen = date.Length;

            Console.BackgroundColor = ConsoleColor.DarkGreen;

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(new string('/', width + 2));
            Console.Write("/");

            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write(new string(' ', (width - vlen) / 2) + version + new string(' ', (width - vlen) / 2));
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("/");
            Console.Write("/" + new string(' ', (width - dlen) / 2) + date + new string(' ', (width - dlen) / 2) + "/\n");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(new string('/', width + 2));

            Console.ResetColor();
        }

        private static void WriteOut(int count)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("OUT[");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(count);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("]: ");
            Console.ResetColor();
        }

        private static void WriteIn(int count)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            if (count > 0)
            {
                Console.Write("\n");
            }
            Console.Write("\nIN [");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(count);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("]: ");
            Console.ResetColor();
        }


        private static int Main(string[] args)
        {

            if (args.Length >= 1)
            {
                string f = File.Exists(args[0]) ? File.ReadAllText(args[0]) : string.Empty;

                if (string.IsNullOrWhiteSpace(f))
                {
                    return -1;
                }

                ExVM v = ExAPI.Start(VM_STACK_SIZE);
                ExAPI.PushRootTable(v);

                WriteVersion(v);

                ExStdMath.RegisterStdMath(v);
                ExStdIO.RegisterStdIO(v);
                ExStdString.RegisterStdString(v);

                return CompileString(v, f);
            }

            Console.Title = "[] ExMat Interactive";
            Console.ResetColor();

            ExVM vm = ExAPI.Start(VM_STACK_SIZE);
            ExAPI.PushRootTable(vm);

            ExStdMath.RegisterStdMath(vm);
            ExStdIO.RegisterStdIO(vm);
            ExStdString.RegisterStdString(vm);

            int count = 0;
            bool carryover = false;
            string code = string.Empty;

            ///////////
            WriteVersion(vm);
            ///////////

            while (true)
            {
                if (carryover)
                {
                    Console.Write("\t");
                    code += Console.ReadLine().TrimEnd(' ', '\t');
                    if (CheckCarryOver(code))
                    {
                        carryover = false;
                    }
                    code = code.TrimEnd('\\', ' ', '\t');
                }
                else
                {
                    ///////////
                    WriteIn(count);
                    ///////////

                    code = Console.ReadLine().TrimEnd(' ', '\t');

                    if (vm._got_input && string.IsNullOrWhiteSpace(code))
                    {
                        vm._got_input = false;
                        code = Console.ReadLine().TrimEnd(' ', '\t');
                    }

                    if (CheckCarryOver(code))
                    {
                        carryover = true;
                    }
                }

                if (carryover)
                {
                    code = code.TrimEnd('\\', ' ', '\t') + "\r\n";
                    continue;
                }

                ///////////
                WriteOut(count);
                ///////////

                carryover = false;
                count++;

                int ret = CompileString(vm, code);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                if (ret > 0)
                {
                    //return ret;
                }
            }
        }

        private static double GetSize<T>() where T : new()
        {
            long start_mem = GC.GetTotalMemory(true);

            T[] array = new T[10000000];
            for (int n = 0; n < 10000000; n++)
            {
                array[n] = new T();
            }

            return (GC.GetTotalMemory(false) - start_mem) / 10000000D;
        }
    }
}
