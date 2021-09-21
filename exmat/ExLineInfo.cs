#if DEBUG
using System.Diagnostics;
#endif

namespace ExMat.InfoVar
{
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    internal sealed class ExLineInfo
    {
        public int Position;
        public int Line;

        public ExLineInfo() { }

#if DEBUG
        private string GetDebuggerDisplay()
        {
            return ">>> LINE " + Line + "(" + Position + ")";
        }
#endif
    }
}
