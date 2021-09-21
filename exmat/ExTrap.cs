#if DEBUG
using System.Diagnostics;
#endif

namespace ExMat.OPs
{
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    internal sealed class ExTrap
    {
        public int StackBase;
        public int StackSize;
        public ExInstr Instruction;
        public int Target;

        public ExTrap(int sbase, int size, ExInstr inst, int target)
        {
            StackBase = sbase;
            StackSize = size;
            Instruction = inst;
            Target = target;
        }

        public ExTrap(ExTrap e)
        {
            StackBase = e.StackBase;
            StackSize = e.StackSize;
            Instruction = e.Instruction;
            Target = e.Target;
        }
#if DEBUG
        private string GetDebuggerDisplay()
        {
            return "TRAP(" + Instruction.GetDebuggerDisplay() + "): " + StackBase + ", " + StackSize + ", " + Target;
        }
#endif
    }
}
