using System.Diagnostics;

namespace ExMat.States
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExScope
    {
        public int outers;
        public int stack_size;

        public string GetDebuggerDisplay()
        {
            return "SCOPE(n_outers: " + outers + ", size_stack: " + stack_size + ")";
        }
    }
}
