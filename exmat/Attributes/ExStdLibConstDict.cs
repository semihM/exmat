using System;

namespace ExMat
{
    /// <summary>
    /// Attribute to register a dictionary property defined in a std lib class
    /// <para>Dictionary must follow <see cref="System.Collections.Dictionary{String,Objects.ExObject}"/> template</para>
    /// <para>Make sure the constants dictionary is defind as a property.</para>
    /// <para>Use <see langword="nameof"/> for best practice of getting the property name</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ExStdLibConstDict : Attribute
    {
        public string Name = string.Empty;

        public ExStdLibConstDict()
        {

        }

        public ExStdLibConstDict(string name)
        {
            Name = name;
        }
    }
}
