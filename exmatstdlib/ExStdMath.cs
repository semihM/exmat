using System;
using System.Collections.Generic;
using ExMat.API;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.BaseLib
{
    public static class ExStdMath
    {
        private static Random rand = new();

        public static int MATH_srand(ExVM vm, int nargs)
        {
            Rand = new(ExAPI.GetFromStack(vm, 2).GetInt());
            return 0;
        }

        public static int MATH_rand(ExVM vm, int nargs)
        {
            vm.Push(Rand.Next());
            return 1;
        }

        public static int MATH_abs(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER
                    ? Math.Abs(i.GetInt())
                    : Math.Abs(i.GetFloat()));
            return 1;
        }

        public static int MATH_sqrt(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER
                    ? (float)Math.Sqrt(i.GetInt())
                    : (float)Math.Sqrt(i.GetFloat()));
            return 1;
        }

        public static int MATH_sin(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER
                    ? (float)Math.Sin(i.GetInt())
                    : (float)Math.Sin(i.GetFloat()));
            return 1;
        }

        public static int MATH_cos(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER
                    ? (float)Math.Cos(i.GetInt())
                    : (float)Math.Cos(i.GetFloat()));
            return 1;
        }

        public static int MATH_tan(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER
                    ? (float)Math.Tan(i.GetInt())
                    : (float)Math.Tan(i.GetFloat()));
            return 1;
        }

        public static int MATH_cot(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER
                    ? (float)(1.0 / Math.Tan(i.GetInt()))
                    : (float)(1.0 / Math.Tan(i.GetFloat())));
            return 1;
        }

        public static int MATH_acos(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER
                    ? (float)Math.Acos(i.GetInt())
                    : (float)Math.Acos(i.GetFloat()));
            return 1;
        }

        public static int MATH_asin(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER
                    ? (float)Math.Asin(i.GetInt())
                    : (float)Math.Asin(i.GetFloat()));
            return 1;
        }

        public static int MATH_atan(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER
                    ? (float)Math.Atan(i.GetInt())
                    : (float)Math.Atan(i.GetFloat()));
            return 1;
        }

        public static int MATH_atan2(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            ExObjectPtr i2 = ExAPI.GetFromStack(vm, 3);

            if (i._type == ExObjType.INTEGER)
            {
                vm.Push(i2._type == ExObjType.INTEGER
                        ? (float)Math.Atan2(i.GetInt(), i2.GetInt())
                        : (float)Math.Atan2(i.GetInt(), i2.GetFloat()));
            }
            else
            {
                vm.Push(i2._type == ExObjType.INTEGER
                        ? (float)Math.Atan2(i.GetFloat(), i2.GetInt())
                        : (float)Math.Atan2(i.GetFloat(), i2.GetFloat()));
            }

            return 1;
        }

        public static int MATH_loge(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER
                    ? (float)Math.Log(i.GetInt())
                    : (float)Math.Log(i.GetFloat()));
            return 1;
        }

        public static int MATH_log2(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER
                    ? (float)Math.Log2(i.GetInt())
                    : (float)Math.Log2(i.GetFloat()));
            return 1;
        }

        public static int MATH_log10(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER
                    ? (float)Math.Log10(i.GetInt())
                    : (float)Math.Log10(i.GetFloat()));
            return 1;
        }

        public static int MATH_exp(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER
                    ? (float)Math.Exp(i.GetInt())
                    : (float)Math.Exp(i.GetFloat()));
            return 1;
        }

        public static int MATH_round(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            int dec = 0;
            if (nargs == 2)
            {
                dec = ExAPI.GetFromStack(vm, 3).GetInt();
            }

            vm.Push((float)(i._type == ExObjType.INTEGER
                    ? Math.Round((double)i.GetInt(), dec)
                    : Math.Round(i.GetFloat(), dec)));
            return 1;
        }

        public static int MATH_floor(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER ? i.GetInt() : (int)Math.Floor(i.GetFloat()));
            return 1;
        }

        public static int MATH_ceil(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            vm.Push(i._type == ExObjType.INTEGER ? i.GetInt() : (int)Math.Ceiling(i.GetFloat()));
            return 1;
        }

        public static int MATH_pow(ExVM vm, int nargs)
        {
            ExObjectPtr b = ExAPI.GetFromStack(vm, 2);
            ExObjectPtr e = ExAPI.GetFromStack(vm, 3);

            vm.Push((float)Math.Pow(
                b._type == ExObjType.INTEGER
                    ? b.GetInt()
                    : b.GetFloat(),
                e._type == ExObjType.INTEGER
                    ? e.GetInt()
                    : e.GetFloat()));

            return 1;
        }

        public static int MATH_isINF(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);

            vm.Push(float.IsFinite(i.GetFloat()));
            return 1;
        }

        public static int MATH_isNINF(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);

            vm.Push(float.IsNegativeInfinity(i.GetFloat()));
            return 1;
        }

        public static int MATH_isNAN(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);

            vm.Push(float.IsNaN(i.GetFloat()));
            return 1;
        }

        private static readonly List<ExRegFunc> _stdmathfuncs = new()
        {
            new() { name = "srand", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_srand")), n_pchecks = 2, mask = ".n" },
            new() { name = "rand", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_rand")), n_pchecks = 1, mask = null },
            new() { name = "abs", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_abs")), n_pchecks = 2, mask = ".n" },
            new() { name = "sqrt", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_sqrt")), n_pchecks = 2, mask = ".n" },
            new() { name = "sin", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_sin")), n_pchecks = 2, mask = ".n" },
            new() { name = "cos", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_cos")), n_pchecks = 2, mask = ".n" },
            new() { name = "tan", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_tan")), n_pchecks = 2, mask = ".n" },
            new() { name = "asin", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_asin")), n_pchecks = 2, mask = ".n" },
            new() { name = "acos", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_acos")), n_pchecks = 2, mask = ".n" },
            new() { name = "atan", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_atan")), n_pchecks = 2, mask = ".n" },
            new() { name = "atan2", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_atan2")), n_pchecks = 3, mask = ".nn" },
            new() { name = "loge", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_loge")), n_pchecks = 2, mask = ".n" },
            new() { name = "log2", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_log2")), n_pchecks = 2, mask = ".n" },
            new() { name = "log10", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_log10")), n_pchecks = 2, mask = ".n" },
            new() { name = "exp", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_exp")), n_pchecks = 2, mask = ".n" },
            new() { name = "round", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_round")), n_pchecks = -2, mask = ".nn" },
            new() { name = "floor", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_floor")), n_pchecks = 2, mask = ".n" },
            new() { name = "ceil", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_ceil")), n_pchecks = 2, mask = ".n" },
            new() { name = "pow", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_pow")), n_pchecks = 3, mask = ".nn" },
            new() { name = "isINF", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_isINF")), n_pchecks = 2, mask = ".n" },
            new() { name = "isNINF", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_isNINF")), n_pchecks = 2, mask = ".n" },
            new() { name = "isNAN", func = new(Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod("MATH_isNAN")), n_pchecks = 2, mask = ".n" },

            new() { name = string.Empty }
        };
        public static List<ExRegFunc> MathFuncs { get => _stdmathfuncs; }
        public static Random Rand { get => rand; set => rand = value; }

        public static bool RegisterStdMath(ExVM vm)
        {
            ExAPI.RegisterNativeFunctions(vm, MathFuncs);

            ExAPI.CreateConstantInt(vm, "RAND_MAX", int.MaxValue);
            ExAPI.CreateConstantFloat(vm, "RAND_MAXF", int.MaxValue);
            ExAPI.CreateConstantFloat(vm, "PI", (float)Math.PI);
            ExAPI.CreateConstantFloat(vm, "E", (float)Math.E);
            ExAPI.CreateConstantFloat(vm, "NAN", float.NaN);
            ExAPI.CreateConstantFloat(vm, "NINF", float.NegativeInfinity);
            ExAPI.CreateConstantFloat(vm, "INF", float.PositiveInfinity);

            return true;
        }
    }
}
