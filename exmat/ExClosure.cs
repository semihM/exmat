using System.Collections.Generic;
using System.Diagnostics;
using ExMat.Class;
using ExMat.FuncPrototype;
using ExMat.Objects;
using ExMat.States;
using ExMat.Utils;

namespace ExMat.Closure
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExOuter : ExRefC
    {
        public int Index;
        public ExObject ValueRef;

        public ExOuter _prev;
        public ExOuter _next;
        public ExSState SharedState;

        public ExOuter()
        {
        }

        public static ExOuter Create(ExSState exS, ExObject o)
        {
            ExOuter exo = new() { SharedState = exS, ValueRef = new(o) };
            return exo;
        }

        public static new ExObjType GetType()
        {
            return ExObjType.OUTER;
        }

        public virtual void Release()
        {
            if (--ReferenceCount == 0)
            {
                Index = -1;
                if (ValueRef != null)
                {
                    ValueRef.Release();
                }
                SharedState = null;
                if (_prev != null)
                {
                    _prev._next = _next;
                    _prev = null;
                }
                if (_next != null)
                {
                    _next._prev = _prev;
                    _next = null;
                }
            }
        }

        public new string GetDebuggerDisplay()
        {
            return "OUTER(" + Index + ", " + (ValueRef == null ? "null" : ValueRef.GetInt()) + ")";
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Disposer.DisposeObjects(ValueRef);

            if (_prev != null)
            {
                _prev._next = _next;
                _prev = null;
            }
            if (_next != null)
            {
                _next._prev = _prev;
                _next = null;
            }

        }
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExClosure : ExRefC
    {
        public ExClass Base;
        public ExPrototype Function;
        public List<ExObject> OutersList;
        public List<ExObject> DefaultParams;
        public ExSState SharedState;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Base = null;

            WeakReference = null;
            Disposer.DisposeObjects(Function);
            Disposer.DisposeList(ref OutersList);
            Disposer.DisposeList(ref DefaultParams);
        }

        public static ExClosure Create(ExSState exS, ExPrototype fpro)
        {
            ExClosure cls = new() { SharedState = exS, Function = fpro };
            ExUtils.InitList(ref cls.OutersList, fpro.nOuters);
            ExUtils.InitList(ref cls.DefaultParams, fpro.nDefaultParameters);
            return cls;
        }

        public ExClosure()
        {
            ReferenceCount = 1;
        }

        public ExClosure Copy()
        {
            ExPrototype fp = Function;
            ExClosure res = Create(SharedState, fp);

            res.WeakReference = WeakReference;
            if (res.WeakReference != null)
            {
                res.WeakReference.ReferenceCount++;
            }

            for (int i = 0; i < fp.nOuters; i++)
            {
                res.OutersList[i].Assign(OutersList[i]);
            }
            for (int i = 0; i < fp.nDefaultParameters; i++)
            {
                res.DefaultParams[i].Assign(DefaultParams[i]);
            }
            return res;
        }

        public virtual void Release()
        {
            foreach (ExObject o in OutersList)
            {
                o.Nullify();
            }
            OutersList = null;

            foreach (ExObject o in DefaultParams)
            {
                o.Nullify();
            }
            DefaultParams = null;
        }
        public static new ExObjType GetType()
        {
            return ExObjType.CLOSURE;
        }

        public new string GetDebuggerDisplay()
        {
            if (Function.Name == null || Function.Name.Type == ExObjType.NULL)
            {
                return "CLOSURE";
            }
            if (Base != null)
            {
                return "[HAS BASE] CLOSURE(" + Function.Name.GetString() + ")";
            }
            return "CLOSURE(" + Function.Name.GetString() + ")";
        }


    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExNativeClosure : ExRefC
    {
        public ExSState SharedState;
        public ExObject Name;
        public ExRegFunc.FunctionRef Function;    // Bir C# metotuna işaret eder
        public bool IsDelegateFunction;

        public int nOuters;
        public int nParameterChecks;

        public List<ExObject> OutersList;
        public List<int> TypeMasks = new();

        public Dictionary<int, ExObject> DefaultValues = new();

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            WeakReference = null;
            TypeMasks = null;
            nOuters = 0;
            nParameterChecks = 0;
            Function = null;

            Disposer.DisposeObjects(Name);
            Disposer.DisposeList(ref OutersList);
            Disposer.DisposeDict(ref DefaultValues);
        }

        public ExNativeClosure()
        {
            ReferenceCount = 1;
        }

        public static ExNativeClosure Create(ExSState exS, ExRegFunc.FunctionRef f, int nout)
        {
            ExNativeClosure cls = new() { SharedState = exS, Function = f };
            ExUtils.InitList(ref cls.OutersList, nout);
            cls.nOuters = nout;
            return cls;
        }

        public virtual void Release()
        {
            foreach (ExObject o in OutersList)
            {
                o.Nullify();
            }
            OutersList = null;
        }
        public static new ExObjType GetType()
        {
            return ExObjType.NATIVECLOSURE;
        }

        public new string GetDebuggerDisplay()
        {
            return "NATIVECLOSURE(" + Name.GetString() + ")";
        }
    }
}
