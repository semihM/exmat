namespace ExMat.OPs
{
    /// <summary>
    /// Bitwise operations
    /// </summary>
    public enum BitOP
    {
        /// <summary>
        /// Bitwise and
        /// </summary>
        AND,
        /// <summary>
        /// Bitwise or
        /// </summary>
        OR = 2,
        /// <summary>
        /// Bitwise xor
        /// </summary>
        XOR,
        /// <summary>
        /// Bit shift left
        /// </summary>
        SHIFTL,
        /// <summary>
        /// Bit shift right
        /// </summary>
        SHIFTR
    }

    /// <summary>
    /// Comparison operations
    /// </summary>
    public enum CmpOP
    {
        /// <summary>
        /// Greater  than
        /// </summary>
        GRT,
        /// <summary>
        /// Greater or equal than
        /// </summary>
        GET = 2,
        /// <summary>
        /// Less than
        /// </summary>
        LST,
        /// <summary>
        /// Less or equal than
        /// </summary>
        LET
    }

    /// <summary>
    /// Operation codes for the VM
    /// </summary>
    public enum ExOperationCode
    {
        /// <summary>
        /// Load integer to stack
        /// </summary>
        LOADINTEGER,    // Tamsayı atama
        /// <summary>
        /// Load float to stack
        /// </summary>
        LOADFLOAT,      // Ondalıklı sayı atama
        /// <summary>
        /// Load boolean to stack
        /// </summary>
        LOADBOOLEAN,    // Boolean atama
        /// <summary>
        /// Load complex number to stack
        /// </summary>
        LOADCOMPLEX,    // Kompleks sayı atama
        /// <summary>
        /// Load space to stack
        /// </summary>
        LOADSPACE,      // Uzay atama
        /// <summary>
        /// Check equal
        /// </summary>
        EQ,             // Eşitlik kontrolü
        /// <summary>
        /// Check not equal
        /// </summary>
        NEQ,            // Eşitsizlik kontrolü
        /// <summary>
        /// Compare
        /// </summary>
        CMP,            // Karşılaştır
        /// <summary>
        /// Conditional jump
        /// </summary>
        JCMP,           // Karşılaştırma koşullu komut atla
        /// <summary>
        /// Jump
        /// </summary>
        JMP,            // Komut atla
        /// <summary>
        /// Conditional jump (on zero)
        /// </summary>
        JZ,             // Koşullu komut atla

        /// <summary>
        /// Call the object
        /// </summary>
        CALL,           // Fonksiyon çağır
        /// <summary>
        /// Prepare stack for a call
        /// </summary>
        PREPCALL,       // Fonksiyon çağırma hazırlığı(bilinen değişken)
        /// <summary>
        /// Prepare stack for a call with outers
        /// </summary>
        PREPCALLK,      // Fonksiyon çağırma hazırlığı(dışardan değişken)
        /// <summary>
        /// Tail call
        /// </summary>
        CALLTAIL,       // Fonksiyon sonucunu çağır

        /// <summary>
        /// Load literal to stack
        /// </summary>
        LOAD,           // Yazı dizisi atama(direkt)
        /// <summary>
        /// Double load literal to stack
        /// </summary>
        DLOAD,          // Yazı dizisi atama(ifade işlenmeli)

        /// <summary>
        /// Get known variable or member
        /// </summary>
        GET,            // Bilinen değişken değeri
        /// <summary>
        /// Get unknown variable or member
        /// </summary>
        GETK,           // Dışardan değişken değeri

        /// <summary>
        /// Move object in stack
        /// </summary>
        MOVE,           // Değerin bellekteki yerini değiştir(direkt)
        /// <summary>
        /// Double move objects in stack
        /// </summary>
        DMOVE,          // Değerin bellekteki yerini değiştir(ifade işlenmeli)

        /// <summary>
        /// Call object's setter
        /// </summary>
        SET,            // Global değişkene, metota, özelliğe değer atama

        /// <summary>
        /// Delete a slot from object
        /// </summary>
        DELETE,         // Değerin denkini sil

        /// <summary>
        /// Create a closure
        /// </summary>
        CLOSURE,        // Fonksiyon oluştur

        /// <summary>
        /// Check existance of value in another
        /// </summary>
        EXISTS,         // Objeye aitlik kontrolü
        /// <summary>
        /// Instancing check
        /// </summary>
        INSTANCEOF,     // Sınıfa aitlik kontrolü
        /// <summary>
        /// Get type of object
        /// </summary>
        TYPEOF,         // Obje tipi
        /// <summary>
        /// Return object from frame
        /// </summary>
        RETURN,         // Değer dön

        /// <summary>
        /// Append value to a list
        /// </summary>
        APPENDTOARRAY,  // Liste sonuna ekle

        /// <summary>
        /// Nullify object in stack
        /// </summary>
        LOADNULL,       // Boş değer ata
        /// <summary>
        /// Push root table to stack
        /// </summary>
        LOADROOT,       // Temel kütüphaneyi yükle

        /// <summary>
        /// Set outer value
        /// </summary>
        SETOUTER,       // Dışardaki değişkenin değerini değiştir
        /// <summary>
        /// Get outer value
        /// </summary>
        GETOUTER,       // Dışardan değişken bul

        /// <summary>
        /// Create new object
        /// </summary>
        NEWOBJECT,      // Yeni obje oluştur
        /// <summary>
        /// Create a new slot in an object
        /// </summary>
        NEWSLOT,        // Yeni obje özelliği oluştur
        /// <summary>
        /// Create an attribute
        /// </summary>
        NEWSLOTA,       // Sınıf elemanına ek özellik oluştur

        /// <summary>
        /// Compound arithmetic
        /// </summary>
        COMPOUNDARITH,  // Bileşik aritmetik işlem

        /// <summary>
        /// Modulo opertaion
        /// </summary>
        MOD,            // Modulo işlemi

        /// <summary>
        /// Increment, no return
        /// </summary>
        INC,            // Tipi belirsiz değeri 1 arttır veya azalt(sonucu dönme)
        /// <summary>
        /// Increment and return
        /// </summary>
        PINC,           // Tipi belirsiz değeri 1 arttır veya azalt(sonucu dön)
        /// <summary>
        /// Increment variable, no return
        /// </summary>
        INCL,           // Değişken değerini 1 arttır veya azalt(sonucu dönme)
        /// <summary>
        /// Increment variable and return
        /// </summary>
        PINCL,          // Değişken değerini 1 arttır veya azalt(sonucu dön)

        /// <summary>
        /// Multiplication operation
        /// </summary>
        MLT,            // Çarpım işlemi
        /// <summary>
        /// Addition operation
        /// </summary>
        ADD,            // Toplama işlemi

        /// <summary>
        /// Line tracking
        /// </summary>
        LINE,           // Hata takibine yardımcı

        /// <summary>
        /// Subtraction operation
        /// </summary>
        SUB,            // Çıkarma işlemi

        /// <summary>
        /// Bitwise operation
        /// </summary>
        BITWISE,

        /// <summary>
        /// Division operation
        /// </summary>
        DIV,
        /// <summary>
        /// Exponential operation
        /// </summary>
        EXP,

        /// <summary>
        /// Logic and operation
        /// </summary>
        AND,
        /// <summary>
        /// Logic or operation
        /// </summary>
        OR,
        /// <summary>
        /// Negate object
        /// </summary>
        NEGATE,
        /// <summary>
        /// Logic not operation
        /// </summary>
        NOT,
        /// <summary>
        /// Bitwise not operation
        /// </summary>
        BNOT,

        /// <summary>
        /// Get base object
        /// </summary>
        GETBASE,

        /// <summary>
        /// Return value as boolean
        /// </summary>
        RETURNBOOL,

        /// <summary>
        /// Matrix multiplication
        /// </summary>
        MMLT,
        /// <summary>
        /// Transpose operation
        /// </summary>
        TRANSPOSE,
        /// <summary>
        /// Cartesian product
        /// </summary>
        CARTESIAN,
        /// <summary>
        /// Load parameter's default value
        /// </summary>
        DEFAULT,

        /// <summary>
        /// Load constants dictionary to stack
        /// </summary>
        LOADCONSTDICT,

        /// <summary>
        /// Reload a standard library
        /// </summary>
        RELOADLIB,

        /// <summary>
        /// Foreach loop
        /// </summary>
        FOREACH,

        /// <summary>
        /// Post foreach for generators
        /// </summary>
        POSTFOREACH,

        /// <summary>
        /// Outer variable control end
        /// </summary>
        CLOSE = 984
    }
    /// <summary>
    /// New object type
    /// </summary>
    public enum ExNewObjectType
    {
        /// <summary>
        /// Dictionary
        /// </summary>
        DICT,
        /// <summary>
        /// List
        /// </summary>
        ARRAY,
        /// <summary>
        /// Class
        /// </summary>
        CLASS,
        /// <summary>
        /// Rule
        /// </summary>
        RULE
    }

    /// <summary>
    /// Array value append type
    /// </summary>
    public enum ArrayAType
    {
        /// <summary>
        /// Unknown
        /// </summary>
        INVALID = -1,
        /// <summary>
        /// From stack
        /// </summary>
        STACK,
        /// <summary>
        /// String
        /// </summary>
        LITERAL,
        /// <summary>
        /// Integer
        /// </summary>
        INTEGER,
        /// <summary>
        /// Float
        /// </summary>
        FLOAT,
        /// <summary>
        /// Boolean
        /// </summary>
        BOOL
    }

    /// <summary>
    /// New slot operation flags
    /// </summary>
    public enum ExNewSlotFlag
    {
        /// <summary>
        /// Is attribute ?
        /// </summary>
        ATTR = 0x01,
        /// <summary>
        /// Is static ?
        /// </summary>
        STATIC = 0x02
    }
}
