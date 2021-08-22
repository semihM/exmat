using System.Diagnostics;

namespace ExMat.InfoVar
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExLineInfo
    {
        public int Position;
        public int Line;

        public ExLineInfo() { }

        private string GetDebuggerDisplay()
        {
            return ">>> LINE " + Line + "(" + Position + ")";
        }
    }
}
