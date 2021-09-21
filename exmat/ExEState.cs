#if DEBUG
using System.Diagnostics;
#endif

namespace ExMat.States
{
    internal enum ExEType
    {
        EXPRESSION, // İfade
        OBJECT,     // Obje
        BASE,       // Metotun ait olduğu sınıf, temel sınıf
        VAR,        // Değişken
        CONSTDELEG, // Sabit tablo temsili fonksiyonu
        OUTER       // Bilinmeyen(dışarıda aranacak) değişken
    }

#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
    internal class ExEState
    {
        public ExEType Type;        // İfade tipi
        public int Position;        // İfade hedef bellek pozisyonu
        public bool ShouldStopDeref;    // İfade değeri bekletilmeli

#if DEBUG
        public string GetDebuggerDisplay()
        {
            return Type + " " + Position + " " + ShouldStopDeref;
        }
#endif
        public ExEState()
        {

        }

        public ExEState Copy()
        {
            return new() { Position = Position, ShouldStopDeref = ShouldStopDeref, Type = Type };
        }
    }
}
