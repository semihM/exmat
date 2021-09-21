using System.Collections.Generic;

// Basic namespaces for common methods
using ExMat.API;
using ExMat.Attributes;
using ExMat.Objects;
using ExMat.VM;
//

namespace ExMat.StdLib                      // REQUIRED: Has to be in ExMat.StdLib namespace
{
    [ExStdLibBase(ExStdLibType.EXTERNAL)]   // REQUIRED: This should be selected as EXTERNAL
    [ExStdLibName("my_plugin_lib")]         // REQUIRED: Custom library name
    [ExStdLibRegister(nameof(Registery))]   // REQUIRED: Name of registery ExMat.StdLibRegistery property
    [ExStdLibConstDict(nameof(Constants))]  // OPTIONAL: Name of constants Dictionary<string,ExObject> property
    public static class SampleStdLibPlugin  // Static
    {
        /// <summary>
        /// An example native function named 'my_method' with 3 parameters: INTEGER param_1, [FLOAT param_2 = 3.14159], [ANY param_3 = "default_value"]
        /// <para>my_method is expected to return 'string'</para>
        /// </summary>
        /// <param name="vm">Virtual machine to use</param>
        /// <param name="nargs">Amount of arguments passed</param>
        /// <returns>On success <see cref="ExFunctionStatus.SUCCESS"/>
        /// <para>On error <see cref="ExFunctionStatus.ERROR"/></para>
        /// <para>On null <see cref="ExFunctionStatus.VOID"/></para></returns>
        [ExNativeFuncBase("my_method", ExBaseType.STRING, "Calls 'MyNativeMethod' and returns some string!")]
        [ExNativeParamBase(1, "param_1", ExBaseType.INTEGER, "An integer parameter")]
        [ExNativeParamBase(2, "param_2", ExBaseType.FLOAT, "A float parameter with default value", def: 3.14159)]
        [ExNativeParamBase(3, "param_3", ".", "A dynamic parameter accepting any type", def: "default_value")]
        public static ExFunctionStatus MyNativeMethod(ExVM vm, int nargs)
        {
            // Get arguments
            ExObject arg1 = vm.GetArgument(1);
            ExObject arg2 = vm.GetArgument(2);

            // Raise errors
            if (arg1.GetInt() < 0)
            {
                return vm.AddToErrorMessage("expected positive value for 'param_1'");
            }
            bool res = false;

            // Check argument count
            if (nargs == 3)
            {
                ExObject arg3 = vm.GetArgument(3);
                vm.PrintLine(ExApi.GetSimpleString(arg3));
            }

            // ExVM.CleanReturn to return values
            //  - First argument has to be (nargs + 2) to return values correctly
            return !ExApi.CheckEqual(arg1, arg2, ref res)
                    ? vm.CleanReturn(nargs + 2, "Something went wrong!")
                    : vm.CleanReturn(nargs + 2, res ? "Equivalent values!" : "Different values!");
        }

        // To allow vargs in a function, use overwriteParamCount
        //  - Number of minimum arguments required == (-overwriteParamCount - 1)
        [ExNativeFuncBase("vargs_method", ExBaseType.BOOL, "Returns true if any argument is passed, otherwise false", -1)]
        public static ExFunctionStatus VargsMethod(ExVM vm, int nargs)
        {
            ExObject[] vargs = ExApi.GetNObjects(vm, nargs);
            return vm.CleanReturn(nargs + 2, vargs.Length > 0);
        }

        // Require 1 arguments minimum with overwriteParamCount = -2
        //  - Type checks must be done inside the method
        [ExNativeFuncBase("vargs_method_with_param", ExBaseType.NULL, "Returns null, prints typeof first argument", -2)]
        public static ExFunctionStatus VargsMethodWithParameter(ExVM vm, int nargs)
        {
            ExObject arg1 = vm.GetArgument(1);

            // Manual type check
            if (ExTypeCheck.IsNumeric(arg1))
            {
                vm.PrintLine("argument 1 is numeric!");
            }
            else
            {
                vm.PrintLine("argument 1 is non-numeric!");
            }

            ExObject[] vargs = ExApi.GetNObjects(vm, nargs - 1);
            return vm.CleanReturn(nargs + 2, new ExObject());
        }

        // OPTIONAL
        //
        // Declare your own constants with a Dictionary<string, ExObject> property
        //  - Name of this property must be given in the class's ExStdLibConstDict attribute  
        public static Dictionary<string, ExObject> Constants => new()
        {
            { "myconst", new(666) },
            { "plugin_const", new("this value is a constant!") }
        };

        // REQUIRED
        //
        // Declare a registery property with ExMat.StdLibRegistery delegate
        // This delegate will be invoked before registering anything from the library
        //  - Name of this property must be given in the class's ExMat.StdLibRegistery attribute  
        public static ExMat.StdLibRegistery Registery => (ExVM vm) => true;
    }
}
