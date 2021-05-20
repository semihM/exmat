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
        private static readonly int VM_STACK_SIZE = 5096;

        private static bool CheckCarryOver(string code)
        {
            return code.Length > 0 && code[^1] == '\\';
        }

        private static int CompileString(ExVM vm, string code)
        {
            int tp = vm._top - vm._stackbase;
            int ret = -1;

            if (ExAPI.CompileFile(vm, code))
            {
                ExAPI.PushRootTable(vm);
                if (ExAPI.Call(vm, 1, true))
                {
                    ExObjType type = ExAPI.GetFromStack(vm, -1)._type;
                    if (type == ExObjType.INTEGER)  // TO-DO maybe always print an additional line?
                    {
                        ExObjectPtr t = ExAPI.GetFromStack(vm, -1);
                        if (t.IsNumeric())
                        {
                            ret = t.GetInt();
                        }
                    }
                    else
                    {
                        ret = 0;
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

        private static int Main(string[] args)
        {
            Console.ResetColor();
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
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("OUT[");
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(count);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("]: ");
                Console.ResetColor();
                ///////////

                carryover = false;
                count++;

                int ret = CompileString(vm, code);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                if (ret > 0)
                {
                    return ret;
                }
            }
        }
    }
}
