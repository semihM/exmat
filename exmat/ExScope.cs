#if DEBUG
using System.Diagnostics;
#endif

namespace ExMat.Compiler
{
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    public class ExScope
    {
        public int nOuters;     // Referans edilen dışardaki değişken sayısı
        public int nLocal;      // Çerçevede tanımlı değişken sayısı

#if DEBUG
        public string GetDebuggerDisplay()
        {
            return "SCOPE(nOuters: " + nOuters + ", StackSize: " + nLocal + ")";
        }
#endif
    }
}
