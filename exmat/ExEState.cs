using System.Diagnostics;

namespace ExMat.States
{
    public enum ExEType
    {
        EXPRESSION, // İfade
        OBJECT,     // Obje
        BASE,       // Metotun ait olduğu sınıf, temel sınıf
        VAR,        // Değişken
        OUTER       // Bilinmeyen(dışarıda aranacak) değişken
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExEState
    {
        public ExEType Type;        // İfade tipi
        public int Position;        // İfade hedef bellek pozisyonu
        public bool ShouldStopDeref;    // İfade değeri bekletilmeli

        public string GetDebuggerDisplay()
        {
            return Type + " " + Position + " " + ShouldStopDeref;
        }
        public ExEState()
        {

        }

        public ExEState Copy()
        {
            return new() { Position = Position, ShouldStopDeref = ShouldStopDeref, Type = Type };
        }
    }
}
