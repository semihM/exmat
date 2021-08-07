namespace ExMat.Token
{
    public enum TokenType
    {
        // Okuma süreci kontrolü
        STARTERTOKEN = -2,  // Kod dizisi başı temsili
        UNKNOWN,            // Bilinmeyen sembol, hata ifadesi
        ENDLINE,            // Kod dizisi sonu
        ////

        IDENTIFIER = 985,   // Değişken ismi
        ASG,                // Değer atama operatörü    '='

        // Temel veri tipleri
        INTEGER,    // Tamsayı
        FLOAT,      // Ondalıklı
        SCIENTIFIC, // Ondalıklı, bilimsel gösterim
        COMPLEX,    // Kompleks
        LITERAL,    // Yazı dizisi
        SPACE,      // Uzay
        HEX,        // Hexadecimal
        BINARY,     // Binary bits
        ////

        // Aritmetik
        ADD,        // +
        SUB,        // -
        MLT,        // *
        DIV,        // /
        MOD,        // %
        EXP,        // **
        INC,        // ++
        DEC,        // --
        LSHF,       // <<
        RSHF,       // >>
        ////

        // Matris işlemleri
        MATTRANSPOSE,   // '
        MATMLT,         // .*
        CARTESIAN,      // *.*
        ////

        // Mantıksal
        BNOT,       // ~
        BAND,       // &
        BOR,        // |
        BXOR,       // ^
        AND,        // &&
        OR,         // ||
        IN,         // in
        NOTIN,      // not in
        ////

        // Karşılaştırma
        EXC,    // !
        QMARK,  // ?
        LST,    // <
        GRT,    // >
        LET,    // <=
        GET,    // >=
        EQU,    // ==
        NEQ,    // !=
        ////

        // Bileşik aritmetik
        ADDEQ,  // +=
        SUBEQ,  // -=
        MLTEQ,  // *=
        DIVEQ,  // /=
        MODEQ,  // %=
        ////

        // Sabit değerler
        NULL,       // Boş değer                'null'
        TRUE,       // Doğru boolean değeri     'true'
        FALSE,      // Yanlış boolean değeri    'false'
        ////

        // Koşullu veya döngüsel
        IF,         // Koşullu ifade:                               'if'
        ELSE,       // Koşullu ifade:                               'else'
        FOR,        // Döngüsel ifade:                              'for'
        CONTINUE,   // Döngüsel ifadede iterasyonu atlama ifadesi   'continue'
        BREAK,      // Döngüsel ifadede iterasyonu durdurma ifadesi 'break'
        ////

        // Tanım ifadeleri
        VAR,        // Değişken tanımı                      'var'
        FUNCTION,   // Fonksiyon tanımı                     'function'
        CLUSTER,    // Küme ifadesi                         'cluster'
        RULE,       // Kural fonksiyonu                     'rule'
        CLASS,      // Sınıf ifadesi                        'class'
        SEQUENCE,   // Dizi ifadesi                         'seq'
        ////

        // Değer dönen ifadeler
        RETURN,     // Değer dönme ifadesi                          'return'
        DELETE,     // Obje içindeki bir değeri silip dönme ifadesi 'delete'
        TYPEOF,     // Objenin türünü dönme ifadesi                 'typeof'
        INSTANCEOF, // Objenin sınıfa aitliğini inceleme ifadesi    'instanceof'
        ////


        // Sınıflara ait
        CONSTRUCTOR,    // Sınıf inşa edici metot
        THIS,           // İçinde bulunulan objeye erişim
        BASE,           // Ait olunan sınıfa erişim
        ////

        // Diğer
        LAMBDA,     // Lambda ifadesi, isimsiz fonksiyon    '$'
        VARGS,      // Belirsiz sayıda parametre            '...'
        DEFAULT,    // Varsayılan parameter değeri          '..'
        NEWSLOT,    // Sınıfa değer ekleme operatörü        '<>'
        GLOBAL,     // Global bir değişkene erişim          '::'
        NEWLINE,    // \n
        DOT,    // .
        COL,    // :
        SEP,    // ,
        SMC,    // ;

        ROUNDOPEN,      // (
        ROUNDCLOSE,     // )
        CURLYOPEN,      // {
        CURLYCLOSE,     // }
        SQUAREOPEN,     // [
        SQUARECLOSE,    // ]

        COMMENT,    // Yorum        '//' ya da '/**/'

        ELEMENTDEF,    // =>

        MACROSTART,
        MACROEND,

        MACROBLOCK,
        MACROPARAM,

        MACROPARAMNUM,
        MACROPARAMSTR,

        ATTRIBUTEBEGIN,     // Özellik tanımı başlangıcı      '/.'
        ATTRIBUTEFINISH,    // Özellik tanımı bitimi          './'

        ////

    }
}
