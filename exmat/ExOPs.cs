using System;
using System.Diagnostics;
using ExMat.Objects;

namespace ExMat.OPs
{
    public enum BitOP
    {
        AND,
        OR = 2,
        XOR,
        SHIFTL,
        SHIFTR
    }

    public enum CmpOP
    {
        GRT,
        GET = 2,
        LST,
        LET
    }

    public enum OPC
    {
        LOADINTEGER,    // Tamsayı atama
        LOADFLOAT,      // Ondalıklı sayı atama
        LOADBOOLEAN,    // Boolean atama
        LOADCOMPLEX,    // Kompleks sayı atama
        LOADSPACE,      // Uzay atama

        EQ,             // Eşitlik kontrolü
        NEQ,            // Eşitsizlik kontrolü
        CMP,            // Karşılaştır
        JCMP,           // Karşılaştırma koşullu komut atla
        JMP,            // Komut atla
        JZ,             // Koşullu komut atla

        CALL,           // Fonksiyon çağır
        PREPCALL,       // Fonksiyon çağırma hazırlığı(bilinen değişken)
        PREPCALLK,      // Fonksiyon çağırma hazırlığı(dışardan değişken)
        CALLTAIL,       // Fonksiyon sonucunu çağır

        LOAD,           // Yazı dizisi atama(direkt)
        DLOAD,          // Yazı dizisi atama(ifade işlenmeli)

        GET,            // Bilinen değişken değeri
        GETK,           // Dışardan değişken değeri

        MOVE,           // Değerin bellekteki yerini değiştir(direkt)
        DMOVE,          // Değerin bellekteki yerini değiştir(ifade işlenmeli)

        SET,            // Global değişkene, metota, özelliğe değer atama

        DELETE,         // Değerin denkini sil

        CLOSURE,        // Fonksiyon oluştur

        EXISTS,         // Objeye aitlik kontrolü
        INSTANCEOF,     // Sınıfa aitlik kontrolü
        TYPEOF,         // Obje tipi

        RETURN,         // Değer dön

        APPENDTOARRAY,  // Liste sonuna ekle

        LOADNULL,       // Boş değer ata
        LOADROOT,       // Temel kütüphaneyi yükle

        SETOUTER,       // Dışardaki değişkenin değerini değiştir
        GETOUTER,       // Dışardan değişken bul

        NEWOBJECT,      // Yeni obje oluştur
        NEWSLOT,        // Yeni obje özelliği oluştur
        NEWSLOTA,       // Sınıf elemanına ek özellik oluştur

        COMPOUNDARITH,  // Bileşik aritmetik işlem

        MOD,            // Modulo işlemi

        INC,            // Tipi belirsiz değeri 1 arttır veya azalt(sonucu dönme)
        PINC,           // Tipi belirsiz değeri 1 arttır veya azalt(sonucu dön)
        INCL,           // Değişken değerini 1 arttır veya azalt(sonucu dönme)
        PINCL,          // Değişken değerini 1 arttır veya azalt(sonucu dön)

        MLT,            // Çarpım işlemi
        ADD,            // Toplama işlemi

        LINE,           // Hata takibine yardımcı

        SUB,            // Çıkarma işlemi

        BITWISE,

        DIV,
        EXP,

        AND,
        OR,
        NEGATE,
        NOT,
        BNOT,

        GETBASE,

        RETURNBOOL,

        JZS,

        RETURNMACRO,

        MMLT,
        TRANSPOSE,
        CARTESIAN,

        DEFAULT,

        CLOSE = 984
    }

    public enum ExNOT
    {
        DICT,
        ARRAY,
        CLASS,
        RULE
    }

    public enum ArrayAType
    {
        INVALID = -1,
        STACK,
        LITERAL,
        INTEGER,
        FLOAT,
        BOOL
    }

    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExInstr : IDisposable
    {
        public OPC op;
        public ExObject arg0;
        public long arg1;
        public ExObject arg2;
        public ExObject arg3;
        private bool disposedValue;

        public ExInstr() { }
        public string GetDebuggerDisplay()
        {
            if (op == OPC.LINE)
            {
                return "LINE";
            }

            return op.ToString() + ": " + arg0.GetInt() + ", " + arg1 + ", " + arg2.GetInt() + ", " + arg3.GetInt();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    arg0.Dispose();
                    arg2.Dispose();
                    arg3.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~ExInstr()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

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

    public enum ExNewSlotFlag
    {
        ATTR = 0x01,
        STATIC = 0x02
    }
}
