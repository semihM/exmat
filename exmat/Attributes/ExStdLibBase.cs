using System;

namespace ExMat
{
    /// <summary>
    /// Types for standard libraries
    /// </summary>
    public enum ExStdLibType
    {
        /// <summary>
        /// External custom library
        /// </summary>
        EXTERNAL,
        /// <summary>
        /// Base library
        /// </summary>
        BASE,
        /// <summary>
        /// Math library
        /// </summary>
        MATH,
        /// <summary>
        /// Input-output library
        /// </summary>
        IO,
        /// <summary>
        /// String library
        /// </summary>
        STRING,
        /// <summary>
        /// Networking library
        /// </summary>
        NETWORK,
        /// <summary>
        /// System library
        /// </summary>
        SYSTEM,
        /// <summary>
        /// Statistics library
        /// </summary>
        STATISTICS
    }

    /// <summary>
    /// Attribute to mark a class as a standard library of given <see cref="ExStdLibType"/> type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ExStdLibBase : Attribute
    {
        public ExStdLibType Type;

        public ExStdLibBase(ExStdLibType type)
        {
            Type = type;
        }
    }
}
