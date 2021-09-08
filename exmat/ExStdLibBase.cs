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
        SYSTEM
    }

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
