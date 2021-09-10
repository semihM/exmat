using System;

namespace ExMat.Objects
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ExNativeFuncBase : Attribute
    {
        /// <summary>
        /// Function name
        /// </summary>
        public string Name;

        /// <summary>
        /// Argument requirement information
        /// <para>Positive 'n': n - 1 parameters == n - 1 arguments</para>
        /// <para>Negative 'n': -n parameters == -n - 1 arguments minimum</para>
        /// <para>Setter should only be used for vargs functions with parameter definitions</para>
        /// </summary>
        public int NumberOfParameters = int.MaxValue;

        /// <summary>
        /// Documentation
        /// </summary>
        public string Description;

        public char BaseTypeMask = '.';

        public ExBaseType Returns = ExBaseType.NULL;

        /// <summary>
        /// Is this a delegate function?
        /// </summary>
        public bool IsDelegateFunction;

        public ExNativeFuncBase() { }

        public ExNativeFuncBase(string name, ExBaseType returns = ExBaseType.NULL, string docs = "")
        {
            Name = name;
            Description = docs;
            Returns = returns;
            BaseTypeMask = '.';
        }

        public ExNativeFuncBase(string name, string docs = "")
        {
            Name = name;
            Description = docs;
            Returns = ExBaseType.NULL;
            BaseTypeMask = '.';
        }

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
