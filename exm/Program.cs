using System;
using ExMat.Compiler;
using ExMat.Objects;
using ExMat.VM;
using ExMat.BaseLib;
using ExMat.API;
using System.IO;

namespace ExMat
{
    class Program
    {
        static int VM_STACK_SIZE = 512;

        static int Main(string[] args)
        {
            args = new string[] { "sq.exe", "hello.nut" };

            int argc = args.Length;
            
            if (argc == 2)
            {
                ExVM vm = ExAPI.Start(VM_STACK_SIZE);
                ExAPI.PushRootTable(vm);

                string file = File.ReadAllText(args[1]);

                if (ExAPI.CompileFile(vm, file))
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
                        throw new Exception("error executing");
                    }
                }
                else
                {
                    throw new Exception("error compling");
                }
            }

            return -1;
        }
    }
}
