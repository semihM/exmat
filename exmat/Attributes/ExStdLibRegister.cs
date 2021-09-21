using System;

namespace ExMat.Attributes
{
    /// <summary>
    /// Attribute to register a main registery method of a standard library
    /// <para>This method is for any extra work that needs to be done for the library's methods</para>
    /// <para>Registery method must be a property defined as a delegate <see cref="ExMat.StdLibRegistery"/></para>
    /// <para>Use <see langword="nameof"/> for best practice of getting the property name</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ExStdLibRegister : Attribute
    {
        /// <summary>
        /// Method name
        /// </summary>
        public string RegisterMethodName;

        /// <summary>
        /// Look for a property with given name as registery method
        /// </summary>
        /// <param name="name">Registery name</param>
        public ExStdLibRegister(string name)
        {
            RegisterMethodName = name;
        }
    }
}
