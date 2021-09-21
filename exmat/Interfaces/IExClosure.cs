namespace ExMat.Interfaces
{
    /// <summary>
    /// Interface for closures
    /// </summary>
    public interface IExClosure
    {
        /// <summary>
        /// Return information about the function meta
        /// </summary>
        /// <param name="attr">Meta attribute name</param>
        /// <returns>Integer(param count, def param count, min required args)
        /// <para>String(function name, description, return info)</para>
        /// <para>Bool(has vargs, is delegate)</para>
        /// <para>Dictionary&lt;string, ExObject>(default param values)</para>
        /// <para>variable(if delegate found)</para>
        /// <para>null(if nothing found)</para></returns>
        public dynamic GetAttribute(string attr);
    }
}
