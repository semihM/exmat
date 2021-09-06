using System;
using System.Collections.Generic;

namespace ExMat.Objects
{
    /// <summary>
    /// Function parameter class
    /// </summary>
    public class ExFuncParameter : IDisposable
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
        /// Parameter information
        /// </summary>
        public string Description;

        /// <summary>
        /// Parameter type mask string. See <see cref="API.ExApi.CompileTypeMask(string, System.Collections.Generic.List{int})"/>
        /// </summary>
        public string TypeMask;

        /// <summary>
        /// Parameter type mask using <see cref="ExObjType"/> combinations
        /// </summary>
        public int Type;

        /// <summary>
        /// Parameter type mask info string using <see cref="ExObjType"/> combinations
        /// </summary>
        public string TypeString;

        /// <summary>
        /// Wheter this parameter has a default value
        /// </summary>
        public bool HasDefaultValue => DefaultValue != null;

        /// <summary>
        /// Default value if any
        /// </summary>
        public ExObject DefaultValue;

        /// <summary>
        /// Wheter this parameter is a valid parameter
        /// </summary>
        public bool Valid = false;

        private bool disposedValue;

        public ExFuncParameter() { }

        public ExFuncParameter(string name, string type = ".", string info = "", ExObject def = null, int idx = 0)
        {
            List<int> types = new(1);
            if (!API.ExApi.CompileTypeMask(type, types) || types.Count != 1)
            {
                Valid = false;
            }
            else
            {
                Index = idx;
                Name = name;
                Description = info;

                TypeMask = type;
                Type = types[0];
                TypeString = API.ExApi.GetTypeMaskInfo(Type);

                DefaultValue = def;

                Valid = true;
            }
        }

        public static ExFuncParameter Create(string name, string type = ".", string info = "", ExObject def = null, int idx = 0)
        {
            ExFuncParameter fp = new(name, type, info, def, idx);
            return fp.Valid ? fp : null;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Disposer.DisposeObjects(DefaultValue);
                    Name = null;
                    Description = null;
                    TypeString = null;
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