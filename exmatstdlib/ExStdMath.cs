﻿using System;
using System.Collections.Generic;
using System.Numerics;
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
            int seed = (int)ExAPI.GetFromStack(vm, 2).GetInt();
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
                        int i = (int)ExAPI.GetFromStack(vm, 2).GetInt();
                        i = i < 0 ? (i > int.MinValue ? Math.Abs(i) : 0) : i;

                        vm.Pop(3);
                        vm.Push(Rand.Next(i));
                        break;
                    }
                case 2:
                    {
                        int i = (int)ExAPI.GetFromStack(vm, 2).GetInt();
                        int j = (int)ExAPI.GetFromStack(vm, 3).GetInt();
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
                        double i = ExAPI.GetFromStack(vm, 2).GetFloat();
                        vm.Pop(3);
                        vm.Push(Rand.NextDouble() * i);
                        break;
                    }
                case 2:
                    {
                        double min = ExAPI.GetFromStack(vm, 2).GetFloat();
                        double max = ExAPI.GetFromStack(vm, 3).GetFloat();
                        vm.Pop(4);
                        vm.Push((Rand.NextDouble() * (max - min)) + min);
                        break;
                    }
            }
            return 1;
        }

        public static int MATH_abs(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        o = o < 0 ? o > long.MinValue ? Math.Abs(o) : 0 : Math.Abs(o);
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        double o = Complex.Abs(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Abs(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_sqrt(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Sqrt(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Sqrt(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Sqrt(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_cbrt(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Cbrt(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Pow(i.GetComplex(), 1.0 / 3.0);
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Cbrt(o));
                        break;
                    }
            }

            return 1;
        }
        public static int MATH_sin(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Sin(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Sin(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Sin(o));
                        break;
                    }
            }

            return 1;
        }
        public static int MATH_sinh(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Sinh(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Sinh(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Sinh(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_cos(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Cos(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Cos(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Cos(o));
                        break;
                    }
            }

            return 1;
        }
        public static int MATH_cosh(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Cosh(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Cosh(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Cosh(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_tan(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Tan(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Tan(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Tan(o));
                        break;
                    }
            }

            return 1;
        }
        public static int MATH_tanh(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Tanh(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Tanh(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Tanh(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_acos(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Acos(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Acos(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Acos(o));
                        break;
                    }
            }

            return 1;
        }
        public static int MATH_acosh(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Acosh(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        if (i._val.c_Float == 0.0)
                        {
                            double o = i._val.f_Float;
                            vm.Pop(nargs + 2);
                            vm.Push(Math.Acosh(o));
                            break;
                        }
                        else
                        {
                            vm.AddToErrorMessage("can't use complex numbers with acosh");
                            return -1;
                        }
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Acosh(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_asin(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Asin(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Asin(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Asin(o));
                        break;
                    }
            }

            return 1;
        }
        public static int MATH_asinh(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Asinh(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        if (i._val.c_Float == 0.0)
                        {
                            double o = i._val.f_Float;
                            vm.Pop(nargs + 2);
                            vm.Push(Math.Asinh(o));
                            break;
                        }
                        else
                        {
                            vm.AddToErrorMessage("can't use complex numbers with asinh");
                            return -1;
                        }
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Asinh(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_atan(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Atan(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Atan(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Atan(o));
                        break;
                    }
            }

            return 1;
        }
        public static int MATH_atanh(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Atanh(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        if (i._val.c_Float == 0.0)
                        {
                            double o = i._val.f_Float;
                            vm.Pop(nargs + 2);
                            vm.Push(Math.Atanh(o));
                            break;
                        }
                        else
                        {
                            vm.AddToErrorMessage("can't use complex numbers with atanh");
                            return -1;
                        }
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Atanh(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_atan2(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            ExObject i2 = ExAPI.GetFromStack(vm, 3);
            double b = 0.0;
            long l = 0;

            switch (i._type)    // TO-DO refactor
            {
                case ExObjType.INTEGER:
                    {
                        l = i.GetInt();
                        switch (i2._type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    long o = i2.GetInt();
                                    vm.Pop(nargs + 2);
                                    vm.Push((double)Math.Atan2(l, o));
                                    break;
                                }
                            case ExObjType.COMPLEX:
                                {
                                    if (i2._val.c_Float == 0.0)
                                    {
                                        double o = i2._val.f_Float;
                                        vm.Pop(nargs + 2);
                                        vm.Push((double)Math.Atan2(l, o));
                                        break;
                                    }
                                    else
                                    {
                                        vm.AddToErrorMessage("can't use complex numbers with atan2");
                                        return -1;
                                    }
                                }
                            default:
                                {
                                    double o = i2.GetFloat();
                                    vm.Pop(nargs + 2);
                                    vm.Push((double)Math.Atan2(l, o));
                                    break;
                                }
                        }

                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        if (i._val.c_Float == 0.0)
                        {
                            b = i._val.f_Float;
                            goto default;
                        }
                        else
                        {
                            vm.AddToErrorMessage("can't use complex numbers with atan2");
                            return -1;
                        }
                    }
                case ExObjType.FLOAT:
                    {
                        b = i.GetFloat();
                        goto default;
                    }
                default:
                    {
                        switch (i2._type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    long o = i2.GetInt();
                                    vm.Pop(nargs + 2);
                                    vm.Push((double)Math.Atan2(b, o));
                                    break;
                                }
                            case ExObjType.COMPLEX:
                                {
                                    if (i2._val.c_Float == 0.0)
                                    {
                                        double o = i2._val.f_Float;
                                        vm.Pop(nargs + 2);
                                        vm.Push((double)Math.Atan2(b, o));
                                        break;
                                    }
                                    else
                                    {
                                        vm.AddToErrorMessage("can't use complex numbers with atan2");
                                        return -1;
                                    }
                                }
                            default:
                                {
                                    double o = i2.GetFloat();
                                    vm.Pop(nargs + 2);
                                    vm.Push((double)Math.Atan2(b, o));
                                    break;
                                }
                        }

                        break;
                    }
            }

            return 1;
        }

        public static int MATH_loge(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Log(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Log(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Log(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_log2(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Log2(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Log(i.GetComplex()) / Math.Log(2.0);
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Log2(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_log10(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Log10(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Log10(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Log10(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_exp(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Exp(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Exp(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Exp(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_round(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            int dec = 0;
            if (nargs == 2)
            {
                dec = (int)ExAPI.GetFromStack(vm, 3).GetInt();
            }

            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push((double)Math.Round((double)o, dec));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = i.GetComplex();
                        vm.Pop(nargs + 2);
                        vm.Push(new Complex(Math.Round(o.Real, dec), Math.Round(o.Imaginary, dec)));
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push((double)Math.Round(o, dec));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_floor(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = i.GetComplex();
                        vm.Pop(nargs + 2);
                        vm.Push(new Complex(Math.Floor(o.Real), Math.Floor(o.Imaginary)));
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push((int)Math.Floor(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_ceil(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = i.GetComplex();
                        vm.Pop(nargs + 2);
                        vm.Push(new Complex(Math.Ceiling(o.Real), Math.Ceiling(o.Imaginary)));
                        break;
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push((int)Math.Ceiling(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_pow(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            ExObject i2 = ExAPI.GetFromStack(vm, 3);

            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long b = i.GetInt();
                        switch (i2._type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    long o = i2.GetInt();
                                    vm.Pop(nargs + 2);
                                    vm.Push((int)Math.Pow(b, o));
                                    break;
                                }
                            case ExObjType.COMPLEX:
                                {
                                    Complex o = Complex.Pow(b, i2.GetComplex());
                                    vm.Pop(nargs + 2);
                                    vm.Push(o);
                                    break;
                                }
                            default:
                                {
                                    double o = i2.GetFloat();
                                    vm.Pop(nargs + 2);
                                    vm.Push((double)Math.Pow(b, o));
                                    break;
                                }
                        }

                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex b = i.GetComplex();
                        switch (i2._type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    Complex o = Complex.Pow(b, i2.GetInt());
                                    vm.Pop(nargs + 2);
                                    vm.Push(o);
                                    break;
                                }
                            case ExObjType.COMPLEX:
                                {
                                    Complex o = Complex.Pow(b, i2.GetComplex());
                                    vm.Pop(nargs + 2);
                                    vm.Push(o);
                                    break;
                                }
                            default:
                                {
                                    Complex o = Complex.Pow(b, i2.GetFloat());
                                    vm.Pop(nargs + 2);
                                    vm.Push(o);
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        double b = i.GetFloat();
                        switch (i2._type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    long o = i2.GetInt();
                                    vm.Pop(nargs + 2);
                                    vm.Push((double)Math.Pow(b, o));
                                    break;
                                }
                            case ExObjType.COMPLEX:
                                {
                                    Complex o = Complex.Pow(b, i2.GetComplex());
                                    vm.Pop(nargs + 2);
                                    vm.Push(o);
                                    break;
                                }
                            default:
                                {
                                    double o = i2.GetFloat();
                                    vm.Pop(nargs + 2);
                                    vm.Push((double)Math.Pow(b, o));
                                    break;
                                }
                        }

                        break;
                    }
            }

            return 1;
        }

        public static int MATH_min(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            ExObject i2 = ExAPI.GetFromStack(vm, 3);

            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long b = i.GetInt();
                        switch (i2._type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    long o = i2.GetInt();
                                    vm.Pop(nargs + 2);
                                    vm.Push(Math.Min(b, o));
                                    break;
                                }
                            case ExObjType.COMPLEX:
                                {
                                    if (i2._val.c_Float == 0.0)
                                    {
                                        double o = i2._val.f_Float;
                                        vm.Pop(nargs + 2);
                                        vm.Push((double)Math.Min(b, o));
                                        break;
                                    }
                                    else
                                    {
                                        vm.AddToErrorMessage("can't compare complex numbers");
                                        return -1;
                                    }
                                }
                            default:
                                {
                                    double o = i2.GetFloat();
                                    vm.Pop(nargs + 2);
                                    vm.Push((double)Math.Min(b, o));
                                    break;
                                }
                        }

                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        if (i._val.c_Float == 0.0)
                        {
                            double b = i._val.f_Float;
                            switch (i2._type)
                            {
                                case ExObjType.INTEGER:
                                    {
                                        long o = i2.GetInt();
                                        vm.Pop(nargs + 2);
                                        vm.Push(Math.Min(b, o));
                                        break;
                                    }
                                case ExObjType.COMPLEX:
                                    {
                                        if (i2._val.c_Float == 0.0)
                                        {
                                            double o = i2._val.f_Float;
                                            vm.Pop(nargs + 2);
                                            vm.Push((double)Math.Min(b, o));
                                            break;
                                        }
                                        else
                                        {
                                            vm.AddToErrorMessage("can't compare complex numbers");
                                            return -1;
                                        }
                                    }
                                default:
                                    {
                                        double o = i2.GetFloat();
                                        vm.Pop(nargs + 2);
                                        vm.Push((double)Math.Min(b, o));
                                        break;
                                    }
                            }
                            break;
                        }
                        else
                        {
                            vm.AddToErrorMessage("can't compare complex numbers");
                            return -1;
                        }
                    }
                default:
                    {
                        double b = i.GetFloat();
                        switch (i2._type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    long o = i2.GetInt();
                                    vm.Pop(nargs + 2);
                                    vm.Push((double)Math.Min(b, o));
                                    break;
                                }
                            case ExObjType.COMPLEX:
                                {
                                    if (i2._val.c_Float == 0.0)
                                    {
                                        double o = i2._val.f_Float;
                                        vm.Pop(nargs + 2);
                                        vm.Push((double)Math.Min(b, o));
                                        break;
                                    }
                                    else
                                    {
                                        vm.AddToErrorMessage("can't compare complex numbers");
                                        return -1;
                                    }
                                }
                            default:
                                {
                                    double o = i2.GetFloat();
                                    vm.Pop(nargs + 2);
                                    vm.Push((double)Math.Min(b, o));
                                    break;
                                }
                        }

                        break;
                    }
            }

            return 1;
        }

        public static int MATH_max(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            ExObject i2 = ExAPI.GetFromStack(vm, 3);

            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long b = i.GetInt();
                        switch (i2._type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    long o = i2.GetInt();
                                    vm.Pop(nargs + 2);
                                    vm.Push(Math.Max(b, o));
                                    break;
                                }
                            case ExObjType.COMPLEX:
                                {
                                    if (i2._val.c_Float == 0.0)
                                    {
                                        double o = i2._val.f_Float;
                                        vm.Pop(nargs + 2);
                                        vm.Push((double)Math.Max(b, o));
                                        break;
                                    }
                                    else
                                    {
                                        vm.AddToErrorMessage("can't compare complex numbers");
                                        return -1;
                                    }
                                }
                            default:
                                {
                                    double o = i2.GetFloat();
                                    vm.Pop(nargs + 2);
                                    vm.Push((double)Math.Max(b, o));
                                    break;
                                }
                        }

                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        if (i._val.c_Float == 0.0)
                        {
                            double b = i._val.f_Float;
                            switch (i2._type)
                            {
                                case ExObjType.INTEGER:
                                    {
                                        long o = i2.GetInt();
                                        vm.Pop(nargs + 2);
                                        vm.Push(Math.Max(b, o));
                                        break;
                                    }
                                case ExObjType.COMPLEX:
                                    {
                                        if (i2._val.c_Float == 0.0)
                                        {
                                            double o = i2._val.f_Float;
                                            vm.Pop(nargs + 2);
                                            vm.Push((double)Math.Max(b, o));
                                            break;
                                        }
                                        else
                                        {
                                            vm.AddToErrorMessage("can't compare complex numbers");
                                            return -1;
                                        }
                                    }
                                default:
                                    {
                                        double o = i2.GetFloat();
                                        vm.Pop(nargs + 2);
                                        vm.Push((double)Math.Max(b, o));
                                        break;
                                    }
                            }
                            break;
                        }
                        else
                        {
                            vm.AddToErrorMessage("can't compare complex numbers");
                            return -1;
                        }
                    }
                default:
                    {
                        double b = i.GetFloat();
                        switch (i2._type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    long o = i2.GetInt();
                                    vm.Pop(nargs + 2);
                                    vm.Push((double)Math.Max(b, o));
                                    break;
                                }
                            case ExObjType.COMPLEX:
                                {
                                    if (i2._val.c_Float == 0.0)
                                    {
                                        double o = i2._val.f_Float;
                                        vm.Pop(nargs + 2);
                                        vm.Push((double)Math.Max(b, o));
                                        break;
                                    }
                                    else
                                    {
                                        vm.AddToErrorMessage("can't compare complex numbers");
                                        return -1;
                                    }
                                }
                            default:
                                {
                                    double o = i2.GetFloat();
                                    vm.Pop(nargs + 2);
                                    vm.Push((double)Math.Max(b, o));
                                    break;
                                }
                        }

                        break;
                    }
            }

            return 1;
        }

        public static int MATH_sum(ExVM vm, int nargs)
        {
            ExObject sum = new(new Complex());
            ExObject[] args;
            if (nargs == 1 && ExAPI.GetFromStack(vm, 2)._type == ExObjType.ARRAY)
            {
                ExObject res = new();
                args = ExAPI.GetFromStack(vm, 2).GetList().ToArray();
                for (int i = 0; i < args.Length; i++)
                {
                    if (vm.DoArithmeticOP(OPs.OPC.ADD, args[i], sum, ref res))
                    {
                        sum._val.f_Float = res._val.f_Float;
                        sum._val.c_Float = res._val.c_Float;
                    }
                    else
                    {
                        return -1;
                    }
                }
                vm.Pop(3);
            }
            else
            {
                ExObject res = new();
                args = ExAPI.GetNObjects(vm, nargs);
                for (int i = 0; i < nargs; i++)
                {
                    if (vm.DoArithmeticOP(OPs.OPC.ADD, args[i], sum, ref res))
                    {
                        sum._val.f_Float = res._val.f_Float;
                        sum._val.c_Float = res._val.c_Float;
                    }
                    else
                    {
                        return -1;
                    }
                }
                vm.Pop(nargs + 2);
            }

            if (sum._val.c_Float == 0.0)
            {
                vm.Push(sum._val.f_Float);
            }
            else
            {
                vm.Push(sum);
            }
            return 1;
        }

        public static int MATH_mul(ExVM vm, int nargs)
        {
            ExObject mul = new(1.0);
            ExObject[] args;

            if (nargs == 1 && ExAPI.GetFromStack(vm, 2)._type == ExObjType.ARRAY)
            {
                ExObject res = new();
                args = ExAPI.GetFromStack(vm, 2).GetList().ToArray();
                for (int i = 0; i < args.Length; i++)
                {
                    if (vm.DoArithmeticOP(OPs.OPC.MLT, args[i], mul, ref res))
                    {
                        mul._val.f_Float = res._val.f_Float;
                        mul._val.c_Float = res._val.c_Float;
                    }
                    else
                    {
                        return -1;
                    }
                }
                vm.Pop(3);
            }
            else
            {
                ExObject res = new();
                args = ExAPI.GetNObjects(vm, nargs);
                for (int i = 0; i < nargs; i++)
                {
                    if (vm.DoArithmeticOP(OPs.OPC.MLT, args[i], mul, ref res))
                    {
                        mul._val.f_Float = res._val.f_Float;
                        mul._val.c_Float = res._val.c_Float;
                    }
                    else
                    {
                        return -1;
                    }
                }
                vm.Pop(nargs + 2);
            }

            if (mul._val.c_Float == 0.0)
            {
                vm.Push(mul._val.f_Float);
            }
            else
            {
                vm.Push(mul);
            }
            return 1;
        }

        public static int MATH_sign(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);

            switch (i._type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Sign(o));
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        if (i._val.c_Float == 0.0)
                        {
                            double o = i._val.f_Float;
                            vm.Pop(nargs + 2);
                            vm.Push(Math.Sign(o));
                            break;
                        }
                        else
                        {
                            vm.AddToErrorMessage("can't get complex number's sign");
                            return -1;
                        }
                    }
                default:
                    {
                        double o = i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(Math.Sign(o));
                        break;
                    }
            }

            return 1;
        }

        public static int MATH_isFIN(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            bool b = i._type == ExObjType.COMPLEX ? Complex.IsFinite(i.GetComplex()) : double.IsFinite(i.GetFloat());

            vm.Pop(nargs + 2);
            vm.Push(b);
            return 1;
        }

        public static int MATH_isINF(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            bool b = i._type == ExObjType.COMPLEX ? Complex.IsInfinity(i.GetComplex()) : double.IsPositiveInfinity(i.GetFloat());

            vm.Pop(nargs + 2);
            vm.Push(b);
            return 1;
        }

        public static int MATH_isNINF(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            bool b = i._type == ExObjType.COMPLEX ? Complex.IsInfinity(i.GetComplex()) : double.IsNegativeInfinity(i.GetFloat());

            vm.Pop(nargs + 2);
            vm.Push(i);
            return 1;
        }

        public static int MATH_isNAN(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            bool b = i._type == ExObjType.COMPLEX ? Complex.IsNaN(i.GetComplex()) : double.IsNaN(i.GetFloat());

            vm.Pop(nargs + 2);
            vm.Push(i);
            return 1;
        }

        private static double[] CreateNumArr(ExVM vm, List<ExObject> l)
        {
            double[] a = new double[l.Count];
            for (int i = 0; i < l.Count; i++)
            {
                if (!l[i].IsNumeric())
                {
                    vm.AddToErrorMessage("cant plot non-numeric values");
                    return null;
                }
                a[i] = l[i].GetFloat();
            }
            return a;
        }

        private static bool CheckFileName(ExVM vm, ref string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                vm.AddToErrorMessage("name can't be empty");
                return false;
            }

            string[] fname;
            if ((fname = name.Split(".")).Length > 1)
            {
                switch (fname[^1])
                {
                    case "png":
                    case "jpg":
                    case "jpeg":
                    case "bmp":
                        {
                            break;
                        }
                    default:
                        {
                            vm.AddToErrorMessage("unsupported extension, use one of jpg|png|bmp");
                            return false;
                        }
                }
            }
            else
            {
                name += ".png";
            }
            return true;
        }

        // TO-DO extremely redundant, refactor...
        public static int MATHPLOT_save_scatters(ExVM vm, int nargs)
        {
            string name = ExAPI.GetFromStack(vm, 2).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return -1;
            }

            int width = (int)ExAPI.GetFromStack(vm, 4).GetInt();
            int height = (int)ExAPI.GetFromStack(vm, 5).GetInt();

            if (width < 0)
            {
                width = 1200;
            }
            if (height < 0)
            {
                height = 800;
            }

            ScottPlot.Plot plt = new(width, height);
            List<ExObject> plots = ExAPI.GetFromStack(vm, 3).GetList();
            bool hadlabels = false;

            foreach (ExObject plot in plots)
            {
                if (plot._type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list of lists containing plot data");
                    return -1;
                }

                List<ExObject> plotdata = plot.GetList();
                int ndata = plotdata.Count;
                System.Drawing.Color color = System.Drawing.Color.Blue;
                string label = null;

                switch (ndata)
                {
                    case 4:
                        {
                            if (plotdata[2]._type != ExObjType.STRING)
                            {
                                vm.AddToErrorMessage("expected string for label");
                                return -1;
                            }
                            label = plotdata[3].GetString();
                            hadlabels = true;
                            goto case 3;
                        }
                    case 3:
                        {
                            if (plotdata[2]._type != ExObjType.STRING)
                            {
                                vm.AddToErrorMessage("expected string for color name");
                                return -1;
                            }
                            color = System.Drawing.Color.FromName(plotdata[2].GetString());
                            break;
                        }
                    case 2:
                        {
                            break;
                        }
                    default:
                        {
                            vm.AddToErrorMessage("not enough plot data given: [x,y,(color),(label)]");
                            return -1;
                        }
                }

                if (plotdata[0]._type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list for X axis");
                    return -1;
                }
                List<ExObject> x = plotdata[0].GetList();

                if (plotdata[1]._type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list for Y axis");
                    return -1;
                }
                List<ExObject> y = plotdata[1].GetList();

                double[] X = CreateNumArr(vm, x);
                if (X == null)
                {
                    return -1;
                }
                double[] Y = CreateNumArr(vm, y);
                if (Y == null)
                {
                    return -1;
                }

                try
                {
                    plt.AddScatter(X, Y, color, label: label);
                }
                catch (Exception e)
                {
                    vm.AddToErrorMessage("plot error: " + e.Message);
                    return -1;
                }
            }

            try
            {
                plt.Legend(hadlabels);
                plt.SaveFig(name);
                plt = null;
            }
            catch (Exception e)
            {
                vm.AddToErrorMessage("plot error: " + e.Message);
                return -1;
            }

            vm.Pop(nargs + 2);
            vm.Push(name);
            return 1;
        }

        public static int MATHPLOT_save_scatter(ExVM vm, int nargs)
        {
            string name = ExAPI.GetFromStack(vm, 2).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return -1;
            }

            List<ExObject> x = ExAPI.GetFromStack(vm, 3).GetList();
            List<ExObject> y = ExAPI.GetFromStack(vm, 4).GetList();
            int width = (int)ExAPI.GetFromStack(vm, 5).GetInt();
            int height = (int)ExAPI.GetFromStack(vm, 6).GetInt();

            System.Drawing.Color color = System.Drawing.Color.Blue;
            string label = null;

            switch (nargs)
            {
                case 7:
                    {
                        label = ExAPI.GetFromStack(vm, 8).GetString();
                        goto case 6;
                    }
                case 6:
                    {
                        color = System.Drawing.Color.FromName(ExAPI.GetFromStack(vm, 7).GetString().ToLower());
                        break;
                    }
            }

            if (width < 0)
            {
                width = 1200;
            }
            if (height < 0)
            {
                height = 800;
            }

            double[] X = CreateNumArr(vm, x);
            if (X == null)
            {
                return -1;
            }
            double[] Y = CreateNumArr(vm, y);
            if (Y == null)
            {
                return -1;
            }

            ScottPlot.Plot plot = new(width, height);

            try
            {
                plot.AddScatter(X, Y, color, label: label);
                plot.Legend(!string.IsNullOrWhiteSpace(label));

                plot.SaveFig(name);
                plot = null;
            }
            catch (Exception e)
            {
                vm.AddToErrorMessage("plot error: " + e.Message);
                return -1;
            }

            vm.Pop(nargs + 2);
            vm.Push(name);
            return 1;
        }

        public static int MATHPLOT_save_scatter_lines(ExVM vm, int nargs)
        {
            string name = ExAPI.GetFromStack(vm, 2).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return -1;
            }

            int width = (int)ExAPI.GetFromStack(vm, 4).GetInt();
            int height = (int)ExAPI.GetFromStack(vm, 5).GetInt();

            if (width < 0)
            {
                width = 1200;
            }
            if (height < 0)
            {
                height = 800;
            }

            ScottPlot.Plot plt = new(width, height);
            List<ExObject> plots = ExAPI.GetFromStack(vm, 3).GetList();
            bool hadlabels = false;

            foreach (ExObject plot in plots)
            {
                if (plot._type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list of lists containing plot data");
                    return -1;
                }

                List<ExObject> plotdata = plot.GetList();
                int ndata = plotdata.Count;
                System.Drawing.Color color = System.Drawing.Color.Blue;
                string label = null;

                switch (ndata)
                {
                    case 4:
                        {
                            if (plotdata[2]._type != ExObjType.STRING)
                            {
                                vm.AddToErrorMessage("expected string for label");
                                return -1;
                            }
                            label = plotdata[3].GetString();
                            hadlabels = true;
                            goto case 3;
                        }
                    case 3:
                        {
                            if (plotdata[2]._type != ExObjType.STRING)
                            {
                                vm.AddToErrorMessage("expected string for color name");
                                return -1;
                            }
                            color = System.Drawing.Color.FromName(plotdata[2].GetString());
                            break;
                        }
                    case 2:
                        {
                            break;
                        }
                    default:
                        {
                            vm.AddToErrorMessage("not enough plot data given: [x,y,(color),(label)]");
                            return -1;
                        }
                }

                if (plotdata[0]._type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list for X axis");
                    return -1;
                }
                List<ExObject> x = plotdata[0].GetList();

                if (plotdata[1]._type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list for Y axis");
                    return -1;
                }
                List<ExObject> y = plotdata[1].GetList();

                double[] X = CreateNumArr(vm, x);
                if (X == null)
                {
                    return -1;
                }
                double[] Y = CreateNumArr(vm, y);
                if (Y == null)
                {
                    return -1;
                }

                try
                {
                    plt.AddScatterLines(X, Y, color, label: label);
                }
                catch (Exception e)
                {
                    vm.AddToErrorMessage("plot error: " + e.Message);
                    return -1;
                }
            }

            try
            {
                plt.Legend(hadlabels);
                plt.SaveFig(name);
                plt = null;
            }
            catch (Exception e)
            {
                vm.AddToErrorMessage("plot error: " + e.Message);
                return -1;
            }

            vm.Pop(nargs + 2);
            vm.Push(name);
            return 1;
        }

        public static int MATHPLOT_save_scatter_line(ExVM vm, int nargs)
        {
            string name = ExAPI.GetFromStack(vm, 2).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return -1;
            }

            List<ExObject> x = ExAPI.GetFromStack(vm, 3).GetList();
            List<ExObject> y = ExAPI.GetFromStack(vm, 4).GetList();
            int width = (int)ExAPI.GetFromStack(vm, 5).GetInt();
            int height = (int)ExAPI.GetFromStack(vm, 6).GetInt();

            System.Drawing.Color color = System.Drawing.Color.Blue;
            string label = null;

            switch (nargs)
            {
                case 7:
                    {
                        label = ExAPI.GetFromStack(vm, 8).GetString();
                        goto case 6;
                    }
                case 6:
                    {
                        color = System.Drawing.Color.FromName(ExAPI.GetFromStack(vm, 7).GetString().ToLower());
                        break;
                    }
            }

            if (width < 0)
            {
                width = 1200;
            }
            if (height < 0)
            {
                height = 800;
            }

            double[] X = CreateNumArr(vm, x);
            if (X == null)
            {
                return -1;
            }
            double[] Y = CreateNumArr(vm, y);
            if (Y == null)
            {
                return -1;
            }

            ScottPlot.Plot plot = new(width, height);

            try
            {
                plot.AddScatterLines(X, Y, color, label: label);
                plot.Legend(!string.IsNullOrWhiteSpace(label));

                plot.SaveFig(name);
                plot = null;
            }
            catch (Exception e)
            {
                vm.AddToErrorMessage("plot error: " + e.Message);
                return -1;
            }

            vm.Pop(nargs + 2);
            vm.Push(name);
            return 1;
        }

        public static int MATHPLOT_save_scatter_points(ExVM vm, int nargs)
        {
            string name = ExAPI.GetFromStack(vm, 2).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return -1;
            }

            int width = (int)ExAPI.GetFromStack(vm, 4).GetInt();
            int height = (int)ExAPI.GetFromStack(vm, 5).GetInt();

            if (width < 0)
            {
                width = 1200;
            }
            if (height < 0)
            {
                height = 800;
            }

            ScottPlot.Plot plt = new(width, height);
            List<ExObject> plots = ExAPI.GetFromStack(vm, 3).GetList();
            bool hadlabels = false;

            foreach (ExObject plot in plots)
            {
                if (plot._type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list of lists containing plot data");
                    return -1;
                }

                List<ExObject> plotdata = plot.GetList();
                int ndata = plotdata.Count;
                System.Drawing.Color color = System.Drawing.Color.Blue;
                string label = null;

                switch (ndata)
                {
                    case 4:
                        {
                            if (plotdata[2]._type != ExObjType.STRING)
                            {
                                vm.AddToErrorMessage("expected string for label");
                                return -1;
                            }
                            label = plotdata[3].GetString();
                            hadlabels = true;
                            goto case 3;
                        }
                    case 3:
                        {
                            if (plotdata[2]._type != ExObjType.STRING)
                            {
                                vm.AddToErrorMessage("expected string for color name");
                                return -1;
                            }
                            color = System.Drawing.Color.FromName(plotdata[2].GetString());
                            break;
                        }
                    case 2:
                        {
                            break;
                        }
                    default:
                        {
                            vm.AddToErrorMessage("not enough plot data given: [x,y,(color),(label)]");
                            return -1;
                        }
                }

                if (plotdata[0]._type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list for X axis");
                    return -1;
                }
                List<ExObject> x = plotdata[0].GetList();

                if (plotdata[1]._type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list for Y axis");
                    return -1;
                }
                List<ExObject> y = plotdata[1].GetList();

                double[] X = CreateNumArr(vm, x);
                if (X == null)
                {
                    return -1;
                }
                double[] Y = CreateNumArr(vm, y);
                if (Y == null)
                {
                    return -1;
                }

                try
                {
                    plt.AddScatterPoints(X, Y, color, label: label);
                }
                catch (Exception e)
                {
                    vm.AddToErrorMessage("plot error: " + e.Message);
                    return -1;
                }
            }

            try
            {
                plt.Legend(hadlabels);
                plt.SaveFig(name);
                plt = null;
            }
            catch (Exception e)
            {
                vm.AddToErrorMessage("plot error: " + e.Message);
                return -1;
            }

            vm.Pop(nargs + 2);
            vm.Push(name);
            return 1;
        }

        public static int MATHPLOT_save_scatter_point(ExVM vm, int nargs)
        {
            string name = ExAPI.GetFromStack(vm, 2).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return -1;
            }

            List<ExObject> x = ExAPI.GetFromStack(vm, 3).GetList();
            List<ExObject> y = ExAPI.GetFromStack(vm, 4).GetList();
            int width = (int)ExAPI.GetFromStack(vm, 5).GetInt();
            int height = (int)ExAPI.GetFromStack(vm, 6).GetInt();

            System.Drawing.Color color = System.Drawing.Color.Blue;
            string label = null;

            switch (nargs)
            {
                case 7:
                    {
                        label = ExAPI.GetFromStack(vm, 8).GetString();
                        goto case 6;
                    }
                case 6:
                    {
                        color = System.Drawing.Color.FromName(ExAPI.GetFromStack(vm, 7).GetString().ToLower());
                        break;
                    }
            }

            if (width < 0)
            {
                width = 1200;
            }
            if (height < 0)
            {
                height = 800;
            }

            double[] X = CreateNumArr(vm, x);
            if (X == null)
            {
                return -1;
            }
            double[] Y = CreateNumArr(vm, y);
            if (Y == null)
            {
                return -1;
            }

            ScottPlot.Plot plot = new(width, height);

            try
            {
                plot.AddScatterPoints(X, Y, color, label: label);
                plot.Legend(!string.IsNullOrWhiteSpace(label));

                plot.SaveFig(name);
                plot = null;
            }
            catch (Exception e)
            {
                vm.AddToErrorMessage("plot error: " + e.Message);
                return -1;
            }

            vm.Pop(nargs + 2);
            vm.Push(name);
            return 1;
        }

        public static int MATHPLOT_save_scatter_steps(ExVM vm, int nargs)
        {
            string name = ExAPI.GetFromStack(vm, 2).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return -1;
            }

            int width = (int)ExAPI.GetFromStack(vm, 4).GetInt();
            int height = (int)ExAPI.GetFromStack(vm, 5).GetInt();

            if (width < 0)
            {
                width = 1200;
            }
            if (height < 0)
            {
                height = 800;
            }

            ScottPlot.Plot plt = new(width, height);
            List<ExObject> plots = ExAPI.GetFromStack(vm, 3).GetList();
            bool hadlabels = false;

            foreach (ExObject plot in plots)
            {
                if (plot._type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list of lists containing plot data");
                    return -1;
                }

                List<ExObject> plotdata = plot.GetList();
                int ndata = plotdata.Count;
                System.Drawing.Color color = System.Drawing.Color.Blue;
                string label = null;

                switch (ndata)
                {
                    case 4:
                        {
                            if (plotdata[2]._type != ExObjType.STRING)
                            {
                                vm.AddToErrorMessage("expected string for label");
                                return -1;
                            }
                            label = plotdata[3].GetString();
                            hadlabels = true;
                            goto case 3;
                        }
                    case 3:
                        {
                            if (plotdata[2]._type != ExObjType.STRING)
                            {
                                vm.AddToErrorMessage("expected string for color name");
                                return -1;
                            }
                            color = System.Drawing.Color.FromName(plotdata[2].GetString());
                            break;
                        }
                    case 2:
                        {
                            break;
                        }
                    default:
                        {
                            vm.AddToErrorMessage("not enough plot data given: [x,y,(color),(label)]");
                            return -1;
                        }
                }

                if (plotdata[0]._type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list for X axis");
                    return -1;
                }
                List<ExObject> x = plotdata[0].GetList();

                if (plotdata[1]._type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list for Y axis");
                    return -1;
                }
                List<ExObject> y = plotdata[1].GetList();

                double[] X = CreateNumArr(vm, x);
                if (X == null)
                {
                    return -1;
                }
                double[] Y = CreateNumArr(vm, y);
                if (Y == null)
                {
                    return -1;
                }

                try
                {
                    plt.AddScatterStep(X, Y, color, label: label);
                }
                catch (Exception e)
                {
                    vm.AddToErrorMessage("plot error: " + e.Message);
                    return -1;
                }
            }

            try
            {
                plt.Legend(hadlabels);
                plt.SaveFig(name);
                plt = null;
            }
            catch (Exception e)
            {
                vm.AddToErrorMessage("plot error: " + e.Message);
                return -1;
            }

            vm.Pop(nargs + 2);
            vm.Push(name);
            return 1;
        }

        public static int MATHPLOT_save_scatter_step(ExVM vm, int nargs)
        {
            string name = ExAPI.GetFromStack(vm, 2).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return -1;
            }

            List<ExObject> x = ExAPI.GetFromStack(vm, 3).GetList();
            List<ExObject> y = ExAPI.GetFromStack(vm, 4).GetList();
            int width = (int)ExAPI.GetFromStack(vm, 5).GetInt();
            int height = (int)ExAPI.GetFromStack(vm, 6).GetInt();

            System.Drawing.Color color = System.Drawing.Color.Blue;
            string label = null;

            switch (nargs)
            {
                case 7:
                    {
                        label = ExAPI.GetFromStack(vm, 8).GetString();
                        goto case 6;
                    }
                case 6:
                    {
                        color = System.Drawing.Color.FromName(ExAPI.GetFromStack(vm, 7).GetString().ToLower());
                        break;
                    }
            }

            if (width < 0)
            {
                width = 1200;
            }
            if (height < 0)
            {
                height = 800;
            }

            double[] X = CreateNumArr(vm, x);
            if (X == null)
            {
                return -1;
            }
            double[] Y = CreateNumArr(vm, y);
            if (Y == null)
            {
                return -1;
            }

            ScottPlot.Plot plot = new(width, height);

            try
            {
                plot.AddScatterStep(X, Y, color, label: label);
                plot.Legend(!string.IsNullOrWhiteSpace(label));

                plot.SaveFig(name);
                plot = null;
            }
            catch (Exception e)
            {
                vm.AddToErrorMessage("plot error: " + e.Message);
                return -1;
            }

            vm.Pop(nargs + 2);
            vm.Push(name);
            return 1;
        }

        public static int MATHPLOT_save_scatter_complex(ExVM vm, int nargs)
        {
            string name = ExAPI.GetFromStack(vm, 2).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return -1;
            }

            List<ExObject> x = ExAPI.GetFromStack(vm, 3).GetList();
            int count = x.Count;

            double[] X = new double[count];
            double[] Y = new double[count];
            for (int i = 0; i < count; i++)
            {
                ExObject v = x[i];
                if (v._type != ExObjType.COMPLEX)
                {
                    if (!v.IsNumeric())
                    {
                        vm.AddToErrorMessage("cant plot non-numeric in complex plane");
                        return -1;
                    }
                    X[i] = v.GetFloat();
                    Y[i] = 0.0;
                }
                else
                {
                    X[i] = v._val.f_Float;
                    Y[i] = v._val.c_Float;
                }
            }

            int width = (int)ExAPI.GetFromStack(vm, 4).GetInt();
            int height = (int)ExAPI.GetFromStack(vm, 5).GetInt();

            System.Drawing.Color color = System.Drawing.Color.Blue;
            string label = null;

            switch (nargs)
            {
                case 6:
                    {
                        label = ExAPI.GetFromStack(vm, 7).GetString();
                        goto case 5;
                    }
                case 5:
                    {
                        color = System.Drawing.Color.FromName(ExAPI.GetFromStack(vm, 6).GetString().ToLower());
                        break;
                    }
            }

            if (width < 0)
            {
                width = 1200;
            }
            if (height < 0)
            {
                height = 800;
            }


            ScottPlot.Plot plot = new(width, height);

            try
            {
                plot.AddScatterPoints(X, Y, color, label: label);
                plot.Legend(!string.IsNullOrWhiteSpace(label));

                plot.SaveFig(name);
                plot = null;
            }
            catch (Exception e)
            {
                vm.AddToErrorMessage("plot error: " + e.Message);
                return -1;
            }

            vm.Pop(nargs + 2);
            vm.Push(name);
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
                name = "isFIN",
                func = new(GetStdMathMethod("MATH_isFIN")),
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
            new()
            {
                name = "save_scatter",
                func = new(GetStdMathMethod("MATHPLOT_save_scatter")),
                n_pchecks = -4,
                mask = ".saannss",
                d_defaults = new()
                {
                    { 4, new(1200) },
                    { 5, new(800) },
                    { 6, new("blue") },
                    { 7, new(s: null) }
                }
            },
            new()
            {
                name = "save_scatters",
                func = new(GetStdMathMethod("MATHPLOT_save_scatters")),
                n_pchecks = -3,
                mask = ".sann",
                d_defaults = new()
                {
                    { 3, new(1200) },
                    { 4, new(800) }
                }
            },
            new()
            {
                name = "save_scatter_step",
                func = new(GetStdMathMethod("MATHPLOT_save_scatter_step")),
                n_pchecks = -4,
                mask = ".saannss",
                d_defaults = new()
                {
                    { 4, new(1200) },
                    { 5, new(800) },
                    { 6, new("blue") },
                    { 7, new(s: null) }
                }
            },
            new()
            {
                name = "save_scatter_steps",
                func = new(GetStdMathMethod("MATHPLOT_save_scatter_steps")),
                n_pchecks = -3,
                mask = ".sann",
                d_defaults = new()
                {
                    { 3, new(1200) },
                    { 4, new(800) }
                }
            },
            new()
            {
                name = "save_scatter_point",
                func = new(GetStdMathMethod("MATHPLOT_save_scatter_point")),
                n_pchecks = -4,
                mask = ".saannss",
                d_defaults = new()
                {
                    { 4, new(1200) },
                    { 5, new(800) },
                    { 6, new("blue") },
                    { 7, new(s: null) }
                }
            },
            new()
            {
                name = "save_scatter_points",
                func = new(GetStdMathMethod("MATHPLOT_save_scatter_points")),
                n_pchecks = -3,
                mask = ".sann",
                d_defaults = new()
                {
                    { 3, new(1200) },
                    { 4, new(800) }
                }
            },
            new()
            {
                name = "save_scatter_line",
                func = new(GetStdMathMethod("MATHPLOT_save_scatter_line")),
                n_pchecks = -4,
                mask = ".saannss",
                d_defaults = new()
                {
                    { 4, new(1200) },
                    { 5, new(800) },
                    { 6, new("blue") },
                    { 7, new(s: null) }
                }
            },
            new()
            {
                name = "save_scatter_lines",
                func = new(GetStdMathMethod("MATHPLOT_save_scatter_lines")),
                n_pchecks = -3,
                mask = ".sann",
                d_defaults = new()
                {
                    { 3, new(1200) },
                    { 4, new(800) }
                }
            },
            new()
            {
                name = "save_complex",
                func = new(GetStdMathMethod("MATHPLOT_save_scatter_complex")),
                n_pchecks = -3,
                mask = ".sannss",
                d_defaults = new()
                {
                    { 3, new(1200) },
                    { 4, new(800) },
                    { 5, new("blue") },
                    { 6, new(s: null) }
                }
            },

            new() { name = string.Empty }
        };
        public static List<ExRegFunc> MathFuncs => _stdmathfuncs;
        public static Random Rand { get => rand; set => rand = value; }

        public static bool RegisterStdMath(ExVM vm, bool force = false)
        {
            ExAPI.RegisterNativeFunctions(vm, MathFuncs, force);

            ExAPI.CreateConstantInt(vm, "INT8_MAX", sbyte.MaxValue);
            ExAPI.CreateConstantInt(vm, "INT8_MIN", sbyte.MinValue);

            ExAPI.CreateConstantInt(vm, "INT16_MAX", short.MaxValue);
            ExAPI.CreateConstantInt(vm, "INT16_MIN", short.MinValue);

            ExAPI.CreateConstantInt(vm, "INT32_MAX", int.MaxValue);
            ExAPI.CreateConstantInt(vm, "INT32_MIN", int.MinValue);

            ExAPI.CreateConstantInt(vm, "INT64_MAX", long.MaxValue);
            ExAPI.CreateConstantInt(vm, "INT64_MIN", long.MinValue);

            ExAPI.CreateConstantInt(vm, "UINT8_MAX", byte.MaxValue);
            ExAPI.CreateConstantInt(vm, "UINT16_MAX", ushort.MaxValue);
            ExAPI.CreateConstantInt(vm, "UINT32_MAX", uint.MaxValue);

            ExAPI.CreateConstantFloat(vm, "FLOAT32_MAX", float.MaxValue);
            ExAPI.CreateConstantFloat(vm, "FLOAT32_MIN", float.MinValue);

            ExAPI.CreateConstantFloat(vm, "FLOAT64_MAX", double.MaxValue);
            ExAPI.CreateConstantFloat(vm, "FLOAT64_MIN", double.MinValue);

            ExAPI.CreateConstantFloat(vm, "TAU", Math.Tau);
            ExAPI.CreateConstantFloat(vm, "PI", Math.PI);
            ExAPI.CreateConstantFloat(vm, "E", Math.E);
            ExAPI.CreateConstantFloat(vm, "GOLDEN", (1.0 + Math.Sqrt(5.0)) / 2.0);
            ExAPI.CreateConstantFloat(vm, "DEGREE", Math.PI / 180.0);
            ExAPI.CreateConstantFloat(vm, "EPSILON", double.Epsilon);

            ExAPI.CreateConstantFloat(vm, "NAN", double.NaN);
            ExAPI.CreateConstantFloat(vm, "NINF", double.NegativeInfinity);
            ExAPI.CreateConstantFloat(vm, "INF", double.PositiveInfinity);

            ExAPI.CreateConstantDict(vm, "SPACES", new()
            {
                { "R", ExSpace.Create("R", '\\', 1) },
                { "R2", ExSpace.Create("R", '\\', 2) },
                { "R3", ExSpace.Create("R", '\\', 3) },
                { "Rn", ExSpace.Create("R", '\\', -1) },
                { "Rmn", ExSpace.Create("R", '\\', -1, -1) },

                { "Z", ExSpace.Create("Z", '\\', 1) },
                { "Z2", ExSpace.Create("Z", '\\', 2) },
                { "Z3", ExSpace.Create("Z", '\\', 3) },
                { "Zn", ExSpace.Create("Z", '\\', -1) },
                { "Zmn", ExSpace.Create("Z", '\\', -1, -1) },

                { "N", ExSpace.Create("N", '\\', 1) },
                { "N2", ExSpace.Create("N", '\\', 2) },
                { "N3", ExSpace.Create("N", '\\', 3) },
                { "Nn", ExSpace.Create("N", '\\', -1) },
                { "Nmn", ExSpace.Create("N", '\\', -1, -1) },

                { "C", ExSpace.Create("C", '\\', 1) },
                { "C2", ExSpace.Create("C", '\\', 2) },
                { "C3", ExSpace.Create("C", '\\', 3) },
                { "Cn", ExSpace.Create("C", '\\', -1) },
                { "Cmn", ExSpace.Create("C", '\\', -1, -1) },

                { "A", ExSpace.Create("A", '\\', 1) },
                { "A2", ExSpace.Create("A", '\\', 2) },
                { "A3", ExSpace.Create("A", '\\', 3) },
                { "An", ExSpace.Create("A", '\\', -1) },
                { "Amn", ExSpace.Create("A", '\\', -1, -1) },
            });

            return true;
        }
    }
}
