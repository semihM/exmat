#if DEBUG
using System.Diagnostics;
#endif
using ExMat.Objects;
using ExMat.States;

namespace ExMat.Outer
{
    /// <summary>
    /// Internal object for outer value references
    /// </summary>
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public sealed class ExOuter : ExRefC
    {
        /// <summary>
        /// 
        /// </summary>
        public int Index;
        /// <summary>
        /// 
        /// </summary>
        public ExObject ValueRef;
        /// <summary>
        /// 
        /// </summary>
        public ExOuter _prev;
        /// <summary>
        /// 
        /// </summary>
        public ExOuter _next;
        /// <summary>
        /// 
        /// </summary>
        public ExSState SharedState;
        /// <summary>
        /// 
        /// </summary>
        public ExOuter()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exS"></param>
        /// <param name="o"></param>
        /// <returns></returns>
        public static ExOuter Create(ExSState exS, ExObject o)
        {
            ExOuter exo = new() { SharedState = exS, ValueRef = new(o) };
            return exo;
        }
        /// <summary>
        /// Deref
        /// </summary>
        internal void Release()
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
        internal new string GetDebuggerDisplay()
        {
            return "OUTER(" + Index + ", " + (ValueRef == null ? "null" : ValueRef.GetInt()) + ")";
        }
#endif
        /// <summary>
        /// Disposer
        /// </summary>
        /// <param name="disposing"></param>
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
