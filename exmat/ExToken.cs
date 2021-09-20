namespace ExMat.Token
{
    public enum TokenType
    {
        // Okuma süreci kontrolü
        /// <summary>
        /// Internal, placeholder token to use before starting the lexer
        /// </summary>
        STARTERTOKEN = -2,  // Kod dizisi başı temsili
        /// <summary>
        /// Unknown token
        /// </summary>
        UNKNOWN,            // Bilinmeyen sembol, hata ifadesi
        /// <summary>
        /// End of line, <see cref="ExMat.EndChar"/> value
        /// </summary>
        ENDLINE,            // Kod dizisi sonu
        ////

        /// <summary>
        /// Any [a-zA-Z][a-zA-Z0-9]+ pattern value
        /// </summary>
        IDENTIFIER = 985,   // Değişken ismi
        /// <summary>
        /// '=' character
        /// </summary>
        ASG,                // Değer atama operatörü    '='

        // Temel veri tipleri
        /// <summary>
        /// Integer value
        /// </summary>
        INTEGER,    // Tamsayı
        /// <summary>
        /// Float value
        /// </summary>
        FLOAT,      // Ondalıklı
        /// <summary>
        /// Float value with scientific notation
        /// </summary>
        SCIENTIFIC, // Ondalıklı, bilimsel gösterim
        /// <summary>
        /// Complex number
        /// </summary>
        COMPLEX,    // Kompleks
        /// <summary>
        /// Characters enclosed within "" or $""
        /// </summary>
        LITERAL,    // Yazı dizisi
        /// <summary>
        /// <code>@(Z|R|N|C|E)[+-]?('\\d(\\*\\d)*)?@</code> pattern
        /// </summary>
        SPACE,      // Uzay
        /// <summary>
        /// \x{nn} or \u{nnnn} characters
        /// </summary>
        HEX,        // Hexadecimal
        /// <summary>
        /// 0B... or 0b... characters
        /// </summary>
        BINARY,     // Binary bits
        ////

        // Aritmetik
        /// <summary>
        /// '+' character
        /// </summary>
        ADD,        // +
        /// <summary>
        /// '-' character
        /// </summary>
        SUB,        // -
        /// <summary>
        /// '*' character
        /// </summary>
        MLT,        // *
        /// <summary>
        /// '/' character
        /// </summary>
        DIV,        // /
        /// <summary>
        /// '%' character
        /// </summary>
        MOD,        // %
        /// <summary>
        /// "**" characters
        /// </summary>
        EXP,        // **
        /// <summary>
        /// "++" characters
        /// </summary>
        INC,        // ++
        /// <summary>
        /// "--" characters
        /// </summary>
        DEC,        // --
        /// <summary>
        /// "<<" characters
        /// </summary>
        LSHF,       // <<
        /// <summary>
        /// ">>" characters
        /// </summary>
        RSHF,       // >>
        ////

        // Matris işlemleri
        /// <summary>
        /// ' character
        /// </summary>
        MATTRANSPOSE,   // '
        /// <summary>
        /// ".*" characters
        /// </summary>
        MATMLT,         // .*
        /// <summary>
        /// "*.*" characters
        /// </summary>
        CARTESIAN,      // *.*
        ////

        // Mantıksal
        /// <summary>
        /// '~' character
        /// </summary>
        BNOT,       // ~
        /// <summary>
        /// '&' character
        /// </summary>
        BAND,       // &
        /// <summary>
        /// '|' character
        /// </summary>
        BOR,        // |
        /// <summary>
        /// '^' character
        /// </summary>
        BXOR,       // ^
        /// <summary>
        /// "&&" characters or "and" named keyword 
        /// </summary>
        AND,        // &&
        /// <summary>
        /// "||" characters or"or" named keyword
        /// </summary>
        OR,         // ||
        /// <summary>
        /// "in" named keyword
        /// </summary>
        IN,         // in
        /// <summary>
        /// "not in" combination
        /// </summary>
        NOTIN,      // not in
        ////

        // Karşılaştırma
        /// <summary>
        /// '!' character
        /// </summary>
        EXC,    // !
        /// <summary>
        /// '?' character
        /// </summary>
        QMARK,  // ?
        /// <summary>
        /// '<' character
        /// </summary>
        LST,    // <
        /// <summary>
        /// '>' character
        /// </summary>
        GRT,    // >
        /// <summary>
        /// "<=" characters
        /// </summary>
        LET,    // <=
        /// <summary>
        /// ">=" characters
        /// </summary>
        GET,    // >=
        /// <summary>
        /// "==" characters
        /// </summary>
        EQU,    // ==
        /// <summary>
        /// "!=" characters
        /// </summary>
        NEQ,    // !=
        ////

        // Bileşik aritmetik
        /// <summary>
        /// "+=" characters
        /// </summary>
        ADDEQ,  // +=
        /// <summary>
        /// "-=" characters
        /// </summary>
        SUBEQ,  // -=
        /// <summary>
        /// "*=" characters
        /// </summary>
        MLTEQ,  // *=
        /// <summary>
        /// "/=" characters
        /// </summary>
        DIVEQ,  // /=
        /// <summary>
        /// "%=" characters
        /// </summary>
        MODEQ,  // %=
        ////

        // Sabit değerler
        /// <summary>
        /// "<see cref="ExMat.NullName"/>" named keyword
        /// </summary>
        NULL,       // Boş değer                'null'
        /// <summary>
        /// "true" named keyword
        /// </summary>
        TRUE,       // Doğru boolean değeri     'true'
        /// <summary>
        /// "false" named keyword
        /// </summary>
        FALSE,      // Yanlış boolean değeri    'false'
        ////

        // Koşullu veya döngüsel
        /// <summary>
        /// "if" named keyword
        /// </summary>
        IF,         // Koşullu ifade:                               'if'
        /// <summary>
        /// "else" named keyword
        /// </summary>
        ELSE,       // Koşullu ifade:                               'else'
        /// <summary>
        /// "for" named keyword
        /// </summary>
        FOR,        // Döngüsel ifade:                              'for'
        /// <summary>
        /// "foreach" named keyword
        /// </summary>
        FOREACH,    // Döngüsel ifade:                              'foreach'
        /// <summary>
        /// "continue" named keyword
        /// </summary>
        CONTINUE,   // Döngüsel ifadede iterasyonu atlama ifadesi   'continue'
        /// <summary>
        /// "break" named keyword
        /// </summary>
        BREAK,      // Döngüsel ifadede iterasyonu durdurma ifadesi 'break'
        ////

        // Tanım ifadeleri
        /// <summary>
        /// "var" named keyword
        /// </summary>
        VAR,        // Değişken tanımı                      'var'
        /// <summary>
        /// "const" named keyword
        /// </summary>
        CONST,      // Sabit tanımı                         'const'
        /// <summary>
        /// "function" named keyword
        /// </summary>
        FUNCTION,   // Fonksiyon tanımı                     'function'
        /// <summary>
        /// "cluster" named keyword
        /// </summary>
        CLUSTER,    // Küme ifadesi                         'cluster'
        /// <summary>
        /// "rule" named keyword
        /// </summary>
        RULE,       // Kural fonksiyonu                     'rule'
        /// <summary>
        /// "class" named keyword
        /// </summary>
        CLASS,      // Sınıf ifadesi                        'class'
        /// <summary>
        /// "seq" named keyword
        /// </summary>
        SEQUENCE,   // Dizi ifadesi                         'seq'
        ////

        // Değer dönen ifadeler
        /// <summary>
        /// "return" named keyword
        /// </summary>
        RETURN,
        /// <summary>
        /// "delete" named keyword, for deleting slots
        /// </summary>
        DELETE,
        /// <summary>
        /// "typeof" named keyword, for returning type name strings
        /// </summary>
        TYPEOF,
        /// <summary>
        /// "instanceof" named keyword, for checking if an instance was instanced from a class
        /// <para><c>myInstance instanceof myClass</c></para>
        /// </summary>
        INSTANCEOF,
        ////


        // Sınıflara ait
        /// <summary>
        /// "<see cref="ExMat.ConstructorName"/>" named keyword, class constructor name
        /// </summary>
        CONSTRUCTOR,
        /// <summary>
        /// "<see cref="ExMat.ThisName"/>" named keyword, self reference
        /// </summary>
        THIS,
        /// <summary>
        /// "<see cref="ExMat.BaseName"/>" named keyword, base object/class reference
        /// </summary>
        BASE,
        /// <summary>
        /// "<see cref="ExMat.ReloadName"/>" named keyword, for reloading std libs
        /// </summary>
        RELOAD,
        ////

        // Diğer
        /// <summary>
        /// "$(" characters, lambda functions
        /// </summary>
        LAMBDA,
        /// <summary>
        /// "..." characters, variable arguments
        /// </summary>
        VARGS,
        /// <summary>
        /// ".." characters, parameter's default value 
        /// </summary>
        DEFAULT,
        /// <summary>
        /// "<>" characters, new dictionary slot or class member
        /// </summary>
        NEWSLOT,
        /// <summary>
        /// "::" characters, global reference
        /// </summary>
        GLOBAL,
        /// <summary>
        /// '\n' character
        /// </summary>
        NEWLINE,
        /// <summary>
        /// '.' character
        /// </summary>
        DOT,
        /// <summary>
        /// ':' character
        /// </summary>
        COL,
        /// <summary>
        /// ',' character
        /// </summary>
        SEP,
        /// <summary>
        /// ';' character
        /// </summary>
        SMC,

        /// <summary>
        /// '(' character
        /// </summary>
        ROUNDOPEN,
        /// <summary>
        /// ')' character
        /// </summary>
        ROUNDCLOSE,
        /// <summary>
        /// '{' character
        /// </summary>
        CURLYOPEN,
        /// <summary>
        /// '}' character
        /// </summary>
        CURLYCLOSE,
        /// <summary>
        /// ']' character
        /// </summary>
        SQUAREOPEN,
        /// <summary>
        /// '[' character
        /// </summary>
        SQUARECLOSE,

        /// <summary>
        /// "//" characters for single line comment, "/*" and "*/" characters for block comment
        /// </summary>
        COMMENT,    // Yorum        '//' ya da '/**/'

        /// <summary>
        /// "=>" characters, cluster output element definition
        /// </summary>
        ELEMENTDEF,

        /// <summary>
        /// "/." characters, class member or method attribute block start
        /// </summary>
        ATTRIBUTEBEGIN,
        /// <summary>
        /// "./" characters, class member or method attribute block end
        /// </summary>
        ATTRIBUTEFINISH
    }
}
