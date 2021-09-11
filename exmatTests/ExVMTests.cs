using System;
using System.Collections.Generic;
using System.Numerics;
using ExMat.Objects;
using ExMat.OPs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExMat.VM.Tests
{
    [TestClass()]
    public class ExVMTests
    {
        public ExVMTests() { }

        #region InnerDoArithmeticOPComplex
        [TestMethod()]
        public void InnerDoArithmeticOPComplexTestAdd()
        {
            Complex a = new(2, 6);
            Complex b = new(1, -2);

            Complex res = Complex.Add(a, b);

            ExObject tmp = null;

            Assert.IsTrue(ExVM.InnerDoArithmeticOPComplex(ExOperationCode.ADD, a, b, ref tmp));

            Assert.IsNotNull(tmp);

            Assert.AreEqual(res, tmp.GetComplex());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPComplexTestSub()
        {
            Complex a = new(2, 6);
            Complex b = new(1, -2);

            Complex res = Complex.Subtract(a, b);

            ExObject tmp = null;

            Assert.IsTrue(ExVM.InnerDoArithmeticOPComplex(ExOperationCode.SUB, a, b, ref tmp));

            Assert.IsNotNull(tmp);

            Assert.AreEqual(res, tmp.GetComplex());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPComplexTestMlt()
        {
            Complex a = new(2, 6);
            Complex b = new(1, -2);

            Complex res = Complex.Multiply(a, b);

            ExObject tmp = null;

            Assert.IsTrue(ExVM.InnerDoArithmeticOPComplex(ExOperationCode.MLT, a, b, ref tmp));

            Assert.IsNotNull(tmp);

            Assert.AreEqual(res, tmp.GetComplex());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPComplexTestDiv()
        {
            Complex a = new(2, 6);
            Complex b = new(1, -2);

            Complex res = Complex.Divide(a, b);

            ExObject tmp = null;

            Assert.IsTrue(ExVM.InnerDoArithmeticOPComplex(ExOperationCode.DIV, a, b, ref tmp));

            Assert.IsNotNull(tmp);

            Assert.AreEqual(res, tmp.GetComplex());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPComplexTestExp()
        {
            Complex a = new(2, 6);
            Complex b = new(1, -2);

            Complex res = Complex.Pow(a, b);

            ExObject tmp = null;

            Assert.IsTrue(ExVM.InnerDoArithmeticOPComplex(ExOperationCode.EXP, a, b, ref tmp));

            Assert.IsNotNull(tmp);

            Assert.AreEqual(res, tmp.GetComplex());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPComplexTestMod()
        {
            ExObject tmp = null;

            Assert.IsFalse(ExVM.InnerDoArithmeticOPComplex(ExOperationCode.MOD, new Complex(), new Complex(), ref tmp));

            Assert.IsNull(tmp);
        }

        [TestMethod()]
        public void InnerDoArithmeticOPComplexTestUnknown()
        {
            ExObject tmp = null;

            Assert.IsFalse(ExVM.InnerDoArithmeticOPComplex(ExOperationCode.CLOSE, new Complex(), new Complex(), ref tmp));

            Assert.IsNull(tmp);
        }
        #endregion

        #region InnerDoArithmeticOPFloat
        [TestMethod()]
        public void InnerDoArithmeticOPFloatAdd()
        {
            double a = Math.E;
            double b = Math.PI;

            double res = a + b;

            ExObject tmp = null;

            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(ExOperationCode.ADD, a, b, ref tmp));

            Assert.IsNotNull(tmp);

            Assert.AreEqual(res, tmp.GetFloat());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPFloatSub()
        {
            double a = Math.E;
            double b = Math.PI;

            double res = a - b;

            ExObject tmp = null;

            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(ExOperationCode.SUB, a, b, ref tmp));

            Assert.IsNotNull(tmp);

            Assert.AreEqual(res, tmp.GetFloat());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPFloatMlt()
        {
            double a = Math.E;
            double b = Math.PI;

            double res = a * b;

            ExObject tmp = null;

            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(ExOperationCode.MLT, a, b, ref tmp));

            Assert.IsNotNull(tmp);

            Assert.AreEqual(res, tmp.GetFloat());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPFloatDiv()
        {
            double a = Math.E;
            double b = Math.PI;

            double res = a / b;

            ExObject tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(ExOperationCode.DIV, a, b, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(res, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(ExOperationCode.DIV, Math.E, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.PositiveInfinity, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(ExOperationCode.DIV, 0, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.NaN, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(ExOperationCode.DIV, -Math.E, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.NegativeInfinity, tmp.GetFloat());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPFloatExp()
        {
            double a = Math.E;
            double b = Math.PI;

            double res = Math.Pow(a, b);

            ExObject tmp = null;

            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(ExOperationCode.EXP, a, b, ref tmp));

            Assert.IsNotNull(tmp);

            Assert.AreEqual(res, tmp.GetFloat());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPFloatMod()
        {
            double a = Math.E;
            double b = Math.PI;

            double res = a % b;

            ExObject tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(ExOperationCode.MOD, a, b, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(res, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(ExOperationCode.MOD, Math.E, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.PositiveInfinity, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(ExOperationCode.MOD, 0, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.NaN, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(ExOperationCode.MOD, -Math.E, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.NegativeInfinity, tmp.GetFloat());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPFloatUnknownOPC()
        {
            ExObject tmp = null;

            Assert.IsFalse(ExVM.InnerDoArithmeticOPFloat(ExOperationCode.CLOSE, Math.E, Math.PI, ref tmp));

            Assert.IsNull(tmp);
        }
        #endregion

        #region InnerDoArithmeticOPInt
        [TestMethod()]
        public void InnerDoArithmeticOPIntAdd()
        {
            long a = 3;
            long b = 4;

            long res = a + b;

            ExObject tmp = null;

            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(ExOperationCode.ADD, a, b, ref tmp));

            Assert.IsNotNull(tmp);

            Assert.AreEqual(res, tmp.GetInt());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPIntSub()
        {
            long a = 3;
            long b = 4;

            long res = a - b;

            ExObject tmp = null;

            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(ExOperationCode.SUB, a, b, ref tmp));

            Assert.IsNotNull(tmp);

            Assert.AreEqual(res, tmp.GetInt());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPIntMlt()
        {
            long a = 3;
            long b = 4;

            long res = a * b;

            ExObject tmp = null;

            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(ExOperationCode.MLT, a, b, ref tmp));

            Assert.IsNotNull(tmp);

            Assert.AreEqual(res, tmp.GetInt());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPIntDiv()
        {
            long a = 3;
            long b = 4;

            long res = a / b;

            ExObject tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(ExOperationCode.DIV, a, b, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(res, tmp.GetInt());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(ExOperationCode.DIV, 3, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.PositiveInfinity, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(ExOperationCode.DIV, 0, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.NaN, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(ExOperationCode.DIV, -3, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.NegativeInfinity, tmp.GetFloat());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPIntExp()
        {
            long a = 3;
            long b = 4;

            double res = Math.Pow(a, b);

            ExObject tmp = null;

            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(ExOperationCode.EXP, a, b, ref tmp));

            Assert.IsNotNull(tmp);

            Assert.AreEqual(res, tmp.GetFloat());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPIntMod()
        {
            long a = 3;
            long b = 4;

            long res = a % b;

            ExObject tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(ExOperationCode.MOD, a, b, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(res, tmp.GetInt());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(ExOperationCode.MOD, 3, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.PositiveInfinity, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(ExOperationCode.MOD, 0, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.NaN, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(ExOperationCode.MOD, -3, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.NegativeInfinity, tmp.GetFloat());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPIntUnknownOPC()
        {
            ExObject tmp = null;

            Assert.IsFalse(ExVM.InnerDoArithmeticOPInt(ExOperationCode.CLOSE, 3, 4, ref tmp));

            Assert.IsNull(tmp);
        }
        #endregion

        #region DoBitwiseOP
        [TestMethod()]
        public void DoBitwiseOPAnd()
        {
            ExObject a = new(2);
            ExObject b = new(6);

            long res = 2 & 6;

            ExObject tmp = new();

            Assert.IsTrue(ExVM.DoBitwiseOP((long)BitOP.AND, a, b, tmp));

            Assert.IsTrue(ExTypeCheck.IsNotNull(tmp));

            Assert.AreEqual(res, tmp.GetInt());
        }

        [TestMethod()]
        public void DoBitwiseOPOr()
        {
            ExObject a = new(2);
            ExObject b = new(6);

            long res = 2 | 6;

            ExObject tmp = new();

            Assert.IsTrue(ExVM.DoBitwiseOP((long)BitOP.OR, a, b, tmp));

            Assert.IsTrue(ExTypeCheck.IsNotNull(tmp));

            Assert.AreEqual(res, tmp.GetInt());
        }

        [TestMethod()]
        public void DoBitwiseOPXor()
        {
            ExObject a = new(2);
            ExObject b = new(6);

            long res = 2 ^ 6;

            ExObject tmp = new();

            Assert.IsTrue(ExVM.DoBitwiseOP((long)BitOP.XOR, a, b, tmp));

            Assert.IsTrue(ExTypeCheck.IsNotNull(tmp));

            Assert.AreEqual(res, tmp.GetInt());
        }

        [TestMethod()]
        public void DoBitwiseOPShiftLeft()
        {
            ExObject a = new(2);
            ExObject b = new(6);

            long res = 2 << 6;

            ExObject tmp = new();

            Assert.IsTrue(ExVM.DoBitwiseOP((long)BitOP.SHIFTL, a, b, tmp));

            Assert.IsTrue(ExTypeCheck.IsNotNull(tmp));

            Assert.AreEqual(res, tmp.GetInt());
        }

        [TestMethod()]
        public void DoBitwiseOPShiftRight()
        {
            ExObject a = new(6);
            ExObject b = new(1);

            long res = 6 >> 1;

            ExObject tmp = new();

            Assert.IsTrue(ExVM.DoBitwiseOP((long)BitOP.SHIFTR, a, b, tmp));

            Assert.IsTrue(ExTypeCheck.IsNotNull(tmp));

            Assert.AreEqual(res, tmp.GetInt());
        }

        [TestMethod()]
        public void DoBitwiseOPUnknownOPC()
        {
            ExObject tmp = new();

            Assert.IsFalse(ExVM.DoBitwiseOP((long)ExOperationCode.CLOSE, new(1), new(1), tmp));

            Assert.IsTrue(ExTypeCheck.IsNull(tmp));
        }

        [TestMethod()]
        public void DoBitwiseOPNonInteger()
        {
            ExObject a = new(Math.PI);
            ExObject b = new(1);

            ExObject tmp = new();

            Assert.IsFalse(ExVM.DoBitwiseOP((long)BitOP.SHIFTR, a, b, tmp));

            Assert.IsTrue(ExTypeCheck.IsNull(tmp));
        }
        #endregion

        #region InnerDoCompareOP
        [TestMethod()]
        public void InnerDoCompareOPIntegersLT()
        {
            ExObject a = new(2);
            ExObject b = new(6);

            int tmp = 999;
            int res = -1;

            Assert.IsTrue(ExVM.InnerDoCompareOP(a, b, ref tmp));
            Assert.AreEqual(res, tmp);
        }

        [TestMethod()]
        public void InnerDoCompareOPIntegersGT()
        {
            ExObject a = new(5);
            ExObject b = new(-123);

            int tmp = 999;
            int res = 1;

            Assert.IsTrue(ExVM.InnerDoCompareOP(a, b, ref tmp));
            Assert.AreEqual(res, tmp);
        }

        [TestMethod()]
        public void InnerDoCompareOPIntegersGETorLET()
        {
            ExObject a = new(33);
            ExObject b = new(33);

            int tmp = 999;
            int res = 0;

            Assert.IsTrue(ExVM.InnerDoCompareOP(a, b, ref tmp));
            Assert.AreEqual(res, tmp);
        }

        [TestMethod()]
        public void InnerDoCompareOPFloatsLT()
        {
            ExObject a = new(Math.E);
            ExObject b = new(Math.PI);

            int tmp = 999;
            int res = -1;

            Assert.IsTrue(ExVM.InnerDoCompareOP(a, b, ref tmp));
            Assert.AreEqual(res, tmp);
        }

        [TestMethod()]
        public void InnerDoCompareOPFloatsGT()
        {
            ExObject a = new(Math.PI);
            ExObject b = new(Math.E);

            int tmp = 999;
            int res = 1;

            Assert.IsTrue(ExVM.InnerDoCompareOP(a, b, ref tmp));
            Assert.AreEqual(res, tmp);
        }

        [TestMethod()]
        public void InnerDoCompareOPFloatsGETorLET()
        {
            ExObject a = new(Math.E);
            ExObject b = new(Math.E);

            int tmp = 999;
            int res = 0;

            Assert.IsTrue(ExVM.InnerDoCompareOP(a, b, ref tmp));
            Assert.AreEqual(res, tmp);
        }

        [TestMethod()]
        public void InnerDoCompareOPStringsLT()
        {
            ExObject a = new("test string");
            ExObject b = new("the other 1");

            int tmp = 999;
            int res = -1;

            Assert.IsTrue(ExVM.InnerDoCompareOP(a, b, ref tmp));
            Assert.AreEqual(res, tmp);
        }
        [TestMethod()]
        public void InnerDoCompareOPStringsGT()
        {
            ExObject a = new("the other 1");
            ExObject b = new("test string");

            int tmp = 999;
            int res = -1;

            Assert.IsTrue(ExVM.InnerDoCompareOP(a, b, ref tmp));
            Assert.AreEqual(res, tmp);
        }

        [TestMethod()]
        public void InnerDoCompareOPStringsGETorLET()
        {
            ExObject a = new("test string");
            ExObject b = new("test string");

            int tmp = 999;
            int res = 0;

            Assert.IsTrue(ExVM.InnerDoCompareOP(a, b, ref tmp));
            Assert.AreEqual(res, tmp);
        }
        #endregion

        #region DoNegateOP
        [TestMethod()]
        public void DoNegateOPInt()
        {
            ExObject a = new(2);

            long res = -2;

            ExObject tmp = new();

            Assert.IsTrue(ExVM.DoNegateOP(tmp, a));

            Assert.IsTrue(ExTypeCheck.IsNotNull(tmp));

            Assert.AreEqual(res, tmp.GetInt());
        }

        [TestMethod()]
        public void DoNegateOPFloat()
        {
            ExObject a = new(Math.PI);

            double res = -Math.PI;

            ExObject tmp = new();

            Assert.IsTrue(ExVM.DoNegateOP(tmp, a));

            Assert.IsTrue(ExTypeCheck.IsNotNull(tmp));

            Assert.AreEqual(res, tmp.GetFloat());
        }

        [TestMethod()]
        public void DoNegateOPComplex()
        {
            ExObject a = new(new Complex(3, 4.123));

            Complex res = -new Complex(3, 4.123);

            ExObject tmp = new();

            Assert.IsTrue(ExVM.DoNegateOP(tmp, a));

            Assert.IsTrue(ExTypeCheck.IsNotNull(tmp));

            Assert.AreEqual(res, tmp.GetComplex());
        }

        [TestMethod()]
        public void DoNegateOPUnknown()
        {
            ExObject a = new(new Dictionary<string, ExObject>());

            ExObject tmp = new();

            Assert.IsFalse(ExVM.DoNegateOP(tmp, a));

            Assert.IsFalse(ExTypeCheck.IsNotNull(tmp));
        }
        #endregion
    }
}