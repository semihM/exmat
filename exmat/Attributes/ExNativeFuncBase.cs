using System;

namespace ExMat.Attributes
{
    /// <summary>
    /// Attribute to register a method as a non-delegate native function
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ExNativeFuncBase : Attribute
    {
        /// <summary>
        /// Function name
        /// </summary>
        public string Name;

        /// <summary>
        /// Argument requirement information
        /// <para>If this value is not <see cref="int.MaxValue"/>, <see cref="ExNativeParamBase"/> attributes will be ignored</para>
        /// <para>To use vargs, set this to -1</para>
        /// <para>To use vargs with x amount of parameters, set this to (-1 - x)</para>
        /// <para>Positive 'n': n - 1 parameters == n - 1 arguments</para>
        /// <para>Negative 'n': -n parameters == -n - 1 arguments minimum</para>
        /// </summary>
        public int NumberOfParameters = int.MaxValue;

        /// <summary>
        /// Documentation
        /// </summary>
        public string Description;

        /// <summary>
        /// Base object type, '.' for native functions, other characters for delegates
        /// </summary>
        public char BaseTypeMask = '.';

        /// <summary>
        /// Return type
        /// </summary>
        public ExBaseType Returns = ExBaseType.NULL;

        /// <summary>
        /// Is this a delegate function? Works with <see cref="BaseTypeMask"/> to decide which object type's delegate it will be
        /// </summary>
        public bool IsDelegateFunction;

        /// <summary>
        /// Empty constructor
        /// </summary>
        public ExNativeFuncBase() { }

        /// <summary>
        /// Native function named <paramref name="name"/>, returning <paramref name="returns"/>, described as <paramref name="docs"/>, with <see cref="BaseTypeMask"/> = <c>'.'</c>
        /// </summary>
        /// <param name="name">Function name</param>
        /// <param name="returns">Return type</param>
        /// <param name="docs">Documentation</param>
        public ExNativeFuncBase(string name, ExBaseType returns = ExBaseType.NULL, string docs = "")
        {
            Name = name;
            Description = docs;
            Returns = returns;
            BaseTypeMask = '.';
        }

        /// <summary>
        /// Native function named <paramref name="name"/>, returning <see cref="ExObjType.NULL"/>, described as <paramref name="docs"/>, with <see cref="BaseTypeMask"/> = <c>'.'</c>
        /// </summary>
        /// <param name="name">Function name</param>
        /// <param name="docs">Documentation</param>
        public ExNativeFuncBase(string name, string docs = "")
        {
            Name = name;
            Description = docs;
            Returns = ExBaseType.NULL;
            BaseTypeMask = '.';
        }

        /// <summary>
        /// Native function named <paramref name="name"/>, returning <paramref name="returns"/>, described as <paramref name="docs"/>, with <see cref="BaseTypeMask"/> = <c>'.'</c>
        /// </summary>
        /// <param name="name">Function name</param>
        /// <param name="returns">Return type</param>
        /// <param name="docs">Documentation</param>
        /// <param name="overwriteParamCount">Sets <see cref="NumberOfParameters"/>, see <see cref="NumberOfParameters"/> documentation</param>
        public ExNativeFuncBase(string name, ExBaseType returns, string docs, int overwriteParamCount)
        {
            Name = name;
            NumberOfParameters = overwriteParamCount;
            Description = docs;
            Returns = returns;
            BaseTypeMask = '.';
        }

    }
}
