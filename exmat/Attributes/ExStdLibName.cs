using System;

namespace ExMat
{
    /// <summary>
    /// Attribute to set the name of a standard library
    /// <para>This name is used by the users to refer to this library</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ExStdLibName : Attribute
    {
        public string Name;

        public ExStdLibName(string name)
        {
            Name = name;
        }
    }
}
