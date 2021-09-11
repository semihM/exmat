using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using ExMat.API;
using ExMat.Objects;
using ExMat.Utils;
using ExMat.VM;

namespace ExMat.StdLib
{
    [ExStdLibBase(ExStdLibType.MATH)]
    [ExStdLibName("math")]
    [ExStdLibRegister(nameof(Registery))]
    [ExStdLibConstDict(nameof(Constants))]
    public static class ExStdMath
    {
        #region UTILITY
        private static List<int> Primes;
        private static readonly int PrimeSearchSize = int.MaxValue / 100;
        private static readonly int PrimeCacheMaxSize = 1358124 * 2;
        private static int PrimeCount = 1358124;
        private static int PrimeMax = 21474829;

        public static Random Rand { get; set; } = new();

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

        private static double[] CreateNumArr(ExVM vm, List<ExObject> l)
        {
            double[] a = new double[l.Count];
            for (int i = 0; i < l.Count; i++)
            {
                if (!ExTypeCheck.IsNumeric(l[i]))
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

        #endregion

        #region MATH FUNCTIONS
        [ExNativeFuncBase("srand", "Set the seed used for random number generators")]
        [ExNativeParamBase(1, "seed", "n", "Seed to use")]
        public static ExFunctionStatus MathSrand(ExVM vm, int nargs)
        {
            Rand = new((int)vm.GetArgument(1).GetInt());
            return ExFunctionStatus.VOID;
        }

        [ExNativeFuncBase("rand", ExBaseType.INTEGER, "")]
        [ExNativeParamBase(1, "bound1", "n", "If used alone: [0,bound1), otherwise: [bound1, bound2)", 0)]
        [ExNativeParamBase(2, "bound2", "n", "Upper bound for number range", int.MaxValue)]
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

        [ExNativeFuncBase("randf", ExBaseType.FLOAT, "Get a random float in given range: [0, 1) , [0, bound1) or [bound1, bound2)")]
        [ExNativeParamBase(1, "bound1", "n", "If used alone: [0,bound1), otherwise: [bound1, bound2)", 0.0)]
        [ExNativeParamBase(2, "bound2", "n", "Upper bound for number range", 1.0)]
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

        [ExNativeFuncBase("next_prime", ExBaseType.INTEGER, "Get the next closest prime bigger than the given value")]
        [ExNativeParamBase(1, "start", "i", "Starting value to get next closest prime of")]
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

        [ExNativeFuncBase("isPrime", ExBaseType.BOOL, "Check wheter the given number is a prime number.")]
        [ExNativeParamBase(1, "value", "i", "Value to check")]
        public static ExFunctionStatus MathIsPrime(ExVM vm, int nargs)
        {
            long a = vm.GetArgument(1).GetInt();

            return vm.CleanReturn(nargs + 2, (Primes != null && a > 0 && a <= PrimeMax && Primes.BinarySearch(item: (int)a) >= 0) || IsPrime(a));
        }

        [ExNativeFuncBase("areCoPrime", ExBaseType.BOOL, "Check if given 2 values are coprimes.")]
        [ExNativeParamBase(1, "num1", "r", "Value 1")]
        [ExNativeParamBase(2, "num2", "r", "Value 2")]
        public static ExFunctionStatus MathAreCoprime(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, GetGCD(vm.GetArgument(1).GetInt(), vm.GetArgument(2).GetInt()) == 1.0);
        }

        [ExNativeFuncBase("prime", ExBaseType.INTEGER, "Get the n'th prime number")]
        [ExNativeParamBase(1, "n", "i", "Index of the prime, that is n'th prime.")]
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

        [ExNativeFuncBase("factorize", ExBaseType.ARRAY, "Get the prime factorization of a positive number. An empty list is returned for negative values.")]
        [ExNativeParamBase(1, "positive_num", "r", "A positive value to factorize")]
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

        [ExNativeFuncBase("GCD", ExBaseType.FLOAT, "Get the greatest common divisor(GCD) of 2 numbers")]
        [ExNativeParamBase(1, "num1", "r", "Value 1")]
        [ExNativeParamBase(2, "num2", "r", "Value 2")]
        public static ExFunctionStatus MathGcd(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, GetGCD(vm.GetArgument(1).GetFloat(), vm.GetArgument(2).GetFloat()));
        }

        [ExNativeFuncBase("LCD", ExBaseType.FLOAT, "Get the least common denominator(LCD) of 2 numbers")]
        [ExNativeParamBase(1, "num1", "r", "Value 1")]
        [ExNativeParamBase(2, "num2", "r", "Value 2")]
        public static ExFunctionStatus MathLcd(ExVM vm, int nargs)
        {
            double a = vm.GetArgument(1).GetFloat();
            double b = vm.GetArgument(2).GetFloat();
            return vm.CleanReturn(nargs + 2, Math.Abs(a * b) / GetGCD(a, b));
        }

        [ExNativeFuncBase("isDivisible", ExBaseType.BOOL, "Check divisibility of (numerator / denominator)")]
        [ExNativeParamBase(1, "numerator", "i", "Numerator")]
        [ExNativeParamBase(2, "denominator", "i", "Denominator")]
        public static ExFunctionStatus MathIsDivisible(ExVM vm, int nargs)
        {
            Math.DivRem(vm.GetArgument(1).GetInt(), vm.GetArgument(2).GetInt(), out long r);
            return vm.CleanReturn(nargs + 2, r == 0);
        }

        [ExNativeFuncBase("divQuot", ExBaseType.INTEGER, "Get the quotient from (numerator / denominator)")]
        [ExNativeParamBase(1, "numerator", "i", "Numerator")]
        [ExNativeParamBase(2, "denominator", "i", "Denominator")]
        public static ExFunctionStatus MathDivQuot(ExVM vm, int nargs)
        {
            return vm.CleanReturn(nargs + 2, Math.DivRem(vm.GetArgument(1).GetInt(), vm.GetArgument(2).GetInt(), out long _));
        }

        [ExNativeFuncBase("divRem", ExBaseType.INTEGER, "Get the remainder from (numerator / denominator)")]
        [ExNativeParamBase(1, "numerator", "i", "Numerator")]
        [ExNativeParamBase(2, "denominator", "i", "Denominator")]
        public static ExFunctionStatus MathDivRem(ExVM vm, int nargs)
        {
            Math.DivRem(vm.GetArgument(1).GetInt(), vm.GetArgument(2).GetInt(), out long rem);
            return vm.CleanReturn(nargs + 2, rem);
        }

        [ExNativeFuncBase("divRemQuot", ExBaseType.ARRAY, "Get the remainder and the quotient from (numerator / denominator) in a list.")]
        [ExNativeParamBase(1, "numerator", "i", "Numerator")]
        [ExNativeParamBase(2, "denominator", "i", "Denominator")]
        public static ExFunctionStatus MathDivRemQuot(ExVM vm, int nargs)
        {
            long quot = Math.DivRem(vm.GetArgument(1).GetInt(), vm.GetArgument(2).GetInt(), out long rem);
            return vm.CleanReturn(nargs + 2, new List<ExObject>(2) { new(rem), new(quot) });
        }

        [ExNativeFuncBase("recip", ExBaseType.FLOAT, "Get the reciprocal of a value, that is 1/value.")]
        [ExNativeParamBase(1, "value", "i", "Value to get 1/value of")]
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

        [ExNativeFuncBase("digits", ExBaseType.ARRAY, "Get the digits of an integer value in a list.")]
        [ExNativeParamBase(1, "value", "i", "Value to get digits of")]
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

        [ExNativeFuncBase("abs", ExBaseType.FLOAT | ExBaseType.INTEGER, "Get the absolute value of a number or the magnitute of a complex number.")]
        [ExNativeParamBase(1, "value", "n", "Value to use")]
        public static ExFunctionStatus MathAbs(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            switch (i.Type)
            {
                case ExObjType.INTEGER:
                    {
                        long o = i.GetInt();
                        return o < 0 ? vm.CleanReturn(nargs + 2, o > long.MinValue ? Math.Abs(o) : 0) : vm.CleanReturn(nargs + 2, Math.Abs(o));
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

        [ExNativeFuncBase("sqrt", ExBaseType.FLOAT | ExBaseType.INTEGER | ExBaseType.COMPLEX, "Get the square root of a number.")]
        [ExNativeParamBase(1, "value", "n", "Value to use")]
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

        [ExNativeFuncBase("cbrt", ExBaseType.FLOAT | ExBaseType.INTEGER | ExBaseType.COMPLEX, "Get the cube root of a number.")]
        [ExNativeParamBase(1, "value", "n", "Value to use")]
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

        [ExNativeFuncBase("sin", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the sin of a number. Uses radians.")]
        [ExNativeParamBase(1, "value", "n", "Radians to use")]
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

        [ExNativeFuncBase("sinh", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the sinh of a number. Uses radians.")]
        [ExNativeParamBase(1, "value", "n", "Radians to use")]
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

        [ExNativeFuncBase("cos", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the cos of a number. Uses radians.")]
        [ExNativeParamBase(1, "value", "n", "Radians to use")]
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

        [ExNativeFuncBase("cosh", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the cosh of a number. Uses radians.")]
        [ExNativeParamBase(1, "value", "n", "Radians to use")]
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

        [ExNativeFuncBase("tan", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the tan of a number. Uses radians.")]
        [ExNativeParamBase(1, "value", "n", "Radians to use")]
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

        [ExNativeFuncBase("tanh", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the tanh of a number. Uses radians.")]
        [ExNativeParamBase(1, "value", "n", "Radians to use")]
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

        [ExNativeFuncBase("acos", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the arccos of a number.")]
        [ExNativeParamBase(1, "value", "n", "Value to use")]
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

        [ExNativeFuncBase("acosh", ExBaseType.FLOAT, "Get the arccosh of a number.")]
        [ExNativeParamBase(1, "value", "n", "Value to use")]
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
                        return i.Value.c_Float == 0.0
                            ? vm.CleanReturn(nargs + 2, Math.Acosh(i.Value.f_Float))
                            : vm.AddToErrorMessage("can't use complex numbers with acosh");
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Acosh(i.GetFloat()));
                    }
            }
        }

        [ExNativeFuncBase("asin", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the arcsin of a number.")]
        [ExNativeParamBase(1, "value", "n", "Value to use")]
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

        [ExNativeFuncBase("asinh", ExBaseType.FLOAT, "Get the arcsinh of a number.")]
        [ExNativeParamBase(1, "value", "n", "Value to use")]
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
                        return i.Value.c_Float == 0.0
                            ? vm.CleanReturn(nargs + 2, Math.Asinh(i.Value.f_Float))
                            : vm.AddToErrorMessage("can't use complex numbers with asinh");
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Asinh(i.GetFloat()));
                    }
            }
        }

        [ExNativeFuncBase("atan", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the arctan of a number.")]
        [ExNativeParamBase(1, "value", "n", "Value to use")]
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

        [ExNativeFuncBase("atanh", ExBaseType.FLOAT, "Get the arctanh of a number.")]
        [ExNativeParamBase(1, "value", "n", "Value to use")]
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
                        return i.Value.c_Float == 0.0
                            ? vm.CleanReturn(nargs + 2, Math.Atanh(i.Value.f_Float))
                            : vm.AddToErrorMessage("can't use complex numbers with atanh");
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Atanh(i.GetFloat()));
                    }
            }
        }

        [ExNativeFuncBase("atan2", ExBaseType.FLOAT, "Return the angle whose tangent is the quotient of two specified numbers. An angle, θ, measured in radians, such that -π ≤ θ ≤ π, and tan(θ) = y / x, where (x, y) is a point in the Cartesian plane.")]
        [ExNativeParamBase(1, "y", "n", "Cartesian plane x coordinate")]
        [ExNativeParamBase(2, "x", "n", "Cartesian plane y coordinate")]
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
                                    return i2.Value.c_Float == 0.0
                                        ? vm.CleanReturn(nargs + 2, Math.Atan2(l, i2.Value.f_Float))
                                        : vm.AddToErrorMessage("can't use complex numbers with atan2");
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
                                    return i2.Value.c_Float == 0.0
                                        ? vm.CleanReturn(nargs + 2, Math.Atan2(b, i2.Value.f_Float))
                                        : vm.AddToErrorMessage("can't use complex numbers with atan2");
                                }
                            default:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Atan2(b, i2.GetFloat()));
                                }
                        }
                    }
            }
        }

        [ExNativeFuncBase("loge", ExBaseType.FLOAT | ExBaseType.COMPLEX, "")]
        [ExNativeParamBase(1, "value", "n", "Value to use")]
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

        [ExNativeFuncBase("log2", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the base 2 logarithm of a number.")]
        [ExNativeParamBase(1, "value", "n", "Value to use")]
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

        [ExNativeFuncBase("log10", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the base 10 logarithm of a number.")]
        [ExNativeParamBase(1, "value", "n", "Value to use")]
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

        [ExNativeFuncBase("log", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the base 'b' logarithm of 'a', that is log'b'('a') == log(a,b). If 'b' is not given, works same as 'loge' function, that is loge('a') == log(a) == log(a,E) == ln(a)")]
        [ExNativeParamBase(1, "a", "n", "Argument")]
        [ExNativeParamBase(2, "b", "n", "Base", Math.E)]
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

        [ExNativeFuncBase("exp", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the natural exponential function's value at 'x', that is E raised to the power of 'x'")]
        [ExNativeParamBase(1, "x", "n", "Value to raise E to")]
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

        [ExNativeFuncBase("round", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Round a number to given digits")]
        [ExNativeParamBase(1, "value", "n", "Value to round")]
        [ExNativeParamBase(2, "digits", "n", "Digits to round to", 0)]
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

        [ExNativeFuncBase("floor", ExBaseType.INTEGER | ExBaseType.COMPLEX, "Round a number to closest integer which is lower than the value.")]
        [ExNativeParamBase(1, "value", "n", "Value to round")]
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

        [ExNativeFuncBase("ceil", ExBaseType.INTEGER | ExBaseType.COMPLEX, "Round a number to closest integer which is higher than the value.")]
        [ExNativeParamBase(1, "value", "n", "Value to round")]
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

        [ExNativeFuncBase("pow", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Raise a number to the given power")]
        [ExNativeParamBase(1, "value", "n", "Value to raise")]
        [ExNativeParamBase(2, "power", "n", "Power to raise to")]
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

        [ExNativeFuncBase("min", ExBaseType.INTEGER | ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the minimum of two given values")]
        [ExNativeParamBase(1, "value1", "n", "Value 1")]
        [ExNativeParamBase(2, "value2", "n", "Value 2")]
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
                                    return i2.Value.c_Float == 0.0
                                        ? vm.CleanReturn(nargs + 2, Math.Min(b, i2.Value.f_Float))
                                        : vm.AddToErrorMessage("can't compare complex numbers");
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
                                        return i2.Value.c_Float == 0.0
                                            ? vm.CleanReturn(nargs + 2, Math.Min(b, i2.Value.f_Float))
                                            : vm.AddToErrorMessage("can't compare complex numbers");
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
                                    return i2.Value.c_Float == 0.0
                                        ? vm.CleanReturn(nargs + 2, Math.Min(b, i2.Value.f_Float))
                                        : vm.AddToErrorMessage("can't compare complex numbers");
                                }
                            default:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Min(b, i2.GetFloat()));
                                }
                        }
                    }
            }
        }

        [ExNativeFuncBase("max", ExBaseType.INTEGER | ExBaseType.FLOAT | ExBaseType.COMPLEX, "Get the maximum of two given values")]
        [ExNativeParamBase(1, "value1", "n", "Value 1")]
        [ExNativeParamBase(2, "value2", "n", "Value 2")]
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
                                    return i2.Value.c_Float == 0.0
                                        ? vm.CleanReturn(nargs + 2, Math.Max(b, i2.Value.f_Float))
                                        : vm.AddToErrorMessage("can't compare complex numbers");
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
                                        return i2.Value.c_Float == 0.0
                                            ? vm.CleanReturn(nargs + 2, Math.Max(b, i2.Value.f_Float))
                                            : vm.AddToErrorMessage("can't compare complex numbers");
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
                                    return i2.Value.c_Float == 0.0
                                        ? vm.CleanReturn(nargs + 2, Math.Max(b, i2.Value.f_Float))
                                        : vm.AddToErrorMessage("can't compare complex numbers");
                                }
                            default:
                                {
                                    return vm.CleanReturn(nargs + 2, Math.Max(b, i2.GetFloat()));
                                }
                        }
                    }
            }
        }

        [ExNativeFuncBase("sum", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Return the sum of given arguments or the sum of items of the given list.", -1)]
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

        [ExNativeFuncBase("mul", ExBaseType.FLOAT | ExBaseType.COMPLEX, "Return the product of given arguments or the product of items of the given list.", -1)]
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

        [ExNativeFuncBase("sign", ExBaseType.INTEGER, "Get the sign of the given value. Returns -1: Negative, 0: Zero, 1: Positive")]
        [ExNativeParamBase(1, "value", "n", "Value to get the sign of")]
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
                        return i.Value.c_Float == 0.0
                            ? vm.CleanReturn(nargs + 2, Math.Sign(i.Value.f_Float))
                            : vm.AddToErrorMessage("can't get complex number's sign");
                    }
                default:
                    {
                        return vm.CleanReturn(nargs + 2, Math.Sign(i.GetFloat()));
                    }
            }
        }

        [ExNativeFuncBase("isFIN", ExBaseType.BOOL, "Check if given value is finite")]
        [ExNativeParamBase(1, "value", "n", "Value to check")]
        public static ExFunctionStatus MathIsFIN(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            return vm.CleanReturn(nargs + 2, i.Type == ExObjType.COMPLEX ? Complex.IsFinite(i.GetComplex()) : double.IsFinite(i.GetFloat()));
        }

        [ExNativeFuncBase("isINF", ExBaseType.BOOL, "Check if given value is positive infinity (INF)")]
        [ExNativeParamBase(1, "value", "n", "Value to check")]
        public static ExFunctionStatus MathIsINF(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            return vm.CleanReturn(nargs + 2, i.Type == ExObjType.COMPLEX ? Complex.IsInfinity(i.GetComplex()) : double.IsPositiveInfinity(i.GetFloat()));
        }

        [ExNativeFuncBase("isNINF", ExBaseType.BOOL, "Check if given value is negative infinity (NINF)")]
        [ExNativeParamBase(1, "value", "n", "Value to check")]
        public static ExFunctionStatus MathIsNINF(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            return vm.CleanReturn(nargs + 2, i.Type == ExObjType.COMPLEX ? Complex.IsInfinity(i.GetComplex()) : double.IsNegativeInfinity(i.GetFloat()));
        }

        [ExNativeFuncBase("isNAN", ExBaseType.BOOL, "Check if given value is NaN (NAN)")]
        [ExNativeParamBase(1, "value", "n", "Value to check")]
        public static ExFunctionStatus MathIsNAN(ExVM vm, int nargs)
        {
            ExObject i = vm.GetArgument(1);
            return vm.CleanReturn(nargs + 2, i.Type == ExObjType.COMPLEX ? Complex.IsNaN(i.GetComplex()) : double.IsNaN(i.GetFloat()));
        }

        // TO-DO extremely redundant, refactor...
        [ExNativeFuncBase("save_scatters", ExBaseType.STRING, "Save multiple plots of data points and lines connecting them in a single plot, using given data lists and plot information as an image")]
        [ExNativeParamBase(1, "filename", "s", "Image file name to save as")]
        [ExNativeParamBase(2, "data_list", "a", "List of plot information lists. Each list must follow the format:\n\t [<list> x_axis, <list> y_axis, {OPTIONAL<string> color = \"blue\"},  {OPTIONAL<string?> label = null}]")]
        [ExNativeParamBase(3, "width", "n", "Width of the image", 1200)]
        [ExNativeParamBase(4, "height", "n", "Height of the image", 800)]
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

        [ExNativeFuncBase("save_scatter", ExBaseType.STRING, "Save a plot of data points and lines connecting them, using given data lists and plot information as an image")]
        [ExNativeParamBase(1, "filename", "s", "Image file name to save as")]
        [ExNativeParamBase(2, "x", "a", "X axis data")]
        [ExNativeParamBase(3, "y", "a", "Y axis data")]
        [ExNativeParamBase(4, "width", "n", "Width of the image", 1200)]
        [ExNativeParamBase(5, "height", "n", "Height of the image", 800)]
        [ExNativeParamBase(6, "color", "s", "Plot data point color name", "blue")]
        [ExNativeParamBase(7, "plot_label", "s", "Plot label, null to not use any labels", def: null)]
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
                        color = System.Drawing.Color.FromName(vm.GetArgument(6).GetString().ToLower(CultureInfo.CurrentCulture));
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

        [ExNativeFuncBase("save_scatter_lines", ExBaseType.STRING, "Save multiple plots of line plots in a single plot, using given data lists and plot information as an image")]
        [ExNativeParamBase(1, "filename", "s", "Image file name to save as")]
        [ExNativeParamBase(2, "data_list", "a", "List of plot information lists. Each list must follow the format:\n\t [<list> x_axis, <list> y_axis, {OPTIONAL<string> color = \"blue\"},  {OPTIONAL<string?> label = null}]")]
        [ExNativeParamBase(3, "width", "n", "Width of the image", 1200)]
        [ExNativeParamBase(4, "height", "n", "Height of the image", 800)]
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

        [ExNativeFuncBase("save_scatter_line", ExBaseType.STRING, "Save a plot of line plot connecting data points, using given data lists and plot information as an image")]
        [ExNativeParamBase(1, "filename", "s", "Image file name to save as")]
        [ExNativeParamBase(2, "x", "a", "X axis data")]
        [ExNativeParamBase(3, "y", "a", "Y axis data")]
        [ExNativeParamBase(4, "width", "n", "Width of the image", 1200)]
        [ExNativeParamBase(5, "height", "n", "Height of the image", 800)]
        [ExNativeParamBase(6, "color", "s", "Plot data point color name", "blue")]
        [ExNativeParamBase(7, "plot_label", "s", "Plot label, null to not use any labels", def: null)]
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
                        color = System.Drawing.Color.FromName(vm.GetArgument(6).GetString().ToLower(CultureInfo.CurrentCulture));
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

        [ExNativeFuncBase("save_scatter_points", ExBaseType.STRING, "Save multiple scatter plots in a single plot, using given data lists and plot information as an image")]
        [ExNativeParamBase(1, "filename", "s", "Image file name to save as")]
        [ExNativeParamBase(2, "data_list", "a", "List of plot information lists. Each list must follow the format:\n\t [<list> x_axis, <list> y_axis, {OPTIONAL<string> color = \"blue\"},  {OPTIONAL<string?> label = null}]")]
        [ExNativeParamBase(3, "width", "n", "Width of the image", 1200)]
        [ExNativeParamBase(4, "height", "n", "Height of the image", 800)]
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

        [ExNativeFuncBase("save_scatter_point", ExBaseType.STRING, "Save a scatter plot with data points only, using given data lists and plot information as an image")]
        [ExNativeParamBase(1, "filename", "s", "Image file name to save as")]
        [ExNativeParamBase(2, "x", "a", "X axis data")]
        [ExNativeParamBase(3, "y", "a", "Y axis data")]
        [ExNativeParamBase(4, "width", "n", "Width of the image", 1200)]
        [ExNativeParamBase(5, "height", "n", "Height of the image", 800)]
        [ExNativeParamBase(6, "color", "s", "Plot data point color name", "blue")]
        [ExNativeParamBase(7, "plot_label", "s", "Plot label, null to not use any labels", def: null)]
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
                        color = System.Drawing.Color.FromName(vm.GetArgument(6).GetString().ToLower(CultureInfo.CurrentCulture));
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

        [ExNativeFuncBase("save_scatter_steps", ExBaseType.STRING, "Save multiple step plots in a single plot, using given data lists and plot information as an image")]
        [ExNativeParamBase(1, "filename", "s", "Image file name to save as")]
        [ExNativeParamBase(2, "data_list", "a", "List of plot information lists. Each list must follow the format:\n\t [<list> x_axis, <list> y_axis, {OPTIONAL<string> color = \"blue\"},  {OPTIONAL<string?> label = null}]")]
        [ExNativeParamBase(3, "width", "n", "Width of the image", 1200)]
        [ExNativeParamBase(4, "height", "n", "Height of the image", 800)]
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

        [ExNativeFuncBase("save_scatter_step", ExBaseType.STRING, "Save a step plot using given data lists and plot information as an image")]
        [ExNativeParamBase(1, "filename", "s", "Image file name to save as")]
        [ExNativeParamBase(2, "x", "a", "X axis data")]
        [ExNativeParamBase(3, "y", "a", "Y axis data")]
        [ExNativeParamBase(4, "width", "n", "Width of the image", 1200)]
        [ExNativeParamBase(5, "height", "n", "Height of the image", 800)]
        [ExNativeParamBase(6, "color", "s", "Plot data point color name", "blue")]
        [ExNativeParamBase(7, "plot_label", "s", "Plot label, null to not use any labels", def: null)]
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
                        color = System.Drawing.Color.FromName(vm.GetArgument(6).GetString().ToLower(CultureInfo.CurrentCulture));
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

        [ExNativeFuncBase("save_complex", ExBaseType.STRING, "Save a scatter plot of complex numbers, using given plot information as an image")]
        [ExNativeParamBase(1, "filename", "s", "Image file name to save as")]
        [ExNativeParamBase(2, "complex_nums", "a", "Complex number list to plot")]
        [ExNativeParamBase(3, "width", "n", "Width of the image", 1200)]
        [ExNativeParamBase(4, "height", "n", "Height of the image", 800)]
        [ExNativeParamBase(5, "color", "s", "Plot data point color name", "blue")]
        [ExNativeParamBase(6, "plot_label", "s", "Plot label, null to not use any labels", def: null)]
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
                    if (!ExTypeCheck.IsNumeric(v))
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
                        color = System.Drawing.Color.FromName(vm.GetArgument(5).GetString().ToLower(CultureInfo.CurrentCulture));
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

        #endregion

        // MAIN
        public static Dictionary<string, ExObject> Constants => new()
        {
            { "INT8_MAX", new(sbyte.MaxValue) },
            { "INT8_MIN", new(sbyte.MinValue) },

            { "INT16_MAX", new(short.MaxValue) },
            { "INT16_MIN", new(short.MinValue) },

            { "INT32_MAX", new(int.MaxValue) },
            { "INT32_MIN", new(int.MinValue) },

            { "INT64_MAX", new(long.MaxValue) },
            { "INT64_MIN", new(long.MinValue) },

            { "UINT8_MAX", new(byte.MaxValue) },
            { "UINT16_MAX", new(ushort.MaxValue) },
            { "UINT32_MAX", new(uint.MaxValue) },

            { "FLOAT32_MAX", new(float.MaxValue) },
            { "FLOAT32_MIN", new(float.MinValue) },

            { "FLOAT64_MAX", new(double.MaxValue) },
            { "FLOAT64_MIN", new(double.MinValue) },

            { "TAU", new(Math.Tau) },
            { "PI", new(Math.PI) },
            { "E", new(Math.E) },
            { "GOLDEN", new((1.0 + Math.Sqrt(5.0)) / 2.0) },
            { "DEGREE", new(Math.PI / 180.0) },
            { "EPSILON", new(double.Epsilon) },

            { "NAN", new(double.NaN) },
            { "NINF", new(double.NegativeInfinity) },
            { "INF", new(double.PositiveInfinity) },

            {
                "SPACES",
                new(new Dictionary<string, ExObject>()
                {
                    { "R", new(ExSpace.Create("R", '\\', 1) )},
                    { "R2", new(ExSpace.Create("R", '\\', 2) )},
                    { "R3", new(ExSpace.Create("R", '\\', 3) )},
                    { "Rn", new(ExSpace.Create("R", '\\', -1) )},
                    { "Rmn", new(ExSpace.Create("R", '\\', -1, -1) )},

                    { "Z", new(ExSpace.Create("Z", '\\', 1) )},
                    { "Z2", new(ExSpace.Create("Z", '\\', 2) )},
                    { "Z3", new(ExSpace.Create("Z", '\\', 3) )},
                    { "Zn", new(ExSpace.Create("Z", '\\', -1) )},
                    { "Zmn", new(ExSpace.Create("Z", '\\', -1, -1) )},

                    { "N", new(ExSpace.Create("N", '\\', 1) )},
                    { "N2", new(ExSpace.Create("N", '\\', 2) )},
                    { "N3", new(ExSpace.Create("N", '\\', 3) )},
                    { "Nn", new(ExSpace.Create("N", '\\', -1) )},
                    { "Nmn", new(ExSpace.Create("N", '\\', -1, -1) )},

                    { "C", new(ExSpace.Create("C", '\\', 1) )},
                    { "C2", new(ExSpace.Create("C", '\\', 2) )},
                    { "C3", new(ExSpace.Create("C", '\\', 3) )},
                    { "Cn", new(ExSpace.Create("C", '\\', -1) )},
                    { "Cmn", new(ExSpace.Create("C", '\\', -1, -1) )},

                    { "A", new(ExSpace.Create("A", '\\', 1) )},
                    { "A2", new(ExSpace.Create("A", '\\', 2) )},
                    { "A3", new(ExSpace.Create("A", '\\', 3) )},
                    { "An", new(ExSpace.Create("A", '\\', -1) )},
                    { "Amn", new(ExSpace.Create("A", '\\', -1, -1) )}
                })
            }
        };

        public static ExMat.StdLibRegistery Registery => (ExVM vm) =>
        {
            return true;
        };

    }
}
