using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExMat.Objects.Tests
{
    [TestClass()]
    public class ExObjectTests
    {
        public ExObjectTests() { }

        #region ExObjVal Related Tests
        [TestMethod()]
        public void ValidateExObjVal()
        {
            ExObjVal obj = new();

            Assert.AreEqual(false, obj.b_Bool);
            Assert.AreEqual(0, obj.i_Int);
            Assert.AreEqual(0.0, obj.f_Float);
            Assert.AreEqual(0.0, obj.c_Float);

            Assert.IsNull(obj.c_Space);
            Assert.IsNull(obj.s_String);

            Assert.IsNull(obj.l_List);
            Assert.IsNull(obj.d_Dict);

            Assert.IsNull(obj._Class);
            Assert.IsNull(obj._Instance);

            Assert.IsNull(obj._FuncPro);
            Assert.IsNull(obj._Outer);

            Assert.IsNull(obj._Closure);
            Assert.IsNull(obj._NativeClosure);

            Assert.IsNull(obj._RefC);
            Assert.IsNull(obj._WeakRef);
        }

        [TestMethod()]
        public void CreateRefCounterInstance()
        {
            ExRefC obj = new();

            Assert.AreEqual(0, obj.ReferenceCount);

            Assert.IsNull(obj.WeakReference);
        }

        [TestMethod()]
        public void CreateWeakrefInstance()
        {
            ExWeakRef obj = new();

            Assert.AreEqual(0, obj.ReferenceCount);

            Assert.IsNull(obj.ReferencedObject);

            Assert.IsNull(obj.WeakReference);
        }

        [TestMethod()]
        public void CreateWeakrefInstanceFromCounter()
        {
            ExRefC obj = new();

            Assert.AreEqual(obj.GetWeakRef(ExObjType.NULL, new()), obj.WeakReference);

            Assert.AreEqual(obj.ReferenceCount, 0);

            Assert.AreSame(obj.WeakReference.ReferencedObject.Value._RefC, obj);
        }
        #endregion

        #region Instancing Basic Types
        [TestMethod()]
        public void CreateNullInstance()
        {
            ExObject obj = new();

            ExObjVal lookup = new();

            Assert.AreEqual(ExObjType.NULL, obj.Type);

            typeof(ExObjVal)
                .GetFields()
                .ToList()
                .ForEach(fi => Assert.AreEqual(fi.GetValue(obj.Value), fi.GetValue(lookup)));
        }

        [TestMethod()]
        public void CreateIntegerInstance()
        {
            long test = long.MaxValue;

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.INTEGER, obj.Type);

            Assert.IsNull(obj.Value._RefC);

            Assert.AreEqual(obj.Value.i_Int, test);

            Assert.AreEqual(obj.GetInt(), test);
        }

        [TestMethod()]
        public void CreateFloatInstance()
        {
            double test = Math.E;

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.FLOAT, obj.Type);

            Assert.IsNull(obj.Value._RefC);

            Assert.AreEqual(obj.Value.f_Float, test);

            Assert.AreEqual(obj.GetFloat(), test);
        }

        [TestMethod()]
        public void CreateStringNullInstance()
        {
            string test = null;

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.STRING, obj.Type);

            Assert.IsNull(obj.Value._RefC);

            Assert.AreEqual(obj.Value.s_String, test);

            Assert.AreNotEqual(obj.GetString(), test);
        }

        [TestMethod()]
        public void CreateStringEmptyInstance()
        {
            string test = string.Empty;

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.STRING, obj.Type);

            Assert.IsNull(obj.Value._RefC);

            Assert.AreEqual(obj.Value.s_String, test);

            Assert.AreEqual(obj.GetString(), test);
        }

        [TestMethod()]
        public void CreateStringInstance()
        {
            string test = "Very cool string here!";

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.STRING, obj.Type);

            Assert.IsNull(obj.Value._RefC);

            Assert.AreEqual(obj.Value.s_String, test);

            Assert.AreEqual(obj.GetString(), test);
        }

        [TestMethod()]
        public void CreateComplexInstance()
        {
            Complex test = new(3, 4);

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.COMPLEX, obj.Type);

            Assert.IsNull(obj.Value._RefC);

            Assert.AreEqual(obj.Value.f_Float, test.Real);
            Assert.AreEqual(obj.Value.c_Float, test.Imaginary);

            Assert.AreEqual(obj.GetComplex(), test);
        }

        [TestMethod()]
        public void CreateBooleanInstance()
        {
            bool test = true;

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.BOOL, obj.Type);

            Assert.IsNull(obj.Value._RefC);

            Assert.AreEqual(obj.Value.b_Bool, test);

            Assert.AreEqual(obj.GetBool(), test);
        }

        [TestMethod()]
        public void CreateSpaceInstance()
        {
            ExSpace test = new();

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.SPACE, obj.Type);

            Assert.IsNotNull(obj.Value._RefC);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value.c_Space, test);

            Assert.AreSame(obj.GetSpace(), test);
        }

        [TestMethod()]
        public void CreateListEmptyInstance()
        {
            List<ExObject> test = new();

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.ARRAY, obj.Type);

            Assert.IsNotNull(obj.Value._RefC);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value.l_List, test);

            Assert.AreSame(obj.GetList(), test);
        }

        [TestMethod()]
        public void CreateDictInstance()
        {
            Dictionary<string, ExObject> test = new();

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.DICT, obj.Type);

            Assert.IsNotNull(obj.Value._RefC);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value.d_Dict, test);

            Assert.AreSame(obj.GetDict(), test);
        }

        [TestMethod()]
        public void CreateListInstanceFromExList()
        {
            List<ExObject> test = new();

            ExList obj = new(test);

            Assert.AreEqual(ExObjType.ARRAY, obj.Type);

            Assert.IsNotNull(obj.Value._RefC);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value.l_List, test);

            Assert.AreSame(obj.GetList(), test);
        }

        [TestMethod()]
        public void CreateExClassInstance()
        {
            ExClass.ExClass test = new();

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.CLASS, obj.Type);

            Assert.IsNotNull(obj.Value._RefC);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value._Class, test);

            Assert.AreSame(obj.GetClass(), test);
        }

        [TestMethod()]
        public void CreateExInstanceInstance()
        {
            ExClass.ExInstance test = new();

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.INSTANCE, obj.Type);

            Assert.IsNotNull(obj.Value._RefC);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value._Instance, test);

            Assert.AreSame(obj.GetInstance(), test);
        }

        [TestMethod()]
        public void CreateExClosureInstance()
        {
            Closure.ExClosure test = new();

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.CLOSURE, obj.Type);

            Assert.IsNotNull(obj.Value._RefC);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value._Closure, test);

            Assert.AreSame(obj.GetClosure(), test);
        }

        [TestMethod()]
        public void CreateExNativeClosureInstance()
        {
            Closure.ExNativeClosure test = new();

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.NATIVECLOSURE, obj.Type);

            Assert.IsNotNull(obj.Value._RefC);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value._NativeClosure, test);

            Assert.AreSame(obj.GetNClosure(), test);
        }
        #endregion

        #region Instancing Internal Types
        [TestMethod()]
        public void CreateExPrototypeInstance()
        {
            FuncPrototype.ExPrototype test = new();

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.FUNCPRO, obj.Type);

            Assert.IsNotNull(obj.Value._RefC);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value._FuncPro, test);
        }

        [TestMethod()]
        public void CreateExOuterInstance()
        {
            Outer.ExOuter test = new();

            ExObject obj = new(test);

            Assert.AreEqual(ExObjType.OUTER, obj.Type);

            Assert.IsNotNull(obj.Value._RefC);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value._Outer, test);
        }
        #endregion
    }
}
