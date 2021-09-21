using System;

namespace ExMat.Attributes
{
    /// <summary>
    /// Attribute to mark a class as a standard library of given <see cref="ExStdLibType"/> type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ExStdLibBase : Attribute
    {
        /// <summary>
        /// Library type
        /// </summary>
        public ExStdLibType Type;

        /// <summary>
        /// Constructor for library of type <paramref name="type"/>
        /// </summary>
        /// <param name="type">Library type</param>
        public ExStdLibBase(ExStdLibType type)
        {
            Type = type;
        }
    }
}
