using System;
using System.Collections.Generic;
using System.Reflection;
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
            int seed = ExAPI.GetFromStack(vm, 2).GetInt();
            vm.Pop(3);
            vm.Push(true);
            Rand = new(seed);
            return 0;
        }

        public static int MATH_rand(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 0:
                    {
                        vm.Pop(2);
                        vm.Push(Rand.Next());
                        break;
                    }
                case 1:
                    {
                        int i = ExAPI.GetFromStack(vm, 2).GetInt();
                        vm.Pop(3);
                        vm.Push(Rand.Next(i));
                        break;
                    }
                case 2:
                    {
                        int i = ExAPI.GetFromStack(vm, 2).GetInt();
                        int j = ExAPI.GetFromStack(vm, 3).GetInt();
                        vm.Pop(4);
                        vm.Push(Rand.Next(i, j));
                        break;
                    }
            }
            return 1;
        }

        public static int MATH_randf(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                case 0:
                    {
                        vm.Pop(2);
                        vm.Push(Rand.NextDouble());
                        break;
                    }
                case 1:
                    {
                        float i = ExAPI.GetFromStack(vm, 2).GetFloat();
                        vm.Pop(3);
                        vm.Push(Rand.NextDouble() * i);
                        break;
                    }
                case 2:
                    {
                        float min = ExAPI.GetFromStack(vm, 2).GetFloat();
                        float max = ExAPI.GetFromStack(vm, 3).GetFloat();
                        vm.Pop(4);
                        vm.Push((Rand.NextDouble() * (max - min)) + min);
                        break;
                    }
            }
            return 1;
        }

        public static int MATH_abs(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Abs(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Abs(o));
            }

            return 1;
        }

        public static int MATH_sqrt(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Sqrt(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Sqrt(o));
            }

            return 1;
        }

        public static int MATH_cbrt(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Cbrt(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Cbrt(o));
            }

            return 1;
        }
        public static int MATH_sin(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Sin(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Sin(o));
            }

            return 1;
        }
        public static int MATH_sinh(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Sinh(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Sinh(o));
            }

            return 1;
        }

        public static int MATH_cos(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Cos(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Cos(o));
            }

            return 1;
        }
        public static int MATH_cosh(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Cosh(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Cosh(o));
            }

            return 1;
        }

        public static int MATH_tan(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Tan(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Tan(o));
            }

            return 1;
        }
        public static int MATH_tanh(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Tanh(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Tanh(o));
            }

            return 1;
        }

        public static int MATH_acos(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Acos(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Acos(o));
            }

            return 1;
        }
        public static int MATH_acosh(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Acosh(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Acosh(o));
            }

            return 1;
        }

        public static int MATH_asin(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Asin(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Asin(o));
            }

            return 1;
        }
        public static int MATH_asinh(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Asinh(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Asinh(o));
            }

            return 1;
        }

        public static int MATH_atan(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Atan(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Atan(o));
            }

            return 1;
        }
        public static int MATH_atanh(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Atanh(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Atanh(o));
            }

            return 1;
        }

        public static int MATH_atan2(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            ExObjectPtr i2 = ExAPI.GetFromStack(vm, 3);

            if (i._type == ExObjType.INTEGER)
            {
                int b = i.GetInt();
                if (i2._type == ExObjType.INTEGER)
                {
                    int o = i2.GetInt();
                    vm.Pop(nargs + 2);
                    vm.Push((float)Math.Atan2(b, o));
                }
                else
                {
                    float o = i2.GetFloat();
                    vm.Pop(nargs + 2);
                    vm.Push((float)Math.Atan2(b, o));
                }

            }
            else
            {
                float b = i.GetFloat();
                if (i2._type == ExObjType.INTEGER)
                {
                    int o = i2.GetInt();
                    vm.Pop(nargs + 2);
                    vm.Push((float)Math.Atan2(b, o));
                }
                else
                {
                    float o = i2.GetFloat();
                    vm.Pop(nargs + 2);
                    vm.Push((float)Math.Atan2(b, o));
                }
            }

            return 1;
        }

        public static int MATH_loge(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Log(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Log(o));
            }

            return 1;
        }

        public static int MATH_log2(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Log2(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Log2(o));
            }

            return 1;
        }

        public static int MATH_log10(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Log10(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Log10(o));
            }

            return 1;
        }

        public static int MATH_exp(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Exp(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Exp(o));
            }

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

            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push((float)Math.Round((double)o, dec));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push((float)Math.Round(o, dec));
            }

            return 1;
        }

        public static int MATH_floor(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(o);
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push((int)Math.Floor(o));
            }

            return 1;
        }

        public static int MATH_ceil(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(o);
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push((int)Math.Ceiling(o));
            }

            return 1;
        }

        public static int MATH_pow(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            ExObjectPtr i2 = ExAPI.GetFromStack(vm, 3);

            if (i._type == ExObjType.INTEGER)
            {
                int b = i.GetInt();
                if (i2._type == ExObjType.INTEGER)
                {
                    int o = i2.GetInt();
                    vm.Pop(nargs + 2);
                    vm.Push((int)Math.Pow(b, o));
                }
                else
                {
                    float o = i2.GetFloat();
                    vm.Pop(nargs + 2);
                    vm.Push((float)Math.Pow(b, o));
                }

            }
            else
            {
                float b = i.GetFloat();
                if (i2._type == ExObjType.INTEGER)
                {
                    int o = i2.GetInt();
                    vm.Pop(nargs + 2);
                    vm.Push((float)Math.Pow(b, o));
                }
                else
                {
                    float o = i2.GetFloat();
                    vm.Pop(nargs + 2);
                    vm.Push((float)Math.Pow(b, o));
                }
            }

            return 1;
        }

        public static int MATH_min(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            ExObjectPtr i2 = ExAPI.GetFromStack(vm, 3);

            if (i._type == ExObjType.INTEGER)
            {
                int b = i.GetInt();
                if (i2._type == ExObjType.INTEGER)
                {
                    int o = i2.GetInt();
                    vm.Pop(nargs + 2);
                    vm.Push(Math.Min(b, o));
                }
                else
                {
                    float o = i2.GetFloat();
                    vm.Pop(nargs + 2);
                    vm.Push((float)Math.Min(b, o));
                }

            }
            else
            {
                float b = i.GetFloat();
                if (i2._type == ExObjType.INTEGER)
                {
                    int o = i2.GetInt();
                    vm.Pop(nargs + 2);
                    vm.Push((float)Math.Min(b, o));
                }
                else
                {
                    float o = i2.GetFloat();
                    vm.Pop(nargs + 2);
                    vm.Push((float)Math.Min(b, o));
                }
            }

            return 1;
        }

        public static int MATH_max(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);
            ExObjectPtr i2 = ExAPI.GetFromStack(vm, 3);

            if (i._type == ExObjType.INTEGER)
            {
                int b = i.GetInt();
                if (i2._type == ExObjType.INTEGER)
                {
                    int o = i2.GetInt();
                    vm.Pop(nargs + 2);
                    vm.Push(Math.Max(b, o));
                }
                else
                {
                    float o = i2.GetFloat();
                    vm.Pop(nargs + 2);
                    vm.Push((float)Math.Max(b, o));
                }

            }
            else
            {
                float b = i.GetFloat();
                if (i2._type == ExObjType.INTEGER)
                {
                    int o = i2.GetInt();
                    vm.Pop(nargs + 2);
                    vm.Push((float)Math.Max(b, o));
                }
                else
                {
                    float o = i2.GetFloat();
                    vm.Pop(nargs + 2);
                    vm.Push((float)Math.Max(b, o));
                }
            }

            return 1;
        }

        public static int MATH_sum(ExVM vm, int nargs)
        {
            ExObjectPtr sum = new((float)0);

            ExObjectPtr[] args = ExAPI.GetNObjects(vm, nargs);

            for (int i = 0; i < nargs; i++)
            {
                ExObjectPtr res = new();
                if (vm.DoArithmeticOP(OPs.OPC.ADD, args[i], sum, ref res))
                {
                    sum._val.f_Float = res.GetFloat();
                }
                else
                {
                    return -1;
                }
            }

            vm.Pop(nargs + 2);
            vm.Push(sum);
            return 1;
        }

        public static int MATH_mul(ExVM vm, int nargs)
        {
            ExObjectPtr mul = new((float)1);

            ExObjectPtr[] args = ExAPI.GetNObjects(vm, nargs);

            for (int i = 0; i < nargs; i++)
            {
                ExObjectPtr res = new();
                if (vm.DoArithmeticOP(OPs.OPC.MLT, args[i], mul, ref res))
                {
                    mul._val.f_Float = res.GetFloat();
                }
                else
                {
                    return -1;
                }
            }

            vm.Pop(nargs + 2);
            vm.Push(mul);
            return 1;
        }

        public static int MATH_sign(ExVM vm, int nargs)
        {
            ExObjectPtr i = ExAPI.GetFromStack(vm, 2);

            if (i._type == ExObjType.INTEGER)
            {
                int o = i.GetInt();
                vm.Pop(nargs + 2);
                vm.Push(Math.Sign(o));
            }
            else
            {
                float o = i.GetFloat();
                vm.Pop(nargs + 2);
                vm.Push(Math.Sign(o));
            }

            return 1;
        }

        public static int MATH_isINF(ExVM vm, int nargs)
        {
            bool i = float.IsFinite(ExAPI.GetFromStack(vm, 2).GetFloat());

            vm.Pop(nargs + 2);
            vm.Push(i);
            return 1;
        }

        public static int MATH_isNINF(ExVM vm, int nargs)
        {
            bool i = float.IsNegativeInfinity(ExAPI.GetFromStack(vm, 2).GetFloat());

            vm.Pop(nargs + 2);
            vm.Push(i);
            return 1;
        }

        public static int MATH_isNAN(ExVM vm, int nargs)
        {
            bool i = float.IsNaN(ExAPI.GetFromStack(vm, 2).GetFloat());

            vm.Pop(nargs + 2);
            vm.Push(i);
            return 1;
        }

        public static MethodInfo GetStdMathMethod(string name)
        {
            return Type.GetType("ExMat.BaseLib.ExStdMath").GetMethod(name);
        }

        private static readonly List<ExRegFunc> _stdmathfuncs = new()
        {
            new()
            {
                name = "srand",
                func = new(GetStdMathMethod("MATH_srand")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "rand",
                func = new(GetStdMathMethod("MATH_rand")),
                n_pchecks = -1,
                mask = ".nn",
                d_defaults = new()
                {
                    { 1, new(0) },
                    { 2, new(int.MaxValue) }
                }
            },
            new()
            {
                name = "randf",
                func = new(GetStdMathMethod("MATH_randf")),
                n_pchecks = -1,
                mask = ".nn",
                d_defaults = new()
                {
                    { 1, new(0) },
                    { 2, new(1) }
                }
            },

            new()
            {
                name = "abs",
                func = new(GetStdMathMethod("MATH_abs")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "sqrt",
                func = new(GetStdMathMethod("MATH_sqrt")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "cbrt",
                func = new(GetStdMathMethod("MATH_cbrt")),
                n_pchecks = 2,
                mask = ".n"
            },

            new()
            {
                name = "sin",
                func = new(GetStdMathMethod("MATH_sin")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "cos",
                func = new(GetStdMathMethod("MATH_cos")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "tan",
                func = new(GetStdMathMethod("MATH_tan")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "sinh",
                func = new(GetStdMathMethod("MATH_sinh")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "cosh",
                func = new(GetStdMathMethod("MATH_cosh")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "tanh",
                func = new(GetStdMathMethod("MATH_tanh")),
                n_pchecks = 2,
                mask = ".n"
            },

            new()
            {
                name = "asin",
                func = new(GetStdMathMethod("MATH_asin")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "acos",
                func = new(GetStdMathMethod("MATH_acos")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "atan",
                func = new(GetStdMathMethod("MATH_atan")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "atan2",
                func = new(GetStdMathMethod("MATH_atan2")),
                n_pchecks = 3,
                mask = ".nn"
            },
            new()
            {
                name = "asinh",
                func = new(GetStdMathMethod("MATH_asinh")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "acosh",
                func = new(GetStdMathMethod("MATH_acosh")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "atanh",
                func = new(GetStdMathMethod("MATH_atanh")),
                n_pchecks = 2,
                mask = ".n"
            },

            new()
            {
                name = "loge",
                func = new(GetStdMathMethod("MATH_loge")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "log2",
                func = new(GetStdMathMethod("MATH_log2")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "log10",
                func = new(GetStdMathMethod("MATH_log10")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "exp",
                func = new(GetStdMathMethod("MATH_exp")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "round",
                func = new(GetStdMathMethod("MATH_round")),
                n_pchecks = -2,
                mask = ".nn",
                d_defaults = new()
                {
                    { 2, new(0) }
                }
            },
            new()
            {
                name = "floor",
                func = new(GetStdMathMethod("MATH_floor")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "ceil",
                func = new(GetStdMathMethod("MATH_ceil")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "pow",
                func = new(GetStdMathMethod("MATH_pow")),
                n_pchecks = 3,
                mask = ".nn"
            },

            new()
            {
                name = "sum",
                func = new(GetStdMathMethod("MATH_sum")),
                n_pchecks = -1,
                mask = null
            },
            new()
            {
                name = "mul",
                func = new(GetStdMathMethod("MATH_mul")),
                n_pchecks = -1,
                mask = null
            },

            new()
            {
                name = "min",
                func = new(GetStdMathMethod("MATH_min")),
                n_pchecks = 3,
                mask = ".nn"
            },
            new()
            {
                name = "max",
                func = new(GetStdMathMethod("MATH_max")),
                n_pchecks = 3,
                mask = ".nn"
            },
            new()
            {
                name = "sign",
                func = new(GetStdMathMethod("MATH_sign")),
                n_pchecks = 2,
                mask = ".n"
            },

            new()
            {
                name = "isINF",
                func = new(GetStdMathMethod("MATH_isINF")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "isNINF",
                func = new(GetStdMathMethod("MATH_isNINF")),
                n_pchecks = 2,
                mask = ".n"
            },
            new()
            {
                name = "isNAN",
                func = new(GetStdMathMethod("MATH_isNAN")),
                n_pchecks = 2,
                mask = ".n"
            },

            new() { name = string.Empty }
        };
        public static List<ExRegFunc> MathFuncs { get => _stdmathfuncs; }
        public static Random Rand { get => rand; set => rand = value; }

        public static bool RegisterStdMath(ExVM vm, bool force = false)
        {
            ExAPI.RegisterNativeFunctions(vm, MathFuncs, force);

            ExAPI.CreateConstantInt(vm, "INT_MAX", int.MaxValue);
            ExAPI.CreateConstantInt(vm, "INT_MIN", int.MinValue);
            ExAPI.CreateConstantFloat(vm, "INT_MAXF", int.MaxValue);
            ExAPI.CreateConstantFloat(vm, "INT_MINF", int.MinValue);
            ExAPI.CreateConstantFloat(vm, "FLOAT_MAX", float.MaxValue);
            ExAPI.CreateConstantFloat(vm, "FLOAT_MIN", float.MinValue);
            ExAPI.CreateConstantFloat(vm, "TAU", (float)Math.Tau);
            ExAPI.CreateConstantFloat(vm, "PI", (float)Math.PI);
            ExAPI.CreateConstantFloat(vm, "E", (float)Math.E);
            ExAPI.CreateConstantFloat(vm, "NAN", float.NaN);
            ExAPI.CreateConstantFloat(vm, "NINF", float.NegativeInfinity);
            ExAPI.CreateConstantFloat(vm, "INF", float.PositiveInfinity);

            return true;
        }
    }
}
