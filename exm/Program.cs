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
        private static readonly int VM_STACK_SIZE = 10192;

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
                        ExObjectPtr s = new();
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
            vm._lastreturn.Assign(new ExObjectPtr());
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
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(new string('-', 60));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(vm._rootdict.GetDict()["_version_"].GetString());
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(new string('-', 60));
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
    }
}
