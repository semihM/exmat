﻿namespace ExMat.Token
{
    public enum OperatorAssociativity
    {
        LEFT,
        RIGHT
    };

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

        TYPEOF,
        INSTANCEOF,
        IN,

        NEWSLOT

    }

    public class ExToken
    {
        public TokenType e_type = TokenType.NULL;

        public dynamic d_value;

        public OperatorAssociativity e_assoc = OperatorAssociativity.LEFT;

        public ExToken() { }

        public ExToken(TokenType type, dynamic value, OperatorAssociativity assoc = OperatorAssociativity.LEFT)
        {
            e_type = type;
            d_value = value;
            e_assoc = assoc;
        }

    }
}