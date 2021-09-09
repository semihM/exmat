using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ExMat.Objects
{
    /// <summary>
    /// Native function registery class
    /// </summary>
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class ExNativeFunc : ExNativeFuncBase, IDisposable
    {
        /// <summary>
        /// Library of which this function is a part of 
        /// </summary>
        public ExStdLibType Base;

        /// <summary>
        /// Function reference
        /// </summary>
        public ExMat.StdLibFunction Function;    // Fonksiyon referansı

        /// <summary>
        /// Parameter information
        /// </summary>
        public List<ExNativeParam> Parameters;

        /// <summary>
        /// Combined parameter type mask string
        /// </summary>
        public string ParameterMask;

        private void SetNumberOfParameterCombinedMask()
        {
            if (Parameters == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(ParameterMask))
            {
                StringBuilder pmask = new(BaseTypeMask.ToString());
                foreach (ExNativeParam p in Parameters)
                {
                    pmask.Append(p.TypeMask);
                }
                ParameterMask = pmask.ToString();
            }
        }

        private void SetNumberOfParameters()
        {
            if (NumberOfParameters == int.MaxValue)
            {
                if (Parameters == null)
                {
                    NumberOfParameters = BaseTypeMask == '.' ? -1 : 1;
                }
                else
                {
                    NumberOfParameters = 1;
                    foreach (ExNativeParam fp in Parameters)
                    {
                        if (fp.HasDefaultValue)
                        {
                            NumberOfParameters *= -1;
                            break;
                        }
                        NumberOfParameters++;
                    }
                }
            }
        }

        /// <summary>
        /// Information string about return type
        /// </summary>
        public string ReturnsType;

        private void SetReturnInfo()
        {
            ReturnsType = API.ExApi.GetTypeMaskInfo((int)Returns);
        }

        private bool disposedValue;

        private void InitializeFromNativeBase(ExNativeFuncBase ex)
        {
            Name = ex.Name;
            NumberOfParameters = ex.NumberOfParameters;
            Description = ex.Description;
            Returns = ex.Returns;
            BaseTypeMask = ex.BaseTypeMask;
            IsDelegateFunction = ex.IsDelegateFunction;
        }

        public ExNativeFunc() { }

        public ExNativeFunc(ExMat.StdLibFunction foo)
        {
            Function = foo;

            Parameters = new();

            foreach (Attribute attr in GetCustomAttributes(foo.Method))
            {
                if (attr is ExNativeParamBase exNativeParam)
                {
                    Parameters.Add(new(exNativeParam));
                }
                else if (attr is ExNativeFuncBase exNative)
                {
                    InitializeFromNativeBase(exNative);
                }
            }

            if (NumberOfParameters != int.MaxValue)
            {
                Parameters = null;
            }
            else
            {
                Parameters = new(Parameters.OrderBy(x => x.Index));
            }

            SetNumberOfParameters();
            SetNumberOfParameterCombinedMask();
            SetReturnInfo();
        }

        public ExNativeFunc(ExMat.StdLibFunction foo, char baseMask)
        {
            Function = foo;

            Parameters = new();

            foreach (Attribute attr in GetCustomAttributes(foo.Method))
            {
                if (attr is ExNativeParamBase exNativeParam)
                {
                    Parameters.Add(new(exNativeParam));
                }
                else if (attr is ExNativeFuncDelegate exNative && exNative.BaseTypeMask == baseMask)
                {
                    InitializeFromNativeBase(exNative);
                }
            }

            if (NumberOfParameters != int.MaxValue)
            {
                Parameters = null;
            }
            else
            {
                Parameters = new(Parameters.OrderBy(x => x.Index));
            }

            SetNumberOfParameters();
            SetNumberOfParameterCombinedMask();
            SetReturnInfo();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Name = null;
                    Function = null;
                    Description = null;

                    Disposer.DisposeList(ref Parameters);

                    ReturnsType = null;
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

        private object GetDebuggerDisplay()
        {
            return string.Format("RegFunc {0}", Name);
        }
    }
}
