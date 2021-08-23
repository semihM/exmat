using System.Diagnostics;

namespace ExMat.OPs
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExTrap
    {
        public int StackBase;
        public int StackSize;
        public ExInstr Instruction;
        public int Target;

        public ExTrap() { }
        public ExTrap(ExTrap e) { StackBase = e.StackBase; StackSize = e.StackSize; Instruction = e.Instruction; Target = e.Target; }
        private string GetDebuggerDisplay()
        {
            return "TRAP(" + Instruction.GetDebuggerDisplay() + "): " + StackBase + ", " + StackSize + ", " + Target;
        }
    }
}
