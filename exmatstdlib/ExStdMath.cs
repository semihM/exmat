using System;
using System.Collections.Generic;
using System.Numerics;
using ExMat.API;
using ExMat.Objects;
using ExMat.Utils;
using ExMat.VM;

namespace ExMat.BaseLib
{
    public static class ExStdMath
    {
        private static List<int> Primes;
        private static readonly int PrimeSearchSize = int.MaxValue / 100;
        private static readonly int PrimeCacheMaxSize = 1358124 * 2;
        private static int PrimeCount = 1358124;
        private static int PrimeMax = 21474829;

        public static ExFunctionStatus MathSrand(ExVM vm, int nargs)
        {
            Rand = new((int)vm.GetArgument(1).GetInt());
            return ExFunctionStatus.VOID;
        }

        public static ExFunctionStatus MathRand(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                default:
                    {
                        return vm.CleanReturn(2, Rand.Next());
                    }
                case 1:
                    {
                        return vm.CleanReturn(3, Rand.Next(ExUtils.LongTo32NonNegativeIntegerRange(vm.GetArgument(1).GetInt())));
                    }
                case 2:
                    {
                        return vm.CleanReturn(4, Rand.Next(ExUtils.LongTo32SignedIntegerRange(vm.GetArgument(1).GetInt()), ExUtils.LongTo32SignedIntegerRange(vm.GetArgument(2).GetInt())));
                    }
            }
        }

        public static ExFunctionStatus MathRandf(ExVM vm, int nargs)
        {
            switch (nargs)
            {
                default:
                    {
                        return vm.CleanReturn(2, Rand.NextDouble());
                    }
                case 1:
                    {
                        return vm.CleanReturn(3, Rand.NextDouble() * vm.GetArgument(1).GetFloat());
                    }
                case 2:
                    {
                        double min = vm.GetArgument(1).GetFloat();
                        return vm.CleanReturn(4, (Rand.NextDouble() * (vm.GetArgument(2).GetFloat() - min)) + min);
                    }
            }
        }

        private static void FindPrimes()
        {
            bool[] bnotp = new bool[PrimeSearchSize];

            for (int p = 2; p * p < PrimeSearchSize; p++)
            {
                if (!bnotp[p])
                {
                    for (int i = p * p; i < PrimeSearchSize; i += p)
                    {
                        bnotp[i] = true;
                    }
                }
            }

            Primes = new(PrimeCount);
            for (int p = 2, i = 0; p < PrimeSearchSize; p++)
            {
                if (!bnotp[p])
                {
                    Primes.Add(p);
                    i++;
                }
            }

        }

        private static bool IsPrime(long o)
        {
            if (o <= 3)
            {
                return o > 1;
            }

            if (o % 2 == 0 || o % 3 == 0)
            {
                return false;
            }

            int i = 5;
            while (i * i <= o)
            {
                if (o % i == 0 || o % (i + 2) == 0)
                {
                    return false;
                }

                i += 6;
            }
            return true;
        }

        private static long NextClosestPrime(long o)
        {
            if (o <= 1)
            {
                return 2;
            }

            if (o % 2 == 1)
            {
                o += 2;
            }
            else
            {
                o++;
            }

            while (!IsPrime(o))
            { o += 2; }
            return o;
        }

        // o is always passed odd
        private static long NextPrime(long o)
        {
            o += 2;
            while (!IsPrime(o))
            { o += 2; }
            return o;
        }

        private static long FindAndCacheNextPrimes(int diff)
        {
            long last = Primes[^1];
            while (diff > 0)
            {
                last = NextPrime(last);

                if (PrimeCount < PrimeCacheMaxSize && last < int.MaxValue)
                {
                    Primes.Add((int)last);
                    PrimeCount++;
                    PrimeMax = (int)last;
                }
                diff--;
            }
            return last;
        }

        public static ExFunctionStatus MathNextPrime(ExVM vm, int nargs)
        {
            long a = vm.GetArgument(1).GetInt();

            if (Primes == null)
            {
                FindPrimes();
            }

            int idx;
            return vm.CleanReturn(nargs + 2,
                                  (a > 0 && a <= PrimeMax && (idx = Primes.BinarySearch(item: (int)a)) >= 0 && idx < PrimeCount) ? Primes[idx + 1] : NextClosestPrime(a));
        }

        public static ExFunctionStatus MathIsPrime(ExVM vm, int nargs)
        {
            long a = vm.GetArgument(1).GetInt();

            return vm.CleanReturn(nargs + 2, (Primes != null && a > 0 && a <= PrimeMax && Primes.BinarySearch(item: (int)a) >= 0) || IsPrime(a));
        }

        public static ExFunctionStatus MathAreCoprime(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, GetGCD(vm.GetArgument(1).GetInt(), vm.GetArgument(2).GetInt()) == 1.0);
        }

        public static ExFunctionStatus MathPrime(ExVM vm, int nargs)
        {
            int a = (int)vm.GetArgument(1).GetInt();

            if (Primes == null)
            {
                FindPrimes();
            }

            int n = Primes.Count;
            if (a <= 0)
            {
                return vm.AddToErrorMessage("expected positive integer for 'n'th prime index");
            }
            if (a > n)
            {
                // TO-DO Find out why, for some reason doing this sometimes reduces memory use by half for the prime list
                return vm.CleanReturn(nargs + 2, FindAndCacheNextPrimes(a - n));
            }

            return vm.CleanReturn(nargs + 2, Primes[a - 1]);
        }

        public static ExFunctionStatus MathPrimeFactors(ExVM vm, int nargs)
        {
            long a = vm.GetArgument(1).GetInt();

            if (Primes != null && a > 0 && a <= PrimeMax && Primes.BinarySearch(item: (int)a) >= 0)
            {
                return vm.CleanReturn(nargs + 2, new List<ExObject>(1) { new(a) });
            }

            if (a < 0)
            {
                return vm.CleanReturn(nargs + 2, new List<ExObject>());
            }

            List<ExObject> lis = new();
            long p = 2;
            while (a >= Math.Pow(p, 2))
            {
                if (a % p == 0)
                {
                    lis.Add(new(p));
                    a /= p;
                }
                else
                {
                    p++;
                }
            }

            lis.Add(new(a));

            return vm.CleanReturn(nargs + 2, lis);
        }

        private static double GetGCD(double a, double b)
        {
            while (b != 0)
            {
                double r = b;
                b = a % b;
                a = r;
            }
            return a;
        }

        public static ExFunctionStatus MathGcd(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, GetGCD(vm.GetArgument(1).GetFloat(), vm.GetArgument(2).GetFloat()));
        }

        public static ExFunctionStatus MathLcd(ExVM vm, int nargs)
        {
            double a = vm.GetArgument(1).GetFloat();
            double b = vm.GetArgument(2).GetFloat();
            return vm.CleanReturn(nargs + 2, Math.Abs(a * b) / GetGCD(a, b));
        }

        public static ExFunctionStatus MathIsDivisible(ExVM vm, int nargs)
        {
            Math.DivRem(vm.GetArgument(1).GetInt(), vm.GetArgument(2).GetInt(), out long r);
            return vm.CleanReturn(nargs + 2, r == 0);
        }

        public static ExFunctionStatus MathDivQuot(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, Math.DivRem(vm.GetArgument(1).GetInt(), vm.GetArgument(2).GetInt(), out long _));
        }

        public static ExFunctionStatus MathDivRem(ExVM vm, int nargs)
        {
            Math.DivRem(vm.GetArgument(1).GetInt(), vm.GetArgument(2).GetInt(), out long rem);
            return vm.CleanReturn(nargs + 2, rem);
        }

        public static ExFunctionStatus MathDivRemQuot(ExVM vm, int nargs)
        {
            long quot = Math.DivRem(vm.GetArgument(1).GetInt(), vm.GetArgument(2).GetInt(), out long rem);
            return vm.CleanReturn(nargs + 2, new List<ExObject>(2) { new(rem), new(quot) });
        }

        public static ExFunctionStatus MathRecip(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, 1.0 / i.GetInt());
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Reciprocal(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, 1.0 / i.GetFloat());
                    }
            }
        }
        public static ExFunctionStatus MathDigits(ExVM vm, int nargs)
        {
            long i = vm.GetArgument(1).GetInt();
            List<ExObject> lis = new();
            if (i < 0)
            {
                if (i == long.MinValue)
                {
                    lis = new() { new(-9), new(2), new(2), new(3), new(3), new(7), new(2), new(0), new(3), new(6), new(8), new(5), new(4), new(7), new(7), new(5), new(8), new(0), new(8) };
                }
                else
                {
                    i = Math.Abs(i);
                    while (i > 0)
                    {
                        lis.Add(new(i % 10));
                        i /= 10;
                    }
                    lis.Reverse();
                    lis[0].Value.i_Int *= -1;
                }
            }
            else
            {
                while (i > 0)
                {
                    lis.Add(new(i % 10));
                    i /= 10;
                }
                lis.Reverse();
            }
            return vm.CleanReturn(nargs + 2, lis);
        }

        public static ExFunctionStatus MathAbs(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        if (o < 0)
                        {
                            return vm.CleanReturn(nargs + 2, o > long.MinValue ? Math.Abs(o) : 0);
                        }
                        else
                        {
                            return vm.CleanReturn(nargs + 2, Math.Abs(o));
                        }
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Abs(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Abs(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathSqrt(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Sqrt(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Sqrt(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Sqrt(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathCbrt(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Cbrt(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Pow(i.GetComplex(), 1.0 / 3.0));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Cbrt(i.GetFloat()));
                    }
            }
        }
        public static ExFunctionStatus MathSin(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Sin(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Sin(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Sin(i.GetFloat()));
                    }
            }
        }
        public static ExFunctionStatus MathSinh(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Sinh(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Sinh(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Sinh(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathCos(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Cos(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Cos(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Cos(i.GetFloat()));
                    }
            }
        }
        public static ExFunctionStatus MathCosh(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Cosh(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Cosh(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Cosh(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathTan(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Tan(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Tan(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Tan(i.GetFloat()));
                    }
            }
        }
        public static ExFunctionStatus MathTanh(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Tanh(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Tanh(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Tanh(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathAcos(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Acos(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Acos(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Acos(i.GetFloat()));
                    }
            }
        }
        public static ExFunctionStatus MathAcosh(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Acosh(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        if (i.Value.c_Float == 0.0)
                        {
                            return vm.CleanReturn(nargs + 2, Math.Acosh(i.Value.f_Float));
                        }
                        else
                        {
                            return vm.AddToErrorMessage("can't use complex numbers with acosh");
                        }
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Acosh(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathAsin(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Asin(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Asin(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Asin(i.GetFloat()));
                    }
            }
        }
        public static ExFunctionStatus MathAsinh(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Asinh(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        if (i.Value.c_Float == 0.0)
                        {
                            return vm.CleanReturn(nargs + 2, Math.Asinh(i.Value.f_Float));
                        }
                        else
                        {
                            return vm.AddToErrorMessage("can't use complex numbers with asinh");
                        }
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Asinh(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathAtan(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Atan(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Atan(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Atan(i.GetFloat()));
                    }
            }
        }
        public static ExFunctionStatus MathAtanh(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Atanh(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        if (i.Value.c_Float == 0.0)
                        {
                            return vm.CleanReturn(nargs + 2, Math.Atanh(i.Value.f_Float));
                        }
                        else
                        {
                            return vm.AddToErrorMessage("can't use complex numbers with atanh");
                        }
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Atanh(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathAtan2(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            ExObject i2 = vm.GetArgument(2);
            double b = 0.0;
            long l;

            switch (i.Type)    // TO-DO refactor
            {
                case ExObjType.INTEGER:
                    {
                        l = i.GetInt();
                        switch (i2.Type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Atan2(l, i2.GetInt()));
                                }
                            case ExObjType.COMPLEX:
                                {
                                    if (i2.Value.c_Float == 0.0)
                                    {
                                        return vm.CleanReturn(nargs + 2, Math.Atan2(l, i2.Value.f_Float));
                                    }
                                    else
                                    {
                                        return vm.AddToErrorMessage("can't use complex numbers with atan2");
                                    }
                                }
                            default:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Atan2(l, i2.GetFloat()));
                                }
                        }
                    }
                case ExObjType.COMPLEX:
                    {
                        if (i.Value.c_Float == 0.0)
                        {
                            b = i.Value.f_Float;
                            goto default;
                        }
                        else
                        {
                            return vm.AddToErrorMessage("can't use complex numbers with atan2");
                        }
                    }
                case ExObjType.FLOAT:
                    {
                        b = i.GetFloat();
                        goto default;
                    }
                default:
                    {
                        switch (i2.Type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Atan2(b, i2.GetInt()));
                                }
                            case ExObjType.COMPLEX:
                                {
                                    if (i2.Value.c_Float == 0.0)
                                    {
                                        return vm.CleanReturn(nargs + 2, Math.Atan2(b, i2.Value.f_Float));
                                    }
                                    else
                                    {
                                        return vm.AddToErrorMessage("can't use complex numbers with atan2");
                                    }
                                }
                            default:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Atan2(b, i2.GetFloat()));
                                }
                        }
                    }
            }
        }

        public static ExFunctionStatus MathLoge(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Log(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Log(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Log(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathLog2(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Log2(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Log(i.GetComplex()) / Math.Log(2.0));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Log2(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathLog10(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Log10(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Log10(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Log10(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathLog(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            double newbase = nargs == 2 ? vm.GetPositiveIntegerArgument(2, 0) : Math.E;
            if (newbase == 1)
            {
                return vm.AddToErrorMessage("can't use 1 as logarithm base");
            }

            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Log(i.GetInt(), newbase));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Log10(i.GetComplex()) / Math.Log(newbase));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Log(i.GetFloat(), newbase));
                    }
            }
        }

        public static ExFunctionStatus MathExp(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Exp(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        return vm.CleanReturn(nargs + 2, Complex.Exp(i.GetComplex()));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Exp(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathRound(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            int dec = 0;
            if (nargs == 2)
            {
                dec = (int)vm.GetArgument(2).GetInt();
            }

            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Round((double)i.GetInt(), dec));
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = i.GetComplex();
                        return vm.CleanReturn(nargs + 2, new Complex(Math.Round(o.Real, dec), Math.Round(o.Imaginary, dec)));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Round(i.GetFloat(), dec));
                    }
            }
        }

        public static ExFunctionStatus MathFloor(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, i.GetInt());
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = i.GetComplex();
                        return vm.CleanReturn(nargs + 2, new Complex(Math.Floor(o.Real), Math.Floor(o.Imaginary)));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, (int)Math.Floor(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathCeil(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, i.GetInt());
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = i.GetComplex();
                        return vm.CleanReturn(nargs + 2, new Complex(Math.Ceiling(o.Real), Math.Ceiling(o.Imaginary)));
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, (int)Math.Ceiling(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathPow(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            ExObject i2 = vm.GetArgument(2);

            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        long b = i.GetInt();
                        switch (i2.Type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Pow(b, i2.GetInt()));
                                }
                            case ExObjType.COMPLEX:
                                {
                                    return vm.CleanReturn(nargs + 2, Complex.Pow(b, i2.GetComplex()));
                                }
                            default:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Pow(b, i2.GetFloat()));
                                }
                        }
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex b = i.GetComplex();
                        switch (i2.Type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    return vm.CleanReturn(nargs + 2, Complex.Pow(b, i2.GetInt()));
                                }
                            case ExObjType.COMPLEX:
                                {
                                    return vm.CleanReturn(nargs + 2, Complex.Pow(b, i2.GetComplex()));
                                }
                            default:
                                {
                                    return vm.CleanReturn(nargs + 2, Complex.Pow(b, i2.GetFloat()));
                                }
                        }
                    }
                default:
                    {
                        double b = i.GetFloat();
                        switch (i2.Type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Pow(b, i2.GetInt()));
                                }
                            case ExObjType.COMPLEX:
                                {
                                    return vm.CleanReturn(nargs + 2, Complex.Pow(b, i2.GetComplex()));
                                }
                            default:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Pow(b, i2.GetFloat()));
                                }
                        }
                    }
            }
        }

        public static ExFunctionStatus MathMin(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            ExObject i2 = vm.GetArgument(2);

            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        long b = i.GetInt();
                        switch (i2.Type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Min(b, i2.GetInt()));
                                }
                            case ExObjType.COMPLEX:
                                {
                                    if (i2.Value.c_Float == 0.0)
                                    {
                                        return vm.CleanReturn(nargs + 2, Math.Min(b, i2.Value.f_Float));
                                    }
                                    else
                                    {
                                        return vm.AddToErrorMessage("can't compare complex numbers");
                                    }
                                }
                            default:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Min(b, i2.GetFloat()));
                                }
                        }
                    }
                case ExObjType.COMPLEX:
                    {
                        if (i.Value.c_Float == 0.0)
                        {
                            double b = i.Value.f_Float;
                            switch (i2.Type)
                            {
                                case ExObjType.INTEGER:
                                    {
                                        return vm.CleanReturn(nargs + 2, Math.Min(b, i2.GetInt()));
                                    }
                                case ExObjType.COMPLEX:
                                    {
                                        if (i2.Value.c_Float == 0.0)
                                        {
                                            return vm.CleanReturn(nargs + 2, Math.Min(b, i2.Value.f_Float));
                                        }
                                        else
                                        {
                                            return vm.AddToErrorMessage("can't compare complex numbers");
                                        }
                                    }
                                default:
                                    {
                                        return vm.CleanReturn(nargs + 2, Math.Min(b, i2.GetFloat()));
                                    }
                            }
                        }
                        else
                        {
                            return vm.AddToErrorMessage("can't compare complex numbers");
                        }
                    }
                default:
                    {
                        double b = i.GetFloat();
                        switch (i2.Type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Min(b, i2.GetInt()));
                                }
                            case ExObjType.COMPLEX:
                                {
                                    if (i2.Value.c_Float == 0.0)
                                    {
                                        return vm.CleanReturn(nargs + 2, Math.Min(b, i2.Value.f_Float));
                                    }
                                    else
                                    {
                                        return vm.AddToErrorMessage("can't compare complex numbers");
                                    }
                                }
                            default:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Min(b, i2.GetFloat()));
                                }
                        }
                    }
            }
        }

        public static ExFunctionStatus MathMax(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            ExObject i2 = vm.GetArgument(2);

            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        long b = i.GetInt();
                        switch (i2.Type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Max(b, i2.GetInt()));
                                }
                            case ExObjType.COMPLEX:
                                {
                                    if (i2.Value.c_Float == 0.0)
                                    {
                                        return vm.CleanReturn(nargs + 2, Math.Max(b, i2.Value.f_Float));
                                    }
                                    else
                                    {
                                        return vm.AddToErrorMessage("can't compare complex numbers");
                                    }
                                }
                            default:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Max(b, i2.GetFloat()));
                                }
                        }
                    }
                case ExObjType.COMPLEX:
                    {
                        if (i.Value.c_Float == 0.0)
                        {
                            double b = i.Value.f_Float;
                            switch (i2.Type)
                            {
                                case ExObjType.INTEGER:
                                    {
                                        return vm.CleanReturn(nargs + 2, Math.Max(b, i2.GetInt()));
                                    }
                                case ExObjType.COMPLEX:
                                    {
                                        if (i2.Value.c_Float == 0.0)
                                        {
                                            return vm.CleanReturn(nargs + 2, Math.Max(b, i2.Value.f_Float));
                                        }
                                        else
                                        {
                                            return vm.AddToErrorMessage("can't compare complex numbers");
                                        }
                                    }
                                default:
                                    {
                                        return vm.CleanReturn(nargs + 2, Math.Max(b, i2.GetFloat()));
                                    }
                            }
                        }
                        else
                        {
                            return vm.AddToErrorMessage("can't compare complex numbers");
                        }
                    }
                default:
                    {
                        double b = i.GetFloat();
                        switch (i2.Type)
                        {
                            case ExObjType.INTEGER:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Max(b, i2.GetInt()));
                                }
                            case ExObjType.COMPLEX:
                                {
                                    if (i2.Value.c_Float == 0.0)
                                    {
                                        return vm.CleanReturn(nargs + 2, Math.Max(b, i2.Value.f_Float));
                                    }
                                    else
                                    {
                                        return vm.AddToErrorMessage("can't compare complex numbers");
                                    }
                                }
                            default:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Max(b, i2.GetFloat()));
                                }
                        }
                    }
            }
        }

        public static ExFunctionStatus MathSum(ExVM vm, int nargs)
        {
            ExObject sum = new(new Complex());
            ExObject[] args;
            ExObject res = new();
            int count;
            if (nargs == 1 && vm.GetArgument(1).Type == ExObjType.ARRAY)
            {
                args = vm.GetArgument(1).GetList().ToArray();
                count = args.Length;
            }
            else
            {
                args = ExApi.GetNObjects(vm, nargs);
                count = nargs;
            }

            for (int i = 0; i < count; i++)
            {
                if (vm.DoArithmeticOP(OPs.ExOperationCode.ADD, args[i], sum, ref res))
                {
                    sum.Value.f_Float = res.Value.f_Float;
                    sum.Value.c_Float = res.Value.c_Float;
                }
                else
                {
                    return ExFunctionStatus.ERROR;
                }
            }

            return vm.CleanReturn(nargs + 2, sum.Value.c_Float == 0.0 ? new ExObject(sum.Value.f_Float) : sum);
        }

        public static ExFunctionStatus MathMul(ExVM vm, int nargs)
        {
            ExObject mul = new(1.0);
            ExObject[] args;
            ExObject res = new();
            int count;
            if (nargs == 1 && vm.GetArgument(1).Type == ExObjType.ARRAY)
            {
                args = vm.GetArgument(1).GetList().ToArray();
                count = args.Length;
            }
            else
            {
                args = ExApi.GetNObjects(vm, nargs);
                count = nargs;
            }

            for (int i = 0; i < count; i++)
            {
                if (vm.DoArithmeticOP(OPs.ExOperationCode.MLT, args[i], mul, ref res))
                {
                    mul.Value.f_Float = res.Value.f_Float;
                    mul.Value.c_Float = res.Value.c_Float;
                }
                else
                {
                    return ExFunctionStatus.ERROR;
                }
            }

            return vm.CleanReturn(nargs + 2, mul.Value.c_Float == 0.0 ? new ExObject(mul.Value.f_Float) : mul);
        }

        public static ExFunctionStatus MathSign(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);

            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Sign(i.GetInt()));
                    }
                case ExObjType.COMPLEX:
                    {
                        if (i.Value.c_Float == 0.0)
                        {
                            return vm.CleanReturn(nargs + 2, Math.Sign(i.Value.f_Float));
                        }
                        else
                        {
                            return vm.AddToErrorMessage("can't get complex number's sign");
                        }
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Sign(i.GetFloat()));
                    }
            }
        }

        public static ExFunctionStatus MathIsFIN(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            return vm.CleanReturn(nargs + 2, i.Type == ExObjType.COMPLEX ? Complex.IsFinite(i.GetComplex()) : double.IsFinite(i.GetFloat()));
        }

        public static ExFunctionStatus MathIsINF(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            return vm.CleanReturn(nargs + 2, i.Type == ExObjType.COMPLEX ? Complex.IsInfinity(i.GetComplex()) : double.IsPositiveInfinity(i.GetFloat()));
        }

        public static ExFunctionStatus MathIsNINF(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            return vm.CleanReturn(nargs + 2, i.Type == ExObjType.COMPLEX ? Complex.IsInfinity(i.GetComplex()) : double.IsNegativeInfinity(i.GetFloat()));
        }

        public static ExFunctionStatus MathIsNAN(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            return vm.CleanReturn(nargs + 2, i.Type == ExObjType.COMPLEX ? Complex.IsNaN(i.GetComplex()) : double.IsNaN(i.GetFloat()));
        }

        private static double[] CreateNumArr(ExVM vm, List<ExObject> l)
        {
            double[] a = new double[l.Count];
            for (int i = 0; i < l.Count; i++)
            {
                if (!l[i].IsNumeric())
                {
                    vm.AddToErrorMessage("cant plot non-numeric values");
                    return Array.Empty<double>();
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

            string[] fname = name.Split(".");
            if (fname.Length > 1)
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
        public static ExFunctionStatus MathPlotSaveScatters(ExVM vm, int nargs)
        {
            string name = vm.GetArgument(1).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return ExFunctionStatus.ERROR;
            }

            int width = (int)vm.GetPositiveIntegerArgument(3, 1200);
            int height = (int)vm.GetPositiveIntegerArgument(4, 800);

            ScottPlot.Plot plt = new(width, height);
            List<ExObject> plots = vm.GetArgument(2).GetList();
            bool hadlabels = false;

            foreach (ExObject plot in plots)
            {
                if (plot.Type != ExObjType.ARRAY)
                {
                    return vm.AddToErrorMessage("expected list of lists containing plot data");
                }

                List<ExObject> plotdata = plot.GetList();
                int ndata = plotdata.Count;
                System.Drawing.Color color = System.Drawing.Color.Blue;
                string label = null;

                switch (ndata)
                {
                    case 4:
                        {
                            if (plotdata[2].Type != ExObjType.STRING)
                            {
                                return vm.AddToErrorMessage("expected string for label");
                            }
                            label = plotdata[3].GetString();
                            hadlabels = true;
                            goto case 3;
                        }
                    case 3:
                        {
                            if (plotdata[2].Type != ExObjType.STRING)
                            {
                                return vm.AddToErrorMessage("expected string for color name");
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
                            return vm.AddToErrorMessage("not enough plot data given: [x,y,(color),(label)]");
                        }
                }

                if (plotdata[0].Type != ExObjType.ARRAY)
                {
                    return vm.AddToErrorMessage("expected list for X axis");
                }
                List<ExObject> x = plotdata[0].GetList();

                if (plotdata[1].Type != ExObjType.ARRAY)
                {
                    return vm.AddToErrorMessage("expected list for Y axis");
                }
                List<ExObject> y = plotdata[1].GetList();

                double[] X = CreateNumArr(vm, x);
                if (X.Length == 0)
                {
                    return ExFunctionStatus.ERROR;
                }
                double[] Y = CreateNumArr(vm, y);
                if (Y.Length == 0)
                {
                    return ExFunctionStatus.ERROR;
                }

                try
                {
                    plt.AddScatter(X, Y, color, label: label);
                }
                catch (Exception e)
                {
                    return vm.AddToErrorMessage("plot error: " + e.Message);
                }
            }

            try
            {
                plt.Legend(hadlabels);
                plt.SaveFig(name);
            }
            catch (Exception e)
            {
                return vm.AddToErrorMessage("plot error: " + e.Message);
            }

            return vm.CleanReturn(nargs + 2, name);
        }

        public static ExFunctionStatus MathPlotSaveScatter(ExVM vm, int nargs)
        {
            string name = vm.GetArgument(1).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return ExFunctionStatus.ERROR;
            }

            List<ExObject> x = vm.GetArgument(2).GetList();
            List<ExObject> y = vm.GetArgument(3).GetList();
            int width = (int)vm.GetPositiveIntegerArgument(4, 1200);
            int height = (int)vm.GetPositiveIntegerArgument(5, 800);

            System.Drawing.Color color = System.Drawing.Color.Blue;
            string label = null;

            switch (nargs)
            {
                case 7:
                    {
                        label = vm.GetArgument(7).GetString();
                        goto case 6;
                    }
                case 6:
                    {
                        color = System.Drawing.Color.FromName(vm.GetArgument(6).GetString().ToLower());
                        break;
                    }
            }

            double[] X = CreateNumArr(vm, x);
            if (X.Length == 0)
            {
                return ExFunctionStatus.ERROR;
            }
            double[] Y = CreateNumArr(vm, y);
            if (Y.Length == 0)
            {
                return ExFunctionStatus.ERROR;
            }

            ScottPlot.Plot plot = new(width, height);

            try
            {
                plot.AddScatter(X, Y, color, label: label);
                plot.Legend(!string.IsNullOrWhiteSpace(label));

                plot.SaveFig(name);
            }
            catch (Exception e)
            {
                return vm.AddToErrorMessage("plot error: " + e.Message);
            }

            return vm.CleanReturn(nargs + 2, name);
        }

        public static ExFunctionStatus MathPlotSaveScatterLines(ExVM vm, int nargs)
        {
            string name = vm.GetArgument(1).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return ExFunctionStatus.ERROR;
            }

            int width = (int)vm.GetPositiveIntegerArgument(3, 1200);
            int height = (int)vm.GetPositiveIntegerArgument(4, 800);

            ScottPlot.Plot plt = new(width, height);
            List<ExObject> plots = vm.GetArgument(2).GetList();
            bool hadlabels = false;

            foreach (ExObject plot in plots)
            {
                if (plot.Type != ExObjType.ARRAY)
                {
                    return vm.AddToErrorMessage("expected list of lists containing plot data");
                }

                List<ExObject> plotdata = plot.GetList();
                int ndata = plotdata.Count;
                System.Drawing.Color color = System.Drawing.Color.Blue;
                string label = null;

                switch (ndata)
                {
                    case 4:
                        {
                            if (plotdata[2].Type != ExObjType.STRING)
                            {
                                return vm.AddToErrorMessage("expected string for label");
                            }
                            label = plotdata[3].GetString();
                            hadlabels = true;
                            goto case 3;
                        }
                    case 3:
                        {
                            if (plotdata[2].Type != ExObjType.STRING)
                            {
                                return vm.AddToErrorMessage("expected string for color name");
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
                            return vm.AddToErrorMessage("not enough plot data given: [x,y,(color),(label)]");
                        }
                }

                if (plotdata[0].Type != ExObjType.ARRAY)
                {
                    return vm.AddToErrorMessage("expected list for X axis");
                }
                List<ExObject> x = plotdata[0].GetList();

                if (plotdata[1].Type != ExObjType.ARRAY)
                {
                    return vm.AddToErrorMessage("expected list for Y axis");
                }
                List<ExObject> y = plotdata[1].GetList();

                double[] X = CreateNumArr(vm, x);
                if (X.Length == 0)
                {
                    return ExFunctionStatus.ERROR;
                }
                double[] Y = CreateNumArr(vm, y);
                if (Y.Length == 0)
                {
                    return ExFunctionStatus.ERROR;
                }

                try
                {
                    plt.AddScatterLines(X, Y, color, label: label);
                }
                catch (Exception e)
                {
                    return vm.AddToErrorMessage("plot error: " + e.Message);
                }
            }

            try
            {
                plt.Legend(hadlabels);
                plt.SaveFig(name);
            }
            catch (Exception e)
            {
                return vm.AddToErrorMessage("plot error: " + e.Message);
            }

            return vm.CleanReturn(nargs + 2, name);
        }

        public static ExFunctionStatus MathPlotSaveScatterLine(ExVM vm, int nargs)
        {
            string name = vm.GetArgument(1).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return ExFunctionStatus.ERROR;
            }

            List<ExObject> x = vm.GetArgument(2).GetList();
            List<ExObject> y = vm.GetArgument(3).GetList();
            int width = (int)vm.GetPositiveIntegerArgument(4, 1200);
            int height = (int)vm.GetPositiveIntegerArgument(5, 800);

            System.Drawing.Color color = System.Drawing.Color.Blue;
            string label = null;

            switch (nargs)
            {
                case 7:
                    {
                        label = vm.GetArgument(7).GetString();
                        goto case 6;
                    }
                case 6:
                    {
                        color = System.Drawing.Color.FromName(vm.GetArgument(6).GetString().ToLower());
                        break;
                    }
            }

            double[] X = CreateNumArr(vm, x);
            if (X.Length == 0)
            {
                return ExFunctionStatus.ERROR;
            }
            double[] Y = CreateNumArr(vm, y);
            if (Y.Length == 0)
            {
                return ExFunctionStatus.ERROR;
            }

            ScottPlot.Plot plot = new(width, height);

            try
            {
                plot.AddScatterLines(X, Y, color, label: label);
                plot.Legend(!string.IsNullOrWhiteSpace(label));

                plot.SaveFig(name);
            }
            catch (Exception e)
            {
                return vm.AddToErrorMessage("plot error: " + e.Message);
            }

            return vm.CleanReturn(nargs + 2, name);
        }

        public static ExFunctionStatus MathPlotSaveScatterPoints(ExVM vm, int nargs)
        {
            string name = vm.GetArgument(1).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return ExFunctionStatus.ERROR;
            }

            int width = (int)vm.GetPositiveIntegerArgument(3, 1200);
            int height = (int)vm.GetPositiveIntegerArgument(4, 800);

            ScottPlot.Plot plt = new(width, height);
            List<ExObject> plots = vm.GetArgument(2).GetList();
            bool hadlabels = false;

            foreach (ExObject plot in plots)
            {
                if (plot.Type != ExObjType.ARRAY)
                {
                    return vm.AddToErrorMessage("expected list of lists containing plot data");
                }

                List<ExObject> plotdata = plot.GetList();
                int ndata = plotdata.Count;
                System.Drawing.Color color = System.Drawing.Color.Blue;
                string label = null;

                switch (ndata)
                {
                    case 4:
                        {
                            if (plotdata[2].Type != ExObjType.STRING)
                            {
                                return vm.AddToErrorMessage("expected string for label");
                            }
                            label = plotdata[3].GetString();
                            hadlabels = true;
                            goto case 3;
                        }
                    case 3:
                        {
                            if (plotdata[2].Type != ExObjType.STRING)
                            {
                                return vm.AddToErrorMessage("expected string for color name");
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
                            return vm.AddToErrorMessage("not enough plot data given: [x,y,(color),(label)]");
                        }
                }

                if (plotdata[0].Type != ExObjType.ARRAY)
                {
                    return vm.AddToErrorMessage("expected list for X axis");
                }
                List<ExObject> x = plotdata[0].GetList();

                if (plotdata[1].Type != ExObjType.ARRAY)
                {
                    return vm.AddToErrorMessage("expected list for Y axis");
                }
                List<ExObject> y = plotdata[1].GetList();

                double[] X = CreateNumArr(vm, x);
                if (X.Length == 0)
                {
                    return ExFunctionStatus.ERROR;
                }
                double[] Y = CreateNumArr(vm, y);
                if (Y.Length == 0)
                {
                    return ExFunctionStatus.ERROR;
                }

                try
                {
                    plt.AddScatterPoints(X, Y, color, label: label);
                }
                catch (Exception e)
                {
                    return vm.AddToErrorMessage("plot error: " + e.Message);
                }
            }

            try
            {
                plt.Legend(hadlabels);
                plt.SaveFig(name);
            }
            catch (Exception e)
            {
                return vm.AddToErrorMessage("plot error: " + e.Message);
            }

            return vm.CleanReturn(nargs + 2, name);
        }

        public static ExFunctionStatus MathPlotSaveScatterPoint(ExVM vm, int nargs)
        {
            string name = vm.GetArgument(1).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return ExFunctionStatus.ERROR;
            }

            List<ExObject> x = vm.GetArgument(2).GetList();
            List<ExObject> y = vm.GetArgument(3).GetList();
            int width = (int)vm.GetPositiveIntegerArgument(4, 1200);
            int height = (int)vm.GetPositiveIntegerArgument(5, 800);

            System.Drawing.Color color = System.Drawing.Color.Blue;
            string label = null;

            switch (nargs)
            {
                case 7:
                    {
                        label = vm.GetArgument(7).GetString();
                        goto case 6;
                    }
                case 6:
                    {
                        color = System.Drawing.Color.FromName(vm.GetArgument(6).GetString().ToLower());
                        break;
                    }
            }

            double[] X = CreateNumArr(vm, x);
            if (X.Length == 0)
            {
                return ExFunctionStatus.ERROR;
            }
            double[] Y = CreateNumArr(vm, y);
            if (Y.Length == 0)
            {
                return ExFunctionStatus.ERROR;
            }

            ScottPlot.Plot plot = new(width, height);

            try
            {
                plot.AddScatterPoints(X, Y, color, label: label);
                plot.Legend(!string.IsNullOrWhiteSpace(label));

                plot.SaveFig(name);
            }
            catch (Exception e)
            {
                return vm.AddToErrorMessage("plot error: " + e.Message);
            }

            return vm.CleanReturn(nargs + 2, name);
        }

        public static ExFunctionStatus MathPlotSaveScatterSteps(ExVM vm, int nargs)
        {
            string name = vm.GetArgument(1).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return ExFunctionStatus.ERROR;
            }

            int width = (int)vm.GetPositiveIntegerArgument(3, 1200);
            int height = (int)vm.GetPositiveIntegerArgument(4, 800);

            ScottPlot.Plot plt = new(width, height);
            List<ExObject> plots = vm.GetArgument(2).GetList();
            bool hadlabels = false;

            foreach (ExObject plot in plots)
            {
                if (plot.Type != ExObjType.ARRAY)
                {
                    return vm.AddToErrorMessage("expected list of lists containing plot data");
                }

                List<ExObject> plotdata = plot.GetList();
                int ndata = plotdata.Count;
                System.Drawing.Color color = System.Drawing.Color.Blue;
                string label = null;

                switch (ndata)
                {
                    case 4:
                        {
                            if (plotdata[2].Type != ExObjType.STRING)
                            {
                                return vm.AddToErrorMessage("expected string for label");
                            }
                            label = plotdata[3].GetString();
                            hadlabels = true;
                            goto case 3;
                        }
                    case 3:
                        {
                            if (plotdata[2].Type != ExObjType.STRING)
                            {
                                return vm.AddToErrorMessage("expected string for color name");
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
                            return vm.AddToErrorMessage("not enough plot data given: [x,y,(color),(label)]");
                        }
                }

                if (plotdata[0].Type != ExObjType.ARRAY)
                {
                    return vm.AddToErrorMessage("expected list for X axis");
                }
                List<ExObject> x = plotdata[0].GetList();

                if (plotdata[1].Type != ExObjType.ARRAY)
                {
                    return vm.AddToErrorMessage("expected list for Y axis");
                }
                List<ExObject> y = plotdata[1].GetList();

                double[] X = CreateNumArr(vm, x);
                if (X.Length == 0)
                {
                    return ExFunctionStatus.ERROR;
                }
                double[] Y = CreateNumArr(vm, y);
                if (Y.Length == 0)
                {
                    return ExFunctionStatus.ERROR;
                }

                try
                {
                    plt.AddScatterStep(X, Y, color, label: label);
                }
                catch (Exception e)
                {
                    return vm.AddToErrorMessage("plot error: " + e.Message);
                }
            }

            try
            {
                plt.Legend(hadlabels);
                plt.SaveFig(name);
            }
            catch (Exception e)
            {
                return vm.AddToErrorMessage("plot error: " + e.Message);
            }

            return vm.CleanReturn(nargs + 2, name);
        }

        public static ExFunctionStatus MathPlotSaveScatterStep(ExVM vm, int nargs)
        {
            string name = vm.GetArgument(1).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return ExFunctionStatus.ERROR;
            }

            List<ExObject> x = vm.GetArgument(2).GetList();
            List<ExObject> y = vm.GetArgument(3).GetList();
            int width = (int)vm.GetPositiveIntegerArgument(4, 1200);
            int height = (int)vm.GetPositiveIntegerArgument(5, 800);

            System.Drawing.Color color = System.Drawing.Color.Blue;
            string label = null;

            switch (nargs)
            {
                case 7:
                    {
                        label = vm.GetArgument(7).GetString();
                        goto case 6;
                    }
                case 6:
                    {
                        color = System.Drawing.Color.FromName(vm.GetArgument(6).GetString().ToLower());
                        break;
                    }
            }

            double[] X = CreateNumArr(vm, x);
            if (X.Length == 0)
            {
                return ExFunctionStatus.ERROR;
            }
            double[] Y = CreateNumArr(vm, y);
            if (Y.Length == 0)
            {
                return ExFunctionStatus.ERROR;
            }

            ScottPlot.Plot plot = new(width, height);

            try
            {
                plot.AddScatterStep(X, Y, color, label: label);
                plot.Legend(!string.IsNullOrWhiteSpace(label));

                plot.SaveFig(name);
            }
            catch (Exception e)
            {
                return vm.AddToErrorMessage("plot error: " + e.Message);
            }

            return vm.CleanReturn(nargs + 2, name);
        }

        public static ExFunctionStatus MathPlotSaveScatterComplex(ExVM vm, int nargs)
        {
            string name = vm.GetArgument(1).GetString();
            if (!CheckFileName(vm, ref name))
            {
                return ExFunctionStatus.ERROR;
            }

            List<ExObject> x = vm.GetArgument(2).GetList();
            int count = x.Count;

            double[] X = new double[count];
            double[] Y = new double[count];
            for (int i = 0; i < count; i++)
            {
                ExObject v = x[i];
                if (v.Type != ExObjType.COMPLEX)
                {
                    if (!v.IsNumeric())
                    {
                        return vm.AddToErrorMessage("cant plot non-numeric in complex plane");
                    }
                    X[i] = v.GetFloat();
                    Y[i] = 0.0;
                }
                else
                {
                    X[i] = v.Value.f_Float;
                    Y[i] = v.Value.c_Float;
                }
            }

            int width = (int)vm.GetPositiveIntegerArgument(3, 1200);
            int height = (int)vm.GetPositiveIntegerArgument(4, 800);

            System.Drawing.Color color = System.Drawing.Color.Blue;
            string label = null;

            switch (nargs)
            {
                case 6:
                    {
                        label = vm.GetArgument(6).GetString();
                        goto case 5;
                    }
                case 5:
                    {
                        color = System.Drawing.Color.FromName(vm.GetArgument(5).GetString().ToLower());
                        break;
                    }
            }

            ScottPlot.Plot plot = new(width, height);

            try
            {
                plot.AddScatterPoints(X, Y, color, label: label);
                plot.Legend(!string.IsNullOrWhiteSpace(label));

                plot.SaveFig(name);
            }
            catch (Exception e)
            {
                return vm.AddToErrorMessage("plot error: " + e.Message);
            }

            return vm.CleanReturn(nargs + 2, name);
        }

        private static readonly List<ExRegFunc> _stdmathfuncs = new()
        {
            new()
            {
                Name = "srand",
                Function = MathSrand,
                Parameters = new()
                {
                    new("seed", "n", "Seed to use")
                },
                Description = "Set the seed used for random number generators"
            },
            new()
            {
                Name = "rand",
                Function = MathRand,
                Parameters = new()
                {
                    new("bound1", "n", "If used alone: [0,bound1), otherwise: [bound1, bound2)", new(0)),
                    new("bound2", "n", "Upper bound for number range", new(int.MaxValue))
                },
                Returns = ExBaseType.INTEGER,
                Description = $"Get a random integer in given range: [0, {int.MaxValue}) , [0, bound1) or [bound1, bound2)"
            },
            new()
            {
                Name = "randf",
                Function = MathRandf,
                Parameters = new()
                {
                    new("bound1", "n", "If used alone: [0,bound1), otherwise: [bound1, bound2)", new(0.0)),
                    new("bound2", "n", "Upper bound for number range", new(1.0))
                },
                Returns = ExBaseType.FLOAT,
                Description = "Get a random float in given range: [0, 1) , [0, bound1) or [bound1, bound2)"
            },

            new()
            {
                Name = "isDivisible",
                Function = MathIsDivisible,
                Parameters = new()
                {
                    new("numerator", "i", "Numerator"),
                    new("denominator", "i", "Denominator")
                },
                Returns = ExBaseType.BOOL,
                Description = "Check divisibility of (numerator / denominator)"
            },
            new()
            {
                Name = "divRem",
                Function = MathDivRem,
                Parameters = new()
                {
                    new("numerator", "i", "Numerator"),
                    new("denominator", "i", "Denominator")
                },
                Returns = ExBaseType.INTEGER,
                Description = "Get the remainder from (numerator / denominator)"
            },
            new()
            {
                Name = "divQuot",
                Function = MathDivQuot,
                Parameters = new()
                {
                    new("numerator", "i", "Numerator"),
                    new("denominator", "i", "Denominator")
                },
                Returns = ExBaseType.INTEGER,
                Description = "Get the quotient from (numerator / denominator)"
            },
            new()
            {
                Name = "divRemQuot",
                Function = MathDivRemQuot,
                Parameters = new()
                {
                    new("numerator", "i", "Numerator"),
                    new("denominator", "i", "Denominator")
                },
                Returns = ExBaseType.ARRAY,
                Description = "Get the remainder and the quotient from (numerator / denominator) in a list."
            },
            new()
            {
                Name = "recip",
                Function = MathRecip,
                Parameters = new()
                {
                    new("value", "i", "Value to get 1/value of"),
                },
                Returns = ExBaseType.FLOAT,
                Description = "Get the reciprocal of a value, that is 1/value."
            },
            new()
            {
                Name = "GCD",
                Function = MathGcd,
                Parameters = new()
                {
                    new("num1", "r", "Value 1"),
                    new("num2", "r", "Value 2")
                },
                Returns = ExBaseType.FLOAT,
                Description = "Get the greatest common divisor(GCD) of 2 numbers"
            },
            new()
            {
                Name = "LCD",
                Function = MathLcd,
                Parameters = new()
                {
                    new("num1", "r", "Value 1"),
                    new("num2", "r", "Value 2")
                },
                Returns = ExBaseType.FLOAT,
                Description = "Get the least common denominator(LCD) of 2 numbers"
            },
            new()
            {
                Name = "factorize",
                Function = MathPrimeFactors,
                Parameters = new()
                {
                    new("positive_num", "r", "A positive value to factorize")
                },
                Returns = ExBaseType.ARRAY,
                Description = "Get the prime factorization of a positive number. An empty list is returned for negative values."
            },
            new()
            {
                Name = "next_prime",
                Function = MathNextPrime,
                Parameters = new()
                {
                    new("start", "i", "Starting value to get next closest prime of")
                },
                Returns = ExBaseType.INTEGER,
                Description = "Get the next closest prime bigger than the given value"
            },
            new()
            {
                Name = "prime",
                Function = MathPrime,
                Parameters = new()
                {
                    new("n", "i", "Index of the prime, that is n'th prime.")
                },
                Returns = ExBaseType.INTEGER,
                Description = "Get the n'th prime number"
            },
            new()
            {
                Name = "isPrime",
                Function = MathIsPrime,
                Parameters = new()
                {
                    new("value", "i", "Value to check")
                },
                Returns = ExBaseType.BOOL,
                Description = "Check wheter the given number is a prime number."
            },
            new()
            {
                Name = "areCoPrime",
                Function = MathAreCoprime,
                Parameters = new()
                {
                    new("num1", "r", "Value 1"),
                    new("num2", "r", "Value 2")
                },
                Returns = ExBaseType.BOOL,
                Description = "Check if given 2 values are coprimes."
            },
            new()
            {
                Name = "digits",
                Function = MathDigits,
                Parameters = new()
                {
                    new("value", "i", "Value to get digits of")
                },
                Returns = ExBaseType.ARRAY,
                Description = "Get the digits of an integer value in a list."
            },
            new()
            {
                Name = "abs",
                Function = MathAbs,
                Parameters = new()
                {
                    new("value", "n", "Value to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.INTEGER,
                Description = "Get the absolute value of a number or the magnitute of a complex number."
            },
            new()
            {
                Name = "sqrt",
                Function = MathSqrt,
                Parameters = new()
                {
                    new("value", "n", "Value to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.INTEGER | ExBaseType.COMPLEX,
                Description = "Get the square root of a number."
            },
            new()
            {
                Name = "cbrt",
                Function = MathCbrt,
                Parameters = new()
                {
                    new("value", "n", "Value to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.INTEGER | ExBaseType.COMPLEX,
                Description = "Get the cube root of a number."
            },

            new()
            {
                Name = "sin",
                Function = MathSin,
                Parameters = new()
                {
                    new("value", "n", "Radians to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the sin of a number. Uses radians."
            },
            new()
            {
                Name = "cos",
                Function = MathCos,
                Parameters = new()
                {
                    new("value", "n", "Radians to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the cos of a number. Uses radians."
            },
            new()
            {
                Name = "tan",
                Function = MathTan,
                Parameters = new()
                {
                    new("value", "n", "Radians to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the tan of a number. Uses radians."
            },
            new()
            {
                Name = "sinh",
                Function = MathSinh,
                Parameters = new()
                {
                    new("value", "n", "Radians to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the sinh of a number. Uses radians."
            },
            new()
            {
                Name = "cosh",
                Function = MathCosh,
                Parameters = new()
                {
                    new("value", "n", "Radians to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the cosh of a number. Uses radians."
            },
            new()
            {
                Name = "tanh",
                Function = MathTanh,
                Parameters = new()
                {
                    new("value", "n", "Radians to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the tanh of a number. Uses radians."
            },

            new()
            {
                Name = "asin",
                Function = MathAsin,
                Parameters = new()
                {
                    new("value", "n", "Value to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the arcsin of a number."
            },
            new()
            {
                Name = "acos",
                Function = MathAcos,
                Parameters = new()
                {
                    new("value", "n", "Value to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the arccos of a number."
            },
            new()
            {
                Name = "atan",
                Function = MathAtan,
                Parameters = new()
                {
                    new("value", "n", "Value to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the arctan of a number."
            },
            new()
            {
                Name = "atan2",
                Function = MathAtan2,
                Parameters = new()
                {
                    new("y", "n", "Cartesian plane x coordinate"),
                    new("x", "n", "Cartesian plane y coordinate")
                },
                Returns = ExBaseType.FLOAT,
                Description = "Return the angle whose tangent is the quotient of two specified numbers. An angle, θ, measured in radians, such that -π ≤ θ ≤ π, and tan(θ) = y / x, where (x, y) is a point in the Cartesian plane."
            },
            new()
            {
                Name = "asinh",
                Function = MathAsinh,
                Parameters = new()
                {
                    new("value", "n", "Value to use")
                },
                Returns = ExBaseType.FLOAT,
                Description = "Get the arcsinh of a number."
            },
            new()
            {
                Name = "acosh",
                Function = MathAcosh,
                Parameters = new()
                {
                    new("value", "n", "Value to use")
                },
                Returns = ExBaseType.FLOAT,
                Description = "Get the arccosh of a number."
            },
            new()
            {
                Name = "atanh",
                Function = MathAtanh,
                Parameters = new()
                {
                    new("value", "n", "Value to use")
                },
                Returns = ExBaseType.FLOAT,
                Description = "Get the arctanh of a number."
            },

            new()
            {
                Name = "loge",
                Function = MathLoge,
                Parameters = new()
                {
                    new("value", "n", "Value to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = $"Get the base e ({Math.E}) logarithm, that is the natural logarithm, of a number."
            },
            new()
            {
                Name = "log2",
                Function = MathLog2,
                Parameters = new()
                {
                    new("value", "n", "Value to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the base 2 logarithm of a number."
            },
            new()
            {
                Name = "log10",
                Function = MathLog10,
                Parameters = new()
                {
                    new("value", "n", "Value to use")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the base 10 logarithm of a number."
            },
            new()
            {
                Name = "log",
                Function = MathLog,
                Parameters = new()
                {
                    new("a", "n", "Argument"),
                    new("b", "n", "Base", new(Math.E))
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the base 'b' logarithm of 'a', that is log'b'('a') == log(a,b). If 'b' is not given, works same as 'loge' function, that is loge('a') == log(a) == log(a,E) == ln(a)"
            },
            new()
            {
                Name = "exp",
                Function = MathExp,
                Parameters = new()
                {
                    new("x", "n", "Value to raise E to")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the natural exponential function's value at 'x', that is E raised to the power of 'x'"
            },
            new()
            {
                Name = "round",
                Function = MathRound,
                Parameters = new()
                {
                    new("value", "n", "Value to round"),
                    new("digits", "n", "Digits to round to", new(0))
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Round a number to given digits"
            },
            new()
            {
                Name = "floor",
                Function = MathFloor,
                Parameters = new()
                {
                    new("value", "n", "Value to round")
                },
                Returns = ExBaseType.INTEGER | ExBaseType.COMPLEX,
                Description = "Round a number to closest integer which is lower than the value."
            },
            new()
            {
                Name = "ceil",
                Function = MathCeil,
                Parameters = new()
                {
                    new("value", "n", "Value to round")
                },
                Returns = ExBaseType.INTEGER | ExBaseType.COMPLEX,
                Description = "Round a number to closest integer which is higher than the value."
            },
            new()
            {
                Name = "pow",
                Function = MathPow,
                Parameters = new()
                {
                    new("value", "n", "Value to raise"),
                    new("power", "n", "Power to raise to")
                },
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Raise a number to the given power"
            },

            new()
            {
                Name = "sum",
                Function = MathSum,
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Return the sum of given arguments or the sum of items of the given list."
            },
            new()
            {
                Name = "mul",
                Function = MathMul,
                Returns = ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Return the product of given arguments or the product of items of the given list."
            },

            new()
            {
                Name = "min",
                Function = MathMin,
                Parameters = new()
                {
                    new("value1", "n", "Value 1"),
                    new("value2", "n", "Value 2")
                },
                Returns = ExBaseType.INTEGER | ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the minimum of two given values"
            },
            new()
            {
                Name = "max",
                Function = MathMax,
                Parameters = new()
                {
                    new("value1", "n", "Value 1"),
                    new("value2", "n", "Value 2")
                },
                Returns = ExBaseType.INTEGER | ExBaseType.FLOAT | ExBaseType.COMPLEX,
                Description = "Get the maximum of two given values"
            },
            new()
            {
                Name = "sign",
                Function = MathSign,
                Parameters = new()
                {
                    new("value", "n", "Value to get the sign of")
                },
                Returns = ExBaseType.INTEGER,
                Description = "Get the sign of the given value. Returns -1: Negative, 0: Zero, 1: Positive"
            },

            new()
            {
                Name = "isFIN",
                Function = MathIsFIN,
                Parameters = new()
                {
                    new("value", "n", "Value to check")
                },
                Returns = ExBaseType.BOOL,
                Description = "Check if given value is finite"
            },
            new()
            {
                Name = "isINF",
                Function = MathIsINF,
                Parameters = new()
                {
                    new("value", "n", "Value to check")
                },
                Returns = ExBaseType.BOOL,
                Description = "Check if given value is positive infinity (INF)"
            },
            new()
            {
                Name = "isNINF",
                Function = MathIsNINF,
                Parameters = new()
                {
                    new("value", "n", "Value to check")
                },
                Returns = ExBaseType.BOOL,
                Description = "Check if given value is negative infinity (NINF)"
            },
            new()
            {
                Name = "isNAN",
                Function = MathIsNAN,
                Parameters = new()
                {
                    new("value", "n", "Value to check")
                },
                Returns = ExBaseType.BOOL,
                Description = "Check if given value is NaN (NAN)"
            },
            new()
            {
                Name = "save_scatter",
                Function = MathPlotSaveScatter,
                Parameters = new()
                {
                    new("filename", "s", "Image file name to save as"),
                    new("x", "a", "X axis data"),
                    new("y", "a", "Y axis data"),
                    new("width", "n", "Width of the image", new(1200)),
                    new("height", "n", "Height of the image", new(800)),
                    new("color", "s", "Plot data point color name", new("blue")),
                    new("plot_label", "s", "Plot label, null to not use any labels", new(s: null))
                },
                Returns = ExBaseType.STRING,
                Description = "Save a plot of data points and lines connecting them, using given data lists and plot information as an image"
            },
            new()
            {
                Name = "save_scatters",
                Function = MathPlotSaveScatters,
                Parameters = new()
                {
                    new("filename", "s", "Image file name to save as"),
                    new("data_list", "a", "List of plot information lists. Each list must follow the format:\n\t [<list> x_axis, <list> y_axis, {OPTIONAL<string> color = \"blue\"},  {OPTIONAL<string?> label = null}]"),
                    new("width", "n", "Width of the image", new(1200)),
                    new("height", "n", "Height of the image", new(800))
                },
                Returns = ExBaseType.STRING,
                Description = "Save multiple plots of data points and lines connecting them in a single plot, using given data lists and plot information as an image"
            },
            new()
            {
                Name = "save_scatter_step",
                Function = MathPlotSaveScatterStep,
                Parameters = new()
                {
                    new("filename", "s", "Image file name to save as"),
                    new("x", "a", "X axis data"),
                    new("y", "a", "Y axis data"),
                    new("width", "n", "Width of the image", new(1200)),
                    new("height", "n", "Height of the image", new(800)),
                    new("color", "s", "Plot data point color name", new("blue")),
                    new("plot_label", "s", "Plot label, null to not use any labels", new(s: null))
                },
                Returns = ExBaseType.STRING,
                Description = "Save a step plot using given data lists and plot information as an image"
            },
            new()
            {
                Name = "save_scatter_steps",
                Function = MathPlotSaveScatterSteps,
                Parameters = new()
                {
                    new("filename", "s", "Image file name to save as"),
                    new("data_list", "a", "List of plot information lists. Each list must follow the format:\n\t [<list> x_axis, <list> y_axis, {OPTIONAL<string> color = \"blue\"},  {OPTIONAL<string?> label = null}]"),
                    new("width", "n", "Width of the image", new(1200)),
                    new("height", "n", "Height of the image", new(800))
                },
                Returns = ExBaseType.STRING,
                Description = "Save multiple step plots in a single plot, using given data lists and plot information as an image"
            },
            new()
            {
                Name = "save_scatter_point",
                Function = MathPlotSaveScatterPoint,
                Parameters = new()
                {
                    new("filename", "s", "Image file name to save as"),
                    new("x", "a", "X axis data"),
                    new("y", "a", "Y axis data"),
                    new("width", "n", "Width of the image", new(1200)),
                    new("height", "n", "Height of the image", new(800)),
                    new("color", "s", "Plot data point color name", new("blue")),
                    new("plot_label", "s", "Plot label, null to not use any labels", new(s: null))
                },
                Returns = ExBaseType.STRING,
                Description = "Save a scatter plot with data points only, using given data lists and plot information as an image"
            },
            new()
            {
                Name = "save_scatter_points",
                Function = MathPlotSaveScatterPoints,
                Parameters = new()
                {
                    new("filename", "s", "Image file name to save as"),
                    new("data_list", "a", "List of plot information lists. Each list must follow the format:\n\t [<list> x_axis, <list> y_axis, {OPTIONAL<string> color = \"blue\"},  {OPTIONAL<string?> label = null}]"),
                    new("width", "n", "Width of the image", new(1200)),
                    new("height", "n", "Height of the image", new(800))
                },
                Returns = ExBaseType.STRING,
                Description = "Save multiple scatter plots in a single plot, using given data lists and plot information as an image"
            },
            new()
            {
                Name = "save_scatter_line",
                Function = MathPlotSaveScatterLine,
                Parameters = new()
                {
                    new("filename", "s", "Image file name to save as"),
                    new("x", "a", "X axis data"),
                    new("y", "a", "Y axis data"),
                    new("width", "n", "Width of the image", new(1200)),
                    new("height", "n", "Height of the image", new(800)),
                    new("color", "s", "Plot data point color name", new("blue")),
                    new("plot_label", "s", "Plot label, null to not use any labels", new(s: null))
                },
                Returns = ExBaseType.STRING,
                Description = "Save a plot of line plot connecting data points, using given data lists and plot information as an image"
            },
            new()
            {
                Name = "save_scatter_lines",
                Function = MathPlotSaveScatterLines,
                Parameters = new()
                {
                    new("filename", "s", "Image file name to save as"),
                    new("data_list", "a", "List of plot information lists. Each list must follow the format:\n\t [<list> x_axis, <list> y_axis, {OPTIONAL<string> color = \"blue\"},  {OPTIONAL<string?> label = null}]"),
                    new("width", "n", "Width of the image", new(1200)),
                    new("height", "n", "Height of the image", new(800))
                },
                Returns = ExBaseType.STRING,
                Description = "Save multiple plots of line plots in a single plot, using given data lists and plot information as an image"
            },
            new()
            {
                Name = "save_complex",
                Function = MathPlotSaveScatterComplex,
                Parameters = new()
                {
                    new("filename", "s", "Image file name to save as"),
                    new("complex_nums", "a", "Complex number list to plot"),
                    new("width", "n", "Width of the image", new(1200)),
                    new("height", "n", "Height of the image", new(800)),
                    new("color", "s", "Plot data point color name", new("blue")),
                    new("plot_label", "s", "Plot label, null to not use any labels", new(s: null))
                },
                Returns = ExBaseType.STRING,
                Description = "Save a scatter plot of complex numbers, using given plot information as an image"
            }
        };

        private static Random rand = new();
        public static List<ExRegFunc> MathFuncs => _stdmathfuncs;
        public static Random Rand { get => rand; set => rand = value; }

        public static void RegisterStdMathConstants(ExVM vm)
        {
            ExApi.PushRootTable(vm);

            ExApi.CreateConstantInt(vm, "INT8_MAX", sbyte.MaxValue);
            ExApi.CreateConstantInt(vm, "INT8_MIN", sbyte.MinValue);

            ExApi.CreateConstantInt(vm, "INT16_MAX", short.MaxValue);
            ExApi.CreateConstantInt(vm, "INT16_MIN", short.MinValue);

            ExApi.CreateConstantInt(vm, "INT32_MAX", int.MaxValue);
            ExApi.CreateConstantInt(vm, "INT32_MIN", int.MinValue);

            ExApi.CreateConstantInt(vm, "INT64_MAX", long.MaxValue);
            ExApi.CreateConstantInt(vm, "INT64_MIN", long.MinValue);

            ExApi.CreateConstantInt(vm, "UINT8_MAX", byte.MaxValue);
            ExApi.CreateConstantInt(vm, "UINT16_MAX", ushort.MaxValue);
            ExApi.CreateConstantInt(vm, "UINT32_MAX", uint.MaxValue);

            ExApi.CreateConstantFloat(vm, "FLOAT32_MAX", float.MaxValue);
            ExApi.CreateConstantFloat(vm, "FLOAT32_MIN", float.MinValue);

            ExApi.CreateConstantFloat(vm, "FLOAT64_MAX", double.MaxValue);
            ExApi.CreateConstantFloat(vm, "FLOAT64_MIN", double.MinValue);

            ExApi.CreateConstantFloat(vm, "TAU", Math.Tau);
            ExApi.CreateConstantFloat(vm, "PI", Math.PI);
            ExApi.CreateConstantFloat(vm, "E", Math.E);
            ExApi.CreateConstantFloat(vm, "GOLDEN", (1.0 + Math.Sqrt(5.0)) / 2.0);
            ExApi.CreateConstantFloat(vm, "DEGREE", Math.PI / 180.0);
            ExApi.CreateConstantFloat(vm, "EPSILON", double.Epsilon);

            ExApi.CreateConstantFloat(vm, "NAN", double.NaN);
            ExApi.CreateConstantFloat(vm, "NINF", double.NegativeInfinity);
            ExApi.CreateConstantFloat(vm, "INF", double.PositiveInfinity);

            ExApi.CreateConstantDict(vm, "SPACES", new()
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

            vm.Pop(1);
        }

        public static bool RegisterStdMath(ExVM vm)
        {
            ExApi.RegisterNativeFunctions(vm, MathFuncs, ExStdLibType.MATH);

            RegisterStdMathConstants(vm);

            return true;
        }
    }
}
