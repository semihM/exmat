﻿namespace ExMat
{
    /// <summary>
    /// A class which provides basic type and flag checking methods
    /// </summary>
    public static class ExTypeCheck
    {
        public static bool IsNull(Objects.ExObject obj)
        {
            return obj.Type == ExObjType.NULL;
        }

        public static bool IsNotNull(Objects.ExObject obj)
        {
            return obj.Type != ExObjType.NULL;
        }

        public static bool IsDelegable(Objects.ExObject obj)
        {
            return ((int)obj.Type & (int)ExObjFlag.DELEGABLE) != 0;
        }

        public static bool IsRealNumber(Objects.ExObject obj)
        {
            return obj.Type == ExObjType.INTEGER || obj.Type == ExObjType.FLOAT;
        }

        public static bool IsNumeric(Objects.ExObject obj)
        {
            return ((int)obj.Type & (int)ExObjFlag.NUMERIC) != 0;
        }

        public static bool IsCountingRefs(Objects.ExObject obj)
        {
            return ((int)obj.Type & (int)ExObjFlag.COUNTREFERENCES) != 0;
        }

        public static bool IsFalseable(Objects.ExObject obj)
        {
            return ((int)obj.Type & (int)ExObjFlag.CANBEFALSE) != 0;
        }
    }
}
