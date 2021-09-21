using System;
using System.Collections.Generic;
using ExMat.Attributes;

namespace ExMat.Objects
{
    /// <summary>
    /// Native function parameter class
    /// </summary>
    internal class ExNativeParam : ExNativeParamBase, IDisposable
    {
        /// <summary>
        /// Parameter type mask using <see cref="ExObjType"/> combinations
        /// </summary>
        public int Type;

        /// <summary>
        /// Parameter type mask info string using <see cref="ExObjType"/> combinations
        /// </summary>
        public string TypeMaskString;

        /// <summary>
        /// Wheter this parameter has a default value
        /// </summary>
        public bool HasDefaultValue => DefaultValue != null;

        /// <summary>
        /// Wheter this parameter is a valid parameter
        /// </summary>
        public bool Valid;

        private bool disposedValue;

        /// <summary>
        /// Empty constructor
        /// </summary>
        public ExNativeParam() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        public ExNativeParam(ExNativeParamBase ex)
        {
            Initialize(ex.Name, ex.TypeMask, ex.Description, ex.DefaultValue, ex.Index);
        }

        private void Initialize(string name, string type = ".", string info = "", ExObject def = null, int idx = 0)
        {
            List<int> types = new(1);
            if (API.ExApi.CompileTypeMask(type, types) || types.Count != 1)
            {
                Index = idx;
                Name = name;
                Description = info;

                TypeMask = type;
                Type = types[0];
                TypeMaskString = API.ExApi.GetTypeMaskInfo(Type);

                DefaultValue = def;

                Valid = true;
            }
        }
        public ExNativeParam(string name, string type = ".", string info = "", ExObject def = null, int idx = 0)
        {
            Initialize(name, type, info, def, idx);
        }

        public static ExNativeParam Create(string name, string type = ".", string info = "", ExObject def = null, int idx = 0)
        {
            ExNativeParam fp = new(name, type, info, def, idx);
            return fp.Valid ? fp : null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ExDisposer.DisposeObjects(DefaultValue);
                    Name = null;
                    Description = null;
                    TypeMaskString = null;
                    TypeMask = null;
                    DefaultValue = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}