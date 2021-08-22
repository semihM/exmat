using System.Diagnostics;

namespace ExMat.Compiler
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExScope
    {
        public int nOuters;     // Referans edilen dışardaki değişken sayısı
        public int nLocal;      // Çerçevede tanımlı değişken sayısı

        public string GetDebuggerDisplay()
        {
            return "SCOPE(nOuters: " + nOuters + ", StackSize: " + nLocal + ")";
        }
    }
}
