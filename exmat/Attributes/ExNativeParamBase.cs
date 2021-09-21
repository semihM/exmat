using System;
using ExMat.Objects;
#if DEBUG
using System.Diagnostics;
#endif

namespace ExMat.Attributes
{
    /// <summary>
    /// Attribute to register a native function parameter
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
#if DEBUG
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
#endif
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
        /// Parameter type mask string. See <see cref="ExMat.TypeMasks"/>
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

        /// <summary>
        /// Empty constructor
        /// </summary>
        public ExNativeParamBase() { }

        /// <summary>
        /// Parameter no #<paramref name="idx"/>, named <paramref name="name"/>, accepting types <paramref name="typeMask"/>, described as <paramref name="description"/>, no default value
        /// </summary>
        /// <param name="idx">Parameter no</param>
        /// <param name="name">Parameter name</param>
        /// <param name="typeMask">Parameter type mask, see <see cref="ExMat.TypeMasks"/> docs</param>
        /// <param name="description">Parameter description</param>
        public ExNativeParamBase(int idx, string name, string typeMask, string description)
        {
            Index = idx;
            Name = name;
            TypeMask = typeMask;
            Description = description;
        }

        /// <summary>
        /// Parameter no #<paramref name="idx"/>, named <paramref name="name"/>, accepting types <paramref name="typeMask"/>, described as <paramref name="description"/>, default value <see cref="ExObjType.NULL"/>
        /// <paramref name="nullDefault"/> value doesn't matter!
        /// </summary>
        /// <param name="idx">Parameter no</param>
        /// <param name="name">Parameter name</param>
        /// <param name="typeMask">Parameter type mask, see <see cref="ExMat.TypeMasks"/> docsoperator</param>
        /// <param name="description">Parameter description</param>
        /// <param name="nullDefault">Any character, <see cref="ExObjType.NULL"/> is used as default value in any case</param>
        public ExNativeParamBase(int idx, string name, string typeMask, string description, char nullDefault)
        {
            Index = idx;
            Name = name;
            TypeMask = typeMask;
            Description = description;
            DefaultValue = new();
        }

        /// <summary>
        /// Parameter no #<paramref name="idx"/>, named <paramref name="name"/>, accepting types <paramref name="typeMask"/>, described as <paramref name="description"/>, default value <see cref="ExObjType.STRING"/> <paramref name="def"/>
        /// </summary>
        /// <param name="idx">Parameter no</param>
        /// <param name="name">Parameter name</param>
        /// <param name="typeMask">Parameter type mask, see <see cref="ExMat.TypeMasks"/> docs</param>
        /// <param name="description">Parameter description</param>
        /// <param name="def">String default value</param>
        public ExNativeParamBase(int idx, string name, string typeMask, string description, string def)
        {
            Index = idx;
            Name = name;
            TypeMask = typeMask;
            Description = description;
            DefaultValue = def == null ? (new()) : (new(def));
        }

        /// <summary>
        /// Parameter no #<paramref name="idx"/>, named <paramref name="name"/>, accepting types <paramref name="typeMask"/>, described as <paramref name="description"/>, default value <see cref="ExObjType.INTEGER"/> <paramref name="def"/>
        /// </summary>
        /// <param name="idx">Parameter no</param>
        /// <param name="name">Parameter name</param>
        /// <param name="typeMask">Parameter type mask, see <see cref="ExMat.TypeMasks"/> docs</param>
        /// <param name="description">Parameter description</param>
        /// <param name="def">Integer default value</param>
        public ExNativeParamBase(int idx, string name, string typeMask, string description, long def)
        {
            Index = idx;
            Name = name;
            TypeMask = typeMask;
            Description = description;
            DefaultValue = new(def);
        }

        /// <summary>
        /// Parameter no #<paramref name="idx"/>, named <paramref name="name"/>, accepting types <paramref name="typeMask"/>, described as <paramref name="description"/>, default value <see cref="ExObjType.FLOAT"/> <paramref name="def"/>
        /// </summary>
        /// <param name="idx">Parameter no</param>
        /// <param name="name">Parameter name</param>
        /// <param name="typeMask">Parameter type mask, see <see cref="ExMat.TypeMasks"/> docs</param>
        /// <param name="description">Parameter description</param>
        /// <param name="def">Float default value</param>
        public ExNativeParamBase(int idx, string name, string typeMask, string description, double def)
        {
            Index = idx;
            Name = name;
            TypeMask = typeMask;
            Description = description;
            DefaultValue = new(def);
        }

        /// <summary>
        /// Parameter no #<paramref name="idx"/>, named <paramref name="name"/>, accepting types <paramref name="typeMask"/>, described as <paramref name="description"/>, default value <see cref="ExObjType.BOOL"/> <paramref name="def"/>
        /// </summary>
        /// <param name="idx">Parameter no</param>
        /// <param name="name">Parameter name</param>
        /// <param name="typeMask">Parameter type mask, see <see cref="ExMat.TypeMasks"/> docs</param>
        /// <param name="description">Parameter description</param>
        /// <param name="def">Boolean default value</param>
        public ExNativeParamBase(int idx, string name, string typeMask, string description, bool def)
        {
            Index = idx;
            Name = name;
            TypeMask = typeMask;
            Description = description;
            DefaultValue = new(def);
        }

        /// <summary>
        /// Parameter no #<paramref name="idx"/>, named <paramref name="name"/>, accepting types <paramref name="typeMask"/>, described as <paramref name="description"/>, no default value
        /// </summary>
        /// <param name="idx">Parameter no</param>
        /// <param name="name">Parameter name</param>
        /// <param name="typeMask">Parameter type mask, allows combinations with <c>|</c> operator</param>
        /// <param name="description">Parameter description</param>
        public ExNativeParamBase(int idx, string name, ExBaseType typeMask, string description)
        {
            Index = idx;
            Name = name;
            TypeMask = API.ExApi.DecompileTypeMaskChar(typeMask);
            Description = description;
        }

        /// <summary>
        /// Parameter no #<paramref name="idx"/>, named <paramref name="name"/>, accepting types <paramref name="typeMask"/>, described as <paramref name="description"/>, default value <see cref="ExObjType.NULL"/>
        /// <paramref name="nullDefault"/> value doesn't matter!
        /// </summary>
        /// <param name="idx">Parameter no</param>
        /// <param name="name">Parameter name</param>
        /// <param name="typeMask">Parameter type mask, allows combinations with <c>|</c> operator</param>
        /// <param name="description">Parameter description</param>
        /// <param name="nullDefault">Any character, <see cref="ExObjType.NULL"/> is used as default value in any case</param>
        public ExNativeParamBase(int idx, string name, ExBaseType typeMask, string description, char nullDefault)
        {
            Index = idx;
            Name = name;
            TypeMask = API.ExApi.DecompileTypeMaskChar(typeMask);
            Description = description;
            DefaultValue = new();
        }

        /// <summary>
        /// Parameter no #<paramref name="idx"/>, named <paramref name="name"/>, accepting types <paramref name="typeMask"/>, described as <paramref name="description"/>, default value <see cref="ExObjType.STRING"/> <paramref name="def"/>
        /// </summary>
        /// <param name="idx">Parameter no</param>
        /// <param name="name">Parameter name</param>
        /// <param name="typeMask">Parameter type mask, allows combinations with <c>|</c> operator</param>
        /// <param name="description">Parameter description</param>
        /// <param name="def">String default value</param>
        public ExNativeParamBase(int idx, string name, ExBaseType typeMask, string description, string def)
        {
            Index = idx;
            Name = name;
            TypeMask = API.ExApi.DecompileTypeMaskChar(typeMask);
            Description = description;
            DefaultValue = def == null ? (new()) : (new(def));
        }

        /// <summary>
        /// Parameter no #<paramref name="idx"/>, named <paramref name="name"/>, accepting types <paramref name="typeMask"/>, described as <paramref name="description"/>, default value <see cref="ExObjType.INTEGER"/> <paramref name="def"/>
        /// </summary>
        /// <param name="idx">Parameter no</param>
        /// <param name="name">Parameter name</param>
        /// <param name="typeMask">Parameter type mask, allows combinations with <c>|</c> operator</param>
        /// <param name="description">Parameter description</param>
        /// <param name="def">Integer default value</param>
        public ExNativeParamBase(int idx, string name, ExBaseType typeMask, string description, long def)
        {
            Index = idx;
            Name = name;
            TypeMask = API.ExApi.DecompileTypeMaskChar(typeMask);
            Description = description;
            DefaultValue = new(def);
        }

        /// <summary>
        /// Parameter no #<paramref name="idx"/>, named <paramref name="name"/>, accepting types <paramref name="typeMask"/>, described as <paramref name="description"/>, default value <see cref="ExObjType.FLOAT"/> <paramref name="def"/>
        /// </summary>
        /// <param name="idx">Parameter no</param>
        /// <param name="name">Parameter name</param>
        /// <param name="typeMask">Parameter type mask, allows combinations with <c>|</c> operator</param>
        /// <param name="description">Parameter description</param>
        /// <param name="def">Float default value</param>
        public ExNativeParamBase(int idx, string name, ExBaseType typeMask, string description, double def)
        {
            Index = idx;
            Name = name;
            TypeMask = API.ExApi.DecompileTypeMaskChar(typeMask);
            Description = description;
            DefaultValue = new(def);
        }

        /// <summary>
        /// Parameter no #<paramref name="idx"/>, named <paramref name="name"/>, accepting types <paramref name="typeMask"/>, described as <paramref name="description"/>, default value <see cref="ExObjType.BOOL"/> <paramref name="def"/>
        /// </summary>
        /// <param name="idx">Parameter no</param>
        /// <param name="name">Parameter name</param>
        /// <param name="typeMask">Parameter type mask, allows combinations with <c>|</c> operator</param>
        /// <param name="description">Parameter description</param>
        /// <param name="def">Boolean default value</param>
        public ExNativeParamBase(int idx, string name, ExBaseType typeMask, string description, bool def)
        {
            Index = idx;
            Name = name;
            TypeMask = API.ExApi.DecompileTypeMaskChar(typeMask);
            Description = description;
            DefaultValue = new(def);
        }

#if DEBUG
        private string GetDebuggerDisplay()
        {
            return "ExRegParam: " + Name;
        }
#endif
    }
}