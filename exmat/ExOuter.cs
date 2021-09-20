#if DEBUG
using System.Diagnostics;
#endif
using ExMat.Objects;
using ExMat.States;

namespace ExMat.Outer
{
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
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

#if DEBUG
        public new string GetDebuggerDisplay()
        {
            return "OUTER(" + Index + ", " + (ValueRef == null ? "null" : ValueRef.GetInt()) + ")";
        }
#endif

        protected override void Dispose(bool disposing)
        {
            if (ReferenceCount > 0)
            {
                return;
            }
            base.Dispose(disposing);

            ExDisposer.DisposeObjects(ValueRef);

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
}
