using System;

namespace ExMat.Attributes
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
        /// <summary>
        /// Initialize <see cref="ExNativeFuncBase"/> as delegate
        /// </summary>
        public ExNativeFuncDelegate()
        {
            IsDelegateFunction = true;
        }

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
            Description = docs;
            Returns = returns;
            BaseTypeMask = basetype;
            IsDelegateFunction = true;
        }

        /// <summary>
        /// Construct delegate  from common delegate types
        /// </summary>
        /// <param name="commonDelegateType">Template type</param>
        /// <param name="basetype">Delegate base type mask</param>
        public ExNativeFuncDelegate(ExCommonDelegateType commonDelegateType, char basetype)
        {
            IsDelegateFunction = true;
            BaseTypeMask = basetype;
            Init(this, commonDelegateType);
        }

        /// <summary>
        /// Construct delegate  from common delegate types
        /// </summary>
        /// <param name="commonDelegateType">Template type</param>
        /// <param name="basetype">Delegate base type mask</param>
        public ExNativeFuncDelegate(ExCommonDelegateType commonDelegateType, ExBaseType basetype)
        {
            string mask = API.ExApi.DecompileTypeMaskChar(basetype);
            BaseTypeMask = string.IsNullOrWhiteSpace(mask) ? '.' : mask[0];

            IsDelegateFunction = true;

            Init(this, commonDelegateType);
        }

        private static void Init(ExNativeFuncDelegate attr, ExCommonDelegateType commonDelegateType)
        {
            switch (commonDelegateType)
            {
                case ExCommonDelegateType.LENGTH:
                    {
                        attr.Name = "len";
                        attr.Returns = ExBaseType.INTEGER;
                        attr.Description = "Returns the 'length' of the object";
                        break;
                    }
                case ExCommonDelegateType.WEAKREF:
                    {
                        attr.Name = "weakref";
                        attr.Returns = ExBaseType.WEAKREF;
                        attr.Description = "Returns a weak reference of the object";
                        break;
                    }
            }
        }
    }
}
