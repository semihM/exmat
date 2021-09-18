namespace ExMat
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
            return !IsNull(obj);
        }

        public static bool IsDelegable(Objects.ExObject obj)
        {
            return ((int)obj.Type & (int)ExObjFlag.DELEGABLE) != 0;
        }

        public static bool IsRealNumber(Objects.ExObject obj)
        {
            return obj.Type is ExObjType.INTEGER or ExObjType.FLOAT;
        }

        public static bool IsNumeric(Objects.ExObject obj)
        {
            return ((int)obj.Type & (int)ExObjFlag.NUMERIC) != 0;
        }

        public static bool IsCountingRefs(Objects.ExObject obj)
        {
            return DoesTypeCountRef(obj.Type);
        }

        public static bool DoesTypeCountRef(ExObjType t)
        {
            return ((int)t & (int)ExObjFlag.COUNTREFERENCES) != 0;
        }

        public static bool IsFalseable(Objects.ExObject obj)
        {
            return ((int)obj.Type & (int)ExObjFlag.CANBEFALSE) != 0;
        }
    }
}
