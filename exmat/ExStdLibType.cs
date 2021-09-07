using System;

namespace ExMat
{
    /// <summary>
    /// Types for standard libraries
    /// </summary>
    public enum ExStdLibType
    {
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
        SYSTEM
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ExStdLib : Attribute
    {
        public ExStdLibType Type;

        public ExStdLib(ExStdLibType type)
        {
            Type = type;
        }
    }
}
