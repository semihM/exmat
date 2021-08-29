using System;
using System.Numerics;
using ExMat.Exceptions;
using ExMat.Objects;
using ExMat.OPs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExMat.VM.Tests
{
    [TestClass()]
    public class ExVMTests
    {
        public ExVMTests()
        {
        }

        #region InnerDoArithmeticOPComplex
        [TestMethod()]
        public void InnerDoArithmeticOPComplexTestAdd()
        {
            Complex a = new(2, 6);
            Complex b = new(1, -2);

            Complex res = Complex.Add(a, b);

            ExObject tmp = null;

            Assert.IsTrue(ExVM.InnerDoArithmeticOPComplex(OPC.ADD, a, b, ref tmp));

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

            Assert.IsTrue(ExVM.InnerDoArithmeticOPComplex(OPC.SUB, a, b, ref tmp));

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

            Assert.IsTrue(ExVM.InnerDoArithmeticOPComplex(OPC.MLT, a, b, ref tmp));

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

            Assert.IsTrue(ExVM.InnerDoArithmeticOPComplex(OPC.DIV, a, b, ref tmp));

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

            Assert.IsTrue(ExVM.InnerDoArithmeticOPComplex(OPC.EXP, a, b, ref tmp));

            Assert.IsNotNull(tmp);

            Assert.AreEqual(res, tmp.GetComplex());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPComplexTestMod()
        {
            ExObject tmp = null;

            Assert.IsFalse(ExVM.InnerDoArithmeticOPComplex(OPC.MOD, new Complex(), new Complex(), ref tmp));

            Assert.IsNull(tmp);
        }

        [TestMethod()]
        public void InnerDoArithmeticOPComplexTestThrows()
        {
            ExObject tmp = null;

            Assert.ThrowsException<ExException>(
                () => ExVM.InnerDoArithmeticOPComplex(OPC.CLOSE, new Complex(), new Complex(), ref tmp)
            );
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

            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(OPC.ADD, a, b, ref tmp));

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

            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(OPC.SUB, a, b, ref tmp));

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

            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(OPC.MLT, a, b, ref tmp));

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
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(OPC.DIV, a, b, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(res, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(OPC.DIV, Math.E, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.PositiveInfinity, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(OPC.DIV, 0, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.NaN, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(OPC.DIV, -Math.E, 0, ref tmp));
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

            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(OPC.EXP, a, b, ref tmp));

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
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(OPC.MOD, a, b, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(res, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(OPC.MOD, Math.E, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.PositiveInfinity, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(OPC.MOD, 0, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.NaN, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPFloat(OPC.MOD, -Math.E, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.NegativeInfinity, tmp.GetFloat());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPFloatThrows()
        {
            ExObject tmp = null;

            Assert.ThrowsException<ExException>(
                () => ExVM.InnerDoArithmeticOPFloat(OPC.CLOSE, Math.E, Math.PI, ref tmp)
            );
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

            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(OPC.ADD, a, b, ref tmp));

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

            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(OPC.SUB, a, b, ref tmp));

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

            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(OPC.MLT, a, b, ref tmp));

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
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(OPC.DIV, a, b, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(res, tmp.GetInt());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(OPC.DIV, 3, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.PositiveInfinity, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(OPC.DIV, 0, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.NaN, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(OPC.DIV, -3, 0, ref tmp));
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

            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(OPC.EXP, a, b, ref tmp));

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
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(OPC.MOD, a, b, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(res, tmp.GetInt());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(OPC.MOD, 3, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.PositiveInfinity, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(OPC.MOD, 0, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.NaN, tmp.GetFloat());

            tmp = null;
            Assert.IsTrue(ExVM.InnerDoArithmeticOPInt(OPC.MOD, -3, 0, ref tmp));
            Assert.IsNotNull(tmp);
            Assert.AreEqual(double.NegativeInfinity, tmp.GetFloat());
        }

        [TestMethod()]
        public void InnerDoArithmeticOPIntThrows()
        {
            ExObject tmp = null;

            Assert.ThrowsException<ExException>(
                () => ExVM.InnerDoArithmeticOPInt(OPC.CLOSE, 3, 4, ref tmp)
            );
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

            Assert.IsTrue(tmp.Type != ExObjType.NULL);

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

            Assert.IsTrue(tmp.Type != ExObjType.NULL);

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

            Assert.IsTrue(tmp.Type != ExObjType.NULL);

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

            Assert.IsTrue(tmp.Type != ExObjType.NULL);

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

            Assert.IsTrue(tmp.Type != ExObjType.NULL);

            Assert.AreEqual(res, tmp.GetInt());
        }

        [TestMethod()]
        public void DoBitwiseOPThrowOpc()
        {
            ExObject tmp = new();

            Assert.ThrowsException<ExException>(
                () => ExVM.DoBitwiseOP((long)OPC.CLOSE, new(1), new(1), tmp)
            );
        }

        [TestMethod()]
        public void DoBitwiseOPNonInteger()
        {
            ExObject a = new(Math.PI);
            ExObject b = new(1);

            ExObject tmp = new();

            Assert.IsFalse(ExVM.DoBitwiseOP((long)BitOP.SHIFTR, a, b, tmp));

            Assert.IsTrue(tmp.Type == ExObjType.NULL);
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
        public void InnerDoCompareOPStringsLT()
        {
            ExObject a = new("test string");
            ExObject b = new("the other 1");

            int tmp = 999;
            int res = -1;

            Assert.IsTrue(ExVM.InnerDoCompareOP(a, b, ref tmp));
            Assert.AreEqual(res, tmp);
        }
        #endregion
    }
}