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

        #region Instancing
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

            ExObject obj = new(i: test);

            Assert.AreEqual(ExObjType.INTEGER, obj.Type);

            Assert.IsNull(obj.Value._RefC);

            Assert.AreEqual(obj.Value.i_Int, test);

            Assert.AreEqual(obj.GetInt(), test);
        }

        [TestMethod()]
        public void CreateFloatInstance()
        {
            double test = Math.E;

            ExObject obj = new(f: test);

            Assert.AreEqual(ExObjType.FLOAT, obj.Type);

            Assert.IsNull(obj.Value._RefC);

            Assert.AreEqual(obj.Value.f_Float, test);

            Assert.AreEqual(obj.GetFloat(), test);
        }

        [TestMethod()]
        public void CreateStringNullInstance()
        {
            string test = null;

            ExObject obj = new(s: test);

            Assert.AreEqual(ExObjType.STRING, obj.Type);

            Assert.IsNull(obj.Value._RefC);

            Assert.AreEqual(obj.Value.s_String, test);

            Assert.AreNotEqual(obj.GetString(), test);
        }

        [TestMethod()]
        public void CreateStringEmptyInstance()
        {
            string test = string.Empty;

            ExObject obj = new(s: test);

            Assert.AreEqual(ExObjType.STRING, obj.Type);

            Assert.IsNull(obj.Value._RefC);

            Assert.AreEqual(obj.Value.s_String, test);

            Assert.AreEqual(obj.GetString(), test);
        }

        [TestMethod()]
        public void CreateStringInstance()
        {
            string test = "Very cool string here!";

            ExObject obj = new(s: test);

            Assert.AreEqual(ExObjType.STRING, obj.Type);

            Assert.IsNull(obj.Value._RefC);

            Assert.AreEqual(obj.Value.s_String, test);

            Assert.AreEqual(obj.GetString(), test);
        }

        [TestMethod()]
        public void CreateComplexInstance()
        {
            Complex test = new(3, 4);

            ExObject obj = new(cmplx: test);

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

            ExObject obj = new(b: test);

            Assert.AreEqual(ExObjType.BOOL, obj.Type);

            Assert.IsNull(obj.Value._RefC);

            Assert.AreEqual(obj.Value.b_Bool, test);

            Assert.AreEqual(obj.GetBool(), test);
        }

        [TestMethod()]
        public void CreateSpaceInstance()
        {
            ExSpace test = new();

            ExObject obj = new(space: test);

            Assert.AreEqual(ExObjType.SPACE, obj.Type);

            Assert.AreSame(obj.Value._RefC, test);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value.c_Space, test);

            Assert.AreSame(obj.GetSpace(), test);
        }

        [TestMethod()]
        public void CreateListEmptyInstance()
        {
            List<ExObject> test = new();

            ExObject obj = new(lis: test);

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

            ExObject obj = new(dict: test);

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

            ExList obj = new(e: test);

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

            ExObject obj = new(@class: test);

            Assert.AreEqual(ExObjType.CLASS, obj.Type);

            Assert.AreSame(obj.Value._RefC, test);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value._Class, test);

            Assert.AreSame(obj.GetClass(), test);
        }

        [TestMethod()]
        public void CreateExInstanceInstance()
        {
            ExClass.ExInstance test = new();

            ExObject obj = new(inst: test);

            Assert.AreEqual(ExObjType.INSTANCE, obj.Type);

            Assert.AreSame(obj.Value._RefC, test);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value._Instance, test);

            Assert.AreSame(obj.GetInstance(), test);
        }

        [TestMethod()]
        public void CreateExClosureInstance()
        {
            Closure.ExClosure test = new();

            ExObject obj = new(cls: test);

            Assert.AreEqual(ExObjType.CLOSURE, obj.Type);

            Assert.AreSame(obj.Value._RefC, test);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value._Closure, test);

            Assert.AreSame(obj.GetClosure(), test);
        }

        [TestMethod()]
        public void CreateExNativeClosureInstance()
        {
            Closure.ExNativeClosure test = new();

            ExObject obj = new(ncls: test);

            Assert.AreEqual(ExObjType.NATIVECLOSURE, obj.Type);

            Assert.AreSame(obj.Value._RefC, test);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value._NativeClosure, test);

            Assert.AreSame(obj.GetNClosure(), test);
        }

        [TestMethod()]
        public void CreateExPrototypeInstance()
        {
            FuncPrototype.ExPrototype test = new();

            ExObject obj = new(pro: test);

            Assert.AreEqual(ExObjType.FUNCPRO, obj.Type);

            Assert.AreSame(obj.Value._RefC, test);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value._FuncPro, test);
        }

        [TestMethod()]
        public void CreateExOuterInstance()
        {
            Outer.ExOuter test = new();

            ExObject obj = new(outer: test);

            Assert.AreEqual(ExObjType.OUTER, obj.Type);

            Assert.AreSame(obj.Value._RefC, test);

            Assert.AreEqual(1, obj.Value._RefC.ReferenceCount);

            Assert.AreSame(obj.Value._Outer, test);
        }
        #endregion

        #region ExTypeCheck Tests
        [TestMethod()]
        public void ValidateExObjTypeRefCheck()
        {
            Assert.IsFalse(ExTypeCheck.DoesTypeCountRef(ExObjType.NULL));
            Assert.IsFalse(ExTypeCheck.DoesTypeCountRef(ExObjType.INTEGER));
            Assert.IsFalse(ExTypeCheck.DoesTypeCountRef(ExObjType.FLOAT));
            Assert.IsFalse(ExTypeCheck.DoesTypeCountRef(ExObjType.BOOL));
            Assert.IsFalse(ExTypeCheck.DoesTypeCountRef(ExObjType.STRING));
            Assert.IsFalse(ExTypeCheck.DoesTypeCountRef(ExObjType.COMPLEX));

            Assert.IsTrue(ExTypeCheck.DoesTypeCountRef(ExObjType.ARRAY));
            Assert.IsTrue(ExTypeCheck.DoesTypeCountRef(ExObjType.DICT));
            Assert.IsTrue(ExTypeCheck.DoesTypeCountRef(ExObjType.SPACE));
            Assert.IsTrue(ExTypeCheck.DoesTypeCountRef(ExObjType.CLASS));
            Assert.IsTrue(ExTypeCheck.DoesTypeCountRef(ExObjType.INSTANCE));
            Assert.IsTrue(ExTypeCheck.DoesTypeCountRef(ExObjType.CLOSURE));
            Assert.IsTrue(ExTypeCheck.DoesTypeCountRef(ExObjType.NATIVECLOSURE));
            Assert.IsTrue(ExTypeCheck.DoesTypeCountRef(ExObjType.WEAKREF));

            Assert.IsTrue(ExTypeCheck.DoesTypeCountRef(ExObjType.FUNCPRO));
            Assert.IsTrue(ExTypeCheck.DoesTypeCountRef(ExObjType.OUTER));
        }

        [TestMethod()]
        public void ValidateExTypeCheckRefCount()
        {
            Assert.IsFalse(ExTypeCheck.IsCountingRefs(new()));
            Assert.IsFalse(ExTypeCheck.IsCountingRefs(new(int.MaxValue)));
            Assert.IsFalse(ExTypeCheck.IsCountingRefs(new(Math.PI)));
            Assert.IsFalse(ExTypeCheck.IsCountingRefs(new(false)));
            Assert.IsFalse(ExTypeCheck.IsCountingRefs(new(string.Empty)));
            Assert.IsFalse(ExTypeCheck.IsCountingRefs(new(new Complex(3, 4))));

            Assert.IsTrue(ExTypeCheck.IsCountingRefs(new(new List<ExObject>())));
            Assert.IsTrue(ExTypeCheck.IsCountingRefs(new(new Dictionary<string, ExObject>())));
            Assert.IsTrue(ExTypeCheck.IsCountingRefs(new(new ExSpace())));
            Assert.IsTrue(ExTypeCheck.IsCountingRefs(new(new ExClass.ExClass())));
            Assert.IsTrue(ExTypeCheck.IsCountingRefs(new(new ExClass.ExInstance())));
            Assert.IsTrue(ExTypeCheck.IsCountingRefs(new(new Closure.ExClosure())));
            Assert.IsTrue(ExTypeCheck.IsCountingRefs(new(new Closure.ExNativeClosure())));
            Assert.IsTrue(ExTypeCheck.IsCountingRefs(new(new ExWeakRef())));

            Assert.IsTrue(ExTypeCheck.IsCountingRefs(new(new FuncPrototype.ExPrototype())));
            Assert.IsTrue(ExTypeCheck.IsCountingRefs(new(new Outer.ExOuter())));
        }

        [TestMethod()]
        public void ValidateExTypeCheckDeleg()
        {
            Assert.IsFalse(ExTypeCheck.IsDelegable(new()));
            Assert.IsFalse(ExTypeCheck.IsDelegable(new(false)));
            Assert.IsTrue(ExTypeCheck.IsDelegable(new(int.MaxValue)));
            Assert.IsTrue(ExTypeCheck.IsDelegable(new(Math.PI)));
            Assert.IsTrue(ExTypeCheck.IsDelegable(new(string.Empty)));
            Assert.IsTrue(ExTypeCheck.IsDelegable(new(new Complex(3, 4))));

            Assert.IsFalse(ExTypeCheck.IsDelegable(new(new ExSpace())));
            Assert.IsTrue(ExTypeCheck.IsDelegable(new(new List<ExObject>())));
            Assert.IsTrue(ExTypeCheck.IsDelegable(new(new Dictionary<string, ExObject>())));
            Assert.IsTrue(ExTypeCheck.IsDelegable(new(new ExClass.ExClass())));
            Assert.IsTrue(ExTypeCheck.IsDelegable(new(new ExClass.ExInstance())));
            Assert.IsTrue(ExTypeCheck.IsDelegable(new(new Closure.ExClosure())));
            Assert.IsTrue(ExTypeCheck.IsDelegable(new(new Closure.ExNativeClosure())));
            Assert.IsTrue(ExTypeCheck.IsDelegable(new(new ExWeakRef())));

            Assert.IsFalse(ExTypeCheck.IsDelegable(new(new FuncPrototype.ExPrototype())));
            Assert.IsFalse(ExTypeCheck.IsDelegable(new(new Outer.ExOuter())));
        }

        [TestMethod()]
        public void ValidateExTypeCheckFalseable()
        {
            Assert.IsFalse(ExTypeCheck.IsFalseable(new(string.Empty)));
            Assert.IsTrue(ExTypeCheck.IsFalseable(new()));
            Assert.IsTrue(ExTypeCheck.IsFalseable(new(false)));
            Assert.IsTrue(ExTypeCheck.IsFalseable(new(int.MaxValue)));
            Assert.IsTrue(ExTypeCheck.IsFalseable(new(Math.PI)));
            Assert.IsTrue(ExTypeCheck.IsFalseable(new(new Complex(3, 4))));

            Assert.IsFalse(ExTypeCheck.IsFalseable(new(new ExSpace())));
            Assert.IsFalse(ExTypeCheck.IsFalseable(new(new List<ExObject>())));
            Assert.IsFalse(ExTypeCheck.IsFalseable(new(new Dictionary<string, ExObject>())));
            Assert.IsFalse(ExTypeCheck.IsFalseable(new(new ExClass.ExClass())));
            Assert.IsFalse(ExTypeCheck.IsFalseable(new(new ExClass.ExInstance())));
            Assert.IsFalse(ExTypeCheck.IsFalseable(new(new Closure.ExClosure())));
            Assert.IsFalse(ExTypeCheck.IsFalseable(new(new Closure.ExNativeClosure())));
            Assert.IsFalse(ExTypeCheck.IsFalseable(new(new ExWeakRef())));

            Assert.IsFalse(ExTypeCheck.IsFalseable(new(new FuncPrototype.ExPrototype())));
            Assert.IsFalse(ExTypeCheck.IsFalseable(new(new Outer.ExOuter())));
        }

        [TestMethod()]
        public void ValidateExTypeCheckNumeric()
        {
            Assert.IsFalse(ExTypeCheck.IsNumeric(new(string.Empty)));
            Assert.IsFalse(ExTypeCheck.IsNumeric(new()));
            Assert.IsFalse(ExTypeCheck.IsNumeric(new(false)));
            Assert.IsTrue(ExTypeCheck.IsNumeric(new(int.MaxValue)));
            Assert.IsTrue(ExTypeCheck.IsNumeric(new(Math.PI)));
            Assert.IsTrue(ExTypeCheck.IsNumeric(new(new Complex(3, 4))));

            Assert.IsFalse(ExTypeCheck.IsNumeric(new(new ExSpace())));
            Assert.IsFalse(ExTypeCheck.IsNumeric(new(new List<ExObject>())));
            Assert.IsFalse(ExTypeCheck.IsNumeric(new(new Dictionary<string, ExObject>())));
            Assert.IsFalse(ExTypeCheck.IsNumeric(new(new ExClass.ExClass())));
            Assert.IsFalse(ExTypeCheck.IsNumeric(new(new ExClass.ExInstance())));
            Assert.IsFalse(ExTypeCheck.IsNumeric(new(new Closure.ExClosure())));
            Assert.IsFalse(ExTypeCheck.IsNumeric(new(new Closure.ExNativeClosure())));
            Assert.IsFalse(ExTypeCheck.IsNumeric(new(new ExWeakRef())));

            Assert.IsFalse(ExTypeCheck.IsNumeric(new(new FuncPrototype.ExPrototype())));
            Assert.IsFalse(ExTypeCheck.IsNumeric(new(new Outer.ExOuter())));
        }

        [TestMethod()]
        public void ValidateExTypeCheckRealNumber()
        {
            Assert.IsFalse(ExTypeCheck.IsRealNumber(new(string.Empty)));
            Assert.IsFalse(ExTypeCheck.IsRealNumber(new()));
            Assert.IsFalse(ExTypeCheck.IsRealNumber(new(false)));
            Assert.IsFalse(ExTypeCheck.IsRealNumber(new(new Complex(3, 4))));
            Assert.IsTrue(ExTypeCheck.IsRealNumber(new(int.MaxValue)));
            Assert.IsTrue(ExTypeCheck.IsRealNumber(new(Math.PI)));

            Assert.IsFalse(ExTypeCheck.IsRealNumber(new(new ExSpace())));
            Assert.IsFalse(ExTypeCheck.IsRealNumber(new(new List<ExObject>())));
            Assert.IsFalse(ExTypeCheck.IsRealNumber(new(new Dictionary<string, ExObject>())));
            Assert.IsFalse(ExTypeCheck.IsRealNumber(new(new ExClass.ExClass())));
            Assert.IsFalse(ExTypeCheck.IsRealNumber(new(new ExClass.ExInstance())));
            Assert.IsFalse(ExTypeCheck.IsRealNumber(new(new Closure.ExClosure())));
            Assert.IsFalse(ExTypeCheck.IsRealNumber(new(new Closure.ExNativeClosure())));
            Assert.IsFalse(ExTypeCheck.IsRealNumber(new(new ExWeakRef())));

            Assert.IsFalse(ExTypeCheck.IsRealNumber(new(new FuncPrototype.ExPrototype())));
            Assert.IsFalse(ExTypeCheck.IsRealNumber(new(new Outer.ExOuter())));
        }

        [TestMethod()]
        public void ValidateExTypeCheckNull()
        {
            Assert.IsFalse(ExTypeCheck.IsNull(new(string.Empty)));
            Assert.IsFalse(ExTypeCheck.IsNull(new(false)));
            Assert.IsFalse(ExTypeCheck.IsNull(new(new Complex(3, 4))));
            Assert.IsFalse(ExTypeCheck.IsNull(new(int.MaxValue)));
            Assert.IsFalse(ExTypeCheck.IsNull(new(Math.PI)));
            Assert.IsTrue(ExTypeCheck.IsNull(new()));

            Assert.IsFalse(ExTypeCheck.IsNull(new(new ExSpace())));
            Assert.IsFalse(ExTypeCheck.IsNull(new(new List<ExObject>())));
            Assert.IsFalse(ExTypeCheck.IsNull(new(new Dictionary<string, ExObject>())));
            Assert.IsFalse(ExTypeCheck.IsNull(new(new ExClass.ExClass())));
            Assert.IsFalse(ExTypeCheck.IsNull(new(new ExClass.ExInstance())));
            Assert.IsFalse(ExTypeCheck.IsNull(new(new Closure.ExClosure())));
            Assert.IsFalse(ExTypeCheck.IsNull(new(new Closure.ExNativeClosure())));
            Assert.IsFalse(ExTypeCheck.IsNull(new(new ExWeakRef())));

            Assert.IsFalse(ExTypeCheck.IsNull(new(new FuncPrototype.ExPrototype())));
            Assert.IsFalse(ExTypeCheck.IsNull(new(new Outer.ExOuter())));
        }
        #endregion

        #region Assigning
        [TestMethod()]
        public void AssignIntegerToNull()
        {
            long src = char.MaxValue;

            ExObject dest = new();

            dest.Assign(i: src);

            Assert.IsNull(dest.Value._RefC);

            Assert.AreEqual(ExObjType.INTEGER, dest.Type);

            Assert.AreEqual(dest.Value.i_Int, src);

            Assert.AreEqual(dest.GetInt(), src);
        }

        [TestMethod()]
        public void AssignFloatToNull()
        {
            double src = Math.E;

            ExObject dest = new();

            dest.Assign(f: src);

            Assert.IsNull(dest.Value._RefC);

            Assert.AreEqual(ExObjType.FLOAT, dest.Type);

            Assert.AreEqual(dest.Value.f_Float, src);

            Assert.AreEqual(dest.GetFloat(), src);
        }

        [TestMethod()]
        public void AssignBoolToNull()
        {
            bool src = true;

            ExObject dest = new();

            dest.Assign(b: src);

            Assert.IsNull(dest.Value._RefC);

            Assert.AreEqual(ExObjType.BOOL, dest.Type);

            Assert.AreEqual(dest.Value.b_Bool, src);

            Assert.AreEqual(dest.GetBool(), src);
        }

        [TestMethod()]
        public void AssignStringToNull()
        {
            string src = "hmm yes, this is indeed a string...";

            ExObject dest = new();

            dest.Assign(s: src);

            Assert.IsNull(dest.Value._RefC);

            Assert.AreEqual(ExObjType.STRING, dest.Type);

            Assert.AreSame(dest.Value.s_String, src);

            Assert.AreSame(dest.GetString(), src);
        }

        [TestMethod()]
        public void AssignComplexToNull()
        {
            Complex src = new(3, 4);

            ExObject dest = new();

            dest.Assign(cmplx: src);

            Assert.IsNull(dest.Value._RefC);

            Assert.AreEqual(ExObjType.COMPLEX, dest.Type);

            Assert.AreEqual(dest.Value.f_Float, src.Real);
            Assert.AreEqual(dest.Value.c_Float, src.Imaginary);

            Assert.AreEqual(dest.GetComplex(), src);
        }

        [TestMethod()]
        public void AssignSpaceToNull()
        {
            ExSpace src = new();

            ExObject dest = new();

            dest.Assign(space: src);

            Assert.AreSame(dest.Value._RefC, src);

            Assert.AreEqual(1, dest.Value._RefC.ReferenceCount);

            Assert.AreEqual(ExObjType.SPACE, dest.Type);

            Assert.AreSame(dest.Value.c_Space, src);

            Assert.AreSame(dest.GetSpace(), src);
        }

        [TestMethod()]
        public void AssignListToNull()
        {
            List<ExObject> src = new();

            ExObject dest = new();

            dest.Assign(lis: src);

            Assert.IsNotNull(dest.Value._RefC);

            Assert.AreEqual(1, dest.Value._RefC.ReferenceCount);

            Assert.AreEqual(ExObjType.ARRAY, dest.Type);

            Assert.AreSame(dest.Value.l_List, src);

            Assert.AreSame(dest.GetList(), src);
        }

        [TestMethod()]
        public void AssignDictToNull()
        {
            Dictionary<string, ExObject> src = new();

            ExObject dest = new();

            dest.Assign(dict: src);

            Assert.IsNotNull(dest.Value._RefC);

            Assert.AreEqual(1, dest.Value._RefC.ReferenceCount);

            Assert.AreEqual(ExObjType.DICT, dest.Type);

            Assert.AreSame(dest.Value.d_Dict, src);

            Assert.AreSame(dest.GetDict(), src);
        }

        [TestMethod()]
        public void AssignClosureToNull()
        {
            Closure.ExClosure src = new();

            ExObject dest = new();

            dest.Assign(cls: src);

            Assert.AreSame(dest.Value._Closure, dest.Value._RefC);

            Assert.AreEqual(1, dest.Value._RefC.ReferenceCount);

            Assert.AreEqual(ExObjType.CLOSURE, dest.Type);

            Assert.AreSame(dest.Value._Closure, src);

            Assert.AreSame(dest.GetClosure(), src);
        }

        [TestMethod()]
        public void AssignNativeClosureToNull()
        {
            Closure.ExNativeClosure src = new();

            ExObject dest = new();

            dest.Assign(ncls: src);

            Assert.AreSame(dest.Value._NativeClosure, dest.Value._RefC);

            Assert.AreEqual(1, dest.Value._RefC.ReferenceCount);

            Assert.AreEqual(ExObjType.NATIVECLOSURE, dest.Type);

            Assert.AreSame(dest.Value._NativeClosure, src);

            Assert.AreSame(dest.GetNClosure(), src);
        }

        [TestMethod()]
        public void AssignClassToNull()
        {
            ExClass.ExClass src = new();

            ExObject dest = new();

            dest.Assign(@class: src);

            Assert.AreSame(dest.Value._Class, dest.Value._RefC);

            Assert.AreEqual(1, dest.Value._RefC.ReferenceCount);

            Assert.AreEqual(ExObjType.CLASS, dest.Type);

            Assert.AreSame(dest.Value._Class, src);

            Assert.AreSame(dest.GetClass(), src);
        }

        [TestMethod()]
        public void AssignInstanceToNull()
        {
            ExClass.ExInstance src = new();

            ExObject dest = new();

            dest.Assign(inst: src);

            Assert.AreSame(dest.Value._Instance, dest.Value._RefC);

            Assert.AreEqual(1, dest.Value._RefC.ReferenceCount);

            Assert.AreEqual(ExObjType.INSTANCE, dest.Type);

            Assert.AreSame(dest.Value._Instance, src);

            Assert.AreSame(dest.GetInstance(), src);
        }

        [TestMethod()]
        public void AssignWeakrefToNull()
        {
            ExWeakRef src = new();

            ExObject dest = new();

            dest.Assign(wref: src);

            Assert.AreSame(dest.Value._WeakRef, dest.Value._RefC);

            Assert.AreEqual(1, dest.Value._RefC.ReferenceCount);

            Assert.AreEqual(ExObjType.WEAKREF, dest.Type);

            Assert.AreSame(dest.Value._WeakRef, src);

            Assert.AreSame(dest.GetWeakRef(), src);
        }

        [TestMethod()]
        public void AssignOuterToNull()
        {
            Outer.ExOuter src = new();

            ExObject dest = new();

            dest.Assign(outer: src);

            Assert.AreSame(dest.Value._Outer, dest.Value._RefC);

            Assert.AreEqual(1, dest.Value._RefC.ReferenceCount);

            Assert.AreEqual(ExObjType.OUTER, dest.Type);

            Assert.AreSame(dest.Value._Outer, src);
        }

        [TestMethod()]
        public void AssignPrototypeToNull()
        {
            FuncPrototype.ExPrototype src = new();

            ExObject dest = new();

            dest.Assign(pro: src);

            Assert.AreSame(dest.Value._FuncPro, dest.Value._RefC);

            Assert.AreEqual(1, dest.Value._RefC.ReferenceCount);

            Assert.AreEqual(ExObjType.FUNCPRO, dest.Type);

            Assert.AreSame(dest.Value._FuncPro, src);
        }

        [TestMethod()]
        public void AssignNullExObjectToNull()
        {
            ExObject src = new();

            ExObject dest = new();

            dest.Assign(other: src);

            Assert.AreEqual(dest.Type, src.Type);

            Assert.AreEqual(dest.Value, src.Value);

            Assert.IsNull(dest.Value._RefC);
        }

        [TestMethod()]
        public void AssignNullExObjectToListExObjectReferenceCount1()
        {
            string str = "test array";

            char[] test = str.ToArray();

            ExObject src = new();

            ExObject dest = new ExList(test);

            List<ExObject> lis = dest.GetList();

            dest.Assign(other: src);

            Assert.AreEqual(dest.Type, src.Type);

            Assert.AreEqual(dest.Value, src.Value);

            Assert.IsNull(dest.Value._RefC);

            Assert.AreEqual(0, lis.Count);

            Assert.AreEqual(str.Length, test.Length);
        }

        [TestMethod()]
        public void AssignNullExObjectToListExObjectReferenceCount2()
        {
            string str = "test array";

            char[] test = str.ToArray();

            ExObject src = new();

            ExObject dest = new ExList(test);

            ExObject temp = new(dest);

            List<ExObject> lis = dest.GetList();

            Assert.AreSame(lis, temp.GetList());

            Assert.AreEqual(2, dest.Value._RefC.ReferenceCount);

            dest.Assign(other: src);

            Assert.AreEqual(dest.Type, src.Type);

            Assert.AreEqual(dest.Value, src.Value);

            Assert.IsNull(dest.Value._RefC);

            Assert.AreEqual(str.Length, lis.Count);

            Assert.AreEqual(str.Length, test.Length);

            lis.ForEach(o => Assert.IsNotNull(o));
        }

        [TestMethod()]
        public void AssignNullExObjectToClosureReferenceCount1()
        {
            States.ExSState ss = new();
            Closure.ExClosure closure = new() { SharedState = ss };

            ExObject src = new();

            ExObject dest = new(closure);

            dest.Assign(other: src);

            Assert.AreEqual(dest.Type, src.Type);

            Assert.AreEqual(dest.Value, src.Value);

            Assert.IsNull(dest.Value._RefC);

            Assert.AreEqual(0, closure.ReferenceCount);

            Assert.IsNull(closure.SharedState);
        }

        [TestMethod()]
        public void AssignNullExObjectToClosureReferenceCount2()
        {
            States.ExSState ss = new();
            Closure.ExClosure closure = new() { SharedState = ss };

            ExObject src = new();

            ExObject dest = new(closure);

            ExObject temp = new(dest);

            Closure.ExClosure cls = dest.GetClosure();

            Assert.AreSame(cls, temp.GetClosure());

            Assert.AreEqual(2, dest.Value._RefC.ReferenceCount);

            dest.Assign(other: src);

            Assert.AreEqual(dest.Type, src.Type);

            Assert.AreEqual(dest.Value, src.Value);

            Assert.IsNull(dest.Value._RefC);

            Assert.AreSame(cls, closure);

            Assert.AreEqual(1, cls.ReferenceCount);
        }
        #endregion
    }
}
