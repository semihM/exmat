namespace ExMat.Interfaces
{
    public interface IExClosureAttr
    {
        /// <summary>
        /// Return information about the function meta
        /// </summary>
        /// <param name="attr">Meta attribute name</param>
        /// <returns>Integer(param count, def param count, min required args)
        /// <para>String(function name)</para>
        /// <para>Bool(has vargs, is delegate)</para>
        /// <para>Dictionary<string, ExObject>(default param values)</para>
        /// <para>variable(if delegate found)</para>
        /// <para>null(if nothing found)</para></returns>
        public dynamic GetAttribute(string attr);
    }
}
