﻿using System;
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

        private static int Main(string[] args)
        {
            
            ExVM vm = ExAPI.Start(VM_STACK_SIZE);
            ExAPI.PushRootTable(vm);

            ExStdMath.RegisterStdMath(vm);
            ExStdIO.RegisterStdIO(vm);
            ExStdString.RegisterStdString(vm);

            int count = 0;
            bool carryover = false;
            string code = string.Empty;

            ///////////
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(vm._rootdict.GetDict()["_version_"].GetString());
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(new string('-',60));
            Console.ResetColor();
            ///////////
            while (true)
            {
                if(carryover)
                {
                    Console.Write("\t");
                    code += Console.ReadLine().TrimEnd(' ','\t');
                    if(CheckCarryOver(code))
                    {
                        carryover = false;
                    }
                    code = code.TrimEnd('\\', ' ', '\t');
                }
                else
                {
                    ///////////
                    Console.ForegroundColor = ConsoleColor.Green;
                    if(count > 0)
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

                if (ExAPI.CompileFile(vm, code))
                {
                    ExAPI.PushRootTable(vm);
                    if (ExAPI.Call(vm, 1, true))
                    {
                        ExObjType type = ExAPI.GetFromStack(vm, -1)._type;
                        if (type == ExObjType.INTEGER)
                        {
                            ExObjectPtr t = ExAPI.GetFromStack(vm, -1);
                            if (t.IsNumeric())
                            {
                                return t._val.i_Int;
                            }
                            return -1;
                        }
                    }
                    else
                    {
                        Console.WriteLine("\n\n+/+/+/+/+/+/+/+/+/+/+/+/+/+");
                        Console.WriteLine("FAILED TO EXECUTE");
                        Console.WriteLine(vm._error);
                        Console.WriteLine("+/+/+/+/+/+/+/+/+/+/+/+/+/+");
                        vm._error = string.Empty;
                    }
                }
                else
                {
                    Console.WriteLine("\n\n+/+/+/+/+/+/+/+/+/+/+/+/+/+");
                    Console.WriteLine("FAILED TO COMPILE");
                    Console.WriteLine(vm._error);
                    Console.WriteLine("+/+/+/+/+/+/+/+/+/+/+/+/+/+");
                    vm._error = string.Empty;
                }
            }
        }
    }
}
