using System;
using System.Diagnostics;

namespace ExMat.Objects
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExNativeParamBase : Attribute
    {
        /// <summary>
        /// Parameter index
        /// </summary>
        public int Index;

        /// <summary>
        /// Parameter name
        /// </summary>
        public string Name;

        /// <summary>
        /// Parameter type mask string. See <see cref="API.ExApi.CompileTypeMask(string, System.Collections.Generic.List{int})"/>
        /// </summary>
        public string TypeMask;

        /// <summary>
        /// Parameter information
        /// </summary>
        public string Description;

        /// <summary>
        /// Default value if any
        /// </summary>
        public ExObject DefaultValue;

        public ExNativeParamBase() { }

        public ExNativeParamBase(int idx, string name, string typeMask, string description)
        {
            Index = idx;
            Name = name;
            TypeMask = typeMask;
            Description = description;
        }

        public ExNativeParamBase(int idx, string name, string typeMask, string description, char nullDefault)
        {
            Index = idx;
            Name = name;
            TypeMask = typeMask;
            Description = description;
            DefaultValue = new();
        }

        public ExNativeParamBase(int idx, string name, string typeMask, string description, string def)
        {
            Index = idx;
            Name = name;
            TypeMask = typeMask;
            Description = description;
            if (def == null)
            {
                DefaultValue = new();
            }
            else
            {
                DefaultValue = new(def);
            }
        }

        public ExNativeParamBase(int idx, string name, string typeMask, string description, long def)
        {
            Index = idx;
            Name = name;
            TypeMask = typeMask;
            Description = description;
            DefaultValue = new(def);
        }

        public ExNativeParamBase(int idx, string name, string typeMask, string description, double def)
        {
            Index = idx;
            Name = name;
            TypeMask = typeMask;
            Description = description;
            DefaultValue = new(def);
        }

        public ExNativeParamBase(int idx, string name, string typeMask, string description, bool def)
        {
            Index = idx;
            Name = name;
            TypeMask = typeMask;
            Description = description;
            DefaultValue = new(def);
        }

        private string GetDebuggerDisplay()
        {
            return "ExRegParam: " + Name;
        }
    }
}