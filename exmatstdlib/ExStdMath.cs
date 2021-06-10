using System;
using System.Collections.Generic;
using System.Numerics;
using ExMat.API;
using ExMat.Objects;
using ExMat.VM;

namespace ExMat.BaseLib
{
    public static class ExStdMath
    {
        private static Random rand = new();
        private static List<int> Primes;
        private static readonly int PrimeSearchSize = int.MaxValue / 100;
        private static readonly int PrimeCacheMaxSize = 1358124 * 2;
        private static int PrimeCount = 1358124;
        private static int PrimeMax = 21474829;

        public static int MathSrand(ExVM vm, int nargs)
        {
            int seed = (int)ExAPI.GetFromStack(vm, 2).GetInt();
            vm.Pop(3);
            vm.Push(true);
            Rand = new(seed);
            return 0;
        }

        public static int MathRand(ExVM vm, int nargs)
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

        public static int MathRandf(ExVM vm, int nargs)
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

        public static int MathNextPrime(ExVM vm, int nargs)
        {
            long a = ExAPI.GetFromStack(vm, 2).GetInt();

            if (Primes == null)
            {
                FindPrimes();
            }

            vm.Pop(nargs + 2);

            int idx;
            if (a > 0 && a <= PrimeMax && (idx = Primes.BinarySearch(item: (int)a)) >= 0 && idx < PrimeCount)
            {
                vm.Push(Primes[++idx]);
            }
            else
            {
                vm.Push(NextClosestPrime(a));
            }

            return 1;
        }

        public static int MathIsPrime(ExVM vm, int nargs)
        {
            long a = ExAPI.GetFromStack(vm, 2).GetInt();

            vm.Pop(nargs + 2);
            vm.Push((Primes != null && a > 0 && a <= PrimeMax && Primes.BinarySearch(item: (int)a) >= 0) || IsPrime(a));

            return 1;
        }

        public static int MathAreCoprime(ExVM vm, int nargs)
        {
            long a = ExAPI.GetFromStack(vm, 2).GetInt();
            long b = ExAPI.GetFromStack(vm, 3).GetInt();

            vm.Pop(nargs + 2);
            vm.Push(GetGCD(a, b) == 1.0);

            return 1;
        }

        public static int MathPrime(ExVM vm, int nargs)
        {
            int a = (int)ExAPI.GetFromStack(vm, 2).GetInt();

            if (Primes == null)
            {
                FindPrimes();
            }

            int n = Primes.Count;
            if (a <= 0)
            {
                vm.AddToErrorMessage("expected positive integer for 'n'th prime index");
                return -1;
            }
            if (a > n)
            {
                vm.Pop(nargs + 2);
                // TO-DO Find out why, for some reason doing this sometimes reduces memory use by half for the prime list
                vm.Push(FindAndCacheNextPrimes(a - n));
                return 1;
            }

            vm.Pop(nargs + 2);
            vm.Push(Primes[--a]);

            return 1;
        }

        public static int MathPrimeFactors(ExVM vm, int nargs)
        {
            long a = ExAPI.GetFromStack(vm, 2).GetInt();

            if (Primes != null && a > 0 && a <= PrimeMax && Primes.BinarySearch(item: (int)a) >= 0)
            {
                vm.Pop(nargs + 2);
                vm.Push(new List<ExObject>(1) { new(a) });

                return 1;
            }

            if (a < 0)
            {
                vm.Pop(nargs + 2);
                vm.Push(new List<ExObject>());
                return 1;
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

            vm.Pop(nargs + 2);
            vm.Push(lis);

            return 1;
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

        public static int MathGcd(ExVM vm, int nargs)
        {
            double a = ExAPI.GetFromStack(vm, 2).GetFloat();
            double b = ExAPI.GetFromStack(vm, 3).GetFloat();

            vm.Pop(nargs + 2);
            vm.Push(GetGCD(a, b));

            return 1;
        }

        public static int MathLcd(ExVM vm, int nargs)
        {
            double a = ExAPI.GetFromStack(vm, 2).GetFloat();
            double b = ExAPI.GetFromStack(vm, 3).GetFloat();
            vm.Pop(nargs + 2);
            vm.Push(Math.Abs(a * b) / GetGCD(a, b));

            return 1;
        }

        public static int MathIsDivisible(ExVM vm, int nargs)
        {
            long a = ExAPI.GetFromStack(vm, 2).GetInt();
            long b = ExAPI.GetFromStack(vm, 3).GetInt();
            vm.Pop(nargs + 2);
            _ = Math.DivRem(a, b, out long r);
            vm.Push(r == 0);

            return 1;
        }

        public static int MathDivQuot(ExVM vm, int nargs)
        {
            long a = ExAPI.GetFromStack(vm, 2).GetInt();
            long b = ExAPI.GetFromStack(vm, 3).GetInt();

            long quot = Math.DivRem(a, b, out long _);
            vm.Pop(nargs + 2);
            vm.Push(quot);

            return 1;
        }

        public static int MathDivRem(ExVM vm, int nargs)
        {
            long a = ExAPI.GetFromStack(vm, 2).GetInt();
            long b = ExAPI.GetFromStack(vm, 3).GetInt();

            Math.DivRem(a, b, out long rem);
            vm.Pop(nargs + 2);
            vm.Push(rem);

            return 1;
        }

        public static int MathDivRemQuot(ExVM vm, int nargs)
        {
            long a = ExAPI.GetFromStack(vm, 2).GetInt();
            long b = ExAPI.GetFromStack(vm, 3).GetInt();

            long quot = Math.DivRem(a, b, out long rem);
            vm.Pop(nargs + 2);
            vm.Push(new List<ExObject>(2) { new(rem), new(quot) });

            return 1;
        }

        public static int MathRecip(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        double o = 1.0 / i.GetInt();
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                case ExObjType.COMPLEX:
                    {
                        Complex o = Complex.Reciprocal(i.GetComplex());
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
                default:
                    {
                        double o = 1.0 / i.GetFloat();
                        vm.Pop(nargs + 2);
                        vm.Push(o);
                        break;
                    }
            }

            return 1;
        }
        public static int MathDigits(ExVM vm, int nargs)
        {
            long i = ExAPI.GetFromStack(vm, 2).GetInt();
            string s = i.ToString();
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
            vm.Pop(nargs + 2);
            vm.Push(lis);
            return 1;
        }

        public static int MathAbs(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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

        public static int MathSqrt(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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

        public static int MathCbrt(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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
        public static int MathSin(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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
        public static int MathSinh(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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

        public static int MathCos(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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
        public static int MathCosh(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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

        public static int MathTan(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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
        public static int MathTanh(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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

        public static int MathAcos(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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
        public static int MathAcosh(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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
                        if (i.Value.c_Float == 0.0)
                        {
                            double o = i.Value.f_Float;
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

        public static int MathAsin(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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
        public static int MathAsinh(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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
                        if (i.Value.c_Float == 0.0)
                        {
                            double o = i.Value.f_Float;
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

        public static int MathAtan(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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
        public static int MathAtanh(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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
                        if (i.Value.c_Float == 0.0)
                        {
                            double o = i.Value.f_Float;
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

        public static int MathAtan2(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            ExObject i2 = ExAPI.GetFromStack(vm, 3);
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
                                    long o = i2.GetInt();
                                    vm.Pop(nargs + 2);
                                    vm.Push((double)Math.Atan2(l, o));
                                    break;
                                }
                            case ExObjType.COMPLEX:
                                {
                                    if (i2.Value.c_Float == 0.0)
                                    {
                                        double o = i2.Value.f_Float;
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
                        if (i.Value.c_Float == 0.0)
                        {
                            b = i.Value.f_Float;
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
                        switch (i2.Type)
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
                                    if (i2.Value.c_Float == 0.0)
                                    {
                                        double o = i2.Value.f_Float;
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

        public static int MathLoge(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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

        public static int MathLog2(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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

        public static int MathLog10(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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

        public static int MathExp(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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

        public static int MathRound(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            int dec = 0;
            if (nargs == 2)
            {
                dec = (int)ExAPI.GetFromStack(vm, 3).GetInt();
            }

            switch (i.Type)
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

        public static int MathFloor(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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

        public static int MathCeil(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            switch (i.Type)
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

        public static int MathPow(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            ExObject i2 = ExAPI.GetFromStack(vm, 3);

            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        long b = i.GetInt();
                        switch (i2.Type)
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
                        switch (i2.Type)
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
                        switch (i2.Type)
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

        public static int MathMin(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            ExObject i2 = ExAPI.GetFromStack(vm, 3);

            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        long b = i.GetInt();
                        switch (i2.Type)
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
                                    if (i2.Value.c_Float == 0.0)
                                    {
                                        double o = i2.Value.f_Float;
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
                        if (i.Value.c_Float == 0.0)
                        {
                            double b = i.Value.f_Float;
                            switch (i2.Type)
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
                                        if (i2.Value.c_Float == 0.0)
                                        {
                                            double o = i2.Value.f_Float;
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
                        switch (i2.Type)
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
                                    if (i2.Value.c_Float == 0.0)
                                    {
                                        double o = i2.Value.f_Float;
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

        public static int MathMax(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            ExObject i2 = ExAPI.GetFromStack(vm, 3);

            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        long b = i.GetInt();
                        switch (i2.Type)
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
                                    if (i2.Value.c_Float == 0.0)
                                    {
                                        double o = i2.Value.f_Float;
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
                        if (i.Value.c_Float == 0.0)
                        {
                            double b = i.Value.f_Float;
                            switch (i2.Type)
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
                                        if (i2.Value.c_Float == 0.0)
                                        {
                                            double o = i2.Value.f_Float;
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
                        switch (i2.Type)
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
                                    if (i2.Value.c_Float == 0.0)
                                    {
                                        double o = i2.Value.f_Float;
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

        public static int MathSum(ExVM vm, int nargs)
        {
            ExObject sum = new(new Complex());
            ExObject[] args;
            if (nargs == 1 && ExAPI.GetFromStack(vm, 2).Type == ExObjType.ARRAY)
            {
                ExObject res = new();
                args = ExAPI.GetFromStack(vm, 2).GetList().ToArray();
                for (int i = 0; i < args.Length; i++)
                {
                    if (vm.DoArithmeticOP(OPs.OPC.ADD, args[i], sum, ref res))
                    {
                        sum.Value.f_Float = res.Value.f_Float;
                        sum.Value.c_Float = res.Value.c_Float;
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
                        sum.Value.f_Float = res.Value.f_Float;
                        sum.Value.c_Float = res.Value.c_Float;
                    }
                    else
                    {
                        return -1;
                    }
                }
                vm.Pop(nargs + 2);
            }

            if (sum.Value.c_Float == 0.0)
            {
                vm.Push(sum.Value.f_Float);
            }
            else
            {
                vm.Push(sum);
            }
            return 1;
        }

        public static int MathMul(ExVM vm, int nargs)
        {
            ExObject mul = new(1.0);
            ExObject[] args;

            if (nargs == 1 && ExAPI.GetFromStack(vm, 2).Type == ExObjType.ARRAY)
            {
                ExObject res = new();
                args = ExAPI.GetFromStack(vm, 2).GetList().ToArray();
                for (int i = 0; i < args.Length; i++)
                {
                    if (vm.DoArithmeticOP(OPs.OPC.MLT, args[i], mul, ref res))
                    {
                        mul.Value.f_Float = res.Value.f_Float;
                        mul.Value.c_Float = res.Value.c_Float;
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
                        mul.Value.f_Float = res.Value.f_Float;
                        mul.Value.c_Float = res.Value.c_Float;
                    }
                    else
                    {
                        return -1;
                    }
                }
                vm.Pop(nargs + 2);
            }

            if (mul.Value.c_Float == 0.0)
            {
                vm.Push(mul.Value.f_Float);
            }
            else
            {
                vm.Push(mul);
            }
            return 1;
        }

        public static int MathSign(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);

            switch (i.Type)
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
                        if (i.Value.c_Float == 0.0)
                        {
                            double o = i.Value.f_Float;
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

        public static int MathIsFIN(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            bool b = i.Type == ExObjType.COMPLEX ? Complex.IsFinite(i.GetComplex()) : double.IsFinite(i.GetFloat());

            vm.Pop(nargs + 2);
            vm.Push(b);
            return 1;
        }

        public static int MathIsINF(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            bool b = i.Type == ExObjType.COMPLEX ? Complex.IsInfinity(i.GetComplex()) : double.IsPositiveInfinity(i.GetFloat());

            vm.Pop(nargs + 2);
            vm.Push(b);
            return 1;
        }

        public static int MathIsNINF(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            bool b = i.Type == ExObjType.COMPLEX ? Complex.IsInfinity(i.GetComplex()) : double.IsNegativeInfinity(i.GetFloat());

            vm.Pop(nargs + 2);
            vm.Push(b);
            return 1;
        }

        public static int MathIsNAN(ExVM vm, int nargs)
        {
            ExObject i = ExAPI.GetFromStack(vm, 2);
            bool b = i.Type == ExObjType.COMPLEX ? Complex.IsNaN(i.GetComplex()) : double.IsNaN(i.GetFloat());

            vm.Pop(nargs + 2);
            vm.Push(b);
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
        public static int MathPlotSaveScatters(ExVM vm, int nargs)
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
                if (plot.Type != ExObjType.ARRAY)
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
                            if (plotdata[2].Type != ExObjType.STRING)
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
                            if (plotdata[2].Type != ExObjType.STRING)
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

                if (plotdata[0].Type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list for X axis");
                    return -1;
                }
                List<ExObject> x = plotdata[0].GetList();

                if (plotdata[1].Type != ExObjType.ARRAY)
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

        public static int MathPlotSaveScatter(ExVM vm, int nargs)
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

        public static int MathPlotSaveScatterLines(ExVM vm, int nargs)
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
                if (plot.Type != ExObjType.ARRAY)
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
                            if (plotdata[2].Type != ExObjType.STRING)
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
                            if (plotdata[2].Type != ExObjType.STRING)
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

                if (plotdata[0].Type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list for X axis");
                    return -1;
                }
                List<ExObject> x = plotdata[0].GetList();

                if (plotdata[1].Type != ExObjType.ARRAY)
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

        public static int MathPlotSaveScatterLine(ExVM vm, int nargs)
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

        public static int MathPlotSaveScatterPoints(ExVM vm, int nargs)
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
                if (plot.Type != ExObjType.ARRAY)
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
                            if (plotdata[2].Type != ExObjType.STRING)
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
                            if (plotdata[2].Type != ExObjType.STRING)
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

                if (plotdata[0].Type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list for X axis");
                    return -1;
                }
                List<ExObject> x = plotdata[0].GetList();

                if (plotdata[1].Type != ExObjType.ARRAY)
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

        public static int MathPlotSaveScatterPoint(ExVM vm, int nargs)
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

        public static int MathPlotSaveScatterSteps(ExVM vm, int nargs)
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
                if (plot.Type != ExObjType.ARRAY)
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
                            if (plotdata[2].Type != ExObjType.STRING)
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
                            if (plotdata[2].Type != ExObjType.STRING)
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

                if (plotdata[0].Type != ExObjType.ARRAY)
                {
                    vm.AddToErrorMessage("expected list for X axis");
                    return -1;
                }
                List<ExObject> x = plotdata[0].GetList();

                if (plotdata[1].Type != ExObjType.ARRAY)
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

        public static int MathPlotSaveScatterStep(ExVM vm, int nargs)
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

        public static int MathPlotSaveScatterComplex(ExVM vm, int nargs)
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
                if (v.Type != ExObjType.COMPLEX)
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
                    X[i] = v.Value.f_Float;
                    Y[i] = v.Value.c_Float;
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

        private static readonly List<ExRegFunc> _stdmathfuncs = new()
        {
            new()
            {
                Name = "srand",
                Function = MathSrand,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "rand",
                Function = MathRand,
                nParameterChecks = -1,
                ParameterMask = ".nn",
                DefaultValues = new()
                {
                    { 1, new(0) },
                    { 2, new(int.MaxValue) }
                }
            },
            new()
            {
                Name = "randf",
                Function = MathRandf,
                nParameterChecks = -1,
                ParameterMask = ".nn",
                DefaultValues = new()
                {
                    { 1, new(0) },
                    { 2, new(1) }
                }
            },

            new()
            {
                Name = "isDivisible",
                Function = MathIsDivisible,
                nParameterChecks = 3,
                ParameterMask = ".ii"
            },
            new()
            {
                Name = "divRem",
                Function = MathDivRem,
                nParameterChecks = 3,
                ParameterMask = ".ii"
            },
            new()
            {
                Name = "divQuot",
                Function = MathDivQuot,
                nParameterChecks = 3,
                ParameterMask = ".ii"
            },
            new()
            {
                Name = "divRemQuot",
                Function = MathDivRemQuot,
                nParameterChecks = 3,
                ParameterMask = ".ii"
            },
            new()
            {
                Name = "recip",
                Function = MathRecip,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "GCD",
                Function = MathGcd,
                nParameterChecks = 3,
                ParameterMask = ".i|fi|f"
            },
            new()
            {
                Name = "LCD",
                Function = MathLcd,
                nParameterChecks = 3,
                ParameterMask = ".i|fi|f"
            },
            new()
            {
                Name = "factorize",
                Function = MathPrimeFactors,
                nParameterChecks = 2,
                ParameterMask = ".i|f"
            },
            new()
            {
                Name = "next_prime",
                Function = MathNextPrime,
                nParameterChecks = 2,
                ParameterMask = ".i"
            },
            new()
            {
                Name = "prime",
                Function = MathPrime,
                nParameterChecks = 2,
                ParameterMask = ".i"
            },
            new()
            {
                Name = "isPrime",
                Function = MathIsPrime,
                nParameterChecks = 2,
                ParameterMask = ".i"
            },
            new()
            {
                Name = "areCoPrime",
                Function = MathAreCoprime,
                nParameterChecks = 3,
                ParameterMask = ".ii"
            },
            new()
            {
                Name = "digits",
                Function = MathDigits,
                nParameterChecks = 2,
                ParameterMask = ".i"
            },
            new()
            {
                Name = "abs",
                Function = MathAbs,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "sqrt",
                Function = MathSqrt,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "cbrt",
                Function = MathCbrt,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },

            new()
            {
                Name = "sin",
                Function = MathSin,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "cos",
                Function = MathCos,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "tan",
                Function = MathTan,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "sinh",
                Function = MathSinh,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "cosh",
                Function = MathCosh,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "tanh",
                Function = MathTanh,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },

            new()
            {
                Name = "asin",
                Function = MathAsin,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "acos",
                Function = MathAcos,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "atan",
                Function = MathAtan,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "atan2",
                Function = MathAtan2,
                nParameterChecks = 3,
                ParameterMask = ".nn"
            },
            new()
            {
                Name = "asinh",
                Function = MathAsinh,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "acosh",
                Function = MathAcosh,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "atanh",
                Function = MathAtanh,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },

            new()
            {
                Name = "loge",
                Function = MathLoge,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "log2",
                Function = MathLog2,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "log10",
                Function = MathLog10,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "exp",
                Function = MathExp,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "round",
                Function = MathRound,
                nParameterChecks = -2,
                ParameterMask = ".nn",
                DefaultValues = new()
                {
                    { 2, new(0) }
                }
            },
            new()
            {
                Name = "floor",
                Function = MathFloor,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "ceil",
                Function = MathCeil,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "pow",
                Function = MathPow,
                nParameterChecks = 3,
                ParameterMask = ".nn"
            },

            new()
            {
                Name = "sum",
                Function = MathSum,
                nParameterChecks = -1,
                ParameterMask = null
            },
            new()
            {
                Name = "mul",
                Function = MathMul,
                nParameterChecks = -1,
                ParameterMask = null
            },

            new()
            {
                Name = "min",
                Function = MathMin,
                nParameterChecks = 3,
                ParameterMask = ".nn"
            },
            new()
            {
                Name = "max",
                Function = MathMax,
                nParameterChecks = 3,
                ParameterMask = ".nn"
            },
            new()
            {
                Name = "sign",
                Function = MathSign,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },

            new()
            {
                Name = "isFIN",
                Function = MathIsFIN,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "isINF",
                Function = MathIsINF,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "isNINF",
                Function = MathIsNINF,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "isNAN",
                Function = MathIsNAN,
                nParameterChecks = 2,
                ParameterMask = ".n"
            },
            new()
            {
                Name = "save_scatter",
                Function = MathPlotSaveScatter,
                nParameterChecks = -4,
                ParameterMask = ".saannss",
                DefaultValues = new()
                {
                    { 4, new(1200) },
                    { 5, new(800) },
                    { 6, new("blue") },
                    { 7, new(s: null) }
                }
            },
            new()
            {
                Name = "save_scatters",
                Function = MathPlotSaveScatters,
                nParameterChecks = -3,
                ParameterMask = ".sann",
                DefaultValues = new()
                {
                    { 3, new(1200) },
                    { 4, new(800) }
                }
            },
            new()
            {
                Name = "save_scatter_step",
                Function = MathPlotSaveScatterStep,
                nParameterChecks = -4,
                ParameterMask = ".saannss",
                DefaultValues = new()
                {
                    { 4, new(1200) },
                    { 5, new(800) },
                    { 6, new("blue") },
                    { 7, new(s: null) }
                }
            },
            new()
            {
                Name = "save_scatter_steps",
                Function = MathPlotSaveScatterSteps,
                nParameterChecks = -3,
                ParameterMask = ".sann",
                DefaultValues = new()
                {
                    { 3, new(1200) },
                    { 4, new(800) }
                }
            },
            new()
            {
                Name = "save_scatter_point",
                Function = MathPlotSaveScatterPoint,
                nParameterChecks = -4,
                ParameterMask = ".saannss",
                DefaultValues = new()
                {
                    { 4, new(1200) },
                    { 5, new(800) },
                    { 6, new("blue") },
                    { 7, new(s: null) }
                }
            },
            new()
            {
                Name = "save_scatter_points",
                Function = MathPlotSaveScatterPoints,
                nParameterChecks = -3,
                ParameterMask = ".sann",
                DefaultValues = new()
                {
                    { 3, new(1200) },
                    { 4, new(800) }
                }
            },
            new()
            {
                Name = "save_scatter_line",
                Function = MathPlotSaveScatterLine,
                nParameterChecks = -4,
                ParameterMask = ".saannss",
                DefaultValues = new()
                {
                    { 4, new(1200) },
                    { 5, new(800) },
                    { 6, new("blue") },
                    { 7, new(s: null) }
                }
            },
            new()
            {
                Name = "save_scatter_lines",
                Function = MathPlotSaveScatterLines,
                nParameterChecks = -3,
                ParameterMask = ".sann",
                DefaultValues = new()
                {
                    { 3, new(1200) },
                    { 4, new(800) }
                }
            },
            new()
            {
                Name = "save_complex",
                Function = MathPlotSaveScatterComplex,
                nParameterChecks = -3,
                ParameterMask = ".sannss",
                DefaultValues = new()
                {
                    { 3, new(1200) },
                    { 4, new(800) },
                    { 5, new("blue") },
                    { 6, new(s: null) }
                }
            }
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
