namespace ExMat.Token
{
    public enum TokenType
    {
        UNKNOWN = -1,
        ENDLINE,

        /// <summary>
        /// Name
        /// </summary>
        IDENTIFIER = 985,

        /// <summary>
        /// No token
        /// </summary>
        NONE,

        /// <summary>
        /// local
        /// </summary>
        VAR,
        BASE,
        CONSTRUCTOR,

        LITERAL,
        NEWLINE,

        INTEGER,
        FLOAT,
        SCI,
        COMPLEX,

        // #
        COMMENT,

        NULL,
        IF,
        ELSE,
        RULE,
        FOR,
        FOREACH,
        WHILE,
        CONTINUE,
        BREAK,
        FUNCTION,
        RETURN,
        SUM,
        MUL,

        THIS,
        CLASS,
        DELETE,

        A_START,
        A_END,

        TRUE,
        FALSE,

        LSHF,   // <<
        RSHF,   // >>

        R_OPEN, // (
        R_CLOSE,// )
        ADD,    // +
        SUB,    // -
        MLT,    // *
        DIV,    // /
        EXP,    // '
        MOD,    // %
        ASG,    // =
        COL,    // :
        EXC,    // !
        QMARK,  // ?
        TIL,    // ~
        BAND,    // &
        BOR,     // |
        BXOR,    // ^
        AND,    // &&
        OR,     // ||
        XOR,    // ^^

        INC,    // ++
        DEC,    // --

        ADDEQ,  // +=
        SUBEQ,  // -=
        MLTEQ,  // *=
        DIVEQ,  // /=
        MODEQ,  // %=

        GLB,    // ::
        LST,    // <
        GRT,    // >
        LET,    // <=
        GET,    // >=
        EQU,    // ==
        NEQ,    // !=
        DOT,    // .
        MMUL,   // .*
        MCRS,   // .'
        SEP,    // ,
        SMC,  // ;
        CLS_OPEN,   // {
        CLS_CLOSE,  // }
        ARR_OPEN,   // [
        ARR_CLOSE,   // ]

        TYPEOF, // typeof
        INSTANCEOF, // instanceof
        IN,     // in

        NEWSLOT,    // <>

        CLUSTER,    // cluster
        ELEMENT_DEF,    // =>
        SPACE,  // @

        LAMBDA, // $

        MACROSTART,
        MACROEND,

        MACROBLOCK,
        MACROPARAM,

        MACROPARAM_NUM,
        MACROPARAM_STR,

        MMLT,    // .*
        MTRS,    // '

        DEFAULT, // ..
        CARTESIAN, // *.*

        SEQUENCE,   // seq
        VARGS
    }
}
