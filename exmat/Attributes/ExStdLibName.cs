using System;

namespace ExMat.Attributes
{
    /// <summary>
    /// Attribute to set the name of a standard library
    /// <para>This name is used by the users to refer to this library</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ExStdLibName : Attribute
    {
        /// <summary>
        /// Library name
        /// </summary>
        public string Name;

        /// <summary>
        /// Mark as library with given name
        /// </summary>
        /// <param name="name">Library name</param>
        public ExStdLibName(string name)
        {
            Name = name;
        }
    }
}
