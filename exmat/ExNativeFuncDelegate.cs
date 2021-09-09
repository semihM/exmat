using System;

namespace ExMat.Objects
{
    /// <summary>
    /// Common delegate native function types, used for template <see cref="ExNativeFuncDelegate(ExCommonDelegateType, char)"/> constructor
    /// </summary>
    public enum ExCommonDelegateType
    {
        /// <summary>
        /// 'weakref' function template
        /// </summary>
        WEAKREF,
        /// <summary>
        /// 'len' function template
        /// </summary>
        LENGTH
    }

    /// <summary>
    /// Native delegate function attribute
    /// <para>This attribute makes connects the method to given <see cref="ExBaseType"/> type objects as a delegate</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class ExNativeFuncDelegate : ExNativeFuncBase
    {
        public ExNativeFuncDelegate() { }

        /// <summary>
        /// Explicitly define a delegate function
        /// </summary>
        /// <param name="name">Name of the native function user will refer this method as</param>
        /// <param name="returns">What type of values this function can return ?</param>
        /// <param name="docs">Explanation for this function's purpose</param>
        /// <param name="basetype">Base object type this function will be a delegate of, refer to <see cref="ExMat.TypeMasks"/> method</param>
        public ExNativeFuncDelegate(string name, ExBaseType returns, string docs, char basetype)
        {
            Name = name;
            NumberOfParameters = int.MaxValue;
            Description = docs;
            Returns = returns;
            BaseTypeMask = basetype;
            IsDelegateFunction = true;
        }

        public ExNativeFuncDelegate(ExCommonDelegateType commonDelegateType, char basetype)
        {
            switch (commonDelegateType)
            {
                case ExCommonDelegateType.LENGTH:
                    {
                        Name = "len";
                        NumberOfParameters = int.MaxValue;
                        Description = "Returns the 'length' of the object";
                        Returns = ExBaseType.INTEGER;
                        BaseTypeMask = basetype;
                        IsDelegateFunction = true;
                        break;
                    }
                case ExCommonDelegateType.WEAKREF:
                    {
                        Name = "weakref";
                        NumberOfParameters = int.MaxValue;
                        Description = "Returns a weak reference of the object";
                        Returns = ExBaseType.WEAKREF;
                        BaseTypeMask = basetype;
                        IsDelegateFunction = true;
                        break;
                    }
            }
        }
    }
}
