using System;

namespace ExMat.Attributes
{
    /// <summary>
    /// Attribute to register a dictionary property defined in a std lib class
    /// <para>Dictionary must have <see cref="string"/> keys and <see cref="Objects.ExObject"/> values template</para>
    /// <para>Make sure the constants dictionary is defind as a property.</para>
    /// <para>Use <see langword="nameof"/> for best practice of getting the property name</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ExStdLibConstDict : Attribute
    {
        /// <summary>
        /// Dictionary name
        /// </summary>
        public string Name = string.Empty;

        /// <summary>
        /// Empty constructor
        /// </summary>
        public ExStdLibConstDict()
        {

        }

        /// <summary>
        /// Set name as <paramref name="name"/>
        /// </summary>
        /// <param name="name">Dict name</param>
        public ExStdLibConstDict(string name)
        {
            Name = name;
        }
    }
}
