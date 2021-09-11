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

    public enum ExOperationCode
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

        LOADCONSTDICT,
        RELOADLIB,

        CLOSE = 984
    }

    public enum ExNewObjectType
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

    public enum ExNewSlotFlag
    {
        ATTR = 0x01,
        STATIC = 0x02
    }
}
