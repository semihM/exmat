namespace ExMat
{
    /// <summary>
    /// A class which provides basic type and flag checking methods
    /// </summary>
    public static class ExTypeCheck
    {
        /// <summary>
        /// Return wheter given object is type of <see cref="ExObjType.NULL"/>
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <returns></returns>
        public static bool IsNull(Objects.ExObject obj)
        {
            return obj.Type == ExObjType.NULL;
        }

        /// <summary>
        /// Return wheter given object is not type of <see cref="ExObjType.NULL"/>
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <returns></returns>
        public static bool IsNotNull(Objects.ExObject obj)
        {
            return !IsNull(obj);
        }

        /// <summary>
        /// Return wheter given object has delegates available
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <returns></returns>
        public static bool IsDelegable(Objects.ExObject obj)
        {
            return ((int)obj.Type & (int)ExObjFlag.HASDELEGATES) != 0;
        }

        /// <summary>
        /// Return wheter given object is type of <see cref="ExObjType.INTEGER"/> or <see cref="ExObjType.FLOAT"/>
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <returns></returns>
        public static bool IsRealNumber(Objects.ExObject obj)
        {
            return obj.Type is ExObjType.INTEGER or ExObjType.FLOAT;
        }

        /// <summary>
        /// Return wheter given object is type of <see cref="ExObjType.COMPLEX"/>, <see cref="ExObjType.INTEGER"/> or <see cref="ExObjType.FLOAT"/>
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <returns></returns>
        public static bool IsNumeric(Objects.ExObject obj)
        {
            return ((int)obj.Type & (int)ExObjFlag.NUMERIC) != 0;
        }

        /// <summary>
        /// Return wheter given object is counting references made
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <returns></returns>
        public static bool IsCountingRefs(Objects.ExObject obj)
        {
            return DoesTypeCountRef(obj.Type);
        }

        /// <summary>
        /// Return wheter given type allow counting references
        /// </summary>
        /// <param name="t">Type to check</param>
        /// <returns></returns>
        public static bool DoesTypeCountRef(ExObjType t)
        {
            return ((int)t & (int)ExObjFlag.COUNTREFERENCES) != 0;
        }

        /// <summary>
        /// Return wheter given object can ever be counted as <see langword="false"/> boolean value
        /// </summary>
        /// <param name="obj">Object to check</param>
        /// <returns></returns>
        public static bool IsFalseable(Objects.ExObject obj)
        {
            return ((int)obj.Type & (int)ExObjFlag.CANBEFALSE) != 0;
        }
    }
}
