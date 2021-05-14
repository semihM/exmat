using System.Diagnostics;

namespace ExMat.States
{
    public enum ExEType
    {
        EXPRESSION,
        OBJECT,
        BASE,
        VAR,
        OUTER
    }


    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExEState
    {
        public ExEType _type;
        public int _pos;
        public bool stop_deref;

        public string GetDebuggerDisplay()
        {
            return _type + " " + _pos + " " + stop_deref;
        }
        public ExEState()
        {

        }

        public ExEState Copy()
        {
            return new() { _pos = _pos, stop_deref = stop_deref, _type = _type };
        }
    }
}
